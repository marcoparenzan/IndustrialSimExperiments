using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Laboratorio write: il dispatch e gli helper condivisi sono in Lessons.cs.
public static partial class Lessons
{
    private static void RunWrite(Session session)
    {
        NodeId N(string name) => Id(session, name);
        Require(StatusCode.IsGood(Write(session, N("Reactor1.Setpoint"), 65.0)), "Setpoint scritto");
        Require((double)session.ReadValue(N("Reactor1.Setpoint")).Value == 65, "Readback setpoint");
    }
}

