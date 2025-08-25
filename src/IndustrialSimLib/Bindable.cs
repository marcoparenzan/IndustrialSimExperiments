using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialSimLib;

public struct Bindable<T> : IBindable<T>
{
    T value;

    public static implicit operator T(Bindable<T> b) => b.value;
    public static implicit operator Bindable<T>(T v) => new Bindable<T> { value = v };

    public T Value
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
