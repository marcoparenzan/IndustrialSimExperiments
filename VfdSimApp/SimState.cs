using IndustrialSimLib;

class SimState(Action<string, bool> toggle): ISimState
{
    public double Time { get; set; } = 0;
    public bool Running { get; set; } = true;
    public FaultCode ActiveTrip { get; set; } = FaultCode.None;
    public List<string> EventLog { get; set; } = new();

    public void Trip(FaultCode fc)
    {
        if (ActiveTrip != FaultCode.None) return;
        ActiveTrip = fc;
        Running = false;
        EventLog.Add($"[{Time,6:F2}s] TRIP: {fc}");
    }

    public void ResetTrip()
    {
        ActiveTrip = FaultCode.None;
        Running = true;
        EventLog.Add($"[{Time,6:F2}s] RESET trip");
    }

    public void Log(string log)
    {
        EventLog.Add($"[{Time,6:F2}s] {log}");
    }

    public void ToggleAnomaly(string key, bool enable)
    {
        toggle(key, enable);
    }
}