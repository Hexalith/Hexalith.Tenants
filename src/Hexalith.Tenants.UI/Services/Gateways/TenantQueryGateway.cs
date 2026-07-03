using System.Globalization;
using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

// Alias the Memories search DTOs: Hexalith.Memories.Contracts.V1 also defines TenantStatus/TenantSummary,
// which would collide with the tenant domain contracts if imported wholesale.
using MemoriesScoredResult = Hexalith.Memories.Contracts.V1.ScoredResult;
using MemoriesSearchResult = Hexalith.Memories.Contracts.V1.SearchResult;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    IEventStoreGatewayClient queryClient,
    IUserContextAccessor userContextAccessor,
    MemoriesClient memoriesClient) : ITenantQueryGateway {
    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";

    // Memories-backed cross-set search (search-as-index-only). The dedicated tenants-index holds one
    // curated doc per tenant; the BFF recovers tenant ids ONLY from ScoredResult.SourceUri ("tenant:{id}")
    // and hydrates row data through the ETag-fresh detail path, so a stale index never shows wrong data.
    private const string TenantsSearchIndex = "tenants-index";
    private const string SearchSourceUriPrefix = "tenant:";
    private const string SearchAxis = "syntactic";
    private const string SearchCursorPrefix = "memories-search:";
    private const string SearchUnavailableMessage = "Tenant search is temporarily unavailable; showing the full tenant list.";
    private const string SearchHydrationDegradedMessage = "Tenant search results could not be verified; showing a degraded search state.";

    public async Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

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

            if (result.Metadata?.IsStale == true) {
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

            if (result.Metadata?.IsStale == true) {
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

            if (result.Metadata?.IsStale == true) {
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

        if (result.Metadata?.IsStale == true) {
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

        // Empty/whitespace term -> the unchanged cursor-list path (no Memories call). A non-empty term is
        // a cross-set search served by the Memories tenants-index; rows are still hydrated through the
        // ETag-fresh detail path, so a stale index never shows wrong row data (D6).
        return string.IsNullOrWhiteSpace(request.Search)
            ? await ListByCursorAsync(request, previous, cancellationToken).ConfigureAwait(false)
            : await SearchTenantsAsync(request, previous, cancellationToken).ConfigureAwait(false);
    }

    private async Task<TenantListSnapshot> ListByCursorAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken) {
        try {
            SubmitQueryRequest query = CreateListRequest(request);
            EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await queryClient
                .SubmitQueryAsync<PaginatedResult<TenantSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                if (previous is null) {
                    return TenantListSnapshot.Degraded([], "Tenant list was unchanged, but no cached server snapshot is available.");
                }

                ReadModelFreshnessState notModifiedFreshness = ResolveNotModifiedFreshness(result.Metadata, previous.Freshness);
                TenantListSurfaceKind kind = ResolveTenantListKindForFreshness(previous, notModifiedFreshness);
                return previous with {
                    Kind = kind,
                    Freshness = notModifiedFreshness,
                    ETag = result.ETag ?? previous.ETag,
                    ErrorMessage = ResolveTenantListErrorMessageForNotModified(previous, kind),
                };
            }

            PaginatedResult<TenantSummary> payload = result.Payload ?? new PaginatedResult<TenantSummary>([], null, false);
            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            (IReadOnlyList<TenantListRow> rows, bool enrichmentDegraded) = await EnrichRowsAsync(
                payload.Items,
                freshness,
                cancellationToken).ConfigureAwait(false);
            bool isDegraded = enrichmentDegraded || result.Metadata?.IsDegraded == true;

            if (isDegraded) {
                return TenantListSnapshot.Ready(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
                    ReadModelFreshnessState.Unknown,
                    isDegraded: true);
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                return TenantListSnapshot.Ready(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
                    freshness,
                    isDegraded: false);
            }

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

    private async Task<TenantListSnapshot> SearchTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken) {
        MemoriesSearchResult searchResult;
        int offset = ParseSearchOffset(request.Cursor);
        try {
            searchResult = await memoriesClient
                .SearchAsync(
                    new SearchRequest(
                        TenantId: TenantsSearchIndex,
                        Axis: SearchAxis,
                        Query: request.Search,
                        MaxResults: request.PageSize,
                        Offset: offset,
                        AttributeFilters: CreateSearchAttributeFilters(request.Status)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (IsMemoriesSearchUnavailable(ex, cancellationToken)) {
            // Memories unavailable (timeout/503/remote error/unconfigured endpoint) -> never let the
            // exception reach the circuit; degrade to the cursor list with a safe notice (AC9).
            return await SearchUnavailableFallbackAsync(request, previous, cancellationToken).ConfigureAwait(false);
        }

        // Syntactic axis down / partial degradation reported in-band -> same non-blocking fallback.
        if (searchResult.Degraded
            || searchResult.UnavailableAxes?.Any(static axis => string.Equals(axis, SearchAxis, StringComparison.OrdinalIgnoreCase)) == true) {
            return await SearchUnavailableFallbackAsync(request, previous, cancellationToken).ConfigureAwait(false);
        }

        IReadOnlyList<string> tenantIds = ParseTenantIds(searchResult.Results);
        if (tenantIds.Count == 0) {
            return TenantListSnapshot.FilteredEmpty();
        }

        (IReadOnlyList<TenantListRow> rows, bool degraded) =
            await HydrateSearchRowsAsync(tenantIds, request.Status, cancellationToken).ConfigureAwait(false);

        if (rows.Count == 0) {
            // A non-empty match-set that cannot hydrate authoritative rows is degraded. A clean exact-filter
            // match-set with no visible rows remains a filtered-empty state, not an error.
            return degraded
                ? TenantListSnapshot.Degraded([], SearchHydrationDegradedMessage)
                : TenantListSnapshot.FilteredEmpty();
        }

        int nextOffset = offset + searchResult.Results.Count;
        bool hasMore = searchResult.TotalCount > nextOffset;

        // BM25 score order from Memories differs from the cursor list's deterministic id/name order; the
        // visible page is re-sorted client-side (ApplyVisibleRows). Search cursors are opaque offset cursors
        // over the Memories result window and do not carry a list ETag.
        return TenantListSnapshot.Ready(
            rows,
            nextCursor: hasMore ? CreateSearchCursor(nextOffset) : null,
            hasMore,
            eTag: null,
            AggregateRowFreshness(rows),
            degraded);
    }

    // Recover tenant ids ONLY from ScoredResult.SourceUri ("tenant:{id}"): never parse ContentSnippet
    // (200-char truncated) and never depend on cloudevent.subject (not mapped into the hit). Preserves the
    // BM25 score order Memories returns and dedupes defensively; malformed hits are dropped.
    private static IReadOnlyList<string> ParseTenantIds(IReadOnlyList<MemoriesScoredResult> results) {
        List<string> tenantIds = new(results.Count);
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (MemoriesScoredResult result in results) {
            string sourceUri = result.SourceUri;
            if (string.IsNullOrWhiteSpace(sourceUri)
                || !sourceUri.StartsWith(SearchSourceUriPrefix, StringComparison.Ordinal)) {
                continue;
            }

            string tenantId = sourceUri[SearchSourceUriPrefix.Length..];
            if (!string.IsNullOrWhiteSpace(tenantId) && seen.Add(tenantId)) {
                tenantIds.Add(tenantId);
            }
        }

        return tenantIds;
    }

    // Hydrate each match-set id through the existing ETag-fresh detail read (GetTenantAsync already maps
    // 404/403/503 to non-throwing snapshots and resolves freshness via ResolveFreshness — never from
    // Memories). Member/owner counts mirror EnrichRowsAsync. Status is applied twice by design: a structured
    // exact AttributeFilter narrows the Memories match-set (see SearchTenantsAsync), and this hydrated
    // authoritative TenantDetail.Status is the source-of-truth re-check (the index attribute can lag a status
    // change), never fuzzy BM25 text. Ids that hydrate to not-found/forbidden/unavailable are dropped
    // (degraded, not error).
    private async Task<(IReadOnlyList<TenantListRow> Rows, bool IsDegraded)> HydrateSearchRowsAsync(
        IReadOnlyList<string> tenantIds,
        TenantStatus? statusFilter,
        CancellationToken cancellationToken) {
        List<TenantListRow> rows = new(tenantIds.Count);
        bool degraded = false;

        foreach (string tenantId in tenantIds) {
            TenantDetailSnapshot detailSnapshot = await GetTenantAsync(
                new TenantDetailRequest(tenantId),
                previous: null,
                cancellationToken).ConfigureAwait(false);

            if (detailSnapshot.Detail is not { } detail) {
                // Not-found / forbidden / unavailable id from a stale or cross-tenant index entry.
                degraded = true;
                continue;
            }

            if (statusFilter is { } status && detail.Status != status) {
                degraded = true;
                continue;
            }

            int memberCount = detail.Members.Count;
            int ownerCount = detail.Members.Count(static m => m.Role == TenantRole.TenantOwner);
            rows.Add(TenantListRow.FromSummary(new TenantSummary(detail.TenantId, detail.Name, detail.Status)) with {
                MemberCount = TenantCountValue.Known(memberCount),
                OwnerCount = TenantCountValue.Known(ownerCount),
                Freshness = detailSnapshot.Freshness,
            });
        }

        return (rows, degraded);
    }

    private static IReadOnlyDictionary<string, string>? CreateSearchAttributeFilters(TenantStatus? status)
        => status is { } value
            ? new Dictionary<string, string>(StringComparer.Ordinal) { ["status"] = value.ToString() }
            : null;

    private static int ParseSearchOffset(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor)
            || !cursor.StartsWith(SearchCursorPrefix, StringComparison.Ordinal)
            || !int.TryParse(cursor[SearchCursorPrefix.Length..], out int offset)
            || offset < 0)
        {
            return 0;
        }

        return offset;
    }

    private static string CreateSearchCursor(int offset)
        => SearchCursorPrefix + offset.ToString(CultureInfo.InvariantCulture);

    private async Task<TenantListSnapshot> SearchUnavailableFallbackAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken) {
        // Non-blocking: fall back to the unfiltered cursor list and overlay a support-safe degraded notice
        // so the operator can keep browsing. Auth/error states from the cursor list are surfaced as-is.
        TenantListSnapshot cursorSnapshot = await ListByCursorAsync(
            request with { Search = null },
            previous,
            cancellationToken).ConfigureAwait(false);

        return cursorSnapshot.Kind is TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized
            ? cursorSnapshot
            : cursorSnapshot with {
                Kind = TenantListSurfaceKind.Degraded,
                IsDegraded = true,
                ErrorMessage = SearchUnavailableMessage,
            };
    }

    private static bool IsMemoriesSearchUnavailable(Exception exception, CancellationToken cancellationToken)
        => exception switch {
            MemoriesRemoteException => true,
            HttpRequestException => true,
            // MemoriesClient with no configured BaseAddress (Memories:BaseAddress unset) throws this.
            InvalidOperationException => true,
            // Timeout from the client's request cancellation, not the caller cancelling the operation.
            TaskCanceledException or OperationCanceledException => !cancellationToken.IsCancellationRequested,
            _ => false,
        };

    // The no-search cursor path carries only cursor + pageSize: the tenant read backend is consume-only
    // (no server-side filter on ListTenantsQuery), so Search/Status are not server query parameters here.
    // A non-empty Search term is handled cross-set by SearchTenantsAsync (above); Status is applied to the
    // hydrated authoritative detail. Filters are therefore never silently dropped — they are routed.
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
        if (metadata?.IsDegraded == true) {
            return ReadModelFreshnessState.Unknown;
        }

        return metadata?.IsStale switch {
            true => ReadModelFreshnessState.Stale,
            false => ReadModelFreshnessState.Current,
            _ => ReadModelFreshnessState.Unknown,
        };
    }

    private static ReadModelFreshnessState ResolveNotModifiedFreshness(
        QueryResponseMetadata? metadata,
        ReadModelFreshnessState previous)
        => metadata?.IsDegraded == true || metadata?.IsStale is not null
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

    private static string? ResolveTenantListErrorMessageForNotModified(
        TenantListSnapshot previous,
        TenantListSurfaceKind resolvedKind)
        => resolvedKind == TenantListSurfaceKind.Degraded ? previous.ErrorMessage : null;

    private static bool HasAuditFilters(TenantAuditSnapshot snapshot)
        => snapshot.From is not null || snapshot.To is not null || !string.IsNullOrWhiteSpace(snapshot.Category);

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
