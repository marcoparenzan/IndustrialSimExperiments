using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class DoubleBindable : IBindable<double>
{
    double value;
    object bounded;

    public static implicit operator double(DoubleBindable b) => b.value;
    public static implicit operator DoubleBindable(double v) => new DoubleBindable { value = v };

    public double Value => value;

    public void Set(double value)
    {
        this.value = value;
    }

    public void Add(double value)
    {
        this.value += value;
    }

    public void Reset() => Set(0.0);

    public object Bounded { get => bounded; set => bounded = value; }

    public override string ToString()
    {
        return value.ToString("0.000");
    }
}

