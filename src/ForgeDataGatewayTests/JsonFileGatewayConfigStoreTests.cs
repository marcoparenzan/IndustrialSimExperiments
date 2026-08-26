using ForgeDataGatewayCore.Config;

namespace ForgeDataGatewayTests;

public sealed class JsonFileGatewayConfigStoreTests
{
    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsDefaultConfig()
    {
        string path = Path.Combine(Path.GetTempPath(), $"forge-gateway-test-{Guid.NewGuid()}.json");
        var store = new JsonFileGatewayConfigStore(path);

        GatewayConfig config = await store.LoadAsync();

        Assert.Empty(config.Sources);
        Assert.Empty(config.Tags);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsConfig()
    {
        string path = Path.Combine(Path.GetTempPath(), $"forge-gateway-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonFileGatewayConfigStore(path);
            var source = new SourceDefinition { Name = "Simulator", Endpoint = "opc.tcp://localhost:4840/ForgeSim" };
            var tag = new TagDefinition { SourceId = source.Id, NodeId = "ns=2;s=Conveyor.Supply.LineLineVoltage", Alias = "LineVoltage" };
            var original = new GatewayConfig { Sources = [source], Tags = [tag] };
            original.Namespace.Enterprise = "Acme";
            original.Mqtt.Port = 1884;

            await store.SaveAsync(original);
            GatewayConfig loaded = await store.LoadAsync();

            Assert.Single(loaded.Sources);
            Assert.Equal(source.Name, loaded.Sources[0].Name);
            Assert.Equal(source.Endpoint, loaded.Sources[0].Endpoint);
            Assert.Single(loaded.Tags);
            Assert.Equal(tag.Alias, loaded.Tags[0].Alias);
            Assert.Equal("Acme", loaded.Namespace.Enterprise);
            Assert.Equal(1884, loaded.Mqtt.Port);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
