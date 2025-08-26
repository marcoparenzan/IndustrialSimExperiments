using IndustrialSimLib;
using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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
        parent.AddChild(folder); // make it an aggregated child so FindChild works
        return folder;
    }
}