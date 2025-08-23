namespace InductionMotorSimLib;

public class InductionMotorState
{
    public double OutputFrequency { get; set; }        // Hz
    public double OutputVoltage { get; set; }          // V (fundamental rms, phase-phase approx)

    public double SpeedRpm { get; set; }    // rpm
    public double ElectTorque { get; set; } // Nm
    public double PhaseCurrent { get; set; }// A

    public double Trated { get; set; } // Nm
    public double VratedPhPh { get; set; }


    // Anomaly toggles
    public bool An_PhaseLoss { get; set; }
    public bool An_LoadJam { get; set; }
    public bool An_BearingWear { get; set; }
    public bool An_SensorNoise { get; set; }
}
