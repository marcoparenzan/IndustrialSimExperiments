using IndustrialSimLib;

namespace VfdSimLib;

public class VfdState
{
    public DoubleBindable TargetFrequency { get; set; }        // Hz
    public DoubleBindable BusVoltage { get; set; }             // Vdc
    public DoubleBindable HeatsinkTemp { get; set; }           // °C
    public DoubleBindable  VdcNom { get; set; }

    // Anomaly toggles
    public BoolBindable An_UnderVoltage { get; set; }
    public BoolBindable An_OverVoltage { get; set; }
    public BoolBindable An_PhaseLoss { get; set; }
    public BoolBindable An_GroundFault { get; set; }
}
