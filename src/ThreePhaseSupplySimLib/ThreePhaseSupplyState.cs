namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyState
{
    // Targets (operator/grid setpoints)
    public double TargetVoltageLL { get; set; } // V
    public double TargetFrequency { get; set; } // Hz

    // Anomaly toggles
    public bool An_UnderVoltage { get; set; }
    public bool An_OverVoltage { get; set; }
    public bool An_FrequencyDrift { get; set; }
}