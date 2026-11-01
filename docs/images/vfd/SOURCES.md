# VFD reference infographics

Reference material collected on 2026-11-01. The PNG files are **third-party works** (watermarks of the
original authors are visible). They are not covered by this repository's MIT licence, so they stay local
(`docs/images/vfd/*.png` is in `.gitignore`). A backup copy is kept in
`D:\Dev\BuildArtifacts\IndustrialSimExperiments\source-images`.

Documentation in this repository explains the concepts in its own words and draws its own diagrams
(Mermaid or SVG). It does not embed these images.

| File | Author / watermark | Content |
|---|---|---|
| `01-delta-ms300-wiring.png` | Electrical with Upendra | Delta MS300 wiring: R/S/T supply, U/V/W motor, DI1–DI6 + DCM digital inputs (FWD, REV, multi-step, reset, fault), AVI 0–10 V / 4–20 mA speed reference with +10V/ACM, relay RA/RB/RC, AFM analog output, B1/B2 braking resistor, terminal table |
| `02-vfd-working-benefits-issues.png` | unknown | Rectifier → DC link → inverter → induction motor; benefits (energy saving, precise control, soft start, longer equipment life); common issues (overheating, over/undervoltage faults, harmonics, bearing currents) |
| `03-vfd-types-comparison.png` | unknown | VSI, CSI, PWM, regenerative AFE drives: DC link, output, control, advantages, applications, comparison table |
| `04-vfd-four-steps.png` | unknown | Four steps: rectifier (full-wave rectified DC), DC bus filter, IGBT inverter with PWM, control logic and HMI keypad |
| `05-vfd-power-stages-control-strategies.png` | unknown | Power stages, `Vdc ≈ 1.35 × Vac (line-to-line)`, V/f scalar control, vector (FOC) control, DTC, `Ns = 120 f / P` |
| `06-vfd-working-principle.png` | Tech Leaning | Five steps: AC input, diode bridge, DC link, PWM IGBT inverter, speed control keeping V/f constant; benefits and applications |
| `07-transformer-protection.png` | Power Solutions Inc. | Transformer protection: differential 87T, overcurrent 50/51, Buchholz relay, earth fault, thermal overload 49, overvoltage 59 / undervoltage 27 |
| `08-what-is-a-vfd.png` | unknown | Introductory: main parts, three steps, benefits (energy saving, soft start/stop, precise speed control) |

## Known inaccuracies in the sources

- `05`: the legend says "Ns = Synchronous speed (PPM)"; the unit is rpm.
- `05`: `1.35 × V_LL` is the average output of a six-pulse diode bridge. With a DC-link capacitor at light load,
  the bus rises towards the peak `√2 × V_LL ≈ 1.414 × V_LL`.
- `03`: "PWM VFD" is listed as a fourth type. PWM is a modulation technique that almost every modern VSI uses.
  It is not a separate drive topology.
- `07`: two panels are both labelled "F" and both titled "Voltage protection".
