namespace VFDSimLib;

public class VfdState
{
    public double TargetFrequency { get; set; }        // Hz
    public double BusVoltage { get; set; }             // Vdc
    public double HeatsinkTemp { get; set; }           // °C
    public double VdcNom { get; set; }

    // Anomaly toggles
    public bool An_UnderVoltage { get; set; }
    public bool An_OverVoltage { get; set; }
    public bool An_PhaseLoss { get; set; }
    public bool An_GroundFault { get; set; }
}
