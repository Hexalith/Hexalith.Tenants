using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

/// <summary>
/// Carries safe role, scope, and preview evidence composed at the BFF boundary.
/// </summary>
/// <param name="LifecycleAuthority">Global-administrator authority for lifecycle actions.</param>
/// <param name="ConfigurationAuthority">TenantOwner or global-administrator authority for configuration actions.</param>
/// <param name="ConfigurationScope">Ordinal namespace scope for configuration actions.</param>
/// <param name="LifecyclePreview">Lifecycle preview-fact readiness.</param>
/// <param name="ConfigurationPreview">Configuration preview-fact readiness.</param>
public sealed record TenantHighImpactBffEvidence(
    TenantHighImpactAuthorityEvidence LifecycleAuthority,
    TenantHighImpactAuthorityEvidence ConfigurationAuthority,
    TenantHighImpactNamespaceScopeEvidence ConfigurationScope,
    TenantHighImpactPreviewEvidence LifecyclePreview,
    TenantHighImpactPreviewEvidence ConfigurationPreview);
