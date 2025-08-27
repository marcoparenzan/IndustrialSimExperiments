using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorSettings
{
    public double RatedVoltageLL { get; set; } = 400.0; // VAC
    public double RatedFrequency { get; set; } = 50.0;    // Hz
    public int PolePairs { get; set; } = 2;       // 4-pole machine

    // Rated / base values for a ~7.5 kW motor (approx)
    public double RatedPower { get; set; } = 7500.0;  // W
    public double RatedSpeedRpm { get; set; } = 1440.0; // rpm
    public double Inertia { get; set; } = 0.20;       // kg*m^2 (motor + load reflected)
    public double ViscFriction { get; set; } = 0.003; // Nm per rpm
    public double CoulombFriction { get; set; } = 1.0; // Nm
    public double SlipNom { get; set; } = 0.03;       // nominal slip at rated
    public double TorqueMaxPU { get; set; } = 2.2;    // multiple of rated torque
    public double Inom { get; set; } = 15.0;          // A (phase rms)

    // Load model
    public double ConstLoadTorque { get; set; } = 5.0; // Nm base constant load
    public double JamExtraTorque { get; set; } = 80.0; // Nm extra when jammed
    public double BearingExtraTorque { get; set; } = 5.0; // Nm extra when bearings worn
}
