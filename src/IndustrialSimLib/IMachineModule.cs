namespace IndustrialSimLib;

public interface IMachineModule
{
    string Name { get; }
    double SimulationTime { get; }
    IReadOnlyCollection<SimulationTag> Tags { get; }
    void Step(double deltaTimeSeconds);
}
