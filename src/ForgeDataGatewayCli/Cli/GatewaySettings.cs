using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace ForgeDataGatewayCli.Cli;

public sealed class GatewaySettings : CommandSettings
{
    [CommandOption("-c|--config <PATH>")]
    [Description("Path to the gateway config JSON file (default: gateway-config.json next to the executable).")]
    public string? ConfigPath { get; init; }

    [CommandOption("--mqtt-host <HOST>")]
    [Description("MQTT broker host, overriding the config file.")]
    public string? MqttHost { get; init; }

    [CommandOption("--mqtt-port <PORT>")]
    [Description("MQTT broker TCP port, overriding the config file.")]
    public int? MqttPort { get; init; }

    [CommandOption("-d|--duration <SECONDS>")]
    [Description("Stop after this many seconds (0 = run until Ctrl+C).")]
    public double DurationSeconds { get; init; }

    [CommandOption("--dashboard")]
    [Description("Show a live-updating table of the latest sampled values (interactive terminals only).")]
    public bool Dashboard { get; init; }

    public override ValidationResult Validate()
    {
        if (MqttPort is <= 0 or > 65535) return ValidationResult.Error("--mqtt-port must be between 1 and 65535.");
        if (DurationSeconds < 0) return ValidationResult.Error("--duration cannot be negative.");
        return ValidationResult.Success();
    }
}
