namespace InductionMotorSimLib;

public class InductionMotorSettings
{
    public double VratedLL = 400.0; // VAC
    public double Frated = 50.0;    // Hz
    public int PolePairs = 2;       // 4-pole machine

    // Rated / base values for a ~7.5 kW motor (approx)
    public double RatedPower = 7500.0;  // W
    public double RatedSpeedRpm = 1440.0; // rpm
    public double Inertia = 0.20;       // kg*m^2 (motor + load reflected)
    public double ViscFriction = 0.003; // Nm per rpm
    public double CoulombFriction = 1.0; // Nm
    public double SlipNom = 0.03;       // nominal slip at rated
    public double TorqueMaxPU = 2.2;    // multiple of rated torque
    public double Inom = 15.0;          // A (phase rms)

    // Load model
    public double ConstLoadTorque = 5.0; // Nm base constant load
    public double JamExtraTorque = 80.0; // Nm extra when jammed
    public double BearingExtraTorque = 5.0; // Nm extra when bearings worn
}
