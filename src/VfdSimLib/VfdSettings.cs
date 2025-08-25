using IndustrialSimLib;

namespace VfdSimLib;

public class VfdSettings
{
    public DoubleBindable RatedVoltageLL { get; set; } = 400.0; // VAC line-line
    public DoubleBindable RatedFrequency { get; set; } = 50.0;  // Hz
    public DoubleBindable MaxCurrent { get; set; } = 30.0;      // A (trip threshold uses a multiple)
    public DoubleBindable Accel { get; set; } = 10.0;           // Hz/s
    public DoubleBindable Decel { get; set; } = 10.0;           // Hz/s
    public DoubleBindable VoltBoost { get; set; } = 0.07;       // p.u. of rated voltage at low speed
    public DoubleBindable ThermalTimeConstant { get; set; } = 40.0; // s (heatsink)
    public DoubleBindable MaxHeatsinkTemp { get; set; } = 85.0; // °C trip
    public DoubleBindable AmbientTemp { get; set; } = 25.0;     // °C
    public DoubleBindable OverCurrentMultiple { get; set; } = 1.6; // I_trip = multiple * MaxCurrent
    public DoubleBindable UnderVoltPUNomDC { get; set; } = 0.55;   // trip if Vdc < 0.55 * Vdc_nom
    public DoubleBindable OverVoltPUNomDC { get; set; } = 1.20;   // trip if Vdc > 1.20 * Vdc_nom
}
