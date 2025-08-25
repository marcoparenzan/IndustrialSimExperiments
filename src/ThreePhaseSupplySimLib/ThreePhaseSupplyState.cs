using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyState
{
    // Targets (operator/grid setpoints)
    public DoubleBindable TargetVoltageLL { get; set; } // V
    public DoubleBindable TargetFrequency { get; set; } // Hz

    // Anomaly toggles
    public Bindable<bool> An_UnderVoltage { get; set; }
    public Bindable<bool> An_OverVoltage { get; set; }
    public Bindable<bool> An_FrequencyDrift { get; set; }
}