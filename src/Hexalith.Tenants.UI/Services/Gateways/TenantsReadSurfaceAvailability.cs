namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Records whether the composing host configured a real Tenants read transport.
/// </summary>
/// <remarks>
/// <para>
/// This exists to state the composition decision directly instead of inferring it by resolving
/// <see cref="ITenantQueryGateway"/>. Resolving the gateway from <c>TenantsBffComposition</c> created a
/// dependency cycle — <c>TenantsBffComposition</c> -&gt; <see cref="ITenantQueryGateway"/> -&gt;
/// <c>ITenantsBffComposition</c> — which the container detected and threw on as soon as
/// <c>Tenants:BaseAddress</c> was configured. Because both constructor parameters carried defaults the
/// cycle was invisible while the address was absent, so the failure appeared exactly when the read
/// transport was switched on.
/// </para>
/// <para>
/// It is registered by <c>AddHexalithTenantsUiModule</c> in both branches, so absence means the module was
/// never composed. Consumers must treat absence as disconnected: an unregistered read surface is not
/// evidence of a working one.
/// </para>
/// </remarks>
/// <param name="IsConnected">
/// <see langword="true"/> when a usable <c>Tenants:BaseAddress</c> produced a real query gateway;
/// <see langword="false"/> when the module fell back to <see cref="UnavailableTenantQueryGateway"/>.
/// </param>
public sealed record TenantsReadSurfaceAvailability(bool IsConnected);
