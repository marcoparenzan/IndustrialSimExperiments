using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Sampling;

namespace ForgeDataGatewayTests;

/// <summary>A trivial in-memory ISourceClient that returns an incrementing counter on every read,
/// used to exercise SamplingEngine without a real protocol connection.</summary>
internal sealed class FakeSourceClient : ISourceClient
{
    private int counter;

    public string Protocol => "Fake";
    public bool Connected { get; private set; }
    public int ReadCount => counter;

    public Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default)
    {
        Connected = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("FakeSourceClient does not support browsing.");

    public Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default)
    {
        int value = Interlocked.Increment(ref counter);
        return Task.FromResult(new SampledValue(tag.Id, value, DateTimeOffset.UtcNow, ValueQuality.Good));
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Connected = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>An ISourceClient whose ConnectAsync always throws, simulating an unreachable source at
/// startup - used to verify one bad source doesn't stop SamplingEngine from starting the others.</summary>
internal sealed class FailingConnectSourceClient : ISourceClient
{
    public string Protocol => "FailingConnect";

    public Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Simulated connection failure.");

    public Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("Should never be called - the source failed to connect.");

    public Task DisconnectAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}

/// <summary>An ISourceClient whose ReadAsync always throws (simulating e.g. a real OPC UA
/// ServiceResultException like BadServerHalted from a session whose server went away mid-run) -
/// used to verify a persistently failing tag doesn't crash the sampling loop or the engine.</summary>
internal sealed class FailingReadSourceClient : ISourceClient
{
    public string Protocol => "FailingRead";
    public bool Connected { get; private set; }

    public Task ConnectAsync(SourceDefinition source, CancellationToken cancellationToken = default)
    {
        Connected = true;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<BrowseNode>> BrowseAsync(string? nodeId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public Task<SampledValue> ReadAsync(TagDefinition tag, CancellationToken cancellationToken = default)
        => throw new Opc.Ua.ServiceResultException(Opc.Ua.StatusCodes.BadServerHalted);

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        Connected = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
