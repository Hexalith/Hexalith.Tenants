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
    private const int MaximumCursorLength = 4096;

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

        if (normalizedTab == UsersTab)
        {
            string? normalizedUserId = NormalizeUserId(userId);
            string normalizedUserSort = NormalizeUserSort(sort);
            return new TenantWorkspaceState(
                UsersTab,
                AllScope,
                normalizedUserId,
                null,
                null,
                normalizedUserSort,
                false,
                CanRetainUsersCursor(
                    tab,
                    scope,
                    userId,
                    search,
                    status,
                    sort,
                    sortDescending,
                    selectedTenantId,
                    anchor,
                    normalizedUserId,
                    normalizedUserSort)
                        ? NormalizeOpaque(cursor)
                        : null,
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
                CanRetainMyTenantsCursor(
                    tab,
                    scope,
                    userId,
                    search,
                    status,
                    sort,
                    sortDescending,
                    selectedTenantId,
                    anchor)
                        ? NormalizeOpaque(cursor)
                        : null,
                null,
                null);
        }

        string normalizedSort = NormalizeTenantSort(sort);
        string? normalizedSearch = NormalizeSearch(search);
        string? normalizedStatus = NormalizeStatus(status);
        bool normalizedSortDescending = normalizedSort != TenantListSortColumns.TenantId
            && ParseBoolean(sortDescending);
        return new TenantWorkspaceState(
            TenantsTab,
            AllScope,
            null,
            normalizedSearch,
            normalizedStatus,
            normalizedSort,
            normalizedSortDescending,
            CanRetainAllTenantsCursor(
                tab,
                scope,
                userId,
                search,
                status,
                sort,
                sortDescending,
                normalizedSearch,
                normalizedStatus,
                normalizedSort,
                normalizedSortDescending)
                    ? NormalizeOpaque(cursor)
                    : null,
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
        => string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)
            ? null
            : value.Trim();

    private static string? NormalizeOpaque(string? value)
        => string.IsNullOrWhiteSpace(value)
            || value.Length > MaximumCursorLength
            || value.Any(char.IsControl)
            ? null
            : value;

    private static string? NormalizeSafeText(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Any(char.IsControl)
            ? null
            : value;

    private static bool CanRetainUsersCursor(
        string? tab,
        string? scope,
        string? userId,
        string? search,
        string? status,
        string? sort,
        string? sortDescending,
        string? selectedTenantId,
        string? anchor,
        string? normalizedUserId,
        string normalizedSort)
        => string.Equals(tab, UsersTab, StringComparison.Ordinal)
            && IsAbsentOrExact(scope, AllScope)
            && normalizedUserId is not null
            && string.Equals(userId, normalizedUserId, StringComparison.Ordinal)
            && IsAbsent(search)
            && IsAbsent(status)
            && IsAbsentOrExact(sort, normalizedSort)
            && IsAbsent(sortDescending)
            && IsAbsent(selectedTenantId)
            && IsAbsent(anchor);

    private static bool CanRetainMyTenantsCursor(
        string? tab,
        string? scope,
        string? userId,
        string? search,
        string? status,
        string? sort,
        string? sortDescending,
        string? selectedTenantId,
        string? anchor)
        => IsAbsentOrExact(tab, TenantsTab)
            && string.Equals(scope, MyScope, StringComparison.Ordinal)
            && IsAbsent(userId)
            && IsAbsent(search)
            && IsAbsent(status)
            && IsAbsent(sort)
            && IsAbsent(sortDescending)
            && IsAbsent(selectedTenantId)
            && IsAbsent(anchor);

    private static bool CanRetainAllTenantsCursor(
        string? tab,
        string? scope,
        string? userId,
        string? search,
        string? status,
        string? sort,
        string? sortDescending,
        string? normalizedSearch,
        string? normalizedStatus,
        string normalizedSort,
        bool normalizedSortDescending)
        => IsAbsentOrExact(tab, TenantsTab)
            && IsAbsentOrExact(scope, AllScope)
            && IsAbsent(userId)
            && IsAbsentOrExact(search, normalizedSearch)
            && IsAbsentOrExact(status, normalizedStatus)
            && IsAbsentOrExact(sort, normalizedSort)
            && IsValidSortDirection(sortDescending, normalizedSortDescending);

    private static bool IsValidSortDirection(string? value, bool normalized)
        => value is null
            ? !normalized
            : bool.TryParse(value, out bool parsed) && parsed == normalized;

    private static bool IsAbsent(string? value)
        => value is null;

    private static bool IsAbsentOrExact(string? value, string? normalized)
        => value is null || string.Equals(value, normalized, StringComparison.Ordinal);

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
