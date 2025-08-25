using Opc.Ua;
using Opc.Ua.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpcUaServerLib;

public class MyOpcUaServer(Func<IServerInternal, ApplicationConfiguration, CustomNodeManager2> createNodeManager) : StandardServer
{
    protected override MasterNodeManager CreateMasterNodeManager(IServerInternal server, ApplicationConfiguration configuration)
    {
        var nodeManager = createNodeManager(server, configuration);
        return new MasterNodeManager(server, configuration, null, [nodeManager]);
    }
}
