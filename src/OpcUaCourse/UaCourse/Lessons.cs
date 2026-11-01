using Opc.Ua;
using Opc.Ua.Client;
namespace UaCourse;

public static partial class Lessons
{
    public const string NamespaceUri = "urn:uacourse:plant";
    public static NodeId Id(Session session, string name)
    {
        var index = session.NamespaceUris.GetIndex(NamespaceUri);
        if (index < 0) throw new InvalidOperationException($"Namespace {NamespaceUri} assente.");
        return new NodeId(name, (ushort)index);
    }
    public static async Task RunAsync(Session session, string lesson)
    {
        string[] all = ["browse", "paths", "read", "write", "types", "custom-types", "ranges", "model", "methods", "method-errors", "errors", "subscribe", "events"];
        if (lesson == "all") { foreach (var name in all) await RunAsync(session, name); return; }
        Console.WriteLine($"\n--- {lesson} ---");
        switch (lesson)
        {
            case "browse": RunBrowse(session); break;
            case "paths": RunPaths(session); break;
            case "read": RunRead(session); break;
            case "write": RunWrite(session); break;
            case "types": RunTypes(session); break;
            case "custom-types": await RunCustomTypesAsync(session); break;
            case "methods": RunMethods(session); break;
            case "errors": RunErrors(session); break;
            case "ranges": RunRanges(session); break;
            case "model": RunModel(session); break;
            case "method-errors": RunMethodErrors(session); break;
            case "subscribe": await ObserveAsync(session, false); break;
            case "events": await ObserveAsync(session, true); break;
            default: throw new ArgumentException($"Lezione sconosciuta: {lesson}");
        }
    }
    public static StatusCode Write(Session session, NodeId node, object value)
    {
        session.Write(null, new WriteValueCollection { new() { NodeId = node, AttributeId = Attributes.Value, Value = new DataValue(new Variant(value)) } }, out var results, out _);
        return results[0];
    }
    public static EventFilter EventFilter()
    {
        var filter = new EventFilter();
        foreach (var field in new[] { BrowseNames.EventId, BrowseNames.Time, BrowseNames.SourceName, BrowseNames.Message, BrowseNames.Severity })
            filter.SelectClauses.Add(new SimpleAttributeOperand
            { TypeDefinitionId = ObjectTypeIds.BaseEventType, BrowsePath = new QualifiedNameCollection { new(field) }, AttributeId = Attributes.Value });
        return filter;
    }
    private static void Require(bool condition, string description)
    { if (!condition) throw new InvalidOperationException(description); Console.WriteLine($"OK: {description}"); }
}
