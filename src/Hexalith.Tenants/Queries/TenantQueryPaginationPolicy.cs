namespace Hexalith.Tenants.Queries;

/// <summary>
/// Defines server-side pagination bounds for tenant query endpoint families.
/// </summary>
internal static class TenantQueryPaginationPolicy {
    /// <summary>
    /// Default page size for standard tenant list queries.
    /// </summary>
    public const int StandardDefaultPageSize = 20;

    /// <summary>
    /// Maximum page size for standard tenant list queries.
    /// </summary>
    public const int StandardMaximumPageSize = 100;

    /// <summary>
    /// Default page size for tenant audit queries.
    /// </summary>
    public const int AuditDefaultPageSize = 100;

    /// <summary>
    /// Maximum page size for tenant audit queries.
    /// </summary>
    public const int AuditMaximumPageSize = 1000;

    /// <summary>
    /// Applies standard tenant query page-size bounds.
    /// </summary>
    /// <param name="pageSize">Requested page size.</param>
    /// <returns>The bounded page size.</returns>
    public static int ClampStandardPageSize(int pageSize)
        => pageSize <= 0
            ? StandardDefaultPageSize
            : pageSize > StandardMaximumPageSize
                ? StandardMaximumPageSize
                : pageSize;

    /// <summary>
    /// Applies tenant audit query page-size bounds.
    /// </summary>
    /// <param name="pageSize">Requested page size.</param>
    /// <returns>The bounded page size.</returns>
    public static int ClampAuditPageSize(int pageSize)
        => pageSize <= 0
            ? AuditDefaultPageSize
            : pageSize > AuditMaximumPageSize
                ? AuditMaximumPageSize
                : pageSize;
}
