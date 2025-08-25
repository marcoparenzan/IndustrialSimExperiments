using Opc.Ua;
using Opc.Ua.Server;
using OpcUaServerLib;
using PackageSimLib;
using ThreePhaseSupplySimLib;

namespace ConveyorSimApp.OpcUa;

internal sealed class ConveyorNodeManager : MyNodeManager
{
    ThreePhaseSupplyState supplyState;
    ThreePhaseSupplyOutputs supplyOutputs;
    Segment[] segments;
    Package[] packages;

    ConveyorNodeBindings bindings;

    public ConveyorNodeManager(IServerInternal server, ApplicationConfiguration configuration, ConveyorNodeBindings bindings, ThreePhaseSupplyState supplyState, ThreePhaseSupplyOutputs supplyOutputs, Segment[] segments, Package[] packages)
        : base(server, configuration, "urn:ConveyorSim:NodeManager")
    {
        this.bindings = bindings;
        this.supplyState = supplyState;
        this.supplyOutputs = supplyOutputs;
        this.segments = segments;
        this.packages = packages;
        SystemContext.NodeIdFactory = this;
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        // Root folder under Objects
        var conveyor = new FolderState(null)
        {
            SymbolicName = "Conveyor",
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId("Conveyor", NamespaceIndex),
            BrowseName = new QualifiedName("Conveyor", NamespaceIndex),
            DisplayName = "Conveyor",
            WriteMask = 0,
            UserWriteMask = 0
        };

        // Link Objects -> Conveyor (forward reference) so clients can browse from Objects
        if (!externalReferences.TryGetValue(Objects.ObjectsFolder, out var refs))
        {
            refs = new List<IReference>();
            externalReferences[Objects.ObjectsFolder] = refs;
        }
        refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, conveyor.NodeId));

        // Link Conveyor -> Objects (reverse reference)
        conveyor.AddReference(ReferenceTypeIds.Organizes, true, Objects.ObjectsFolder);

        // Register node
        AddPredefinedNode(SystemContext, conveyor);

        // Supply
        var supply = AddFolder(conveyor, "Supply");
        bindings.Supply_LineLineVoltage = supply.AddVar(supplyOutputs, xx => xx.LineLineVoltage, DataTypeIds.Double);
        bindings.Supply_Frequency = supply.AddVar(supplyOutputs, xx => xx.Frequency, DataTypeIds.Double);
        bindings.Supply_TargetVoltageLL = supply.AddVar(supplyState, xx => xx.TargetVoltageLL, DataTypeIds.Double);
        bindings.Supply_TargetFrequency = supply.AddVar(supplyState, xx => xx.TargetFrequency, DataTypeIds.Double);
        bindings.Supply_AnUnderVoltage = supply.AddVar(supplyState, xx => xx.An_UnderVoltage, DataTypeIds.Boolean);
        bindings.Supply_AnOverVoltage = supply.AddVar(supplyState, xx => xx.An_OverVoltage, DataTypeIds.Boolean);
        bindings.Supply_AnFrequencyDrift = supply.AddVar(supplyState, xx => xx.An_FrequencyDrift, DataTypeIds.Boolean);

        // Segments
        var segments = AddFolder(conveyor, "Segments");
        bindings.Segments = new ConveyorNodeBindings.SegmentBinding[this.segments.Length];
        for (int i = 0; i < this.segments.Length; i++)
        {
            var segmentObject = this.segments[i];
            var seg = AddFolder(segments, $"Segment[{i}]");
            var vfd = AddFolder(seg, "Vfd");
            var vfdState = AddFolder(vfd, "State");
            var vfdIn = AddFolder(vfd, "Inputs");
            var vfdOut = AddFolder(vfd, "Outputs");

            var mot = AddFolder(seg, "Motor");
            var motState = AddFolder(mot, "State");
            var motIn = AddFolder(mot, "Inputs");
            var motOut = AddFolder(mot, "Outputs");

            var sb = new ConveyorNodeBindings.SegmentBinding
            {
                Vfd_TargetFrequency = vfdState.AddVar(segmentObject, xx => xx.VfdState.TargetFrequency, DataTypeIds.Double),
                Vfd_BusVoltage = vfdState.AddVar(segmentObject, xx => xx.VfdState.BusVoltage, DataTypeIds.Double),
                Vfd_HeatsinkTemp = vfdState.AddVar(segmentObject, xx => xx.VfdState.HeatsinkTemp, DataTypeIds.Double),
                Vfd_AnUnderVoltage = vfdState.AddVar(segmentObject, xx => xx.VfdState.An_UnderVoltage, DataTypeIds.Boolean),
                Vfd_AnOverVoltage = vfdState.AddVar(segmentObject, xx => xx.VfdState.An_OverVoltage, DataTypeIds.Boolean),
                Vfd_AnPhaseLoss = vfdState.AddVar(segmentObject, xx => xx.VfdState.An_PhaseLoss, DataTypeIds.Boolean),
                Vfd_AnGroundFault = vfdState.AddVar(segmentObject, xx => xx.VfdState.An_GroundFault, DataTypeIds.Boolean),

                VfdIn_SupplyVoltageLL = vfdIn.AddVar(segmentObject, xx => xx.VfdInputs.SupplyVoltageLL, DataTypeIds.Double),
                VfdIn_SupplyFrequency = vfdIn.AddVar(segmentObject, xx => xx.VfdInputs.SupplyFrequency, DataTypeIds.Double),
                VfdIn_MotorCurrentFeedback = vfdIn.AddVar(segmentObject, xx => xx.VfdInputs.MotorCurrentFeedback, DataTypeIds.Double),

                VfdOut_OutputFrequency = vfdOut.AddVar(segmentObject, xx => xx.VfdOutputs.OutputFrequency, DataTypeIds.Double),
                VfdOut_OutputVoltage = vfdOut.AddVar(segmentObject, xx => xx.VfdOutputs.OutputVoltage, DataTypeIds.Double),

                Mot_SpeedRpm = motState.AddVar(segmentObject, xx => xx.MotorState.SpeedRpm, DataTypeIds.Double),
                Mot_ElectTorque = motState.AddVar(segmentObject, xx => xx.MotorState.ElectTorque, DataTypeIds.Double),
                Mot_Trated = motState.AddVar(segmentObject, xx => xx.MotorState.Trated, DataTypeIds.Double),
                Mot_VratedPhPh = motState.AddVar(segmentObject, xx => xx.MotorState.VratedPhPh, DataTypeIds.Double),
                Mot_AnPhaseLoss = motState.AddVar(segmentObject, xx => xx.MotorState.An_PhaseLoss, DataTypeIds.Boolean),
                Mot_AnLoadJam = motState.AddVar(segmentObject, xx => xx.MotorState.An_LoadJam, DataTypeIds.Boolean),
                Mot_AnBearingWear = motState.AddVar(segmentObject, xx => xx.MotorState.An_BearingWear, DataTypeIds.Boolean),
                Mot_AnSensorNoise = motState.AddVar(segmentObject, xx => xx.MotorState.An_SensorNoise, DataTypeIds.Boolean),

                MotIn_DriveFrequencyCmd = motIn.AddVar(segmentObject, xx => xx.MotorInputs.DriveFrequencyCmd, DataTypeIds.Double),
                MotIn_DriveVoltageCmd = motIn.AddVar(segmentObject, xx => xx.MotorInputs.DriveVoltageCmd, DataTypeIds.Double),

                MotOut_PhaseCurrent = motOut.AddVar(segmentObject, xx => xx.MotorOutputs.PhaseCurrent, DataTypeIds.Double),
            };

            bindings.Segments[i] = sb;
        }

        // Packages
        var pkgs = AddFolder(conveyor, "Packages");
        bindings.Pkg_Count = pkgs.AddVar<int>("Count", DataTypeIds.Int32);
        bindings.Pkg_Positions = pkgs.AddArrayVar<double>("Positions", DataTypeIds.Double);
        bindings.Pkg_Masses = pkgs.AddArrayVar<double>("Masses", DataTypeIds.Double);

        var supplyNode = conveyor.FindChildBySymbolicName(SystemContext, "Segments/Segment[1]/Vfd/State/BusVoltage");

        // Save context for ClearChangeMasks
        bindings.Ctx = SystemContext;
    }
}