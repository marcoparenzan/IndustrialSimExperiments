using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorState
{
    public DoubleBindable SpeedRpm { get; } = new();     // rpm
    public DoubleBindable ElectTorque { get; } = new();  // Nm

    public DoubleBindable Trated { get; } = new();           // Nm
    public DoubleBindable VratedPhPh { get; } = new();         // V (rated line-line)

    // Anomaly toggles
    public BoolBindable An_PhaseLoss { get; } = new(); 
    public BoolBindable An_LoadJam { get; } = new(); 
    public BoolBindable An_BearingWear { get; } = new(); 
    public BoolBindable An_SensorNoise { get; } = new(); 
}
