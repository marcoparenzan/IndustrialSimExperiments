using IndustrialSimLib;
using VfdSimLib;

namespace IndustrialSim.Tests;

public sealed class VfdControlTests
{
    private sealed class Rig
    {
        public VfdSettings Settings { get; } = new() { Accel = 1000, Decel = 1000, ControlSource = VfdControlSource.Terminals };
        public VfdState State { get; } = new();
        public VfdInputs Inputs { get; } = new();
        public VfdOutputs Outputs { get; } = new();
        public SimState Sim { get; } = new((_, _) => { });
        private Vfd? vfd;

        public Rig()
        {
            Inputs.SupplyVoltageLL.Set(400);
            State.HeatsinkTemp.Set(Settings.AmbientTemp);
        }

        public void Tick(int count = 1)
        {
            vfd ??= new Vfd(Settings, State, Inputs, Outputs);
            for (int i = 0; i < count; i++)
            {
                vfd.Step(0.01, Sim);
                vfd.Step2(0.01, Sim);
            }
        }
    }

    [Fact]
    public void WithoutRunCommand_TheDriveStaysStopped()
    {
        var rig = new Rig();
        rig.Inputs.AnalogSpeedRef.Set(10);
        rig.Tick(10);
        Assert.Equal(0, rig.Outputs.OutputFrequency.Value, 6);
        Assert.False(rig.Outputs.RunningRelay.Value);
    }

    [Theory]
    [InlineData(true, false, false, 10)]
    [InlineData(false, true, false, 20)]
    [InlineData(true, true, false, 30)]
    [InlineData(true, false, true, 45)]   // code 5 -> fifth step frequency
    [InlineData(true, true, true, 50)]
    public void MultiStepInputs_SelectTheStepFrequency(bool ms1, bool ms2, bool ms3, double expectedHz)
    {
        var rig = new Rig();
        rig.Inputs.RunForward.Set(true);
        rig.Inputs.MultiStep1.Set(ms1);
        rig.Inputs.MultiStep2.Set(ms2);
        rig.Inputs.MultiStep3.Set(ms3);
        rig.Tick(5);
        Assert.Equal(expectedHz, rig.Outputs.OutputFrequency.Value, 6);
    }

    [Theory]
    [InlineData(VfdAnalogInputMode.Voltage0To10, 5, 25)]
    [InlineData(VfdAnalogInputMode.Voltage0To10, 10, 50)]
    [InlineData(VfdAnalogInputMode.Voltage0To10, 12, 50)]   // above range: clamped
    [InlineData(VfdAnalogInputMode.Current4To20, 12, 25)]
    [InlineData(VfdAnalogInputMode.Current4To20, 4, 0)]
    [InlineData(VfdAnalogInputMode.Current4To20, 2, 0)]     // broken wire / below 4 mA: clamped to zero
    public void AnalogReference_IsScaledToTheMaximumOutputFrequency(VfdAnalogInputMode mode, double input, double expectedHz)
    {
        var rig = new Rig();
        rig.Settings.AnalogMode = mode;
        rig.Inputs.RunForward.Set(true);
        rig.Inputs.AnalogSpeedRef.Set(input);
        rig.Tick(5);
        Assert.Equal(expectedHz, rig.Outputs.OutputFrequency.Value, 6);
    }

    [Fact]
    public void MultiStep_TakesPrecedenceOverTheAnalogReference()
    {
        var rig = new Rig();
        rig.Inputs.RunForward.Set(true);
        rig.Inputs.AnalogSpeedRef.Set(10);
        rig.Inputs.MultiStep1.Set(true);
        rig.Tick(5);
        Assert.Equal(10, rig.Outputs.OutputFrequency.Value, 6);
    }

    [Fact]
    public void SetpointMode_IgnoresTheTerminals()
    {
        var rig = new Rig();
        rig.Settings.ControlSource = VfdControlSource.Setpoint;
        rig.State.TargetFrequency.Set(20);
        rig.Tick(5);
        Assert.Equal(20, rig.Outputs.OutputFrequency.Value, 6);
    }

    [Fact]
    public void FaultReset_ClearsAnActiveTripOnTheRisingEdgeOnly()
    {
        var rig = new Rig();
        rig.Inputs.RunForward.Set(true);
        rig.Inputs.MultiStep1.Set(true);
        rig.Inputs.SupplyVoltageLL.Set(100);   // undervoltage trip
        rig.Tick();
        Assert.Equal(VfdFaultCode.UnderVoltage, rig.Sim.ActiveTrip);
        Assert.True(rig.Outputs.FaultRelay.Value);
        Assert.False(rig.Outputs.RunningRelay.Value);

        rig.Inputs.SupplyVoltageLL.Set(400);
        rig.Tick(3);
        Assert.Equal(VfdFaultCode.UnderVoltage, rig.Sim.ActiveTrip);   // no reset without the input

        rig.Inputs.FaultReset.Set(true);
        rig.Tick();
        Assert.Equal(FaultCode.None, rig.Sim.ActiveTrip);
        Assert.False(rig.Outputs.FaultRelay.Value);

        rig.Inputs.SupplyVoltageLL.Set(100);   // trips again while the reset input is still held high
        rig.Tick();
        Assert.Equal(VfdFaultCode.UnderVoltage, rig.Sim.ActiveTrip);
        rig.Tick(3);
        Assert.Equal(VfdFaultCode.UnderVoltage, rig.Sim.ActiveTrip);   // a held input must not keep resetting
    }

    [Fact]
    public void RunningRelayAndAnalogOutput_FollowTheOutputFrequency()
    {
        var rig = new Rig();
        rig.Inputs.RunForward.Set(true);
        rig.Inputs.AnalogSpeedRef.Set(5);
        rig.Tick(5);
        Assert.True(rig.Outputs.RunningRelay.Value);
        Assert.False(rig.Outputs.FaultRelay.Value);
        Assert.Equal(5, rig.Outputs.AnalogOutFrequency.Value, 6);   // 25 Hz of 50 Hz -> 5 V of 10 V
    }
}
