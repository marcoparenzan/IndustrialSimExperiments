using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotor(InductionMotorSettings settings, InductionMotorState state, InductionMotorInputs inputs, InductionMotorOutputs outputs) : IDeviceSimulator
{
    public void Step(double dt, ISimState simState)
    {
        double f = Math.Max(0.1, Math.Abs(inputs.DriveFrequencyCmd));
        double nSyncRpm = 60.0 * f / settings.PolePairs;
        // Negative slip = the rotor runs faster than the field (generating / regenerating).
        double slip = nSyncRpm <= 1e-3 ? 1.0 : (nSyncRpm - state.SpeedRpm) / nSyncRpm;
        double slipMagnitude = Math.Abs(slip);

        double vfPu = (inputs.DriveVoltageCmd / Math.Max(10.0, state.VratedPhPh)) /
                      (f / Math.Max(1.0, settings.RatedFrequency));
        vfPu = Math.Clamp(vfPu, 0.0, 1.2);

        double torquePu = Math.Pow(vfPu, 2.0) * Math.Sign(slip) * (slipMagnitude / (slipMagnitude + settings.SlipNom));
        double maxTorque = settings.TorqueMaxPU * state.Trated;
        double electTorque = Math.Clamp(torquePu * state.Trated, -maxTorque, maxTorque);

        double loadTorque = settings.ConstLoadTorque +
                            settings.ViscFriction * Math.Abs(state.SpeedRpm) +
                            settings.CoulombFriction * Math.Sign(state.SpeedRpm);
        if (state.An_LoadJam) loadTorque += settings.JamExtraTorque;
        if (state.An_BearingWear) loadTorque += settings.BearingExtraTorque;

        // Semi-implicit speed update: the slip law is very stiff at low drive frequency (a few rpm of synchronous speed),
        // and an explicit step makes the rotor overshoot the field and oscillate. Linearising torque and viscous friction
        // in speed keeps the update stable, so the rotor lags the field while accelerating and only leads it when braking.
        const double RadPerSecPerRpm = 2.0 * Math.PI / 60.0;
        double stiffness = settings.ViscFriction / RadPerSecPerRpm;                      // Nm per rad/s
        if (Math.Abs(torquePu * state.Trated) < maxTorque)
            stiffness += state.Trated * vfPu * vfPu / nSyncRpm * settings.SlipNom
                         / Math.Pow(slipMagnitude + settings.SlipNom, 2) / RadPerSecPerRpm;
        double angularAcceleration = (electTorque - loadTorque) / Math.Max(1e-6, settings.Inertia + dt * stiffness);
        state.SpeedRpm.Add(angularAcceleration * 60.0 / (2.0 * Math.PI) * dt);
        if (state.SpeedRpm < 0 && inputs.DriveFrequencyCmd >= 0) state.SpeedRpm.Reset();

        double current = Math.Abs(electTorque) / Math.Max(1e-3, state.Trated) *
                         (1.0 / Math.Max(0.2, vfPu)) * settings.Inom;
        if (state.An_PhaseLoss)
        {
            current *= 1.7;
            electTorque *= 0.6;
        }

        state.ElectTorque.Set(electTorque);
        outputs.PhaseCurrent.Set(current);
        outputs.ShaftPowerW.Set(electTorque * state.SpeedRpm * 2.0 * Math.PI / 60.0);   // negative = regenerating
    }
}
