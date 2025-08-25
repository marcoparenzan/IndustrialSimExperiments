using InductionMotorSimLib;
using Opc.Ua;
using PackageSimLib;
using ThreePhaseSupplySimLib;
using VfdSimLib;

namespace ConveyorSimApp.OpcUa;

public sealed class ConveyorNodeBindings
{
    internal ISystemContext Ctx { get; set; }

    // Supply
    public BaseDataVariableState Supply_LineLineVoltage { get; set; }
    public BaseDataVariableState Supply_Frequency { get; set; }
    public BaseDataVariableState Supply_TargetVoltageLL { get; set; }
    public BaseDataVariableState Supply_TargetFrequency { get; set; }
    public BaseDataVariableState Supply_AnUnderVoltage { get; set; }
    public BaseDataVariableState Supply_AnOverVoltage { get; set; }
    public BaseDataVariableState Supply_AnFrequencyDrift { get; set; }

    // Per-segment bindings
    public sealed class SegmentBinding
    {
        // VFD State
        public BaseDataVariableState Vfd_TargetFrequency { get; set; }
        public BaseDataVariableState Vfd_BusVoltage { get; set; }
        public BaseDataVariableState Vfd_HeatsinkTemp { get; set; }
        public BaseDataVariableState Vfd_AnUnderVoltage { get; set; }
        public BaseDataVariableState Vfd_AnOverVoltage { get; set; }
        public BaseDataVariableState Vfd_AnPhaseLoss { get; set; }
        public BaseDataVariableState Vfd_AnGroundFault { get; set; }

        // VFD Inputs
        public BaseDataVariableState VfdIn_SupplyVoltageLL { get; set; }
        public BaseDataVariableState VfdIn_SupplyFrequency { get; set; }
        public BaseDataVariableState VfdIn_MotorCurrentFeedback { get; set; }

        // VFD Outputs
        public BaseDataVariableState VfdOut_OutputFrequency { get; set; }
        public BaseDataVariableState VfdOut_OutputVoltage { get; set; }

        // Motor State
        public BaseDataVariableState Mot_SpeedRpm { get; set; }
        public BaseDataVariableState Mot_ElectTorque { get; set; }
        public BaseDataVariableState Mot_Trated { get; set; }
        public BaseDataVariableState Mot_VratedPhPh { get; set; }
        public BaseDataVariableState Mot_AnPhaseLoss { get; set; }
        public BaseDataVariableState Mot_AnLoadJam { get; set; }
        public BaseDataVariableState Mot_AnBearingWear { get; set; }
        public BaseDataVariableState Mot_AnSensorNoise { get; set; }

        // Motor Inputs
        public BaseDataVariableState MotIn_DriveFrequencyCmd { get; set; }
        public BaseDataVariableState MotIn_DriveVoltageCmd { get; set; }

        // Motor Outputs
        public BaseDataVariableState MotOut_PhaseCurrent { get; set; }
    }

    public SegmentBinding[] Segments { get; set; }

    // Packages
    public BaseDataVariableState Pkg_Count { get; set; }
    public BaseDataVariableState Pkg_Positions { get; set; } // double[]
    public BaseDataVariableState Pkg_Masses { get; set; }    // double[]

    public void UpdateSupply(ThreePhaseSupplyState st, ThreePhaseSupplyOutputs outs)
    {
        Supply_LineLineVoltage.Value = outs.LineLineVoltage;
        Supply_Frequency.Value = outs.Frequency;
        Supply_TargetVoltageLL.Value = st.TargetVoltageLL;
        Supply_TargetFrequency.Value = st.TargetFrequency;
        Supply_AnUnderVoltage.Value = st.An_UnderVoltage;
        Supply_AnOverVoltage.Value = st.An_OverVoltage;
        Supply_AnFrequencyDrift.Value = st.An_FrequencyDrift;

        Supply_LineLineVoltage.ClearChangeMasks(Ctx, false);
        Supply_Frequency.ClearChangeMasks(Ctx, false);
        Supply_TargetVoltageLL.ClearChangeMasks(Ctx, false);
        Supply_TargetFrequency.ClearChangeMasks(Ctx, false);
        Supply_AnUnderVoltage.ClearChangeMasks(Ctx, false);
        Supply_AnOverVoltage.ClearChangeMasks(Ctx, false);
        Supply_AnFrequencyDrift.ClearChangeMasks(Ctx, false);
    }

    public void UpdateSegment(int i, VfdState vfdState, VfdInputs vfdIn, VfdOutputs vfdOut,
                              InductionMotorState motState, InductionMotorInputs motIn, InductionMotorOutputs motOut)
    {
        var s = Segments[i];

        // VFD state
        s.Vfd_TargetFrequency.Value = vfdState.TargetFrequency;
        s.Vfd_BusVoltage.Value = vfdState.BusVoltage;
        s.Vfd_HeatsinkTemp.Value = vfdState.HeatsinkTemp;
        s.Vfd_AnUnderVoltage.Value = vfdState.An_UnderVoltage;
        s.Vfd_AnOverVoltage.Value = vfdState.An_OverVoltage;
        s.Vfd_AnPhaseLoss.Value = vfdState.An_PhaseLoss;
        s.Vfd_AnGroundFault.Value = vfdState.An_GroundFault;

        // VFD IO
        s.VfdIn_SupplyVoltageLL.Value = vfdIn.SupplyVoltageLL;
        s.VfdIn_SupplyFrequency.Value = vfdIn.SupplyFrequency;
        s.VfdIn_MotorCurrentFeedback.Value = vfdIn.MotorCurrentFeedback;
        s.VfdOut_OutputFrequency.Value = vfdOut.OutputFrequency;
        s.VfdOut_OutputVoltage.Value = vfdOut.OutputVoltage;

        // Motor state
        s.Mot_SpeedRpm.Value = motState.SpeedRpm;
        s.Mot_ElectTorque.Value = motState.ElectTorque;
        s.Mot_Trated.Value = motState.Trated;
        s.Mot_VratedPhPh.Value = motState.VratedPhPh;
        s.Mot_AnPhaseLoss.Value = motState.An_PhaseLoss;
        s.Mot_AnLoadJam.Value = motState.An_LoadJam;
        s.Mot_AnBearingWear.Value = motState.An_BearingWear;
        s.Mot_AnSensorNoise.Value = motState.An_SensorNoise;

        // Motor IO
        s.MotIn_DriveFrequencyCmd.Value = motIn.DriveFrequencyCmd;
        s.MotIn_DriveVoltageCmd.Value = motIn.DriveVoltageCmd;
        s.MotOut_PhaseCurrent.Value = motOut.PhaseCurrent;

        foreach (var v in new[] {
            s.Vfd_TargetFrequency,s.Vfd_BusVoltage,s.Vfd_HeatsinkTemp,s.Vfd_AnUnderVoltage,s.Vfd_AnOverVoltage,s.Vfd_AnPhaseLoss,s.Vfd_AnGroundFault,
            s.VfdIn_SupplyVoltageLL,s.VfdIn_SupplyFrequency,s.VfdIn_MotorCurrentFeedback,s.VfdOut_OutputFrequency,s.VfdOut_OutputVoltage,
            s.Mot_SpeedRpm,s.Mot_ElectTorque,s.Mot_Trated,s.Mot_VratedPhPh,s.Mot_AnPhaseLoss,s.Mot_AnLoadJam,s.Mot_AnBearingWear,s.Mot_AnSensorNoise,
            s.MotIn_DriveFrequencyCmd,s.MotIn_DriveVoltageCmd,s.MotOut_PhaseCurrent})
        {
            v.ClearChangeMasks(Ctx, false);
        }
    }

    public void UpdatePackages(IReadOnlyList<Package> pkgs)
    {
        var positions = pkgs.Select(p => p.PositionM).ToArray();
        var masses = pkgs.Select(p => p.MassKg).ToArray();
        Pkg_Count.Value = pkgs.Count;
        Pkg_Positions.Value = positions;
        Pkg_Masses.Value = masses;

        Pkg_Count.ClearChangeMasks(Ctx, false);
        Pkg_Positions.ClearChangeMasks(Ctx, false);
        Pkg_Masses.ClearChangeMasks(Ctx, false);
    }
}
