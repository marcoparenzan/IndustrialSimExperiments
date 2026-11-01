using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

public static partial class Lessons
{
    // Soluzione della lezione 08: il range usa indici zero-based, estremi inclusivi.
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
        Require(StatusCode.IsGood(values[0].StatusCode), "Read IndexRange valido");
        Require(values[0].Value is double[] { Length: 2 } slice && slice.SequenceEqual(new double[] { 40, 60 }),
            "Profile[1:2] = [40, 60]");
        request.IndexRange = "99:100";
        session.Read(null, 0, TimestampsToReturn.Both,
            new ReadValueIdCollection { request }, out values, out _);
        Require(values[0].StatusCode == StatusCodes.BadIndexRangeNoData, "Range oltre i dati rifiutato");
    }
}
