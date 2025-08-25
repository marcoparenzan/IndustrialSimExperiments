using Opc.Ua;
using Opc.Ua.Configuration;
using Opc.Ua.Server;

namespace OpcUaServerLib;

public class MyOpcUaServerHost
{
    private readonly ApplicationInstance _app;
    private readonly StandardServer _server;

    private MyOpcUaServerHost(ApplicationInstance app, StandardServer server)
    {
        _app = app;
        _server = server;
    }

    public static async Task<MyOpcUaServerHost> StartAsync(string name, string endpointUrl, Func<IServerInternal, ApplicationConfiguration ,CustomNodeManager2> createNodeManager)
    {
        var app = new ApplicationInstance
        {
            ApplicationName = $"{name} OPC UA Server",
            ApplicationType = ApplicationType.Server,
        };

        // Ensure certificate store folders exist
        var basePki = Path.Combine(AppContext.BaseDirectory, "pki");
        var ownPath = Path.Combine(basePki, "own");
        var trustedPath = Path.Combine(basePki, "trusted");
        var issuersPath = Path.Combine(basePki, "issuers");
        var rejectedPath = Path.Combine(basePki, "rejected");
        Directory.CreateDirectory(ownPath);
        Directory.CreateDirectory(trustedPath);
        Directory.CreateDirectory(issuersPath);
        Directory.CreateDirectory(rejectedPath);

        var config = new ApplicationConfiguration
        {
            ApplicationName = $"{name} OPC UA Server",
            ApplicationUri = $"urn:{Utils.GetHostName()}:{name}:Server",
            ApplicationType = ApplicationType.Server,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = ownPath,
                    SubjectName = $"CN={name} OPC UA Server"
                },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = trustedPath },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = issuersPath },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = rejectedPath },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ServerConfiguration = new ServerConfiguration
            {
                BaseAddresses = new StringCollection { endpointUrl },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.None, SecurityPolicyUri = SecurityPolicies.None }
                }
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            TraceConfiguration = new TraceConfiguration(),
            DisableHiResClock = false
        };

        await config.Validate(ApplicationType.Server);
        app.ApplicationConfiguration = config;

        bool haveCert = await app.CheckApplicationInstanceCertificate(true, 2048);
        if (!haveCert) throw new InvalidOperationException("Unable to create application certificate.");

        var server = new MyOpcUaServer(createNodeManager);
        await app.Start(server);

        return new MyOpcUaServerHost(app, server);
    }

    public Task StopAsync()
    {
        _app.Stop();
        return Task.CompletedTask;
    }
}
