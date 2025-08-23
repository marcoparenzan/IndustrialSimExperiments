namespace IndustrialSimLib;

public abstract class SimEvent
{
    public double Time;
    public abstract void Apply(ISimState vfd);
    public override string ToString() => $"{GetType().Name}@{Time:F2}s";
}
