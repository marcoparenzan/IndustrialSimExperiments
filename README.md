# IndustrialSimExperiments

Modular industrial-process simulation platform targeting .NET 10 / C# 13. A configurable **machine module** (e.g. a conveyor) is stepped in real time and exposed over one or more pluggable **protocol adapters** (OPC UA, MQTT, a live console dashboard), driven by a [Spectre.Console.Cli](https://spectreconsole.net/cli/) command-line interface, so simulated processes can be browsed and polled with off-the-shelf industrial tooling.

Requires the .NET 10 SDK. NuGet: `OPCFoundation.NetStandard.Opc.Ua` for the OPC UA server, `MQTTnet` + `MQTTnet.Server` for the MQTT broker, `Spectre.Console.Cli` for the command-line interface, `Spectre.Console` for the live dashboard.

## Solution map

```text
src/
  IndustrialSimLib/          Core: Bindable primitives, ISimState/SimState, SimEvent, FaultCode,
                              and the two extension contracts: IMachineModule, IProtocolAdapter
  InductionMotorLib/         Induction motor device model
  PackageSimLib/              Conveyor package model
  ThreePhaseSupplySimLib/    Grid supply device model
  VfdSimLib/                 VFD (drive) device model
  ConveyorSimLib/            ConveyorMachine: an IMachineModule built from the device models above
  OpcUaServerLib/            OpcUaProtocolAdapter: an IProtocolAdapter that publishes any
                              IMachineModule's tags as an OPC UA address space
  MqttServerLib/             MqttProtocolAdapter: an IProtocolAdapter that hosts an embedded
                              MQTT broker (MQTTnet) and publishes any IMachineModule's tags
                              as retained MQTT topics
  ConsoleDashboardLib/       ConsoleDashboardProtocolAdapter: an IProtocolAdapter that renders
                              any IMachineModule's tags as a live-updating Spectre.Console table
  IndustrialSimApp/          Spectre.Console.Cli host wiring one machine module to one or more
                              protocol adapters, driven by CLI options + appsettings.json
  IndustrialSim.Tests/       xUnit tests for the core, device models, and ConveyorMachine
```

The Visual Studio solution (`IndustrialSimLib.slnx`) groups these into `/Core`, `/Devices`, `/Machines`, `/Protocols`, `/Applications`, `/Tests` folders.

## Design: modular machines and protocols

`IndustrialSimApp` never references a specific machine or protocol directly — it only depends on the `IndustrialSimLib` contracts:

```csharp
public interface IMachineModule
{
    string Name { get; }
    double SimulationTime { get; }
    IReadOnlyCollection<SimulationTag> Tags { get; }
    void Step(double deltaTimeSeconds);
}

public interface IProtocolAdapter : IAsyncDisposable
{
    string Name { get; }
    Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default);
    Task PublishAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
```

A `SimulationTag` is a protocol-neutral `(Path, DataType, Read)` tuple — the machine decides what state it exposes (`Supply.LineLineVoltage`, `Segments.Segment0.Motor.SpeedRpm`, ...) without knowing how it will be transported. A protocol adapter reads `machine.Tags` each publish cycle and maps them onto its own wire format: `OpcUaProtocolAdapter` turns them into OPC UA nodes, `MqttProtocolAdapter` turns them into retained MQTT topics, `ConsoleDashboardProtocolAdapter` turns them into rows of a live terminal table; a future Modbus or S7 adapter would map the same tags onto registers/DB offsets instead.

`SimulationWorker` (in `IndustrialSimApp/SimulationWorker.cs`) is the only place that ties a machine to adapters. Which machine and protocols to use is resolved by `SimulationCommand` (`IndustrialSimApp/Cli/SimulationCommand.cs`) from `appsettings.json`, optionally overridden by CLI options:

- `Simulation:Machine` / `--machine` selects which registered `IMachineModule` to run (currently only `Conveyor`).
- `Simulation:Protocols` / `--protocols` selects which registered `IProtocolAdapter`s to start (`OpcUa`, `Mqtt`, `Console`, all enabled by default — multiple adapters can run at once).

Adding a new machine means implementing `IMachineModule` in a new `*SimLib` project and registering it in `Program.cs`; adding a new protocol means implementing `IProtocolAdapter` in a new `*ServerLib`/`*ClientLib` project. See [EXTENDING.md](EXTENDING.md) for a walkthrough.

## Core primitives (IndustrialSimLib)

- `IBindable` / `DoubleBindable` / `BoolBindable` / `Int32Bindable`: settable, loggable value cells used for device State/Inputs/Outputs so protocol adapters can bind to them by reference.
- `ISimState` / `SimState`: simulation clock, active trip (`FaultCode`), anomaly toggles, and a `Log` callback.
- `SimEvent` / `SimEvents/*`: timed events (e.g. `ToggleAnomalyEvent`, `ResetTripEvent`) that scenarios can schedule against a `SimState`.
- `IDeviceSimulator`: the `Step(dt, ISimState)` contract implemented by each device (`ThreePhaseSupply`, `Vfd`, `InductionMotor`, ...).

## Device models (simplified physics)

Three-phase supply (`ThreePhaseSupplySimLib`)

- Settings: `NominalVoltageLL`, `NominalFrequency`, slew rates, `UnderVoltPU`/`OverVoltPU`.
- Behavior: slews outputs toward targets; anomalies (`UnderVoltage`, `OverVoltage`, `FrequencyDrift`) perturb the setpoints.

VFD (`VfdSimLib`)

- Settings: rated voltage/frequency, accel/decel, boost, max current, thermal constants, trip thresholds.
- Behavior: ramps output frequency to target with V/f + boost; models bus voltage, heatsink thermal RC, and trips on under/over-voltage, over-current, over-temp, phase loss, ground fault.

Induction motor (`InductionMotorLib`)

- Settings: rated voltage/frequency/power/speed, inertia, friction, slip, torque/current limits, load torque.
- Behavior: synchronous speed from pole pairs and frequency, slip-based torque, speed dynamics from net torque and inertia, phase current from torque and V/f margin. Anomalies: phase loss, load jam, bearing wear, sensor noise.

Package (`PackageSimLib`)

- A simple mass (0.5–20 kg) with a position, spawned and moved by the machine that owns it.

## ConveyorMachine (ConveyorSimLib)

An `IMachineModule` composed of a shared `ThreePhaseSupply` feeding N identical segments (each its own VFD + induction motor), plus packages that spawn periodically and travel the belt:

- Mechanicals per segment: `GearRatio`, `PulleyRadiusMeters`, `MechanicalEfficiency`, `RollingCoefficient`.
- Each tick: steps the supply, then per segment feeds supply → VFD → motor → VFD feedback, adds package load torque reflected through the gear ratio, and advances package positions by belt speed.
- Configurable via `ConveyorOptions` (segment count, length, target frequency, package spawn period/mass, mechanicals) — bound from `Machines:Conveyor` in configuration.
- Exposes tags under `Supply.*`, `Packages.*`, and `Segments.Segment{i}.Vfd.*` / `Segments.Segment{i}.Motor.*`.

## Running IndustrialSimApp

```bash
dotnet run --project src/IndustrialSimApp
dotnet run --project src/IndustrialSimApp -- --help
dotnet run --project src/IndustrialSimApp -- --speed-factor 2 --duration 60 --protocols OpcUa --protocols Console
```

Base configuration lives in `src/IndustrialSimApp/appsettings.json`, overridable (in increasing priority) via `INDUSTRIALSIM_` prefixed environment variables using `__` as the section separator (e.g. `INDUSTRIALSIM_Simulation__SpeedFactor=2`), then via CLI options parsed by [`SimulationCommand`](src/IndustrialSimApp/Cli/SimulationCommand.cs) (Spectre.Console.Cli):

```json
{
  "Simulation": {
    "Machine": "Conveyor",
    "Protocols": [ "OpcUa", "Mqtt", "Console" ],
    "StepSeconds": 0.01,
    "SpeedFactor": 1.0,
    "DurationSeconds": 0
  },
  "Machines": {
    "Conveyor": { "SegmentCount": 5, "LengthMeters": 50, "TargetFrequencyHz": 30, "PackageSpawnPeriodSeconds": 1, "PackageMassKg": 5 }
  },
  "Protocols": {
    "OpcUa": { "Endpoint": "opc.tcp://localhost:4840/IndustrialSim" },
    "Mqtt": { "Port": 1883, "TopicRoot": "IndustrialSim" },
    "Console": { "RefreshIntervalMs": 200 }
  }
}
```

- `StepSeconds` (`--step-seconds`): simulated seconds advanced per tick.
- `SpeedFactor` (`-s`/`--speed-factor`): 1.0 = real-time, 2.0 = twice as fast, 0.5 = half speed. The underlying timer floors ticks at 1ms, so effective speed is capped once `StepSeconds / SpeedFactor` drops below 1ms (e.g. with the default `StepSeconds=0.01`, factors above 10 no longer scale linearly).
- `DurationSeconds` (`-d`/`--duration`): `0` runs indefinitely (stop with Ctrl+C); a positive value stops the host once that much simulation time has elapsed.
- `-m`/`--machine`, `-p`/`--protocols`, `--opcua-endpoint`, `--mqtt-port`, `--mqtt-topic-root`, `--console-refresh-ms` mirror the corresponding `appsettings.json` values. `--protocols` fully replaces the configured list (pass it once per protocol, e.g. `--protocols OpcUa --protocols Mqtt`) rather than merging with it. Run with `--help` for the full, auto-generated option list.

## OPC UA server (OpcUaServerLib)

- Endpoint: from `Protocols:OpcUa:Endpoint` (default `opc.tcp://localhost:4840/IndustrialSim`).
- Security: `MessageSecurityMode=None` for local testing; certificate stores are created at runtime under `./pki/{own,trusted,issuers,rejected}`.
- Address space: every `SimulationTag` path is split on `.` and turned into a folder hierarchy under `Objects/<MachineName>` (e.g. `Objects/Conveyor/Segments/Segment0/Motor/SpeedRpm`), so any `IMachineModule` gets a browsable tree with no protocol-specific code.
- Values are refreshed once per `SimulationWorker` tick and are read-only from a client's perspective — they are overwritten by the simulation on the next publish.

Browse with any OPC UA client (e.g. UaExpert): connect to the endpoint above, accept the server's self-signed certificate, and browse `Objects/Conveyor`.

## MQTT broker (MqttServerLib)

- Hosts its own embedded MQTT broker (via `MQTTnet.Server`) on `Protocols:Mqtt:Port` (default `1883`) — no external broker required.
- Topic mapping: every `SimulationTag` path is turned into a topic `<TopicRoot>/<MachineName>/<Path with '.' replaced by '/'>` (e.g. `IndustrialSim/Conveyor/Segments/Segment0/Motor/SpeedRpm`), so any `IMachineModule` is published with no protocol-specific code.
- Payload: each value is JSON-encoded (`System.Text.Json`), so scalars publish as plain JSON numbers/booleans/strings and array tags (e.g. `Packages.Positions`) publish as JSON arrays.
- Messages are published with the **retain** flag, so a client connecting mid-run immediately receives the latest value of every tag instead of waiting for the next tick.

Subscribe with any MQTT client (e.g. `mosquitto_sub`, MQTT Explorer) to `IndustrialSim/#` on `localhost:1883`.

## Console dashboard (ConsoleDashboardLib)

- Renders one row per `SimulationTag` (`Tag` / `Value`) in a Spectre.Console live table titled with the machine name, redrawn in place at most every `Protocols:Console:RefreshIntervalMs` (default 200ms) regardless of the simulation tick rate.
- Requires a real interactive terminal: Spectre's live display drives the terminal directly (cursor control, ANSI capability queries), which can hang if attempted against a redirected/piped stdout. `ConsoleDashboardProtocolAdapter` checks `AnsiConsole.Profile.Capabilities.Interactive` and silently no-ops when it's not running in one (e.g. output piped to a file, or under a test harness) — the other enabled protocols are unaffected.
- Run `dotnet run --project src/IndustrialSimApp` directly in a terminal to see it; disable it with `--protocols OpcUa --protocols Mqtt` (omitting `Console`) if you only want the plain `ILogger` start/stop lines.

## Extending with new devices, machines, or protocols

See [EXTENDING.md](EXTENDING.md) for how to add a new device (a `Settings`/`State`/`Inputs`/`Outputs` + `IDeviceSimulator`, e.g. a new type of motor or actuator), a new machine module composed of devices (`IMachineModule`, e.g. a palletizer), or a new protocol adapter (`IProtocolAdapter`, e.g. Modbus).

## Tests

```bash
dotnet test src/IndustrialSimLib.slnx
```

Covers `Bindable` semantics, timed events, VFD ramp/trip behavior, motor acceleration/current, package defaults, and end-to-end `ConveyorMachine` stepping (including that every exposed tag has a value).

## Build

```bash
dotnet build src/IndustrialSimLib.slnx
```

Requires the .NET 10 SDK. Restores `OPCFoundation.NetStandard.Opc.Ua` for `OpcUaServerLib`, `MQTTnet`/`MQTTnet.Server` for `MqttServerLib`, `Spectre.Console` for `ConsoleDashboardLib`, and `Spectre.Console.Cli` for `IndustrialSimApp`.

## Safety

These are simplified models for simulation and education; do not use as-is for safety or protection system design. Follow manufacturer documentation and applicable electrical codes for real hardware.

## License

See repository license (if provided).
