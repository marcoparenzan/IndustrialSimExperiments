using Opc.Ua;
using Opc.Ua.Configuration;
namespace UaCourse;

public static class Configuration
{
    // Port 4841: ForgeSimApp's own OPC UA server listens on 4840, so both can run side by side.
    public const string Endpoint = "opc.tcp://localhost:4841/UaCourse";

    /// <summary>Where certificates (including private keys), the exported NodeSet2 and the ASP.NET data-protection keys are written.
    /// Never inside the repository: set UACOURSE_ARTIFACTS_ROOT, or the default %LOCALAPPDATA%\UaCourse is used.</summary>
    public static string ArtifactsRoot
    {
        get
        {
            var configured = Environment.GetEnvironmentVariable("UACOURSE_ARTIFACTS_ROOT");
            return string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "UaCourse")
                : configured;
        }
    }
    public static async Task<ApplicationInstance> CreateAsync(bool server)
    {
        var name = server ? "UaCourse.Server" : "UaCourse.Client";
        var pki = Path.Combine(ArtifactsRoot, "pki", name);
        var config = new ApplicationConfiguration
        {
            ApplicationName = name, ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:{name}",
            ProductUri = "urn:uacourse", ApplicationType = server ? ApplicationType.Server : ApplicationType.Client,
            SecurityConfiguration = new SecurityConfiguration
            {
                ApplicationCertificate = new CertificateIdentifier
                { StoreType = "Directory", StorePath = Path.Combine(pki, "own"), SubjectName = $"CN={name}" },
                TrustedPeerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "trusted") },
                TrustedIssuerCertificates = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "issuers") },
                RejectedCertificateStore = new CertificateTrustList { StoreType = "Directory", StorePath = Path.Combine(pki, "rejected") },
                AutoAcceptUntrustedCertificates = false, AddAppCertToTrustedStore = false, MinimumCertificateKeySize = 2048
            },
            TransportQuotas = new TransportQuotas { OperationTimeout = 15_000, MaxMessageSize = 4 * 1024 * 1024 },
            ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = 60_000 },
            ServerConfiguration = server ? new ServerConfiguration
            {
                BaseAddresses = new StringCollection { Endpoint },
                SecurityPolicies = new ServerSecurityPolicyCollection
                {
                    new ServerSecurityPolicy { SecurityMode = MessageSecurityMode.SignAndEncrypt, SecurityPolicyUri = SecurityPolicies.Basic256Sha256 }
                },
                UserTokenPolicies = new UserTokenPolicyCollection { new UserTokenPolicy(UserTokenType.Anonymous) }
            } : null
        };
        await config.Validate(config.ApplicationType);
        // Trust locale del laboratorio: confronto DER esatto, mai accettazione globale.
        config.CertificateValidator.CertificateValidation += (_, e) =>
        {
            if (e.Error.StatusCode != StatusCodes.BadCertificateUntrusted) return;
            var peerName = server ? "UaCourse.Client" : "UaCourse.Server";
            var peerStore = Path.Combine(ArtifactsRoot, "pki", peerName, "own", "certs");
            if (Directory.Exists(peerStore))
                e.Accept = Directory.EnumerateFiles(peerStore, "*.der")
                    .Any(file => File.ReadAllBytes(file).AsSpan().SequenceEqual(e.Certificate.RawData));
        };
        var app = new ApplicationInstance { ApplicationName = name, ApplicationType = config.ApplicationType, ApplicationConfiguration = config };
        if (!await app.CheckApplicationInstanceCertificate(false, 2048)) throw new InvalidOperationException("Certificato applicativo non disponibile.");
        return app;
    }
}
