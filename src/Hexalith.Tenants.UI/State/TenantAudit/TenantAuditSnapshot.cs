using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public sealed record TenantAuditSnapshot(
    TenantAuditSurfaceKind Kind,
    IReadOnlyList<TenantAuditRow> Rows,
    string? NextCursor,
    bool HasMore,
    string? ETag,
    ReadModelFreshnessState Freshness,
    bool IsAuthorizationScopedEmpty,
    TenantAuditReason Reason,
    string? TenantId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Category = null,
    ProjectionLifecycleState Lifecycle = ProjectionLifecycleState.Unknown,
    string? ProjectionVersion = null,
    string? RequestCursor = null,
    int RequestPageSize = 50,
    string? CallerScope = null) {
    public static TenantAuditSnapshot Loading(string? tenantId = null)
        => new(
            TenantAuditSurfaceKind.Loading,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            TenantAuditReason.None,
            tenantId);

    public static TenantAuditSnapshot Ready(
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness,
        TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            freshness == ReadModelFreshnessState.Stale ? TenantAuditSurfaceKind.Stale : TenantAuditSurfaceKind.Ready,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            freshness == ReadModelFreshnessState.Stale ? TenantAuditReason.ProjectionStale : TenantAuditReason.None,
            request);
    }

    public static TenantAuditSnapshot Empty(
        bool isAuthorizationScoped,
        ReadModelFreshnessState freshness,
        string? eTag,
        TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            HasFilters(request) ? TenantAuditSurfaceKind.FilteredEmpty : TenantAuditSurfaceKind.Empty,
            [],
            null,
            false,
            eTag,
            freshness,
            isAuthorizationScoped,
            TenantAuditReason.None,
            request);
    }

    public static TenantAuditSnapshot Stale(
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Stale,
            rows,
            nextCursor,
            hasMore,
            eTag,
            ReadModelFreshnessState.Stale,
            false,
            TenantAuditReason.ProjectionStale,
            request);
    }

    public static TenantAuditSnapshot Degraded(
        IReadOnlyList<TenantAuditRow> rows,
        TenantAuditReason reason,
        TenantAuditRequest request,
        string? eTag = null,
        string? nextCursor = null,
        bool hasMore = false) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Degraded,
            rows,
            nextCursor,
            hasMore,
            eTag,
            ReadModelFreshnessState.Unknown,
            false,
            reason,
            request);
    }

    public static TenantAuditSnapshot Unauthorized(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Unauthorized,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            TenantAuditReason.Unauthorized,
            request);
    }

    public static TenantAuditSnapshot InvalidCursor(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.InvalidCursor,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            TenantAuditReason.InvalidCursor,
            request);
    }

    public static TenantAuditSnapshot ListRefreshed(
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness,
        TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.ListRefreshed,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            false,
            TenantAuditReason.ListRefreshed,
            request);
    }

    public static TenantAuditSnapshot Unavailable(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Unavailable,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            TenantAuditReason.GatewayUnavailable,
            request);
    }

    public static TenantAuditSnapshot Error(
        TenantAuditRequest request,
        TenantAuditReason reason = TenantAuditReason.GatewayFailure) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Error,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            reason,
            request);
    }

    public bool MatchesScope(TenantAuditRequest request, string callerScope) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(callerScope);

        return string.Equals(CallerScope, callerScope, StringComparison.Ordinal)
            && MatchesQueryScope(request);
    }

    /// <summary>Checks tenant, filter, cursor, and page scope without granting caller-bound retention.</summary>
    /// <param name="request">The audit request to compare.</param>
    /// <returns><see langword="true"/> when the non-caller query scope matches.</returns>
    public bool MatchesScope(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        return MatchesQueryScope(request);
    }

    private bool MatchesQueryScope(TenantAuditRequest request)
        => string.Equals(TenantId, request.TenantId, StringComparison.Ordinal)
            && From == request.From
            && To == request.To
            && string.Equals(Category, request.Category?.ToString(), StringComparison.Ordinal)
            && string.Equals(RequestCursor, request.Cursor, StringComparison.Ordinal)
            && RequestPageSize == request.PageSize;

    /// <summary>Returns a support-safe description that omits rows, identities, filters, cursors, validators, and versions.</summary>
    public override string ToString()
        => $"{nameof(TenantAuditSnapshot)} {{ Kind = {Kind}, RowCount = {Rows.Count}, HasMore = {HasMore}, Freshness = {Freshness}, Lifecycle = {Lifecycle}, IsAuthorizationScopedEmpty = {IsAuthorizationScopedEmpty}, Reason = {Reason} }}";

    private static TenantAuditSnapshot FromRequest(
        TenantAuditSurfaceKind kind,
        IReadOnlyList<TenantAuditRow> rows,
        string? nextCursor,
        bool hasMore,
        string? eTag,
        ReadModelFreshnessState freshness,
        bool isAuthorizationScopedEmpty,
        TenantAuditReason reason,
        TenantAuditRequest request)
        => new(
            kind,
            rows,
            nextCursor,
            hasMore,
            eTag,
            freshness,
            isAuthorizationScopedEmpty,
            reason,
            request.TenantId,
            request.From,
            request.To,
            request.Category?.ToString(),
            RequestCursor: request.Cursor,
            RequestPageSize: request.PageSize);

    private static bool HasFilters(TenantAuditRequest request)
        => request.From is not null || request.To is not null || request.Category is not null;
}
