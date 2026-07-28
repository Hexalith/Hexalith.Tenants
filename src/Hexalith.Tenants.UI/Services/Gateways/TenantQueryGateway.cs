using System.Net;
using System.Runtime.CompilerServices;
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

using Microsoft.Extensions.Logging;

using MemoriesOmittedReason = Hexalith.Memories.Contracts.V1.OmittedReason;
using MemoriesScoredResult = Hexalith.Memories.Contracts.V1.ScoredResult;
using MemoriesSearchResult = Hexalith.Memories.Contracts.V1.SearchResult;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    IEventStoreGatewayClient queryClient,
    IUserContextAccessor userContextAccessor,
    MemoriesClient memoriesClient,
    ITenantSearchCursorCodec searchCursorCodec,
    ITenantsBffComposition? bffComposition = null,
    ILogger<TenantQueryGateway>? logger = null) : ITenantQueryGateway {
    /// <summary>The maximum number of concurrent authoritative hydration reads for one raw search page.</summary>
    internal const int MaximumHydrationConcurrency = 8;

    /// <summary>
    /// Maximum accepted canonical search length. This is the workspace URL-state bound itself, not a copy of
    /// it: a second literal could drift, and a gateway that rejected a term the workspace accepted would
    /// leave the surface reporting an active search while serving the ordinary list.
    /// </summary>
    internal const int MaximumSearchLength = TenantWorkspaceState.MaximumSearchLength;

    /// <summary>Reason code recorded when the Memories index call itself could not be completed.</summary>
    internal const string SearchIndexUnavailableReasonCode = "search-index-unavailable";

    /// <summary>Reason code recorded when the Memories response violated a safety invariant.</summary>
    internal const string SearchResponseInvalidReasonCode = "search-response-invalid";

    /// <summary>Reason code recorded when the protected search cursor could not be produced.</summary>
    internal const string SearchCursorProtectionUnavailableReasonCode = "search-cursor-protection-unavailable";

    /// <summary>Reason code recorded when every authoritative hydration read failed operationally.</summary>
    internal const string SearchHydrationUnavailableReasonCode = "search-hydration-unavailable";

    /// <summary>Identifies the signal raised when authoritative search fell back to a usable ordinary list.</summary>
    internal static readonly EventId SearchDegradedToOrdinaryListEvent = new(1901, "AuthoritativeTenantSearchDegraded");

    /// <summary>Identifies the signal raised when the ordinary-list fallback was also unavailable.</summary>
    internal static readonly EventId SearchAndOrdinaryListUnavailableEvent = new(1902, "AuthoritativeTenantSearchAndListUnavailable");

    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";
    private const string SearchAxis = "syntactic";
    private const string TenantSourcePrefix = "tenant:";
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    public async Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default) {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(userContextAccessor.UserId)) {
            return TenantDetailSnapshot.Unauthorized(request.TenantId);
        }

        try {
            // Deliberately unconditional. Safe rows cannot be reconstructed from an already-filtered
            // model, so a 304 always forces a second full read anyway; sending the validator merely
            // doubled the backend queries in the common case. The not-modified branch below is kept
            // as defence for a client that returns 304 regardless.
            EventStoreQueryResult<TenantDetail> result = await queryClient
                .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(request.TenantId), ifNoneMatch: null, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified) {
                result = await queryClient
                    .SubmitQueryAsync<TenantDetail>(CreateDetailRequest(request.TenantId), ifNoneMatch: null, cancellationToken)
                    .ConfigureAwait(false);
            }

            // A payload-less success with nothing to retain is unknown, not degraded: there is no
            // last-confirmed evidence being carried forward, and AC5 requires the two to stay distinct
            // so the surface cannot claim retained data it does not have.
            if (result.Payload is null && !result.IsNotModified && !HasSameTenantDetail(previous, request.TenantId)) {
                return TenantDetailSnapshot.Unknown(
                    "Tenant detail projection returned no payload.",
                    result.ETag);
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
            ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
            if (result.Metadata?.IsDegraded == true) {
                return await RetainPreviousTenantDetailAsync(
                    request.TenantId,
                    previous,
                    "Tenant detail projection is degraded.",
                    result.ETag,
                    cancellationToken,
                    lifecycle).ConfigureAwait(false);
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
                return TenantDetailSnapshot.Stale(composition, result.ETag, lifecycle);
            }

            return TenantDetailSnapshot.Ready(composition, result.ETag, freshness, lifecycle);
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
                ProjectionLifecycleState notModifiedLifecycle = ResolveNotModifiedLifecycle(result.Metadata, previous.Lifecycle);
                return previous with {
                    ETag = result.ETag ?? previous.ETag,
                    Kind = ResolveUserTenantsKindForFreshness(previous, notModifiedFreshness),
                    Freshness = notModifiedFreshness,
                    Lifecycle = notModifiedLifecycle,
                    Rows = previous.Rows
                        .Select(row => row with { Freshness = notModifiedFreshness, Lifecycle = notModifiedLifecycle })
                        .ToArray(),
                    Reason = ResolveUserTenantsReasonForFreshness(previous, notModifiedFreshness),
                };
            }

            PaginatedResult<UserTenantMembership> payload = result.Payload
                ?? new PaginatedResult<UserTenantMembership>([], null, false);
            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
            IReadOnlyList<UserTenantMembershipRow> rows = payload.Items
                .Select(m => UserTenantMembershipRow.FromMembership(m) with {
                    Freshness = freshness,
                    Lifecycle = lifecycle,
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
                    request.TargetUserId) with { Lifecycle = lifecycle };
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
                return UserTenantMembershipSnapshot.Stale(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
                    request.TargetUserId) with { Lifecycle = lifecycle };
            }

            if (rows.Count == 0) {
                return UserTenantMembershipSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag, request.TargetUserId) with { Lifecycle = lifecycle };
            }

            return UserTenantMembershipSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                request.TargetUserId) with { Lifecycle = lifecycle };
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
                ProjectionLifecycleState notModifiedLifecycle = ResolveNotModifiedLifecycle(result.Metadata, previous.Lifecycle);
                return previous with {
                    ETag = result.ETag ?? previous.ETag,
                    Kind = ResolveGlobalAdministratorsKindForFreshness(previous, notModifiedFreshness),
                    Freshness = notModifiedFreshness,
                    Lifecycle = notModifiedLifecycle,
                    Rows = previous.Rows
                        .Select(row => row with { Freshness = notModifiedFreshness, Lifecycle = notModifiedLifecycle })
                        .ToArray(),
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
                        Lifecycle = ProjectionLifecycleState.Unknown,
                        Rows = previous.Rows
                            .Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown, Lifecycle = ProjectionLifecycleState.Unknown })
                            .ToArray(),
                    };
            }

            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
            IReadOnlyList<GlobalAdministratorRow> rows = payload.Items
                .Select(m => GlobalAdministratorRow.FromSummary(m) with { Freshness = freshness, Lifecycle = lifecycle })
                .ToArray();

            if (result.Metadata?.IsDegraded == true) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown }).ToArray();
                return GlobalAdministratorsSnapshot.Degraded(
                    rows,
                    GlobalAdministratorsReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore) with { Lifecycle = lifecycle };
            }

            if (freshness is ReadModelFreshnessState.Stale) {
                rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
                return GlobalAdministratorsSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag) with { Lifecycle = lifecycle };
            }

            if (rows.Count == 0) {
                return GlobalAdministratorsSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag) with { Lifecycle = lifecycle };
            }

            return GlobalAdministratorsSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness) with { Lifecycle = lifecycle };
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
            ProjectionLifecycleState notModifiedLifecycle = ResolveNotModifiedLifecycle(result.Metadata, previous.Lifecycle);
            QueryResponseProvenance notModifiedProvenance = ResolveProvenance(result.Metadata);
            return previous with {
                ETag = result.ETag ?? previous.ETag,
                Kind = ResolveTenantAuditKindForFreshness(previous, notModifiedFreshness),
                Freshness = notModifiedFreshness,
                Lifecycle = notModifiedLifecycle,
                Rows = previous.Rows
                    .Select(row => row with {
                        Freshness = notModifiedFreshness,
                        Lifecycle = notModifiedLifecycle,
                        Provenance = notModifiedProvenance,
                    })
                    .ToArray(),
                Reason = ResolveTenantAuditReasonForFreshness(previous, notModifiedFreshness),
            };
        }

        PaginatedResult<TenantAuditEntry>? payload = result.Payload;
        if (payload is null || payload.Items is null) {
            return previous is not null && previous.MatchesScope(request)
                ? previous with {
                    Kind = TenantAuditSurfaceKind.Degraded,
                    Reason = TenantAuditReason.MissingPayload,
                    ETag = result.ETag ?? previous.ETag,
                    Freshness = ReadModelFreshnessState.Unknown,
                    Lifecycle = ProjectionLifecycleState.Unknown,
                    Rows = previous.Rows
                        .Select(static row => row with {
                            Freshness = ReadModelFreshnessState.Unknown,
                            Lifecycle = ProjectionLifecycleState.Unknown,
                            Provenance = QueryResponseProvenance.Unknown,
                        })
                        .ToArray(),
                }
                : TenantAuditSnapshot.Degraded([], TenantAuditReason.MissingPayload, request, result.ETag);
        }

        ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
        ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
        QueryResponseProvenance provenance = ResolveProvenance(result.Metadata);
        IReadOnlyList<TenantAuditRow> rows = payload.Items
            .Select(entry => TenantAuditRow.FromEntry(entry, freshness) with {
                Lifecycle = lifecycle,
                Provenance = provenance,
            })
            .ToArray();

        if (result.Metadata?.IsDegraded == true) {
            rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Unknown }).ToArray();
            return TenantAuditSnapshot.Degraded(
                rows,
                TenantAuditReason.ProjectionDegraded,
                request,
                result.ETag,
                payload.Cursor,
                payload.HasMore) with { Lifecycle = lifecycle };
        }

        if (freshness is ReadModelFreshnessState.Stale) {
            rows = rows.Select(static row => row with { Freshness = ReadModelFreshnessState.Stale }).ToArray();
            return TenantAuditSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag, request) with { Lifecycle = lifecycle };
        }

        if (isListRefreshed) {
            return TenantAuditSnapshot.ListRefreshed(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness,
                request) with { Lifecycle = lifecycle };
        }

        if (rows.Count == 0) {
            return TenantAuditSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag, request) with { Lifecycle = lifecycle };
        }

        return TenantAuditSnapshot.Ready(rows, payload.Cursor, payload.HasMore, result.ETag, freshness, request) with { Lifecycle = lifecycle };
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

        // Every decode failure is an invalidation that forces raw page zero, and a failed decode's out value
        // is never trusted because it is not a protected offset. The codec's untrusted-input failure modes
        // are contained here; fatal conditions and programming defects still surface. No degradation signal
        // is raised at this point: a contained decode failure whose forced page-zero retry then succeeds
        // authoritatively did not degrade anything, and only a load that actually resolved to the ordinary
        // list may emit the signal.
        bool decoded;
        int rawOffset;
        try {
            decoded = searchCursorCodec.TryDecode(canonicalRequest.SearchCursor, scope, out rawOffset);
        }
        catch (Exception ex) when (IsContainedCodecFailure(ex)) {
            decoded = false;
            rawOffset = 0;
        }

        // The invalidation is owned by the caller, not by the authoritative attempt, so an index-shrink
        // recovery discovered inside that attempt survives a later throw and is still reported to the
        // ordinary-list fallback.
        StrongBox<bool> cursorRecovered = new(!decoded);
        if (!decoded) {
            rawOffset = 0;
        }

        // Exactly one fallback call per load: every authoritative-search failure records a reason code and
        // funnels through the single call below, so the support-safe degradation signal can never be emitted
        // twice for one load, and a failure inside the ordinary-list fallback cannot re-enter the fallback.
        (TenantListSnapshot? authoritative, string? fallbackReasonCode) = await TrySearchAuthoritativelyAsync(
            canonicalRequest,
            rawOffset,
            scope,
            cursorRecovered,
            cancellationToken).ConfigureAwait(false);
        if (authoritative is not null) {
            return authoritative;
        }

        return await FallBackFromSearchAsync(
            canonicalRequest,
            previous,
            cursorRecovered.Value,
            fallbackReasonCode!,
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the index lookup, the single index-shrink recovery, and authoritative hydration. Returns the
    /// authoritative snapshot, or a support-safe reason code identifying why the caller must fall back.
    /// Every reason code names the subsystem that actually failed: the index-unavailable arm wraps only the
    /// Memories call, so a Tenants read fault or a codec fault can never be reported as an unavailable index.
    /// </summary>
    private async Task<(TenantListSnapshot? Snapshot, string? FallbackReasonCode)> TrySearchAuthoritativelyAsync(
        TenantListRequest canonicalRequest,
        int rawOffset,
        string scope,
        StrongBox<bool> cursorRecovered,
        CancellationToken cancellationToken) {
        MemoriesSearchResult? result;
        try {
            result = await SearchMemoriesAsync(canonicalRequest, rawOffset, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (IsSearchAvailabilityFailure(ex)) {
            return (null, SearchIndexUnavailableReasonCode);
        }

        if (!IsValidSearchResult(result, canonicalRequest, rawOffset, allowOffsetBeyondTotal: true)) {
            return (null, SearchResponseInvalidReasonCode);
        }

        MemoriesSearchResult validResult = result!;
        if (rawOffset > validResult.TotalCount
            || (rawOffset > 0
                && rawOffset == validResult.TotalCount
                && validResult.Results.Count == 0)) {
            rawOffset = 0;
            cursorRecovered.Value = true;
            try {
                result = await SearchMemoriesAsync(canonicalRequest, rawOffset, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) when (IsSearchAvailabilityFailure(ex)) {
                return (null, SearchIndexUnavailableReasonCode);
            }

            if (!IsValidSearchResult(result, canonicalRequest, rawOffset, allowOffsetBeyondTotal: false)) {
                return (null, SearchResponseInvalidReasonCode);
            }

            validResult = result!;
        }

        try {
            return await BuildAuthoritativeSearchSnapshotAsync(
                canonicalRequest,
                validResult,
                rawOffset,
                scope,
                cursorRecovered.Value,
                cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }

        // Hydration is a Tenants read, never the Memories index, so it carries its own reason code.
        catch (Exception ex) when (!IsSurfacingDefect(ex) && IsHydrationAvailabilityFailure(ex)) {
            return (null, SearchHydrationUnavailableReasonCode);
        }
    }

    private static TenantListRequest CanonicalizeListRequest(TenantListRequest request) {
        int pageSize = request.PageSize is >= 1 and <= MaximumPageSize ? request.PageSize : DefaultPageSize;
        // Mirrors TenantWorkspaceState.NormalizeSearch so a direct gateway caller cannot bypass the trim
        // and length bound that keep the cursor scope stable and the Memories request line finite.
        string? trimmedSearch = string.IsNullOrWhiteSpace(request.Search) || request.Search.Any(char.IsControl)
            ? null
            : request.Search.Trim();
        string? search = string.IsNullOrEmpty(trimmedSearch) || trimmedSearch.Length > MaximumSearchLength
            ? null
            : trimmedSearch;
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
        // Only statuses the index actually records may be pushed down as an attribute filter. The index
        // publisher coerces TenantStatus.Unknown to the event's concrete fallback and never writes
        // status=Unknown, so forwarding it matched nothing and made "status: Unknown" plus any search term
        // report zero tenants while the same filter on the ordinary list listed them. The authoritative
        // recheck against the hydrated detail enforces the filter either way, so dropping the push-down
        // costs a wider candidate window and keeps both surfaces in agreement.
        //
        // Agreement only actually holds because a window emptied by that recheck keeps advancing: Unknown is
        // the rare sentinel, so its matches usually sit well past the first raw window. Collapsing paging on
        // any empty window would have reinstated the same confident zero-result answer through the other
        // door -- see the window-collapse rule in BuildAuthoritativeSearchSnapshotAsync.
        IReadOnlyDictionary<string, string>? filters = request.Status is null or TenantStatus.Unknown
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

    // Raw-page accounting: the index is authoritative for *which* entries exist at an offset, not for *how
    // many* it returns. The consumed Memories server omits entries it considers unusable while still
    // reporting the untrimmed total, so a page carrying fewer hits than the requested window is an ordinary
    // short page. Only an over-full page (more hits than the requested window) or a page whose hits overflow
    // the reported total is a contract violation. Rejecting short pages would turn one unusable index entry
    // into a permanent, silently-misreported loss of whole-set search for that query.
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

        foreach (MemoriesScoredResult? hit in result.Results) {
            if (hit is null || !string.Equals(hit.Axis, SearchAxis, StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private async Task<(TenantListSnapshot? Snapshot, string? FallbackReasonCode)> BuildAuthoritativeSearchSnapshotAsync(
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
        Task<(int Ordinal, TenantListRow? Row, bool OperationalFailure, bool EnrichmentDegraded, bool HiddenOrAbsent)>[] hydrationTasks = candidates
            .Select(candidate => HydrateSearchCandidateAsync(candidate, request.Status, concurrency, cancellationToken))
            .ToArray();
        (int Ordinal, TenantListRow? Row, bool OperationalFailure, bool EnrichmentDegraded, bool HiddenOrAbsent)[] outcomes = await Task
            .WhenAll(hydrationTasks)
            .ConfigureAwait(false);

        bool operationalFailure = outcomes.Any(static outcome => outcome.OperationalFailure);

        // A malformed member collection is an enrichment failure, not an outage: it is carried on its own
        // flag so it can never trigger the ordinary-list fallback, while still raising exactly the same
        // operator-visible signal the ordinary list raises for the identical payload.
        bool enrichmentDegraded = outcomes.Any(static outcome => outcome.EnrichmentDegraded);
        IReadOnlyList<TenantListRow> rows = SortSearchRows(
            outcomes
                .Where(static outcome => outcome.Row is not null)
                .OrderBy(static outcome => outcome.Ordinal)
                .Select(static outcome => outcome.Row!)
                .ToArray(),
            request.SortColumn,
            request.SortDescending);

        if (operationalFailure && rows.Count == 0) {
            return (null, SearchHydrationUnavailableReasonCode);
        }

        // Advance by the requested window bounded to the reported total, never by the returned hit count.
        // This is the only advancement rule under which consecutive pages neither duplicate nor skip a
        // candidate when the index legitimately omits one of its own unusable entries.
        int nextOffset = (int)Math.Min((long)rawOffset + request.PageSize, result.TotalCount);

        // Fail closed only when the window was emptied by hiding. TotalCount is the raw pre-authorization
        // index total, so deriving HasMore from it alone made an all-unauthorized page distinguishable from
        // a genuine no-match: the former offered a live Next control, the latter did not, which disclosed
        // both the existence and a page-granular count of tenants the caller is not allowed to see.
        //
        // Collapsing on any empty window instead was wrong in the other direction. A window is also emptied
        // by the operator's own status recheck, by a dropped unrenderable record, and by malformed or
        // duplicate index hits -- none of which is a secret. Ending paging there made accessible matches
        // past the window unreachable while the surface claimed nothing matched at all. So the emptiness
        // must be attributable to hiding before it ends paging.
        //
        // This closes the fully hidden window only. A partially hidden window still shows surviving rows
        // beside a live Next, which continues to advertise that the window held more than it rendered. That
        // channel is out of scope for this story and is recorded as an open risk in the evidence report.
        bool windowHiddenOnly = rows.Count == 0
            && candidates.Count == result.Results.Count
            && outcomes.Length > 0
            && outcomes.All(static outcome => outcome.HiddenOrAbsent);
        bool hasMore = !windowHiddenOnly && nextOffset < result.TotalCount;
        string? nextCursor = null;
        if (hasMore) {
            try {
                nextCursor = searchCursorCodec.Encode(scope, nextOffset);
            }

            // Non-fatal cursor protection failures degrade to the ordinary list, never to the caller.
            catch (Exception ex) when (IsContainedCodecFailure(ex)) {
                return (null, SearchCursorProtectionUnavailableReasonCode);
            }
        }

        bool degraded = operationalFailure || enrichmentDegraded;

        // The ordinary list reports Unknown freshness whenever it degrades; the search surface mirrors that
        // exactly so the same payload cannot produce two different freshness claims.
        ReadModelFreshnessState freshness = degraded
            ? ReadModelFreshnessState.Unknown
            : AggregateFreshness(rows);
        ProjectionLifecycleState lifecycle = AggregateLifecycle(rows);

        // A search page with no visible rows is an index/authorization outcome, never a verdict on the
        // operator's filters, so it gets its own surface state instead of reusing the filtered-empty copy.
        TenantListSurfaceKind kind = rows.Count == 0
            ? TenantListSurfaceKind.SearchPageEmpty
            : degraded ? TenantListSurfaceKind.Degraded
            : freshness == ReadModelFreshnessState.Stale ? TenantListSurfaceKind.Stale
            : TenantListSurfaceKind.Ready;

        return (
            new TenantListSnapshot(
                kind,
                rows,
                nextCursor,
                hasMore,
                ETag: null,
                freshness,
                IsDegraded: degraded,
                IsAuthorizationScopedEmpty: rows.Count == 0,
                Reason: operationalFailure
                    ? TenantListReason.SearchPartiallyAvailable
                    : enrichmentDegraded ? TenantListReason.RowEnrichmentUnavailable : TenantListReason.None,
                Notice: cursorRecovered ? TenantListReason.SearchRefreshed : TenantListReason.None,
                IsAuthoritativeSearch: true,
                PagingRecovered: cursorRecovered,
                Lifecycle: lifecycle),
            null);
    }

    /// <summary>
    /// Hydrates one index candidate through the authorized Tenants detail seam. <c>HiddenOrAbsent</c>
    /// distinguishes a candidate the caller may not see from one dropped for a reason that is not a secret,
    /// because only the former may end paging: see the window-collapse rule in
    /// <see cref="BuildAuthoritativeSearchSnapshotAsync"/>.
    /// </summary>
    private async Task<(int Ordinal, TenantListRow? Row, bool OperationalFailure, bool EnrichmentDegraded, bool HiddenOrAbsent)> HydrateSearchCandidateAsync(
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
                || !string.Equals(detail.TenantId, candidate.TenantId, StringComparison.Ordinal)) {
                return (candidate.Ordinal, null, true, false, false);
            }

            // A successfully read projection whose Name is null is one malformed record, not a Tenants
            // outage. Classifying it as an operational failure meant a single bad row could take down
            // whole-set search for the query that matched it and replace the result with the entire
            // unfiltered tenant list under a misleading "search unavailable" notice. Drop the unrenderable
            // row and raise the ordinary enrichment-degraded signal instead, which never reaches the
            // fallback path.
            if (detail.Name is null) {
                return (candidate.Ordinal, null, false, true, false);
            }

            // Not hidden: the operator asked for this filter, so a window emptied by it must keep advancing
            // rather than reporting that nothing matched the search at all.
            if (status is not null && detail.Status != status.Value) {
                return (candidate.Ordinal, null, false, false, false);
            }

            // A malformed member collection must degrade exactly like the ordinary list path: the authorized
            // tenant identity and lifecycle stay visible with unknown counts, and the surface raises the same
            // IsDegraded / RowEnrichmentUnavailable signal. Dropping the row here would make a tenant that is
            // visible in the list vanish when the operator searches for it by name, and reusing the
            // operational-failure flag would send the whole surface to the ordinary-list fallback.
            bool usableMembers = HasUsableMembers(detail);
            ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
            ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
            return (
                candidate.Ordinal,
                new TenantListRow(
                    detail.TenantId,
                    detail.Name,
                    detail.Status,
                    usableMembers ? TenantCountValue.Known(detail.Members.Count) : TenantCountValue.Unknown,
                    usableMembers
                        ? TenantCountValue.Known(detail.Members.Count(static member => member.Role == TenantRole.TenantOwner))
                        : TenantCountValue.Unknown,
                    TenantPendingState.Unknown,
                    freshness,
                    lifecycle),
                false,
                !usableMembers,
                false);
        }

        // The only drop that may end paging: the caller is not permitted to see this candidate, or it no
        // longer exists. Both are silent and indistinguishable from absence by contract.
        catch (EventStoreGatewayException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden or (int)HttpStatusCode.NotFound) {
            return (candidate.Ordinal, null, false, false, true);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (Exception ex) when (IsHydrationAvailabilityFailure(ex)) {
            return (candidate.Ordinal, null, true, false, false);
        }
        finally {
            _ = concurrency.Release();
        }
    }

    /// <summary>
    /// Runs the ordinary authorization-safe list after authoritative search degraded. The protected-search
    /// cursor invalidation is threaded through so search history is still cleared when the same load also
    /// loses Memories, and the emitted reason code is support-safe: no cursor, offset, query, or tenant id.
    /// </summary>
    private async Task<TenantListSnapshot> FallBackFromSearchAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        bool searchCursorInvalidated,
        string reasonCode,
        CancellationToken cancellationToken) {
        TenantListRequest fallbackRequest = request with {
            Search = null,
            SearchCursor = null,
            ETag = null,
        };
        TenantListSnapshot? reusable = previous?.IsAuthoritativeSearch == false ? previous : null;
        TenantListSnapshot fallback = await ListByCursorAsync(fallbackRequest, reusable, cancellationToken)
            .ConfigureAwait(false);

        // The signal is emitted only once the fallback has resolved, so it reports the outcome the operator
        // actually received rather than claiming a usable ordinary list that never materialized.
        if (fallback.Kind is TenantListSurfaceKind.Error or TenantListSurfaceKind.Unauthorized) {
            SignalSearchUnavailable(reasonCode);

            // The notice bars render from the notice reasons alone and never consult Kind, so a terminal
            // surface can and does carry the explanation. The clearing and its notice therefore travel
            // together on this snapshot instead of being deferred to some later renderable load.
            // A search-unavailable signal travels here too: the terminal Error/Unauthorized copy only
            // explains that the ordinary list failed, so without it the operator is never told that
            // whole-set search failed independently -- which is the one thing the reason codes exist for.
            // It must not be the ordinary SearchUnavailable notice, whose copy invites the operator to keep
            // browsing the authorized list. On this path that list is exactly what did not load, and on the
            // Unauthorized surface it would sit under "Sign in required" telling them to browse anyway.
            return fallback with {
                Notice = TenantListReason.SearchAndListUnavailable,
                PagingRecovered = searchCursorInvalidated,
                PagingNotice = searchCursorInvalidated
                    ? TenantListReason.SearchRefreshed
                    : fallback.PagingNotice,
            };
        }

        SignalSearchDegradation(reasonCode);
        bool fallbackRecovered = fallback.Notice == TenantListReason.ListRefreshed;
        return fallback with {
            Notice = TenantListReason.SearchUnavailable,
            IsAuthoritativeSearch = false,
            PagingRecovered = searchCursorInvalidated,
            FallbackPagingRecovered = fallbackRecovered,

            // Notice collision: when a cursor invalidation and an ordinary-list cursor recovery land on the
            // same load, only one paging slot is free. The search-invalidation explanation is never the one
            // dropped, because no load may clear protected search history without copy that explains the
            // search restarted.
            PagingNotice = searchCursorInvalidated
                ? TenantListReason.SearchRefreshed
                : fallbackRecovered
                    ? TenantListReason.ListRefreshed
                    : TenantListReason.None,
        };
    }

    private void SignalSearchDegradation(string reasonCode)
        => logger?.LogWarning(
            SearchDegradedToOrdinaryListEvent,
            "Authoritative tenant search degraded to the ordinary tenant list. ReasonCode={SearchDegradationReasonCode}",
            reasonCode);

    private void SignalSearchUnavailable(string reasonCode)
        => logger?.LogWarning(
            SearchAndOrdinaryListUnavailableEvent,
            "Authoritative tenant search degraded and the ordinary tenant list is also unavailable. ReasonCode={SearchDegradationReasonCode}",
            reasonCode);

    private static bool HasUsableMembers(TenantDetail detail)
        => detail.Members is not null && !detail.Members.Any(static member => member is null);

    /// <summary>
    /// The surfacing set: fatal conditions and programming defects. These are enumerated exactly, and they
    /// are excluded <b>before</b> any base-type match below, because <see cref="ObjectDisposedException"/>
    /// derives from <see cref="InvalidOperationException"/> and <see cref="ArgumentNullException"/> derives
    /// from <see cref="ArgumentException"/> -- both of which are contained base types. Without this ordering
    /// a contained arm would silently swallow the very defects the containment rule promises to re-raise,
    /// and a torn-down DataProtection provider would be mislabelled as a tampered cursor.
    /// </summary>
    private static bool IsSurfacingDefect(Exception exception)
        => exception is OutOfMemoryException
            or NullReferenceException
            or ObjectDisposedException
            or ArgumentNullException;

    /// <summary>
    /// The contained set: the codec's untrusted-input failure modes -- cryptographic, format, JSON,
    /// arithmetic, not-supported, and the argument/state failures the codec contract itself raises for a
    /// malformed scope or position. The two sets are disjoint by construction.
    /// </summary>
    private static bool IsContainedCodecFailure(Exception exception)
        => !IsSurfacingDefect(exception)
            && exception is CryptographicException
                or FormatException
                or ArgumentException
                or InvalidOperationException
                or ArithmeticException
                or JsonException
                or NotSupportedException;

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

    private static ProjectionLifecycleState AggregateLifecycle(IReadOnlyList<TenantListRow> rows) {
        ProjectionLifecycleState[] lifecycles = rows
            .Select(static row => row.Lifecycle)
            .Distinct()
            .ToArray();
        return lifecycles.Length == 1 ? lifecycles[0] : ProjectionLifecycleState.Unknown;
    }

    // Surfacing defects are excluded first here too: an ObjectDisposedException raised while encoding a
    // protected cursor must escape the gateway rather than be reported as an unavailable search index.
    private static bool IsSearchAvailabilityFailure(Exception exception)
        => !IsSurfacingDefect(exception)
            && (exception is MemoriesRemoteException
                or HttpRequestException
                or TimeoutException
                or JsonException
                or InvalidOperationException
                or CryptographicException
                or ArithmeticException
                || exception is OperationCanceledException);

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
            ProjectionLifecycleState notModifiedLifecycle = ResolveNotModifiedLifecycle(result.Metadata, previous.Lifecycle);
            TenantListSurfaceKind kind = ResolveTenantListKindForFreshness(previous, notModifiedFreshness);
            return previous with
            {
                Kind = kind,
                Freshness = notModifiedFreshness,
                Lifecycle = notModifiedLifecycle,
                Rows = previous.Rows
                    .Select(row => row with { Freshness = notModifiedFreshness, Lifecycle = notModifiedLifecycle })
                    .ToArray(),
                ETag = result.ETag ?? previous.ETag,
                Reason = ResolveTenantListReasonForNotModified(previous, kind),
            };
        }

        PaginatedResult<TenantSummary> payload = result.Payload ?? new PaginatedResult<TenantSummary>([], null, false);
        ReadModelFreshnessState freshness = ResolveFreshness(result.Metadata);
        ProjectionLifecycleState lifecycle = ResolveLifecycle(result.Metadata);
        (IReadOnlyList<TenantListRow> rows, bool enrichmentDegraded) = await EnrichRowsAsync(
            payload.Items,
            freshness,
            lifecycle,
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
                Lifecycle = lifecycle,
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
                isDegraded: false) with { Lifecycle = lifecycle };
        }

        if (rows.Count == 0)
        {
            return TenantListSnapshot.Empty(isAuthorizationScoped: true, freshness) with
            {
                ETag = result.ETag,
                Lifecycle = lifecycle,
            };
        }

        return TenantListSnapshot.Ready(
            rows,
            payload.Cursor,
            payload.HasMore,
            result.ETag,
            freshness,
            isDegraded) with { Lifecycle = lifecycle };
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
        ProjectionLifecycleState lifecycle,
        CancellationToken cancellationToken) {
        List<TenantListRow> rows = new(summaries.Count);
        bool degraded = false;

        foreach (TenantSummary summary in summaries) {
            TenantListRow row = TenantListRow.FromSummary(summary) with {
                Freshness = freshness,
                Lifecycle = lifecycle,
            };

            try {
                TenantDetail? detail = await LoadTenantDetailAsync(summary.TenantId, cancellationToken)
                    .ConfigureAwait(false);
                if (detail is not null) {
                    // The ordinary list path dereferences the same detail payload as authoritative search
                    // hydration, so it applies the identical null-shape rejection instead of throwing.
                    if (!HasUsableMembers(detail)) {
                        degraded = true;
                    }
                    else {
                        int memberCount = detail.Members.Count;
                        int ownerCount = detail.Members.Count(static m => m.Role == TenantRole.TenantOwner);
                        row = row with {
                            MemberCount = TenantCountValue.Known(memberCount),
                            OwnerCount = TenantCountValue.Known(ownerCount),
                        };
                    }
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
            // Proof comparison reads the raw dictionary, so it must apply the configuration policy itself.
            // Without this gate the method answers "does key K exist" and "is K equal to V" for any key a
            // caller supplies, which is an existence and value oracle for namespaces outside their grants —
            // the component-level checks are not a substitute, because they are not on this path. Policy
            // resolution is part of the proof boundary and therefore shares its fail-closed containment.
            if (bffComposition is null
                || !await bffComposition
                    .IsConfigurationKeyAuthorizedAsync(tenantId, key, cancellationToken)
                    .ConfigureAwait(false)) {
                return TenantConfigurationProjectionProof.Unavailable(tenantId);
            }

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
        CancellationToken cancellationToken,
        ProjectionLifecycleState lifecycle = ProjectionLifecycleState.Unknown) {
        if (!HasSameTenantDetail(previous, tenantId)) {
            return TenantDetailSnapshot.Degraded(null, message, eTag, lifecycle);
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

        return TenantDetailSnapshot.DegradedFromComposition(composition, message, eTag ?? previous.ETag, lifecycle);
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

    private static ProjectionLifecycleState ResolveLifecycle(QueryResponseMetadata? metadata)
        => metadata is null
            ? ProjectionLifecycleState.Unknown
            : ProjectionLifecyclePolicy.Normalize(metadata.Lifecycle, metadata.Provenance);

    // Transported so a consumer mutation gate can apply the EventStore policy against the declared
    // route provenance instead of re-deriving it from freshness. An absent or out-of-range value fails
    // closed to Unknown, which every provenance-sensitive consumer treats as "not projection-backed".
    private static QueryResponseProvenance ResolveProvenance(QueryResponseMetadata? metadata)
        => metadata is not null && Enum.IsDefined(metadata.Provenance)
            ? metadata.Provenance
            : QueryResponseProvenance.Unknown;

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

    private static ProjectionLifecycleState ResolveNotModifiedLifecycle(
        QueryResponseMetadata? metadata,
        ProjectionLifecycleState previous) {
        if (metadata is null
            || metadata.Provenance is not QueryResponseProvenance.ProjectionBacked) {
            return ProjectionLifecycleState.Unknown;
        }

        return metadata.Lifecycle is ProjectionLifecycleState.Unknown
            ? previous
            : ResolveLifecycle(metadata);
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
