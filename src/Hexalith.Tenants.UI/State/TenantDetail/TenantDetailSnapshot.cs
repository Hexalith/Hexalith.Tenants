using Hexalith.Tenants.UI.State.TenantList;

using TenantDetailContract = Hexalith.Tenants.Contracts.Queries.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantDetail;

public sealed record TenantDetailSnapshot(
    TenantDetailSurfaceKind Kind,
    TenantDetailContract? Detail,
    string? ETag,
    TenantFreshnessState Freshness,
    string? ErrorMessage)
{
    public static TenantDetailSnapshot Loading()
        => State(TenantDetailSurfaceKind.Loading, null, null, TenantFreshnessState.Unknown, null);

    public static TenantDetailSnapshot Ready(TenantDetailContract detail, string? eTag, TenantFreshnessState freshness)
        => State(TenantDetailSurfaceKind.Ready, detail, eTag, freshness, null);

    public static TenantDetailSnapshot Stale(TenantDetailContract detail, string? eTag)
        => State(TenantDetailSurfaceKind.Stale, detail, eTag, TenantFreshnessState.Stale, null);

    public static TenantDetailSnapshot Degraded(TenantDetailContract? detail, string message, string? eTag = null)
        => State(TenantDetailSurfaceKind.Degraded, detail, eTag, TenantFreshnessState.Unknown, message);

    public static TenantDetailSnapshot Unknown(string message, string? eTag = null)
        => State(TenantDetailSurfaceKind.Unknown, null, eTag, TenantFreshnessState.Unknown, message);

    public static TenantDetailSnapshot Unavailable(string message)
        => State(TenantDetailSurfaceKind.Unavailable, null, null, TenantFreshnessState.Unknown, message);

    public static TenantDetailSnapshot NotFound(string tenantId)
        => State(TenantDetailSurfaceKind.NotFound, null, null, TenantFreshnessState.Unknown, tenantId);

    public static TenantDetailSnapshot Unauthorized(string tenantId)
        => State(TenantDetailSurfaceKind.Unauthorized, null, null, TenantFreshnessState.Unknown, tenantId);

    private static TenantDetailSnapshot State(
        TenantDetailSurfaceKind kind,
        TenantDetailContract? detail,
        string? eTag,
        TenantFreshnessState freshness,
        string? message)
        => new(kind, detail, eTag, freshness, message);
}
