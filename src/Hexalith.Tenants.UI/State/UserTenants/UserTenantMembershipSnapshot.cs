using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipSnapshot(
    UserTenantMembershipSurfaceKind Kind,
    IReadOnlyList<UserTenantMembershipRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    TenantFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    UserTenantMembershipReason Reason)
{
    public static UserTenantMembershipSnapshot Loading()
        => new(
            UserTenantMembershipSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            UserTenantMembershipReason.None);

    public static UserTenantMembershipSnapshot Ready(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        TenantFreshnessState freshness)
        => new(
            freshness == TenantFreshnessState.Stale ? UserTenantMembershipSurfaceKind.Stale : UserTenantMembershipSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == TenantFreshnessState.Stale ? UserTenantMembershipReason.ProjectionStale : UserTenantMembershipReason.None);

    public static UserTenantMembershipSnapshot Empty(bool isAuthorizationScoped, TenantFreshnessState freshness, string? eTag)
        => new(
            UserTenantMembershipSurfaceKind.Empty,
            [],
            null,
            false,
            eTag,
            freshness,
            isAuthorizationScoped,
            UserTenantMembershipReason.None);

    public static UserTenantMembershipSnapshot Stale(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag)
        => new(
            UserTenantMembershipSurfaceKind.Stale,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Stale,
            false,
            UserTenantMembershipReason.ProjectionStale);

    public static UserTenantMembershipSnapshot Degraded(
        IReadOnlyList<UserTenantMembershipRow> rows,
        UserTenantMembershipReason reason,
        string? eTag = null,
        string? nextCursor = null,
        bool hasMore = false)
        => new(
            UserTenantMembershipSurfaceKind.Degraded,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Unknown,
            false,
            reason);

    public static UserTenantMembershipSnapshot Unauthorized(UserTenantMembershipReason reason = UserTenantMembershipReason.Unauthorized)
        => new(
            UserTenantMembershipSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason);

    public static UserTenantMembershipSnapshot Unavailable(UserTenantMembershipReason reason = UserTenantMembershipReason.GatewayUnavailable)
        => new(
            UserTenantMembershipSurfaceKind.Unavailable,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason);
}
