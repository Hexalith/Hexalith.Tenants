using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantList;

/// <summary>Describes a tenant-list query request issued by a UI surface.</summary>
/// <param name="Cursor">Optional opaque pagination cursor for the ordinary tenant list.</param>
/// <param name="PageSize">Requested maximum number of tenant rows to return.</param>
/// <param name="Search">Optional search query text.</param>
/// <param name="Status">Optional tenant status filter.</param>
/// <param name="SortColumn">Column used to sort search results.</param>
/// <param name="SortDescending">Whether search results are sorted in descending order.</param>
/// <param name="ETag">Optional validator for conditional list refreshes.</param>
/// <param name="SearchCursor">Optional opaque pagination cursor for authoritative search.</param>
/// <param name="IncludeRowEnrichment">
/// Whether ordinary list reads issue supplementary detail reads to populate member and owner counts.
/// Surfaces that display only summary fields may set this to <see langword="false"/> to avoid enrichment overhead.
/// </param>
public sealed record TenantListRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? Search = null,
    TenantStatus? Status = null,
    string SortColumn = TenantListSortColumns.TenantId,
    bool SortDescending = false,
    string? ETag = null,
    string? SearchCursor = null,
    bool IncludeRowEnrichment = true)
{
    /// <summary>Returns a support-safe description that omits both cursor values and the search term.</summary>
    public override string ToString()
        => $"{nameof(TenantListRequest)} {{ PageSize = {PageSize}, HasSearch = {!string.IsNullOrWhiteSpace(Search)}, Status = {Status}, SortColumn = {SortColumn}, SortDescending = {SortDescending}, HasETag = {ETag is not null} }}";
}
