using IndustrialSimLib;

namespace InductionMotorSimLib;

public class InductionMotorInputs
{
    // Inputs from VFD
    public DoubleBindable DriveFrequencyCmd { get; set; } // Hz
    public DoubleBindable DriveVoltageCmd { get; set; }   // V (rms, LL approx)
}

public class InductionMotorOutputs
{
    // Outputs back to VFD
    public DoubleBindable PhaseCurrent { get; set; } // A (rms estimate)
}