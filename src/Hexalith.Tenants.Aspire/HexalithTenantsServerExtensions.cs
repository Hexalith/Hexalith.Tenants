using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Provides the Aspire hosting extension that adds the Hexalith.Tenants domain service to an AppHost running on
/// the shared Hexalith.EventStore platform.
/// </summary>
/// <remarks>
/// <para>
/// The Tenants service is a Hexalith.EventStore <b>domain module</b>. This helper adds its server project
/// (resolved cross-repo via <see cref="TenantsServerProjectMetadata"/>) and attaches a DAPR sidecar that shares
/// the EventStore state store and pub/sub, so a consuming AppHost (the Tenants AppHost or any module embedding
/// the Tenants service) calls a single method instead of hand-rolling the <see cref="IProjectMetadata"/> class
/// and the <see cref="HexalithEventStoreDomainModuleExtensions.AddEventStoreDomainModule"/> wiring.
/// </para>
/// <para>
/// Mirroring <see cref="HexalithEventStorePlatformExtensions.AddHexalithEventStorePlatformProjects"/>, this
/// helper adds the service runtime only; the consuming AppHost keeps composition-specific configuration on the
/// returned builder and on the EventStore command gateway — the <c>tenants</c> / <c>global-administrators</c>
/// domain-service registrations and the <c>global-administrators</c> → <c>tenants.events</c> topic override on
/// the gateway, the bootstrap global-administrator id (pinned to the deployment's identity-provider realm), and
/// JWT/OIDC authentication.
/// </para>
/// </remarks>
public static class HexalithTenantsServerExtensions
{
    /// <summary>
    /// Adds the Tenants server project with a DAPR sidecar that shares the EventStore state store and pub/sub,
    /// resolving the server project cross-repo from the consuming repository's <c>Hexalith.Tenants</c> source.
    /// </summary>
    /// <param name="builder">The distributed application builder.</param>
    /// <param name="eventStore">
    /// The EventStore topology resources returned by
    /// <see cref="HexalithEventStoreExtensions.AddHexalithEventStore(IDistributedApplicationBuilder, IResourceBuilder{ProjectResource}, IResourceBuilder{ProjectResource}, IResourceBuilder{ProjectResource}?, string?, string?, string?, int, string?, string?, string?)"/>.
    /// Its state-store and pub/sub components are shared with the Tenants sidecar.
    /// </param>
    /// <param name="daprConfigPath">Path to the Tenants module's DAPR access-control (Configuration CRD) YAML.</param>
    /// <param name="appId">The Aspire resource name and DAPR application id for the Tenants service. Defaults to <c>"tenants"</c>.</param>
    /// <param name="daprPlacementHostAddress">Optional DAPR placement service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <param name="daprSchedulerHostAddress">Optional DAPR scheduler service address (<c>host</c> or <c>host:port</c>). <see langword="null"/> uses the DAPR default.</param>
    /// <returns>The Tenants server project resource builder for further composition (bootstrap id, auth, references).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="eventStore"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="appId"/> is <see langword="null"/> or whitespace.</exception>
    public static IResourceBuilder<ProjectResource> AddHexalithTenantsServer(
        this IDistributedApplicationBuilder builder,
        HexalithEventStoreResources eventStore,
        string daprConfigPath,
        string appId = "tenants",
        string? daprPlacementHostAddress = null,
        string? daprSchedulerHostAddress = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(eventStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(daprConfigPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(appId);

        // Add the Tenants domain service; its sidecar shares the EventStore state store + pub/sub (Epic A4).
        return builder.AddProject<TenantsServerProjectMetadata>(appId)
            .AddEventStoreDomainModule(
                eventStore,
                appId,
                daprConfigPath,
                daprPlacementHostAddress: daprPlacementHostAddress,
                daprSchedulerHostAddress: daprSchedulerHostAddress);
    }
}
