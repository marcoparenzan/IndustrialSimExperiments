using System.Diagnostics;

namespace IndustrialSimLib;

[DebuggerDisplay("{Value}")]
public class BoolBindable : IBindable<bool>
{
    private bool value;

    public static implicit operator bool(BoolBindable b) => b.value;
    public static implicit operator BoolBindable(bool v) => new() { value = v };

    public bool Value => value;
    public object Bounded { get; set; } = null!;

    public void Set(bool value) => this.value = value;
    public void Add(bool value) => this.value |= value;
    public void True() => value = true;
    public void False() => value = false;
    public void Toggle() => value = !value;
    public void Reset() => Set(false);
    public override string ToString() => value.ToString();
}
