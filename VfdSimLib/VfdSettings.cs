namespace VFDSimLib;

public class VfdSettings
{
    public double RatedVoltageLL = 400.0; // VAC line-line
    public double RatedFrequency = 50.0;  // Hz
    public double MaxCurrent = 30.0;      // A (trip threshold uses a multiple)
    public double Accel = 10.0;           // Hz/s
    public double Decel = 10.0;           // Hz/s
    public double VoltBoost = 0.07;       // p.u. of rated voltage at low speed
    public double ThermalTimeConstant = 40.0; // s (heatsink)
    public double MaxHeatsinkTemp = 85.0; // °C trip
    public double AmbientTemp = 25.0;     // °C
    public double OverCurrentMultiple = 1.6; // I_trip = multiple * MaxCurrent
    public double UnderVoltPUNomDC = 0.55;   // trip if Vdc < 0.55 * Vdc_nom
    public double OverVoltPUNomDC = 1.20;   // trip if Vdc > 1.20 * Vdc_nom
}
