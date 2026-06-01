namespace Hexalith.Tenants.Client.Configuration;

/// <summary>
/// Options for tenant client event subscription and local projection services.
/// </summary>
public class HexalithTenantsOptions {
    /// <summary>
    /// Configuration section used for tenant client options.
    /// </summary>
    public const string ConfigurationSectionName = "Tenants";

    /// <summary>
    /// DAPR pub/sub component name used for tenant events.
    /// </summary>
    public string PubSubName { get; set; } = "pubsub";

    /// <summary>
    /// Shared DAPR topic that carries tenant domain events.
    /// </summary>
    public string TopicName { get; set; } = "tenants.events";
}
