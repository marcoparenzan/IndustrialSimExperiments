using IndustrialSimLib;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IndustrialSimApp;

internal sealed class SimulationWorker(
    IMachineModule machine,
    IEnumerable<IProtocolAdapter> availableAdapters,
    IReadOnlyList<string> enabledProtocols,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ILogger<SimulationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        double stepSeconds = configuration.GetValue("Simulation:StepSeconds", 0.01);
        double speedFactor = configuration.GetValue("Simulation:SpeedFactor", 1.0);
        double durationSeconds = configuration.GetValue("Simulation:DurationSeconds", 0.0);
        var adapters = availableAdapters.Where(a => enabledProtocols.Contains(a.Name, StringComparer.OrdinalIgnoreCase)).ToArray();

        logger.LogInformation("Starting {Machine} with protocols [{Protocols}]", machine.Name, string.Join(", ", adapters.Select(a => a.Name)));
        foreach (var adapter in adapters) await adapter.StartAsync(machine, stoppingToken);

        // PeriodicTimer truncates its period to whole milliseconds and rejects anything below 1ms,
        // so the period is floored at 1ms; very high SpeedFactor values are effectively capped.
        double periodMilliseconds = Math.Max(1.0, stepSeconds / Math.Max(speedFactor, 0.001) * 1000.0);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(periodMilliseconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                machine.Step(stepSeconds);
                foreach (var adapter in adapters) await adapter.PublishAsync(stoppingToken);
                if (durationSeconds > 0 && machine.SimulationTime >= durationSeconds)
                {
                    lifetime.StopApplication();
                    break;
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            foreach (var adapter in adapters.Reverse()) await adapter.StopAsync(CancellationToken.None);
            logger.LogInformation("Stopped {Machine} at simulation time {Time:F2}s", machine.Name, machine.SimulationTime);
        }
    }
}
