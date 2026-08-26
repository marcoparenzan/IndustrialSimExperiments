using ForgeDataGatewayCore.Config;
using ForgeDataGatewayCore.Mqtt;

namespace ForgeDataGatewayTests;

public sealed class MqttUnsPublisherTests
{
    [Fact]
    public void BuildTopic_SubstitutesAllPlaceholders()
    {
        var options = new NamespaceOptions
        {
            Enterprise = "Acme",
            Site = "Turin",
            Area = "Line1",
            Line = "Cell3",
            TopicTemplate = "{Enterprise}/{Site}/{Area}/{Line}/{Source}/{Tag}"
        };

        string topic = MqttUnsPublisher.BuildTopic(options, "Conveyor", "SpeedRpm");

        Assert.Equal("Acme/Turin/Line1/Cell3/Conveyor/SpeedRpm", topic);
    }

    [Fact]
    public void BuildTopic_DefaultTemplate_OmitsUnusedLinePlaceholder()
    {
        var options = new NamespaceOptions { Enterprise = "Acme", Site = "Turin", Area = "Line1" };

        string topic = MqttUnsPublisher.BuildTopic(options, "Conveyor", "SpeedRpm");

        Assert.Equal("Acme/Turin/Line1/Conveyor/SpeedRpm", topic);
    }
}
