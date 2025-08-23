namespace VFDSimLib;

public class VfdState
{
    public double OutputFrequency { get; set; }        // Hz
    public double OutputVoltage { get; set; }          // V (fundamental rms, phase-phase approx)
    public double TargetFrequency { get; set; }        // Hz
    public double BusVoltage { get; set; }             // Vdc
    public double MotorCurrent { get; set; }           // A (rms, estimate)
    public double HeatsinkTemp { get; set; }           // °C
    public double VdcNom { get; set; }

    // Anomaly toggles
    public bool An_UnderVoltage { get; set; }
    public bool An_OverVoltage { get; set; }
    public bool An_PhaseLoss { get; set; }
    public bool An_GroundFault { get; set; }
}
