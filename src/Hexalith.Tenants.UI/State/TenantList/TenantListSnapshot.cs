using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Tenants.UI.State.TenantList;

public sealed record TenantListSnapshot(
    TenantListSurfaceKind Kind,
    IReadOnlyList<TenantListRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    ReadModelFreshnessState Freshness,
    bool IsDegraded,
    bool IsAuthorizationScopedEmpty,
    TenantListReason Reason = TenantListReason.None,
    TenantListReason Notice = TenantListReason.None,
    bool IsAuthoritativeSearch = false,
    bool PagingRecovered = false,
    TenantListReason PagingNotice = TenantListReason.None) {
    /// <summary>Returns a support-safe description that omits cursor, ETag, and indexed material.</summary>
    public override string ToString()
        => $"{nameof(TenantListSnapshot)} {{ Kind = {Kind}, RowCount = {Rows.Count}, HasMore = {HasMore}, Freshness = {Freshness}, IsDegraded = {IsDegraded}, IsAuthorizationScopedEmpty = {IsAuthorizationScopedEmpty}, Reason = {Reason}, Notice = {Notice}, IsAuthoritativeSearch = {IsAuthoritativeSearch}, PagingRecovered = {PagingRecovered}, PagingNotice = {PagingNotice} }}";

    public static TenantListSnapshot Loading()
        => new(
            TenantListSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            false);

    public static TenantListSnapshot Ready(
        IReadOnlyList<TenantListRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness,
        bool isDegraded)
        => new(
            isDegraded ? TenantListSurfaceKind.Degraded : freshness == ReadModelFreshnessState.Stale ? TenantListSurfaceKind.Stale : TenantListSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            isDegraded,
            false,
            isDegraded ? TenantListReason.ProjectionDegraded : TenantListReason.None);

    public static TenantListSnapshot Empty(bool isAuthorizationScoped, ReadModelFreshnessState freshness)
        => new(
            TenantListSurfaceKind.Empty,
            [],
            null,
            false,
            null,
            freshness,
            false,
            isAuthorizationScoped);

    public static TenantListSnapshot FilteredEmpty()
        => new(
            TenantListSurfaceKind.FilteredEmpty,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            false);

    public static TenantListSnapshot Error(TenantListReason reason = TenantListReason.GatewayUnavailable)
        => new(
            TenantListSurfaceKind.Error,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            false,
            reason);

    public static TenantListSnapshot Unauthorized()
        => new(
            TenantListSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            false);

    public static TenantListSnapshot Stale(IReadOnlyList<TenantListRow> rows, string? eTag)
        => new(
            TenantListSurfaceKind.Stale,
            rows,
            null,
            false,
            eTag,
            ReadModelFreshnessState.Stale,
            false,
            false);

    public static TenantListSnapshot Degraded(
        IReadOnlyList<TenantListRow> rows,
        TenantListReason reason = TenantListReason.ProjectionDegraded)
        => new(
            TenantListSurfaceKind.Degraded,
            rows,
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            true,
            false,
            reason);
}
