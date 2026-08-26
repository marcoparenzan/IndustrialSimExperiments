using ConveyorSimLib;
using InductionMotorSimLib;
using IndustrialSimLib;
using IndustrialSimLib.SimEvents;
using PackageSimLib;
using VfdSimLib;

namespace IndustrialSim.Tests;

public sealed class SimulationTests
{
    [Fact]
    public void Bindables_SetAddAndReset_Work()
    {
        DoubleBindable number = 2.5;
        number.Add(1.5);
        Assert.Equal(4, number.Value);
        number.Reset();
        Assert.Equal(0, number.Value);
    }

    [Fact]
    public void TimedEvent_ClampsNegativeTime_AndAppliesAction()
    {
        bool applied = false;
        var evt = new ToggleAnomalyActionEvent(-1, _ => applied = true);
        evt.Apply(new SimState((_, _) => { }));
        Assert.Equal(0, evt.Time);
        Assert.True(applied);
    }

    [Fact]
    public void Vfd_RampsFrequency_AndTripsOnUndervoltage()
    {
        var settings = new VfdSettings { Accel = 10, RatedVoltageLL = 400 };
        var state = new VfdState();
        state.TargetFrequency.Set(20);
        state.HeatsinkTemp.Set(settings.AmbientTemp);
        var inputs = new VfdInputs();
        inputs.SupplyVoltageLL.Set(400);
        var outputs = new VfdOutputs();
        var vfd = new Vfd(settings, state, inputs, outputs);
        var sim = new SimState((_, _) => { });

        vfd.Step(0.5, sim);
        Assert.Equal(5, outputs.OutputFrequency.Value, 6);
        inputs.SupplyVoltageLL.Set(100);
        vfd.Step(0.1, sim);
        vfd.Step2(0.1, sim);
        Assert.Equal(VfdFaultCode.UnderVoltage, sim.ActiveTrip);
    }

    [Fact]
    public void Motor_AcceleratesAndProducesCurrent()
    {
        var settings = new InductionMotorSettings();
        var state = new InductionMotorState();
        state.Trated.Set(30);
        state.VratedPhPh.Set(400);
        var inputs = new InductionMotorInputs();
        inputs.DriveFrequencyCmd.Set(25);
        inputs.DriveVoltageCmd.Set(200);
        var outputs = new InductionMotorOutputs();
        new InductionMotor(settings, state, inputs, outputs).Step(0.01, new SimState((_, _) => { }));
        Assert.True(state.SpeedRpm.Value > 0);
        Assert.True(outputs.PhaseCurrent.Value > 0);
    }

    [Fact]
    public void Package_HasValidDefaultMass()
        => Assert.InRange(new Package().MassKg.Value, 0.5, 20);

    [Fact]
    public void Conveyor_AdvancesAndExposesProtocolNeutralTags()
    {
        var machine = new ConveyorMachine(new ConveyorOptions
        {
            SegmentCount = 2,
            LengthMeters = 20,
            PackageSpawnPeriodSeconds = 1,
            PackageMassKg = 5
        });

        for (int i = 0; i < 100; i++) machine.Step(0.01);

        Assert.Equal(1, machine.SimulationTime, 8);
        Assert.True(machine.PackageCount > 0);
        Assert.Contains(machine.Tags, tag => tag.Path == "Segments.Segment0.Motor.SpeedRpm");
        Assert.All(machine.Tags, tag => Assert.NotNull(tag.Value));
    }
}
