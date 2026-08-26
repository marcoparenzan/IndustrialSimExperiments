using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Mqtt;
using ForgeDataGatewayCore.Sampling;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ForgeDataGatewayCli.Runner;

internal sealed class GatewayRunner(
    GatewayConfig config,
    SamplingEngine engine,
    MqttUnsPublisher publisher,
    GatewayRunnerOptions options,
    IHostApplicationLifetime lifetime,
    ILogger<GatewayRunner> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation(
            "Starting ForgeDataGateway: {SourceCount} source(s), {TagCount} tag(s), publishing to mqtt://{Host}:{Port}",
            config.Sources.Count(s => s.Enabled), config.Tags.Count(t => t.Enabled), config.Mqtt.Host, config.Mqtt.Port);

        await engine.StartAsync(config, stoppingToken);
        await publisher.StartAsync(stoppingToken);

        Task dashboardTask = options.Dashboard
            ? new LiveValueDashboard(engine, config).RunAsync(stoppingToken)
            : Task.CompletedTask;

        try
        {
            if (options.DurationSeconds > 0)
                await Task.Delay(TimeSpan.FromSeconds(options.DurationSeconds), stoppingToken);
            else
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            // Stop sampling first so the buffer's writer completes and the publisher can drain
            // whatever is left before its own connection is torn down.
            await engine.StopAsync();
            await publisher.StopAsync();
            try { await dashboardTask; } catch (OperationCanceledException) { }
            logger.LogInformation("Stopped ForgeDataGateway.");
        }

        lifetime.StopApplication();
    }
}
