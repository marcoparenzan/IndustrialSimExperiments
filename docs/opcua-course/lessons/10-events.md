# EventType, EventNotifier and filters

## Concepts

Events are not changes of a Variable. An EventType defines fields and a hierarchy; a node with an EventNotifier enables subscription. EventFilter.SelectClauses chooses the fields returned; a WhereClause can restrict the events. EventId identifies the occurrence and SourceNode the source.

## Experiment

Run client events: the client subscribes to Reactor1 and ApplyRecipe produces an event. The server also generates periodic events. The browser keeps the last 50 and shows the last eight.

## In the C# code

Emit creates a BaseEventState, sets message, severity and source and calls reactor.ReportEvent. Lessons.EventFilter requests EventId, Time, SourceName, Message and Severity. The position of the fields in the EventFieldList follows the SelectClauses.

## Exercise and verification

Add a Severity >= 200 filter with ContentFilter and OfType. Then define a RecipeAppliedEventType with RecipeName and Target. Also keep the EventId for correlation and deduplication.

## Mistake to avoid

A message with a high severity does not automatically become an alarm. An event queue can lose items; the sample does not persist the log.

## Goals and lab

Estimated time: 90 minutes. Use client events, [ObserveNotifications.cs](../../../src/OpcUaCourse/UaCourse/Labs/ObserveNotifications.cs), Lessons.EventFilter and PlantNodeManager.Emit. The sample publishes BaseEventType, not yet a custom application type.

## From server event to client fields

The source is Reactor1, which declares EventNotifier.SubscribeToEvents and is registered as a root notifier. The server creates a BaseEventState, sets the fields and calls ReportEvent. An event must not be looked for as a new Variable node at every occurrence.

On the client the MonitoredItem uses AttributeId=EventNotifier. It does not use Attributes.Value. The configuration of the filter determines the array delivered in the EventFieldList.

```csharp
var filter = Lessons.EventFilter();
for (var i = 0; i < filter.SelectClauses.Count; i++)
    Console.WriteLine($"{i}: {filter.SelectClauses[i].BrowsePath}");
```

For the current filter the order is:

| Index | Field | Use |
|---|---|---|
| 0 | EventId | Identity of the occurrence |
| 1 | Time | Time of the event |
| 2 | SourceName | Readable source |
| 3 | Message | LocalizedText for presentation |
| 4 | Severity | UA numeric priority |

EventId is a ByteString: for a stable display use Base64 or hexadecimal, not the generic ToString of byte[]. The EventFieldList returns Variants in the same order as the SelectClauses; do not look for the properties by name as if it were a JSON record.

## Selecting and filtering are different operations

SelectClauses decides which fields to receive. WhereClause decides which events pass. Reducing the columns is not the same as reducing the number of events. To filter a family of types use OfType; for a severity use the comparison on the field.

Before creating a complex filter, verify that the server supports the operators needed and check the result of the creation of the item, including the diagnostics of the filter. An invalid filter does not automatically become a subscription without a filter.

For nested fields, BrowsePath is a sequence of QualifiedNames starting from the type definition. For example the Id field of a two-state variable requires a different path from its readable text.

## Guided lab and expectations

1. Start client events: the item is created and ApplyRecipe(68) is invoked.
2. Wait for an EventFieldList with the recipe message and severity 300.
3. Keep the web app open: you also receive the periodic events with severity 100.
4. Stop the reactor: the server emits the stop message.
5. Observe that the UI list is limited: it keeps 50 strings and shows the last eight events.

The console command can receive a periodic event before the application one; its smoke test proves the event channel, not the exact correlation with a command. For a correlation test add a CommandId to an application event type and verify the value received.

## Solved exercise: avoiding duplicates without losing meaning

During recovery you receive again an event that was already recorded. Can you deduplicate it by Message and Time?

**Solution.** It is not reliable: two distinct events can have the same text and a rounded timestamp. Keep the EventId together with the identity of the source/server and the context needed. The current web service discards the EventId in the presentation string: for a reliable table replace the strings with typed event records.

**Project.** RecipeAppliedEventType adds RecipeName, Target and CommandId. Define the type, publish the fields and update the SelectClauses. Validate with a client that selects both BaseEventType fields and the custom ones.

References: [Event model, Part 5](https://reference.opcfoundation.org/Core/Part5/), [MonitoredItem Services](https://reference.opcfoundation.org/Core/Part4/v105/docs/5.13).

[Index](README.md) · [Using the samples](../USAGE.md) · [Architecture](../ARCHITECTURE.md)
