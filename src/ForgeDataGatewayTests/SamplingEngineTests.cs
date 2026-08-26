using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Sampling;
using Microsoft.Extensions.Logging.Abstractions;

namespace ForgeDataGatewayTests;

public sealed class SamplingEngineTests
{
    [Fact]
    public async Task StartAsync_ConnectsSourceAndSamplesEnabledTagsOnly()
    {
        var fakeClient = new FakeSourceClient();
        await using var engine = new SamplingEngine(_ => fakeClient, NullLogger<SamplingEngine>.Instance);

        var source = new SourceDefinition { Name = "TestSource", Protocol = "Fake", DefaultSamplingIntervalMs = 5 };
        var enabledTag = new TagDefinition { SourceId = source.Id, NodeId = "n1", Alias = "Tag1", Enabled = true };
        var disabledTag = new TagDefinition { SourceId = source.Id, NodeId = "n2", Alias = "Tag2", Enabled = false };
        var config = new GatewayConfig { Sources = [source], Tags = [enabledTag, disabledTag] };

        await engine.StartAsync(config);
        try
        {
            SampledValue sample = await engine.Buffer.ReadAsync();

            Assert.True(fakeClient.Connected);
            Assert.Equal(enabledTag.Id, sample.TagId);
            Assert.Equal(ValueQuality.Good, sample.Quality);
            Assert.True(engine.LatestValues.ContainsKey(enabledTag.Id));
            Assert.False(engine.LatestValues.ContainsKey(disabledTag.Id));
        }
        finally
        {
            await engine.StopAsync();
        }

        Assert.False(fakeClient.Connected);
    }

    [Fact]
    public async Task LatestValues_UpdatesAcrossMultipleTicks()
    {
        var fakeClient = new FakeSourceClient();
        await using var engine = new SamplingEngine(_ => fakeClient, NullLogger<SamplingEngine>.Instance);

        var source = new SourceDefinition { Name = "TestSource", Protocol = "Fake", DefaultSamplingIntervalMs = 2 };
        var tag = new TagDefinition { SourceId = source.Id, NodeId = "n1", Alias = "Tag1" };
        var config = new GatewayConfig { Sources = [source], Tags = [tag] };

        await engine.StartAsync(config);
        try
        {
            await engine.Buffer.ReadAsync();
            await engine.Buffer.ReadAsync();
            await engine.Buffer.ReadAsync();

            Assert.True(fakeClient.ReadCount >= 3);
            Assert.True((int)engine.LatestValues[tag.Id].Value! >= 3);
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task StartAsync_OneSourceFailsToConnect_OtherSourcesStillStart()
    {
        var goodClient = new FakeSourceClient();
        await using var engine = new SamplingEngine(
            protocol => protocol == "Fake" ? goodClient : new FailingConnectSourceClient(),
            NullLogger<SamplingEngine>.Instance);

        var badSource = new SourceDefinition { Name = "Unreachable", Protocol = "FailingConnect", DefaultSamplingIntervalMs = 5 };
        var goodSource = new SourceDefinition { Name = "Reachable", Protocol = "Fake", DefaultSamplingIntervalMs = 5 };
        var badTag = new TagDefinition { SourceId = badSource.Id, NodeId = "n1", Alias = "BadTag" };
        var goodTag = new TagDefinition { SourceId = goodSource.Id, NodeId = "n1", Alias = "GoodTag" };
        var config = new GatewayConfig { Sources = [badSource, goodSource], Tags = [badTag, goodTag] };

        // Must not throw even though one source is unreachable.
        await engine.StartAsync(config);
        try
        {
            SampledValue sample = await engine.Buffer.ReadAsync();
            Assert.Equal(goodTag.Id, sample.TagId);
        }
        finally
        {
            await engine.StopAsync();
        }
    }

    [Fact]
    public async Task SampleLoop_ReadThrows_KeepsRunningAndReportsBadQuality()
    {
        var failingClient = new FailingReadSourceClient();
        await using var engine = new SamplingEngine(_ => failingClient, NullLogger<SamplingEngine>.Instance);

        var source = new SourceDefinition { Name = "Flaky", Protocol = "FailingRead", DefaultSamplingIntervalMs = 5 };
        var tag = new TagDefinition { SourceId = source.Id, NodeId = "n1", Alias = "Tag1" };
        var config = new GatewayConfig { Sources = [source], Tags = [tag] };

        await engine.StartAsync(config);
        try
        {
            // If the read exception propagated out of the sample loop unhandled, StopAsync's
            // Task.WhenAll(sampleLoops) below would rethrow it and fail this test - exactly the
            // BadServerHalted crash this fix addresses. Waiting a few ticks proves the loop survives.
            await Task.Delay(50);
            Assert.True(failingClient.Connected);
        }
        finally
        {
            await engine.StopAsync();
        }
    }
}
