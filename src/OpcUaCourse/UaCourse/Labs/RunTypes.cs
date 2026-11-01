using Opc.Ua;
using Opc.Ua.Client;

namespace UaCourse;

// Types lab: dispatch and shared helpers live in Lessons.cs.
public static partial class Lessons
{
    private static void RunTypes(Session session)
    {
        NodeId N(string name) => Id(session, name);
        var recipe = session.ReadValue(N("Reactor1.Recipe"));
        Require(StatusCode.IsGood(recipe.StatusCode), "Structure readable");
        var body = ExtensionObject.ToEncodeable(recipe.Value as ExtensionObject) as Opc.Ua.Range;
        Require(body is not null && body.Low == 20 && body.High == 90, "Range decoded");
        Console.WriteLine($"ExtensionObject Range: {body!.Low}..{body.High}");
        Require(session.ReadValue(N("Reactor1.Profile")).Value is double[] { Length: 3 }, "Array UA");
        var units = session.ReadValue(N("Reactor1.Temperature.EngineeringUnits"));
        Console.WriteLine($"EngineeringUnits: {units.Value}");
    }
}

