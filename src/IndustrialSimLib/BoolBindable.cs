using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class BoolBindable : IBindable<bool>
{
    bool value;

    public static implicit operator bool(BoolBindable b) => b.value;
    public static implicit operator BoolBindable(bool v) => new BoolBindable { value = v };

    public bool Value => value;

    public void Set(bool value)
    {
        this.value = value;
    }

    public void Add(bool value)
    {
        this.value |= value;
    }

    public void True()
    {
        this.value = true;
    }

    public void False()
    {
        this.value = false;
    }

    public void Toggle()
    {
        this.value = !this.value;
    }

    public void Reset() => Set(false);

    public object Bounded { get; set; }

    public override string ToString()
    {
        return value.ToString();
    }
}

