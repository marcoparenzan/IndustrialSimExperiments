using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorInputs
{
    // Inputs from VFD
    public DoubleBindable DriveFrequencyCmd { get; } = new(); // Hz
    public DoubleBindable DriveVoltageCmd { get; } = new();    // V (rms, LL approx)
}

public class InductionMotorOutputs
{
    // Outputs back to VFD
    public DoubleBindable PhaseCurrent { get; } = new();  // A (rms estimate)
}