namespace InductionMotorSimLib;

public class InductionMotorInputs
{
    // Inputs from VFD
    public double DriveFrequencyCmd { get; set; } // Hz
    public double DriveVoltageCmd { get; set; }   // V (rms, LL approx)
}

public class InductionMotorOutputs
{
    // Outputs back to VFD
    public double PhaseCurrent { get; set; } // A (rms estimate)
}