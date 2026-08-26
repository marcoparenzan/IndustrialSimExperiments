using System.Globalization;
using ConsoleDashboardLib;
using ConveyorSimLib;
using IndustrialSimLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MqttServerLib;
using OpcUaServerLib;
using Spectre.Console.Cli;

namespace IndustrialSimApp.Cli;

public sealed class SimulationCommand : AsyncCommand<SimulationSettings>
{
    protected override async Task<int> ExecuteAsync(CommandContext context, SimulationSettings settings, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                             .AddEnvironmentVariables(prefix: "INDUSTRIALSIM_")
                             .AddInMemoryCollection(BuildOverrides(settings));

        // Array config values merge by index across providers rather than being replaced outright,
        // so a --protocols override shorter than appsettings.json's list would leak trailing entries
        // through. Resolve the effective list here instead of routing it through IConfiguration.
        string[] enabledProtocols = settings.Protocols is { Length: > 0 }
            ? settings.Protocols
            : builder.Configuration.GetSection("Simulation:Protocols").Get<string[]>() ?? ["OpcUa"];
        builder.Services.AddSingleton<IReadOnlyList<string>>(enabledProtocols);

        builder.Services.Configure<ConveyorOptions>(builder.Configuration.GetSection("Machines:Conveyor"));
        builder.Services.AddSingleton<ConveyorMachine>(services =>
            new ConveyorMachine(services.GetRequiredService<Microsoft.Extensions.Options.IOptions<ConveyorOptions>>().Value));
        builder.Services.AddSingleton<IMachineModule>(services =>
        {
            string machine = builder.Configuration["Simulation:Machine"] ?? "Conveyor";
            return machine.Equals("Conveyor", StringComparison.OrdinalIgnoreCase)
                ? services.GetRequiredService<ConveyorMachine>()
                : throw new InvalidOperationException($"Unknown machine module '{machine}'.");
        });
        builder.Services.AddSingleton<IProtocolAdapter>(_ => new OpcUaProtocolAdapter(
            builder.Configuration["Protocols:OpcUa:Endpoint"] ?? "opc.tcp://localhost:4840/IndustrialSim"));
        builder.Services.AddSingleton<IProtocolAdapter>(_ => new MqttProtocolAdapter(
            builder.Configuration.GetValue("Protocols:Mqtt:Port", 1883),
            builder.Configuration["Protocols:Mqtt:TopicRoot"] ?? "IndustrialSim"));
        builder.Services.AddSingleton<IProtocolAdapter>(_ => new ConsoleDashboardProtocolAdapter(
            TimeSpan.FromMilliseconds(builder.Configuration.GetValue("Protocols:Console:RefreshIntervalMs", 200))));
        builder.Services.AddHostedService<SimulationWorker>();

        await builder.Build().RunAsync();
        return 0;
    }

    private static Dictionary<string, string?> BuildOverrides(SimulationSettings settings)
    {
        var overrides = new Dictionary<string, string?>();

        if (settings.Machine is not null) overrides["Simulation:Machine"] = settings.Machine;
        if (settings.StepSeconds is { } stepSeconds) overrides["Simulation:StepSeconds"] = stepSeconds.ToString(CultureInfo.InvariantCulture);
        if (settings.SpeedFactor is { } speedFactor) overrides["Simulation:SpeedFactor"] = speedFactor.ToString(CultureInfo.InvariantCulture);
        if (settings.DurationSeconds is { } durationSeconds) overrides["Simulation:DurationSeconds"] = durationSeconds.ToString(CultureInfo.InvariantCulture);
        if (settings.OpcUaEndpoint is not null) overrides["Protocols:OpcUa:Endpoint"] = settings.OpcUaEndpoint;
        if (settings.MqttPort is { } mqttPort) overrides["Protocols:Mqtt:Port"] = mqttPort.ToString(CultureInfo.InvariantCulture);
        if (settings.MqttTopicRoot is not null) overrides["Protocols:Mqtt:TopicRoot"] = settings.MqttTopicRoot;
        if (settings.ConsoleRefreshMilliseconds is { } consoleRefreshMs) overrides["Protocols:Console:RefreshIntervalMs"] = consoleRefreshMs.ToString(CultureInfo.InvariantCulture);

        return overrides;
    }
}
