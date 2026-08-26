using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotor(InductionMotorSettings settings, InductionMotorState state, InductionMotorInputs inputs, InductionMotorOutputs outputs) : IDeviceSimulator
{
    public void Step(double dt, ISimState simState)
    {
        double f = Math.Max(0.1, Math.Abs(inputs.DriveFrequencyCmd));
        double nSyncRpm = 60.0 * f / settings.PolePairs;
        double slip = nSyncRpm <= 1e-3 ? 1.0 : Math.Max(0.0, (nSyncRpm - state.SpeedRpm) / nSyncRpm);

        double vfPu = (inputs.DriveVoltageCmd / Math.Max(10.0, state.VratedPhPh)) /
                      (f / Math.Max(1.0, settings.RatedFrequency));
        vfPu = Math.Clamp(vfPu, 0.0, 1.2);

        double torquePu = Math.Pow(vfPu, 2.0) * (slip / (slip + settings.SlipNom));
        double electTorque = Math.Clamp(
            torquePu * state.Trated,
            -settings.TorqueMaxPU * state.Trated,
            settings.TorqueMaxPU * state.Trated);

        double loadTorque = settings.ConstLoadTorque +
                            settings.ViscFriction * Math.Abs(state.SpeedRpm) +
                            settings.CoulombFriction * Math.Sign(state.SpeedRpm);
        if (state.An_LoadJam) loadTorque += settings.JamExtraTorque;
        if (state.An_BearingWear) loadTorque += settings.BearingExtraTorque;

        double angularAcceleration = (electTorque - loadTorque) / Math.Max(1e-6, settings.Inertia);
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
    }
}
