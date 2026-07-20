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
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    IEventStoreGatewayClient queryClient,
    IUserContextAccessor userContextAccessor) : ITenantQueryGateway {
    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";

    public async Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(userContextAccessor.UserId)) {
            return TenantDetailSnapshot.Unauthorized(request.TenantId);
        }

        try {
            EventStoreQueryResult<TenantDetail> result = await queryClient
                .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(request.TenantId), request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                if (previous?.Detail is null) {
                    return TenantDetailSnapshot.Degraded(null, "Tenant detail was unchanged, but no cached server snapshot is available.", result.ETag);
                }

                // A 304 means the cached snapshot is unchanged, not that a degraded/stale
                // snapshot recovered. Only an explicit freshness header can change truth state.
                ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
                return previous with {
                    Kind = ResolveDetailKindForFreshness(previous.Kind, notModifiedFreshness),
                    Freshness = notModifiedFreshness,
                    ETag = result.ETag ?? previous.ETag,
                };
            }

            if (result.Payload is null) {
                return TenantDetailSnapshot.Unknown("Tenant detail projection returned no payload.", result.ETag);
            }

            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            if (result.Metadata?.IsDegraded == true) {
                return TenantDetailSnapshot.Degraded(result.Payload, "Tenant detail projection is degraded.", result.ETag);
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                return TenantDetailSnapshot.Stale(result.Payload, result.ETag);
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
            SubmitQueryRequest query = CreateUserTenantsRequest(authenticatedUserId, request);
            EventStoreQueryResult<PaginatedResult<UserTenantMembership>> result = await queryClient
                .SubmitQueryAsync<PaginatedResult<UserTenantMembership>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                if (previous is null || !string.Equals(previous.TargetUserId, request.TargetUserId, StringComparison.Ordinal)) {
                    return UserTenantMembershipSnapshot.Degraded(
                        [],
                        UserTenantMembershipReason.NotModifiedWithoutSnapshot,
                        result.ETag,
                        targetUserId: request.TargetUserId);
                }

                ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
                return previous with {
                    ETag = result.ETag ?? previous.ETag,
                    Kind = ResolveUserTenantsKindForFreshness(previous, notModifiedFreshness),
                    Freshness = notModifiedFreshness,
                    Reason = ResolveUserTenantsReasonForFreshness(previous, notModifiedFreshness),
                };
            }

            PaginatedResult<UserTenantMembership> payload = result.Payload
                ?? new PaginatedResult<UserTenantMembership>([], null, false);
            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            IReadOnlyList<UserTenantMembershipRow> rows = payload.Items
                .Select(m => UserTenantMembershipRow.FromMembership(m) with {
                    Freshness = freshness,
                })
                .ToArray();

            if (result.Metadata?.IsDegraded == true) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown }).ToArray();
                return UserTenantMembershipSnapshot.Degraded(
                    rows,
                    UserTenantMembershipReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore,
                    request.TargetUserId);
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
                return UserTenantMembershipSnapshot.Stale(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
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
            SubmitQueryRequest query = CreateGlobalAdministratorsRequest(request);
            EventStoreQueryResult<PaginatedResult<GlobalAdministratorSummary>> result = await queryClient
                .SubmitQueryAsync<PaginatedResult<GlobalAdministratorSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                if (previous is null) {
                    return GlobalAdministratorsSnapshot.Degraded(
                        [],
                        GlobalAdministratorsReason.NotModifiedWithoutSnapshot,
                        result.ETag);
                }

                ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
                return previous with {
                    ETag = result.ETag ?? previous.ETag,
                    Kind = ResolveGlobalAdministratorsKindForFreshness(previous, notModifiedFreshness),
                    Freshness = notModifiedFreshness,
                    Reason = ResolveGlobalAdministratorsReasonForFreshness(previous, notModifiedFreshness),
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
                        Freshness = ReadModelFreshnessState.Unknown,
                    };
            }

            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            IReadOnlyList<GlobalAdministratorRow> rows = payload.Items
                .Select(m => GlobalAdministratorRow.FromSummary(m) with { Freshness = freshness })
                .ToArray();

            if (result.Metadata?.IsDegraded == true) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown }).ToArray();
                return GlobalAdministratorsSnapshot.Degraded(
                    rows,
                    GlobalAdministratorsReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore);
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
                return GlobalAdministratorsSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag);
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

        if (string.IsNullOrWhiteSpace(userContextAccessor.UserId)) {
            return TenantAuditSnapshot.Unauthorized(request);
        }

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
        SubmitQueryRequest query = CreateTenantAuditRequest(request);
        EventStoreQueryResult<PaginatedResult<TenantAuditEntry>> result = await queryClient
            .SubmitQueryAsync<PaginatedResult<TenantAuditEntry>>(query, request.ETag, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsNotModified) {
            if (previous is null || !previous.MatchesScope(request)) {
                return TenantAuditSnapshot.Degraded(
                    [],
                    TenantAuditReason.NotModifiedWithoutSnapshot,
                    request,
                    result.ETag);
            }

            ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
            return previous with {
                ETag = result.ETag ?? previous.ETag,
                Kind = ResolveTenantAuditKindForFreshness(previous, notModifiedFreshness),
                Freshness = notModifiedFreshness,
                Reason = ResolveTenantAuditReasonForFreshness(previous, notModifiedFreshness),
            };
        }

        PaginatedResult<TenantAuditEntry>? payload = result.Payload;
        if (payload is null) {
            return previous is not null && previous.MatchesScope(request)
                ? previous with {
                    Kind = TenantAuditSurfaceKind.Degraded,
                    Reason = TenantAuditReason.MissingPayload,
                    ETag = result.ETag ?? previous.ETag,
                    Freshness = ReadModelFreshnessState.Unknown,
                }
                : TenantAuditSnapshot.Degraded([], TenantAuditReason.MissingPayload, request, result.ETag);
        }

        ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
        IReadOnlyList<TenantAuditRow> rows = payload.Items
            .Select(entry => TenantAuditRow.FromEntry(entry, freshness))
            .ToArray();

        if (result.Metadata?.IsDegraded == true) {
            rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown }).ToArray();
            return TenantAuditSnapshot.Degraded(
                rows,
                TenantAuditReason.ProjectionDegraded,
                request,
                result.ETag,
                payload.Cursor,
                payload.HasMore);
        }

        if (freshness is ReadModelFreshnessState.Stale) {
            rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
            return TenantAuditSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag, request);
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

        if (string.IsNullOrWhiteSpace(userContextAccessor.UserId)) {
            return TenantListSnapshot.Unauthorized();
        }

        bool searchRequested = !string.IsNullOrWhiteSpace(request.Search);
        TenantListSnapshot snapshot = await ListByCursorAsync(
            searchRequested ? request with { Search = null } : request,
            previous,
            cancellationToken).ConfigureAwait(false);

        // SEARCH-CURSOR-1 is not verified. Keep the authorization-safe ordinary cursor list usable and
        // report a localized, non-blocking notice instead of using the former plaintext offset cursor.
        return searchRequested
            && snapshot.Kind is not (TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized)
            && snapshot.Notice == TenantListReason.None
                ? snapshot with { Notice = TenantListReason.SearchUnavailable }
                : snapshot;
    }

    private async Task<TenantListSnapshot> ListByCursorAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ListByCursorCoreAsync(request, previous, cancellationToken).ConfigureAwait(false);
        }
        catch (EventStoreGatewayException ex) when (IsInvalidListCursor(ex))
        {
            TenantListRequest firstPageRequest = request with
            {
                Cursor = null,
                ETag = null,
            };

            try
            {
                TenantListSnapshot recovered = await ListByCursorCoreAsync(
                    firstPageRequest,
                    previous: null,
                    cancellationToken).ConfigureAwait(false);
                return recovered with { Notice = TenantListReason.ListRefreshed };
            }
            catch (EventStoreGatewayException retryException)
            {
                return MapTenantListException(retryException);
            }
        }
        catch (EventStoreGatewayException ex)
        {
            return MapTenantListException(ex);
        }
    }

    private async Task<TenantListSnapshot> ListByCursorCoreAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken)
    {
        SubmitQueryRequest query = CreateListRequest(request);
        EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await queryClient
            .SubmitQueryAsync<PaginatedResult<TenantSummary>>(query, request.ETag, cancellationToken)
            .ConfigureAwait(false);

        if (result.IsNotModified)
        {
            if (previous is null)
            {
                return TenantListSnapshot.Degraded([], TenantListReason.NotModifiedWithoutSnapshot);
            }

            ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
            TenantListSurfaceKind kind = ResolveTenantListKindForFreshness(previous, notModifiedFreshness);
            return previous with
            {
                Kind = kind,
                Freshness = notModifiedFreshness,
                ETag = result.ETag ?? previous.ETag,
                Reason = ResolveTenantListReasonForNotModified(previous, kind),
            };
        }

        PaginatedResult<TenantSummary> payload = result.Payload ?? new PaginatedResult<TenantSummary>([], null, false);
        ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
        (IReadOnlyList<TenantListRow> rows, bool enrichmentDegraded) = await EnrichRowsAsync(
            payload.Items,
            freshness,
            cancellationToken).ConfigureAwait(false);
        bool projectionDegraded = result.Metadata?.IsDegraded == true;
        bool isDegraded = enrichmentDegraded || projectionDegraded;

        if (isDegraded)
        {
            return TenantListSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                ReadModelFreshnessState.Unknown,
                isDegraded: true) with
            {
                Reason = projectionDegraded
                    ? TenantListReason.ProjectionDegraded
                    : TenantListReason.RowEnrichmentUnavailable,
            };
        }

        if (freshness is ReadModelFreshnessState.Stale)
        {
            return TenantListSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                isDegraded: false);
        }

        if (rows.Count == 0)
        {
            return TenantListSnapshot.Empty(isAuthorizationScoped: true, freshness) with
            {
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

    // The ordinary cursor path carries only cursor + pageSize. Search, filter, and sort are not server
    // query fields in the current tenant-list contract; they reset the cursor and affect only the authorized
    // page already returned. Protected whole-set search remains owned by Story 1.9/SEARCH-CURSOR-1.
    private static SubmitQueryRequest CreateListRequest(TenantListRequest request)
        => new(
            SystemTenant,
            ListTenantsQuery.Domain,
            TenantIndexAggregateId,
            ListTenantsQuery.QueryType,
            ListTenantsQuery.ProjectionType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: null);

    private static SubmitQueryRequest CreateUserTenantsRequest(string authenticatedUserId, UserTenantMembershipRequest request)
        => new(
            SystemTenant,
            GetUserTenantsQuery.Domain,
            TenantIndexAggregateId,
            GetUserTenantsQuery.QueryType,
            GetUserTenantsQuery.ProjectionType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: request.TargetUserId ?? authenticatedUserId);

    private static SubmitQueryRequest CreateGlobalAdministratorsRequest(GlobalAdministratorsRequest request)
        => new(
            SystemTenant,
            GetGlobalAdministratorsQuery.Domain,
            GlobalAdministratorsAggregateId,
            GetGlobalAdministratorsQuery.QueryType,
            GetGlobalAdministratorsQuery.ProjectionType,
            JsonSerializer.SerializeToElement(new {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: GlobalAdministratorsAggregateId);

    private static SubmitQueryRequest CreateTenantAuditRequest(TenantAuditRequest request)
        => new(
            SystemTenant,
            GetTenantAuditQuery.Domain,
            request.TenantId,
            GetTenantAuditQuery.QueryType,
            GetTenantAuditQuery.ProjectionType,
            JsonSerializer.SerializeToElement(new {
                from = request.From,
                to = request.To,
                category = request.Category?.ToString(),
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: request.TenantId);

    private async Task<(IReadOnlyList<TenantListRow> Rows, bool IsDegraded)> EnrichRowsAsync(
        IReadOnlyList<TenantSummary> summaries,
        ReadModelFreshnessState freshness,
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
            .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(tenantId), ifNoneMatch: null, cancellationToken)
            .ConfigureAwait(false);

        return result.Payload;
    }

    private static SubmitQueryRequest CreateDetailRequest(string tenantId)
        => new(
            SystemTenant,
            GetTenantQuery.Domain,
            tenantId,
            GetTenantQuery.QueryType,
            GetTenantQuery.ProjectionType,
            Payload: null,
            EntityId: tenantId);

    private static ReadModelFreshnessState ResolveFreshness(QueryResponseMetadata? metadata) {
        if (metadata is null
            || metadata.Provenance is not QueryResponseProvenance.ProjectionBacked
            || metadata.IsDegraded == true) {
            return ReadModelFreshnessState.Unknown;
        }

        if (metadata.Lifecycle is not ProjectionLifecycleState.Unknown) {
            return ProjectionLifecyclePolicy.Normalize(metadata.Lifecycle, metadata.Provenance) switch {
                ProjectionLifecycleState.Current => ReadModelFreshnessState.Current,
                ProjectionLifecycleState.Stale => ReadModelFreshnessState.Stale,
                _ => ReadModelFreshnessState.Unknown,
            };
        }

        return metadata.IsStale switch {
            true => ReadModelFreshnessState.Stale,
            false => ReadModelFreshnessState.Current,
            _ => ReadModelFreshnessState.Unknown,
        };
    }

    private static ReadModelFreshnessState ResolveNotModifiedFreshness(
        QueryResponseMetadata? metadata,
        ReadModelFreshnessState previous)
        => metadata is null
            || metadata.Provenance is not QueryResponseProvenance.ProjectionBacked
            ? ReadModelFreshnessState.Unknown
            : metadata.IsDegraded == true
                || metadata.IsStale is not null
                || metadata.Lifecycle is not ProjectionLifecycleState.Unknown
            ? ResolveFreshness(metadata)
            : previous;

    private static ReadModelFreshnessState AggregateRowFreshness(IReadOnlyList<TenantListRow> rows) {
        if (rows.Any(static row => row.Freshness == ReadModelFreshnessState.Stale)) {
            return ReadModelFreshnessState.Stale;
        }

        if (rows.Any(static row => row.Freshness == ReadModelFreshnessState.Unknown)) {
            return ReadModelFreshnessState.Unknown;
        }

        if (rows.Any(static row => row.Freshness == ReadModelFreshnessState.Aging)) {
            return ReadModelFreshnessState.Aging;
        }

        return ReadModelFreshnessState.Current;
    }

    private static TenantDetailSurfaceKind ResolveDetailKindForFreshness(
        TenantDetailSurfaceKind previous,
        ReadModelFreshnessState freshness)
        => previous is TenantDetailSurfaceKind.Ready or TenantDetailSurfaceKind.Stale
            ? freshness == ReadModelFreshnessState.Stale ? TenantDetailSurfaceKind.Stale : TenantDetailSurfaceKind.Ready
            : previous;

    private static UserTenantMembershipSurfaceKind ResolveUserTenantsKindForFreshness(
        UserTenantMembershipSnapshot previous,
        ReadModelFreshnessState freshness)
        => previous.Kind is UserTenantMembershipSurfaceKind.Ready
            or UserTenantMembershipSurfaceKind.Empty
            or UserTenantMembershipSurfaceKind.Stale
                ? freshness == ReadModelFreshnessState.Stale
                    ? UserTenantMembershipSurfaceKind.Stale
                    : previous.Rows.Count == 0 ? UserTenantMembershipSurfaceKind.Empty : UserTenantMembershipSurfaceKind.Ready
                : previous.Kind;

    private static UserTenantMembershipReason ResolveUserTenantsReasonForFreshness(
        UserTenantMembershipSnapshot previous,
        ReadModelFreshnessState freshness)
        => previous.Kind is UserTenantMembershipSurfaceKind.Ready
            or UserTenantMembershipSurfaceKind.Empty
            or UserTenantMembershipSurfaceKind.Stale
                ? freshness == ReadModelFreshnessState.Stale
                    ? UserTenantMembershipReason.ProjectionStale
                    : UserTenantMembershipReason.None
                : previous.Reason;

    private static GlobalAdministratorsSurfaceKind ResolveGlobalAdministratorsKindForFreshness(
        GlobalAdministratorsSnapshot previous,
        ReadModelFreshnessState freshness)
        => previous.Kind is GlobalAdministratorsSurfaceKind.Ready
            or GlobalAdministratorsSurfaceKind.Empty
            or GlobalAdministratorsSurfaceKind.Stale
                ? freshness == ReadModelFreshnessState.Stale
                    ? GlobalAdministratorsSurfaceKind.Stale
                    : previous.Rows.Count == 0 ? GlobalAdministratorsSurfaceKind.Empty : GlobalAdministratorsSurfaceKind.Ready
                : previous.Kind;

    private static GlobalAdministratorsReason ResolveGlobalAdministratorsReasonForFreshness(
        GlobalAdministratorsSnapshot previous,
        ReadModelFreshnessState freshness)
        => previous.Kind is GlobalAdministratorsSurfaceKind.Ready
            or GlobalAdministratorsSurfaceKind.Empty
            or GlobalAdministratorsSurfaceKind.Stale
                ? freshness == ReadModelFreshnessState.Stale
                    ? GlobalAdministratorsReason.ProjectionStale
                    : GlobalAdministratorsReason.None
                : previous.Reason;

    private static TenantAuditSurfaceKind ResolveTenantAuditKindForFreshness(
        TenantAuditSnapshot previous,
        ReadModelFreshnessState freshness) {
        if (previous.Kind is not (TenantAuditSurfaceKind.Ready
            or TenantAuditSurfaceKind.Empty
            or TenantAuditSurfaceKind.FilteredEmpty
            or TenantAuditSurfaceKind.Stale
            or TenantAuditSurfaceKind.ListRefreshed)) {
            return previous.Kind;
        }

        if (freshness == ReadModelFreshnessState.Stale) {
            return TenantAuditSurfaceKind.Stale;
        }

        if (previous.Kind == TenantAuditSurfaceKind.ListRefreshed) {
            return TenantAuditSurfaceKind.ListRefreshed;
        }

        if (previous.Rows.Count != 0) {
            return TenantAuditSurfaceKind.Ready;
        }

        return HasAuditFilters(previous) ? TenantAuditSurfaceKind.FilteredEmpty : TenantAuditSurfaceKind.Empty;
    }

    private static TenantAuditReason ResolveTenantAuditReasonForFreshness(
        TenantAuditSnapshot previous,
        ReadModelFreshnessState freshness)
        => previous.Kind is TenantAuditSurfaceKind.Ready
            or TenantAuditSurfaceKind.Empty
            or TenantAuditSurfaceKind.FilteredEmpty
            or TenantAuditSurfaceKind.Stale
            or TenantAuditSurfaceKind.ListRefreshed
                ? freshness == ReadModelFreshnessState.Stale
                    ? TenantAuditReason.ProjectionStale
                    : previous.Kind == TenantAuditSurfaceKind.ListRefreshed
                        ? TenantAuditReason.ListRefreshed
                        : TenantAuditReason.None
                : previous.Reason;

    private static TenantListSurfaceKind ResolveTenantListKindForFreshness(
        TenantListSnapshot previous,
        ReadModelFreshnessState freshness) {
        if (previous.Kind == TenantListSurfaceKind.Degraded) {
            return TenantListSurfaceKind.Degraded;
        }

        if (freshness == ReadModelFreshnessState.Stale) {
            return TenantListSurfaceKind.Stale;
        }

        if (previous.Kind == TenantListSurfaceKind.FilteredEmpty) {
            return TenantListSurfaceKind.FilteredEmpty;
        }

        return previous.Rows.Count == 0 ? TenantListSurfaceKind.Empty : TenantListSurfaceKind.Ready;
    }

    private static TenantListReason ResolveTenantListReasonForNotModified(
        TenantListSnapshot previous,
        TenantListSurfaceKind resolvedKind)
        => resolvedKind == TenantListSurfaceKind.Degraded ? previous.Reason : TenantListReason.None;

    private static bool HasAuditFilters(TenantAuditSnapshot snapshot)
        => snapshot.From is not null || snapshot.To is not null || !string.IsNullOrWhiteSpace(snapshot.Category);

    private static bool IsUnauthorized(EventStoreGatewayException exception)
        => exception.StatusCode is (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden;

    private static TenantListSnapshot MapTenantListException(EventStoreGatewayException exception)
        => IsUnauthorized(exception)
            ? TenantListSnapshot.Unauthorized()
            : TenantListSnapshot.Error();

    private static bool IsInvalidListCursor(EventStoreGatewayException exception)
        => exception.StatusCode == (int)HttpStatusCode.BadRequest
        && string.Equals(exception.ReasonCode, "invalid-cursor", StringComparison.OrdinalIgnoreCase);

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
