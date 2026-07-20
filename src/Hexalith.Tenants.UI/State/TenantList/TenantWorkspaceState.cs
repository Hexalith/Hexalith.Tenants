using System.Globalization;
using System.Text;

using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>
/// Represents the normalized, surface-specific state of the Tenants workspace URL.
/// </summary>
/// <param name="Tab">The active workspace tab.</param>
/// <param name="Scope">The active tenant scope.</param>
/// <param name="UserId">The user identifier for the lookup surface.</param>
/// <param name="Search">The tenant search term.</param>
/// <param name="Status">The tenant status filter.</param>
/// <param name="Sort">The active surface sort column.</param>
/// <param name="SortDescending">Whether the active sort is descending.</param>
/// <param name="Cursor">The opaque cursor for the active surface.</param>
/// <param name="SelectedTenantId">The tenant selected before detail navigation.</param>
/// <param name="Anchor">The return-focus anchor associated with the selected tenant.</param>
public sealed record TenantWorkspaceState(
    string Tab,
    string Scope,
    string? UserId,
    string? Search,
    string? Status,
    string Sort,
    bool SortDescending,
    string? Cursor,
    string? SelectedTenantId,
    string? Anchor)
{
    /// <summary>Identifies the tenant list and self-audit tab.</summary>
    public const string TenantsTab = "tenants";

    /// <summary>Identifies the user membership lookup tab.</summary>
    public const string UsersTab = "users";

    /// <summary>Identifies the all-tenants scope.</summary>
    public const string AllScope = "all";

    /// <summary>Identifies the current-user tenant scope.</summary>
    public const string MyScope = "mine";

    /// <summary>
    /// Creates normalized workspace state from query-string values.
    /// </summary>
    public static TenantWorkspaceState FromQuery(
        string? tab,
        string? scope,
        string? userId,
        string? search,
        string? status,
        string? sort,
        string? sortDescending,
        string? cursor,
        string? selectedTenantId,
        string? anchor)
    {
        string normalizedTab = IsUsersTab(tab) ? UsersTab : TenantsTab;
        string normalizedScope = normalizedTab == TenantsTab && IsMyScope(scope) ? MyScope : AllScope;
        string? normalizedCursor = NormalizeOpaque(cursor);

        if (normalizedTab == UsersTab)
        {
            return new TenantWorkspaceState(
                UsersTab,
                AllScope,
                NormalizeUserId(userId),
                null,
                null,
                NormalizeUserSort(sort),
                false,
                normalizedCursor,
                null,
                null);
        }

        if (normalizedScope == MyScope)
        {
            return new TenantWorkspaceState(
                TenantsTab,
                MyScope,
                null,
                null,
                null,
                TenantListSortColumns.TenantId,
                false,
                normalizedCursor,
                null,
                null);
        }

        string normalizedSort = NormalizeTenantSort(sort);
        return new TenantWorkspaceState(
            TenantsTab,
            AllScope,
            null,
            NormalizeSearch(search),
            NormalizeStatus(status),
            normalizedSort,
            ParseBoolean(sortDescending),
            normalizedCursor,
            NormalizeContextValue(selectedTenantId),
            NormalizeContextValue(anchor));
    }

    /// <summary>
    /// Returns a state transition to a tab and resets the old surface cursor and context.
    /// </summary>
    public TenantWorkspaceState WithTab(string? tab)
        => FromQuery(
            tab,
            Scope,
            UserId,
            Search,
            Status,
            Sort,
            SortDescending ? bool.TrueString : null,
            cursor: null,
            selectedTenantId: null,
            anchor: null);

    /// <summary>
    /// Returns a state transition to a scope and resets the query cursor.
    /// </summary>
    public TenantWorkspaceState WithScope(string? scope)
        => FromQuery(
            TenantsTab,
            scope,
            userId: null,
            Search,
            Status,
            Sort,
            SortDescending ? bool.TrueString : null,
            cursor: null,
            selectedTenantId: null,
            anchor: null);

    /// <summary>
    /// Returns a state transition to a search term and resets the query cursor.
    /// </summary>
    public TenantWorkspaceState WithSearch(string? search)
        => FromQuery(
            TenantsTab,
            AllScope,
            userId: null,
            search,
            Status,
            Sort,
            SortDescending ? bool.TrueString : null,
            cursor: null,
            SelectedTenantId,
            Anchor);

    /// <summary>
    /// Returns a state transition to a status filter and resets the query cursor.
    /// </summary>
    public TenantWorkspaceState WithStatus(string? status)
        => FromQuery(
            TenantsTab,
            AllScope,
            userId: null,
            Search,
            status,
            Sort,
            SortDescending ? bool.TrueString : null,
            cursor: null,
            SelectedTenantId,
            Anchor);

    /// <summary>
    /// Returns a state transition to a tenant-list sort and resets the query cursor.
    /// </summary>
    public TenantWorkspaceState WithSort(string? sort, bool descending)
        => FromQuery(
            TenantsTab,
            AllScope,
            userId: null,
            Search,
            Status,
            sort,
            descending ? bool.TrueString : null,
            cursor: null,
            SelectedTenantId,
            Anchor);

    /// <summary>
    /// Returns a state transition to a user identifier and resets the lookup cursor.
    /// </summary>
    public TenantWorkspaceState WithUserId(string? userId)
        => FromQuery(
            UsersTab,
            AllScope,
            userId,
            search: null,
            status: null,
            Sort,
            sortDescending: null,
            cursor: null,
            selectedTenantId: null,
            anchor: null);

    /// <summary>
    /// Serializes the normalized state to the canonical workspace URL.
    /// </summary>
    public string ToCanonicalUrl()
    {
        StringBuilder builder = new("/tenants");
        if (Tab == UsersTab)
        {
            AppendQuery(builder, "tab", UsersTab);
            AppendQuery(builder, "userId", UserId);
            AppendQuery(builder, "sort", Sort == UserTenantMembershipSortColumns.Tenant ? null : Sort);
            AppendQuery(builder, "cursor", Cursor);
            return builder.ToString();
        }

        if (Scope == MyScope)
        {
            AppendQuery(builder, "tab", TenantsTab);
            AppendQuery(builder, "scope", MyScope);
            AppendQuery(builder, "cursor", Cursor);
            return builder.ToString();
        }

        AppendQuery(builder, "search", Search);
        AppendQuery(builder, "status", Status);
        AppendQuery(builder, "sort", Sort == TenantListSortColumns.TenantId ? null : Sort);
        AppendQuery(builder, "desc", SortDescending ? bool.TrueString : null);
        AppendQuery(builder, "cursor", Cursor);
        AppendQuery(builder, "selected", SelectedTenantId);
        AppendQuery(builder, "anchor", Anchor);
        return builder.ToString();
    }

    /// <summary>
    /// Normalizes a caller-supplied user identifier without treating it as a numeric identity.
    /// </summary>
    public static string? NormalizeUserId(string? value)
        => NormalizeSafeText(value);

    /// <summary>
    /// Normalizes an opaque cursor without parsing or exposing its contents.
    /// </summary>
    public static string? NormalizeCursor(string? value)
        => NormalizeOpaque(value);

    private static bool IsUsersTab(string? value)
        => string.Equals(value, UsersTab, StringComparison.OrdinalIgnoreCase);

    private static bool IsMyScope(string? value)
        => string.Equals(value, MyScope, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeTenantSort(string? value)
        => value switch
        {
            _ when string.Equals(value, TenantListSortColumns.Name, StringComparison.OrdinalIgnoreCase) => TenantListSortColumns.Name,
            _ when string.Equals(value, TenantListSortColumns.Status, StringComparison.OrdinalIgnoreCase) => TenantListSortColumns.Status,
            _ => TenantListSortColumns.TenantId,
        };

    /// <summary>
    /// Normalizes a user-membership sort value to one of the supported column identifiers.
    /// </summary>
    public static string NormalizeUserSort(string? value)
        => value switch
        {
            _ when string.Equals(value, UserTenantMembershipSortColumns.Name, StringComparison.OrdinalIgnoreCase) => UserTenantMembershipSortColumns.Name,
            _ when string.Equals(value, UserTenantMembershipSortColumns.Role, StringComparison.OrdinalIgnoreCase) => UserTenantMembershipSortColumns.Role,
            _ when string.Equals(value, UserTenantMembershipSortColumns.Status, StringComparison.OrdinalIgnoreCase) => UserTenantMembershipSortColumns.Status,
            _ => UserTenantMembershipSortColumns.Tenant,
        };

    private static string? NormalizeSearch(string? value)
        => NormalizeSafeText(value);

    private static string? NormalizeStatus(string? value)
    {
        if (!Enum.TryParse(value, ignoreCase: true, out TenantStatus parsed)
            || !Enum.IsDefined(parsed))
        {
            return null;
        }

        return parsed.ToString();
    }

    private static bool ParseBoolean(string? value)
        => bool.TryParse(value, out bool parsed) && parsed;

    private static string? NormalizeContextValue(string? value)
        => NormalizeSafeText(value);

    private static string? NormalizeOpaque(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)
            ? null
            : value;

    private static string? NormalizeSafeText(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)
            ? null
            : value.Trim();

    private static void AppendQuery(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        _ = builder.Append(builder.Length > "/tenants".Length ? '&' : '?');
        _ = builder.Append(Uri.EscapeDataString(key));
        _ = builder.Append('=');
        _ = builder.Append(Uri.EscapeDataString(value));
    }
}
