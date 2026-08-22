using System.Collections.ObjectModel;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Configuration;

namespace Hexalith.Tenants.UI.State.TenantDetail;

/// <summary>
/// Contains non-sensitive command scope and removable targets derived from current policy.
/// </summary>
public sealed class TenantConfigurationManagementContext
{
    private readonly IReadOnlyDictionary<string, TenantConfigurationSafeRow> _removableByKey;

    private TenantConfigurationManagementContext(
        string tenantId,
        TenantStatus tenantStatus,
        bool isAvailable,
        bool isGlobalAdministrator,
        TenantConfigurationAuthorityState authorityState,
        IEnumerable<string> authorizedPrefixes,
        IEnumerable<TenantConfigurationSafeRow> removableRows)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        ArgumentNullException.ThrowIfNull(authorizedPrefixes);
        ArgumentNullException.ThrowIfNull(removableRows);
        TenantId = tenantId;
        TenantStatus = tenantStatus;
        IsAvailable = isAvailable;
        IsGlobalAdministrator = isGlobalAdministrator;
        AuthorityState = authorityState;
        AuthorizedPrefixes = new ReadOnlyCollection<string>(authorizedPrefixes.ToArray());
        RemovableRows = new ReadOnlyCollection<TenantConfigurationSafeRow>(removableRows.ToArray());
        _removableByKey = new ReadOnlyDictionary<string, TenantConfigurationSafeRow>(
            RemovableRows.ToDictionary(static row => row.Key, StringComparer.Ordinal));
    }

    /// <summary>Gets the literal tenant identifier.</summary>
    public string TenantId { get; }

    /// <summary>Gets current tenant lifecycle state.</summary>
    public TenantStatus TenantStatus { get; }

    /// <summary>Gets whether policy and principal evidence are available.</summary>
    public bool IsAvailable { get; }

    /// <summary>Gets whether global-administrator wildcard scope is proven.</summary>
    public bool IsGlobalAdministrator { get; }

    /// <summary>Gets server-reflected TenantOwner or global-administrator authority.</summary>
    public TenantConfigurationAuthorityState AuthorityState { get; }

    /// <summary>Gets whether the required mutation role is authoritatively reflected.</summary>
    public bool HasMutationAuthority
        => AuthorityState is TenantConfigurationAuthorityState.TenantOwner
            or TenantConfigurationAuthorityState.GlobalAdministrator;

    /// <summary>Gets literal prefixes, or the sole administrator wildcard.</summary>
    public IReadOnlyList<string> AuthorizedPrefixes { get; }

    /// <summary>Gets current safe rows eligible as remove targets.</summary>
    public IReadOnlyList<TenantConfigurationSafeRow> RemovableRows { get; }

    /// <summary>Determines whether a literal full key is in current command scope.</summary>
    /// <param name="key">Literal full key.</param>
    /// <returns><see langword="true"/> only for exact or dot-descendant scope.</returns>
    public bool IsKeyAuthorized(string? key)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return IsGlobalAdministrator || AuthorizedPrefixes.Any(prefix => IsPrefixMatch(prefix, key));
    }

    /// <summary>Finds an exact current removable row.</summary>
    /// <param name="key">Literal full key.</param>
    /// <returns>The safe row, or <see langword="null"/>.</returns>
    public TenantConfigurationSafeRow? FindRemovableRow(string? key)
        => key is not null && _removableByKey.TryGetValue(key, out TenantConfigurationSafeRow? row)
            ? row
            : null;

    internal static TenantConfigurationManagementContext Available(
        string tenantId,
        TenantStatus tenantStatus,
        bool isGlobalAdministrator,
        IEnumerable<string> authorizedPrefixes,
        IEnumerable<TenantConfigurationSafeRow> removableRows,
        TenantConfigurationAuthorityState? authorityState = null)
        => new(
            tenantId,
            tenantStatus,
            true,
            isGlobalAdministrator,
            authorityState ?? (isGlobalAdministrator
                ? TenantConfigurationAuthorityState.GlobalAdministrator
                : TenantConfigurationAuthorityState.TenantOwner),
            authorizedPrefixes,
            removableRows);

    internal static TenantConfigurationManagementContext Unavailable(
        string tenantId,
        TenantStatus tenantStatus = TenantStatus.Unknown)
        => new(
            tenantId,
            tenantStatus,
            false,
            false,
            TenantConfigurationAuthorityState.Indeterminate,
            [],
            []);

    internal static bool IsPrefixMatch(string prefix, string key)
        => string.Equals(prefix, key, StringComparison.Ordinal)
        || key.StartsWith(string.Concat(prefix, "."), StringComparison.Ordinal);
}
