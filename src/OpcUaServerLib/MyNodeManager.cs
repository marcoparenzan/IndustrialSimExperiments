using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaServerLib;

public abstract class MyNodeManager : CustomNodeManager2
{
    public MyNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris)
        : base(server, configuration, namespaceUris)
    {
        SystemContext.NodeIdFactory = this;
    }

    protected FolderState AddFolder(NodeState parent, string name)
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

    protected BaseDataVariableState AddVar<T>(NodeState parent, string name, NodeId dataType)
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

    protected BaseDataVariableState AddArrayVar<T>(NodeState parent, string name, NodeId dataType)
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