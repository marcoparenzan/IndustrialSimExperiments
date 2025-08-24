using IndustrialSimLib;

namespace VFDSimLib;

public class Vfd(VfdSettings settings, VfdState state, VfdInputs inputs, VfdOutputs outputs): IDeviceSimulator
{
    ISimState simState;

    public void Step(double dt, ISimState simState)
    {
        this.simState = simState;

        // Update DC bus based on anomalies (simple scaling)
        if (state.An_UnderVoltage) state.BusVoltage = state.VdcNom * 0.5; // sag
        else if (state.An_OverVoltage) state.BusVoltage = state.VdcNom * 1.25; // surge
        else state.BusVoltage = state.VdcNom;

        // Faults that trip instantly
        if (state.An_GroundFault) { simState.Trip(VfdFaultCode.GroundFault); }

        // If tripped, outputs are disabled (coast) but we keep thermal cooling
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

        // Thermal model (very rough): inverter losses ~ 2% of V*I plus cooling to ambient
        double lossesW = 0.02 * outputs.OutputVoltage * Math.Max(0.0, inputs.MotorCurrentFeedback);
        ThermalStep(dt, lossesW);

        // Fault detection
        DetectTrips();
    }

    private void ThermalStep(double dt, double conductionLossW)
    {
        // Simplified RC: dT/dt = (P/k - (T - Tamb)/tau)
        double tamb = settings.AmbientTemp;
        double k = 25.0; // W/°C (effective cooling constant for this toy model)
        double dT = (conductionLossW / k - (state.HeatsinkTemp - tamb) / settings.ThermalTimeConstant) * dt;
        state.HeatsinkTemp += dT;
    }

    private void DetectTrips()
    {
        // DC bus
        if (state.BusVoltage < settings.UnderVoltPUNomDC * Math.Sqrt(2.0) * settings.RatedVoltageLL)
            simState.Trip(VfdFaultCode.UnderVoltage);
        if (state.BusVoltage > settings.OverVoltPUNomDC * Math.Sqrt(2.0) * settings.RatedVoltageLL)
            simState.Trip(VfdFaultCode.OverVoltage);

        // Over-current (use input feedback)
        if (inputs.MotorCurrentFeedback > settings.OverCurrentMultiple * settings.MaxCurrent)
            simState.Trip(VfdFaultCode.OverCurrent);

        // Over-temperature
        if (state.HeatsinkTemp > settings.MaxHeatsinkTemp)
            simState.Trip(VfdFaultCode.OverTemp);

        // Phase loss is a latched trip in many drives; we do it if it persists
        if (state.An_PhaseLoss && simState.Time % 0.5 < 1e-3)
            simState.Trip(VfdFaultCode.PhaseLoss);
    }
}
