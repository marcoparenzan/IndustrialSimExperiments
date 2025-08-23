namespace IndustrialSimLib;

public class ToggleAnomalyEvent : SimEvent
{
    public string Key = string.Empty; // e.g., "undervoltage", "phaseloss", "loadjam", ...
    public bool Enable;
    public override void Apply(ISimState vfd)
    {
        vfd.ToggleAnomaly(Key, Enable);
        vfd.Log($"[{vfd.Time,6:F2}s] {(Enable ? "EN" : "DIS")}ABLE anomaly '{Key}'");
    }
}
