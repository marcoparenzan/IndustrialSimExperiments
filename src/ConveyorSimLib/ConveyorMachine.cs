using InductionMotorSimLib;
using IndustrialSimLib;
using PackageSimLib;
using ThreePhaseSupplySimLib;
using VfdSimLib;

namespace ConveyorSimLib;

public sealed class ConveyorMachine : IMachineModule
{
    private readonly ConveyorOptions options;
    private readonly ThreePhaseSupply supply;
    private readonly ThreePhaseSupplyState supplyState;
    private readonly ThreePhaseSupplyOutputs supplyOutputs;
    private readonly ConveyorSegment[] segments;
    private readonly List<Package> packages = [];
    private readonly SimState simState = new((_, _) => { });
    private readonly IReadOnlyCollection<SimulationTag> tags;
    private double nextPackageSpawn;

    public ConveyorMachine(ConveyorOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.SegmentCount <= 0) throw new ArgumentOutOfRangeException(nameof(options.SegmentCount));
        if (options.LengthMeters <= 0) throw new ArgumentOutOfRangeException(nameof(options.LengthMeters));
        if (options.PackageSpawnPeriodSeconds <= 0) throw new ArgumentOutOfRangeException(nameof(options.PackageSpawnPeriodSeconds));
        this.options = options;

        var supplySettings = new ThreePhaseSupplySettings();
        supplyState = new ThreePhaseSupplyState();
        supplyState.TargetVoltageLL.Set(supplySettings.NominalVoltageLL);
        supplyState.TargetFrequency.Set(supplySettings.NominalFrequency);
        supplyOutputs = new ThreePhaseSupplyOutputs();
        supplyOutputs.LineLineVoltage.Set(supplySettings.NominalVoltageLL);
        supplyOutputs.Frequency.Set(supplySettings.NominalFrequency);
        supply = new ThreePhaseSupply(supplySettings, supplyState, new ThreePhaseSupplyInputs(), supplyOutputs);

        double segmentLength = options.LengthMeters / options.SegmentCount;
        segments = Enumerable.Range(0, options.SegmentCount)
            .Select(index => CreateSegment(index, segmentLength))
            .ToArray();
        tags = BuildTags();
    }

    public string Name => "Conveyor";
    public double SimulationTime => simState.Time;
    public IReadOnlyCollection<SimulationTag> Tags => tags;
    public int PackageCount => packages.Count;

    public void Step(double deltaTimeSeconds)
    {
        if (!double.IsFinite(deltaTimeSeconds) || deltaTimeSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(deltaTimeSeconds));

        simState.Step(deltaTimeSeconds);
        SpawnPackages();
        supply.Step(deltaTimeSeconds, simState);

        foreach (var segment in segments)
        {
            segment.VfdInputs.SupplyVoltageLL.Set(supplyOutputs.LineLineVoltage);
            segment.VfdInputs.SupplyFrequency.Set(supplyOutputs.Frequency);
            segment.Vfd.Step(deltaTimeSeconds, simState);

            segment.MotorInputs.DriveFrequencyCmd.Set(segment.VfdOutputs.OutputFrequency);
            segment.MotorInputs.DriveVoltageCmd.Set(segment.VfdOutputs.OutputVoltage);

            double beltSpeed = BeltSpeed(segment.MotorState.SpeedRpm);
            double acceleration = (beltSpeed - segment.LastBeltSpeed) / deltaTimeSeconds;
            segment.LastBeltSpeed = beltSpeed;
            double mass = packages.Where(p => p.PositionM >= segment.StartMeters && p.PositionM < segment.EndMeters)
                                  .Sum(p => p.MassKg.Value);
            double force = mass * 9.81 * options.RollingCoefficient + mass * acceleration;
            double packageTorque = force * options.PulleyRadiusMeters /
                                   (options.GearRatio * options.MechanicalEfficiency);
            segment.MotorSettings.ConstLoadTorque = segment.BaseLoadTorque + packageTorque;

            segment.Motor.Step(deltaTimeSeconds, simState);
            segment.VfdInputs.MotorCurrentFeedback.Set(segment.MotorOutputs.PhaseCurrent);
            segment.Vfd.Step2(deltaTimeSeconds, simState);
        }

        MovePackages(deltaTimeSeconds);
    }

    private ConveyorSegment CreateSegment(int index, double length)
    {
        var vfdSettings = new VfdSettings();
        var vfdState = new VfdState();
        vfdState.TargetFrequency.Set(options.TargetFrequencyHz);
        vfdState.BusVoltage.Set(Math.Sqrt(2) * vfdSettings.RatedVoltageLL);
        vfdState.HeatsinkTemp.Set(vfdSettings.AmbientTemp);
        var vfdInputs = new VfdInputs();
        var vfdOutputs = new VfdOutputs();

        var motorSettings = new InductionMotorSettings { ConstLoadTorque = 3 };
        var motorState = new InductionMotorState();
        motorState.VratedPhPh.Set(motorSettings.RatedVoltageLL);
        motorState.Trated.Set(motorSettings.RatedPower /
                              (2 * Math.PI * motorSettings.RatedSpeedRpm / 60));
        var motorInputs = new InductionMotorInputs();
        var motorOutputs = new InductionMotorOutputs();

        return new ConveyorSegment(
            index, index * length, (index + 1) * length,
            new Vfd(vfdSettings, vfdState, vfdInputs, vfdOutputs),
            vfdState, vfdInputs, vfdOutputs,
            new InductionMotor(motorSettings, motorState, motorInputs, motorOutputs),
            motorState, motorSettings, motorInputs, motorOutputs, motorSettings.ConstLoadTorque);
    }

    private void SpawnPackages()
    {
        while (simState.Time >= nextPackageSpawn)
        {
            packages.Add(new Package(options.PackageMassKg));
            nextPackageSpawn += options.PackageSpawnPeriodSeconds;
        }
    }

    private void MovePackages(double dt)
    {
        double segmentLength = options.LengthMeters / options.SegmentCount;
        for (int index = packages.Count - 1; index >= 0; index--)
        {
            var package = packages[index];
            int segmentIndex = Math.Clamp((int)(package.PositionM.Value / segmentLength), 0, segments.Length - 1);
            package.PositionM.Add(BeltSpeed(segments[segmentIndex].MotorState.SpeedRpm) * dt);
            if (package.PositionM >= options.LengthMeters) packages.RemoveAt(index);
        }
    }

    private double BeltSpeed(double rpm) =>
        2 * Math.PI * rpm / 60 * options.PulleyRadiusMeters / options.GearRatio;

    private IReadOnlyCollection<SimulationTag> BuildTags()
    {
        List<SimulationTag> result =
        [
            SimulationTag.Create("Supply.LineLineVoltage", () => supplyOutputs.LineLineVoltage.Value),
            SimulationTag.Create("Supply.Frequency", () => supplyOutputs.Frequency.Value),
            SimulationTag.Create("Packages.Count", () => packages.Count),
            SimulationTag.Create("Packages.Positions", () => packages.Select(p => p.PositionM.Value).ToArray()),
            SimulationTag.Create("Packages.Masses", () => packages.Select(p => p.MassKg.Value).ToArray())
        ];

        foreach (var segment in segments)
        {
            string root = $"Segments.Segment{segment.Index}";
            result.Add(SimulationTag.Create($"{root}.Vfd.OutputFrequency", () => segment.VfdOutputs.OutputFrequency.Value));
            result.Add(SimulationTag.Create($"{root}.Vfd.OutputVoltage", () => segment.VfdOutputs.OutputVoltage.Value));
            result.Add(SimulationTag.Create($"{root}.Vfd.BusVoltage", () => segment.VfdState.BusVoltage.Value));
            result.Add(SimulationTag.Create($"{root}.Vfd.HeatsinkTemp", () => segment.VfdState.HeatsinkTemp.Value));
            result.Add(SimulationTag.Create($"{root}.Motor.SpeedRpm", () => segment.MotorState.SpeedRpm.Value));
            result.Add(SimulationTag.Create($"{root}.Motor.PhaseCurrent", () => segment.MotorOutputs.PhaseCurrent.Value));
        }

        return result.AsReadOnly();
    }
}
