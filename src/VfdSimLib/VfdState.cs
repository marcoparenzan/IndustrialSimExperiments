using IndustrialSimLib;

namespace VfdSimLib;

public class VfdState
{
    public DoubleBindable TargetFrequency { get; } = new();         // Hz
    public DoubleBindable BusVoltage { get; } = new();              // Vdc
    public DoubleBindable HeatsinkTemp { get; } = new();            // °C
    public DoubleBindable  VdcNom { get; } = new();

    // Anomaly toggles
    public BoolBindable An_UnderVoltage { get; } = new(); 
    public BoolBindable An_OverVoltage { get; } = new(); 
    public BoolBindable An_PhaseLoss { get; } = new(); 
    public BoolBindable An_GroundFault { get; } = new(); 
}
