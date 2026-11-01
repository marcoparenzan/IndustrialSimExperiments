using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

public static partial class Lessons
{
    private static async Task ObserveAsync(Session session, bool events)
    {
        using var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 250 };
        session.AddSubscription(subscription); subscription.Create();
        var received = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = new MonitoredItem(subscription.DefaultItem)
        {
            StartNodeId = Id(session, events ? "Reactor1" : "Reactor1.Temperature"),
            AttributeId = events ? Attributes.EventNotifier : Attributes.Value,
            SamplingInterval = 100, QueueSize = 20, DiscardOldest = true,
            Filter = events ? EventFilter() : new DataChangeFilter { Trigger = DataChangeTrigger.StatusValue, DeadbandType = (uint)DeadbandType.Absolute, DeadbandValue = 0.1 }
        };
        item.Notification += (_, e) =>
        {
            if (events && e.NotificationValue is EventFieldList fields)
            { Console.WriteLine(string.Join(" | ", fields.EventFields)); received.TrySetResult(); }
            else if (!events)
                foreach (var value in item.DequeueValues())
                { Console.WriteLine($"Notification: {value.Value} {value.StatusCode}"); received.TrySetResult(); }
        };
        try
        {
            subscription.AddItem(item); subscription.ApplyChanges();
            Require(!ServiceResult.IsBad(item.Status.Error), "MonitoredItem creato");
            if (events) session.Call(Id(session, "Reactor1"), Id(session, "Reactor1.ApplyRecipe"), 68.0);
            await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Console.WriteLine("Notifica ricevuta attraverso Publish.");
        }
        finally { subscription.Delete(true); session.RemoveSubscription(subscription); }
    }
}

