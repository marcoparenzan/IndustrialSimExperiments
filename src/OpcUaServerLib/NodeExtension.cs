using IndustrialSimLib;
using Opc.Ua;
using System.Linq.Expressions;

namespace OpcUaServerLib;

public static class NodeExtension
{
    public static BaseDataVariableState AddVar<T>(this NodeState parent, string name, NodeId? dataType = default)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", parent.BrowseName.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
            DataType = TypeOf<T>(dataType),
            ValueRank = ValueRanks.Scalar,
            AccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite,
            UserAccessLevel = AccessLevels.CurrentRead | AccessLevels.CurrentWrite,
            Value = default(T)
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
        parent.AddChild(node); // ensure it’s visible to FindChild
        //AddPredefinedNode(SystemContext, node);
        return node;
    }

    private static NodeId TypeOf<T>(NodeId? dataType = default)
    {
        if (dataType is not null) return dataType;
        return typeof(T) switch
        {
            Type t when t == typeof(bool) => DataTypeIds.Boolean,
            Type t when t == typeof(sbyte) => DataTypeIds.SByte,
            Type t when t == typeof(byte) => DataTypeIds.Byte,
            Type t when t == typeof(short) => DataTypeIds.Int16,
            Type t when t == typeof(ushort) => DataTypeIds.UInt16,
            Type t when t == typeof(int) => DataTypeIds.Int32,
            Type t when t == typeof(uint) => DataTypeIds.UInt32,
            Type t when t == typeof(long) => DataTypeIds.Int64,
            Type t when t == typeof(ulong) => DataTypeIds.UInt64,
            Type t when t == typeof(float) => DataTypeIds.Float,
            Type t when t == typeof(double) => DataTypeIds.Double,
            Type t when t == typeof(string) => DataTypeIds.String,
            Type t when t == typeof(DateTime) => DataTypeIds.DateTime,
            Type t when t == typeof(Guid) => DataTypeIds.Guid,
            Type t when t == typeof(byte[]) => DataTypeIds.ByteString,
            _ => throw new ArgumentException($"Unsupported type: {typeof(T).FullName}")
        };
    }

    public static BaseDataVariableState AddVar<TTarget, TProperty>(this NodeState parent, TTarget target, Expression<Func<TTarget, IBindable<TProperty>>> selector, NodeId? dataType = default)
    {
        var name = NameOf(selector);

        var state = AddVar<TProperty>(parent, name, dataType);

        var bindable = selector.Compile()(target);
        bindable.Bounded = state;

        return state;
    }

    private static string NameOf<TTarget, TProperty>(Expression<Func<TTarget, IBindable<TProperty>>> selector)
    {
        var name = "";
        if (selector.Body is MemberExpression memberExpr)
        {
            name = memberExpr.Member.Name;
        }
        else if (selector.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression memberExpr2)
        {
            name = memberExpr2.Member.Name;
        }
        else
        {
            throw new ArgumentException("Selector must be a member expression", nameof(selector));
        }

        return name;
    }

    public static BaseDataVariableState AddArrayVar<T>(this NodeState parent, string name, NodeId? dataType = default)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", parent.BrowseName.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
            DataType = TypeOf<T>(dataType),
            ValueRank = ValueRanks.OneDimension,
            AccessLevel = AccessLevels.CurrentRead,
            UserAccessLevel = AccessLevels.CurrentRead,
            Value = Array.Empty<T>()
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, node.NodeId);
        parent.AddChild(node); // ensure it’s visible to FindChild
        //AddPredefinedNode(SystemContext, node);
        return node;
    }

    //public static BaseDataVariableState AddArrayVar<TTarget, TProperty>(this NodeState parent, TTarget[] target, Expression<Func<TTarget, IBindable<TProperty>>> selector, NodeId dataType)
    //{
    //    var name = NameOf(selector);

    //    var name = memberExpr.Member.Name;
    //    var state = AddArrayVar<TProperty>(parent, name, dataType);

    //    var bindable = selector.Compile()(target);
    //    bindable.Bounded = state;

    //    return state;
    //}
    public static FolderState AddFolder(this NodeState parent, string name)
    {
        var folder = new FolderState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = ObjectTypeIds.FolderType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", parent.BrowseName.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
        };
        parent.AddReference(ReferenceTypeIds.Organizes, false, folder.NodeId);
        parent.AddChild(folder); // make it an aggregated child so FindChild works
        return folder;
    }

}
