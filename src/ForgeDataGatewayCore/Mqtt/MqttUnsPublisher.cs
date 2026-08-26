using System.Text.Json;
using System.Threading.Channels;
using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Sampling;
using Microsoft.Extensions.Logging;
using MQTTnet;

namespace ForgeDataGatewayCore.Mqtt;

/// <summary>
/// Drains a SamplingEngine's channel and republishes each value as a retained, JSON-payload MQTT
/// message on a Unified-Namespace-style topic built from NamespaceOptions.TopicTemplate.
/// </summary>
public sealed class MqttUnsPublisher : IAsyncDisposable
{
    private readonly ChannelReader<SampledValue> reader;
    private readonly MqttOptions mqttOptions;
    private readonly NamespaceOptions namespaceOptions;
    private readonly Dictionary<Guid, TagDefinition> tagsById;
    private readonly Dictionary<Guid, SourceDefinition> sourcesById;
    private readonly ILogger<MqttUnsPublisher> logger;
    private readonly MqttClientFactory factory = new();
    private IMqttClient? client;
    private Task? pumpTask;
    private CancellationTokenSource? cts;

    public MqttUnsPublisher(ChannelReader<SampledValue> reader, GatewayConfig config, ILogger<MqttUnsPublisher> logger)
    {
        ArgumentNullException.ThrowIfNull(config);
        this.reader = reader;
        this.logger = logger;
        mqttOptions = config.Mqtt;
        namespaceOptions = config.Namespace;
        tagsById = config.Tags.ToDictionary(t => t.Id);
        sourcesById = config.Sources.ToDictionary(s => s.Id);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (client is not null) throw new InvalidOperationException("The MQTT UNS publisher is already running.");

        client = factory.CreateMqttClient();
        var options = new MqttClientOptionsBuilder()
            .WithClientId(mqttOptions.ClientId)
            .WithTcpServer(mqttOptions.Host, mqttOptions.Port)
            .Build();
        try
        {
            await client.ConnectAsync(options, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The broker being unreachable at startup shouldn't stop sampling. Every publish attempt
            // will fail-and-log (see PumpAsync) and that sample is dropped until the broker is back -
            // the bounded channel protects against a slow/backpressured broker, not a fully offline one.
            logger.LogWarning(ex, "Could not connect to MQTT broker {Host}:{Port}; sampling will continue but publishing will fail until it's reachable.",
                mqttOptions.Host, mqttOptions.Port);
        }

        cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        pumpTask = PumpAsync(cts.Token);
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await foreach (SampledValue sample in reader.ReadAllAsync(cancellationToken))
            {
                if (!tagsById.TryGetValue(sample.TagId, out var tag)) continue;
                sourcesById.TryGetValue(tag.SourceId, out var source);

                string topic = BuildTopic(namespaceOptions, source?.Name ?? "unknown", tag.Alias);
                try
                {
                    string payload = JsonSerializer.Serialize(new
                    {
                        value = sample.Value,
                        timestamp = sample.Timestamp,
                        quality = sample.Quality.ToString()
                    });
                    var message = new MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(payload)
                        .WithRetainFlag()
                        .Build();
                    await client!.PublishAsync(message, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // A broker hiccup (or the broker being briefly unreachable) shouldn't stop the
                    // drain loop - log it and keep going with the next buffered sample.
                    logger.LogWarning(ex, "Failed to publish topic '{Topic}'.", topic);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
    }

    public static string BuildTopic(NamespaceOptions options, string source, string tag) =>
        options.TopicTemplate
            .Replace("{Enterprise}", options.Enterprise)
            .Replace("{Site}", options.Site)
            .Replace("{Area}", options.Area)
            .Replace("{Line}", options.Line)
            .Replace("{Source}", source)
            .Replace("{Tag}", tag);

    public async Task StopAsync()
    {
        if (client is null) return;
        cts?.Cancel();
        if (pumpTask is not null) try { await pumpTask; } catch (OperationCanceledException) { }
        await client.DisconnectAsync();
        client.Dispose();
        client = null;
        cts?.Dispose();
        cts = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
