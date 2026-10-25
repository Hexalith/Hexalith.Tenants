using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Supplies the same caller and current tenant as the signed live-test access token.</summary>
internal sealed class LiveRouteUserContext : IUserContextAccessor
{
    public string? TenantId => "system";

    public string? UserId => AuthenticatedRouteState.CallerId;
}
