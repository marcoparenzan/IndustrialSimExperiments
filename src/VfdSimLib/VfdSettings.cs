using IndustrialSimLib;

namespace VfdSimLib;

public class VfdSettings
{
    public double RatedVoltageLL { get; set; } = 400.0; // VAC line-line
    public double RatedFrequency { get; set; } = 50.0;  // Hz
    public double MaxCurrent { get; set; } = 30.0;      // A (trip threshold uses a multiple)
    public double Accel { get; set; } = 10.0;           // Hz/s
    public double Decel { get; set; } = 10.0;           // Hz/s
    public double VoltBoost { get; set; } = 0.07;       // p.u. of rated voltage at low speed
    public double ThermalTimeConstant { get; set; } = 40.0; // s (heatsink)
    public double MaxHeatsinkTemp { get; set; } = 85.0; // °C trip
    public double AmbientTemp { get; set; } = 25.0;     // °C
    public double OverCurrentMultiple { get; set; } = 1.6; // I_trip = multiple * MaxCurrent
    public double UnderVoltPUNomDC { get; set; } = 0.55;   // trip if Vdc < 0.55 * Vdc_nom
    public double OverVoltPUNomDC { get; set; } = 1.20;   // trip if Vdc > 1.20 * Vdc_nom
}
