namespace ForgeDataGatewayCore.Config;

public sealed class GatewayConfig
{
    public List<SourceDefinition> Sources { get; set; } = [];
    public List<TagDefinition> Tags { get; set; } = [];
    public NamespaceOptions Namespace { get; set; } = new();
    public MqttOptions Mqtt { get; set; } = new();
}

public sealed class SourceDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "";
    public string Protocol { get; set; } = "OpcUa";
    public string Endpoint { get; set; } = "";
    public int DefaultSamplingIntervalMs { get; set; } = 1000;
    public bool Enabled { get; set; } = true;
}

public sealed class TagDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceId { get; set; }
    public string NodeId { get; set; } = "";
    public string Alias { get; set; } = "";
    public int? SamplingIntervalMs { get; set; }
    public bool Enabled { get; set; } = true;
}

public sealed class NamespaceOptions
{
    public string Enterprise { get; set; } = "Forge";
    public string Site { get; set; } = "Site1";
    public string Area { get; set; } = "Area1";
    public string Line { get; set; } = "Line1";
    public string TopicTemplate { get; set; } = "{Enterprise}/{Site}/{Area}/{Source}/{Tag}";
}

public sealed class MqttOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1883;
    public string ClientId { get; set; } = "ForgeDataGateway";
}
