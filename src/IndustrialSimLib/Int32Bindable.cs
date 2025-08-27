using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class Int32Bindable : IBindable<int>
{
    int value;
    object bounded;

    public static implicit operator int(Int32Bindable b) => b.value;
    public static implicit operator Int32Bindable(int v) => new Int32Bindable { value = v };

    public int Value => value;

    public void Set(int value)
    {
        this.value = value;
    }

    public void Add(int value)
    {
        this.value += value;
    }

    public void Reset() => Set(0);

    public object Bounded { get => bounded; set => bounded = value; }

    public override string ToString()
    {
        return value.ToString("0");
    }
}

