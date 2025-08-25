using InductionMotorSimLib;
using Opc.Ua;
using PackageSimLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreePhaseSupplySimLib;
using VFDSimLib;

namespace ConveyorSimApp.OpcUa;

public sealed class ConveyorNodeBindings
{
    internal ISystemContext Ctx = default!;

    // Supply
    public BaseDataVariableState Supply_LineLineVoltage = default!;
    public BaseDataVariableState Supply_Frequency = default!;
    public BaseDataVariableState Supply_TargetVoltageLL = default!;
    public BaseDataVariableState Supply_TargetFrequency = default!;
    public BaseDataVariableState Supply_AnUnderVoltage = default!;
    public BaseDataVariableState Supply_AnOverVoltage = default!;
    public BaseDataVariableState Supply_AnFrequencyDrift = default!;

    // Per-segment bindings
    public sealed class SegmentBinding
    {
        // VFD State
        public BaseDataVariableState Vfd_TargetFrequency = default!;
        public BaseDataVariableState Vfd_BusVoltage = default!;
        public BaseDataVariableState Vfd_HeatsinkTemp = default!;
        public BaseDataVariableState Vfd_AnUnderVoltage = default!;
        public BaseDataVariableState Vfd_AnOverVoltage = default!;
        public BaseDataVariableState Vfd_AnPhaseLoss = default!;
        public BaseDataVariableState Vfd_AnGroundFault = default!;

        // VFD Inputs
        public BaseDataVariableState VfdIn_SupplyVoltageLL = default!;
        public BaseDataVariableState VfdIn_SupplyFrequency = default!;
        public BaseDataVariableState VfdIn_MotorCurrentFeedback = default!;

        // VFD Outputs
        public BaseDataVariableState VfdOut_OutputFrequency = default!;
        public BaseDataVariableState VfdOut_OutputVoltage = default!;

        // Motor State
        public BaseDataVariableState Mot_SpeedRpm = default!;
        public BaseDataVariableState Mot_ElectTorque = default!;
        public BaseDataVariableState Mot_Trated = default!;
        public BaseDataVariableState Mot_VratedPhPh = default!;
        public BaseDataVariableState Mot_AnPhaseLoss = default!;
        public BaseDataVariableState Mot_AnLoadJam = default!;
        public BaseDataVariableState Mot_AnBearingWear = default!;
        public BaseDataVariableState Mot_AnSensorNoise = default!;

        // Motor Inputs
        public BaseDataVariableState MotIn_DriveFrequencyCmd = default!;
        public BaseDataVariableState MotIn_DriveVoltageCmd = default!;

        // Motor Outputs
        public BaseDataVariableState MotOut_PhaseCurrent = default!;
    }

    public SegmentBinding[] Segments = default!;

    // Packages
    public BaseDataVariableState Pkg_Count = default!;
    public BaseDataVariableState Pkg_Positions = default!; // double[]
    public BaseDataVariableState Pkg_Masses = default!;    // double[]

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
