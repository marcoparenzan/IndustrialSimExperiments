# IndustrialSimExperiments

A set of .NET 10 C# simulations for:
- Three‑phase supply → VFD (drive) → induction motor
- Anomalies, trips, thermal dynamics
- Explicit I/O wiring between devices
- Conveyor simulation with 5 VFD/motor segments and package tracking
- Optional online anomaly detection (streaming and ML.NET)

This repo targets C# 13 and .NET 10.

## Project map

- IndustrialSimLib: Simulation basics (ISimState, events, logging, faults)
- ThreePhaseSupplySimLib: Three-phase grid supply device (Settings/State/Inputs/Outputs)
- VFDSimLib: VFD model (VfdSettings/State/Inputs/Outputs, thermal, trips)
- InductionMotorLib: Induction motor model (Settings/State, Inputs/Outputs)
- VFDSimApp: Single VFD + motor scenario with anomaly events and textual output
- ConveyorSimApp: 5-segment conveyor, each with a VFD and motor; packages move along
- PackageSimLib: Simple package entity used by the conveyor app
- VfdSimApp/Wiring.md: Reference wiring notes for a real VFD↔motor system

## Design principles

1) Device state vs I/O separation
- State classes store internal physics/thermal/trip variables.
- I/O classes represent “wiring” between devices and only carry input/output signals.
- Program wires Outputs → Inputs explicitly each step.

2) Deterministic step loop
- Each device implements Step(dt, ISimState) (and VFD also Step2 for thermal/trips).
- Program orchestrates: Supply → VFD → Motor, then feedback MotorCurrent to VFD.

3) Event-driven anomalies and logs
- Time-ordered SimEvent[] schedule toggles (undervoltage, phaseloss, jam, etc.).
- ISimState.Log and ISimState.Trip record events to a shared log.

## Three‑phase supply model

Namespace: VFDSimLib

- Settings: NominalVoltageLL, NominalFrequency, slew rates, under/over-voltage PU, drift.
- State: TargetVoltageLL, TargetFrequency, anomaly toggles (An_UnderVoltage, An_OverVoltage, An_FrequencyDrift).
- Outputs: LineLineVoltage (Vrms L‑L) and Frequency (Hz).
- Behavior: Slew outputs toward target. Apply anomalies as multiplicative or additive perturbations before slewing.

Recommended PU to match legacy VFD behavior:
- UnderVoltPU = 0.50
- OverVoltPU = 1.25

## VFD model

Namespace: VFDSimLib

- Settings: RatedVoltageLL, RatedFrequency, Accel/Decel ramps, VoltBoost, MaxCurrent, Over/Under-voltage thresholds, Ambient, ThermalTimeConstant, MaxHeatsinkTemp, OverCurrentMultiple.
- State: TargetFrequency, BusVoltage, HeatsinkTemp, anomaly toggles (PhaseLoss, GroundFault, etc.).
- Inputs: SupplyVoltageLL, SupplyFrequency, MotorCurrentFeedback.
- Outputs: OutputFrequency, OutputVoltage.
- Step logic:
  - BusVoltage = sqrt(2) × SupplyVoltageLL (overridden by VFD-side anomalies if set).
  - Ramp OutputFrequency to TargetFrequency using Accel/Decel.
  - V/f + boost for OutputVoltage, capped to RatedVoltageLL.
- Step2 logic:
  - Thermal: heatsink RC with simple conduction loss ~ 2%·Vout·I.
  - Trips:
    - UnderVoltage/OverVoltage: compare BusVoltage to configured PU thresholds of rated DC.
    - OverCurrent: compare MotorCurrentFeedback to MaxCurrent × OverCurrentMultiple.
    - OverTemp: HeatsinkTemp > MaxHeatsinkTemp.
    - PhaseLoss (latched if persistent), GroundFault (instant).

## Induction motor model

Namespace: InductionMotorSimLib

- Settings: RatedVoltageLL, RatedFrequency, PolePairs, RatedPower, RatedSpeedRpm, Inertia, Visc/Coulomb friction, SlipNom, TorqueMaxPU, Inom, load torque parameters.
- State: SpeedRpm, ElectTorque, Trated (= RatedPower / ω_rated), VratedPhPh, anomaly toggles.
- Inputs: DriveFrequencyCmd, DriveVoltageCmd.
- Outputs: PhaseCurrent (feedback to VFD).
- Core equations:
  - Synchronous speed (rpm): n_sync = 60·f / PolePairs
  - Slip s = (n_sync − n) / n_sync
  - V/f per-unit: vf_pu = (V_cmd/V_rated) / (f/ f_rated) clamped [0..1.2]
  - Torque (simplified): T_e ≈ Trated · (vf_pu)^2 · s/(s+SlipNom), limited by TorqueMaxPU
  - Load torque: Const + viscous (per rpm) + Coulomb + anomalies (jam, bearing)
  - Dynamics: dω = (T_e − T_load)/J, update SpeedRpm; prevent reverse if commanded f ≥ 0
  - Current proxy: I ≈ |T_e|/Trated · (1/vf_margin) · Inom
  - Phase loss anomaly: I ×= 1.7, T_e ×= 0.6

## Wiring and dataflow

- SupplyOutputs → VfdInputs (SupplyVoltageLL/Frequency)
- VfdOutputs → MotorInputs (DriveVoltageCmd/FrequencyCmd)
- MotorOutputs (PhaseCurrent) → VfdInputs (MotorCurrentFeedback)

The state objects are not used for cross-device wiring.

## VFDSimApp (single drive) overview

- Provides a scenario of anomaly toggles (load jam, under-voltage sag, phase loss) and prints a periodic table of key variables.
- See VfdSimApp/Program.cs for the loop skeleton.

## ConveyorSimApp (5 segments with package tracking)

- Five identical segments (50 m total, 10 m/segment).
- Each segment has its own VFD and motor with explicit Inputs/Outputs and State.
- Packages (0.5–20 kg) are spawned periodically and move along the conveyor.
- Load torque augmentation from packages:
  - Resistive force: F_resist = m·g·μ
  - Inertial: F_inertial ≈ m·dv/dt (based on belt speed changes per segment)
  - Reflected torque at motor shaft: T_pkg = (F·R_drum)/(GearRatio·η)
  - Segment’s ConstLoadTorque := BaseConstTorque + T_pkg before Motor.Step
- Wire sequence per segment each tick:
  1) Feed supply → VFD inputs
  2) VFD Step
  3) VFD outputs → motor inputs
  4) Update dynamic load torque from packages
  5) Motor Step
  6) Motor current → VFD inputs
  7) VFD Step2 (thermal + trips)
- Packages advance by the current segment’s belt speed. Removed at loading bay (end of line).
- Console output prints per-segment rows plus a few package positions.

## Anomaly detection (streaming + ML options)

Online streaming detector (EWMA + CUSUM)
- Low-latency, no training: detect spikes and drifts on any scalar telemetry (current, temperature, DC bus, speed, torque).
- Recommended to log anomalies via ISimState.Log, keep trip logic physics/threshold-based.

Example detector (add as IndustrialSimLib/StreamingAnomalyDetector.cs):