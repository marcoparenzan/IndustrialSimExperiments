namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyInputs
{
    // Reserved for future external commands (remote setpoints, etc.)
}

public class ThreePhaseSupplyOutputs
{
    public double LineLineVoltage { get; set; } // V rms
    public double Frequency { get; set; }       // Hz
}