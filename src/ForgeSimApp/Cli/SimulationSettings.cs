using System.ComponentModel;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IndustrialSimApp.Cli;

public sealed class SimulationSettings : CommandSettings
{
    [CommandOption("-m|--machine <NAME>")]
    [Description("Machine module to run (default: from appsettings.json, e.g. Conveyor).")]
    public string? Machine { get; init; }

    [CommandOption("-p|--protocols <NAMES>")]
    [Description("Protocol adapters to enable, e.g. --protocols OpcUa --protocols Mqtt --protocols Console.")]
    public string[]? Protocols { get; init; }

    [CommandOption("--step-seconds <SECONDS>")]
    [Description("Simulated seconds advanced per tick.")]
    public double? StepSeconds { get; init; }

    [CommandOption("-s|--speed-factor <FACTOR>")]
    [Description("Simulation speed relative to real time (1.0 = real-time, 2.0 = twice as fast).")]
    public double? SpeedFactor { get; init; }

    [CommandOption("-d|--duration <SECONDS>")]
    [Description("Stop after this much simulated time has elapsed (0 = run until Ctrl+C).")]
    public double? DurationSeconds { get; init; }

    [CommandOption("--opcua-endpoint <URL>")]
    [Description("OPC UA server endpoint URL.")]
    public string? OpcUaEndpoint { get; init; }

    [CommandOption("--mqtt-port <PORT>")]
    [Description("MQTT broker TCP port.")]
    public int? MqttPort { get; init; }

    [CommandOption("--mqtt-topic-root <ROOT>")]
    [Description("Root MQTT topic segment.")]
    public string? MqttTopicRoot { get; init; }

    [CommandOption("--console-refresh-ms <MILLISECONDS>")]
    [Description("Minimum interval between console dashboard redraws.")]
    public int? ConsoleRefreshMilliseconds { get; init; }

    public override ValidationResult Validate()
    {
        if (StepSeconds is <= 0) return ValidationResult.Error("--step-seconds must be positive.");
        if (SpeedFactor is <= 0) return ValidationResult.Error("--speed-factor must be positive.");
        if (DurationSeconds is < 0) return ValidationResult.Error("--duration cannot be negative.");
        if (MqttPort is <= 0 or > 65535) return ValidationResult.Error("--mqtt-port must be between 1 and 65535.");
        if (ConsoleRefreshMilliseconds is <= 0) return ValidationResult.Error("--console-refresh-ms must be positive.");
        return ValidationResult.Success();
    }
}
