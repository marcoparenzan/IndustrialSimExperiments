namespace ForgeDataGatewayCore.Sampling;

public enum ValueQuality
{
    Good,
    Uncertain,
    Bad
}

public sealed record SampledValue(Guid TagId, object? Value, DateTimeOffset Timestamp, ValueQuality Quality);

public sealed record BrowseNode(string NodeId, string DisplayName, bool HasChildren);
