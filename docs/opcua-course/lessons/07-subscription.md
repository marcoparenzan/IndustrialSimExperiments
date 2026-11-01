# Subscriptions, MonitoredItems, Publish and deadband

## Concepts

SamplingInterval concerns the measurement or sampling of the node; PublishingInterval the pace of the Publish responses. QueueSize limits the pending notifications per item. DiscardOldest decides which sample to sacrifice when the queue is full. The server can revise the requested parameters. The SDK client keeps Publish requests outstanding to receive notifications.

## Experiment

Run client subscribe. The lab sets sampling 100 ms, publishing 250 ms and an absolute deadband of 0.1. The first notification can arrive even without a significant change; by itself it does not prove that the filter suppressed other samples.

## In the C# code

ObserveAsync creates the subscription, creates the item, applies the changes and checks item.Status.Error. ClearChangeMasks in the server makes the changes available to the SDK. UaConnection uses several items in the same subscription.

## Exercise and verification

Compare requested and RevisedSamplingInterval/RevisedQueueSize. Increase the simulated variation, slow down Publish and observe the loss with small queues. Measure latency using the source timestamp.

## Mistake to avoid

A subscription does not guarantee that every sample of the source is received. A percentage deadband requires an EURange; the sample uses an absolute deadband.

## Goals and lab

Estimated time: 120 minutes. Open [ObserveNotifications.cs](../../../src/OpcUaCourse/UaCourse/Labs/ObserveNotifications.cs). Run client subscribe and then watch the longer flow of the web app. The console command ends at the first valid notification: it is a smoke test, not an exhaustive measurement of the filter.

## The three rates of the sample

| Activity | Interval | Meaning |
|---|---|---|
| Simulator timer | 500 ms | New value in the server |
| Console sampling requested | 100 ms | The item checking the node |
| Publishing requested | 250 ms | Pace of the notification responses |

Sampling every 100 ms does not generate five independent measurements if the source changes every 500 ms. The web app adds a rendering timer every 500 ms, which reads a local cache: it performs no periodic UA Reads.

## Guided construction

```csharp
using var subscription = new Subscription(session.DefaultSubscription)
{
    PublishingInterval = 250
};
session.AddSubscription(subscription);
subscription.Create();

var item = new MonitoredItem(subscription.DefaultItem)
{
    StartNodeId = Lessons.Id(session, "Reactor1.Temperature"),
    AttributeId = Attributes.Value,
    SamplingInterval = 100,
    QueueSize = 20,
    DiscardOldest = true
};
subscription.AddItem(item);
subscription.ApplyChanges();
```

Subscription.Create creates the server resource; AddItem changes the client description; ApplyChanges makes the item changes effective. Do not assume that constructing a C# object is equivalent to automatically creating a remote MonitoredItem.

Check item.Status.Error after the creation. The parameters can be revised: measure the effective ones from the SDK state, not just those assigned to the object before the request.

## Filters and quality

The console uses DataChangeTrigger.StatusValue and an absolute deadband of 0.1. The filter considers changes of status and value according to the trigger; it does not use the timestamp as an autonomous event. An absolute deadband has the unit of the value: 0.1 means 0.1 °C for this Temperature.

A percentage deadband requires an appropriate scale declared with EURange. EngineeringUnits alone does not define that scale. The first value of the item is normally notified at startup: receiving it does not prove that other values were filtered.

## Queues, loss and Publish

QueueSize belongs to the MonitoredItem. DiscardOldest=true keeps the most recent samples when the queue is full; false changes which sample is discarded. The queue of the item is not the same thing as the retransmission buffer of the messages of the subscription.

The client keeps Publish requests outstanding; the server uses them to deliver notifications or keep-alives. Republish concerns messages still available in the buffer of the subscription and does not recover a sample that was discarded before being included in a message.

The callback of the sample uses DequeueValues and moves the data into the snapshot. Do not call slow remote services inside every callback: it would slow down consumption and complicate load control.

## Solved exercise: live data or every sample?

You want to display a live level and archive every measurement for audit. Can a single label with queue=1 satisfy both?

**Solution.** For the live view mainly the current value matters; for the archive, completeness matters. Define an acquisition/archiving path with measurable loss, adequate capacity and possibly historical recovery. The renderer can consume a reduced snapshot without determining what the archive keeps.

**Test.** In a modified lab keep the subscription for 30 s, compare values with deadband 0 and 0.1, print the revised parameters and the number of notifications. Do not expect exact counts on the wall clock: scheduler and publishing produce variations. Verify instead the order and the qualitative behavior.

References: [MonitoredItem Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.13), [Subscription Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.14).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
