namespace IndustrialSimLib.SimEvents;

public sealed class ToggleAnomalyActionEvent : SimEvent
{
    private readonly Action<ISimState> action;

    public ToggleAnomalyActionEvent(double time, Action<ISimState> action)
    {
        Time = time;
        this.action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public override void Apply(ISimState simState) => action(simState);
}
