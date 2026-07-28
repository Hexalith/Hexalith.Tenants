using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantsBffComposition {
    bool IsReadSurfaceConnected { get; }

    bool IsCommandSurfaceConnected { get; }

    TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => TenantLifecycleAuthorizationReflectionState.Indeterminate;

    TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
        => TenantLifecycleAuthorizationReflectionState.Indeterminate;

    ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(GlobalAdministratorsAuthorizationReflection);

    ValueTask<TenantConfigurationComposition> ComposeTenantDetailAsync(
        TenantDetail detail,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(detail);
        return ValueTask.FromResult(new TenantConfigurationComposition(
            TenantConfigurationSafeComposer.SanitizeDetail(detail),
            TenantConfigurationSafeModel.Unavailable(detail.TenantId),
            TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status)));
    }

    ValueTask<TenantConfigurationComposition> ReauthorizeTenantDetailAsync(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        bool degraded,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);
        return ValueTask.FromResult(new TenantConfigurationComposition(
            TenantConfigurationSafeComposer.SanitizeDetail(sanitizedDetail),
            TenantConfigurationSafeModel.Unavailable(safeModel.TenantId),
            TenantConfigurationManagementContext.Unavailable(safeModel.TenantId, sanitizedDetail.Status)));
    }

    ValueTask<TenantConfigurationManagementContext> ReauthorizeConfigurationManagementAsync(
        string tenantId,
        TenantStatus tenantStatus,
        TenantConfigurationSafeModel safeModel,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(safeModel);
        return ValueTask.FromResult(TenantConfigurationManagementContext.Unavailable(tenantId, tenantStatus));
    }

    /// <summary>
    /// Determines whether the current principal holds namespace authorization for a literal key.
    /// </summary>
    /// <remarks>
    /// Projection-proof comparison reads the raw configuration dictionary, so it needs its own policy
    /// gate rather than trusting that a caller already applied one. Namespace authorization is the
    /// correct gate here, not display approval: a key may legitimately be commanded under proven scope
    /// while remaining absent from the read model.
    /// </remarks>
    /// <param name="tenantId">Literal requested tenant identifier.</param>
    /// <param name="key">Literal configuration key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the key is within proven scope; otherwise <see langword="false"/>.</returns>
    ValueTask<bool> IsConfigurationKeyAuthorizedAsync(
        string tenantId,
        string key,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(false);
}
