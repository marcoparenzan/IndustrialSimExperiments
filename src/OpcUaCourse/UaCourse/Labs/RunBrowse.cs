using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Laboratorio browse: il dispatch e gli helper condivisi sono in Lessons.cs.
public static partial class Lessons
{
    private static void RunBrowse(Session session)
    {
        NodeId N(string name) => Id(session, name);
        session.Browse(null, null, N("Reactor1"), 2, BrowseDirection.Forward,
            ReferenceTypeIds.HierarchicalReferences, true, 0, out var cp, out var references);
        while (true)
        {
            foreach (var reference in references) Console.WriteLine($"{reference.NodeClass}: {reference.BrowseName}, {reference.NodeId}");
            if (cp is null || cp.Length == 0) break;
            session.BrowseNext(null, false, cp, out cp, out references);
        }
    }
}

