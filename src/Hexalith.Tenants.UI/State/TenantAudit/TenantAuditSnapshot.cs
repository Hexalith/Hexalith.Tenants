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
    string? ProjectionVersion = null) {
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

    public static TenantAuditSnapshot Error(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return FromRequest(
            TenantAuditSurfaceKind.Error,
            [],
            null,
            false,
            null,
            ReadModelFreshnessState.Unknown,
            false,
            TenantAuditReason.GatewayFailure,
            request);
    }

    public bool MatchesScope(TenantAuditRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        return string.Equals(TenantId, request.TenantId, StringComparison.Ordinal)
            && From == request.From
            && To == request.To
            && string.Equals(Category, request.Category?.ToString(), StringComparison.Ordinal);
    }

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
            request.Category?.ToString());

    private static bool HasFilters(TenantAuditRequest request)
        => request.From is not null || request.To is not null || request.Category is not null;
}
