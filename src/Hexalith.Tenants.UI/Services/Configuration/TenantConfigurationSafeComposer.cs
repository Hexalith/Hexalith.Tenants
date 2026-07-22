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

        TenantConfigurationSafeRow[] rows = ComposeRows(detail.Configuration, policy);
        return new(
            sanitized,
            TenantConfigurationSafeModel.Available(detail.TenantId, rows),
            TenantConfigurationManagementContext.Available(
                detail.TenantId,
                detail.Status,
                policy.IsGlobalAdministrator,
                policy.AuthorizedPrefixes,
                rows));
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
                rows));
    }

    internal static TenantDetail SanitizeDetail(TenantDetail detail)
    {
        ArgumentNullException.ThrowIfNull(detail);
        return new(
            detail.TenantId,
            detail.Name,
            detail.Description,
            detail.Status,
            new ReadOnlyCollection<TenantMember>(detail.Members.ToArray()),
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)),
            detail.CreatedAt);
    }

    private static TenantConfigurationSafeRow[] ComposeRows(
        IReadOnlyDictionary<string, string> configuration,
        TenantConfigurationReadPolicyResolution policy)
        => configuration
            .Where(item => policy.DisplaySafeKeys.Contains(item.Key)
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
}
