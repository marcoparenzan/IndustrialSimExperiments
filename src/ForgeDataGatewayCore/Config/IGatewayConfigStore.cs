namespace ForgeDataGatewayCore.Config;

public interface IGatewayConfigStore
{
    Task<GatewayConfig> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(GatewayConfig config, CancellationToken cancellationToken = default);
}
