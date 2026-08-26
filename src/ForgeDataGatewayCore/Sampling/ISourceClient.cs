using ForgeDataGatewayCore.Config;

namespace ForgeDataGatewayCore.Sampling;

/// <summary>
/// A client that connects to one protocol endpoint (one <see cref="SourceDefinition"/>), optionally
/// browses its address space, and reads tag values on demand. Implementations are the consuming-side
/// counterpart of the simulator's IProtocolAdapter: one per protocol, resolved by <see cref="Protocol"/>.
/// </summary>
public interface ISourceClient : IAsyncDisposable
{
    string Protocol { get; }

    Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default);

    /// <summary>Browses the address space starting at <paramref name="nodeId"/> (or the root when null).
    /// Protocols without a browsable address space should throw <see cref="NotSupportedException"/>.</summary>
    Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default);

    Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default);

    Task DisconnectAsync(CancellationToken cancellationToken = default);
}
