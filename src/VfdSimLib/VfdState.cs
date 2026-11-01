using IndustrialSimLib;

namespace VfdSimLib;

public class VfdState
{
    public DoubleBindable TargetFrequency { get; } = new();         // Hz
    public DoubleBindable BusVoltage { get; } = new();              // Vdc
    public DoubleBindable HeatsinkTemp { get; } = new();            // °C
    public DoubleBindable  VdcNom { get; } = new();
    public DoubleBindable BusExcessEnergyJ { get; } = new();        // J stored in the DC link above the load-dependent level (regeneration)
    public BoolBindable BrakeChopperActive { get; } = new();
    public DoubleBindable BrakingPowerW { get; } = new();           // W dissipated in the braking resistor

    // Anomaly toggles
    public BoolBindable An_UnderVoltage { get; } = new(); 
    public BoolBindable An_OverVoltage { get; } = new(); 
    public BoolBindable An_PhaseLoss { get; } = new(); 
    public BoolBindable An_GroundFault { get; } = new(); 
}
