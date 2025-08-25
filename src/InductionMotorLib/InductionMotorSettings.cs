using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorSettings
{
    public DoubleBindable RatedVoltageLL { get; set; } = 400.0; // VAC
    public DoubleBindable RatedFrequency { get; set; } = 50.0;    // Hz
    public int PolePairs { get; set; } = 2;       // 4-pole machine

    // Rated / base values for a ~7.5 kW motor (approx)
    public DoubleBindable RatedPower { get; set; } = 7500.0;  // W
    public DoubleBindable RatedSpeedRpm { get; set; } = 1440.0; // rpm
    public DoubleBindable Inertia { get; set; } = 0.20;       // kg*m^2 (motor + load reflected)
    public DoubleBindable ViscFriction { get; set; } = 0.003; // Nm per rpm
    public DoubleBindable CoulombFriction { get; set; } = 1.0; // Nm
    public DoubleBindable SlipNom { get; set; } = 0.03;       // nominal slip at rated
    public DoubleBindable TorqueMaxPU { get; set; } = 2.2;    // multiple of rated torque
    public DoubleBindable Inom { get; set; } = 15.0;          // A (phase rms)

    // Load model
    public DoubleBindable ConstLoadTorque { get; set; } = 5.0; // Nm base constant load
    public DoubleBindable JamExtraTorque { get; set; } = 80.0; // Nm extra when jammed
    public DoubleBindable BearingExtraTorque { get; set; } = 5.0; // Nm extra when bearings worn
}
