using IndustrialSimLib;
using Spectre.Console;

namespace ConsoleDashboardLib;

/// <summary>
/// Renders machine tags as a live-updating table. Spectre's live display drives the terminal
/// directly (cursor control, ANSI queries) and is only meaningful - and safe - on a real
/// interactive terminal, so this adapter is a no-op whenever output is redirected/piped
/// (e.g. under a test harness or when logs are captured to a file).
/// </summary>
public sealed class ConsoleDashboardProtocolAdapter(TimeSpan refreshInterval) : IProtocolAdapter
{
    private readonly bool interactive = AnsiConsole.Profile.Capabilities.Interactive;
    private readonly Table table = new Table().Border(TableBorder.Rounded).Expand();
    private readonly List<SimulationTag> rows = [];
    private IMachineModule? machine;
    private LiveDisplayContext? liveContext;
    private Task? liveTask;
    private TaskCompletionSource? stopSignal;
    private DateTime lastRefresh = DateTime.MinValue;

    public string Name => "Console";

    public Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (liveTask is not null) throw new InvalidOperationException("The console dashboard is already running.");
        this.machine = machine;
        if (!interactive) return Task.CompletedTask;

        table.Title($"[bold]{machine.Name}[/]");
        table.AddColumn("Tag");
        table.AddColumn(new TableColumn("Value").RightAligned());
        rows.Clear();
        foreach (var tag in machine.Tags)
        {
            rows.Add(tag);
            table.AddRow(tag.Path, "-");
        }

        stopSignal = new TaskCompletionSource();
        var started = new TaskCompletionSource();
        liveTask = AnsiConsole.Live(table).StartAsync(async ctx =>
        {
            liveContext = ctx;
            ctx.Refresh();
            started.TrySetResult();
            await stopSignal.Task;
        });
        return started.Task;
    }

    public Task PublishAsync(CancellationToken cancellationToken = default)
    {
        if (machine is null || liveContext is null) return Task.CompletedTask;

        var now = DateTime.UtcNow;
        if (now - lastRefresh < refreshInterval) return Task.CompletedTask;
        lastRefresh = now;

        for (int i = 0; i < rows.Count; i++)
            table.UpdateCell(i, 1, FormatValue(rows[i].Value));
        liveContext.Refresh();
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        machine = null;
        if (liveTask is null) return;
        stopSignal?.TrySetResult();
        await liveTask;
        liveTask = null;
        liveContext = null;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private static string FormatValue(object? value) => value switch
    {
        null => "-",
        double d => d.ToString("F3"),
        Array array => string.Join(", ", array.Cast<object>().Select(FormatValue)),
        _ => value.ToString() ?? "-"
    };
}
