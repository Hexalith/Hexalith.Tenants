using Hexalith.EventStore.Client.Projections;

using TenantDetailContract = Hexalith.Tenants.Contracts.Queries.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantDetail;

public sealed record TenantDetailSnapshot(
    TenantDetailSurfaceKind Kind,
    TenantDetailContract? Detail,
    string? ETag,
    ReadModelFreshnessState Freshness,
    string? ErrorMessage) {
    public static TenantDetailSnapshot Loading()
        => State(TenantDetailSurfaceKind.Loading, null, null, ReadModelFreshnessState.Unknown, null);

    public static TenantDetailSnapshot Ready(TenantDetailContract detail, string? eTag, ReadModelFreshnessState freshness)
        => State(TenantDetailSurfaceKind.Ready, detail, eTag, freshness, null);

    public static TenantDetailSnapshot Stale(TenantDetailContract detail, string? eTag)
        => State(TenantDetailSurfaceKind.Stale, detail, eTag, ReadModelFreshnessState.Stale, null);

    public static TenantDetailSnapshot Degraded(TenantDetailContract? detail, string message, string? eTag = null)
        => State(TenantDetailSurfaceKind.Degraded, detail, eTag, ReadModelFreshnessState.Unknown, message);

    public static TenantDetailSnapshot Unknown(string message, string? eTag = null)
        => State(TenantDetailSurfaceKind.Unknown, null, eTag, ReadModelFreshnessState.Unknown, message);

    public static TenantDetailSnapshot Unavailable(string message)
        => State(TenantDetailSurfaceKind.Unavailable, null, null, ReadModelFreshnessState.Unknown, message);

    public static TenantDetailSnapshot NotFound(string tenantId)
        => State(TenantDetailSurfaceKind.NotFound, null, null, ReadModelFreshnessState.Unknown, tenantId);

    public static TenantDetailSnapshot Unauthorized(string tenantId)
        => State(TenantDetailSurfaceKind.Unauthorized, null, null, ReadModelFreshnessState.Unknown, tenantId);

    private static TenantDetailSnapshot State(
        TenantDetailSurfaceKind kind,
        TenantDetailContract? detail,
        string? eTag,
        ReadModelFreshnessState freshness,
        string? message)
        => new(kind, detail, eTag, freshness, message);
}
