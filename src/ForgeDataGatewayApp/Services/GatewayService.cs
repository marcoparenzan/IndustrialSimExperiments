using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Mqtt;
using ForgeDataGatewayCore.OpcUa;
using ForgeDataGatewayCore.Sampling;

namespace ForgeDataGatewayApp.Services;

/// <summary>
/// The single stateful hub the Blazor pages bind to: owns the in-memory GatewayConfig, the running
/// SamplingEngine/MqttUnsPublisher (if started), and exposes the operations the UI needs (browse,
/// edit sources/tags, save + reload). Registered as a singleton so the sampling loop and MQTT
/// publisher survive across page navigations and reconnecting circuits.
/// </summary>
public sealed class GatewayService(IGatewayConfigStore store, ILoggerFactory loggerFactory, ILogger<GatewayService> logger)
{
    private SamplingEngine? engine;
    private MqttUnsPublisher? publisher;

    public GatewayConfig Config { get; private set; } = new();
    public bool IsRunning => engine is not null;
    public event Action? Changed;

    public async Task InitializeAsync()
    {
        Config = await store.LoadAsync();
        try
        {
            if (Config.Sources.Any(s => s.Enabled)) await StartAsync();
        }
        catch (Exception ex)
        {
            // Nothing here should ever crash app startup - worst case the gateway starts stopped
            // and the user retries from the Home page once whatever failed is fixed.
            logger.LogError(ex, "Failed to auto-start the gateway at startup.");
        }
    }

    public async Task StartAsync()
    {
        if (engine is not null) return;

        engine = new SamplingEngine(ResolveClient, loggerFactory.CreateLogger<SamplingEngine>());
        publisher = new MqttUnsPublisher(engine.Buffer, Config, loggerFactory.CreateLogger<MqttUnsPublisher>());

        await engine.StartAsync(Config);
        await publisher.StartAsync();
        logger.LogInformation("Gateway started: {SourceCount} source(s), {TagCount} tag(s).",
            Config.Sources.Count(s => s.Enabled), Config.Tags.Count(t => t.Enabled));
        Changed?.Invoke();
    }

    public async Task StopAsync()
    {
        if (engine is null) return;

        // Stop sampling first so the buffer's writer completes and the publisher can drain
        // whatever is left before its own connection is torn down.
        await engine.StopAsync();
        if (publisher is not null) await publisher.StopAsync();
        engine = null;
        publisher = null;
        logger.LogInformation("Gateway stopped.");
        Changed?.Invoke();
    }

    /// <summary>Stops the engine (if running), persists the current in-memory config, and restarts
    /// it - the only way the UI applies edits to sources/tags/namespace/mqtt settings.</summary>
    public async Task SaveAndReloadAsync()
    {
        bool wasRunning = IsRunning;
        await StopAsync();
        await store.SaveAsync(Config);
        if (wasRunning || Config.Sources.Any(s => s.Enabled)) await StartAsync();
        Changed?.Invoke();
    }

    public IReadOnlyDictionary<Guid, SampledValue> LatestValues =>
        engine?.LatestValues ?? new Dictionary<Guid, SampledValue>();

    /// <summary>Opens a short-lived client connection to browse a source's address space - independent
    /// of the long-lived sampling connection, since browsing is an on-demand UI action.</summary>
    public async Task<IReadOnlyList<BrowseNode>> BrowseAsync(Guid sourceId, string? nodeId)
    {
        var source = Config.Sources.FirstOrDefault(s => s.Id == sourceId)
            ?? throw new InvalidOperationException($"Unknown source '{sourceId}'.");

        await using ISourceClient client = ResolveClient(source.Protocol);
        await client.ConnectAsync(source);
        try
        {
            return await client.BrowseAsync(nodeId);
        }
        finally
        {
            await client.DisconnectAsync();
        }
    }

    private static ISourceClient ResolveClient(string protocol) => protocol switch
    {
        "OpcUa" => new OpcUaSourceClient(),
        _ => throw new NotSupportedException($"No ISourceClient registered for protocol '{protocol}'.")
    };
}
