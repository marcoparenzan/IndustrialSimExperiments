using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Sampling;
using Spectre.Console;

namespace ForgeDataGatewayCli.Runner;

/// <summary>
/// Renders SamplingEngine.LatestValues as a live-updating Spectre.Console table. Mirrors the
/// interactive-only guard used by ConsoleDashboardProtocolAdapter in the simulator: Spectre's live
/// display drives the terminal directly and can hang against a redirected/piped stdout, so this
/// no-ops when the console isn't a real interactive terminal.
/// </summary>
public sealed class LiveValueDashboard(SamplingEngine engine, GatewayConfig config)
{
    private static readonly bool Interactive = AnsiConsole.Profile.Capabilities.Interactive;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (!Interactive) return;

        var table = new Table().Border(TableBorder.Rounded).Expand().Title("[bold]ForgeDataGateway[/]");
        table.AddColumn("Source");
        table.AddColumn("Tag");
        table.AddColumn(new TableColumn("Value").RightAligned());
        table.AddColumn("Quality");
        table.AddColumn("Timestamp");

        var rows = config.Tags.Select(tag => (Tag: tag, Source: config.Sources.FirstOrDefault(s => s.Id == tag.SourceId))).ToList();
        foreach (var row in rows) table.AddRow(row.Source?.Name ?? "?", row.Tag.Alias, "-", "-", "-");

        try
        {
            await AnsiConsole.Live(table).StartAsync(async ctx =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    for (int i = 0; i < rows.Count; i++)
                    {
                        if (!engine.LatestValues.TryGetValue(rows[i].Tag.Id, out SampledValue? value)) continue;
                        table.UpdateCell(i, 2, value.Value?.ToString() ?? "-");
                        table.UpdateCell(i, 3, value.Quality.ToString());
                        table.UpdateCell(i, 4, value.Timestamp.ToString("HH:mm:ss.fff"));
                    }
                    ctx.Refresh();
                    await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
                }
            });
        }
        catch (OperationCanceledException) { }
    }
}
