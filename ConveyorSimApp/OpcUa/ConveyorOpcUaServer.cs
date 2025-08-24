using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using VFDSimLib;
using InductionMotorSimLib;
using ThreePhaseSupplySimLib;
using PackageSimLib;

namespace ConveyorSimApp.OpcUa;

public sealed class ConveyorOpcUaServerHost
{
    private readonly ApplicationInstance _app;
    private readonly ConveyorServer _server;

    public ConveyorNodeBindings Bindings => _server.NodeManager.Bindings;

    private ConveyorOpcUaServerHost(ApplicationInstance app, ConveyorServer server)
    {
        _app = app;
        _server = server;
    }

    public static async Task<ConveyorOpcUaServerHost> StartAsync(int segmentCount, string endpointUrl = "opc.tcp://localhost:4840/ConveyorSim")
    {
        var app = new ApplicationInstance
        {
            ApplicationName = "ConveyorSim OPC UA Server",
            ApplicationType = ApplicationType.Server,
        };

        // Ensure certificate store folders exist
        var basePki = Path.Combine(AppContext.BaseDirectory, "pki");
        var ownPath = Path.Combine(basePki, "own");
        var trustedPath = Path.Combine(basePki, "trusted");
        var issuersPath = Path.Combine(basePki, "issuers");
        var rejectedPath = Path.Combine(basePki, "rejected");
        Directory.CreateDirectory(ownPath);
        Directory.CreateDirectory(trustedPath);
        Directory.CreateDirectory(issuersPath);
        Directory.CreateDirectory(rejectedPath);

        var config = new ApplicationConfiguration
        {
            ApplicationName = "ConveyorSim OPC UA Server",
            ApplicationUri = $"urn:{Utils.GetHostName()}:ConveyorSim:Server",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = ownPath,
                    SubjectName = "CN=ConveyorSim OPC UA Server"
                },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = trustedPath },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = issuersPath },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = rejectedPath },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { endpointUrl },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }
                }
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            TraceConfiguration = new TraceConfiguration(),
            DisableHiResClock = false
        };

        await config.Validate(ApplicationType.Server);
        app.ApplicationConfiguration = config;

        bool haveCert = await app.CheckApplicationInstanceCertificate(true, 2048);
        if (!haveCert) throw new InvalidOperationException("Unable to create application certificate.");

        var server = new ConveyorServer(segmentCount);
        await app.Start(server);

        return new ConveyorOpcUaServerHost(app, server);
    }

    public Task StopAsync()
    {
        _app.Stop();
        return Task.CompletedTask;
    }
}

internal sealed class ConveyorServer : StandardServer
{
    private readonly int _segmentCount;
    public ConveyorNodeManager NodeManager { get; private set; } = default!;

    public ConveyorServer(int segmentCount) => _segmentCount = segmentCount;

    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        List<INodeManager> nodeManagers =
        [
            NodeManager = new ConveyorNodeManager(server, configuration, _segmentCount)
        ];
        return new MasterNodeManager(server, configuration, null, nodeManagers.ToArray());
    }
}

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

internal sealed class ConveyorNodeManager : CustomNodeManager2
{
    private readonly int _segments;
    public ConveyorNodeBindings Bindings { get; } = new();

    public ConveyorNodeManager(IServerInternal server, ApplicationConfiguration configuration, int segments)
        : base(server, configuration, "urn:ConveyorSim:NodeManager")
    {
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
        Bindings.Supply_LineLineVoltage = AddVar<double>(supply, "LineLineVoltage", DataTypeIds.Double);
        Bindings.Supply_Frequency = AddVar<double>(supply, "Frequency", DataTypeIds.Double);
        Bindings.Supply_TargetVoltageLL = AddVar<double>(supply, "TargetVoltageLL", DataTypeIds.Double);
        Bindings.Supply_TargetFrequency = AddVar<double>(supply, "TargetFrequency", DataTypeIds.Double);
        Bindings.Supply_AnUnderVoltage = AddVar<bool>(supply, "An_UnderVoltage", DataTypeIds.Boolean);
        Bindings.Supply_AnOverVoltage = AddVar<bool>(supply, "An_OverVoltage", DataTypeIds.Boolean);
        Bindings.Supply_AnFrequencyDrift = AddVar<bool>(supply, "An_FrequencyDrift", DataTypeIds.Boolean);

        // Segments
        var segments = AddFolder(conveyor, "Segments");
        Bindings.Segments = new ConveyorNodeBindings.SegmentBinding[_segments];
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

            Bindings.Segments[i] = sb;
        }

        // Packages
        var pkgs = AddFolder(conveyor, "Packages");
        Bindings.Pkg_Count = AddVar<int>(pkgs, "Count", DataTypeIds.Int32);
        Bindings.Pkg_Positions = AddArrayVar<double>(pkgs, "Positions", DataTypeIds.Double);
        Bindings.Pkg_Masses = AddArrayVar<double>(pkgs, "Masses", DataTypeIds.Double);

        // Save context for ClearChangeMasks
        Bindings.Ctx = SystemContext;
    }

    private FolderState AddFolder(NodeState parent, string name)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = name,
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, folder.NodeId);
        AddPredefinedNode(SystemContext, folder);
        return folder;
    }

    private BaseDataVariableState AddVar<T>(NodeState parent, string name, NodeId dataType)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = name,
            DataType = dataType,
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite,
            UserAccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite,
            Value = default(T)
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
        AddPredefinedNode(SystemContext, node);
        return node;
    }

    private BaseDataVariableState AddArrayVar<T>(NodeState parent, string name, NodeId dataType)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = name,
            DataType = dataType,
            ValueRank = ValueRanks.OneDimension,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = Array.Empty<T>()
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
        AddPredefinedNode(SystemContext, node);
        return node;
    }
}