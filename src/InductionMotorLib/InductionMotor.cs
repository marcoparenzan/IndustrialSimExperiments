using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotor(InductionMotorSettings settings, InductionMotorState state, InductionMotorInputs inputs, InductionMotorOutputs outputs) : IDeviceSimulator
{
    ISimState simState;

    public void Step(double dt, ISimState simState)
    {
        this.simState = simState;

        // Handle zero/near-zero frequency (hold torque limited)
        double f = Math.Max(0.1, Math.Abs(inputs.DriveFrequencyCmd));
        double nSyncRpm = 60.0 * f / settings.PolePairs; // synchronous speed

        // Slip (simple definition)
        double slip = nSyncRpm <= 1e-3 ? 1.0 : Math.Max(0.0, (nSyncRpm - state.SpeedRpm) / nSyncRpm);

        // V/f ratio + low-speed boost influence torque
        double vf_pu = (inputs.DriveVoltageCmd / Math.Max(10.0, state.VratedPhPh)) / (f / Math.Max(1.0, settings.RatedFrequency));
        vf_pu = Math.Clamp(vf_pu, 0.0, 1.2);

        // Simplified torque model: T ~ Trated * (vf)^2 * (slip / (slip + s0)) * torque_limit
        double torquePU = Math.Pow(vf_pu, 2.0) * (slip / (slip + settings.SlipNom));
        double T_e = torquePU * state.Trated;
        T_e = Math.Clamp(T_e, -settings.TorqueMaxPU * state.Trated, settings.TorqueMaxPU * state.Trated);

        // Load torque: constant + viscous + Coulomb + anomalies
        double T_load = settings.ConstLoadTorque + settings.ViscFriction * Math.Abs(state.SpeedRpm) + settings.CoulombFriction * Math.Sign(state.SpeedRpm);
        if (state.An_LoadJam) T_load += settings.JamExtraTorque;
        if (state.An_BearingWear) T_load += settings.BearingExtraTorque;

        // Net acceleration: J * dω = (T_e - T_load)
        double domega = (T_e - T_load) / Math.Max(1e-6, settings.Inertia); // rad/s^2
        double domega_rpm = domega * 60.0 / (2.0 * Math.PI);

        state.SpeedRpm.Add(domega_rpm * dt);
        // Prevent running backwards in this simple model
        if (state.SpeedRpm < 0 && inputs.DriveFrequencyCmd >= 0) state.SpeedRpm.Reset();

        // Current proxy: proportional to torque demand divided by (V/f) margin
        double vf_margin = Math.Max(0.2, vf_pu);
        double I = Math.Abs(T_e) / Math.Max(1e-3, state.Trated) * (1.0 / vf_margin) * settings.Inom;

        // Phase loss anomaly => higher current for same torque, lower torque capability
        if (state.An_PhaseLoss == true)
        {
            I *= 1.7;         // current inflates
            T_e *= 0.6;       // torque drops
        }

        state.ElectTorque.Set(T_e);
        outputs.PhaseCurrent.Set(I); // wiring output to VFD
    }
}
