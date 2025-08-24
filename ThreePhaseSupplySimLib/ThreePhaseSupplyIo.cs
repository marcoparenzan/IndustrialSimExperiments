namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyInputs
{
    // Reserved for future external commands (e.g., remote setpoints)
}

public class ThreePhaseSupplyOutputs
{
    public double LineLineVoltage { get; set; } // V rms
    public double Frequency { get; set; }       // Hz
}