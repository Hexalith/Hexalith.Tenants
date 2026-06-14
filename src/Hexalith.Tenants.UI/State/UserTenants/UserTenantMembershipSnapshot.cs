using Hexalith.Tenants.UI.State.TruthState;

namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipSnapshot(
    UserTenantMembershipSurfaceKind Kind,
    IReadOnlyList<UserTenantMembershipRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    TenantFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    UserTenantMembershipReason Reason,
    string? TargetUserId = null) {
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
        TenantFreshnessState freshness,
        string? targetUserId = null)
        => new(
            freshness == TenantFreshnessState.Stale ? UserTenantMembershipSurfaceKind.Stale : UserTenantMembershipSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == TenantFreshnessState.Stale ? UserTenantMembershipReason.ProjectionStale : UserTenantMembershipReason.None,
            targetUserId);

    public static UserTenantMembershipSnapshot Empty(
        bool isAuthorizationScoped,
        TenantFreshnessState freshness,
        string? eTag,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Empty,
            [],
            null,
            false,
            eTag,
            freshness,
            isAuthorizationScoped,
            UserTenantMembershipReason.None,
            targetUserId);

    public static UserTenantMembershipSnapshot Invalid(
        UserTenantMembershipReason reason = UserTenantMembershipReason.InvalidTargetUser,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Invalid,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason,
            targetUserId);

    public static UserTenantMembershipSnapshot Stale(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Stale,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Stale,
            false,
            UserTenantMembershipReason.ProjectionStale,
            targetUserId);

    public static UserTenantMembershipSnapshot Degraded(
        IReadOnlyList<UserTenantMembershipRow> rows,
        UserTenantMembershipReason reason,
        string? eTag = null,
        string? nextCursor = null,
        bool hasMore = false,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Degraded,
            rows,
            nextCursor,
            hasMore,
            eTag,
            TenantFreshnessState.Unknown,
            false,
            reason,
            targetUserId);

    public static UserTenantMembershipSnapshot Unauthorized(
        UserTenantMembershipReason reason = UserTenantMembershipReason.Unauthorized,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason,
            targetUserId);

    public static UserTenantMembershipSnapshot Unavailable(
        UserTenantMembershipReason reason = UserTenantMembershipReason.GatewayUnavailable,
        string? targetUserId = null)
        => new(
            UserTenantMembershipSurfaceKind.Unavailable,
            [],
            null,
            false,
            null,
            TenantFreshnessState.Unknown,
            false,
            reason,
            targetUserId);
}
