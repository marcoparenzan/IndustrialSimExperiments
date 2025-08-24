using IndustrialSimLib;

namespace InductionMotorSimLib;

public record InductionMotorFaultCode(string Code) : FaultCode(Code)
{
    //public static InductionMotorFaultCode XXX = new(nameof(XXX));
}
