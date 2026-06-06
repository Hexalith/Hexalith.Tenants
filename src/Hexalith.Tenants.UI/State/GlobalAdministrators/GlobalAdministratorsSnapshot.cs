using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorsSnapshot(
    GlobalAdministratorsSurfaceKind Kind,
    IReadOnlyList<GlobalAdministratorRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    TenantFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    GlobalAdministratorsReason Reason)
{
    public static GlobalAdministratorsSnapshot Loading()
        => new(
            GlobalAdministratorsSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            GlobalAdministratorsReason.None);

    public static GlobalAdministratorsSnapshot Ready(
        IReadOnlyList<GlobalAdministratorRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        TenantFreshnessState freshness)
        => new(
            freshness == TenantFreshnessState.Stale ? GlobalAdministratorsSurfaceKind.Stale : GlobalAdministratorsSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == TenantFreshnessState.Stale ? GlobalAdministratorsReason.ProjectionStale : GlobalAdministratorsReason.None);

    public static GlobalAdministratorsSnapshot Empty(
        bool isAuthorizationScoped,
        TenantFreshnessState freshness,
        string? eTag)
        => new(
            GlobalAdministratorsSurfaceKind.Empty,
            [],
            null,
            false,
            eTag,
            freshness,
            isAuthorizationScoped,
            GlobalAdministratorsReason.None);

    public static GlobalAdministratorsSnapshot Stale(
        IReadOnlyList<GlobalAdministratorRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag)
        => new(
            GlobalAdministratorsSurfaceKind.Stale,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Stale,
            false,
            GlobalAdministratorsReason.ProjectionStale);

    public static GlobalAdministratorsSnapshot Degraded(
        IReadOnlyList<GlobalAdministratorRow> rows,
        GlobalAdministratorsReason reason,
        string? eTag = null,
        string? nextCursor = null,
        bool hasMore = false)
        => new(
            GlobalAdministratorsSurfaceKind.Degraded,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Unknown,
            false,
            reason);

    public static GlobalAdministratorsSnapshot Unauthorized(
        GlobalAdministratorsReason reason = GlobalAdministratorsReason.Unauthorized)
        => new(
            GlobalAdministratorsSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason);

    public static GlobalAdministratorsSnapshot Unavailable(
        GlobalAdministratorsReason reason = GlobalAdministratorsReason.GatewayUnavailable)
        => new(
            GlobalAdministratorsSurfaceKind.Unavailable,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason);

    public static GlobalAdministratorsSnapshot Invalid(
        GlobalAdministratorsReason reason = GlobalAdministratorsReason.InvalidCursor)
        => new(
            GlobalAdministratorsSurfaceKind.Invalid,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason);
}
