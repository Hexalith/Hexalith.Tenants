using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantsBffComposition(
    ITenantCommandGateway commandGateway,
    IHttpContextAccessor? httpContextAccessor = null,
    ITenantConfigurationPrincipalResolver? principalResolver = null,
    TenantConfigurationReadPolicyProvider? policyProvider = null,
    ITenantsReadSurfaceAvailability? readSurface = null) : ITenantsBffComposition {
    // Reads the composition decision rather than resolving ITenantQueryGateway, which would close a
    // container cycle (this type -> gateway -> this type) the moment Tenants:BaseAddress is configured.
    // Absence fails closed: an unregistered read surface is not evidence of a connected one.
    public bool IsReadSurfaceConnected => readSurface?.IsConnected == true;

    public bool IsCommandSurfaceConnected => commandGateway is not UnavailableTenantCommandGateway;

    public TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => IsCommandSurfaceConnected
            ? TenantsGlobalAdministratorClaims.Evaluate(httpContextAccessor?.HttpContext?.User)
            : TenantLifecycleAuthorizationReflectionState.Indeterminate;

    public TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
        => TenantsGlobalAdministratorClaims.Evaluate(httpContextAccessor?.HttpContext?.User);

    public async ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
        CancellationToken cancellationToken = default) {
        if (principalResolver is null) {
            return TenantLifecycleAuthorizationReflectionState.Indeterminate;
        }

        TenantConfigurationPrincipalEvidence evidence = await principalResolver.ResolveAsync(cancellationToken)
            .ConfigureAwait(false);
        return evidence.State switch {
            TenantConfigurationPrincipalEvidenceState.GlobalAdministrator
                => TenantLifecycleAuthorizationReflectionState.Authorized,
            TenantConfigurationPrincipalEvidenceState.NonAdministrator
                => TenantLifecycleAuthorizationReflectionState.MissingPermission,
            _ => TenantLifecycleAuthorizationReflectionState.Indeterminate,
        };
    }

    public ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveLifecycleAuthorizationAsync(
        CancellationToken cancellationToken = default)
        => ResolveGlobalAdministratorsAuthorizationAsync(cancellationToken);

    public async ValueTask<TenantConfigurationComposition> ComposeTenantDetailAsync(
        TenantDetail detail,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(detail);

        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(detail.TenantId, cancellationToken)
            .ConfigureAwait(false);
        return TenantConfigurationSafeComposer.Compose(detail, policy);
    }

    public async ValueTask<TenantConfigurationComposition> ReauthorizeTenantDetailAsync(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        bool degraded,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);

        if (!string.Equals(sanitizedDetail.TenantId, safeModel.TenantId, StringComparison.Ordinal)) {
            return new(
                TenantConfigurationSafeComposer.SanitizeDetail(sanitizedDetail),
                TenantConfigurationSafeModel.Unavailable(sanitizedDetail.TenantId),
                TenantConfigurationManagementContext.Unavailable(sanitizedDetail.TenantId, sanitizedDetail.Status));
        }

        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(sanitizedDetail.TenantId, cancellationToken)
            .ConfigureAwait(false);
        (TenantConfigurationSafeModel safe, TenantConfigurationManagementContext management) =
            TenantConfigurationSafeComposer.Reauthorize(safeModel, sanitizedDetail.Status, policy, degraded);
        return new(TenantConfigurationSafeComposer.SanitizeDetail(sanitizedDetail), safe, management);
    }

    public async ValueTask<TenantConfigurationManagementContext> ReauthorizeConfigurationManagementAsync(
        string tenantId,
        TenantStatus tenantStatus,
        TenantConfigurationSafeModel safeModel,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(safeModel);
        if (!string.Equals(tenantId, safeModel.TenantId, StringComparison.Ordinal)) {
            return TenantConfigurationManagementContext.Unavailable(tenantId, tenantStatus);
        }

        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);
        return TenantConfigurationSafeComposer.Reauthorize(safeModel, tenantStatus, policy, safeModel.IsDegraded).ManagementContext;
    }

    public async ValueTask<bool> IsConfigurationKeyAuthorizedAsync(
        string tenantId,
        string key,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(tenantId, cancellationToken)
            .ConfigureAwait(false);
        return policy.IsAvailable
            && (policy.IsGlobalAdministrator
                || policy.AuthorizedPrefixes.Any(prefix => TenantConfigurationManagementContext.IsPrefixMatch(prefix, key)));
    }

    private async ValueTask<TenantConfigurationReadPolicyResolution> ResolvePolicyAsync(
        string tenantId,
        CancellationToken cancellationToken) {
        if (principalResolver is null || policyProvider is null) {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        TenantConfigurationPrincipalEvidence principal = await principalResolver.ResolveAsync(cancellationToken)
            .ConfigureAwait(false);
        return policyProvider.Resolve(tenantId, principal);
    }
}
