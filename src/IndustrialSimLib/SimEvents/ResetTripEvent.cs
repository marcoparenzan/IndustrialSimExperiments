namespace IndustrialSimLib.SimEvents;

public class ResetTripEvent : SimEvent
{
    public override void Apply(ISimState vfd) => vfd.ResetTrip();
}
