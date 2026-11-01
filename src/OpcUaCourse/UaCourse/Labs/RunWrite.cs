using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Write lab: dispatch and shared helpers live in Lessons.cs.
public static partial class Lessons
{
    private static void RunWrite(Session session)
    {
        NodeId N(string name) => Id(session, name);
        Require(StatusCode.IsGood(Write(session, N("Reactor1.Setpoint"), 65.0)), "Setpoint written");
        Require((double)session.ReadValue(N("Reactor1.Setpoint")).Value == 65, "Setpoint readback");
    }
}

