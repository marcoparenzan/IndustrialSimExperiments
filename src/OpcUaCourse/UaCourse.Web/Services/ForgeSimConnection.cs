using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;
using UaCourse.Web.Modules;

namespace UaCourse.Web.Services;

// Read-only connection to the OPC UA server of ForgeSimApp (see lesson 19). It has its own session, endpoint
// (SecurityMode None) and namespace; labels are tag paths such as "Segments.Segment0.Vfd.BusVoltage", resolved by browse path.
public sealed class ForgeSimConnection(ScreenCatalog catalog, IConfiguration configuration, ILogger<ForgeSimConnection> logger)
    : BackgroundService, ISignalSource
{
    private readonly ConcurrentDictionary<string, SignalValue> signals = new();
    public bool Connected { get; private set; }
    public string Status { get; private set; } = "Connecting to ForgeSimApp";
    public string Endpoint => configuration["ForgeSim:Endpoint"] ?? Lessons.ForgeSimEndpoint;
    public SignalValue? Get(string node) => signals.GetValueOrDefault(node);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var nodes = catalog.Screens.Where(s => s.Source == ScreenSources.ForgeSim)
            .SelectMany(s => s.Labels).Select(l => l.Node).Distinct().ToList();
        if (nodes.Count == 0) { Status = "No ForgeSim screens"; return; }
        while (!stoppingToken.IsCancellationRequested)
        {
            Session? current = null;
            Subscription? subscription = null;
            try
            {
                var app = await Configuration.CreateAsync(false);
                var endpoint = CoreClientUtils.SelectEndpoint(app.ApplicationConfiguration, Endpoint, false, 5000);
                current = await Session.Create(app.ApplicationConfiguration,
                    new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(app.ApplicationConfiguration)),
                    false, "UaCourse.Blazor.ForgeSim", 60_000, new UserIdentity(new AnonymousIdentityToken()), null);
                var lost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                current.KeepAlive += (_, e) => { if (ServiceResult.IsBad(e.Status)) lost.TrySetResult(); };
                current.KeepAliveInterval = 2000;
                var (ns, machine) = Lessons.FindForgeSimMachine(current);
                subscription = new Subscription(current.DefaultSubscription) { PublishingInterval = 250, KeepAliveCount = 10, LifetimeCount = 100 };
                current.AddSubscription(subscription); subscription.Create();
                foreach (var node in nodes)
                {
                    NodeId id;
                    try { id = Lessons.ResolveForgeSimPath(current, machine, ns, node); }
                    catch (InvalidOperationException ex)
                    {
                        // e.g. Machine.ActiveTrip exists only in the Python conveyor: the label stays at "—".
                        logger.LogWarning("ForgeSim tag not available: {Node} ({Reason})", node, ex.Message);
                        continue;
                    }
                    var item = new MonitoredItem(subscription.DefaultItem)
                    { StartNodeId = id, DisplayName = node, AttributeId = Attributes.Value, SamplingInterval = 250, QueueSize = 10, DiscardOldest = true };
                    var captured = node;
                    item.Notification += (_, _) =>
                    {
                        foreach (var value in item.DequeueValues()) signals[captured] = new(value.Value, value.StatusCode, value.SourceTimestamp);
                    };
                    subscription.AddItem(item);
                }
                subscription.ApplyChanges();
                foreach (var item in subscription.MonitoredItems)
                    if (ServiceResult.IsBad(item.Status.Error)) throw new ServiceResultException(item.Status.Error);
                Connected = true;
                Status = $"ForgeSimApp · {Endpoint} · SecurityMode None · read-only";
                await lost.Task.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ForgeSimApp connection lost");
                Status = "ForgeSimApp unavailable (start it with --protocols OpcUa) · retrying in 3 s";
            }
            finally
            {
                Connected = false; signals.Clear();
                try { subscription?.Delete(true); } catch (Exception ex) { logger.LogDebug(ex, "Closing subscription"); }
                subscription?.Dispose();
                try { current?.Close(); } catch (Exception ex) { logger.LogDebug(ex, "Closing session"); }
                current?.Dispose();
            }
            try { await Task.Delay(3000, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
}
