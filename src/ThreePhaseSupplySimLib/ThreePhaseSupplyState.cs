using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyState
{
    // Targets (operator/grid setpoints)
    public DoubleBindable TargetVoltageLL { get; set; } // V
    public DoubleBindable TargetFrequency { get; set; } // Hz

    // Anomaly toggles
    public BoolBindable An_UnderVoltage { get; set; }
    public BoolBindable An_OverVoltage { get; set; }
    public BoolBindable An_FrequencyDrift { get; set; }
}