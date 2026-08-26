using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Sampling;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;

namespace ForgeDataGatewayCore.OpcUa;

public sealed class OpcUaSourceClient : ISourceClient
{
    private Session? session;

    public string Protocol => "OpcUa";

    public async Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (session is not null) throw new InvalidOperationException("The OPC UA client is already connected.");

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
            ApplicationName = "ForgeDataGateway",
            ApplicationUri = $"urn:{Utils.GetHostName()}:ForgeDataGateway:Client",
            ApplicationType = ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                {
                    StoreType = "Directory",
                    StorePath = ownPath,
                    SubjectName = "CN=ForgeDataGateway OPC UA Client"
                },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = trustedPath },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = issuersPath },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = rejectedPath },
                AutoAcceptUntrustedCertificates = true,
                AddAppCertToTrustedStore = true
            },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60000 },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15000 },
            TraceConfiguration = new TraceConfiguration(),
            DisableHiResClock = false
        };

        await config.Validate(ApplicationType.Client);
        var app = new ApplicationInstance
        {
            ApplicationName = "ForgeDataGateway",
            ApplicationType = ApplicationType.Client,
            ApplicationConfiguration = config
        };

        bool haveCert = await app.CheckApplicationInstanceCertificate(true, 2048);
        if (!haveCert) throw new InvalidOperationException("Unable to create application certificate.");

        var endpointDescription = CoreClientUtils.SelectEndpoint(config, source.Endpoint, useSecurity: false);
        var endpointConfiguration = EndpointConfiguration.Create(config);
        var configuredEndpoint = new ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

        session = await Session.Create(config, configuredEndpoint, false, source.Name, 60000, null, null);
    }

    public Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default)
    {
        if (session is null) throw new InvalidOperationException("The OPC UA client is not connected.");

        NodeId start = string.IsNullOrEmpty(nodeId) ? ObjectIds.ObjectsFolder : NodeId.Parse(nodeId);
        session.Browse(
            null, null, start, 0u, BrowseDirection.Forward, ReferenceTypeIds.HierarchicalReferences, true,
            (uint)(NodeClass.Object | NodeClass.Variable), out _, out ReferenceDescriptionCollection references);

        var nodes = references
            .Select(reference => new BrowseNode(
                reference.NodeId.ToString(),
                reference.DisplayName.Text,
                reference.NodeClass == NodeClass.Object))
            .ToList();
        return Task.FromResult<IReadOnlyList<BrowseNode>>(nodes);
    }

    public Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tag);
        if (session is null) throw new InvalidOperationException("The OPC UA client is not connected.");

        // A read failing (node temporarily unavailable, server restarting, session's channel dropped, ...)
        // is a routine, expected occurrence for a polling client - it is represented as a Bad-quality
        // sample rather than an exception, so one failed tick never takes down the sampling loop.
        try
        {
            DataValue value = session.ReadValue(NodeId.Parse(tag.NodeId));
            ValueQuality quality = StatusCode.IsGood(value.StatusCode) ? ValueQuality.Good
                : StatusCode.IsUncertain(value.StatusCode) ? ValueQuality.Uncertain
                : ValueQuality.Bad;
            return Task.FromResult(new SampledValue(tag.Id, value.Value, DateTimeOffset.UtcNow, quality));
        }
        catch (Exception)
        {
            return Task.FromResult(new SampledValue(tag.Id, null, DateTimeOffset.UtcNow, ValueQuality.Bad));
        }
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        session?.Close();
        session?.Dispose();
        session = null;
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync() => await DisconnectAsync();
}
