using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListRequest(
    string? Cursor = null,
    int PageSize = 20,
    string? Search = null,
    TenantStatus? Status = null,
    string SortColumn = TenantListSortColumns.TenantId,
    bool SortDescending = false,
    string? ETag = null);

public static class TenantListSortColumns {
    public const string TenantId = "tenantId";
    public const string Name = "name";
    public const string Status = "status";
}
