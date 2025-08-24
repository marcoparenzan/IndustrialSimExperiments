namespace IndustrialSimLib;

public abstract class SimEvent
{
    double time;
    public double Time { get => time; set => time = value < 0 ? 0 : value; } // s
    public abstract void Apply(ISimState vfd);
    public override string ToString() => $"{GetType().Name}@{Time:F2}s";
}
