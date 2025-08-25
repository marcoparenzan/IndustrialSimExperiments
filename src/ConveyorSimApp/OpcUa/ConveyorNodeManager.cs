using Opc.Ua;
using Opc.Ua.Server;
using OpcUaServerLib;

namespace ConveyorSimApp.OpcUa;

internal sealed class ConveyorNodeManager : MyNodeManager
{
    private readonly int _segments;

    ConveyorNodeBindings bindings;

    public ConveyorNodeManager(IServerInternal server, ApplicationConfiguration configuration, ConveyorNodeBindings xxbindings, int segments)
        : base(server, configuration, "urn:ConveyorSim:NodeManager")
    {
        bindings = xxbindings;
        _segments = segments;
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
        bindings.Supply_LineLineVoltage = AddVar<double>(supply, "LineLineVoltage", DataTypeIds.Double);
        bindings.Supply_Frequency = AddVar<double>(supply, "Frequency", DataTypeIds.Double);
        bindings.Supply_TargetVoltageLL = AddVar<double>(supply, "TargetVoltageLL", DataTypeIds.Double);
        bindings.Supply_TargetFrequency = AddVar<double>(supply, "TargetFrequency", DataTypeIds.Double);
        bindings.Supply_AnUnderVoltage = AddVar<bool>(supply, "An_UnderVoltage", DataTypeIds.Boolean);
        bindings.Supply_AnOverVoltage = AddVar<bool>(supply, "An_OverVoltage", DataTypeIds.Boolean);
        bindings.Supply_AnFrequencyDrift = AddVar<bool>(supply, "An_FrequencyDrift", DataTypeIds.Boolean);

        // Segments
        var segments = AddFolder(conveyor, "Segments");
        bindings.Segments = new ConveyorNodeBindings.SegmentBinding[_segments];
        for (int i = 0; i < _segments; i++)
        {
            var seg = AddFolder(segments, $"Segment{i}");
            var vfd = AddFolder(seg, "VFD");
            var vfdState = AddFolder(vfd, "State");
            var vfdIn = AddFolder(vfd, "Inputs");
            var vfdOut = AddFolder(vfd, "Outputs");

            var mot = AddFolder(seg, "Motor");
            var motState = AddFolder(mot, "State");
            var motIn = AddFolder(mot, "Inputs");
            var motOut = AddFolder(mot, "Outputs");

            var sb = new ConveyorNodeBindings.SegmentBinding
            {
                Vfd_TargetFrequency = AddVar<double>(vfdState, "TargetFrequency", DataTypeIds.Double),
                Vfd_BusVoltage = AddVar<double>(vfdState, "BusVoltage", DataTypeIds.Double),
                Vfd_HeatsinkTemp = AddVar<double>(vfdState, "HeatsinkTemp", DataTypeIds.Double),
                Vfd_AnUnderVoltage = AddVar<bool>(vfdState, "An_UnderVoltage", DataTypeIds.Boolean),
                Vfd_AnOverVoltage = AddVar<bool>(vfdState, "An_OverVoltage", DataTypeIds.Boolean),
                Vfd_AnPhaseLoss = AddVar<bool>(vfdState, "An_PhaseLoss", DataTypeIds.Boolean),
                Vfd_AnGroundFault = AddVar<bool>(vfdState, "An_GroundFault", DataTypeIds.Boolean),

                VfdIn_SupplyVoltageLL = AddVar<double>(vfdIn, "SupplyVoltageLL", DataTypeIds.Double),
                VfdIn_SupplyFrequency = AddVar<double>(vfdIn, "SupplyFrequency", DataTypeIds.Double),
                VfdIn_MotorCurrentFeedback = AddVar<double>(vfdIn, "MotorCurrentFeedback", DataTypeIds.Double),

                VfdOut_OutputFrequency = AddVar<double>(vfdOut, "OutputFrequency", DataTypeIds.Double),
                VfdOut_OutputVoltage = AddVar<double>(vfdOut, "OutputVoltage", DataTypeIds.Double),

                Mot_SpeedRpm = AddVar<double>(motState, "SpeedRpm", DataTypeIds.Double),
                Mot_ElectTorque = AddVar<double>(motState, "ElectTorque", DataTypeIds.Double),
                Mot_Trated = AddVar<double>(motState, "Trated", DataTypeIds.Double),
                Mot_VratedPhPh = AddVar<double>(motState, "VratedPhPh", DataTypeIds.Double),
                Mot_AnPhaseLoss = AddVar<bool>(motState, "An_PhaseLoss", DataTypeIds.Boolean),
                Mot_AnLoadJam = AddVar<bool>(motState, "An_LoadJam", DataTypeIds.Boolean),
                Mot_AnBearingWear = AddVar<bool>(motState, "An_BearingWear", DataTypeIds.Boolean),
                Mot_AnSensorNoise = AddVar<bool>(motState, "An_SensorNoise", DataTypeIds.Boolean),

                MotIn_DriveFrequencyCmd = AddVar<double>(motIn, "DriveFrequencyCmd", DataTypeIds.Double),
                MotIn_DriveVoltageCmd = AddVar<double>(motIn, "DriveVoltageCmd", DataTypeIds.Double),

                MotOut_PhaseCurrent = AddVar<double>(motOut, "PhaseCurrent", DataTypeIds.Double),
            };

            bindings.Segments[i] = sb;
        }

        // Packages
        var pkgs = AddFolder(conveyor, "Packages");
        bindings.Pkg_Count = AddVar<int>(pkgs, "Count", DataTypeIds.Int32);
        bindings.Pkg_Positions = AddArrayVar<double>(pkgs, "Positions", DataTypeIds.Double);
        bindings.Pkg_Masses = AddArrayVar<double>(pkgs, "Masses", DataTypeIds.Double);

        // Save context for ClearChangeMasks
        bindings.Ctx = SystemContext;
    }
}