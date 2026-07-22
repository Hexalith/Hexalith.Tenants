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
}
