namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// States whether the registered Tenants query gateway has a usable read transport.
/// </summary>
/// <remarks>
/// A host that registers its own <see cref="ITenantQueryGateway"/> must also register this contract with
/// matching availability before calling <c>AddHexalithTenantsUiModule</c>. Requiring the pair prevents a
/// custom gateway from being presented as disconnected, or an unavailable gateway as connected.
/// </remarks>
public interface ITenantsReadSurfaceAvailability
{
    /// <summary>Gets whether the registered query gateway can execute authoritative reads.</summary>
    bool IsConnected { get; }
}
