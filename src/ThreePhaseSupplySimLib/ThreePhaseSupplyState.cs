using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyState
{
    // Targets (operator/grid setpoints)
    public DoubleBindable TargetVoltageLL { get; } = new(); // V
    public DoubleBindable TargetFrequency { get; } = new();  // Hz

    // Anomaly toggles
    public BoolBindable An_UnderVoltage { get; } = new(); 
    public BoolBindable An_OverVoltage { get; } = new(); 
    public BoolBindable An_FrequencyDrift { get; } = new(); 
}