using Aspire.Hosting.ApplicationModel;

using CommunityToolkit.Aspire.Hosting.Dapr;

namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Contains the resource builders created by <see cref="HexalithTenantsExtensions.AddHexalithTenants"/>
/// for further customization by the consumer.
/// </summary>
/// <param name="StateStore">The DAPR state store component resource builder.</param>
/// <param name="PubSub">The DAPR pub/sub component resource builder.</param>
/// <param name="CommandApi">The CommandApi project resource builder.</param>
public record HexalithTenantsResources(
    IResourceBuilder<IDaprComponentResource> StateStore,
    IResourceBuilder<IDaprComponentResource> PubSub,
    IResourceBuilder<ProjectResource> CommandApi);
