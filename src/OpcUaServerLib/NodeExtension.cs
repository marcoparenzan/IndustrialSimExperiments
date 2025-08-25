using IndustrialSimLib;
using Opc.Ua;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaServerLib;

public static class NodeExtension
{
    public static BaseDataVariableState AddVar<T>(this NodeState parent, string name, NodeId dataType)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", parent.BrowseName.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
            DataType = dataType,
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

    public static BaseDataVariableState AddVar<TTarget, TProperty>(this NodeState parent, TTarget target, Expression<Func<TTarget, IBindable<TProperty>>> selector, NodeId dataType)
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

    public static BaseDataVariableState AddArrayVar<T>(this NodeState parent, string name, NodeId dataType)
    {
        var node = new BaseDataVariableState(parent)
        {
            SymbolicName = name,
            ReferenceTypeId = ReferenceTypeIds.Organizes,
            TypeDefinitionId = VariableTypeIds.BaseDataVariableType,
            NodeId = new NodeId($"{parent.BrowseName.Name}.{name}", parent.BrowseName.NamespaceIndex),
            BrowseName = new QualifiedName(name, parent.BrowseName.NamespaceIndex),
            DisplayName = name,
            DataType = dataType,
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

}
