using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

public static partial class Lessons
{
    // Soluzione lezione 06: precondizioni e integrità del valore dopo un comando rifiutato.
    private static void RunMethodErrors(Session session)
    {
        var reactor = Id(session, "Reactor1");
        session.Call(reactor, Id(session, "Reactor1.Stop"));
        try
        {
            session.Call(reactor, Id(session, "Reactor1.Start"));
            ExpectStatus(() => session.Call(reactor, Id(session, "Reactor1.Start")), StatusCodes.BadInvalidState);
            session.Call(reactor, Id(session, "Reactor1.ApplyRecipe"), 64.0);
            ExpectStatus(() => session.Call(reactor, Id(session, "Reactor1.ApplyRecipe"), 999.0), StatusCodes.BadOutOfRange);
            Require((double)session.ReadValue(Id(session, "Reactor1.Setpoint")).Value == 64.0,
                "Call rifiutata non modifica il setpoint");
            Require((bool)session.ReadValue(Id(session, "Reactor1.Running")).Value,
                "Secondo Start rifiutato conserva lo stato");
        }
        finally { session.Call(reactor, Id(session, "Reactor1.Stop")); }
    }
    private static void ExpectStatus(Action operation, uint expected)
    {
        try { operation(); }
        catch (ServiceResultException ex) when (ex.StatusCode == expected)
        { Console.WriteLine($"OK: {new StatusCode(expected)}"); return; }
        throw new InvalidOperationException($"Atteso {new StatusCode(expected)}");
    }
}
