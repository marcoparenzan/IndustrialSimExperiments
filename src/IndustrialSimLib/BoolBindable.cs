using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialSimLib;

public struct BoolBindable : IBindable<bool>
{
    bool value;

    public static implicit operator bool(BoolBindable b) => b.value;
    public static implicit operator BoolBindable(bool v) => new BoolBindable { value = v };

    public bool Value
    {
        get => value;
        set
        {
            this.value = value;
        }
    }

    public object Bounded { get; set; }

    public override string ToString()
    {
        return value.ToString();
    }
}

