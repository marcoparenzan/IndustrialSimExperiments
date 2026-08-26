using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupply(ThreePhaseSupplySettings settings, ThreePhaseSupplyState state, ThreePhaseSupplyInputs _, ThreePhaseSupplyOutputs outputs) : IDeviceSimulator
{
    public void Step(double dt, ISimState simState)
    {
        double vTarget = state.TargetVoltageLL > 0 ? state.TargetVoltageLL : settings.NominalVoltageLL;
        double fTarget = state.TargetFrequency > 0 ? state.TargetFrequency : settings.NominalFrequency;

        if (state.An_UnderVoltage) vTarget = settings.NominalVoltageLL * settings.UnderVoltPU;
        else if (state.An_OverVoltage) vTarget = settings.NominalVoltageLL * settings.OverVoltPU;
        if (state.An_FrequencyDrift) fTarget = settings.NominalFrequency + settings.DriftHz;

        outputs.LineLineVoltage.Set(Slew(outputs.LineLineVoltage, vTarget, settings.VoltageSlewRate * dt));
        outputs.Frequency.Set(Slew(outputs.Frequency, fTarget, settings.FrequencySlewRate * dt));
    }

    private static double Slew(double current, double target, double maxStep)
    {
        double difference = target - current;
        return Math.Abs(difference) <= maxStep
            ? target
            : current + Math.Sign(difference) * maxStep;
    }
}
