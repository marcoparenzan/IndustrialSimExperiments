using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Errors lab: dispatch and shared helpers live in Lessons.cs.
public static partial class Lessons
{
    private static void RunErrors(Session session)
    {
        NodeId N(string name) => Id(session, name);
        Require(Write(session, N("Reactor1.Setpoint"), 999.0) == StatusCodes.BadOutOfRange, "BadOutOfRange");
        Require(Write(session, N("Reactor1.Setpoint"), "wrong") == StatusCodes.BadTypeMismatch, "BadTypeMismatch");
        Require(Write(session, N("Reactor1.Temperature"), 50.0) == StatusCodes.BadNotWritable, "BadNotWritable");
        try { session.ReadValue(N("Missing")); throw new InvalidOperationException("Read of a nonexistent node succeeded"); }
        catch (ServiceResultException ex) when (ex.StatusCode == StatusCodes.BadNodeIdUnknown)
        { Console.WriteLine("OK: BadNodeIdUnknown"); }
    }
}

