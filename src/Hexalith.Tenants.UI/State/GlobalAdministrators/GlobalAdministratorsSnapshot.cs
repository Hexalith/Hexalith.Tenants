using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorsSnapshot(
    GlobalAdministratorsSurfaceKind Kind,
    IReadOnlyList<GlobalAdministratorRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    ReadModelFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    GlobalAdministratorsReason Reason,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown,
    string? ProjectionVersion = null,
    bool IsCompleteEvidence = false,
    bool PagingRecovered = false,
    string? RequestCursor = null,
    int RequestPageSize = 20) {
    public static GlobalAdministratorsSnapshot Loading()
        => new(
            GlobalAdministratorsSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            GlobalAdministratorsReason.None);

    public static GlobalAdministratorsSnapshot Ready(
        IReadOnlyList<GlobalAdministratorRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness)
        => new(
            freshness == ReadModelFreshnessState.Stale ? GlobalAdministratorsSurfaceKind.Stale : GlobalAdministratorsSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == ReadModelFreshnessState.Stale ? GlobalAdministratorsReason.ProjectionStale : GlobalAdministratorsReason.None);

    public static GlobalAdministratorsSnapshot Empty(
        bool isAuthorizationScoped,
        ReadModelFreshnessState freshness,
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

    public static GlobalAdministratorsSnapshot Unknown(
        IReadOnlyList<GlobalAdministratorRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag)
        => new(
            GlobalAdministratorsSurfaceKind.Unknown,
            rows,
            nextCursor,
            hasMore,
            eTag,
            ReadModelFreshnessState.Unknown,
            false,
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
            ReadModelFreshnessState.Stale,
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
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Unknown,
            false,
            reason);

    public static GlobalAdministratorsSnapshot Error(
        GlobalAdministratorsReason reason = GlobalAdministratorsReason.GatewayFailure)
        => new(
            GlobalAdministratorsSurfaceKind.Error,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
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
            ReadModelFreshnessState.Unknown,
            false,
            reason);

    /// <summary>
    /// Gets a value indicating whether this snapshot carries projection-confirmed evidence strong enough to
    /// enable or confirm a global administrator mutation.
    /// </summary>
    /// <remarks>
    /// Freshness alone is not sufficient. <c>ResolveFreshness</c> deliberately classifies a projection-backed
    /// response carrying only the legacy <c>X-Hexalith-Is-Stale: false</c> compatibility signal as
    /// <see cref="ReadModelFreshnessState.Current"/> even when no <c>X-Hexalith-Projection-Lifecycle</c>
    /// evidence was supplied, which is correct for honest display but is not projection-confirmed evidence.
    /// Mutation gating therefore also requires <see cref="ProjectionLifecycleState.Current"/>, matching the
    /// platform-owned <c>ProjectionLifecyclePolicy.CanMutate</c> contract and the equivalent detail/member
    /// gate in <c>MemberAccessReview.ActionsAreEvidenceBacked</c>.
    /// </remarks>
    public bool IsMutationEvidenceBacked
        => Freshness is ReadModelFreshnessState.Current
            && Lifecycle is ProjectionLifecycleState.Current;

    /// <summary>
    /// Returns a support-safe description that omits rows, identities, cursors, validators, and versions.
    /// </summary>
    /// <returns>A bounded support-safe snapshot description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorsSnapshot)} {{ Kind = {Kind}, Freshness = {Freshness}, Reason = {Reason}, Lifecycle = {Lifecycle}, IsCompleteEvidence = {IsCompleteEvidence}, PagingRecovered = {PagingRecovered} }}";
}
