using IndustrialSimLib;

namespace VfdSimLib;

public class VfdState
{
    public DoubleBindable TargetFrequency { get; set; }        // Hz
    public DoubleBindable BusVoltage { get; set; }             // Vdc
    public DoubleBindable HeatsinkTemp { get; set; }           // °C
    public DoubleBindable  VdcNom { get; set; }

    // Anomaly toggles
    public Bindable<bool> An_UnderVoltage { get; set; }
    public Bindable<bool> An_OverVoltage { get; set; }
    public Bindable<bool> An_PhaseLoss { get; set; }
    public Bindable<bool> An_GroundFault { get; set; }
}
