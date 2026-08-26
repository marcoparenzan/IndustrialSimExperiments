using System.Text.Json;

namespace ForgeDataGatewayCore.Config;

public sealed class JsonFileGatewayConfigStore(string path) : IGatewayConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<GatewayConfig> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path)) return new GatewayConfig();
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<GatewayConfig>(stream, JsonOptions, cancellationToken) ?? new GatewayConfig();
    }

    public async Task SaveAsync(GatewayConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, config, JsonOptions, cancellationToken);
    }
}
