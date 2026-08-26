using IndustrialSimLib;

namespace PackageSimLib;

public class Package
{
    public DoubleBindable PositionM { get; } = new();
    public DoubleBindable MassKg { get; } = new();

    public Package(double massKg = 1.0)
    {
        if (massKg is < 0.5 or > 20.0)
            throw new ArgumentOutOfRangeException(nameof(massKg), "Package mass must be between 0.5 and 20 kg.");

        MassKg.Set(massKg);
    }
}
