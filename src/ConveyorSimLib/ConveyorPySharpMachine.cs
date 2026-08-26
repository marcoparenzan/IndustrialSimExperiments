using System.Globalization;
using IndustrialSimLib;
using PySharpLib;
using PySharpLib.Runtime;

namespace ConveyorSimLib;

/// <summary>A drop-in <see cref="IMachineModule"/> alternative to ConveyorSimLib's ConveyorMachine —
/// same config shape (<see cref="PySharpConveyorOptions"/> mirrors ConveyorOptions field-for-field),
/// same tag names/paths, but the actual motor/VFD/belt physics lives in
/// <c>Scripts/conveyor_machine.py</c> and runs inside a real PySharp <see cref="PyEngine"/> instead
/// of four separate C# device libraries. See conveyor_machine.py's own header for the physics
/// port's scope notes.</summary>
public sealed class ConveyorPySharpMachine : IMachineModule
{
    private readonly ConveyorOptions options;
    private readonly PyEngine engine;
    private readonly object initFn;
    private readonly object stepFn;
    private readonly IReadOnlyCollection<SimulationTag> tags;
    private Dictionary<string, object?> lastTags;
    private double simulationTime;

    public ConveyorPySharpMachine(ConveyorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SegmentCount <= 0) throw new ArgumentOutOfRangeException(nameof(options.SegmentCount));
        if (options.LengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(options.LengthMeters));
        if (options.PackageSpawnPeriodSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(options.PackageSpawnPeriodSeconds));
        if (options.PackageMassKg is < 0.5 or > 20.0)
            throw new ArgumentOutOfRangeException(nameof(options.PackageMassKg), "Package mass must be between 0.5 and 20 kg.");
        this.options = options;

        string scriptPath = Path.Combine(AppContext.BaseDirectory, "Scripts", "conveyor_machine.py");
        string source = File.ReadAllText(scriptPath);

        engine = new PyEngine();
        var module = engine.Run(source, "conveyor_machine.py");

        if (!module.Dict.TryGet("init", out var initFnObj) ||
            !module.Dict.TryGet("step", out var stepFnObj) ||
            !module.Dict.TryGet("snapshot", out var snapshotFnObj))
            throw new InvalidOperationException("conveyor_machine.py is missing init/step/snapshot.");
        initFn = initFnObj;
        stepFn = stepFnObj;

        // Interp.Call does not marshal raw CLR scalars the way PyEngine.Run's Globals dict does —
        // a boxed System.Int32 doesn't match the interpreter's own BigInteger-shaped Python int, so
        // arithmetic against it fails with a real TypeError. Marshal explicitly, matching what
        // PyEngine.Run does internally for injected globals.
        engine.Interp.Call(initFn, new object[]
        {
            ClrMarshal.ToPython(options.SegmentCount), ClrMarshal.ToPython(options.LengthMeters),
            ClrMarshal.ToPython(options.TargetFrequencyHz), ClrMarshal.ToPython(options.PackageSpawnPeriodSeconds),
            ClrMarshal.ToPython(options.PackageMassKg), ClrMarshal.ToPython(options.PulleyRadiusMeters),
            ClrMarshal.ToPython(options.GearRatio), ClrMarshal.ToPython(options.MechanicalEfficiency),
            ClrMarshal.ToPython(options.RollingCoefficient),
        });

        lastTags = ToTagDict(engine.Interp.Call(snapshotFnObj, []));
        tags = BuildTags();
    }

    public string Name => "Conveyor (PySharp)";
    public double SimulationTime => simulationTime;
    public IReadOnlyCollection<SimulationTag> Tags => tags;

    public void Step(double deltaTimeSeconds)
    {
        if (!double.IsFinite(deltaTimeSeconds) || deltaTimeSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));

        lastTags = ToTagDict(engine.Interp.Call(stepFn, [ClrMarshal.ToPython(deltaTimeSeconds)]));
        simulationTime = ReadDouble("__time__");
    }

    private static Dictionary<string, object?> ToTagDict(object pyResult) =>
        (Dictionary<string, object?>)ClrMarshal.ToPlainObject(pyResult)!;

    private IReadOnlyCollection<SimulationTag> BuildTags()
    {
        List<SimulationTag> result =
        [
            SimulationTag.Create("Supply.LineLineVoltage", () => ReadDouble("Supply.LineLineVoltage")),
            SimulationTag.Create("Supply.Frequency", () => ReadDouble("Supply.Frequency")),
            SimulationTag.Create("Packages.Count", () => ReadInt("Packages.Count")),
            SimulationTag.Create("Packages.Positions", () => ReadDoubleArray("Packages.Positions")),
            SimulationTag.Create("Packages.Masses", () => ReadDoubleArray("Packages.Masses")),
            // Bonus tags the C# ConveyorMachine doesn't expose — the Python core tracks the same
            // shared-trip/running state real ConveyorMachine's SimState does internally, so surface it.
            SimulationTag.Create("Machine.Running", () => (bool)lastTags["__running__"]!),
            SimulationTag.Create("Machine.ActiveTrip", () => (string)lastTags["__active_trip__"]!),
        ];

        for (int i = 0; i < options.SegmentCount; i++)
        {
            string root = $"Segments.Segment{i}";
            result.Add(SimulationTag.Create($"{root}.Vfd.OutputFrequency", () => ReadDouble($"{root}.Vfd.OutputFrequency")));
            result.Add(SimulationTag.Create($"{root}.Vfd.OutputVoltage", () => ReadDouble($"{root}.Vfd.OutputVoltage")));
            result.Add(SimulationTag.Create($"{root}.Vfd.BusVoltage", () => ReadDouble($"{root}.Vfd.BusVoltage")));
            result.Add(SimulationTag.Create($"{root}.Vfd.HeatsinkTemp", () => ReadDouble($"{root}.Vfd.HeatsinkTemp")));
            result.Add(SimulationTag.Create($"{root}.Motor.SpeedRpm", () => ReadDouble($"{root}.Motor.SpeedRpm")));
            result.Add(SimulationTag.Create($"{root}.Motor.PhaseCurrent", () => ReadDouble($"{root}.Motor.PhaseCurrent")));
        }

        return result.AsReadOnly();
    }

    private double ReadDouble(string path) => Convert.ToDouble(lastTags[path], CultureInfo.InvariantCulture);
    private int ReadInt(string path) => Convert.ToInt32(lastTags[path], CultureInfo.InvariantCulture);

    private double[] ReadDoubleArray(string path) =>
        ((List<object?>)lastTags[path]!).Select(v => Convert.ToDouble(v, CultureInfo.InvariantCulture)).ToArray();
}
