using ForgeDataGatewayCli.Runner;
using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Mqtt;
using ForgeDataGatewayCore.OpcUa;
using ForgeDataGatewayCore.Sampling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

namespace ForgeDataGatewayCli.Cli;

public sealed class GatewayCommand : AsyncCommand<GatewaySettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, GatewaySettings settings, CancellationToken cancellationToken)
    {
        string configPath = settings.ConfigPath ?? Path.Combine(AppContext.BaseDirectory, "gateway-config.json");
        var store = new JsonFileGatewayConfigStore(configPath);
        GatewayConfig config = await store.LoadAsync(cancellationToken);

        if (settings.MqttHost is not null) config.Mqtt.Host = settings.MqttHost;
        if (settings.MqttPort is { } mqttPort) config.Mqtt.Port = mqttPort;

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton(config);
        builder.Services.AddSingleton(services => new SamplingEngine(
            ResolveClient, services.GetRequiredService<ILogger<SamplingEngine>>()));
        builder.Services.AddSingleton(services => new MqttUnsPublisher(
            services.GetRequiredService<SamplingEngine>().Buffer,
            services.GetRequiredService<GatewayConfig>(),
            services.GetRequiredService<ILogger<MqttUnsPublisher>>()));
        builder.Services.AddSingleton(new GatewayRunnerOptions(settings.DurationSeconds, settings.Dashboard));
        builder.Services.AddHostedService<GatewayRunner>();

        await builder.Build().RunAsync(cancellationToken);
        return 0;
    }

    private static ISourceClient ResolveClient(string protocol) => protocol switch
    {
        "OpcUa" => new OpcUaSourceClient(),
        _ => throw new NotSupportedException($"No ISourceClient registered for protocol '{protocol}'.")
    };
}
