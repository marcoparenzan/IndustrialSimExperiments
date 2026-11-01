using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

public static partial class Lessons
{
    // Lesson 08 solution: the range uses zero-based indices, inclusive bounds.
    private static void RunRanges(Session session)
    {
        var request = new ReadValueId
        {
            NodeId = Id(session, "Reactor1.Profile"),
            AttributeId = Attributes.Value,
            IndexRange = "1:2"
        };
        session.Read(null, 0, TimestampsToReturn.Both,
            new ReadValueIdCollection { request }, out var values, out _);
        Require(StatusCode.IsGood(values[0].StatusCode), "Valid IndexRange read");
        Require(values[0].Value is double[] { Length: 2 } slice && slice.SequenceEqual(new double[] { 40, 60 }),
            "Profile[1:2] = [40, 60]");
        request.IndexRange = "99:100";
        session.Read(null, 0, TimestampsToReturn.Both,
            new ReadValueIdCollection { request }, out values, out _);
        Require(values[0].StatusCode == StatusCodes.BadIndexRangeNoData, "Range beyond the data is rejected");
    }
}
