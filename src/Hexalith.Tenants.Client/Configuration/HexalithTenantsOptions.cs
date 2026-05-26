namespace Hexalith.Tenants.Client.Configuration;

public class HexalithTenantsOptions {
    public string PubSubName { get; set; } = "pubsub";

    public string TopicName { get; set; } = "tenants.events";

    public string CommandApiAppId { get; set; } = "commandapi";
}
