using IndustrialSimLib;
using Opc.Ua;
using Opc.Ua.Server;

namespace OpcUaServerLib;

public sealed class OpcUaProtocolAdapter(string endpointUrl) : IProtocolAdapter
{
    private readonly Dictionary<string, BaseDataVariableState> nodes = new(StringComparer.Ordinal);
    private MyOpcUaServerHost? host;
    private MyNodeManager? manager;
    private IMachineModule? machine;

    public string Name => "OpcUa";

    public async Task StartAsync(IMachineModule machine, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(machine);
        if (host is not null) throw new InvalidOperationException("The OPC UA adapter is already running.");
        this.machine = machine;

        host = await MyOpcUaServerHost.StartAsync(machine.Name, endpointUrl, (server, configuration) =>
        {
            manager = new MyNodeManager(server, configuration, machine.Name,
                $"urn:IndustrialSim:{machine.Name}", BuildAddressSpace);
            manager.SystemContext.NodeIdFactory = manager;
            return manager;
        });
    }

    public Task PublishAsync(CancellationToken cancellationToken = default)
    {
        if (machine is null || manager is null)
            throw new InvalidOperationException("The OPC UA adapter has not been started.");

        foreach (var tag in machine.Tags)
        {
            if (!nodes.TryGetValue(tag.Path, out var node)) continue;
            node.Value = tag.Value;
            node.Timestamp = DateTime.UtcNow;
            node.ClearChangeMasks(manager.SystemContext, false);
        }
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (host is null) return;
        await host.StopAsync();
        host = null;
        manager = null;
        machine = null;
        nodes.Clear();
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private void BuildAddressSpace(NodeState root)
    {
        if (machine is null) return;
        var folders = new Dictionary<string, NodeState>(StringComparer.Ordinal) { [string.Empty] = root };
        foreach (var tag in machine.Tags)
        {
            string[] parts = tag.Path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            NodeState parent = root;
            string folderPath = string.Empty;
            for (int index = 0; index < parts.Length - 1; index++)
            {
                folderPath = string.IsNullOrEmpty(folderPath) ? parts[index] : $"{folderPath}.{parts[index]}";
                if (!folders.TryGetValue(folderPath, out var folder))
                {
                    folder = parent.AddFolder(parts[index]);
                    folders.Add(folderPath, folder);
                }
                parent = folder;
            }
            nodes.Add(tag.Path, CreateVariable(parent, parts[^1], tag));
        }
    }

    private static BaseDataVariableState CreateVariable(NodeState parent, string name, SimulationTag tag)
    {
        bool isArray = tag.DataType.IsArray;
        Type elementType = isArray ? tag.DataType.GetElementType()! : tag.DataType;
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.NodeId.Identifier}.{name}", parent.NodeId.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
            DataType = OpcDataType(elementType),
            ValueRank = isArray ? ValueRanks.OneDimension : ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = tag.Value
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
        parent.AddChild(node);
        return node;
    }

    private static NodeId OpcDataType(Type type) => type == typeof(bool) ? DataTypeIds.Boolean
        : type == typeof(int) ? DataTypeIds.Int32
        : type == typeof(long) ? DataTypeIds.Int64
        : type == typeof(float) ? DataTypeIds.Float
        : type == typeof(double) ? DataTypeIds.Double
        : type == typeof(string) ? DataTypeIds.String
        : throw new NotSupportedException($"OPC UA type not supported: {type.FullName}");
}
