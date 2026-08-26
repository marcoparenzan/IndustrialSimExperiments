using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class DoubleBindable : IBindable<double>
{
    private double value;

    public static implicit operator double(DoubleBindable b) => b.value;
    public static implicit operator DoubleBindable(double v) => new() { value = v };

    public double Value => value;
    public object Bounded { get; set; } = null!;

    public void Set(double value) => this.value = value;
    public void Add(double value) => this.value += value;
    public void Reset() => Set(0.0);
    public override string ToString() => value.ToString("0.000");
}
