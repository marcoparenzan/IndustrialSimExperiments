using System;
using System.Collections.Generic;
using System.Text;

namespace IndustrialSimLib.SimEvents;

public class ToggleAnomalyActionEvent(double time, Action<ISimState> action) : SimEvent
{
    public double Time { get; set; } = time;
    public override void Apply(ISimState simState) => action(simState);
}