using InductionMotorSimLib;
using IndustrialSimLib;
using VfdSimLib;

namespace IndustrialSim.Tests;

public sealed class VfdRegenerationTests
{
    private const double Dt = 0.001;

    private sealed class Rig
    {
        public VfdSettings VfdSettings { get; } = new() { Accel = 50 };
        public VfdState VfdState { get; } = new();
        public VfdInputs VfdInputs { get; } = new();
        public VfdOutputs VfdOutputs { get; } = new();
        public InductionMotorSettings MotorSettings { get; } = new() { ConstLoadTorque = 3 };
        public InductionMotorState MotorState { get; } = new();
        public InductionMotorInputs MotorInputs { get; } = new();
        public InductionMotorOutputs MotorOutputs { get; } = new();
        public SimState Sim { get; } = new((_, _) => { });
        public double MaxBusVoltage { get; private set; }
        public bool ChopperWasActive { get; private set; }
        private Vfd vfd = null!;
        private InductionMotor motor = null!;

        public Rig Build()
        {
            VfdState.VdcNom.Set(Math.Sqrt(2) * VfdSettings.RatedVoltageLL);
            VfdState.BusVoltage.Set(VfdState.VdcNom);
            VfdState.HeatsinkTemp.Set(VfdSettings.AmbientTemp);
            VfdInputs.SupplyVoltageLL.Set(400);
            MotorState.VratedPhPh.Set(MotorSettings.RatedVoltageLL);
            MotorState.Trated.Set(MotorSettings.RatedPower / (2 * Math.PI * MotorSettings.RatedSpeedRpm / 60));
            vfd = new Vfd(VfdSettings, VfdState, VfdInputs, VfdOutputs);
            motor = new InductionMotor(MotorSettings, MotorState, MotorInputs, MotorOutputs);
            return this;
        }

        public void Run(double seconds)
        {
            for (int i = 0; i < (int)(seconds / Dt); i++)
            {
                Sim.Step(Dt);
                vfd.Step(Dt, Sim);
                MotorInputs.DriveFrequencyCmd.Set(VfdOutputs.OutputFrequency);
                MotorInputs.DriveVoltageCmd.Set(VfdOutputs.OutputVoltage);
                motor.Step(Dt, Sim);
                VfdInputs.MotorCurrentFeedback.Set(MotorOutputs.PhaseCurrent);
                VfdInputs.MotorPowerFeedback.Set(MotorOutputs.ShaftPowerW);
                vfd.Step2(Dt, Sim);
                MaxBusVoltage = Math.Max(MaxBusVoltage, VfdState.BusVoltage);
                ChopperWasActive |= VfdState.BrakeChopperActive;
            }
        }
    }

    private static Rig SpunUp(Action<VfdSettings>? configure = null)
    {
        var rig = new Rig();
        configure?.Invoke(rig.VfdSettings);
        rig.Build();
        rig.VfdState.TargetFrequency.Set(30);
        rig.Run(3);
        Assert.Equal(FaultCode.None, rig.Sim.ActiveTrip);   // the ramp-up itself must be clean
        return rig;
    }

    [Fact]
    public void Motor_RunningFasterThanTheDriveFrequency_GeneratesNegativeTorqueAndPower()
    {
        var settings = new InductionMotorSettings();
        var state = new InductionMotorState();
        state.VratedPhPh.Set(400);
        state.Trated.Set(settings.RatedPower / (2 * Math.PI * settings.RatedSpeedRpm / 60));
        state.SpeedRpm.Set(900);
        var inputs = new InductionMotorInputs();
        inputs.DriveFrequencyCmd.Set(10);
        inputs.DriveVoltageCmd.Set(80);
        var outputs = new InductionMotorOutputs();

        new InductionMotor(settings, state, inputs, outputs).Step(0.001, new SimState((_, _) => { }));

        Assert.True(state.ElectTorque.Value < 0);
        Assert.True(outputs.ShaftPowerW.Value < 0);
    }

    [Fact]
    public void Motor_WithPositiveSlip_StillProducesPositivePower()
    {
        var settings = new InductionMotorSettings();
        var state = new InductionMotorState();
        state.VratedPhPh.Set(400);
        state.Trated.Set(settings.RatedPower / (2 * Math.PI * settings.RatedSpeedRpm / 60));
        state.SpeedRpm.Set(600);
        var inputs = new InductionMotorInputs();
        inputs.DriveFrequencyCmd.Set(30);
        inputs.DriveVoltageCmd.Set(240);
        var outputs = new InductionMotorOutputs();

        new InductionMotor(settings, state, inputs, outputs).Step(0.001, new SimState((_, _) => { }));

        Assert.True(state.ElectTorque.Value > 0);
        Assert.True(outputs.ShaftPowerW.Value > 0);
    }

    [Fact]
    public void GentleDeceleration_DoesNotTripAndDoesNotNeedTheChopper()
    {
        var rig = SpunUp();
        rig.VfdState.TargetFrequency.Set(0);
        rig.Run(5);
        Assert.Equal(FaultCode.None, rig.Sim.ActiveTrip);
        Assert.False(rig.ChopperWasActive);
    }

    [Fact]
    public void FastDeceleration_WithoutBrakingResistor_TripsOnOverVoltage()
    {
        var rig = SpunUp(s => s.Decel = 300);
        rig.VfdState.TargetFrequency.Set(0);
        rig.Run(1);
        Assert.Equal(VfdFaultCode.OverVoltage, rig.Sim.ActiveTrip);
        Assert.False(rig.ChopperWasActive);
    }

    [Fact]
    public void FastDeceleration_WithBrakingResistor_RidesThroughWithTheChopper()
    {
        var rig = SpunUp(s => { s.Decel = 300; s.BrakingResistorOhms = 40; });
        rig.VfdState.TargetFrequency.Set(0);
        rig.Run(1);
        Assert.Equal(FaultCode.None, rig.Sim.ActiveTrip);
        Assert.True(rig.ChopperWasActive);
        Assert.True(rig.MaxBusVoltage < rig.VfdSettings.OverVoltPUNomDC * rig.VfdState.VdcNom.Value);
    }

    [Fact]
    public void BrakeChopper_SwitchesOffAgainOnceTheEnergyIsDissipated()
    {
        var rig = SpunUp(s => { s.Decel = 300; s.BrakingResistorOhms = 40; });
        rig.VfdState.TargetFrequency.Set(0);
        rig.Run(3);
        Assert.False(rig.VfdState.BrakeChopperActive.Value);
        Assert.Equal(0, rig.VfdState.BrakingPowerW.Value, 6);
        Assert.True(rig.VfdState.BusVoltage.Value < 1.1 * rig.VfdState.VdcNom.Value);
    }
}
