# Supply and transformer protection

A drive sits at the end of a supply chain: grid, transformer, feeder, then the drive. Protection of that chain is what keeps a fault at the drive from damaging the transformer and keeps a grid disturbance from damaging the drive. This page summarises the common protection functions of a power transformer, identified by their ANSI/IEEE device numbers, and compares them with what `ThreePhaseSupplySimLib` simulates. Source infographic: [images/vfd/SOURCES.md](../images/vfd/SOURCES.md), item 07 (third-party, kept local). The text below is independent.

## Protection functions

| ANSI | Function | What it detects | Typical action |
|---|---|---|---|
| 87T | Transformer differential | Current entering ≠ current leaving, compared through current transformers (CT) on both sides: internal winding faults | Instantaneous trip of both breakers |
| 50 / 51 | Instantaneous / time overcurrent | Short circuit and overload; the time-overcurrent curve is inverse (higher current, faster trip) | Trip, coordinated with downstream devices |
| 50N / 51N | Earth-fault overcurrent | Residual (zero-sequence) current from a phase to ground, measured with a zero-sequence CT or the sum of the three phases | Trip, usually more sensitive than phase overcurrent |
| 63 | Buchholz (gas / oil relay) | Gas from an internal arc or overheating, or a surge of oil, in an oil-filled transformer | Alarm for slow gas, trip for a sudden surge |
| 49 | Thermal overload | Winding or oil temperature, or a thermal image calculated from the current | Alarm, then trip |
| 59 | Overvoltage | Voltage above the set limit (grid surge, tap-changer error) | Alarm or time-delayed trip |
| 27 | Undervoltage | Voltage below the set limit (sag, loss of supply) | Alarm, trip, or transfer to another source |

A multifunction protection relay usually combines several of these in one device.

## Relation to the simulator

`ThreePhaseSupplySimLib` models only the grid voltage and frequency, with three injectable anomalies (settings in `ThreePhaseSupplySettings`, flags in `ThreePhaseSupplyState`). The protection functions that exist today are inside the drive, not on the supply side.

| Item | Status | Where |
|---|---|---|
| Supply voltage and frequency with slew rate | Modelled | `ThreePhaseSupply.Step` |
| Undervoltage / overvoltage event on the supply | Modelled as an injected anomaly (0.50 and 1.25 per unit by default) | `An_UnderVoltage`, `An_OverVoltage` |
| Frequency drift | Modelled as an injected anomaly | `An_FrequencyDrift`, `DriftHz` |
| Drive reaction to supply voltage (27/59 equivalent inside the drive) | Modelled as DC-bus undervoltage / overvoltage trip | `Vfd.DetectTrips` |
| Drive overcurrent, overtemperature | Modelled | `Vfd.DetectTrips` |
| Supply-side 27 / 59 relay with thresholds and delays | Not modelled | |
| 50/51 overcurrent with inverse-time curve | Not modelled | |
| 50N/51N earth fault (the drive has a ground-fault anomaly only) | Not modelled on the supply side | `VfdState.An_GroundFault` |
| 49 thermal image of the transformer | Not modelled | |
| 87T differential, 63 Buchholz | Not modelled (need a transformer model) | |
| Transformer, impedance, voltage drop under load | Not modelled | |
| Harmonic distortion of the supply caused by the drives | Not modelled | |

These gaps are recorded in the roadmap as supply-side protection and transformer modelling. Neither is planned in the current work.

## Safety

These are simplified models for simulation and education. Do not use them for the design of safety or protection systems; protection settings and coordination studies belong to a qualified engineer using the manufacturer's data and the applicable standards.
