using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.TenantCommands;
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

    public bool IsGlobalAdministratorDispatchConnected
        => commandGateway.SupportsGlobalAdministratorDispatch;

    public bool IsGlobalAdministratorStatusConnected
        => commandGateway.SupportsCommandStatusLookup;

    public bool IsGlobalAdministratorRequeryConnected
        => readSurface?.IsConnected == true;

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
            TenantConfigurationSafeComposer.Reauthorize(sanitizedDetail, safeModel, policy, degraded);
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

    public async ValueTask<TenantConfigurationManagementContext> ReauthorizeConfigurationManagementAsync(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);
        if (!string.Equals(sanitizedDetail.TenantId, safeModel.TenantId, StringComparison.Ordinal))
        {
            return TenantConfigurationManagementContext.Unavailable(
                sanitizedDetail.TenantId,
                sanitizedDetail.Status);
        }

        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(
                sanitizedDetail.TenantId,
                cancellationToken)
            .ConfigureAwait(false);
        return TenantConfigurationSafeComposer.Reauthorize(
            sanitizedDetail,
            safeModel,
            policy,
            safeModel.IsDegraded).ManagementContext;
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

    public async ValueTask<TenantSetConfigurationPreview> ComposeSetConfigurationPreviewAsync(
        TenantDetail rawDetail,
        TenantSetConfigurationIntent intent,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawDetail);
        ArgumentNullException.ThrowIfNull(intent);

        if (!string.Equals(rawDetail.TenantId, intent.TenantId, StringComparison.Ordinal)
            || string.IsNullOrEmpty(intent.NamespacePrefix)
            || string.IsNullOrEmpty(intent.KeySuffix)
            || !string.Equals(
                string.Concat(intent.NamespacePrefix, ".", intent.KeySuffix),
                intent.FullKey,
                StringComparison.Ordinal))
        {
            return TenantSetConfigurationPreview.Unavailable(intent);
        }

        // Resolve and validate current authority before touching the raw configuration dictionary. This
        // ordering is the boundary that prevents the preview seam becoming a key-existence oracle.
        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(intent.TenantId, cancellationToken)
            .ConfigureAwait(false);
        bool namespaceAuthorized = policy.IsAvailable
            && (policy.IsGlobalAdministrator
                || policy.AuthorizedPrefixes.Contains(intent.NamespacePrefix, StringComparer.Ordinal));
        bool hasMutationAuthority = policy.IsGlobalAdministrator
            || !string.IsNullOrWhiteSpace(policy.Subject)
                && ((IReadOnlyList<TenantMember>?)rawDetail.Members ?? []).Any(member => member is not null
                    && string.Equals(member.UserId, policy.Subject, StringComparison.Ordinal)
                    && member.Role is TenantRole.TenantOwner);
        if (!namespaceAuthorized
            || !hasMutationAuthority
            || (!policy.IsGlobalAdministrator
                && !TenantConfigurationManagementContext.IsPrefixMatch(intent.NamespacePrefix, intent.FullKey)))
        {
            return TenantSetConfigurationPreview.Unavailable(intent);
        }

        TenantSetConfigurationCurrentState currentState = rawDetail.Configuration.TryGetValue(
            intent.FullKey,
            out string? currentValue)
            ? string.Equals(
                TenantSetConfigurationValueFingerprint.Create(currentValue),
                intent.ValueFingerprint,
                StringComparison.Ordinal)
                ? TenantSetConfigurationCurrentState.Matching
                : TenantSetConfigurationCurrentState.Different
            : TenantSetConfigurationCurrentState.Absent;

        return TenantSetConfigurationPreview.Create(
            intent,
            rawDetail.Status,
            currentState,
            freshness,
            lifecycle,
            projectionVersion,
            isAuthorized: true);
    }

    public async ValueTask<TenantRemoveConfigurationPreview> ComposeRemoveConfigurationPreviewAsync(
        TenantDetail rawDetail,
        TenantRemoveConfigurationIntent intent,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle,
        string? projectionVersion,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(rawDetail);
        ArgumentNullException.ThrowIfNull(intent);

        if (!string.Equals(rawDetail.TenantId, intent.TenantId, StringComparison.Ordinal)
            || string.IsNullOrEmpty(intent.NamespacePrefix)
            || string.IsNullOrEmpty(intent.FullKey)
            || !TenantConfigurationManagementContext.IsPrefixMatch(intent.NamespacePrefix, intent.FullKey))
        {
            return TenantRemoveConfigurationPreview.Unavailable(intent);
        }

        // Authority is resolved before the dictionary lookup so this seam cannot reveal whether an
        // unauthorized literal key exists.
        TenantConfigurationReadPolicyResolution policy = await ResolvePolicyAsync(intent.TenantId, cancellationToken)
            .ConfigureAwait(false);
        bool namespaceAuthorized = policy.IsAvailable
            && (policy.IsGlobalAdministrator
                || policy.AuthorizedPrefixes.Contains(intent.NamespacePrefix, StringComparer.Ordinal));
        bool hasMutationAuthority = policy.IsGlobalAdministrator
            || !string.IsNullOrWhiteSpace(policy.Subject)
                && ((IReadOnlyList<TenantMember>?)rawDetail.Members ?? []).Any(member => member is not null
                    && string.Equals(member.UserId, policy.Subject, StringComparison.Ordinal)
                    && member.Role is TenantRole.TenantOwner);
        if (!namespaceAuthorized || !hasMutationAuthority)
        {
            return TenantRemoveConfigurationPreview.Unavailable(intent);
        }

        TenantRemoveConfigurationCurrentState currentState = rawDetail.Configuration.ContainsKey(intent.FullKey)
            ? TenantRemoveConfigurationCurrentState.Present
            : TenantRemoveConfigurationCurrentState.Absent;
        return TenantRemoveConfigurationPreview.Create(
            intent,
            rawDetail.Status,
            currentState,
            freshness,
            lifecycle,
            projectionVersion,
            isAuthorized: true);
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
