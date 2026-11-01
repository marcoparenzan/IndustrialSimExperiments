using IndustrialSimLib;

namespace VfdSimLib;

public class Vfd(VfdSettings settings, VfdState state, VfdInputs inputs, VfdOutputs outputs) : IDeviceSimulator
{
    public void Step(double dt, ISimState simState)
    {
        double baseLL = inputs.SupplyVoltageLL > 0 ? inputs.SupplyVoltageLL : settings.RatedVoltageLL;
        if (state.An_UnderVoltage) baseLL = settings.RatedVoltageLL * 0.5;
        else if (state.An_OverVoltage) baseLL = settings.RatedVoltageLL * 1.25;

        // Bus voltage sags from the no-load peak (sqrt(2) * V_LL) towards the six-pulse average as the motor loads the DC link.
        double loadPu = Math.Clamp(inputs.MotorCurrentFeedback / Math.Max(1e-6, settings.MaxCurrent), 0.0, 1.0);
        state.BusVoltage.Set(baseLL * (Math.Sqrt(2.0) - (Math.Sqrt(2.0) - settings.BusFullLoadFactor) * loadPu));

        if (state.An_GroundFault) simState.Trip(VfdFaultCode.GroundFault);

        if (!simState.Running)
        {
            ThermalStep(dt, conductionLossW: 0.0);
            outputs.OutputFrequency.Reset();
            outputs.OutputVoltage.Reset();
            inputs.MotorCurrentFeedback.Reset();
            return;
        }

        double df = state.TargetFrequency - outputs.OutputFrequency;
        double slew = (df >= 0 ? settings.Accel : settings.Decel) * dt;
        if (Math.Abs(df) <= Math.Abs(slew)) outputs.OutputFrequency.Set(state.TargetFrequency);
        else outputs.OutputFrequency.Add(Math.Sign(df) * Math.Abs(slew));

        double vf = Math.Max(0.1, outputs.OutputFrequency);
        double voltCmd = settings.RatedVoltageLL * (vf / Math.Max(1.0, settings.RatedFrequency));
        double boost = settings.VoltBoost * settings.RatedVoltageLL;
        outputs.OutputVoltage.Set(Math.Min(settings.RatedVoltageLL, voltCmd + boost));
    }

    public void Step2(double dt, ISimState simState)
    {
        double lossesW = 0.02 * outputs.OutputVoltage * Math.Max(0.0, inputs.MotorCurrentFeedback);
        ThermalStep(dt, lossesW);
        DcLinkStep(dt);
        DetectTrips(simState);
    }

    // Regenerated power charges the DC link above the level Step() computed from the load; the brake chopper,
    // when a resistor is fitted, burns it off. Sub-stepped because the chopper reacts far faster than a sim tick.
    private void DcLinkStep(double dt)
    {
        const double maxSubStep = 0.001;
        int subSteps = Math.Max(1, (int)Math.Ceiling(dt / maxSubStep));
        double h = dt / subSteps;
        double busStatic = state.BusVoltage;
        double vdcNom = state.VdcNom > 0 ? state.VdcNom : Math.Sqrt(2.0) * settings.RatedVoltageLL;
        double regenW = Math.Max(0.0, -inputs.MotorPowerFeedback);
        double energy = state.BusExcessEnergyJ;
        double brakeW = 0.0;
        for (int i = 0; i < subSteps; i++)
        {
            double vdc = Math.Sqrt(busStatic * busStatic + 2.0 * energy / Math.Max(1e-9, settings.DcLinkCapacitanceF));
            if (settings.BrakingResistorOhms > 0)
            {
                if (vdc > settings.BrakeChopperOnPU * vdcNom) state.BrakeChopperActive.Set(true);
                else if (vdc < settings.BrakeChopperOffPU * vdcNom) state.BrakeChopperActive.Set(false);
            }
            else state.BrakeChopperActive.Set(false);
            brakeW = state.BrakeChopperActive ? vdc * vdc / settings.BrakingResistorOhms : 0.0;
            energy = Math.Max(0.0, energy + (regenW - brakeW - energy / Math.Max(1e-6, settings.BusDischargeTimeConstant)) * h);
        }
        state.BusExcessEnergyJ.Set(energy);
        state.BrakingPowerW.Set(brakeW);
        state.BusVoltage.Set(Math.Sqrt(busStatic * busStatic + 2.0 * energy / Math.Max(1e-9, settings.DcLinkCapacitanceF)));
    }

    private void ThermalStep(double dt, double conductionLossW)
    {
        double tamb = settings.AmbientTemp;
        double k = 25.0;
        double dT = (conductionLossW / k - (state.HeatsinkTemp - tamb) / settings.ThermalTimeConstant) * dt;
        state.HeatsinkTemp.Add(dT);
    }

    private void DetectTrips(ISimState simState)
    {
        double vdcNom = state.VdcNom > 0 ? state.VdcNom : Math.Sqrt(2.0) * settings.RatedVoltageLL;
        if (state.BusVoltage < settings.UnderVoltPUNomDC * vdcNom)
            simState.Trip(VfdFaultCode.UnderVoltage);
        if (state.BusVoltage > settings.OverVoltPUNomDC * vdcNom)
            simState.Trip(VfdFaultCode.OverVoltage);
        if (inputs.MotorCurrentFeedback > settings.OverCurrentMultiple * settings.MaxCurrent)
            simState.Trip(VfdFaultCode.OverCurrent);
        if (state.HeatsinkTemp > settings.MaxHeatsinkTemp)
            simState.Trip(VfdFaultCode.OverTemp);
        if (state.An_PhaseLoss && simState.Time % 0.5 < 1e-3)
            simState.Trip(VfdFaultCode.PhaseLoss);
    }
}
