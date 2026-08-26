namespace IndustrialSimLib;

public interface IProtocolAdapter : IAsyncDisposable
{
    string Name { get; }
    Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default);
    Task PublishAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
