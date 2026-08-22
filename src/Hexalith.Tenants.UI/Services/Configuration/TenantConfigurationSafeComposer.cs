using System.Collections.ObjectModel;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Removes raw configuration and composes immutable positive-policy state.
/// </summary>
internal static class TenantConfigurationSafeComposer
{
    /// <summary>
    /// Composes raw server detail into safe read and management models.
    /// </summary>
    /// <param name="detail">Raw server-side detail.</param>
    /// <param name="policy">Validated current policy.</param>
    /// <returns>Factory-sanitized composition.</returns>
    public static TenantConfigurationComposition Compose(
        TenantDetail detail,
        TenantConfigurationReadPolicyResolution policy)
    {
        ArgumentNullException.ThrowIfNull(detail);
        ArgumentNullException.ThrowIfNull(policy);

        TenantDetail sanitized = SanitizeDetail(detail);
        if (!policy.IsAvailable)
        {
            return new(
                sanitized,
                TenantConfigurationSafeModel.Unavailable(detail.TenantId),
                TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status));
        }

        TenantConfigurationSafeRow[] rows = ComposeRows(
            (IReadOnlyDictionary<string, string>?)detail.Configuration,
            policy);
        return new(
            sanitized,
            TenantConfigurationSafeModel.Available(detail.TenantId, rows),
            TenantConfigurationManagementContext.Available(
                detail.TenantId,
                detail.Status,
                policy.IsGlobalAdministrator,
                policy.AuthorizedPrefixes,
                rows,
                ResolveAuthority(sanitized, policy)));
    }

    /// <summary>
    /// Reauthorizes only a prior safe model against current policy.
    /// </summary>
    /// <param name="safeModel">Prior same-tenant safe model.</param>
    /// <param name="tenantStatus">Last-confirmed tenant lifecycle.</param>
    /// <param name="policy">Validated current policy.</param>
    /// <param name="degraded">Whether retained rows must be labeled degraded.</param>
    /// <returns>Reauthorized safe model and management context.</returns>
    public static (TenantConfigurationSafeModel SafeModel, TenantConfigurationManagementContext ManagementContext) Reauthorize(
        TenantConfigurationSafeModel safeModel,
        TenantStatus tenantStatus,
        TenantConfigurationReadPolicyResolution policy,
        bool degraded)
    {
        ArgumentNullException.ThrowIfNull(safeModel);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.IsAvailable || !safeModel.IsAvailable)
        {
            return (
                TenantConfigurationSafeModel.Unavailable(safeModel.TenantId),
                TenantConfigurationManagementContext.Unavailable(safeModel.TenantId, tenantStatus));
        }

        TenantConfigurationSafeRow[] rows = safeModel.Rows
            .Where(row => policy.DisplaySafeKeys.Contains(row.Key)
                && TryResolveNamespace(row.Key, policy, out _))
            .Select(row =>
            {
                _ = TryResolveNamespace(row.Key, policy, out string? matchedNamespace);
                return new TenantConfigurationSafeRow(matchedNamespace!, row.Key, row.Value);
            })
            .OrderBy(static row => row.Namespace, StringComparer.Ordinal)
            .ThenBy(static row => row.Key, StringComparer.Ordinal)
            .ToArray();

        return (
            TenantConfigurationSafeModel.Available(safeModel.TenantId, rows, degraded),
            TenantConfigurationManagementContext.Available(
                safeModel.TenantId,
                tenantStatus,
                policy.IsGlobalAdministrator,
                policy.AuthorizedPrefixes,
                rows,
                ResolveAuthority(safeModel.TenantId, tenantStatus, safeModel, policy)));
    }

    /// <summary>
    /// Reauthorizes a prior safe model with current principal policy and sanitized membership evidence.
    /// </summary>
    /// <param name="sanitizedDetail">Current same-tenant sanitized authoritative detail.</param>
    /// <param name="safeModel">Prior same-tenant safe configuration model.</param>
    /// <param name="policy">Current validated principal policy.</param>
    /// <param name="degraded">Whether retained rows must be labeled degraded.</param>
    /// <returns>Reauthorized safe model and management context.</returns>
    public static (TenantConfigurationSafeModel SafeModel, TenantConfigurationManagementContext ManagementContext) Reauthorize(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        TenantConfigurationReadPolicyResolution policy,
        bool degraded)
    {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);
        ArgumentNullException.ThrowIfNull(policy);

        if (!string.Equals(sanitizedDetail.TenantId, safeModel.TenantId, StringComparison.Ordinal)
            || !policy.IsAvailable
            || !safeModel.IsAvailable)
        {
            return (
                TenantConfigurationSafeModel.Unavailable(sanitizedDetail.TenantId),
                TenantConfigurationManagementContext.Unavailable(
                    sanitizedDetail.TenantId,
                    sanitizedDetail.Status));
        }

        TenantConfigurationSafeRow[] rows = safeModel.Rows
            .Where(row => policy.DisplaySafeKeys.Contains(row.Key)
                && TryResolveNamespace(row.Key, policy, out _))
            .Select(row =>
            {
                _ = TryResolveNamespace(row.Key, policy, out string? matchedNamespace);
                return new TenantConfigurationSafeRow(matchedNamespace!, row.Key, row.Value);
            })
            .OrderBy(static row => row.Namespace, StringComparer.Ordinal)
            .ThenBy(static row => row.Key, StringComparer.Ordinal)
            .ToArray();

        return (
            TenantConfigurationSafeModel.Available(safeModel.TenantId, rows, degraded),
            TenantConfigurationManagementContext.Available(
                safeModel.TenantId,
                sanitizedDetail.Status,
                policy.IsGlobalAdministrator,
                policy.AuthorizedPrefixes,
                rows,
                ResolveAuthority(sanitizedDetail, policy)));
    }

    internal static TenantDetail SanitizeDetail(TenantDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        // Members/Configuration are non-nullable on the contract, but a wire payload can still present null
        // collections. Coalesce so one malformed field cannot NRE the composer and take the whole detail dark.
        IReadOnlyList<TenantMember> members =
            (IReadOnlyList<TenantMember>?)detail.Members ?? [];
        return new(
            detail.TenantId,
            detail.Name,
            detail.Description,
            detail.Status,
            new ReadOnlyCollection<TenantMember>(members.ToArray()),
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)),
            detail.CreatedAt);
    }

    private static TenantConfigurationSafeRow[] ComposeRows(
        IReadOnlyDictionary<string, string>? configuration,
        TenantConfigurationReadPolicyResolution policy)
        => (configuration ?? new Dictionary<string, string>(StringComparer.Ordinal))

            // The contract types a value as non-null, but System.Text.Json does not enforce that on a
            // projection payload. Skipping the row keeps one malformed entry from throwing inside the
            // composer, where the gateway's blanket catch would discard the entire tenant detail —
            // members, metadata, lifecycle and audit included — rather than just this key.
            .Where(item => item.Value is not null
                && policy.DisplaySafeKeys.Contains(item.Key)
                && TryResolveNamespace(item.Key, policy, out _))
            .Select(item =>
            {
                _ = TryResolveNamespace(item.Key, policy, out string? matchedNamespace);
                return new TenantConfigurationSafeRow(matchedNamespace!, item.Key, item.Value);
            })
            .OrderBy(static row => row.Namespace, StringComparer.Ordinal)
            .ThenBy(static row => row.Key, StringComparer.Ordinal)
            .ToArray();

    private static bool TryResolveNamespace(
        string key,
        TenantConfigurationReadPolicyResolution policy,
        out string? matchedNamespace)
    {
        if (policy.IsGlobalAdministrator)
        {
            matchedNamespace = NamespaceFromKey(key);
            return true;
        }

        matchedNamespace = policy.AuthorizedPrefixes
            .Where(prefix => TenantConfigurationManagementContext.IsPrefixMatch(prefix, key))
            .OrderByDescending(static prefix => prefix.Length)
            .ThenBy(static prefix => prefix, StringComparer.Ordinal)
            .FirstOrDefault();
        return matchedNamespace is not null;
    }

    private static string NamespaceFromKey(string key)
    {
        int separator = key.IndexOf('.', StringComparison.Ordinal);
        return separator > 0 ? key[..separator] : key;
    }

    private static TenantConfigurationAuthorityState ResolveAuthority(
        TenantDetail detail,
        TenantConfigurationReadPolicyResolution policy)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return ResolveAuthority(detail.TenantId, detail.Status, detail.Members, policy);
    }

    private static TenantConfigurationAuthorityState ResolveAuthority(
        string tenantId,
        TenantStatus tenantStatus,
        TenantConfigurationSafeModel safeModel,
        TenantConfigurationReadPolicyResolution policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(safeModel);

        // A safe configuration model deliberately carries no membership rows. Reauthorization that only
        // receives that model therefore cannot re-prove TenantOwner authority; it may preserve global
        // administrator evidence, but ordinary principals fail closed until sanitized detail is supplied.
        _ = tenantStatus;
        return policy.PrincipalState is TenantConfigurationPrincipalEvidenceState.GlobalAdministrator
            ? TenantConfigurationAuthorityState.GlobalAdministrator
            : policy.PrincipalState is TenantConfigurationPrincipalEvidenceState.Indeterminate
                ? TenantConfigurationAuthorityState.Indeterminate
                : TenantConfigurationAuthorityState.MissingPermission;
    }

    private static TenantConfigurationAuthorityState ResolveAuthority(
        string tenantId,
        TenantStatus tenantStatus,
        IReadOnlyList<TenantMember> members,
        TenantConfigurationReadPolicyResolution policy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(members);

        // Lifecycle status never gates mutation authority itself -- a disabled tenant blocks
        // configuration through the separate TenantDisabled domain outcome, not by revoking role.
        _ = tenantStatus;

        if (policy.PrincipalState is TenantConfigurationPrincipalEvidenceState.GlobalAdministrator)
        {
            return TenantConfigurationAuthorityState.GlobalAdministrator;
        }

        if (policy.PrincipalState is TenantConfigurationPrincipalEvidenceState.Indeterminate
            || string.IsNullOrWhiteSpace(policy.Subject))
        {
            return TenantConfigurationAuthorityState.Indeterminate;
        }

        return members.Any(member => member is not null
                && string.Equals(member.UserId, policy.Subject, StringComparison.Ordinal)
                && member.Role is TenantRole.TenantOwner)
            ? TenantConfigurationAuthorityState.TenantOwner
            : TenantConfigurationAuthorityState.MissingPermission;
    }
}
