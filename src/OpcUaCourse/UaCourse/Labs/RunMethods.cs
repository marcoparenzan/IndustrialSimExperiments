using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Methods lab: dispatch and shared helpers live in Lessons.cs.
public static partial class Lessons
{
    private static void RunMethods(Session session)
    {
        NodeId N(string name) => Id(session, name);
        session.Call(N("Reactor1"), N("Reactor1.Stop"));
        var result = session.Call(N("Reactor1"), N("Reactor1.ApplyRecipe"), 70.0);
        Require((double)result[0] == 70, "ApplyRecipe output");
        Require((bool)session.Call(N("Reactor1"), N("Reactor1.Start"))[0], "Start output");
    }
}

