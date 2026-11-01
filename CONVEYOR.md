# The Conveyor machine

What `ForgeSimApp` simulates by default: a belt conveyor built from independently-driven segments, carrying packages, fed by a shared grid supply. This document is the deep dive — architecture, physics, configuration, and the tags it exposes. For the surrounding host/protocol architecture see [README.md](README.md); for how to add a *new* machine module see [EXTENDING.md](EXTENDING.md).

## The physical picture

```
Grid ─▶ ThreePhaseSupply ─┬─▶ Segment 0: Vfd ─▶ InductionMotor ─▶ pulley ─▶ belt
                          ├─▶ Segment 1: Vfd ─▶ InductionMotor ─▶ pulley ─▶ belt
                          ├─▶ Segment 2: Vfd ─▶ InductionMotor ─▶ pulley ─▶ belt
                          ├─▶ Segment 3: Vfd ─▶ InductionMotor ─▶ pulley ─▶ belt
                          └─▶ Segment 4: Vfd ─▶ InductionMotor ─▶ pulley ─▶ belt

Packages (0.5–20 kg) spawn at position 0 on a timer, ride the belt segment to segment,
and disappear once they roll off the far end.
```

One shared **three-phase supply** feeds `SegmentCount` (default 5) identical **segments**, each with its own **VFD** (variable-frequency drive) driving its own **induction motor**, geared down to a pulley that moves that stretch of belt. Every segment runs the same target frequency and mechanical ratios, so under no load the whole belt moves at one uniform speed — but each segment's motor reacts independently to whatever package load happens to be sitting on it at that instant, which is what makes per-segment current/temperature/speed worth watching individually rather than modeling the belt as one lump.

## Two implementations, one contract

Both live in `ConveyorSimLib` and implement `IMachineModule` identically — same `Name`-adjacent tag paths, same `ConveyorOptions` config shape, same `Machines:Conveyor` configuration section — so they're interchangeable via `Simulation:Machine` / `--machine` with no other config changes:

| | `ConveyorMachine` | `ConveyorPySharpMachine` |
|---|---|---|
| `--machine` value | `Conveyor` | `ConveyorPySharp` (the current default) |
| Physics lives in | C#: `ThreePhaseSupplySimLib`, `VfdSimLib`, `InductionMotorSimLib`, `PackageSimLib` | Python: `Scripts/conveyor_machine.py`, run inside a `PySharp.Interpreter` `PyEngine` |
| `Name` tag | `Conveyor` | `Conveyor (PySharp)` |
| Extra tags | — | `Machine.Running`, `Machine.ActiveTrip` (see [Anomalies and trips](#anomalies-and-trips)) |

`conveyor_machine.py` is a line-by-line port of the same equations described below — its own header states the intent explicitly: no randomness anywhere, so a `--machine Conveyor` run and a `--machine ConveyorPySharp` run given the same config should track each other tick for tick. This was verified in practice: a live run of each produced matching motor speeds/currents/voltages for the same elapsed simulation time. `ConveyorSimLib.csproj` keeps `ProjectReference`s to all four device libraries (for `ConveyorMachine`) alongside the `PySharp.Interpreter` package (for `ConveyorPySharpMachine`) — both implementations ship in the same assembly.

Why maintain two: the Python port is a from-scratch reimplementation exercising `PySharp.Interpreter` (embedding a Python interpreter in a .NET host) rather than reusing the C# device libraries — useful both as a PySharp integration test and as a template for scripting future machine variants without touching C#.

## What happens each tick (`Step(deltaTimeSeconds)`)

1. Advance simulation time; spawn a new package at position 0 if the spawn period has elapsed.
2. Step the shared `ThreePhaseSupply` (slews output voltage/frequency toward nominal).
3. For each segment, in order:
   1. Feed the segment's `Vfd` the supply's current output voltage/frequency; step the VFD's frequency/voltage ramp (`Vfd.Step`).
   2. Feed the VFD's output frequency/voltage to the segment's `InductionMotor` as its drive command.
   3. Compute this segment's package load torque (see below) from whatever packages currently sit between its start/end position, using *last* tick's belt speed for the acceleration term.
   4. Step the motor (`InductionMotor.Step`): new speed, torque, phase current.
   5. Feed the motor's phase current back to the VFD as feedback; step the VFD's thermal/trip logic (`Vfd.Step2`).
4. Move every package forward by its current segment's belt speed × dt; drop packages that reach the end of the belt.

This Vfd → Motor → Vfd-feedback ordering (steps i–v) is the concrete example `EXTENDING.md` points to for how a machine module should wire devices together.

## Device physics (as configured for the Conveyor)

Three-phase supply — `NominalVoltageLL` 400 V, `NominalFrequency` 50 Hz, slews toward target at 500 V/s and 10 Hz/s.

VFD (per segment; background and model coverage in [docs/vfd/VFD.md](docs/vfd/VFD.md)) — rated 400 V / 50 Hz, `Accel`/`Decel` 10 Hz/s, `VoltBoost` 7% at low speed (V/f + boost curve), heatsink thermal RC (`ThermalTimeConstant` 40 s, ambient 25 °C, trip above 85 °C), DC bus voltage sags from √2·V_LL at no load to 1.35·V_LL at full current (`BusFullLoadFactor`) and trips on under/over-voltage (0.55/1.20 × `VdcNom` = √2·rated V_LL), over-current (1.6× `MaxCurrent`=30 A), or ground fault/phase-loss anomalies.

Induction motor (per segment) — ~7.5 kW / 1440 rpm rated, 2 pole pairs, inertia 0.20 kg·m², nominal slip 3%, torque capped at 2.2× rated. Speed comes from net torque (electromagnetic − load) over inertia, integrated semi-implicitly so the stiff slip law stays stable at low drive frequency; phase current from torque and the V/f margin. When the rotor runs faster than the field (slip < 0, e.g. while the drive decelerates) the torque is negative and `ShaftPowerW` is negative: the motor regenerates into the VFD.

Package — a mass sampled once at spawn (`PackageMassKg`, clamped 0.5–20 kg by `PackageSimLib.Package`'s constructor) and a belt position that advances with whichever segment it's currently on.

## Package load model

Every tick, each segment sums the mass of packages currently within its `[StartMeters, EndMeters)` span and reflects their rolling resistance and inertia back to the motor as extra load torque:

```
F_resist   = mass · 9.81 · RollingCoefficient
F_inertial = mass · (beltSpeed − lastBeltSpeed) / dt        // uses last tick's belt speed
T_package  = (F_resist + F_inertial) · PulleyRadiusMeters / (GearRatio · MechanicalEfficiency)
ConstLoadTorque = BaseLoadTorque (3 Nm, fixed) + T_package
```

`BeltSpeed(rpm) = 2π · rpm/60 · PulleyRadiusMeters / GearRatio` converts motor shaft speed to linear belt speed and is used both for this torque calculation and to advance package positions.

## Configuration (`Machines:Conveyor`, bound to `ConveyorOptions`)

| Field | Default | Meaning |
|---|---|---|
| `SegmentCount` | 5 | Number of independently-driven belt segments |
| `LengthMeters` | 50 | Total belt length; divided evenly across segments |
| `TargetFrequencyHz` | 30 | VFD target frequency for every segment (belt run speed) |
| `PackageSpawnPeriodSeconds` | 1 | A new package spawns at position 0 this often |
| `PackageMassKg` | 5 | Mass of each spawned package (clamped 0.5–20 kg) |
| `PulleyRadiusMeters` | 0.15 | Drive pulley radius |
| `GearRatio` | 12 | Motor-shaft-to-pulley gear reduction |
| `MechanicalEfficiency` | 0.9 | Drivetrain efficiency used in the load-torque reflection |
| `RollingCoefficient` | 0.03 | Belt rolling-resistance coefficient |
| `VfdDecelHzPerSecond` | 10 | VFD deceleration ramp (Hz/s) |
| `VfdBrakingResistorOhms` | 0 | Braking resistor on the DC link; 0 = none fitted (no brake chopper) |

## Exposed tags (`SimulationTag` paths, protocol-neutral)

```
Supply.LineLineVoltage                          Supply.Frequency
Packages.Count                                   Packages.Positions (array)
Packages.Masses (array)
Segments.Segment{i}.Vfd.OutputFrequency          Segments.Segment{i}.Vfd.OutputVoltage
Segments.Segment{i}.Vfd.BusVoltage               Segments.Segment{i}.Vfd.BusVoltageNominal
Segments.Segment{i}.Vfd.HeatsinkTemp           Segments.Segment{i}.Vfd.BrakeChopperActive (bool)
Segments.Segment{i}.Vfd.BrakingPowerW            Segments.Segment{i}.Vfd.FaultRelay (bool)
Segments.Segment{i}.Vfd.RunningRelay (bool)      Segments.Segment{i}.Vfd.AnalogOutFrequency (0-10 V)
Segments.Segment{i}.Motor.SpeedRpm               Segments.Segment{i}.Motor.PhaseCurrent
```

Both machines also have `SetTargetFrequency(hertz)` (e.g. `0` stops the belt). Regeneration: a fast stop (high `VfdDecelHzPerSecond`) charges the DC link; without a braking resistor the VFD trips on `OverVoltage`, with one the brake chopper switches on above 1.15 × `VdcNom` and off below 1.10 ×.

`i` ranges `0..SegmentCount-1`. `ConveyorPySharpMachine` exposes two more: `Machine.Running` (bool) and `Machine.ActiveTrip` (string, empty when no trip is active) — see below. Every protocol adapter (OPC UA, MQTT, the console dashboard) maps these same paths onto its own wire format with no Conveyor-specific code — see README's per-protocol sections for exactly how.

## Anomalies and trips

The device `State` objects (`ThreePhaseSupplyState.An_UnderVoltage`, `VfdState.An_PhaseLoss`, `InductionMotorState.An_LoadJam`, ...) all exist and are read by their device's `Step`, but **nothing in either Conveyor implementation ever sets them true** — `ConveyorMachine` constructs its `SimState` with a no-op anomaly-toggle callback (`new SimState((_, _) => { })`), and the Python port carries the same booleans as constants fixed at `False`. So under normal operation they're inert.

**Trip detection is live regardless**, because it runs off real physics, not the anomaly flags: `Vfd.Step2`'s `DetectTrips` checks bus voltage against the under/over-voltage thresholds and motor current against the over-current threshold every tick, using values the simulation actually computes. A trip latches the first fault (`SimState.Trip`), sets `Running = false`, and downstream VFD/motor outputs go to zero until something calls `ResetTrip()` — which nothing currently does automatically, so a tripped Conveyor stays tripped for the rest of the run. `ConveyorPySharpMachine`'s bonus `Machine.Running`/`Machine.ActiveTrip` tags are how a protocol client can actually observe this state; `ConveyorMachine` doesn't expose it as a tag today (only visible in-process via `IMachineModule`, not over OPC UA/MQTT).

If you want to actually exercise a trip: with default settings the conveyor runs comfortably within limits indefinitely, so triggering one means editing `Machines:Conveyor` config toward the edges (e.g. a `PackageMassKg` heavy enough to push a segment's current past 1.6× `MaxCurrent`=30 A) rather than flipping a switch at runtime.

## Running it

```bash
dotnet run --project src/ForgeSimApp                              # ConveyorPySharp (current default)
dotnet run --project src/ForgeSimApp -- --machine Conveyor         # the C# implementation instead
```

See [README.md](README.md#running-forgesimapp) for the full set of `--protocols`/`--speed-factor`/etc. options, and [FORGE-DATA-GATEWAY.md](FORGE-DATA-GATEWAY.md) for polling these tags over OPC UA and republishing them to MQTT.
