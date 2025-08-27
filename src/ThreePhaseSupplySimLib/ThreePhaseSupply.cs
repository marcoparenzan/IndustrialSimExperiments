using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupply(ThreePhaseSupplySettings settings, ThreePhaseSupplyState state, ThreePhaseSupplyInputs inputs, ThreePhaseSupplyOutputs outputs) : IDeviceSimulator
{
    ISimState simState = default!;

    public void Step(double dt, ISimState simState)
    {
        this.simState = simState;

        // Base targets fall back to nominal if unset
        double vTarget = state.TargetVoltageLL > 0 ? state.TargetVoltageLL : settings.NominalVoltageLL;
        double fTarget = state.TargetFrequency > 0 ? state.TargetFrequency : settings.NominalFrequency;

        // Apply anomalies (grid-side)
        if (state.An_UnderVoltage) vTarget = settings.NominalVoltageLL * settings.UnderVoltPU;
        else if (state.An_OverVoltage) vTarget = settings.NominalVoltageLL * settings.OverVoltPU;

        if (state.An_FrequencyDrift) fTarget = settings.NominalFrequency + settings.DriftHz;

        // Slew toward target
        outputs.LineLineVoltage.Set(Slew(outputs.LineLineVoltage, vTarget, settings.VoltageSlewRate * dt));
        outputs.Frequency.Set(Slew(outputs.Frequency, fTarget, settings.FrequencySlewRate * dt));
    }

    private static double Slew(double current, double target, double maxStep)
    {
        double df = target - current;
        if (Math.Abs(df) <= maxStep) return target;
        return current + Math.Sign(df) * maxStep;
    }
}