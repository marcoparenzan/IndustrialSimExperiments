using Opc.Ua;
using Opc.Ua.Server;

namespace OpcUaServerLib;

public class MyOpcUaServer(Func<IServerInternal, ApplicationConfiguration, CustomNodeManager2> createNodeManager) : StandardServer
{
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManager = createNodeManager(server, configuration);
        return new MasterNodeManager(server, configuration, null, [nodeManager]);
    }
}
