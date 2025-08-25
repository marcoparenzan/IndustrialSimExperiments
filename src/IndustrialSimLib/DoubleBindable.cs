using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IndustrialSimLib;

public struct DoubleBindable : IBindable<double>
{
    double value;

    public static implicit operator double(DoubleBindable b) => b.value;
    public static implicit operator DoubleBindable(double v) => new DoubleBindable { value = v };

    public double Value
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
        return value.ToString("0.000");
    }
}

