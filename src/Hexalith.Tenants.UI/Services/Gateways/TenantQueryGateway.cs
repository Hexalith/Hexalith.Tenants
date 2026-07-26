using System.Net;
using System.Security.Cryptography;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Configuration;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using MemoriesOmittedReason = Hexalith.Memories.Contracts.V1.OmittedReason;
using MemoriesScoredResult = Hexalith.Memories.Contracts.V1.ScoredResult;
using MemoriesSearchResult = Hexalith.Memories.Contracts.V1.SearchResult;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    IEventStoreGatewayClient queryClient,
    IUserContextAccessor userContextAccessor,
    MemoriesClient memoriesClient,
    ITenantSearchCursorCodec searchCursorCodec,
    ITenantsBffComposition? bffComposition = null) : ITenantQueryGateway {
    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";
    private const string SearchAxis = "syntactic";
    private const string TenantSourcePrefix = "tenant:";
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;
    private const int MaximumHydrationConcurrency = 8;

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
                result = await queryClient
                    .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(request.TenantId), ifNoneMatch: null, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (result.IsNotModified || result.Payload is null) {
                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail could not be refreshed from current projection evidence.",
                    result.ETag,
                    cancellationToken).ConfigureAwait(false);
            }

            if (!string.Equals(result.Payload.TenantId, request.TenantId, StringComparison.Ordinal)) {
                if (!HasSameTenantDetail(previous, request.TenantId)) {
                    return TenantDetailSnapshot.Unavailable("Tenant detail identity could not be verified.");
                }

                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail projection identity did not match the requested tenant.",
                    result.ETag,
                    cancellationToken).ConfigureAwait(false);
            }

            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            if (result.Metadata?.IsDegraded == true) {
                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail projection is degraded.",
                    result.ETag,
                    cancellationToken).ConfigureAwait(false);
            }

            TenantConfigurationComposition composition;
            try {
                composition = await ComposeTenantDetailAsync(result.Payload, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception) {
                return HasSameTenantDetail(previous, request.TenantId)
                    ? TenantDetailSnapshot.Degraded(
                        previous!.Detail,
                        "Tenant configuration authorization could not be refreshed.",
                        previous.ETag)
                    : TenantDetailSnapshot.Unavailable("Tenant configuration read is unavailable.");
            }
            if (freshness is ReadModelFreshnessState.Stale) {
                return TenantDetailSnapshot.Stale(composition, result.ETag);
            }

            return TenantDetailSnapshot.Ready(composition, result.ETag, freshness);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (EventStoreGatewayException ex) {
            if (ex.StatusCode != (int)HttpStatusCode.Unauthorized
                && ex.StatusCode != (int)HttpStatusCode.Forbidden
                && ex.StatusCode != (int)HttpStatusCode.NotFound
                && ex.StatusCode != (int)HttpStatusCode.BadRequest
                && HasSameTenantDetail(previous, request.TenantId)) {
                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail query gateway returned a safe degraded state.",
                    previous?.ETag,
                    cancellationToken).ConfigureAwait(false);
            }

            return MapDetailException(request.TenantId, ex);
        }
        catch (Exception) {
            if (HasSameTenantDetail(previous, request.TenantId)) {
                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail query gateway returned a safe degraded state.",
                    previous?.ETag,
                    cancellationToken).ConfigureAwait(false);
            }

            return TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable.");
        }
    }

    public Task<TenantConfigurationProjectionProof> GetSetConfigurationProjectionProofAsync(
        SetTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return GetConfigurationProjectionProofAsync(
            request.TenantId,
            request.Key,
            request.Value,
            isRemove: false,
            cancellationToken);
    }

    public Task<TenantConfigurationProjectionProof> GetRemoveConfigurationProjectionProofAsync(
        RemoveTenantConfiguration request,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);
        return GetConfigurationProjectionProofAsync(
            request.TenantId,
            request.Key,
            expectedValue: null,
            isRemove: true,
            cancellationToken);
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

        TenantListRequest canonicalRequest = CanonicalizeListRequest(request);
        if (canonicalRequest.Search is null) {
            return await ListByCursorAsync(canonicalRequest, previous, cancellationToken).ConfigureAwait(false);
        }

        string userId = userContextAccessor.UserId!;
        string scope = TenantSearchCursorScopes.Create(
            userId,
            canonicalRequest.Search,
            canonicalRequest.Status?.ToString(),
            canonicalRequest.SortColumn,
            canonicalRequest.SortDescending,
            canonicalRequest.PageSize);

        bool decoded;
        bool cursorRecovered;
        int rawOffset;
        try {
            decoded = searchCursorCodec.TryDecode(canonicalRequest.SearchCursor, scope, out rawOffset);
        }
        catch (CryptographicException) {
            decoded = false;
            rawOffset = 0;
        }

        cursorRecovered = !decoded;
        if (!decoded) {
            rawOffset = 0;
        }

        try {
            MemoriesSearchResult? result = await SearchMemoriesAsync(canonicalRequest, rawOffset, cancellationToken)
                .ConfigureAwait(false);
            if (!IsValidSearchResult(result, canonicalRequest, rawOffset, allowOffsetBeyondTotal: true)) {
                return await FallBackFromSearchAsync(canonicalRequest, previous, cancellationToken).ConfigureAwait(false);
            }

            MemoriesSearchResult validResult = result!;
            if (rawOffset > validResult.TotalCount
                || (rawOffset > 0
                    && rawOffset == validResult.TotalCount
                    && validResult.Results.Count == 0)) {
                rawOffset = 0;
                cursorRecovered = true;
                result = await SearchMemoriesAsync(canonicalRequest, rawOffset, cancellationToken).ConfigureAwait(false);
                if (!IsValidSearchResult(result, canonicalRequest, rawOffset, allowOffsetBeyondTotal: false)) {
                    return await FallBackFromSearchAsync(canonicalRequest, previous, cancellationToken).ConfigureAwait(false);
                }

                validResult = result!;
            }

            return await BuildAuthoritativeSearchSnapshotAsync(
                canonicalRequest,
                validResult,
                rawOffset,
                scope,
                cursorRecovered,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (IsSearchAvailabilityFailure(ex)) {
            return await FallBackFromSearchAsync(canonicalRequest, previous, cancellationToken).ConfigureAwait(false);
        }
    }

    private static TenantListRequest CanonicalizeListRequest(TenantListRequest request) {
        int pageSize = request.PageSize is >= 1 and <= MaximumPageSize ? request.PageSize : DefaultPageSize;
        string? search = string.IsNullOrWhiteSpace(request.Search) || request.Search.Any(char.IsControl)
            ? null
            : request.Search;
        TenantStatus? status = request.Status is not null && Enum.IsDefined(request.Status.Value)
            ? request.Status
            : null;
        string sort = request.SortColumn switch {
            TenantListSortColumns.Name => TenantListSortColumns.Name,
            TenantListSortColumns.Status => TenantListSortColumns.Status,
            _ => TenantListSortColumns.TenantId,
        };

        return request with {
            PageSize = pageSize,
            Search = search,
            Status = status,
            SortColumn = sort,
            SearchCursor = search is null ? null : request.SearchCursor,
        };
    }

    private async Task<MemoriesSearchResult> SearchMemoriesAsync(
        TenantListRequest request,
        int offset,
        CancellationToken cancellationToken) {
        IReadOnlyDictionary<string, string>? filters = request.Status is null
            ? null
            : new Dictionary<string, string>(StringComparer.Ordinal) {
                ["status"] = request.Status.Value.ToString(),
            };
        var memoriesRequest = new SearchRequest(
            TenantSearchCursorScopes.SearchIndex,
            SearchAxis,
            request.Search,
            MaxResults: request.PageSize,
            Offset: offset,
            Explain: false,
            TokenBudget: null,
            AttributeFilters: filters);
        return await memoriesClient.SearchAsync(memoriesRequest, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsValidSearchResult(
        MemoriesSearchResult? result,
        TenantListRequest request,
        int offset,
        bool allowOffsetBeyondTotal) {
        if (result is null
            || result.Results is null
            || result.TotalCount < 0
            || result.TotalCount > int.MaxValue
            || result.Results.Count > request.PageSize
            || !string.Equals(result.Query, request.Search, StringComparison.Ordinal)
            || result.Degraded
            || result.OmittedCount != 0
            || result.OmittedReason != MemoriesOmittedReason.None
            || result.UnavailableAxes?.Count > 0
            || result.AxesUsed is null
            || result.AxesUsed.Count != 1
            || !string.Equals(result.AxesUsed[0], SearchAxis, StringComparison.Ordinal)
            || (!result.HasIndexedMemoryUnits && (result.TotalCount != 0 || result.Results.Count != 0))) {
            return false;
        }

        if (!allowOffsetBeyondTotal && offset > result.TotalCount) {
            return false;
        }

        if (offset <= result.TotalCount
            && (long)offset + result.Results.Count > result.TotalCount) {
            return false;
        }

        if (offset <= result.TotalCount
            && result.Results.Count != Math.Min((long)request.PageSize, result.TotalCount - offset)) {
            return false;
        }

        foreach (MemoriesScoredResult? hit in result.Results) {
            if (hit is null || !string.Equals(hit.Axis, SearchAxis, StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private async Task<TenantListSnapshot> BuildAuthoritativeSearchSnapshotAsync(
        TenantListRequest request,
        MemoriesSearchResult result,
        int rawOffset,
        string scope,
        bool cursorRecovered,
        CancellationToken cancellationToken) {
        var candidates = new List<(int Ordinal, string TenantId)>(result.Results.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int index = 0; index < result.Results.Count; index++) {
            string? sourceUri = result.Results[index].SourceUri;
            if (sourceUri is null
                || !sourceUri.StartsWith(TenantSourcePrefix, StringComparison.Ordinal)
                || sourceUri.Length == TenantSourcePrefix.Length) {
                continue;
            }

            string tenantId = sourceUri[TenantSourcePrefix.Length..];
            if (tenantId.Any(char.IsControl) || !seen.Add(tenantId)) {
                continue;
            }

            candidates.Add((index, tenantId));
        }

        using var concurrency = new SemaphoreSlim(MaximumHydrationConcurrency, MaximumHydrationConcurrency);
        Task<(int Ordinal, TenantListRow? Row, bool OperationalFailure)>[] hydrationTasks = candidates
            .Select(candidate => HydrateSearchCandidateAsync(candidate, request.Status, concurrency, cancellationToken))
            .ToArray();
        (int Ordinal, TenantListRow? Row, bool OperationalFailure)[] outcomes = await Task
            .WhenAll(hydrationTasks)
            .ConfigureAwait(false);

        bool operationalFailure = outcomes.Any(static outcome => outcome.OperationalFailure);
        IReadOnlyList<TenantListRow> rows = SortSearchRows(
            outcomes
                .Where(static outcome => outcome.Row is not null)
                .OrderBy(static outcome => outcome.Ordinal)
                .Select(static outcome => outcome.Row!)
                .ToArray(),
            request.SortColumn,
            request.SortDescending);

        if (operationalFailure && rows.Count == 0) {
            return await FallBackFromSearchAsync(request, previous: null, cancellationToken).ConfigureAwait(false);
        }

        int nextOffset = checked(rawOffset + result.Results.Count);
        bool hasMore = nextOffset < result.TotalCount;
        string? nextCursor = hasMore ? searchCursorCodec.Encode(scope, nextOffset) : null;
        ReadModelFreshnessState freshness = AggregateFreshness(rows);
        TenantListSurfaceKind kind = rows.Count == 0
            ? TenantListSurfaceKind.FilteredEmpty
            : operationalFailure ? TenantListSurfaceKind.Degraded
            : freshness == ReadModelFreshnessState.Stale ? TenantListSurfaceKind.Stale
            : TenantListSurfaceKind.Ready;

        return new TenantListSnapshot(
            kind,
            rows,
            nextCursor,
            hasMore,
            ETag: null,
            freshness,
            IsDegraded: operationalFailure,
            IsAuthorizationScopedEmpty: rows.Count == 0,
            Reason: operationalFailure ? TenantListReason.SearchPartiallyAvailable : TenantListReason.None,
            Notice: cursorRecovered ? TenantListReason.SearchRefreshed : TenantListReason.None,
            IsAuthoritativeSearch: true,
            PagingRecovered: cursorRecovered);
    }

    private async Task<(int Ordinal, TenantListRow? Row, bool OperationalFailure)> HydrateSearchCandidateAsync(
        (int Ordinal, string TenantId) candidate,
        TenantStatus? status,
        SemaphoreSlim concurrency,
        CancellationToken cancellationToken) {
        await concurrency.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            EventStoreQueryResult<TenantDetail> result = await queryClient
                .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(candidate.TenantId), ifNoneMatch: null, cancellationToken)
                .ConfigureAwait(false);
            TenantDetail? detail = result.Payload;
            if (result.IsNotModified
                || detail is null
                || result.Metadata?.IsDegraded == true
                || !string.Equals(detail.TenantId, candidate.TenantId, StringComparison.Ordinal)
                || detail.Name is null
                || detail.Members is null
                || detail.Members.Any(static member => member is null)) {
                return (candidate.Ordinal, null, true);
            }

            if (status is not null && detail.Status != status.Value) {
                return (candidate.Ordinal, null, false);
            }

            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            return (
                candidate.Ordinal,
                new TenantListRow(
                    detail.TenantId,
                    detail.Name,
                    detail.Status,
                    TenantCountValue.Known(detail.Members.Count),
                    TenantCountValue.Known(detail.Members.Count(static member => member.Role == TenantRole.TenantOwner)),
                    TenantPendingState.Unknown,
                    freshness),
                false);
        }
        catch (EventStoreGatewayException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden or (int)HttpStatusCode.NotFound) {
            return (candidate.Ordinal, null, false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (IsHydrationAvailabilityFailure(ex)) {
            return (candidate.Ordinal, null, true);
        }
        finally {
            _ = concurrency.Release();
        }
    }

    private async Task<TenantListSnapshot> FallBackFromSearchAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken) {
        TenantListRequest fallbackRequest = request with {
            Search = null,
            SearchCursor = null,
            ETag = null,
        };
        TenantListSnapshot? reusable = previous?.IsAuthoritativeSearch == false ? previous : null;
        TenantListSnapshot fallback = await ListByCursorAsync(fallbackRequest, reusable, cancellationToken)
            .ConfigureAwait(false);
        bool pagingRecovered = fallback.Notice == TenantListReason.ListRefreshed;
        return fallback.Kind is TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized
            ? fallback
            : fallback with {
                Notice = TenantListReason.SearchUnavailable,
                IsAuthoritativeSearch = false,
                PagingRecovered = pagingRecovered,
                PagingNotice = pagingRecovered ? TenantListReason.ListRefreshed : TenantListReason.None,
            };
    }

    private static IReadOnlyList<TenantListRow> SortSearchRows(
        IReadOnlyList<TenantListRow> rows,
        string sortColumn,
        bool descending) {
        IOrderedEnumerable<TenantListRow> ordered = sortColumn switch {
            TenantListSortColumns.Name => descending
                ? rows.OrderByDescending(static row => row.Name, StringComparer.OrdinalIgnoreCase)
                : rows.OrderBy(static row => row.Name, StringComparer.OrdinalIgnoreCase),
            TenantListSortColumns.Status => descending
                ? rows.OrderByDescending(static row => row.Status)
                : rows.OrderBy(static row => row.Status),
            _ => descending
                ? rows.OrderByDescending(static row => row.TenantId, StringComparer.Ordinal)
                : rows.OrderBy(static row => row.TenantId, StringComparer.Ordinal),
        };
        return sortColumn == TenantListSortColumns.TenantId
            ? ordered.ToArray()
            : ordered.ThenBy(static row => row.TenantId, StringComparer.Ordinal).ToArray();
    }

    private static ReadModelFreshnessState AggregateFreshness(IReadOnlyList<TenantListRow> rows)
        => rows.Count == 0 || rows.Any(static row => row.Freshness == ReadModelFreshnessState.Unknown)
            ? ReadModelFreshnessState.Unknown
            : rows.Any(static row => row.Freshness == ReadModelFreshnessState.Stale)
                ? ReadModelFreshnessState.Stale
                : ReadModelFreshnessState.Current;

    private static bool IsSearchAvailabilityFailure(Exception exception)
        => exception is MemoriesRemoteException
            or HttpRequestException
            or TimeoutException
            or JsonException
            or InvalidOperationException
            or CryptographicException
            or ArithmeticException
            || exception is OperationCanceledException;

    private static bool IsHydrationAvailabilityFailure(Exception exception)
        => exception is EventStoreGatewayException
            or HttpRequestException
            or TimeoutException
            or JsonException
            || exception is OperationCanceledException;

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

    private async Task<TenantConfigurationProjectionProof> GetConfigurationProjectionProofAsync(
        string tenantId,
        string key,
        string? expectedValue,
        bool isRemove,
        CancellationToken cancellationToken) {
        if (string.IsNullOrWhiteSpace(userContextAccessor.UserId)) {
            return TenantConfigurationProjectionProof.Unavailable(tenantId);
        }

        try {
            EventStoreQueryResult<TenantDetail> result = await queryClient
                .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(tenantId), ifNoneMatch: null, cancellationToken)
                .ConfigureAwait(false);
            TenantDetail? detail = result.Payload;
            if (result.IsNotModified
                || detail is null
                || !string.Equals(detail.TenantId, tenantId, StringComparison.Ordinal)
                || ResolveFreshness(result.Metadata) is not ReadModelFreshnessState.Current) {
                return TenantConfigurationProjectionProof.Unavailable(tenantId);
            }

            bool contains = detail.Configuration.TryGetValue(key, out string? currentValue);
            TenantConfigurationProjectionProofKind kind = isRemove
                ? contains
                    ? TenantConfigurationProjectionProofKind.RemoveNotConfirmed
                    : TenantConfigurationProjectionProofKind.RemoveConfirmed
                : contains && string.Equals(currentValue, expectedValue, StringComparison.Ordinal)
                    ? TenantConfigurationProjectionProofKind.SetConfirmed
                    : TenantConfigurationProjectionProofKind.SetNotConfirmed;
            return TenantConfigurationProjectionProof.Create(tenantId, kind);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            return TenantConfigurationProjectionProof.Unavailable(tenantId);
        }
    }

    private async ValueTask<TenantConfigurationComposition> ComposeTenantDetailAsync(
        TenantDetail detail,
        CancellationToken cancellationToken) {
        if (bffComposition is not null) {
            return await bffComposition.ComposeTenantDetailAsync(detail, cancellationToken).ConfigureAwait(false);
        }

        TenantDetail sanitized = TenantConfigurationSafeComposer.SanitizeDetail(detail);
        return new(
            sanitized,
            TenantConfigurationSafeModel.Unavailable(detail.TenantId),
            TenantConfigurationManagementContext.Unavailable(detail.TenantId, detail.Status));
    }

    private async Task<TenantDetailSnapshot> RetainPreviousTenantDetailAsync(
        string tenantId,
        TenantDetailSnapshot? previous,
        string message,
        string? eTag,
        CancellationToken cancellationToken) {
        if (!HasSameTenantDetail(previous, tenantId)) {
            return TenantDetailSnapshot.Degraded(null, message, eTag);
        }

        TenantConfigurationComposition composition;
        try {
            composition = bffComposition is null
                ? new(
                    TenantConfigurationSafeComposer.SanitizeDetail(previous!.Detail!),
                    TenantConfigurationSafeModel.Unavailable(tenantId),
                    TenantConfigurationManagementContext.Unavailable(tenantId, previous.Detail!.Status))
                : await bffComposition.ReauthorizeTenantDetailAsync(
                    previous!.Detail!,
                    previous.Configuration,
                    degraded: true,
                    cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception) {
            return TenantDetailSnapshot.Degraded(
                previous!.Detail,
                "Tenant configuration authorization could not be refreshed.",
                eTag ?? previous.ETag);
        }

        return TenantDetailSnapshot.DegradedFromComposition(composition, message, eTag ?? previous.ETag);
    }

    private static bool HasSameTenantDetail(TenantDetailSnapshot? previous, string tenantId)
        => previous?.Detail is not null
            && string.Equals(previous.Detail.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(previous.Configuration.TenantId, tenantId, StringComparison.Ordinal);

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
