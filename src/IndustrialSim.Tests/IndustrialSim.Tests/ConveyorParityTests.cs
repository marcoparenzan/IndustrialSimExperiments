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
