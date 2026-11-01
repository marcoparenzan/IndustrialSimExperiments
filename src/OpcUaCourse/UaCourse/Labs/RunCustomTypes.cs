using Opc.Ua;
using Opc.Ua.Client;
using UaCourse.Models;

namespace UaCourse;

public static partial class Lessons
{
    private static async Task RunCustomTypesAsync(Session session)
    {
        var node = Id(session, "Reactor1.BatchRecipe");
        var type = Id(session, "RecipeDataType");
        session.Read(null, 0, TimestampsToReturn.Neither,
            new ReadValueIdCollection { new() { NodeId = type, AttributeId = Attributes.DataTypeDefinition } },
            out var definitions, out _);
        Require(StatusCode.IsGood(definitions[0].StatusCode), "DataTypeDefinition readable");
        var definition = ExtensionObject.ToEncodeable(definitions[0].Value as ExtensionObject) as StructureDefinition;
        Require(definition is not null && definition.Fields.Select(f => f.Name)
            .SequenceEqual(new[] { "Name", "Revision", "Target", "Steps" }), "StructureDefinition fields consistent");
        Require(definition!.DefaultEncodingId == Id(session, "RecipeDataType.Binary"), "Binary encoding distinct from the DataType");
        session.Browse(null, null, type, 0, BrowseDirection.Forward, ReferenceTypeIds.HasEncoding,
            false, 0, out _, out var encodings);
        Require(encodings.Any(e => ExpandedNodeId.ToNodeId(e.NodeId, session.NamespaceUris) == definition.DefaultEncodingId),
            "HasEncoding navigable");
        var previous = DecodeRecipe(session.ReadValue(node).Value);
        using var subscription = new Subscription(session.DefaultSubscription) { PublishingInterval = 100 };
        session.AddSubscription(subscription); subscription.Create();
        var item = new MonitoredItem(subscription.DefaultItem)
        { StartNodeId = node, AttributeId = Attributes.Value, QueueSize = 10, SamplingInterval = 100, DiscardOldest = true };
        var received = new TaskCompletionSource<RecipeData>(TaskCreationOptions.RunContinuationsAsynchronously);
        var sent = new RecipeData { Name = "Lab09", Revision = 42, Target = 66, Steps = [20, 35, 50, 66] };
        item.Notification += (_, _) =>
        {
            foreach (var value in item.DequeueValues())
                if (StatusCode.IsGood(value.StatusCode) &&
                    ExtensionObject.ToEncodeable(value.Value as ExtensionObject) is RecipeData recipe &&
                    recipe.Name == sent.Name && recipe.Revision == sent.Revision) received.TrySetResult(recipe);
        };
        try
        {
            subscription.AddItem(item); subscription.ApplyChanges();
            Require(!ServiceResult.IsBad(item.Status.Error), "Structure subscription created");
            var output = session.Call(Id(session, "Reactor1"), Id(session, "Reactor1.LoadRecipe"), new ExtensionObject(sent));
            Require((uint)output[0] == sent.Revision, "Method receives the structure and returns the revision");
            Require(sent.IsEqual(DecodeRecipe(session.ReadValue(node).Value)), "Readback of all custom fields");
            var notified = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Require(sent.IsEqual(notified), "Publish of the custom structure");
            var invalid = (RecipeData)sent.Clone(); invalid.Steps = [20, double.NaN];
            ExpectStatus(() => session.Call(Id(session, "Reactor1"), Id(session, "Reactor1.LoadRecipe"),
                new ExtensionObject(invalid)), StatusCodes.BadOutOfRange);
            Require(sent.IsEqual(DecodeRecipe(session.ReadValue(node).Value)), "Invalid recipe does not change the state");
            Console.WriteLine(notified);
        }
        finally
        {
            subscription.Delete(true); session.RemoveSubscription(subscription);
            session.Call(Id(session, "Reactor1"), Id(session, "Reactor1.LoadRecipe"), new ExtensionObject(previous));
        }
    }
    private static RecipeData DecodeRecipe(object value) =>
        ExtensionObject.ToEncodeable(value as ExtensionObject) as RecipeData ??
        throw new InvalidOperationException("RecipeData not decoded");
}
