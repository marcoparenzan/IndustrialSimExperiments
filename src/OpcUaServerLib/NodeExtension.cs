using IndustrialSimLib;
using Opc.Ua;
using System.Linq.Expressions;
using System.Reflection;

namespace OpcUaServerLib;

public static class NodeExtension
{
    public static NodeState AddVar<T>(this NodeState parent, string name, NodeId? dataType = default)
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

    public static NodeState AddVar<TTarget, TProperty>(this NodeState parent, TTarget target, Expression<Func<TTarget, IBindable<TProperty>>> selector, NodeId? dataType = default)
    {
        var memberExpr = MemberExpressionOf(selector);
        var name = memberExpr.Member.Name;

        var state = AddVar<TProperty>(parent, name, dataType);

        var bindable = selector.Compile()(target);
        bindable.Bounded = state;

        //var pi = (PropertyInfo)memberExpr.Member;

        //if (pi.CanWrite)
        //{
        //    // Assign concrete bindable instance (works for class or struct when a setter exists)
        //    pi.SetValue(target, bindable);
        //}
        //else if (typeof(IBindable<TProperty>).IsAssignableFrom(pi.PropertyType))
        //{
        //    var xx = (IBindable<TProperty>?)pi.GetValue(target);
        //    xx.Bounded = bindable;
        //    //if (pi.PropertyType.IsValueType)
        //    //{
        //    //    // Value type + no setter: attempt to write the backing field with the boxed struct instance.
        //    //    // NOTE: This may fail on .NET 5+/.NET 9 for readonly (init-only) backing fields.
        //    //    var backingField = pi.DeclaringType!.GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        //    //    if (backingField is null)
        //    //        throw new InvalidOperationException($"Backing field for {pi.DeclaringType!.Name}.{name} not found.");

        //    //    if (!pi.PropertyType.IsInstanceOfType(bindable))
        //    //        throw new InvalidOperationException($"Bindable instance is not assignable to property type {pi.PropertyType}.");

        //    //    // Effectively: Set(target, bindable) — write the struct back so the Bounded assignment is retained.
        //    //    backingField.SetValue(target, bindable);
        //    //}
        //    //else
        //    //{
        //    //    throw new InvalidOperationException($"Cannot set bindable for property {name}");
        //    //}
        //}
        //else
        //{
        //    throw new InvalidOperationException($"Cannot set bindable for property {name}");
        //}

        return parent;
    }

    private static MemberExpression MemberExpressionOf<TTarget, TProperty>(Expression<Func<TTarget, IBindable<TProperty>>> selector)
    {
        if (selector.Body is MemberExpression memberExpr)
        {
            return memberExpr;
        }
        else if (selector.Body is UnaryExpression unaryExpr && unaryExpr.Operand is MemberExpression memberExpr2)
        {
            return memberExpr2;
        }
        else
        {
            throw new ArgumentException("Selector must be a member expression", nameof(selector));
        }
    }

    public static NodeState AddArrayVar<T>(this NodeState parent, string name, NodeId? dataType = default)
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

        return parent;
    }

    public static NodeState AddFolder(this NodeState parent, string name)
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
