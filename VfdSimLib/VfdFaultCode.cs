using IndustrialSimLib;

namespace VFDSimLib;

public record VfdFaultCode(string Code) : FaultCode(Code)
{
    public static VfdFaultCode UnderVoltage = new(nameof(UnderVoltage));
    public static VfdFaultCode OverVoltage = new(nameof(OverVoltage));
    public static VfdFaultCode OverCurrent = new(nameof(OverCurrent));
    public static VfdFaultCode OverTemp = new(nameof(OverTemp));
    public static VfdFaultCode GroundFault = new(nameof(GroundFault));
    public static VfdFaultCode PhaseLoss = new(nameof(PhaseLoss));
}
