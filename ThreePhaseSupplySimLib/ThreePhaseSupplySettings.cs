namespace VFDSimLib;

public class ThreePhaseSupplySettings
{
    public double NominalVoltageLL { get; set; } = 400.0; // VAC L-L
    public double NominalFrequency { get; set; } = 50.0;  // Hz

    public double VoltageSlewRate { get; set; } = 500.0;  // V/s
    public double FrequencySlewRate { get; set; } = 10.0; // Hz/s

    // Match legacy VFD behavior
    public double UnderVoltPU { get; set; } = 0.50;   // 50% sag
    public double OverVoltPU { get; set; } = 1.25;    // 25% surge
    public double DriftHz { get; set; } = 0.0;
}