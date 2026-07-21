using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? Search = null,
    TenantStatus? Status = null,
    string SortColumn = TenantListSortColumns.TenantId,
    bool SortDescending = false,
    string? ETag = null,
    string? SearchCursor = null) {
    /// <summary>Returns a support-safe description that omits both cursor values and the search term.</summary>
    public override string ToString()
        => $"{nameof(TenantListRequest)} {{ PageSize = {PageSize}, HasSearch = {!string.IsNullOrWhiteSpace(Search)}, Status = {Status}, SortColumn = {SortColumn}, SortDescending = {SortDescending}, HasETag = {ETag is not null} }}";
}
