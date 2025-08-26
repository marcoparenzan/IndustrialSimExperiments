using ConveyorSimApp.Models;
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

    public ConveyorNodeManager(IServerInternal server, ApplicationConfiguration configuration,  ThreePhaseSupplyState supplyState, ThreePhaseSupplyOutputs supplyOutputs, Segment[] segments, Package[] packages)
        : base(server, configuration, "urn:ConveyorSim:NodeManager")
    {
        this.supplyState = supplyState;
        this.supplyOutputs = supplyOutputs;
        this.segments = segments;
        this.packages = packages;
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        // Root folder under Objects
        var rootNode = new FolderState(null)
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
        refs.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, rootNode.NodeId));

        // Link Conveyor -> Objects (reverse reference)
        rootNode.AddReference(ReferenceTypeIds.Organizes, true, Objects.ObjectsFolder);

        // Register node
        AddPredefinedNode(SystemContext, rootNode);

        // Supply
        var supply = rootNode.AddFolder("Supply");
        supply.AddVar(supplyOutputs, xx => xx.LineLineVoltage);
        supply.AddVar(supplyOutputs, xx => xx.Frequency);
        supply.AddVar(supplyState, xx => xx.TargetVoltageLL);
        supply.AddVar(supplyState, xx => xx.TargetFrequency);
        supply.AddVar(supplyState, xx => xx.An_UnderVoltage);
        supply.AddVar(supplyState, xx => xx.An_OverVoltage);
        supply.AddVar(supplyState, xx => xx.An_FrequencyDrift);

        // Segments
        var segments = rootNode.AddFolder("Segments");
        for (int i = 0; i < this.segments.Length; i++)
        {
            var segmentObject = this.segments[i];
            var seg = segments.AddFolder($"Segment[{i}]");
            var vfd = seg.AddFolder("Vfd");
            var vfdState = vfd.AddFolder("State");
            var vfdIn = vfd.AddFolder("Inputs");
            var vfdOut = vfd.AddFolder("Outputs");

            var mot = seg.AddFolder("Motor");
            var motState = mot.AddFolder("State");
            var motIn = mot.AddFolder("Inputs");
            var motOut = mot.AddFolder("Outputs");

            vfdState.AddVar(segmentObject.VfdState, xx => xx.TargetFrequency);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.BusVoltage);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.HeatsinkTemp);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.An_UnderVoltage);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.An_OverVoltage);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.An_PhaseLoss);
            vfdState.AddVar(segmentObject.VfdState, xx => xx.An_GroundFault);

            vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.SupplyVoltageLL);
            vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.SupplyFrequency);
            vfdIn.AddVar(segmentObject.VfdInputs, xx => xx.MotorCurrentFeedback);

            vfdOut.AddVar(segmentObject.VfdOutputs, xx => xx.OutputFrequency);
            vfdOut.AddVar(segmentObject.VfdOutputs, xx => xx.OutputVoltage);

            motState.AddVar(segmentObject.MotorState, xx => xx.SpeedRpm);
            motState.AddVar(segmentObject.MotorState, xx => xx.ElectTorque);
            motState.AddVar(segmentObject.MotorState, xx => xx.Trated);
            motState.AddVar(segmentObject.MotorState, xx => xx.VratedPhPh);
            motState.AddVar(segmentObject.MotorState, xx => xx.An_PhaseLoss);
            motState.AddVar(segmentObject.MotorState, xx => xx.An_LoadJam);
            motState.AddVar(segmentObject.MotorState, xx => xx.An_BearingWear);
            motState.AddVar(segmentObject.MotorState, xx => xx.An_SensorNoise);

            motIn.AddVar(segmentObject.MotorInputs, xx => xx.DriveFrequencyCmd);
            motIn.AddVar(segmentObject.MotorInputs, xx => xx.DriveVoltageCmd);

            motOut.AddVar(segmentObject.MotorOutputs, xx => xx.PhaseCurrent);
        }

        // Packages
        var pkgs = rootNode.AddFolder("Packages");
        pkgs.AddVar<int>("Count", DataTypeIds.Int32);
        pkgs.AddArrayVar<double>("Positions");
        pkgs.AddArrayVar<double>("Masses");

        var supplyNode = rootNode.FindChildBySymbolicName(SystemContext, "Segments/Segment[1]/Vfd/State/BusVoltage");

        AddPredefinedNode(SystemContext, rootNode);
    }
}