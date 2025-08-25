using IndustrialSimLib;

namespace ThreePhaseSupplySimLib;

public class ThreePhaseSupplySettings
{
    public DoubleBindable NominalVoltageLL { get; set; } = 400.0; // VAC L-L
    public DoubleBindable NominalFrequency { get; set; } = 50.0;  // Hz

    public DoubleBindable VoltageSlewRate { get; set; } = 500.0;  // V/s
    public DoubleBindable FrequencySlewRate { get; set; } = 10.0; // Hz/s

    // Match legacy VFD behavior by default
    public DoubleBindable UnderVoltPU { get; set; } = 0.50;   // 50% sag
    public DoubleBindable OverVoltPU { get; set; } = 1.25;    // 25% surge
    public DoubleBindable DriftHz { get; set; } = 0.0;        // optional frequency drift
}