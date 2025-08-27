using IndustrialSimLib;
using System;

namespace PackageSimLib;

public class Package
{
    public DoubleBindable PositionM { get; } = new(); 
    public DoubleBindable MassKg { get; } = new(); 
}
