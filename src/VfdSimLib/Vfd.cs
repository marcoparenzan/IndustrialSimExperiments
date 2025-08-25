using IndustrialSimLib;

namespace VfdSimLib;

public class Vfd(VfdSettings settings, VfdState state, VfdInputs inputs, VfdOutputs outputs): IDeviceSimulator
{
    ISimState simState;

    public void Step(double dt, ISimState simState)
    {
        this.simState = simState;

        // Update DC bus from supply (with optional VFD-side anomaly overrides for compatibility)
        double baseLL = inputs.SupplyVoltageLL > 0 ? inputs.SupplyVoltageLL : settings.RatedVoltageLL;
        if (state.An_UnderVoltage) baseLL = settings.RatedVoltageLL * 0.5;
        else if (state.An_OverVoltage) baseLL = settings.RatedVoltageLL * 1.25;

        state.BusVoltage = Math.Sqrt(2.0) * baseLL;

        // Faults that trip instantly
        if (state.An_GroundFault) { simState.Trip(VfdFaultCode.GroundFault); }

        if (!simState.Running)
        {
            ThermalStep(dt, conductionLossW: 0.0);
            outputs.OutputFrequency = 0.0;
            outputs.OutputVoltage = 0.0;
            inputs.MotorCurrentFeedback = 0.0;
            return;
        }

        // Slew output frequency toward target
        double df = state.TargetFrequency - outputs.OutputFrequency;
        double slew = (df >= 0 ? settings.Accel : settings.Decel) * dt;
        if (Math.Abs(df) <= Math.Abs(slew)) outputs.OutputFrequency = state.TargetFrequency;
        else outputs.OutputFrequency += Math.Sign(df) * Math.Abs(slew);

        // V/f + boost, capped at rated voltage
        double vf = Math.Max(0.1, outputs.OutputFrequency);
        double voltCmd = settings.RatedVoltageLL * (vf / Math.Max(1.0, settings.RatedFrequency));
        double boost = settings.VoltBoost * settings.RatedVoltageLL;
        outputs.OutputVoltage = Math.Min(settings.RatedVoltageLL, voltCmd + boost);
    }

    public void Step2(double dt, ISimState simState)
    {
        this.simState = simState;

        double lossesW = 0.02 * outputs.OutputVoltage * Math.Max(0.0, inputs.MotorCurrentFeedback);
        ThermalStep(dt, lossesW);
        DetectTrips();
    }

    private void ThermalStep(double dt, double conductionLossW)
    {
        double tamb = settings.AmbientTemp;
        double k = 25.0;
        double dT = (conductionLossW / k - (state.HeatsinkTemp - tamb) / settings.ThermalTimeConstant) * dt;
        state.HeatsinkTemp += dT;
    }

    private void DetectTrips()
    {
        if (state.BusVoltage < settings.UnderVoltPUNomDC * Math.Sqrt(2.0) * settings.RatedVoltageLL)
            simState.Trip(VfdFaultCode.UnderVoltage);
        if (state.BusVoltage > settings.OverVoltPUNomDC * Math.Sqrt(2.0) * settings.RatedVoltageLL)
            simState.Trip(VfdFaultCode.OverVoltage);

        if (inputs.MotorCurrentFeedback > settings.OverCurrentMultiple * settings.MaxCurrent)
            simState.Trip(VfdFaultCode.OverCurrent);

        if (state.HeatsinkTemp > settings.MaxHeatsinkTemp)
            simState.Trip(VfdFaultCode.OverTemp);

        if (state.An_PhaseLoss && simState.Time % 0.5 < 1e-3)
            simState.Trip(VfdFaultCode.PhaseLoss);
    }
}
