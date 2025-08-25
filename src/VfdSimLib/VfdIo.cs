using IndustrialSimLib;

namespace VfdSimLib;

public class VfdInputs
{
    // Feedback from motor to the VFD
    public DoubleBindable MotorCurrentFeedback { get; set; } // A (rms)

    // Grid/supply inputs
    public DoubleBindable SupplyVoltageLL { get; set; }  // V (rms L-L)
    public DoubleBindable SupplyFrequency { get; set; }  // Hz
}

public class VfdOutputs
{
    // VFD outputs toward the motor
    public DoubleBindable OutputFrequency { get; set; }  // Hz
    public DoubleBindable OutputVoltage { get; set; }    // V (rms, LL approx)
}