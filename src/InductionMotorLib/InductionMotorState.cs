using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorState
{
    public DoubleBindable SpeedRpm { get; set; }    // rpm
    public DoubleBindable ElectTorque { get; set; } // Nm

    public DoubleBindable Trated { get; set; }      // Nm
    public DoubleBindable VratedPhPh { get; set; }  // V (rated line-line)

    // Anomaly toggles
    public Bindable<bool> An_PhaseLoss { get; set; }
    public Bindable<bool> An_LoadJam { get; set; }
    public Bindable<bool> An_BearingWear { get; set; }
    public Bindable<bool> An_SensorNoise { get; set; }
}
