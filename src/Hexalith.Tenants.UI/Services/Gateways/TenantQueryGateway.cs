using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TruthState;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    ITenantsQueryApiClient queryClient,
    IUserContextAccessor userContextAccessor) : ITenantQueryGateway {
    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";

    public async Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        try {
            EventStoreQueryResult<TenantDetail> result = await queryClient
                .SendAsync<TenantDetail>(CreateDetailRequest(request.TenantId), request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                return previous?.Detail is null
                    ? TenantDetailSnapshot.Degraded(null, "Tenant detail was unchanged, but no cached server snapshot is available.", result.ETag)
                    : previous with {
                        // A 304 means the cached snapshot is unchanged, not that a degraded/stale
                        // snapshot recovered. Preserve the prior truth state; only a previously
                        // Ready snapshot earns refreshed Current freshness from the conditional hit.
                        Freshness = previous.Kind is TenantDetailSurfaceKind.Ready ? TenantFreshnessState.Current : previous.Freshness,
                        ETag = result.ETag ?? previous.ETag,
                    };
            }

            if (result.Payload is null) {
                return TenantDetailSnapshot.Unknown("Tenant detail projection returned no payload.", result.ETag);
            }

            TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
            if (result.Metadata?.IsStale == true) {
                return TenantDetailSnapshot.Stale(result.Payload, result.ETag);
            }

            if (result.Metadata?.IsDegraded == true) {
                return TenantDetailSnapshot.Degraded(result.Payload, "Tenant detail projection is degraded.", result.ETag);
            }

            return TenantDetailSnapshot.Ready(result.Payload, result.ETag, freshness);
        }
        catch (EventStoreGatewayException ex) {
            return MapDetailException(request.TenantId, ex);
        }
    }

    public async Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId)) {
            return UserTenantMembershipSnapshot.Unauthorized(UserTenantMembershipReason.MissingAuthenticatedUser);
        }

        UserTenantMembershipRequest selfRequest = request with {
            TargetUserId = authenticatedUserId,
        };

        return await GetUserTenantsCoreAsync(authenticatedUserId, selfRequest, previous, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId)) {
            return UserTenantMembershipSnapshot.Unauthorized(
                UserTenantMembershipReason.MissingAuthenticatedUser,
                request.TargetUserId);
        }

        if (string.IsNullOrWhiteSpace(request.TargetUserId)) {
            return UserTenantMembershipSnapshot.Invalid(UserTenantMembershipReason.MissingTargetUser);
        }

        return await GetUserTenantsCoreAsync(authenticatedUserId, request, previous, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<UserTenantMembershipSnapshot> GetUserTenantsCoreAsync(
        string authenticatedUserId,
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken) {
        try {
            TenantsQueryApiRequest query = CreateUserTenantsRequest(authenticatedUserId, request);
            EventStoreQueryResult<PaginatedResult<UserTenantMembership>> result = await queryClient
                .SendAsync<PaginatedResult<UserTenantMembership>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                return previous is null || !string.Equals(previous.TargetUserId, request.TargetUserId, StringComparison.Ordinal)
                    ? UserTenantMembershipSnapshot.Degraded(
                        [],
                        UserTenantMembershipReason.NotModifiedWithoutSnapshot,
                        result.ETag,
                        targetUserId: request.TargetUserId)
                    : previous with {
                        ETag = result.ETag ?? previous.ETag,
                        Freshness = previous.Kind is UserTenantMembershipSurfaceKind.Ready
                            or UserTenantMembershipSurfaceKind.Empty
                                ? TenantFreshnessState.Current
                                : previous.Freshness,
                    };
            }

            PaginatedResult<UserTenantMembership> payload = result.Payload
                ?? new PaginatedResult<UserTenantMembership>([], null, false);
            TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
            IReadOnlyList<UserTenantMembershipRow> rows = payload.Items
                .Select(m => UserTenantMembershipRow.FromMembership(m) with {
                    Freshness = freshness,
                })
                .ToArray();

            if (result.Metadata?.IsStale == true) {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Stale }).ToArray();
                return UserTenantMembershipSnapshot.Stale(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
                    request.TargetUserId);
            }

            if (result.Metadata?.IsDegraded == true) {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Unknown }).ToArray();
                return UserTenantMembershipSnapshot.Degraded(
                    rows,
                    UserTenantMembershipReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore,
                    request.TargetUserId);
            }

            if (rows.Count == 0) {
                return UserTenantMembershipSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag, request.TargetUserId);
            }

            return UserTenantMembershipSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                request.TargetUserId);
        }
        catch (EventStoreGatewayException ex) {
            return MapUserTenantException(ex, request.TargetUserId);
        }
    }

    public async Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
        GlobalAdministratorsRequest request,
        GlobalAdministratorsSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId)) {
            return GlobalAdministratorsSnapshot.Unauthorized(GlobalAdministratorsReason.MissingAuthenticatedUser);
        }

        try {
            TenantsQueryApiRequest query = CreateGlobalAdministratorsRequest(request);
            EventStoreQueryResult<PaginatedResult<GlobalAdministratorSummary>> result = await queryClient
                .SendAsync<PaginatedResult<GlobalAdministratorSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                return previous is null
                    ? GlobalAdministratorsSnapshot.Degraded(
                        [],
                        GlobalAdministratorsReason.NotModifiedWithoutSnapshot,
                        result.ETag)
                    : previous with {
                        ETag = result.ETag ?? previous.ETag,
                        Freshness = previous.Kind is GlobalAdministratorsSurfaceKind.Ready
                            or GlobalAdministratorsSurfaceKind.Empty
                                ? TenantFreshnessState.Current
                                : previous.Freshness,
                    };
            }

            PaginatedResult<GlobalAdministratorSummary>? payload = result.Payload;
            if (payload is null) {
                return previous is null
                    ? GlobalAdministratorsSnapshot.Degraded(
                        [],
                        GlobalAdministratorsReason.MissingPayload,
                        result.ETag)
                    : previous with {
                        Kind = GlobalAdministratorsSurfaceKind.Degraded,
                        Reason = GlobalAdministratorsReason.MissingPayload,
                        ETag = result.ETag ?? previous.ETag,
                        Freshness = TenantFreshnessState.Unknown,
                    };
            }

            TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
            IReadOnlyList<GlobalAdministratorRow> rows = payload.Items
                .Select(m => GlobalAdministratorRow.FromSummary(m) with { Freshness = freshness })
                .ToArray();

            if (result.Metadata?.IsStale == true) {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Stale }).ToArray();
                return GlobalAdministratorsSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag);
            }

            if (result.Metadata?.IsDegraded == true) {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Unknown }).ToArray();
                return GlobalAdministratorsSnapshot.Degraded(
                    rows,
                    GlobalAdministratorsReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore);
            }

            if (rows.Count == 0) {
                return GlobalAdministratorsSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag);
            }

            return GlobalAdministratorsSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness);
        }
        catch (EventStoreGatewayException ex) {
            return MapGlobalAdministratorsException(ex);
        }
    }

    public async Task<TenantAuditSnapshot> GetTenantAuditAsync(
        TenantAuditRequest request,
        TenantAuditSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TenantId)) {
            return TenantAuditSnapshot.Degraded([], TenantAuditReason.MissingTenantId, request);
        }

        try {
            return await GetTenantAuditCoreAsync(request, previous, isListRefreshed: false, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (EventStoreGatewayException ex) when (IsInvalidAuditCursor(ex)) {
            TenantAuditRequest firstPageRequest = request with {
                Cursor = null,
                ETag = null,
            };

            try {
                return await GetTenantAuditCoreAsync(firstPageRequest, null, isListRefreshed: true, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (EventStoreGatewayException retryException) {
                return MapTenantAuditException(firstPageRequest, retryException);
            }
        }
        catch (EventStoreGatewayException ex) {
            return MapTenantAuditException(request, ex);
        }
    }

    private async Task<TenantAuditSnapshot> GetTenantAuditCoreAsync(
        TenantAuditRequest request,
        TenantAuditSnapshot? previous,
        bool isListRefreshed,
        CancellationToken cancellationToken) {
        TenantsQueryApiRequest query = CreateTenantAuditRequest(request);
        EventStoreQueryResult<PaginatedResult<TenantAuditEntry>> result = await queryClient
            .SendAsync<PaginatedResult<TenantAuditEntry>>(query, request.ETag, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsNotModified) {
            if (previous is null || !previous.MatchesScope(request)) {
                return TenantAuditSnapshot.Degraded(
                    [],
                    TenantAuditReason.NotModifiedWithoutSnapshot,
                    request,
                    result.ETag);
            }

            return previous with {
                ETag = result.ETag ?? previous.ETag,
                Freshness = previous.Kind is TenantAuditSurfaceKind.Ready
                    or TenantAuditSurfaceKind.Empty
                    or TenantAuditSurfaceKind.FilteredEmpty
                    or TenantAuditSurfaceKind.ListRefreshed
                        ? TenantFreshnessState.Current
                        : previous.Freshness,
            };
        }

        PaginatedResult<TenantAuditEntry>? payload = result.Payload;
        if (payload is null) {
            return previous is not null && previous.MatchesScope(request)
                ? previous with {
                    Kind = TenantAuditSurfaceKind.Degraded,
                    Reason = TenantAuditReason.MissingPayload,
                    ETag = result.ETag ?? previous.ETag,
                    Freshness = TenantFreshnessState.Unknown,
                }
                : TenantAuditSnapshot.Degraded([], TenantAuditReason.MissingPayload, request, result.ETag);
        }

        TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
        IReadOnlyList<TenantAuditRow> rows = payload.Items
            .Select(entry => TenantAuditRow.FromEntry(entry, freshness))
            .ToArray();

        if (result.Metadata?.IsStale == true) {
            rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Stale }).ToArray();
            return TenantAuditSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag, request);
        }

        if (result.Metadata?.IsDegraded == true) {
            rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Unknown }).ToArray();
            return TenantAuditSnapshot.Degraded(
                rows,
                TenantAuditReason.ProjectionDegraded,
                request,
                result.ETag,
                payload.Cursor,
                payload.HasMore);
        }

        if (isListRefreshed) {
            return TenantAuditSnapshot.ListRefreshed(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                request);
        }

        if (rows.Count == 0) {
            return TenantAuditSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag, request);
        }

        return TenantAuditSnapshot.Ready(rows, payload.Cursor, payload.HasMore, result.ETag, freshness, request);
    }

    public async Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        try {
            TenantsQueryApiRequest query = CreateListRequest(request);
            EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await queryClient
                .SendAsync<PaginatedResult<TenantSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                return previous is null
                    ? TenantListSnapshot.Degraded([], "Tenant list was unchanged, but no cached server snapshot is available.")
                    : previous with {
                        Kind = previous.Rows.Count == 0 ? TenantListSurfaceKind.Empty : TenantListSurfaceKind.Ready,
                        Freshness = TenantFreshnessState.Current,
                        ETag = result.ETag ?? previous.ETag,
                        ErrorMessage = null,
                    };
            }

            PaginatedResult<TenantSummary> payload = result.Payload ?? new PaginatedResult<TenantSummary>([], null, false);
            TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
            (IReadOnlyList<TenantListRow> rows, bool enrichmentDegraded) = await EnrichRowsAsync(
                payload.Items,
                freshness,
                cancellationToken).ConfigureAwait(false);
            bool isDegraded = enrichmentDegraded || result.Metadata?.IsDegraded == true;

            if (rows.Count == 0) {
                return TenantListSnapshot.Empty(isAuthorizationScoped: true, freshness) with {
                    ETag = result.ETag,
                };
            }

            return TenantListSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                isDegraded);
        }
        catch (EventStoreGatewayException ex) when (IsUnauthorized(ex)) {
            return TenantListSnapshot.Unauthorized();
        }
        catch (EventStoreGatewayException ex) when (IsUnavailableOrInvalid(ex)) {
            return TenantListSnapshot.Error("Tenant query gateway is unavailable.");
        }
    }

    private static TenantsQueryApiRequest CreateListRequest(TenantListRequest request)
        => new(
            AppendQuery("/api/tenants", new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["cursor"] = request.Cursor,
                ["pageSize"] = request.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }),
            ListTenantsQuery.QueryType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            Tenant: SystemTenant,
            Domain: ListTenantsQuery.Domain,
            AggregateId: TenantIndexAggregateId,
            EntityId: null,
            ProjectionType: ListTenantsQuery.ProjectionType);

    private static TenantsQueryApiRequest CreateUserTenantsRequest(string authenticatedUserId, UserTenantMembershipRequest request)
        => new(
            AppendQuery($"/api/users/{EscapePath(request.TargetUserId ?? authenticatedUserId)}/tenants", new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["cursor"] = request.Cursor,
                ["pageSize"] = request.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }),
            GetUserTenantsQuery.QueryType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            Tenant: authenticatedUserId,
            Domain: GetUserTenantsQuery.Domain,
            AggregateId: TenantIndexAggregateId,
            EntityId: request.TargetUserId,
            ProjectionType: GetUserTenantsQuery.ProjectionType);

    private static TenantsQueryApiRequest CreateGlobalAdministratorsRequest(GlobalAdministratorsRequest request)
        => new(
            AppendQuery("/api/global-administrators", new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["cursor"] = request.Cursor,
                ["pageSize"] = request.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }),
            GetGlobalAdministratorsQuery.QueryType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            Tenant: SystemTenant,
            Domain: GetGlobalAdministratorsQuery.Domain,
            AggregateId: GlobalAdministratorsAggregateId,
            EntityId: GlobalAdministratorsAggregateId,
            ProjectionType: GetGlobalAdministratorsQuery.ProjectionType);

    private static TenantsQueryApiRequest CreateTenantAuditRequest(TenantAuditRequest request)
        => new(
            AppendQuery($"/api/tenants/{EscapePath(request.TenantId)}/audit", new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["from"] = request.From?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["to"] = request.To?.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                ["category"] = request.Category?.ToString(),
                ["cursor"] = request.Cursor,
                ["pageSize"] = request.PageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }),
            GetTenantAuditQuery.QueryType,
            JsonSerializer.SerializeToElement(new {
                from = request.From,
                to = request.To,
                category = request.Category?.ToString(),
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            Tenant: SystemTenant,
            Domain: GetTenantAuditQuery.Domain,
            AggregateId: request.TenantId,
            EntityId: request.TenantId,
            ProjectionType: GetTenantAuditQuery.ProjectionType);

    private async Task<(IReadOnlyList<TenantListRow> Rows, bool IsDegraded)> EnrichRowsAsync(
        IReadOnlyList<TenantSummary> summaries,
        TenantFreshnessState freshness,
        CancellationToken cancellationToken) {
        List<TenantListRow> rows = new(summaries.Count);
        bool degraded = false;

        foreach (TenantSummary summary in summaries) {
            TenantListRow row = TenantListRow.FromSummary(summary) with {
                Freshness = freshness,
            };

            try {
                TenantDetail? detail = await LoadTenantDetailAsync(summary.TenantId, cancellationToken)
                    .ConfigureAwait(false);
                if (detail is not null) {
                    int memberCount = detail.Members.Count;
                    int ownerCount = detail.Members.Count(static m => m.Role == TenantRole.TenantOwner);
                    row = row with {
                        MemberCount = TenantCountValue.Known(memberCount),
                        OwnerCount = TenantCountValue.Known(ownerCount),
                    };
                }
            }
            catch (EventStoreGatewayException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden or (int)HttpStatusCode.NotFound or (int)HttpStatusCode.ServiceUnavailable) {
                degraded = true;
            }

            rows.Add(row);
        }

        return (rows, degraded);
    }

    private async Task<TenantDetail?> LoadTenantDetailAsync(string tenantId, CancellationToken cancellationToken) {
        EventStoreQueryResult<TenantDetail> result = await queryClient
            .SendAsync<TenantDetail>(CreateDetailRequest(tenantId), ifNoneMatch: null, cancellationToken)
            .ConfigureAwait(false);

        return result.Payload;
    }

    private static TenantsQueryApiRequest CreateDetailRequest(string tenantId)
        => new(
            $"/api/tenants/{EscapePath(tenantId)}",
            GetTenantQuery.QueryType,
            JsonSerializer.SerializeToElement(new { }),
            Tenant: SystemTenant,
            Domain: GetTenantQuery.Domain,
            AggregateId: tenantId,
            EntityId: tenantId,
            ProjectionType: GetTenantQuery.ProjectionType);

    private static string AppendQuery(string path, IReadOnlyDictionary<string, string?> query) {
        string[] pairs = query
            .Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(static kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}")
            .ToArray();
        return pairs.Length == 0 ? path : path + "?" + string.Join("&", pairs);
    }

    private static string EscapePath(string value)
        => Uri.EscapeDataString(value);

    private static TenantFreshnessState ResolveFreshness(QueryResponseMetadata? metadata, string? eTag) {
        if (metadata?.IsDegraded == true) {
            return TenantFreshnessState.Unknown;
        }

        if (metadata?.IsStale == true) {
            return TenantFreshnessState.Stale;
        }

        return metadata?.IsNotModified == true
            || !string.IsNullOrWhiteSpace(eTag)
            || !string.IsNullOrWhiteSpace(metadata?.ETag)
            || !string.IsNullOrWhiteSpace(metadata?.ProjectionVersion)
                ? TenantFreshnessState.Current
                : TenantFreshnessState.Unknown;
    }

    private static bool IsUnauthorized(EventStoreGatewayException exception)
        => exception.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden;

    private static bool IsUnavailableOrInvalid(EventStoreGatewayException exception)
        => exception.StatusCode is >= 400;

    private static TenantDetailSnapshot MapDetailException(string tenantId, EventStoreGatewayException exception)
        => exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden => TenantDetailSnapshot.Unauthorized(tenantId),
            (int)HttpStatusCode.NotFound => TenantDetailSnapshot.NotFound(tenantId),
            (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.ServiceUnavailable => TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable."),
            _ => TenantDetailSnapshot.Degraded(null, "Tenant detail query gateway returned a safe degraded state."),
        };

    private static UserTenantMembershipSnapshot MapUserTenantException(EventStoreGatewayException exception, string? targetUserId)
        => exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => UserTenantMembershipSnapshot.Unauthorized(targetUserId: targetUserId),
            (int)HttpStatusCode.BadRequest
                => UserTenantMembershipSnapshot.Invalid(targetUserId: targetUserId),
            (int)HttpStatusCode.ServiceUnavailable
                => UserTenantMembershipSnapshot.Unavailable(targetUserId: targetUserId),
            _ => UserTenantMembershipSnapshot.Degraded([], UserTenantMembershipReason.GatewayFailure, targetUserId: targetUserId),
        };

    private static GlobalAdministratorsSnapshot MapGlobalAdministratorsException(EventStoreGatewayException exception)
        => exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => GlobalAdministratorsSnapshot.Unauthorized(),
            (int)HttpStatusCode.BadRequest
                => GlobalAdministratorsSnapshot.Invalid(),
            (int)HttpStatusCode.NotFound or (int)HttpStatusCode.NotImplemented or (int)HttpStatusCode.ServiceUnavailable
                => GlobalAdministratorsSnapshot.Unavailable(),
            _ => GlobalAdministratorsSnapshot.Degraded([], GlobalAdministratorsReason.GatewayFailure),
        };

    private static TenantAuditSnapshot MapTenantAuditException(TenantAuditRequest request, EventStoreGatewayException exception)
        => exception.StatusCode switch {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => TenantAuditSnapshot.Unauthorized(request),
            (int)HttpStatusCode.BadRequest when IsInvalidAuditCursor(exception)
                => TenantAuditSnapshot.InvalidCursor(request),
            (int)HttpStatusCode.NotFound or (int)HttpStatusCode.NotImplemented or (int)HttpStatusCode.ServiceUnavailable
                => TenantAuditSnapshot.Unavailable(request),
            _ => TenantAuditSnapshot.Error(request),
        };

    private static bool IsInvalidAuditCursor(EventStoreGatewayException exception)
        => exception.StatusCode is (int)HttpStatusCode.BadRequest
        && (Contains(exception.ReasonCode, "invalid-cursor")
            || Contains(exception.Reason, "invalid-cursor")
            || Contains(exception.Title, "invalid-cursor")
            || Contains(exception.Type, "invalid-cursor")
            || Contains(exception.Detail, "invalid-cursor"));

    private static bool Contains(string? value, string expected)
        => value?.Contains(expected, StringComparison.OrdinalIgnoreCase) == true;
}
