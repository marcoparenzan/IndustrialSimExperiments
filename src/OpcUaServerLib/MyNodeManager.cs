using IndustrialSimLib;
using Opc.Ua;
using Opc.Ua.Server;

namespace ConveyorSimApp.OpcUa;

public class MyNodeManager : CustomNodeManager2
{
    private string name;
    private Action<NodeState> builder;

    public MyNodeManager(IServerInternal server, ApplicationConfiguration configuration, string name, string namespaceUri, Action<NodeState> builder)
        : base(server, configuration, namespaceUri)
    {
        this.name = name;
        this.builder = builder;
        SystemContext.NodeIdFactory = this;
    }

    public void UpdateDoubleBindable(DoubleBindable bindable, double? value = default)
    {
        var bound = (BaseDataVariableState)bindable.Bounded;
        bound.Value = value ?? bindable.Value;
        bound.ClearChangeMasks(SystemContext, false);
    }

    public void UpdateBoolBindable(BoolBindable bindable, bool? value = default)
    {
        var bound = (BaseDataVariableState)bindable.Bounded;
        bound.Value = value ?? bindable.Value;
        bound.ClearChangeMasks(SystemContext, false);
    }

    public override void CreateAddressSpace(IDictionary<NodeId, IList<IReference>> externalReferences)
    {
        // Root folder under Objects
        var rootNode = new FolderState(null)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId(name, NamespaceIndex),
            BrowseName = new QualifiedName(name, NamespaceIndex),
            DisplayName = name,
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

        builder(rootNode);

        AddPredefinedNode(SystemContext, rootNode);
    }
}