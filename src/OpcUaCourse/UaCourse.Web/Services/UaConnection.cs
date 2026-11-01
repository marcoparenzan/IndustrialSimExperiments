using System.Collections.Concurrent;
using Opc.Ua;
using Opc.Ua.Client;
using UaCourse.Web.Modules;

namespace UaCourse.Web.Services;

public record SignalValue(object? Value, StatusCode Quality, DateTime SourceTimestamp);

// One shared connection for the lab; components do not own sessions.
public sealed class UaConnection(ScreenCatalog catalog, ILogger<UaConnection> logger) : BackgroundService
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly ConcurrentDictionary<string, SignalValue> signals = new();
    private readonly ConcurrentQueue<string> events = new();
    private Session? session;
    public bool Connected { get; private set; }
    public string Status { get; private set; } = "Connecting";
    public IReadOnlyList<string> Events => events.ToArray();
    public SignalValue? Get(string node) => signals.GetValueOrDefault(node);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Session? current = null;
            Subscription? subscription = null;
            try
            {
                var app = await Configuration.CreateAsync(false);
                var endpoint = CoreClientUtils.SelectEndpoint(app.ApplicationConfiguration, Configuration.Endpoint, true, 5000);
                current = await Session.Create(app.ApplicationConfiguration,
                    new ConfiguredEndpoint(null, endpoint, EndpointConfiguration.Create(app.ApplicationConfiguration)),
                    false, "UaCourse.Blazor", 60_000, new UserIdentity(new AnonymousIdentityToken()), null);
                var lost = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                current.KeepAlive += (_, e) => { if (ServiceResult.IsBad(e.Status)) lost.TrySetResult(); };
                current.KeepAliveInterval = 2000;
                subscription = new Subscription(current.DefaultSubscription) { PublishingInterval = 250, KeepAliveCount = 10, LifetimeCount = 100 };
                current.AddSubscription(subscription); subscription.Create();
                foreach (var node in catalog.Screens.SelectMany(s => s.Labels).Select(l => l.Node).Distinct())
                {
                    var item = new MonitoredItem(subscription.DefaultItem)
                    { StartNodeId = Lessons.Id(current, node), DisplayName = node, AttributeId = Attributes.Value, SamplingInterval = 250, QueueSize = 10, DiscardOldest = true };
                    item.Notification += (_, _) =>
                    {
                        foreach (var value in item.DequeueValues()) signals[node] = new(value.Value, value.StatusCode, value.SourceTimestamp);
                    };
                    subscription.AddItem(item);
                }
                var eventItem = new MonitoredItem(subscription.DefaultItem)
                { StartNodeId = Lessons.Id(current, "Reactor1"), AttributeId = Attributes.EventNotifier, Filter = Lessons.EventFilter(), QueueSize = 50, DiscardOldest = true };
                eventItem.Notification += (sender, e) =>
                {
                    if (e.NotificationValue is not EventFieldList fields) return;
                    events.Enqueue(string.Join(" · ", fields.EventFields.Skip(1)));
                    while (events.Count > 50) events.TryDequeue(out _);
                };
                subscription.AddItem(eventItem); subscription.ApplyChanges();
                foreach (var item in subscription.MonitoredItems)
                    if (ServiceResult.IsBad(item.Status.Error)) throw new ServiceResultException(item.Status.Error);
                await gate.WaitAsync(stoppingToken);
                try { session = current; Connected = true; Status = "OPC UA · SignAndEncrypt · subscription 250 ms"; }
                finally { gate.Release(); }
                await lost.Task.WaitAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { logger.LogWarning(ex, "OPC UA connection lost"); Status = "Server unavailable · retrying in 3 s"; }
            finally
            {
                await gate.WaitAsync(CancellationToken.None);
                try
                {
                    Connected = false; session = null; signals.Clear();
                    try { subscription?.Delete(true); } catch (Exception ex) { logger.LogDebug(ex, "Closing subscription"); }
                    subscription?.Dispose();
                    try { current?.Close(); } catch (Exception ex) { logger.LogDebug(ex, "Closing session"); }
                    current?.Dispose();
                }
                finally { gate.Release(); }
            }
            try { await Task.Delay(3000, stoppingToken); } catch (OperationCanceledException) { break; }
        }
    }
    public Task<string> CallAsync(string method, params object[] arguments) => WithSessionAsync(s =>
    {
        var output = s.Call(Lessons.Id(s, "Reactor1"), Lessons.Id(s, $"Reactor1.{method}"), arguments);
        return output.Count == 0 ? $"{method}: completed" : $"{method}: {string.Join(", ", output)}";
    });
    public Task<string> WriteAsync(double value) => WithSessionAsync(s =>
    {
        var status = Lessons.Write(s, Lessons.Id(s, "Reactor1.Setpoint"), value);
        if (StatusCode.IsBad(status)) throw new ServiceResultException(status);
        return $"Setpoint written: {value:F1} °C";
    });
    public Task<string> RecipeAsync() => WithSessionAsync(s =>
    {
        var value = s.ReadValue(Lessons.Id(s, "Reactor1.Recipe"));
        if (StatusCode.IsBad(value.StatusCode)) throw new ServiceResultException(value.StatusCode);
        var range = ExtensionObject.ToEncodeable(value.Value as ExtensionObject) as Opc.Ua.Range
            ?? throw new InvalidOperationException("UA Range not decoded");
        var profile = s.ReadValue(Lessons.Id(s, "Reactor1.Profile"));
        if (StatusCode.IsBad(profile.StatusCode)) throw new ServiceResultException(profile.StatusCode);
        return $"Range UA: Low={range.Low}, High={range.High}; Profile: [{string.Join(", ", (double[])profile.Value)}]";
    });
    public Task<string> BatchRecipeAsync() => WithSessionAsync(s =>
    {
        var value = s.ReadValue(Lessons.Id(s, "Reactor1.BatchRecipe"));
        var recipe = ExtensionObject.ToEncodeable(value.Value as ExtensionObject) as UaCourse.Models.RecipeData
            ?? throw new InvalidOperationException("RecipeData not decoded");
        return $"RecipeDataType: {recipe}";
    });
    public Task<string> LoadBatchRecipeAsync(string name, uint revision, double target) => WithSessionAsync(s =>
    {
        var recipe = new UaCourse.Models.RecipeData { Name = name, Revision = revision, Target = target, Steps = [20, 40, target] };
        var output = s.Call(Lessons.Id(s, "Reactor1"), Lessons.Id(s, "Reactor1.LoadRecipe"), new ExtensionObject(recipe));
        return $"LoadRecipe: revision accepted {output[0]}";
    });
    private async Task<string> WithSessionAsync(Func<Session, string> operation)
    {
        await gate.WaitAsync();
        try
        {
            var active = session ?? throw new InvalidOperationException("OPC UA session not connected");
            return await Task.Run(() => operation(active));
        }
        finally { gate.Release(); }
    }
}
