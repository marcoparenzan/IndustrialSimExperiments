using ConveyorSimLib;
using IndustrialSimLib;

namespace IndustrialSim.Tests;

/// <summary>The C# ConveyorMachine and the Python ConveyorPySharpMachine implement the same physics
/// (see EXTENDING.md, "Parity rule"). These tests fail as soon as the two drift apart.</summary>
public sealed class ConveyorParityTests
{
    private const double Dt = 0.01;
    private static readonly string[] PythonOnlyTags = ["Machine.Running", "Machine.ActiveTrip"];

    private static ConveyorOptions Options() => new()
    {
        SegmentCount = 3,
        LengthMeters = 30,
        PackageSpawnPeriodSeconds = 1,
        PackageMassKg = 5,
    };

    [Fact]
    public void BothMachines_ExposeTheSameTags_ExceptThePythonOnlyOnes()
    {
        var csharp = new ConveyorMachine(Options());
        var python = new ConveyorPySharpMachine(Options());

        var csharpPaths = csharp.Tags.Select(t => t.Path).Order().ToArray();
        var pythonPaths = python.Tags.Select(t => t.Path).Order().ToArray();

        Assert.Equal(csharpPaths, pythonPaths.Except(PythonOnlyTags).ToArray());
        Assert.Equal(PythonOnlyTags.Order(), pythonPaths.Intersect(PythonOnlyTags).Order());
    }

    [Theory]
    [InlineData(100)]   // 1 s: ramp starting
    [InlineData(1500)]  // 15 s: ramp finished, packages on the belt
    public void BothMachines_ProduceTheSameValues_AfterTheSameSteps(int steps)
    {
        var csharp = new ConveyorMachine(Options());
        var python = new ConveyorPySharpMachine(Options());

        for (int i = 0; i < steps; i++)
        {
            csharp.Step(Dt);
            python.Step(Dt);
        }

        AssertSameValues(csharp, python);
    }

    // Stopping the belt makes the motors regenerate: exercises the negative-slip torque, the DC-link energy model and the brake chopper.
    [Theory]
    [InlineData(10, 0, "")]            // gentle default ramp: no trip
    [InlineData(300, 0, "OverVoltage")] // fast ramp, no braking resistor: bus overvoltage trip
    [InlineData(300, 40, "")]          // fast ramp with a braking resistor: rides through
    public void BothMachines_StayInStep_WhenTheBeltIsStopped(double decelHzPerSecond, double resistorOhms, string expectedTrip)
    {
        var options = Options();
        options.VfdDecelHzPerSecond = decelHzPerSecond;
        options.VfdBrakingResistorOhms = resistorOhms;
        var csharp = new ConveyorMachine(options);
        var python = new ConveyorPySharpMachine(options);

        for (int i = 0; i < 1500; i++) { csharp.Step(Dt); python.Step(Dt); }
        csharp.SetTargetFrequency(0);
        python.SetTargetFrequency(0);
        bool chopperSeen = false;
        for (int i = 0; i < 300; i++)
        {
            csharp.Step(Dt);
            python.Step(Dt);
            chopperSeen |= (bool)python.Tags.Single(t => t.Path == "Segments.Segment0.Vfd.BrakeChopperActive").Value!;
            if (i % 5 == 0) AssertSameValues(csharp, python);   // the transient matters, not only the end state
        }

        AssertSameValues(csharp, python);
        Assert.Equal(expectedTrip, (string)python.Tags.Single(t => t.Path == "Machine.ActiveTrip").Value!);
        Assert.Equal(resistorOhms > 0, chopperSeen);
    }

    private static void AssertSameValues(ConveyorMachine csharp, ConveyorPySharpMachine python)
    {
        Assert.Equal(csharp.SimulationTime, python.SimulationTime, 6);
        var pythonTags = python.Tags.ToDictionary(t => t.Path);
        var differences = new List<string>();
        foreach (var tag in csharp.Tags)
            Compare(tag.Path, tag.Value, pythonTags[tag.Path].Value, differences);

        Assert.True(differences.Count == 0, string.Join(Environment.NewLine, differences));
    }

    private static void Compare(string path, object? expected, object? actual, List<string> differences)
    {
        switch (expected)
        {
            case double[] e when actual is double[] a:
                if (e.Length != a.Length) { differences.Add($"{path}: length {e.Length} vs {a.Length}"); return; }
                for (int i = 0; i < e.Length; i++) Compare($"{path}[{i}]", e[i], a[i], differences);
                break;
            case double e when actual is double a:
                if (Math.Abs(e - a) > 1e-6 * Math.Max(1.0, Math.Abs(e))) differences.Add($"{path}: {e:R} vs {a:R}");
                break;
            default:
                if (!Equals(expected, actual)) differences.Add($"{path}: {expected} vs {actual}");
                break;
        }
    }
}
