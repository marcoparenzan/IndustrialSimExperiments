namespace InductionMotorSimLib;

public class InductionMotorState
{
    public double SpeedRpm { get; set; }    // rpm
    public double ElectTorque { get; set; } // Nm

    public double Trated { get; set; }      // Nm
    public double VratedPhPh { get; set; }  // V (rated line-line)

    // Anomaly toggles
    public bool An_PhaseLoss { get; set; }
    public bool An_LoadJam { get; set; }
    public bool An_BearingWear { get; set; }
    public bool An_SensorNoise { get; set; }
}
