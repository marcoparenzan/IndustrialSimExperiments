using IndustrialSimLib;

namespace VfdSimLib;

public class VfdInputs
{
    // Feedback from motor to the VFD
    public DoubleBindable MotorCurrentFeedback { get; } = new();  // A (rms)

    // Grid/supply inputs
    public DoubleBindable SupplyVoltageLL { get; } = new();   // V (rms L-L)
    public DoubleBindable SupplyFrequency { get; } = new();  // Hz
}

public class VfdOutputs
{
    // VFD outputs toward the motor
    public DoubleBindable OutputFrequency { get; } = new();   // Hz
    public DoubleBindable OutputVoltage { get; } = new();     // V (rms, LL approx)
}