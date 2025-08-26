using IndustrialSimLib;
using Opc.Ua;
using Opc.Ua.Server;
using System.Linq.Expressions;

namespace OpcUaServerLib;

public abstract class MyNodeManager: CustomNodeManager2
{
    public MyNodeManager(IServerInternal server, ApplicationConfiguration configuration, params string[] namespaceUris)
        : base(server, configuration, namespaceUris)
    {
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
}
