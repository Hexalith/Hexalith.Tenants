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
    bool IsRefreshing = false,
    string? RequestCursor = null,
    int RequestPageSize = 20,
    bool PagingRecovered = false)
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
    /// <remarks>
    /// <see cref="PagingRecovered"/> is deliberately carried forward: the retained rows still are the
    /// page-one payload a recovery produced, so the "paging restarted at the first page" notice remains
    /// true for what is on screen. Only a retention that replaces the *reason* for showing those rows --
    /// see <see cref="Degraded"/> -- has to clear it.
    /// </remarks>
    public static TenantUsersSnapshot Refreshing(TenantUsersSnapshot previous)
    {
        ArgumentNullException.ThrowIfNull(previous);
        return previous with { IsRefreshing = true };
    }

    /// <summary>Creates a degraded state while retaining any applicable last-confirmed rows.</summary>
    /// <remarks>
    /// <para>
    /// Clears <see cref="PagingRecovered"/>: this snapshot exists because a read failed, not because paging
    /// restarted at the first page. Carrying the flag re-rendered the polite "restarted at the first page"
    /// notice for a read that recovered nothing and in fact failed.
    /// </para>
    /// <para>
    /// Clears <see cref="IsAuthorizationScopedEmpty"/> for the same reason. That flag is the
    /// authorization-safe-absence channel -- it asserts the read succeeded and the caller's scope genuinely
    /// contains no members -- and the renderer short-circuits its entire state switch on it. Carrying it
    /// forward from a previously authorized-empty page presented a read the gateway could not complete as a
    /// successful "No visible members", conflating two states AC6 requires to stay distinct.
    /// </para>
    /// </remarks>
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
                PagingRecovered = false,
                IsAuthorizationScopedEmpty = false,
            }
            : EmptyState(TenantUsersSurfaceKind.Degraded, tenantId, reason);

    /// <summary>Creates an unauthorized state without revealing existence.</summary>
    public static TenantUsersSnapshot Unauthorized(string? tenantId = null)
        => EmptyState(TenantUsersSurfaceKind.Unauthorized, tenantId, TenantUsersReason.Unauthorized);

    /// <summary>Creates a not-found state.</summary>
    public static TenantUsersSnapshot NotFound(string tenantId)
        => EmptyState(TenantUsersSurfaceKind.NotFound, tenantId, TenantUsersReason.NotFound);

    /// <summary>Creates an invalid-request state.</summary>
    public static TenantUsersSnapshot Invalid(
        string tenantId,
        TenantUsersReason reason = TenantUsersReason.InvalidCursor)
        => EmptyState(TenantUsersSurfaceKind.Invalid, tenantId, reason);

    /// <summary>Creates an unavailable-dependency state.</summary>
    public static TenantUsersSnapshot Unavailable(string? tenantId = null)
        => EmptyState(TenantUsersSurfaceKind.Unavailable, tenantId, TenantUsersReason.GatewayUnavailable);

    /// <summary>Creates an operational error state.</summary>
    public static TenantUsersSnapshot Error(string tenantId, TenantUsersSnapshot? previous = null)
        => previous is not null && string.Equals(previous.TenantId, tenantId, StringComparison.Ordinal)
            ? Degraded(tenantId, previous, TenantUsersReason.GatewayFailure)
            : EmptyState(TenantUsersSurfaceKind.Error, tenantId, TenantUsersReason.GatewayFailure);

    /// <summary>Returns a support-safe description that omits identities, cursors, ETags, and versions.</summary>
    public override string ToString()
        => $"{nameof(TenantUsersSnapshot)} {{ Kind = {Kind}, RowCount = {Rows.Count}, HasMore = {HasMore}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorizationScopedEmpty = {IsAuthorizationScopedEmpty}, Reason = {Reason}, IsRefreshing = {IsRefreshing}, PagingRecovered = {PagingRecovered} }}";

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
