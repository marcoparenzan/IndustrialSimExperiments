namespace VFDSimLib;

public class VfdInputs
{
    // Feedback from motor to the VFD
    public double MotorCurrentFeedback { get; set; } // A (rms)
}

public class VfdOutputs
{
    // VFD outputs toward the motor
    public double OutputFrequency { get; set; }  // Hz
    public double OutputVoltage { get; set; }    // V (rms, LL approx)
}