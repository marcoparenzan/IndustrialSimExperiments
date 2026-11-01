using ConveyorSimLib;
using IndustrialSimLib;
using VfdSimLib;

namespace IndustrialSim.Tests;

public sealed class VfdBusVoltageTests
{
    private static (Vfd Vfd, VfdState State, VfdInputs Inputs, SimState Sim) Create(double supplyLL = 400)
    {
        var settings = new VfdSettings { RatedVoltageLL = 400, MaxCurrent = 30 };
        var state = new VfdState();
        state.TargetFrequency.Set(30);
        state.HeatsinkTemp.Set(settings.AmbientTemp);
        var inputs = new VfdInputs();
        inputs.SupplyVoltageLL.Set(supplyLL);
        var vfd = new Vfd(settings, state, inputs, new VfdOutputs());
        return (vfd, state, inputs, new SimState((_, _) => { }));
    }

    [Fact]
    public void BusVoltage_AtNoLoad_IsThePeakOfTheLineVoltage()
    {
        var (vfd, state, _, sim) = Create();
        vfd.Step(0.01, sim);
        Assert.Equal(400 * Math.Sqrt(2), state.BusVoltage.Value, 6);
    }

    [Fact]
    public void BusVoltage_AtFullLoad_DropsToTheSixPulseAverage()
    {
        var (vfd, state, inputs, sim) = Create();
        inputs.MotorCurrentFeedback.Set(30);
        vfd.Step(0.01, sim);
        Assert.Equal(400 * 1.35, state.BusVoltage.Value, 6);
    }

    [Fact]
    public void BusVoltage_AboveFullLoad_IsClampedAtFullLoadValue()
    {
        var (vfd, state, inputs, sim) = Create();
        inputs.MotorCurrentFeedback.Set(45);
        vfd.Step(0.01, sim);
        Assert.Equal(400 * 1.35, state.BusVoltage.Value, 6);
    }

    [Fact]
    public void BusVoltage_AtHalfLoad_IsBetweenPeakAndAverage()
    {
        var (vfd, state, inputs, sim) = Create();
        inputs.MotorCurrentFeedback.Set(15);
        vfd.Step(0.01, sim);
        Assert.Equal(400 * (Math.Sqrt(2) + 1.35) / 2, state.BusVoltage.Value, 6);
    }

    [Fact]
    public void OverVoltageTrip_UsesNominalBusVoltageWhenSet()
    {
        var (vfd, state, _, sim) = Create();
        state.VdcNom.Set(450);   // 1.20 x 450 = 540 V < 566 V no-load bus
        vfd.Step(0.01, sim);
        vfd.Step2(0.01, sim);
        Assert.Equal(VfdFaultCode.OverVoltage, sim.ActiveTrip);
    }

    [Fact]
    public void OverVoltageTrip_FallsBackToRatedVoltageWhenNominalIsNotSet()
    {
        var (vfd, _, _, sim) = Create();
        vfd.Step(0.01, sim);
        vfd.Step2(0.01, sim);
        Assert.Equal(FaultCode.None, sim.ActiveTrip);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Conveyor_ExposesNominalBusVoltage(bool python)
    {
        IMachineModule machine = python
            ? new ConveyorPySharpMachine(new ConveyorOptions { SegmentCount = 1 })
            : new ConveyorMachine(new ConveyorOptions { SegmentCount = 1 });
        var tag = machine.Tags.Single(t => t.Path == "Segments.Segment0.Vfd.BusVoltageNominal");
        Assert.Equal(400 * Math.Sqrt(2), Convert.ToDouble(tag.Value), 6);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Conveyor_BusVoltage_SagsBelowNominalWhileTheMotorRuns(bool python)
    {
        var options = new ConveyorOptions { SegmentCount = 1, LengthMeters = 10 };
        IMachineModule machine = python ? new ConveyorPySharpMachine(options) : new ConveyorMachine(options);
        for (int i = 0; i < 1500; i++) machine.Step(0.01);

        double Read(string name) => Convert.ToDouble(machine.Tags.Single(t => t.Path == $"Segments.Segment0.Vfd.{name}").Value);
        double current = Convert.ToDouble(machine.Tags.Single(t => t.Path == "Segments.Segment0.Motor.PhaseCurrent").Value);
        Assert.True(current > 0, "motor must be drawing current for this test to mean anything");
        Assert.True(Read("BusVoltage") < Read("BusVoltageNominal"));
        Assert.True(Read("BusVoltage") > 400 * 1.35);
    }
}
