namespace ConveyorSimLib;

public sealed class ConveyorOptions
{
    public int SegmentCount { get; set; } = 5;
    public double LengthMeters { get; set; } = 50;
    public double TargetFrequencyHz { get; set; } = 30;
    public double PackageSpawnPeriodSeconds { get; set; } = 1;
    public double PackageMassKg { get; set; } = 5;
    public double PulleyRadiusMeters { get; set; } = 0.15;
    public double GearRatio { get; set; } = 12;
    public double MechanicalEfficiency { get; set; } = 0.9;
    public double RollingCoefficient { get; set; } = 0.03;
}
