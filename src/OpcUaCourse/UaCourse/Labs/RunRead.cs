using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Laboratorio read: il dispatch e gli helper condivisi sono in Lessons.cs.
public static partial class Lessons
{
    private static void RunRead(Session session)
    {
        NodeId N(string name) => Id(session, name);
        var reads = new ReadValueIdCollection();
        foreach (var attribute in new[] { Attributes.Value, Attributes.DataType, Attributes.ValueRank, Attributes.AccessLevel })
            reads.Add(new ReadValueId { NodeId = N("Reactor1.Temperature"), AttributeId = attribute });
        session.Read(null, 0, TimestampsToReturn.Both, reads, out var values, out _);
        foreach (var value in values) { Require(StatusCode.IsGood(value.StatusCode), "Read valido"); Console.WriteLine($"{value.Value}, {value.StatusCode}, source={value.SourceTimestamp:O}, server={value.ServerTimestamp:O}"); }
    }
}

