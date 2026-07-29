using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.UserTenants;

public sealed record UserTenantMembershipSnapshot(
    UserTenantMembershipSurfaceKind Kind,
    IReadOnlyList<UserTenantMembershipRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    ReadModelFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    UserTenantMembershipReason Reason,
    string? TargetUserId = null,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown,
    string? ProjectionVersion = null,
    string? RequestCursor = null,
    int RequestPageSize = 20,
    bool PagingRecovered = false) {
    public static UserTenantMembershipSnapshot Loading()
        => new(
            UserTenantMembershipSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            UserTenantMembershipReason.None);

    public static UserTenantMembershipSnapshot Ready(
        IReadOnlyList<UserTenantMembershipRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness,
        string? targetUserId = null)
        => new(
            freshness == ReadModelFreshnessState.Stale ? UserTenantMembershipSurfaceKind.Stale : UserTenantMembershipSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == ReadModelFreshnessState.Stale ? UserTenantMembershipReason.ProjectionStale : UserTenantMembershipReason.None,
            targetUserId);

    public static UserTenantMembershipSnapshot Empty(
        bool isAuthorizationScoped,
        ReadModelFreshnessState freshness,
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
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Stale,
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
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Unknown,
            false,
            reason,
            targetUserId);

    /// <summary>Returns a support-safe description that omits rows, identities, cursors, validators, and versions.</summary>
    public override string ToString()
        => $"{nameof(UserTenantMembershipSnapshot)} {{ Kind = {Kind}, RowCount = {Rows.Count}, HasMore = {HasMore}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorizationScopedEmpty = {IsAuthorizationScopedEmpty}, Reason = {Reason}, PagingRecovered = {PagingRecovered} }}";
}
