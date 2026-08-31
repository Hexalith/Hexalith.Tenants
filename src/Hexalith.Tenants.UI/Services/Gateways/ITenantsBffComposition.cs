using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Gateways;

public interface ITenantsBffComposition {
    bool IsReadSurfaceConnected { get; }

    bool IsCommandSurfaceConnected { get; }

    /// <summary>Gets whether fixed-scope command dispatch is connected.</summary>
    bool IsGlobalAdministratorDispatchConnected => false;

    /// <summary>Gets whether command status lookup is connected.</summary>
    bool IsGlobalAdministratorStatusConnected => false;

    /// <summary>Gets whether fixed-scope projection requery is connected.</summary>
    bool IsGlobalAdministratorRequeryConnected => false;

    /// <summary>Gets whether the downstream grant consequence preview is ready.</summary>
    bool IsGlobalAdministratorGrantPreviewReady => false;

    /// <summary>Gets whether the downstream removal consequence preview is ready.</summary>
    bool IsGlobalAdministratorRemovePreviewReady => false;

    TenantLifecycleAuthorizationReflectionState LifecycleAuthorizationReflection
        => TenantLifecycleAuthorizationReflectionState.Indeterminate;

    TenantLifecycleAuthorizationReflectionState GlobalAdministratorsAuthorizationReflection
        => TenantLifecycleAuthorizationReflectionState.Indeterminate;

    /// <summary>
    /// Resolves global-administrator authorization from the current authoritative principal evidence.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for principal resolution.</param>
    /// <returns>The fail-closed authorization reflection state for the current principal.</returns>
    /// <remarks>
    /// The default fails closed rather than forwarding to <see cref="GlobalAdministratorsAuthorizationReflection"/>:
    /// that property reads the request principal only, which is the interpretation this seam exists to replace.
    /// Forwarding to it let an implementation silently inherit the discarded HTTP-only evidence.
    /// </remarks>
    ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveGlobalAdministratorsAuthorizationAsync(
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(TenantLifecycleAuthorizationReflectionState.Indeterminate);

    /// <summary>
    /// Resolves tenant-lifecycle authorization from the current authoritative circuit principal.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for principal resolution.</param>
    /// <returns>The fail-closed authorization reflection state for the current principal.</returns>
    ValueTask<TenantLifecycleAuthorizationReflectionState> ResolveLifecycleAuthorizationAsync(
        CancellationToken cancellationToken = default)
        => ResolveGlobalAdministratorsAuthorizationAsync(cancellationToken);

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
    /// Reauthorizes configuration management from current circuit principal and sanitized detail evidence.
    /// </summary>
    /// <param name="sanitizedDetail">Current sanitized authoritative detail.</param>
    /// <param name="safeModel">Current safe configuration model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current role and namespace management context.</returns>
    ValueTask<TenantConfigurationManagementContext> ReauthorizeConfigurationManagementAsync(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);
        return ReauthorizeConfigurationManagementAsync(
            sanitizedDetail.TenantId,
            sanitizedDetail.Status,
            safeModel,
            cancellationToken);
    }

    /// <summary>
    /// Composes safe per-action role, namespace, and preview evidence without reading claims in the component.
    /// </summary>
    /// <param name="sanitizedDetail">Current sanitized authoritative detail.</param>
    /// <param name="managementContext">Current configuration management evidence.</param>
    /// <param name="lifecycleAuthorization">Current circuit-derived lifecycle authorization.</param>
    /// <returns>Safe BFF evidence for the shared high-impact kernel.</returns>
    TenantHighImpactBffEvidence ComposeTenantHighImpactEvidence(
        TenantDetail sanitizedDetail,
        TenantConfigurationManagementContext managementContext,
        TenantLifecycleAuthorizationReflectionState lifecycleAuthorization)
    {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(managementContext);

        bool sameTenant = string.Equals(
            sanitizedDetail.TenantId,
            managementContext.TenantId,
            StringComparison.Ordinal);
        bool previewReady = sameTenant
            && !string.IsNullOrWhiteSpace(sanitizedDetail.TenantId)
            && sanitizedDetail.Status is not TenantStatus.Unknown;
        TenantHighImpactAuthorityEvidence configurationAuthority = managementContext.AuthorityState switch
        {
            TenantConfigurationAuthorityState.TenantOwner
                or TenantConfigurationAuthorityState.GlobalAdministrator
                => TenantHighImpactAuthorityEvidence.Authorized,
            TenantConfigurationAuthorityState.MissingPermission
                => TenantHighImpactAuthorityEvidence.MissingPermission,
            _ => TenantHighImpactAuthorityEvidence.Indeterminate,
        };
        TenantHighImpactNamespaceScopeEvidence scope = !sameTenant || !managementContext.IsAvailable
            ? TenantHighImpactNamespaceScopeEvidence.Indeterminate
            : managementContext.IsGlobalAdministrator || managementContext.AuthorizedPrefixes.Count > 0
                ? TenantHighImpactNamespaceScopeEvidence.Authorized
                : TenantHighImpactNamespaceScopeEvidence.Missing;

        return new(
            lifecycleAuthorization switch
            {
                TenantLifecycleAuthorizationReflectionState.Authorized
                    => TenantHighImpactAuthorityEvidence.Authorized,
                TenantLifecycleAuthorizationReflectionState.MissingPermission
                    => TenantHighImpactAuthorityEvidence.MissingPermission,
                _ => TenantHighImpactAuthorityEvidence.Indeterminate,
            },
            configurationAuthority,
            scope,
            previewReady ? TenantHighImpactPreviewEvidence.Ready : TenantHighImpactPreviewEvidence.Missing,
            previewReady ? TenantHighImpactPreviewEvidence.Ready : TenantHighImpactPreviewEvidence.Missing);
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

    /// <summary>Composes an authorization-scoped, redacted preview from one fresh raw detail read.</summary>
    ValueTask<TenantSetConfigurationPreview> ComposeSetConfigurationPreviewAsync(
        TenantDetail rawDetail,
        TenantSetConfigurationIntent intent,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawDetail);
        ArgumentNullException.ThrowIfNull(intent);
        return ValueTask.FromResult(TenantSetConfigurationPreview.Unavailable(intent));
    }

    /// <summary>Composes an authorization-scoped, value-free remove preview from one fresh raw detail read.</summary>
    ValueTask<TenantRemoveConfigurationPreview> ComposeRemoveConfigurationPreviewAsync(
        TenantDetail rawDetail,
        TenantRemoveConfigurationIntent intent,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawDetail);
        ArgumentNullException.ThrowIfNull(intent);
        return ValueTask.FromResult(TenantRemoveConfigurationPreview.Unavailable(intent));
    }
}
