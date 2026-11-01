# Open points

Things that are decided but not obvious, known defects, and things that were **not verified**. The course's own list is in [opcua-course/OPEN-POINTS.md](opcua-course/OPEN-POINTS.md).

## Decisions to confirm

| Topic | Current state | Question |
|---|---|---|
| Third-party infographics | Eight PNG files in `docs/images/vfd/` are git-ignored; `SOURCES.md` lists them. The repository is public. | Is local-only the right call, or should they be removed from the repository folder too? |
| `LICENSE.txt` | Copyright year set to "2026" (it was "2025-2026"). | Done. |
| `PySharp.Interpreter` | Preview package `2.6.1-net11-preview.2` from the local feed; the old `2.0.0` is gone. | Publish a stable net11 package, or keep the local feed documented. |
| The original `D:\Dev\2026\IoTHub\OpcUaExperiments` folder | Untouched; its content now lives in `src/OpcUaCourse` and `docs/opcua-course`. | Move it to `BuildArtifacts\trash` (not done: only you decide). |
| `.ralfai/` folder in the repository root | Untracked; created by another tool, not by this work. | Add to `.gitignore` or delete. |

## Model behaviour

| Topic | State | Next step |
|---|---|---|
| Motor integration | Speed is integrated semi-implicitly (needed so that regeneration is not triggered by numerical overshoot). This changed the transient numbers of every simulation compared with the previous explicit step. | Re-baseline any stored expectations. |
| Start-up artifact | The rotor jumps to about 24 rpm in the first tick (field 3 rpm at the 0.1 Hz floor); the drive books a small regeneration (bus about 580 V at 0.5 s, decaying with a 0.1 s time constant). | Smoother start model. |
| `BusDischargeTimeConstant` | 0.1 s: with 5 s even a gentle stop tripped OverVoltage. It is a tuning value, not a measured one. | Calibrate against a datasheet if needed. |
| VFD control terminals | Implemented and tested in C# only; the Python conveyor never uses them. | Port if a machine uses terminal control. |
| Reverse, stall prevention, fan failure, THD | Not implemented. | See the roadmap. |
| Anomaly flags | Exist on every device but nothing sets them in the Conveyor. | Scenario API. |
| `Machine.Running` / `Machine.ActiveTrip` tags | Only the Python conveyor exposes them; the VFD dashboard label "Active trip" stays at "—" with the C# conveyor. | Add to the C# machine. |

## Defects fixed on the way

- `OpcUaServerLib` listed every folder and variable twice in Browse (explicit `AddReference` plus `AddChild`). Fixed in `NodeExtension.AddFolder` and `OpcUaProtocolAdapter.CreateVariable`; checked by hand with the `client forgesim` lab (10 entries instead of 20). `NodeExtension.AddVar` and `AddArrayVar` keep the same pattern but are not used by the adapter; not changed.

## Build

Seven warnings (five distinct) remain and were left alone: `NU1510` (`Microsoft.Extensions.Logging.Abstractions` 10.0.0 "will not be pruned" on net11), `CS0618` three times (`ApplicationInstance.CheckApplicationInstanceCertificate` is obsolete in the OPC UA SDK; in `OpcUaServerLib`, `ForgeDataGatewayCore` and the course), `CS9113` (unused parameter in `ThreePhaseSupply`).

## Not verified

- Interactive use of the course web app in a browser (clicking Start/Stop/Write/Load recipe) and the course's layout runner `check-layout.cjs` (needs the Playwright npm package; updated for three dashboards but never run). Pages were opened headlessly and inspected as screenshots; some captures failed intermittently.
- The VFD dashboard with the **C# conveyor** (only the Python conveyor was used), at mobile widths, and the final layout in the *live* state (inspected offline, same positions).
- ForgeSim with `OpcUa` and `Mqtt` together while a client is connected; the Gateway UI; the console dashboard (needs an interactive terminal).
- An independent review of the English translation of the course (the translator and the reviewer were the same agent); Mermaid diagrams were not rendered.
- A first full run of the VFD dashboard showed 16 failed ForgeSim connections although ForgeSim seemed to be running; the next run connected immediately. Cause not proven.
