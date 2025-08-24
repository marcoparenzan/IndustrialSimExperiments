# VFD ↔ 3‑Phase Induction Motor Wiring (Reference)

Note: This is a generic reference for documentation alongside the simulator. Always follow the specific VFD/motor manufacturer’s manual, local codes, and safety practices.

## Power and Motor Wiring

    Three‑Phase AC Supply                     VFD (Drive)                           Induction Motor
    ┌────────────────────┐          ┌───────────────────────────┐                ┌──────────────────┐
    │  L1  L2  L3  (PE)  ├──────────┤  L1  L2  L3        (PE)   │                │   T1  T2  T3 (PE)│
    │  (No Neutral)      │          │   |   |   |         |     │                │    |   |   |  |  │
    │  Main breaker +    │          │  Input Rectifier/DC Bus   │                │  3Φ Stator + PE  │
    │  fuses/EMO contact │          │                           │                └──────────────────┘
    └─────────┬──────────┘          │     U     V     W         │
              │ PE                  └─────┼─────┼─────┼─────────┘
              │                              │     │     │
              └──────────────────────────────┴─────┴─────┴──────────→ To motor terminals T1/T2/T3
                                                   
    Protective Earth (PE): Supply PE → VFD PE → Motor frame PE (bonded)

- Supply: 3‑phase L1/L2/L3 to VFD input L1/L2/L3. Do not connect Neutral.
- VFD output: U/V/W to motor T1/T2/T3. If rotation is wrong, swap any two phases at the motor.
- Protective Earth: Bond supply PE to VFD PE and to motor frame PE.
- Upstream protection: breaker or fuses per VFD manual; include emergency stop scheme as required.

## Braking Resistor / DC Bus (if supported)

    VFD DC Bus/Brake Terminals:
      P+ (DC+)  ────┐
                    │  Braking Resistor (value/power per VFD datasheet)
      PB/BR    ────┘
      DC- may be provided internally (do not connect unless specified)

- Wire braking resistor between P+ and PB/BR per manual. Mount on suitable heatsink/spacing.

## Control I/O (Example)

    VFD +24V OUT ──────┐
                       ├── DI1 = RUN/STOP (via dry contact to COM)
    VFD COM  ──────────┘

    VFD +10V REF ──────┐
                        \ 10k Potentiometer (speed reference 0–10V)
                        /  Wiper → AI1 (0–10V)
                        \  Other end → COM
    VFD AI1 (0–10V) ───┘
    VFD COM  ──────────┘

    VFD Relay OUT (FAULT) ──→ PLC/Indicator (per contact rating)

- Typical DI: Run/Stop, Fwd/Rev, Reset. Typical AI: AI1 0–10V for speed reference (or 4–20 mA).
- Use the VFD’s internal +10V reference only if allowed by the manual; otherwise use an external source.
- Shield and ground analog signal cables per EMC guidelines.

## Cable and EMC Notes
- Use VFD‑rated shielded motor cable; terminate shield 360° at VFD and motor ends if recommended.
- Keep motor cable short to reduce dv/dt stress; add output filters if needed.
- Separate power and control wiring; cross at 90° when necessary.

## Safety
- Lock‑out/tag‑out before wiring. Verify absence of voltage.
- DC bus remains charged after power off; respect discharge times per the manual.
- Ensure proper overload settings and motor nameplate parameters in the VFD.
