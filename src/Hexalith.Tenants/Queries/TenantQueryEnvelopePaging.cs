using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.Queries;

/// <summary>
/// Applies the gateway envelope paging policy to tenant queries. When a request carries
/// <see cref="QueryEnvelope.Paging"/>, that policy alone governs the page: the legacy camelCase payload
/// <c>cursor</c> and <c>pageSize</c> fields are ignored, the existing page-size bounds, cursor codec and
/// cursor scopes still apply, offset paging is rejected, and every page reports its paging metadata.
/// </summary>
internal static class TenantQueryEnvelopePaging
{
    /// <summary>
    /// Resolves the cursor and bounded page size from the envelope paging policy.
    /// </summary>
    /// <param name="paging">The envelope paging policy.</param>
    /// <param name="clampPageSize">The endpoint family's page-size bounds; it maps a missing size to the family default.</param>
    /// <returns>The envelope-governed pagination, or an invalid-page failure for offset paging.</returns>
    public static TenantQueryPaginationPayload Resolve(QueryPagingOptions paging, Func<int, int> clampPageSize)
    {
        ArgumentNullException.ThrowIfNull(paging);
        ArgumentNullException.ThrowIfNull(clampPageSize);
        int pageSize = clampPageSize(paging.PageSize ?? 0);

        // Tenant pages are keyset (cursor) pages; an offset cannot be honored, so it is refused rather than ignored.
        return paging.Offset is not null
            ? new(null, pageSize, QueryAdapterFailureReason.InvalidPage, FromEnvelope: true)
            : new(paging.Cursor, pageSize, FromEnvelope: true);
    }

    /// <summary>
    /// Creates the paging metadata returned with a page served under envelope paging.
    /// </summary>
    /// <typeparam name="T">The page item type.</typeparam>
    /// <param name="pagination">The resolved pagination.</param>
    /// <param name="page">The served page, carrying its protected next cursor.</param>
    /// <returns>The paging metadata, or <see langword="null"/> when the legacy payload fields governed the page.</returns>
    public static QueryPagingMetadata? CreateMetadata<T>(TenantQueryPaginationPayload pagination, PaginatedResult<T> page)
    {
        ArgumentNullException.ThrowIfNull(pagination);
        ArgumentNullException.ThrowIfNull(page);
        return pagination.FromEnvelope
            ? new QueryPagingMetadata(pagination.PageSize, NextCursor: page.HasMore ? page.Cursor : null, HasMore: page.HasMore)
            : null;
    }
}
