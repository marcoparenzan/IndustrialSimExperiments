
namespace IndustrialSimLib;

public interface ISimState
{
    FaultCode ActiveTrip { get; }
    bool Running { get; }
    double Time { get; }

    double Step(double dt);
    void Log(string log);
    void ToggleAnomaly(string key, bool enable);
    void ResetTrip();
    void Trip(FaultCode fc);
}