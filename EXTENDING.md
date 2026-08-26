# Extending ForgeSimApp

`ForgeSimApp` is a thin host: `SimulationCommand` (`src/ForgeSimApp/Cli/SimulationCommand.cs`, a Spectre.Console.Cli `AsyncCommand`) reads configuration + CLI options, resolves one `IMachineModule` and a set of `IProtocolAdapter`s from DI, and hands off to `SimulationWorker` (`src/ForgeSimApp/SimulationWorker.cs`), which steps/publishes them in a loop. Neither the host nor the protocol layer knows anything about conveyors specifically — everything machine-specific lives behind `IMachineModule`, and everything protocol-specific lives behind `IProtocolAdapter`. Both contracts live in `src/IndustrialSimLib`.

There's a third, lower layer below both: **devices** (`ThreePhaseSupplySimLib`, `VfdSimLib`, `InductionMotorLib`, `PackageSimLib`, under `/Devices` in `IndustrialSimLib.slnx`) are the individual pieces of physical equipment a machine module composes and steps — a VFD, a motor, a grid supply. A machine module owns instances of these, wires their inputs/outputs to each other, and derives its `SimulationTag`s from their state. Adding a new kind of physical equipment means adding a device; adding a new arrangement of equipment exposed as a runnable simulation means adding a machine module.

## Adding a new device

A device is a standalone physics model with no knowledge of machines, tags, or protocols — it only depends on `IndustrialSimLib`. Every existing device (`ThreePhaseSupply`, `Vfd`, `InductionMotor`) follows the same four-class shape; `ThreePhaseSupplySimLib` is the simplest complete example to copy from.

1. Create a new class library, e.g. `src/PressSimLib/PressSimLib.csproj`, referencing only `IndustrialSimLib`.
2. Split its data into four plain classes (see `ThreePhaseSupplySettings`/`ThreePhaseSupplyState`/`ThreePhaseSupplyIo.cs`):

   ```csharp
   // Design-time constants: rated values, thresholds, time constants. Plain properties, not Bindable.
   public class PressSettings
   {
       public double RatedForceKN { get; set; } = 500.0;
       public double StrokeRateHz { get; set; } = 0.5;
   }

   // Internal runtime state: physical state plus anomaly toggles (An_* prefix convention),
   // as Bindable cells so protocol adapters and diagnostics can read/observe them.
   public class PressState
   {
       public DoubleBindable PositionMm { get; } = new();
       public BoolBindable An_JammedDie { get; } = new();
   }

   // Inputs: signals wired in from other devices. Outputs: signals this device exposes outward.
   public class PressInputs
   {
       public DoubleBindable DriveTorque { get; } = new();
   }

   public class PressOutputs
   {
       public DoubleBindable ForceKN { get; } = new();
   }
   ```

   `DoubleBindable`/`BoolBindable`/`Int32Bindable` (`src/IndustrialSimLib`) implicitly convert to/from their underlying primitive, so they read like plain values (`if (state.An_JammedDie)`, `inputs.DriveTorque > 0`) while still exposing `.Set(value)`, `.Add(delta)`, `.Reset()` for mutation — devices should read plain values but only mutate through `Set`/`Add`/`Reset`, never assign a Bindable field itself (that would rebind the reference, breaking anything holding onto the original instance, e.g. an `SimulationTag`'s closure).
3. Implement `IDeviceSimulator` on a class taking all four in its constructor:

   ```csharp
   public class Press(PressSettings settings, PressState state, PressInputs inputs, PressOutputs outputs) : IDeviceSimulator
   {
       public void Step(double dt, ISimState simState)
       {
           // read inputs/state, apply anomalies, write outputs/state via Set/Add
       }
   }
   ```

   If the device needs to trip (stop the machine on a fault), define a `PressFaultCode : FaultCode` record with static instances (see `VfdFaultCode`) and call `simState.Trip(PressFaultCode.SomeFault)`; check `simState.Running` at the top of `Step` to hold/reset outputs while tripped (see `Vfd.Step`'s early-return branch).
4. If the device's physics needs a second pass after other devices have produced feedback it depends on, add a second method (by convention `Step2(double dt, ISimState simState)`, not part of `IDeviceSimulator`) — the owning machine module calls it explicitly at the right point in its own loop. `Vfd` does this: `Step` ramps output frequency/voltage using only its own inputs, and `Step2` computes thermal rise and detects trips using `MotorCurrentFeedback`, which only becomes available after the motor has stepped off the VFD's `Step` output. A device with no such cross-dependency only needs `Step`.
5. Add the project to `src/IndustrialSimLib.slnx` under `/Devices`, and reference it from whichever machine module(s) will use it.
6. Add xUnit tests in `IndustrialSim.Tests` mirroring `Vfd_RampsFrequency_AndTripsOnUndervoltage` or `Motor_AcceleratesAndProducesCurrent` — construct settings/state/inputs/outputs, call `Step` (and `Step2` if present) a few times, and assert on the resulting output/state values and, if applicable, `simState.ActiveTrip`.

Not every reusable class needs to be a full device: `Package` (`src/PackageSimLib/Package.cs`) is a plain data entity (mass + position as `Bindable`s) with no `Step` of its own — it's moved and loaded by the machine module that owns it (`ConveyorMachine.MovePackages`). Use this simpler shape for passive entities that don't have independent physics.

## Adding a new machine module

1. Create a new class library, e.g. `src/PalletizerSimLib/PalletizerSimLib.csproj`, referencing `IndustrialSimLib` and whichever `*SimLib` device projects it needs (`InductionMotorLib`, `VfdSimLib`, ...).
2. Implement `IMachineModule`:

   ```csharp
   public sealed class PalletizerMachine : IMachineModule
   {
       public string Name => "Palletizer";
       public double SimulationTime => simState.Time;
       public IReadOnlyCollection<SimulationTag> Tags => tags;

       public void Step(double deltaTimeSeconds)
       {
           simState.Step(deltaTimeSeconds);
           // step owned devices, update derived state
       }
   }
   ```

   In the constructor, build each device from a `Settings`/`State`/`Inputs`/`Outputs` set and keep the instances around (`ConveyorMachine.CreateSegment` does this for a VFD + motor pair). In `Step`, wire devices together by copying one device's `Outputs` into the next device's `Inputs` with `.Set(...)` between calling their `Step` methods, in dependency order — `ConveyorMachine.Step` does `supply.Step(...)` → copy `supplyOutputs` into each segment's `VfdInputs` → `segment.Vfd.Step(...)` → copy `VfdOutputs` into `MotorInputs` → `segment.Motor.Step(...)` → copy the motor's current feedback back into `VfdInputs` → `segment.Vfd.Step2(...)`. This copy-by-value wiring (rather than sharing `Bindable` instances between devices) is what keeps each device's `Inputs`/`Outputs` an honest, self-contained contract.

   Build `Tags` once (in the constructor, like `ConveyorMachine.BuildTags`) as a list of `SimulationTag.Create("Path.Segment", () => someBindable.Value)`. Tag paths use `.` as the hierarchy separator — protocol adapters turn that into their own addressing (OPC UA turns it into folders).
3. Add an `Options` class (e.g. `PalletizerOptions`) if the machine needs configuration, following `ConveyorOptions`.
4. Add the project to `src/IndustrialSimLib.slnx` under `/Machines`, and add a `ProjectReference` from `ForgeSimApp.csproj`.
5. Register it in `src/ForgeSimApp/Cli/SimulationCommand.cs`:

   ```csharp
   builder.Services.Configure<PalletizerOptions>(builder.Configuration.GetSection("Machines:Palletizer"));
   builder.Services.AddSingleton<PalletizerMachine>(...);
   builder.Services.AddSingleton<IMachineModule>(services =>
   {
       string machine = builder.Configuration["Simulation:Machine"] ?? "Conveyor";
       return machine switch
       {
           "Conveyor" => services.GetRequiredService<ConveyorMachine>(),
           "Palletizer" => services.GetRequiredService<PalletizerMachine>(),
           _ => throw new InvalidOperationException($"Unknown machine module '{machine}'.")
       };
   });
   ```

6. Select it by setting `"Simulation:Machine": "Palletizer"` in `appsettings.json` (or `FORGESIM_Simulation__Machine=Palletizer`, or `--machine Palletizer`), and add a `Machines:Palletizer` configuration section.
7. Add xUnit tests in `IndustrialSim.Tests` mirroring `Conveyor_AdvancesAndExposesProtocolNeutralTags` — step the machine, assert `SimulationTime` advances and every tag has a non-null value.

## Adding a new protocol adapter

1. Create a new project, e.g. `src/ModbusServerLib/ModbusServerLib.csproj`, referencing `IndustrialSimLib` and whatever Modbus library you choose.
2. Implement `IProtocolAdapter`:

   ```csharp
   public sealed class ModbusProtocolAdapter(int port) : IProtocolAdapter
   {
       public string Name => "Modbus";

       public Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default)
       {
           // map machine.Tags to registers once, start the Modbus server
       }

       public Task PublishAsync(CancellationToken cancellationToken = default)
       {
           // write current tag.Value into the mapped registers
       }

       public Task StopAsync(CancellationToken cancellationToken = default) { /* stop the server */ }
       public ValueTask DisposeAsync() => ...;
   }
   ```

   Three reference implementations exist. `OpcUaProtocolAdapter` (`src/OpcUaServerLib/OpcUaProtocolAdapter.cs`) builds an OPC UA address space from `machine.Tags` once in `StartAsync`, then copies `tag.Value` into the corresponding node on every `PublishAsync`. `MqttProtocolAdapter` (`src/MqttServerLib/MqttProtocolAdapter.cs`) maps each tag path to an MQTT topic string and, on every `PublishAsync`, JSON-serializes `tag.Value` and injects a retained message into its embedded `MQTTnet.Server` broker. `ConsoleDashboardProtocolAdapter` (`src/ConsoleDashboardLib/ConsoleDashboardProtocolAdapter.cs`) instead renders `machine.Tags` as rows of a Spectre.Console live table, refreshing on a throttle rather than every tick — and is a useful example of a "protocol" adapter that isn't a network protocol at all, plus of guarding a presentation-layer adapter against non-interactive environments (it no-ops when `AnsiConsole.Profile.Capabilities.Interactive` is false, since Spectre's live display can hang if driven against a redirected/piped stdout). A register-based protocol (Modbus/S7) would instead assign each tag a fixed offset in `StartAsync` and write raw bytes/words in `PublishAsync`.
3. Add the project under `/Protocols` in `IndustrialSimLib.slnx`, and reference it from `ForgeSimApp.csproj`.
4. Register it as another `IProtocolAdapter` in `src/ForgeSimApp/Cli/SimulationCommand.cs`:

   ```csharp
   builder.Services.AddSingleton<IProtocolAdapter>(_ => new ModbusProtocolAdapter(
       builder.Configuration.GetValue("Protocols:Modbus:Port", 502)));
   ```

5. Add `"Modbus"` to `Simulation:Protocols` in `appsettings.json` to enable it by default (multiple adapters can run at once — `SimulationWorker` starts and publishes to all enabled adapters every tick). Optionally expose it on the CLI too: add a nullable property to `SimulationSettings` (`src/ForgeSimApp/Cli/SimulationSettings.cs`) with a `[CommandOption]` attribute, then map it to a config key in `SimulationCommand.BuildOverrides`, following `MqttPort`/`--mqtt-port` as a template.

## Notes

- Tag paths and types are the only contract between a machine and a protocol adapter — keep machine code free of any protocol-specific concerns (no OPC UA / MQTT / Modbus / S7 / Spectre.Console types in `*SimLib` machine projects).
- `SimulationWorker`'s tick period is derived from `Simulation:StepSeconds / Simulation:SpeedFactor` and is floored at 1ms by the underlying `PeriodicTimer` (see README's "Running ForgeSimApp" section) — this floor applies regardless of which machine or protocol is active.
- Configuration precedence is `appsettings.json` < `FORGESIM_`-prefixed environment variables < CLI options. `Simulation:Protocols`/`--protocols` is the one exception: because .NET configuration merges array values by index across providers rather than replacing them outright, a shorter `--protocols` override would otherwise leak trailing entries from `appsettings.json` through. `SimulationCommand` sidesteps this by resolving the effective protocol list directly from `SimulationSettings` and injecting it as `IReadOnlyList<string>`, rather than routing it back through `IConfiguration`'s array binding — keep that pattern for any other list-valued CLI option.
