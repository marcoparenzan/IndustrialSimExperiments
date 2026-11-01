using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

public static partial class Lessons
{
    // Partial solution of lesson 05: checks the contract actually published.
    private static void RunModel(Session session)
    {
        session.Browse(null, null, Id(session, "Reactor1"), 0, BrowseDirection.Forward,
            ReferenceTypeIds.HasTypeDefinition, false, 0, out _, out var types);
        Require(types.Any(r => ExpandedNodeId.ToNodeId(r.NodeId, session.NamespaceUris) == Id(session, "ReactorType")),
            "Reactor1 has type ReactorType");
        session.Browse(null, null, Id(session, "ReactorType.Temperature"), 0, BrowseDirection.Forward,
            ReferenceTypeIds.HasModellingRule, false, 0, out _, out var rules);
        Require(rules.Any(r => ExpandedNodeId.ToNodeId(r.NodeId, session.NamespaceUris) == ObjectIds.ModellingRule_Mandatory),
            "Temperature is declared Mandatory");
        var reads = new ReadValueIdCollection
        {
            new() { NodeId = Id(session, "ReactorType.Temperature"), AttributeId = Attributes.DataType },
            new() { NodeId = Id(session, "Reactor1.Temperature"), AttributeId = Attributes.DataType }
        };
        session.Read(null, 0, TimestampsToReturn.Neither, reads, out var values, out _);
        Require(values.All(v => StatusCode.IsGood(v.StatusCode) && v.Value is NodeId id && id == DataTypeIds.Double),
            "Declaration and instance have DataType Double");
    }
}
