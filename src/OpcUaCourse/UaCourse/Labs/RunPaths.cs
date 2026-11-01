using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Paths lab: dispatch and shared helpers live in Lessons.cs.
public static partial class Lessons
{
    private static void RunPaths(Session session)
    {
        NodeId N(string name) => Id(session, name);
        session.Browse(null, null, N("Plant"), 0, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true, 0, out _, out var plantChildren);
        foreach (var child in plantChildren) Console.WriteLine($"Plant child: {child.BrowseName} ref={child.ReferenceTypeId}");
        var path = new BrowsePath
        {
            StartingNode = N("Plant"), RelativePath = new RelativePath
            {
                Elements = new RelativePathElementCollection
                {
                    new() { ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences, IncludeSubtypes = true, IsInverse = false, TargetName = new QualifiedName("Reactor1", N("Plant").NamespaceIndex) },
                    new() { ReferenceTypeId = ReferenceTypeIds.HierarchicalReferences, IncludeSubtypes = true, IsInverse = false, TargetName = new QualifiedName("Temperature", N("Plant").NamespaceIndex) }
                }
            }
        };
        session.TranslateBrowsePathsToNodeIds(null, new BrowsePathCollection { path }, out var targets, out _);
        Console.WriteLine($"Translate: {targets[0].StatusCode}, targets={targets[0].Targets.Count}");
        Require(StatusCode.IsGood(targets[0].StatusCode) && targets[0].Targets.Count == 1, "Browse path resolved");
        Console.WriteLine(targets[0].Targets[0].TargetId);
    }
}

