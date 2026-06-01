namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Options for configuring the Hexalith Tenants Aspire hosting topology.
/// </summary>
public sealed class HexalithTenantsAspireOptions {
    /// <summary>
    /// Gets or sets the DAPR application ID for the Tenants service.
    /// </summary>
    public string AppId { get; set; } = "tenants";

    /// <summary>
    /// Gets or sets the DAPR state-store component resource name.
    /// </summary>
    public string StateStoreName { get; set; } = "statestore";

    /// <summary>
    /// Gets or sets the DAPR pub/sub component resource name.
    /// </summary>
    public string PubSubName { get; set; } = "pubsub";

    /// <summary>
    /// Gets or sets the DAPR sidecar configuration path.
    /// </summary>
    public string? DaprConfigPath { get; set; }

    /// <summary>
    /// Gets or sets the DAPR state-store component type.
    /// </summary>
    public string StateStoreComponentType { get; set; } = "state.redis";

    /// <summary>
    /// Gets or sets the Redis host metadata used by local DAPR components.
    /// </summary>
    public string RedisHost { get; set; } = "localhost:6379";

    internal void Validate() {
        ValidateIdentifier(AppId, nameof(AppId));
        ValidateIdentifier(StateStoreName, nameof(StateStoreName));
        ValidateIdentifier(PubSubName, nameof(PubSubName));
        ValidateComponentType(StateStoreComponentType, nameof(StateStoreComponentType));
        ValidateRequired(RedisHost, nameof(RedisHost));
        ValidateNoWhitespace(RedisHost, nameof(RedisHost));

        if (DaprConfigPath is not null && string.IsNullOrWhiteSpace(DaprConfigPath)) {
            throw new ArgumentException("DAPR config path cannot be empty or whitespace.", nameof(DaprConfigPath));
        }
    }

    private static void ValidateRequired(string value, string propertyName) {
        if (string.IsNullOrWhiteSpace(value)) {
            throw new ArgumentException($"{propertyName} cannot be empty or whitespace.", propertyName);
        }
    }

    private static void ValidateIdentifier(string value, string propertyName) {
        ValidateRequired(value, propertyName);
        ValidateNoWhitespace(value, propertyName);
    }

    private static void ValidateComponentType(string value, string propertyName) {
        ValidateRequired(value, propertyName);
        ValidateNoWhitespace(value, propertyName);

        if (!value.Contains('.', StringComparison.Ordinal)) {
            throw new ArgumentException($"{propertyName} must use the DAPR component type format category.provider.", propertyName);
        }
    }

    private static void ValidateNoWhitespace(string value, string propertyName) {
        if (value.Any(char.IsWhiteSpace)) {
            throw new ArgumentException($"{propertyName} cannot contain whitespace.", propertyName);
        }
    }
}
