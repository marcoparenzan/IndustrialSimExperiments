using System.Text.Json;
using IndustrialSimLib;
using MQTTnet;
using MQTTnet.Server;

namespace MqttServerLib;

public sealed class MqttProtocolAdapter(int port, string topicRoot = "IndustrialSim") : IProtocolAdapter
{
    private readonly MqttServerFactory factory = new();
    private readonly Dictionary<string, string> topics = new(StringComparer.Ordinal);
    private MqttServer? server;
    private IMachineModule? machine;

    public string Name => "Mqtt";

    public async Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (server is not null) throw new InvalidOperationException("The MQTT adapter is already running.");
        this.machine = machine;

        var options = factory.CreateServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(port)
            .Build();
        server = factory.CreateMqttServer(options);

        topics.Clear();
        foreach (var tag in machine.Tags)
            topics[tag.Path] = $"{topicRoot}/{machine.Name}/{tag.Path.Replace('.', '/')}";

        await server.StartAsync();
    }

    public async Task PublishAsync(CancellationToken cancellationToken = default)
    {
        if (server is null || machine is null)
            throw new InvalidOperationException("The MQTT adapter has not been started.");

        foreach (var tag in machine.Tags)
        {
            if (!topics.TryGetValue(tag.Path, out var topic)) continue;
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(JsonSerializer.Serialize(tag.Value, tag.DataType))
                .WithRetainFlag()
                .Build();
            await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(message));
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (server is null) return;
        await server.StopAsync();
        server.Dispose();
        server = null;
        machine = null;
        topics.Clear();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
