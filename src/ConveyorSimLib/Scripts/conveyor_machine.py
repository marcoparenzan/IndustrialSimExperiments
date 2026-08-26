# conveyor_machine.py — the actual simulation core, in Python, run by PySharp.
#
# A line-by-line port of the real C# conveyor stack (ConveyorSimLib.ConveyorMachine +
# ThreePhaseSupplySimLib.ThreePhaseSupply + VfdSimLib.Vfd + InductionMotorSimLib.InductionMotor +
# PackageSimLib.Package), so PySharpMachineLib.PySharpMachine can be a drop-in IMachineModule
# alternative to ConveyorMachine with identical physics and identical tag names/paths — same
# `Machines:Conveyor` config section, same output, just with the actual motor/VFD/belt math
# living here instead of in four separate C# libraries. No randomness anywhere, so a
# `--machine Conveyor` run and a `--machine ConveyorPySharp` run with the same config should track
# each other exactly, tick for tick.
#
# Anomaly toggles (An_UnderVoltage, An_GroundFault, An_LoadJam, ...) exist in the real C# state
# objects but nothing in ConveyorMachine.Step ever sets them True, so they're carried here as
# constants fixed at False rather than as live inputs — same practical-subset choice PySharp's own
# modules make elsewhere. Trip detection (VFD over/under-voltage, over-current, over-temp) IS real
# and IS reachable: the real Vfd.Step2/DetectTrips runs every tick in the real code too.

import math

_state = None


def init(segment_count, length_m, target_freq_hz, spawn_period_s, package_mass_kg,
         pulley_radius_m, gear_ratio, mech_efficiency, rolling_coeff):
    global _state

    supply_rated_ll = 400.0
    supply_rated_f = 50.0

    motor_rated_power = 7500.0
    motor_rated_speed_rpm = 1440.0
    motor_trated = motor_rated_power / (2 * math.pi * motor_rated_speed_rpm / 60)
    motor_vrated = 400.0

    segment_length = length_m / segment_count
    segments = []
    for i in range(segment_count):
        segments.append({
            "index": i,
            "start_m": i * segment_length,
            "end_m": (i + 1) * segment_length,
            "vfd": {
                "target_freq": target_freq_hz,
                "bus_v": math.sqrt(2.0) * supply_rated_ll,
                "heatsink_temp": 25.0,
                "out_freq": 0.0,
                "out_v": 0.0,
            },
            "motor": {
                "speed_rpm": 0.0,
                "elect_torque": 0.0,
                "trated": motor_trated,
                "vrated": motor_vrated,
                "phase_current": 0.0,
            },
            "base_load_torque": 3.0,   # matches CreateSegment's `new InductionMotorSettings { ConstLoadTorque = 3 }`
            "const_load_torque": 3.0,
            "last_belt_speed": 0.0,
        })

    _state = {
        "options": {
            "length_m": length_m,
            "segment_count": segment_count,
            "pulley_radius_m": pulley_radius_m,
            "gear_ratio": gear_ratio,
            "mech_efficiency": mech_efficiency,
            "rolling_coeff": rolling_coeff,
            "package_mass_kg": package_mass_kg,
            "spawn_period_s": spawn_period_s,
            # ThreePhaseSupplySettings defaults
            "supply_nominal_ll": supply_rated_ll,
            "supply_nominal_f": supply_rated_f,
            "supply_v_slew": 500.0,
            "supply_f_slew": 10.0,
            # VfdSettings defaults
            "vfd_rated_ll": 400.0,
            "vfd_rated_f": 50.0,
            "vfd_max_current": 30.0,
            "vfd_accel": 10.0,
            "vfd_decel": 10.0,
            "vfd_volt_boost": 0.07,
            "vfd_thermal_tc": 40.0,
            "vfd_max_heatsink_temp": 85.0,
            "vfd_ambient_temp": 25.0,
            "vfd_overcurrent_mult": 1.6,
            "vfd_undervolt_pu_dc": 0.55,
            "vfd_overvolt_pu_dc": 1.20,
            # InductionMotorSettings defaults
            "motor_pole_pairs": 2,
            "motor_rated_f": 50.0,
            "motor_inertia": 0.20,
            "motor_visc_friction": 0.003,
            "motor_coulomb_friction": 1.0,
            "motor_slip_nom": 0.03,
            "motor_torque_max_pu": 2.2,
            "motor_inom": 15.0,
        },
        "time": 0.0,
        "running": True,
        "active_trip": None,
        "event_log": [],
        "next_spawn": 0.0,
        "packages": [],   # list of [position_m, mass_kg]
        "supply": {
            "target_v": supply_rated_ll,
            "target_f": supply_rated_f,
            "out_v": supply_rated_ll,
            "out_f": supply_rated_f,
        },
        "segments": segments,
    }


def _sign(x):
    if x > 0:
        return 1
    if x < 0:
        return -1
    return 0


def _slew(current, target, max_step):
    diff = target - current
    if abs(diff) <= max_step:
        return target
    return current + _sign(diff) * abs(max_step)


def _trip(s, code):
    # SimState.Trip: latches the FIRST trip only; every later call is a no-op.
    if s["active_trip"] is not None:
        return
    s["active_trip"] = code
    s["running"] = False
    s["event_log"].append(f"[{s['time']:6.2f}s] TRIP: {code}")


def _step_supply(s, dt):
    opt = s["options"]
    supply = s["supply"]
    v_target = supply["target_v"] if supply["target_v"] > 0 else opt["supply_nominal_ll"]
    f_target = supply["target_f"] if supply["target_f"] > 0 else opt["supply_nominal_f"]
    supply["out_v"] = _slew(supply["out_v"], v_target, opt["supply_v_slew"] * dt)
    supply["out_f"] = _slew(supply["out_f"], f_target, opt["supply_f_slew"] * dt)


def _vfd_thermal_step(vfd, opt, dt, conduction_loss_w):
    tamb = opt["vfd_ambient_temp"]
    k = 25.0
    dT = (conduction_loss_w / k - (vfd["heatsink_temp"] - tamb) / opt["vfd_thermal_tc"]) * dt
    vfd["heatsink_temp"] += dT


def _vfd_detect_trips(s, seg):
    opt = s["options"]
    vfd = seg["vfd"]
    if vfd["bus_v"] < opt["vfd_undervolt_pu_dc"] * math.sqrt(2.0) * opt["vfd_rated_ll"]:
        _trip(s, "UnderVoltage")
    if vfd["bus_v"] > opt["vfd_overvolt_pu_dc"] * math.sqrt(2.0) * opt["vfd_rated_ll"]:
        _trip(s, "OverVoltage")
    if vfd["_motor_current_feedback"] > opt["vfd_overcurrent_mult"] * opt["vfd_max_current"]:
        _trip(s, "OverCurrent")
    if vfd["heatsink_temp"] > opt["vfd_max_heatsink_temp"]:
        _trip(s, "OverTemp")
    # An_PhaseLoss is always False in this v1 scope, so the PhaseLoss trip path never fires.


def _step_segment(s, seg, dt, supply_v):
    opt = s["options"]
    vfd = seg["vfd"]
    motor = seg["motor"]

    # --- Vfd.Step ---
    base_ll = supply_v if supply_v > 0 else opt["vfd_rated_ll"]
    vfd["bus_v"] = math.sqrt(2.0) * base_ll
    # An_GroundFault is always False in this v1 scope.

    if not s["running"]:
        _vfd_thermal_step(vfd, opt, dt, 0.0)
        vfd["out_freq"] = 0.0
        vfd["out_v"] = 0.0
        vfd["_motor_current_feedback"] = 0.0  # inputs.MotorCurrentFeedback.Reset()
    else:
        df = vfd["target_freq"] - vfd["out_freq"]
        max_slew = (opt["vfd_accel"] if df >= 0 else opt["vfd_decel"]) * dt
        if abs(df) <= abs(max_slew):
            vfd["out_freq"] = vfd["target_freq"]
        else:
            vfd["out_freq"] += _sign(df) * abs(max_slew)

        vf = max(0.1, vfd["out_freq"])
        volt_cmd = opt["vfd_rated_ll"] * (vf / max(1.0, opt["vfd_rated_f"]))
        boost = opt["vfd_volt_boost"] * opt["vfd_rated_ll"]
        vfd["out_v"] = min(opt["vfd_rated_ll"], volt_cmd + boost)

    # --- motor inputs from VFD outputs (ConveyorMachine.Step, unconditional every tick) ---
    drive_freq_cmd = vfd["out_freq"]
    drive_voltage_cmd = vfd["out_v"]

    # --- package-induced load torque, computed BEFORE Motor.Step using last tick's speed ---
    belt_speed = _belt_speed(opt, motor["speed_rpm"])
    acceleration = (belt_speed - seg["last_belt_speed"]) / dt
    seg["last_belt_speed"] = belt_speed
    mass = sum(m for (pos, m) in s["packages"] if seg["start_m"] <= pos < seg["end_m"])
    force = mass * 9.81 * opt["rolling_coeff"] + mass * acceleration
    package_torque = force * opt["pulley_radius_m"] / (opt["gear_ratio"] * opt["mech_efficiency"])
    seg["const_load_torque"] = seg["base_load_torque"] + package_torque

    # --- InductionMotor.Step ---
    f = max(0.1, abs(drive_freq_cmd))
    n_sync_rpm = 60.0 * f / opt["motor_pole_pairs"]
    if n_sync_rpm <= 1e-3:
        slip = 1.0
    else:
        slip = max(0.0, (n_sync_rpm - motor["speed_rpm"]) / n_sync_rpm)

    vf_pu = (drive_voltage_cmd / max(10.0, motor["vrated"])) / (f / max(1.0, opt["motor_rated_f"]))
    vf_pu = min(1.2, max(0.0, vf_pu))

    torque_pu = (vf_pu ** 2.0) * (slip / (slip + opt["motor_slip_nom"]))
    elect_torque = torque_pu * motor["trated"]
    elect_torque = min(opt["motor_torque_max_pu"] * motor["trated"],
                        max(-opt["motor_torque_max_pu"] * motor["trated"], elect_torque))

    load_torque = (seg["const_load_torque"] +
                   opt["motor_visc_friction"] * abs(motor["speed_rpm"]) +
                   opt["motor_coulomb_friction"] * _sign(motor["speed_rpm"]))
    # An_LoadJam / An_BearingWear are always False in this v1 scope.

    angular_acceleration = (elect_torque - load_torque) / max(1e-6, opt["motor_inertia"])
    motor["speed_rpm"] += angular_acceleration * 60.0 / (2.0 * math.pi) * dt
    if motor["speed_rpm"] < 0 and drive_freq_cmd >= 0:
        motor["speed_rpm"] = 0.0

    current = (abs(elect_torque) / max(1e-3, motor["trated"])) * (1.0 / max(0.2, vf_pu)) * opt["motor_inom"]
    # An_PhaseLoss is always False in this v1 scope.

    motor["elect_torque"] = elect_torque
    motor["phase_current"] = current

    # --- Vfd.Step2 (thermal + trip detection), driven by this tick's fresh motor current) ---
    vfd["_motor_current_feedback"] = motor["phase_current"]
    losses_w = 0.02 * vfd["out_v"] * max(0.0, vfd["_motor_current_feedback"])
    _vfd_thermal_step(vfd, opt, dt, losses_w)
    _vfd_detect_trips(s, seg)


def _belt_speed(opt, rpm):
    return 2 * math.pi * rpm / 60 * opt["pulley_radius_m"] / opt["gear_ratio"]


def _spawn_packages(s):
    opt = s["options"]
    while s["time"] >= s["next_spawn"]:
        s["packages"].append([0.0, opt["package_mass_kg"]])
        s["next_spawn"] += opt["spawn_period_s"]


def _move_packages(s, dt):
    opt = s["options"]
    segment_length = opt["length_m"] / opt["segment_count"]
    segments = s["segments"]
    packages = s["packages"]
    kept = []
    for pos, mass in packages:
        segment_index = int(pos / segment_length)
        segment_index = max(0, min(len(segments) - 1, segment_index))
        new_pos = pos + _belt_speed(opt, segments[segment_index]["motor"]["speed_rpm"]) * dt
        if new_pos < opt["length_m"]:
            kept.append([new_pos, mass])
    s["packages"] = kept


def snapshot():
    """Initial tag values before the first step() call — used to seed the C# side's cache so
    IMachineModule.Tags has real values from construction time, not just after the first Step()."""
    return _snapshot(_state)


def step(dt):
    global _state
    s = _state

    s["time"] += dt
    _spawn_packages(s)
    _step_supply(s, dt)

    supply_v = s["supply"]["out_v"]
    for seg in s["segments"]:
        _step_segment(s, seg, dt, supply_v)

    _move_packages(s, dt)

    return _snapshot(s)


def _snapshot(s):
    tags = {
        "__time__": s["time"],
        "__running__": s["running"],
        "__active_trip__": s["active_trip"] if s["active_trip"] is not None else "",
        "Supply.LineLineVoltage": s["supply"]["out_v"],
        "Supply.Frequency": s["supply"]["out_f"],
        "Packages.Count": len(s["packages"]),
        "Packages.Positions": [pos for (pos, _mass) in s["packages"]],
        "Packages.Masses": [mass for (_pos, mass) in s["packages"]],
    }
    for seg in s["segments"]:
        root = f"Segments.Segment{seg['index']}"
        tags[f"{root}.Vfd.OutputFrequency"] = seg["vfd"]["out_freq"]
        tags[f"{root}.Vfd.OutputVoltage"] = seg["vfd"]["out_v"]
        tags[f"{root}.Vfd.BusVoltage"] = seg["vfd"]["bus_v"]
        tags[f"{root}.Vfd.HeatsinkTemp"] = seg["vfd"]["heatsink_temp"]
        tags[f"{root}.Motor.SpeedRpm"] = seg["motor"]["speed_rpm"]
        tags[f"{root}.Motor.PhaseCurrent"] = seg["motor"]["phase_current"]
    return tags
