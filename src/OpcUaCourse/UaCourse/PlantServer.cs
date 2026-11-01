using Opc.Ua;
using Opc.Ua.Server;
namespace UaCourse;

internal sealed class PlantServer : StandardServer
{
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        => new(server, configuration, null, new PlantNodeManager(server, configuration));
    protected override ServerProperties LoadServerProperties() => new()
    {
        ManufacturerName = "UaCourse", ProductName = "Training Plant", ProductUri = "urn:uacourse",
        SoftwareVersion = "1.0.0", BuildNumber = "1", BuildDate = new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc)
    };
}

internal sealed partial class PlantNodeManager : CustomNodeManager2
{
    public const string NamespaceUri = "urn:uacourse:plant";
    private BaseObjectState reactor = null!;
    private BaseDataVariableState temperature = null!, setpoint = null!, running = null!;
    private Timer? timer;
    private long tick;
    private readonly List<BaseDataVariableState> waterSignals = [];
    public PlantNodeManager(IServerInternal server, ApplicationConfiguration configuration)
        : base(server, configuration, NamespaceUri) { SystemContext.NodeIdFactory = this; }
    public override NodeId New(ISystemContext context, NodeState node) => node.NodeId ?? new NodeId(Guid.NewGuid(), NamespaceIndex);

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        lock (Lock)
        {
            var reactorType = new BaseObjectTypeState
            {
                NodeId = Id("ReactorType"), BrowseName = Q("ReactorType"), DisplayName = "ReactorType",
                IsAbstract = false, SuperTypeId = ObjectTypeIds.BaseObjectType
            };
            reactorType.AddReference(ReferenceTypeIds.HasSubtype, true, ObjectTypeIds.BaseObjectType);
            if (!externalReferences.TryGetValue(ObjectTypeIds.BaseObjectType, out var types))
                externalReferences[ObjectTypeIds.BaseObjectType] = types = new List<IReference>();
            types.Add(new NodeStateReference(ReferenceTypeIds.HasSubtype, false, reactorType.NodeId));
            var declaration = Variable(reactorType, "ReactorType.Temperature", "Temperature", DataTypeIds.Double, 20.0);
            declaration.ModellingRuleId = ObjectIds.ModellingRule_Mandatory;
            declaration.AddReference(ReferenceTypeIds.HasModellingRule, false, ObjectIds.ModellingRule_Mandatory);
            AddPredefinedNode(SystemContext, reactorType);
            var plant = new FolderState(null)
            {
                NodeId = Id("Plant"), BrowseName = Q("Plant"), DisplayName = "Plant",
                TypeDefinitionId = ObjectTypeIds.FolderType, ReferenceTypeId = ReferenceTypeIds.Organizes
            };
            plant.AddReference(ReferenceTypeIds.Organizes, true, ObjectIds.ObjectsFolder);
            if (!externalReferences.TryGetValue(ObjectIds.ObjectsFolder, out var objects))
                externalReferences[ObjectIds.ObjectsFolder] = objects = new List<IReference>();
            objects.Add(new NodeStateReference(ReferenceTypeIds.Organizes, false, plant.NodeId));
            reactor = new BaseObjectState(plant)
            {
                NodeId = Id("Reactor1"), BrowseName = Q("Reactor1"), DisplayName = "Reactor1",
                TypeDefinitionId = reactorType.NodeId, ReferenceTypeId = ReferenceTypeIds.HasComponent,
                EventNotifier = EventNotifiers.SubscribeToEvents
            };
            plant.AddChild(reactor);
            AddRootNotifier(reactor);
            temperature = Variable(reactor, "Reactor1.Temperature", "Temperature", DataTypeIds.Double, 20.0);
            temperature.AddChild(new PropertyState<EUInformation>(temperature)
            {
                NodeId = Id("Reactor1.Temperature.EngineeringUnits"), BrowseName = BrowseNames.EngineeringUnits,
                DisplayName = "EngineeringUnits", ReferenceTypeId = ReferenceTypeIds.HasProperty,
                TypeDefinitionId = VariableTypeIds.PropertyType, DataType = DataTypeIds.EUInformation,
                ValueRank = ValueRanks.Scalar, AccessLevel = AccessLevels.CurrentRead, UserAccessLevel = AccessLevels.CurrentRead,
                Value = new EUInformation { NamespaceUri = "http://www.opcfoundation.org/UA/units/un/cefact", UnitId = 4408652,
                    DisplayName = "°C", Description = "degree Celsius" }
            });
            setpoint = Variable(reactor, "Reactor1.Setpoint", "Setpoint", DataTypeIds.Double, 60.0, true);
            setpoint.OnWriteValue = (ISystemContext context, NodeState node, NumericRange range, QualifiedName encoding,
                ref object value, ref StatusCode status, ref DateTime timestamp) =>
            {
                if (value is not double number) return StatusCodes.BadTypeMismatch;
                if (!double.IsFinite(number) || number < 20 || number > 90) return StatusCodes.BadOutOfRange;
                if (range != NumericRange.Empty) return StatusCodes.BadIndexRangeInvalid;
                timestamp = DateTime.UtcNow;
                return ServiceResult.Good;
            };
            running = Variable(reactor, "Reactor1.Running", "Running", DataTypeIds.Boolean, false);
            Variable(reactor, "Reactor1.Profile", "Profile", DataTypeIds.Double, new double[] { 20, 40, 60 }, rank: ValueRanks.OneDimension);
            Variable(reactor, "Reactor1.Recipe", "Recipe", DataTypeIds.Range,
                new ExtensionObject(new Opc.Ua.Range { Low = 20, High = 90 }));
            CreateRecipeModel(externalReferences);
            var start = Method("Start", [], [Arg("Accepted", DataTypeIds.Boolean)]);
            start.OnCallMethod = (ISystemContext ctx, MethodState method, IList<object> input, IList<object> output) =>
            {
                lock (Lock)
                {
                    if ((bool)running.Value) return StatusCodes.BadInvalidState;
                    Set(running, true); output[0] = true; Emit("Reattore avviato", 100);
                    return ServiceResult.Good;
                }
            };
            var stop = Method("Stop", [], []);
            stop.OnCallMethod = (ISystemContext ctx, MethodState method, IList<object> input, IList<object> output) =>
            {
                lock (Lock) { Set(running, false); Emit("Reattore arrestato", 200); return ServiceResult.Good; }
            };
            var apply = Method("ApplyRecipe", [Arg("Target", DataTypeIds.Double)], [Arg("Target", DataTypeIds.Double)]);
            apply.OnCallMethod = (ISystemContext ctx, MethodState method, IList<object> input, IList<object> output) =>
            {
                lock (Lock)
                {
                    var target = (double)input[0];
                    if (!double.IsFinite(target) || target < 20 || target > 90) return StatusCodes.BadOutOfRange;
                    Set(setpoint, target); output[0] = target; Emit($"Ricetta applicata: {target:F1} °C", 300);
                    return ServiceResult.Good;
                }
            };
            AddPredefinedNode(SystemContext, plant);
            var water = new BaseObjectState(plant)
            {
                NodeId = Id("WaterPlant"), BrowseName = Q("WaterPlant"), DisplayName = "WaterPlant",
                ReferenceTypeId = ReferenceTypeIds.HasComponent, TypeDefinitionId = ObjectTypeIds.BaseObjectType
            };
            plant.AddChild(water);
            foreach (var (name, initial) in new (string, double)[]
            {
                ("Influent", 5.2), ("FilterLevel", 5.1), ("Well1", 6.2), ("Well2", 7.3),
                ("Well4", 4.5), ("GST1", 35), ("GST2", 32.1), ("GST3", 32.2),
                ("Pressure", 95.3), ("LowPressure", 12.5), ("Distribution", 3650), ("Totalizer", 0),
                ("Filter1", 540.8), ("Filter2", 535.7), ("Filter3", 537.4), ("Filter4", 536.9),
                ("Filter5", 525.3), ("Filter6", 526.6), ("Filter7", 530.4), ("Filter8", 528.6)
            }) waterSignals.Add(Variable(water, $"WaterPlant.{name}", name, DataTypeIds.Double, initial));
            AddPredefinedNode(SystemContext, water);
            timer = new Timer(Update, null, 500, 500);
        }
    }
    private void Set(BaseDataVariableState node, object value)
    {
        node.Value = value; node.Timestamp = DateTime.UtcNow; node.StatusCode = StatusCodes.Good;
        node.ClearChangeMasks(SystemContext, false);
    }
    private void Update(object? state)
    {
        lock (Lock)
        {
            var target = (bool)running.Value ? (double)setpoint.Value : 20;
            var current = (double)temperature.Value;
            Set(temperature, current + (target - current) * 0.08);
            foreach (var signal in waterSignals)
            {
                var name = signal.BrowseName.Name;
                var value = (double)signal.Value;
                if (name == "Totalizer") Set(signal, value + ((bool)running.Value ? 30.4 : 0));
                else Set(signal, value + Math.Sin(tick * 0.2 + waterSignals.IndexOf(signal)) *
                    (name.StartsWith("Filter") && name != "FilterLevel" ? 0.3 : 0.015));
            }
            if (++tick % 4 == 0) Emit($"Temperatura: {(double)temperature.Value:F2} °C", 100);
        }
    }
    private void Emit(string message, ushort severity)
    {
        var ev = new BaseEventState(null);
        ev.Initialize(SystemContext, reactor, EventSeverity.Low, new LocalizedText("it", message));
        ev.Severity.Value = severity; ev.SourceNode.Value = reactor.NodeId; ev.SourceName.Value = "Reactor1";
        reactor.ReportEvent(SystemContext, ev);
    }
    private BaseDataVariableState Variable(NodeState parent, string id, string name, NodeId type, object value,
        bool writable = false, int rank = ValueRanks.Scalar)
    {
        var node = new BaseDataVariableState(parent)
        {
            NodeId = Id(id), BrowseName = Q(name), DisplayName = name,
            ReferenceTypeId = ReferenceTypeIds.HasComponent, TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            DataType = type, ValueRank = rank, Value = value, StatusCode = StatusCodes.Good,
            Timestamp = DateTime.UtcNow, AccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead,
            UserAccessLevel = writable ? AccessLevels.CurrentReadOrWrite : AccessLevels.CurrentRead
        };
        parent.AddChild(node); return node;
    }
    private MethodState Method(string name, Argument[] input, Argument[] output)
    {
        var method = new MethodState(reactor)
        {
            NodeId = Id($"Reactor1.{name}"), BrowseName = Q(name), DisplayName = name,
            ReferenceTypeId = ReferenceTypeIds.HasComponent, Executable = true, UserExecutable = true
        };
        method.InputArguments = Arguments(method, "InputArguments", input);
        method.OutputArguments = Arguments(method, "OutputArguments", output);
        reactor.AddChild(method); return method;
    }
    private PropertyState<Argument[]> Arguments(MethodState method, string name, Argument[] value) => new(method)
    {
        NodeId = Id($"{method.BrowseName.Name}.{name}"), BrowseName = new QualifiedName(name), DisplayName = name,
        ReferenceTypeId = ReferenceTypeIds.HasProperty, TypeDefinitionId = VariableTypeIds.PropertyType,
        DataType = DataTypeIds.Argument, ValueRank = ValueRanks.OneDimension,
        AccessLevel = AccessLevels.CurrentRead, UserAccessLevel = AccessLevels.CurrentRead, Value = value
    };
    private static Argument Arg(string name, NodeId type) => new() { Name = name, DataType = type, ValueRank = ValueRanks.Scalar, Description = name };
    private NodeId Id(string name) => new(name, NamespaceIndex);
    private QualifiedName Q(string name) => new(name, NamespaceIndex);
    protected override void Dispose(bool disposing)
    {
        if (disposing) { timer?.Dispose(); timer = null; }
        base.Dispose(disposing);
    }
}
