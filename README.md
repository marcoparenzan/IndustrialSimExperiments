# IndustrialSimExperiments

Industrial simulation playground targeting .NET 10 and C# 13:
- Three‑phase supply → VFD (drive) → induction motor
- Explicit separation of device State vs Inputs/Outputs (wiring)
- Anomalies, trips, thermal dynamics
- Conveyor simulation (5 segments, 50 m) with package tracking
- Built‑in OPC UA server exposing all state/IO and package data

Requires Visual Studio 2022 and .NET 10 SDK (preview). NuGet: OPCFoundation.NetStandard.Opc.Ua for OPC UA.

## Solution map

- IndustrialSimLib: Simulation primitives (ISimState, events, logging, trips)
- ThreePhaseSupplySimLib: Grid supply device (Settings/State/Inputs/Outputs)
- VFDSimLib: VFD device (Settings/State/Inputs/Outputs, thermal, trips)
- InductionMotorLib: Induction motor (Settings/State, Inputs/Outputs)
- VFDSimApp: Single VFD + motor scenario with timed anomalies
- ConveyorSimApp: 5‑segment conveyor with packages, and an OPC UA server
- VfdSimApp/Wiring.md: Reference wiring notes for real VFD↔motor

## Design: State vs I/O

- State classes: internal physics/thermal/trip variables (not used for wiring).
- Inputs/Outputs classes: only “externally wired” signals.
- Program orchestrates the dataflow: Supply → VFD → Motor → VFD feedback.

## Models (simplified)

Three‑phase supply (ThreePhaseSupplySimLib)
- Settings: NominalVoltageLL, NominalFrequency, slew rates, UnderVoltPU=0.50, OverVoltPU=1.25 (match legacy).
- State: TargetVoltageLL, TargetFrequency, anomalies (Under/OverVoltage, FrequencyDrift).
- Outputs: LineLineVoltage (Vrms L‑L), Frequency (Hz).
- Behavior: Slew outputs toward targets with anomalies applied to setpoints.

VFD (VFDSimLib)
- Settings: RatedVoltageLL/Frequency, Accel/Decel, VoltBoost, MaxCurrent, Under/Over‑voltage thresholds, Ambient, ThermalTimeConstant, MaxHeatsinkTemp, OverCurrentMultiple.
- State: TargetFrequency, BusVoltage, HeatsinkTemp, anomalies (PhaseLoss, GroundFault, Under/OverVoltage).
- Inputs: SupplyVoltageLL/Frequency, MotorCurrentFeedback.
- Outputs: OutputFrequency/OutputVoltage.
- Step: ramp OutputFrequency to Target, V/f+boost for OutputVoltage; BusVoltage = √2 × SupplyVoltageLL (unless local anomalies override).
- Step2: thermal RC (losses ≈ 2%·Vout·I), trips: UV/OV, OverCurrent, OverTemp, PhaseLoss, GroundFault.

Induction motor (InductionMotorLib)
- Settings: RatedVoltageLL/Frequency, PolePairs, RatedPower/Speed, Inertia, Visc/Coulomb friction, SlipNom, TorqueMaxPU, Inom, load torque coefs.
- State: SpeedRpm, ElectTorque, Trated (=P/ωrated), VratedPhPh; anomalies: PhaseLoss, LoadJam, BearingWear, SensorNoise.
- Inputs: DriveFrequencyCmd, DriveVoltageCmd. Outputs: PhaseCurrent.
- Core: synchronous speed ns=60·f/PolePairs; slip s=(ns−n)/ns; torque ∝ (V/f)^2 · s/(s+SlipNom) · Trated clamped by TorqueMaxPU; speed dynamics from (Te−Tload)/J; I proxy from |Te|/Trated and V/f margin. PhaseLoss: I×1.7, Te×0.6.

## ConveyorSimApp (5 segments, 50 m)

- 5 identical segments (SegmentLengthM=10 m). Each has its own VFD + motor.
- Mechanicals: GearRatio=12, PulleyRadius=0.15 m, MechEfficiency=0.9, rolling μ=0.03.
- Packages (0.5–20 kg) spawn periodically and move with the belt.
- Per‑segment load torque augmentation (reflected to motor):
  - F_resist = m·g·μ
  - F_inertial ≈ m·dv/dt (belt acceleration)
  - T_pkg = (F·R_drum)/(GearRatio·η)
  - Apply: MotorSettings.ConstLoadTorque = BaseConstTorque + T_pkg each tick.
- Prints a per‑segment table (frequency, volts, current, rpm, belt speed, packages, temperature, Vdc).

Run:
- dotnet run --project ConveyorSimApp
- Default: 600 s sim time, speedFactor=0.5 (slower than real‑time), 2 s start delay.

CLI args / environment
- Args: [0]=SIM_DURATION_SEC, [1]=SIM_SPEED_FACTOR, [2]=SIM_START_DELAY_SEC
- Env: SIM_DURATION_SEC, SIM_SPEED_FACTOR, SIM_START_DELAY_SEC
- speedFactor: 1.0=real‑time, 0.5=half‑speed (slower), 2.0=2× faster.

## OPC UA server (ConveyorSimApp)

- Endpoint: opc.tcp://localhost:4840/ConveyorSim
- Security: None enabled (MessageSecurityMode=None) for local testing
- Certificate stores created at runtime under ./pki/{own,trusted,issuers,rejected}
- Address space root: Objects/Conveyor (linked via Organizes to show under Objects)

Published nodes (browse)
- Objects/Conveyor/Supply
  - LineLineVoltage, Frequency, TargetVoltageLL, TargetFrequency
  - An_UnderVoltage, An_OverVoltage, An_FrequencyDrift
- Objects/Conveyor/Segments/Segment{i}/VFD/State
  - TargetFrequency, BusVoltage, HeatsinkTemp
  - An_UnderVoltage, An_OverVoltage, An_PhaseLoss, An_GroundFault
- Objects/Conveyor/Segments/Segment{i}/VFD/Inputs
  - SupplyVoltageLL, SupplyFrequency, MotorCurrentFeedback
- Objects/Conveyor/Segments/Segment{i}/VFD/Outputs
  - OutputFrequency, OutputVoltage
- Objects/Conveyor/Segments/Segment{i}/Motor/State
  - SpeedRpm, ElectTorque, Trated, VratedPhPh
  - An_PhaseLoss, An_LoadJam, An_BearingWear, An_SensorNoise
- Objects/Conveyor/Segments/Segment{i}/Motor/Inputs
  - DriveFrequencyCmd, DriveVoltageCmd
- Objects/Conveyor/Segments/Segment{i}/Motor/Outputs
  - PhaseCurrent
- Objects/Conveyor/Packages
  - Count, Positions (Double[]), Masses (Double[])

NodeId pattern (ns assigned at runtime, often ns=2)
- ns=2;s=Conveyor.Supply.LineLineVoltage
- ns=2;s=Conveyor.Segments.Segment0.VFD.State.BusVoltage
- ns=2;s=Conveyor.Segments.Segment0.VFD.Outputs.OutputFrequency
- ns=2;s=Conveyor.Segments.Segment0.Motor.Outputs.PhaseCurrent
- ns=2;s=Conveyor.Packages.Positions

Using Traeger OPC Watch
- Connect to opc.tcp://localhost:4840/ConveyorSim.
- Browse Objects → Conveyor → Supply/Segments/Packages to find tags.
- Accept certificates if prompted (server auto‑accepts clients in dev).

Note: Variables are writable at the address space level but are overwritten every tick by the simulation. Treat them as read‑only unless you implement write‑through handling.

## VFDSimApp (single drive)

- Similar device models and anomalies:
  - Toggles include: load jam, undervoltage sag, phase loss, bearing wear (examples).
- Output prints a single row per sample and a final event log.

## Anomaly detection (optional)

Streaming detector (EWMA z‑score + CUSUM) for spikes/drifts, fully online, no training:
- Add IndustrialSimLib/StreamingAnomalyDetector.cs.
- Use in VFD Step2 for I/T_hs/Vdc and Motor Step for Speed/Torque: detector.Add(value,time) then ISimState.Log anomalies.
- You can also use ML.NET SR‑CNN if you prefer a packaged detector (Microsoft.ML.TimeSeries).

## Wiring reference

See VfdSimApp/Wiring.md for an ASCII diagram and control I/O examples (use manufacturer manuals and local codes in real systems).

## Build

- Requirements: .NET 10 SDK (preview) and VS 2022.
- Restore NuGet packages (OPCFoundation.NetStandard.Opc.Ua for ConveyorSimApp).
- Build/Run: dotnet build, dotnet run --project ConveyorSimApp

## Safety

These are simplified models for simulation/education; do not use as‑is for safety or protection design. Follow manufacturer documentation and electrical codes for real hardware.

## License

See repository license (if provided).