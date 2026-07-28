using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.TenantUsers;

/// <summary>
/// Contains one immutable tenant-members page and its independent server-side evidence.
/// </summary>
public sealed record TenantUsersSnapshot(
    TenantUsersSurfaceKind Kind,
    string? TenantId,
    IReadOnlyList<TenantMember> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    string? ProjectionVersion,
    ReadModelFreshnessState Freshness,
    ProjectionLifecycleState Lifecycle,
    bool IsAuthorizationScopedEmpty,
    TenantUsersReason Reason,
    bool IsRefreshing = false)
{
    /// <summary>Creates a first-load state without discarding a separate prior snapshot.</summary>
    public static TenantUsersSnapshot Loading(string? tenantId = null)
        => EmptyState(TenantUsersSurfaceKind.Loading, tenantId, TenantUsersReason.None);

    /// <summary>Creates an authorized row state.</summary>
    public static TenantUsersSnapshot Ready(
        string tenantId,
        IReadOnlyList<TenantMember> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        string? projectionVersion,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle)
        => new(
            freshness switch
            {
                ReadModelFreshnessState.Stale => TenantUsersSurfaceKind.Stale,
                ReadModelFreshnessState.Unknown => TenantUsersSurfaceKind.Unknown,
                _ => TenantUsersSurfaceKind.Ready,
            },
            tenantId,
            rows,
            nextCursor,
            hasMore,
            eTag,
            projectionVersion,
            freshness,
            lifecycle,
            false,
            freshness == ReadModelFreshnessState.Stale
                ? TenantUsersReason.ProjectionStale
                : TenantUsersReason.None);

    /// <summary>Creates an authorization-safe empty state.</summary>
    public static TenantUsersSnapshot Empty(
        string tenantId,
        bool isAuthorizationScoped,
        string? eTag,
        string? projectionVersion,
        ReadModelFreshnessState freshness,
        ProjectionLifecycleState lifecycle)
        => new(
            freshness == ReadModelFreshnessState.Stale
                ? TenantUsersSurfaceKind.Stale
                : freshness == ReadModelFreshnessState.Unknown
                    ? TenantUsersSurfaceKind.Unknown
                    : TenantUsersSurfaceKind.Empty,
            tenantId,
            [],
            null,
            false,
            eTag,
            projectionVersion,
            freshness,
            lifecycle,
            isAuthorizationScoped,
            freshness == ReadModelFreshnessState.Stale
                ? TenantUsersReason.ProjectionStale
                : TenantUsersReason.None);

    /// <summary>Retains last-confirmed data while recording client-only refresh intent.</summary>
    public static TenantUsersSnapshot Refreshing(TenantUsersSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return previous with { IsRefreshing = true };
    }

    /// <summary>Creates a degraded state while retaining any applicable last-confirmed rows.</summary>
    public static TenantUsersSnapshot Degraded(
        string tenantId,
        TenantUsersSnapshot? previous,
        TenantUsersReason reason)
        => previous is not null && string.Equals(previous.TenantId, tenantId, StringComparison.Ordinal)
            ? previous with
            {
                Kind = TenantUsersSurfaceKind.Degraded,
                Freshness = ReadModelFreshnessState.Unknown,
                Lifecycle = ProjectionLifecycleState.Unknown,
                Reason = reason,
                IsRefreshing = false,
            }
            : EmptyState(TenantUsersSurfaceKind.Degraded, tenantId, reason);

    /// <summary>Creates an unauthorized state without revealing existence.</summary>
    public static TenantUsersSnapshot Unauthorized(string? tenantId = null)
        => EmptyState(TenantUsersSurfaceKind.Unauthorized, tenantId, TenantUsersReason.Unauthorized);

    /// <summary>Creates a not-found state.</summary>
    public static TenantUsersSnapshot NotFound(string tenantId)
        => EmptyState(TenantUsersSurfaceKind.NotFound, tenantId, TenantUsersReason.NotFound);

    /// <summary>Creates an invalid-request state.</summary>
    public static TenantUsersSnapshot Invalid(string tenantId)
        => EmptyState(TenantUsersSurfaceKind.Invalid, tenantId, TenantUsersReason.InvalidCursor);

    /// <summary>Creates an unavailable-dependency state.</summary>
    public static TenantUsersSnapshot Unavailable(string? tenantId = null)
        => EmptyState(TenantUsersSurfaceKind.Unavailable, tenantId, TenantUsersReason.GatewayUnavailable);

    /// <summary>Creates an operational error state.</summary>
    public static TenantUsersSnapshot Error(string tenantId, TenantUsersSnapshot? previous = null)
        => Degraded(tenantId, previous, TenantUsersReason.GatewayFailure);

    /// <summary>Returns a support-safe description that omits identities, cursors, ETags, and versions.</summary>
    public override string ToString()
        => $"{nameof(TenantUsersSnapshot)} {{ Kind = {Kind}, RowCount = {Rows.Count}, HasMore = {HasMore}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorizationScopedEmpty = {IsAuthorizationScopedEmpty}, Reason = {Reason}, IsRefreshing = {IsRefreshing} }}";

    private static TenantUsersSnapshot EmptyState(
        TenantUsersSurfaceKind kind,
        string? tenantId,
        TenantUsersReason reason)
        => new(
            kind,
            tenantId,
            [],
            null,
            false,
            null,
            null,
            ReadModelFreshnessState.Unknown,
            ProjectionLifecycleState.Unknown,
            false,
            reason);
}
