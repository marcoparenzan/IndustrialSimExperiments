using System.Collections.Concurrent;
using System.Threading.Channels;
using ForgeDataGatewayCore.Config;
using Microsoft.Extensions.Logging;

namespace ForgeDataGatewayCore.Sampling;

/// <summary>
/// Connects one ISourceClient per enabled source and runs one PeriodicTimer-driven sampling loop per
/// enabled tag. Every sample updates a non-destructive "latest value" snapshot (for UI reads) and is
/// written into a bounded channel (the in-memory queue buffer a downstream publisher drains).
/// </summary>
public sealed class SamplingEngine(Func<string, ISourceClient> resolveClient, ILogger<SamplingEngine> logger) : IAsyncDisposable
{
    private readonly Dictionary<Guid, ISourceClient> clientsBySourceId = [];
    private readonly ConcurrentDictionary<Guid, SampledValue> latestValues = new();
    private readonly Channel<SampledValue> channel = Channel.CreateBounded<SampledValue>(
        new BoundedChannelOptions(10_000) { FullMode = BoundedChannelFullMode.DropOldest, SingleReader = true });
    private List<Task> sampleLoops = [];
    private CancellationTokenSource? cts;

    public ChannelReader<SampledValue> Buffer => channel.Reader;
    public IReadOnlyDictionary<Guid, SampledValue> LatestValues => latestValues;

    public async Task StartAsync(GatewayConfig config, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);
        if (cts is not null) throw new InvalidOperationException("The sampling engine is already running.");
        cts = new CancellationTokenSource();

        foreach (var source in config.Sources.Where(s => s.Enabled))
        {
            ISourceClient? client = null;
            try
            {
                client = resolveClient(source.Protocol);
                await client.ConnectAsync(source, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One unreachable or misconfigured source (unreachable endpoint, unsupported protocol,
                // ...) shouldn't prevent the others from starting - skip its tags and keep going.
                logger.LogWarning(ex, "Could not connect to source '{Source}' ({Endpoint}); its tags will not be sampled.",
                    source.Name, source.Endpoint);
                if (client is not null) await client.DisposeAsync();
                continue;
            }
            clientsBySourceId[source.Id] = client;

            foreach (var tag in config.Tags.Where(t => t.Enabled && t.SourceId == source.Id))
                sampleLoops.Add(SampleLoopAsync(client, tag, source.DefaultSamplingIntervalMs, cts.Token));
        }
    }

    private async Task SampleLoopAsync(ISourceClient client, TagDefinition tag, int defaultIntervalMs, CancellationToken cancellationToken)
    {
        // PeriodicTimer truncates its period to whole milliseconds and rejects anything below 1ms
        // (see ForgeSimApp/SimulationWorker.cs), so the interval is floored here too.
        double intervalMs = Math.Max(1.0, tag.SamplingIntervalMs ?? defaultIntervalMs);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(intervalMs));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                // ISourceClient.ReadAsync is expected to report failures as Bad-quality samples rather
                // than throwing (see OpcUaSourceClient.ReadAsync), but this loop stays defensive against
                // implementations that don't - a single bad tick must never take the whole loop down.
                try
                {
                    SampledValue value = await client.ReadAsync(tag, cancellationToken);
                    latestValues[tag.Id] = value;
                    await channel.Writer.WriteAsync(value, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogWarning(ex, "Failed to read tag '{Alias}' ({NodeId}).", tag.Alias, tag.NodeId);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public async Task StopAsync()
    {
        if (cts is null) return;
        await cts.CancelAsync();
        try { await Task.WhenAll(sampleLoops); } catch (OperationCanceledException) { }
        sampleLoops = [];

        foreach (var client in clientsBySourceId.Values) await client.DisconnectAsync();
        clientsBySourceId.Clear();
        channel.Writer.TryComplete();

        cts.Dispose();
        cts = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
