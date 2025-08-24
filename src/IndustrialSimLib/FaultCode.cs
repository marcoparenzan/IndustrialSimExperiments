namespace IndustrialSimLib;

public record FaultCode(string Code)
{
    public static FaultCode None = new(nameof(None));
}
