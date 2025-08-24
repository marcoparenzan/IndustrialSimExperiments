namespace IndustrialSimLib;

public interface IDeviceSimulator
{
    void Step(double dt, ISimState simState);
}