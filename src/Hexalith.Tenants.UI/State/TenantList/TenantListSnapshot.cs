using Hexalith.Tenants.UI.State.TruthState;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListSnapshot(
    TenantListSurfaceKind Kind,
    IReadOnlyList<TenantListRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    TenantFreshnessState Freshness,
    bool IsDegraded,
    bool IsAuthorizationScopedEmpty,
    string? ErrorMessage) {
    public static TenantListSnapshot Loading()
        => new(
            TenantListSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            false,
            null);

    public static TenantListSnapshot Ready(
        IReadOnlyList<TenantListRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        TenantFreshnessState freshness,
        bool isDegraded)
        => new(
            isDegraded ? TenantListSurfaceKind.Degraded : freshness == TenantFreshnessState.Stale ? TenantListSurfaceKind.Stale : TenantListSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            isDegraded,
            false,
            null);

    public static TenantListSnapshot Empty(bool isAuthorizationScoped, TenantFreshnessState freshness)
        => new(
            TenantListSurfaceKind.Empty,
            [],
            null,
            false,
            null,
            freshness,
            false,
            isAuthorizationScoped,
            null);

    public static TenantListSnapshot FilteredEmpty()
        => new(
            TenantListSurfaceKind.FilteredEmpty,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            false,
            null);

    public static TenantListSnapshot Error(string message)
        => new(
            TenantListSurfaceKind.Error,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            false,
            message);

    public static TenantListSnapshot Unauthorized()
        => new(
            TenantListSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            false,
            null);

    public static TenantListSnapshot Stale(IReadOnlyList<TenantListRow> rows, string? eTag)
        => new(
            TenantListSurfaceKind.Stale,
            rows,
            null,
            false,
            eTag,
            TenantFreshnessState.Stale,
            false,
            false,
            null);

    public static TenantListSnapshot Degraded(IReadOnlyList<TenantListRow> rows, string message)
        => new(
            TenantListSurfaceKind.Degraded,
            rows,
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            true,
            false,
            message);
}
