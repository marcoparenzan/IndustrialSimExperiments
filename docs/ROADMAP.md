# Roadmap

What exists today is described in the [README](../README.md), [docs/vfd/VFD.md](vfd/VFD.md) and [docs/opcua-course](opcua-course/README.md). The course has its own [roadmap](opcua-course/ROADMAP.md). Items below are ideas in rough order of value, not commitments; open questions and known defects are in [OPENPOINTS.md](OPENPOINTS.md).

## VFD and motor model

1. Cooling-fan failure (heatsink time constant) so that the over-temperature trip becomes reachable, and a simple THD indicator tag. Bearing currents stay a documentation topic.
2. Reverse rotation: the motor model has no negative drive frequency yet.
3. Deceleration stall prevention (the drive extends the ramp instead of tripping on bus overvoltage).
4. Start-up transient: at the 0.1 Hz floor with boost voltage the rotor jumps to about 24 rpm in the first tick; a smoother start would remove the small excess bus energy it leaves.
5. Vector control and direct torque control are documented only; a model would need a flux/torque motor model.

## Supply side

6. Supply-side protection with thresholds and delays (27/59, 50/51, 50N/51N, 49), and a transformer model with impedance and voltage drop under load. Differential 87T and Buchholz relay need that model first. See [SUPPLY-PROTECTION.md](vfd/SUPPLY-PROTECTION.md).
7. Anomalies are never switched on by the Conveyor today (`SimState` is built with a no-op toggle). A scenario/schedule API would make the existing anomaly flags useful.

## Protocols and integration

8. A mapping provider for the OPC UA adapter, so a machine can say "these tags form a `VfdType` instance" (units, ranges, a fault-reset method) instead of exposing flat folders. Lesson 19 has the design exercise.
9. Writable tags and commands in ForgeSim (and then commands on the VFD dashboard), with the authorization questions of lesson 13.
10. Modbus and S7 adapters, as already described in EXTENDING.md.
11. Python port of the VFD control-terminal logic, if a machine ever uses it.

## Quality

12. An automated test for the OPC UA adapter (start it, browse, read, check there are no duplicate references).
13. Browser tests in the repository (the course has `check-layout.cjs`, which needs the Playwright npm package that is not installed here).
14. Replace the full Bootstrap distribution of the course web app (about 8 MB with source maps) with the files it really uses.
