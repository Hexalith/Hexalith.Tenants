using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Provides extension methods for adding the Hexalith Tenants topology
/// to an Aspire distributed application.
/// </summary>
public static class HexalithTenantsExtensions {
    /// <summary>
    /// Adds the Hexalith Tenants topology to the distributed application builder.
    /// This provisions DAPR state store (in-memory with actor support), DAPR pub/sub,
    /// and wires the Tenants service with a DAPR sidecar.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="tenants">The Tenants project resource builder.</param>
    /// <param name="daprConfigPath">
    /// Path to the Dapr sidecar configuration file (access control policies).
    /// When null, the sidecar starts without access control.
    /// </param>
    /// <returns>A <see cref="HexalithTenantsResources"/> containing the resource builders for further customization.</returns>
    public static HexalithTenantsResources AddHexalithTenants(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> tenants,
        string? daprConfigPath = null) {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(tenants);

        // Use AddDaprComponent instead of AddDaprStateStore so that WithMetadata
        // actually propagates into the generated YAML. AddDaprStateStore spawns a
        // separate in-memory provider process whose lifecycle hook ignores metadata.
        // Redis-backed state store is required so that all DAPR sidecars
        // (eventstore, tenants) share the same state — in-memory stores are
        // per-sidecar and cause command status polling to fail.
        IResourceBuilder<IDaprComponentResource> stateStore = builder
            .AddDaprComponent("statestore", "state.redis")
            .WithMetadata("actorStateStore", "true")
            .WithMetadata("redisHost", "localhost:6379");
        IResourceBuilder<IDaprComponentResource> pubSub = builder.AddDaprPubSub("pubsub");

        // Wire up Tenants service with DAPR sidecar and component references.
        // AppPort is intentionally omitted so the CommunityToolkit auto-detects
        // the app's actual port from the Aspire resource model.
        _ = tenants
            .WithDaprSidecar(sidecar => sidecar
                .WithOptions(new DaprSidecarOptions {
                    AppId = "tenants",
                    Config = daprConfigPath,
                })
                .WithReference(stateStore)
                .WithReference(pubSub));

        return new HexalithTenantsResources(stateStore, pubSub, tenants);
    }
}
