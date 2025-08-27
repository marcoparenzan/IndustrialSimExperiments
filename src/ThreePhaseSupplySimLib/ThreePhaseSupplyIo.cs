using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplyInputs
{
    // Reserved for future external commands (remote setpoints, etc.)
}

public class ThreePhaseSupplyOutputs
{
    public DoubleBindable LineLineVoltage { get; } = new();  // V rms
    public DoubleBindable Frequency { get; } = new();        // Hz
}