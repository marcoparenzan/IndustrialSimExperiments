using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class Int32Bindable : IBindable<int>
{
    private int value;

    public static implicit operator int(Int32Bindable b) => b.value;
    public static implicit operator Int32Bindable(int v) => new() { value = v };

    public int Value => value;
    public object Bounded { get; set; } = null!;

    public void Set(int value) => this.value = value;
    public void Add(int value) => this.value += value;
    public void Reset() => Set(0);
    public override string ToString() => value.ToString("0");
}
