namespace KpiLib.Models;

public class UnitConversion
{
    public long UnitFrom { get; set; }
    public long UnitTo { get; set; }
    public double Factor { get; set; }
    public double Offset { get; set; }
}
