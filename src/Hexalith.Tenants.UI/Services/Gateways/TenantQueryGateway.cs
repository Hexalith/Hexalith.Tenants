using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(
    IEventStoreGatewayClient gatewayClient,
    IUserContextAccessor userContextAccessor) : ITenantQueryGateway
{
    private const string SystemTenant = "system";
    private const string GlobalAdministratorsAggregateId = "global-administrators";
    private const string TenantIndexAggregateId = "index";

    public async Task<TenantDetailSnapshot> GetTenantAsync(
        TenantDetailRequest request,
        TenantDetailSnapshot? previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            EventStoreQueryResult<TenantDetail> result = await gatewayClient
                .SubmitQueryAsync<TenantDetail>(CreateDetailQuery(request.TenantId), request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified)
            {
                return previous?.Detail is null
                    ? TenantDetailSnapshot.Degraded(null, "Tenant detail was unchanged, but no cached server snapshot is available.", result.ETag)
                    : previous with
                    {
                        // A 304 means the cached snapshot is unchanged, not that a degraded/stale
                        // snapshot recovered. Preserve the prior truth state; only a previously
                        // Ready snapshot earns refreshed Current freshness from the conditional hit.
                        Freshness = previous.Kind is TenantDetailSurfaceKind.Ready ? TenantFreshnessState.Current : previous.Freshness,
                        ETag = result.ETag ?? previous.ETag,
                    };
            }

            if (result.Payload is null)
            {
                return TenantDetailSnapshot.Unknown("Tenant detail projection returned no payload.", result.ETag);
            }

            TenantFreshnessState freshness = ResolveFreshness(result.Metadata, result.ETag);
            if (result.Metadata?.IsStale == true)
            {
                return TenantDetailSnapshot.Stale(result.Payload, result.ETag);
            }

            if (result.Metadata?.IsDegraded == true)
            {
                return TenantDetailSnapshot.Degraded(result.Payload, "Tenant detail projection is degraded.", result.ETag);
            }

            return TenantDetailSnapshot.Ready(result.Payload, result.ETag, freshness);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapDetailException(request.TenantId, ex);
        }
    }

    public async Task<UserTenantMembershipSnapshot> GetMyTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            return UserTenantMembershipSnapshot.Unauthorized(UserTenantMembershipReason.MissingAuthenticatedUser);
        }

        UserTenantMembershipRequest selfRequest = request with
        {
            TargetUserId = authenticatedUserId,
        };

        return await GetUserTenantsCoreAsync(authenticatedUserId, selfRequest, previous, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<UserTenantMembershipSnapshot> GetUserTenantsAsync(
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            return UserTenantMembershipSnapshot.Unauthorized(
                UserTenantMembershipReason.MissingAuthenticatedUser,
                request.TargetUserId);
        }

        if (string.IsNullOrWhiteSpace(request.TargetUserId))
        {
            return UserTenantMembershipSnapshot.Invalid(UserTenantMembershipReason.MissingTargetUser);
        }

        return await GetUserTenantsCoreAsync(authenticatedUserId, request, previous, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<UserTenantMembershipSnapshot> GetUserTenantsCoreAsync(
        string authenticatedUserId,
        UserTenantMembershipRequest request,
        UserTenantMembershipSnapshot? previous,
        CancellationToken cancellationToken)
    {
        try
        {
            SubmitQueryRequest query = CreateUserTenantsQuery(authenticatedUserId, request);
            EventStoreQueryResult<PaginatedResult<UserTenantMembership>> result = await gatewayClient
                .SubmitQueryAsync<PaginatedResult<UserTenantMembership>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified)
            {
                return previous is null || !string.Equals(previous.TargetUserId, request.TargetUserId, StringComparison.Ordinal)
                    ? UserTenantMembershipSnapshot.Degraded(
                        [],
                        UserTenantMembershipReason.NotModifiedWithoutSnapshot,
                        result.ETag,
                        targetUserId: request.TargetUserId)
                    : previous with
                    {
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
                .Select(m => UserTenantMembershipRow.FromMembership(m) with
                {
                    Freshness = freshness,
                })
                .ToArray();

            if (result.Metadata?.IsStale == true)
            {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Stale }).ToArray();
                return UserTenantMembershipSnapshot.Stale(
                    rows,
                    payload.Cursor,
                    payload.HasMore,
                    result.ETag,
                    request.TargetUserId);
            }

            if (result.Metadata?.IsDegraded == true)
            {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Unknown }).ToArray();
                return UserTenantMembershipSnapshot.Degraded(
                    rows,
                    UserTenantMembershipReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore,
                    request.TargetUserId);
            }

            if (rows.Count == 0)
            {
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
        catch (EventStoreGatewayException ex)
        {
            return MapUserTenantException(ex, request.TargetUserId);
        }
    }

    public async Task<GlobalAdministratorsSnapshot> GetGlobalAdministratorsAsync(
        GlobalAdministratorsRequest request,
        GlobalAdministratorsSnapshot? previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? authenticatedUserId = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(authenticatedUserId))
        {
            return GlobalAdministratorsSnapshot.Unauthorized(GlobalAdministratorsReason.MissingAuthenticatedUser);
        }

        try
        {
            SubmitQueryRequest query = CreateGlobalAdministratorsQuery(request);
            EventStoreQueryResult<PaginatedResult<GlobalAdministratorSummary>> result = await gatewayClient
                .SubmitQueryAsync<PaginatedResult<GlobalAdministratorSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified)
            {
                return previous is null
                    ? GlobalAdministratorsSnapshot.Degraded(
                        [],
                        GlobalAdministratorsReason.NotModifiedWithoutSnapshot,
                        result.ETag)
                    : previous with
                    {
                        ETag = result.ETag ?? previous.ETag,
                        Freshness = previous.Kind is GlobalAdministratorsSurfaceKind.Ready
                            or GlobalAdministratorsSurfaceKind.Empty
                                ? TenantFreshnessState.Current
                                : previous.Freshness,
                    };
            }

            PaginatedResult<GlobalAdministratorSummary>? payload = result.Payload;
            if (payload is null)
            {
                return previous is null
                    ? GlobalAdministratorsSnapshot.Degraded(
                        [],
                        GlobalAdministratorsReason.MissingPayload,
                        result.ETag)
                    : previous with
                    {
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

            if (result.Metadata?.IsStale == true)
            {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Stale }).ToArray();
                return GlobalAdministratorsSnapshot.Stale(rows, payload.Cursor, payload.HasMore, result.ETag);
            }

            if (result.Metadata?.IsDegraded == true)
            {
                rows = rows.Select(static row => row with { Freshness = TenantFreshnessState.Unknown }).ToArray();
                return GlobalAdministratorsSnapshot.Degraded(
                    rows,
                    GlobalAdministratorsReason.ProjectionDegraded,
                    result.ETag,
                    payload.Cursor,
                    payload.HasMore);
            }

            if (rows.Count == 0)
            {
                return GlobalAdministratorsSnapshot.Empty(isAuthorizationScoped: true, freshness, result.ETag);
            }

            return GlobalAdministratorsSnapshot.Ready(
                rows,
                payload.Cursor,
                payload.HasMore,
                result.ETag,
                freshness);
        }
        catch (EventStoreGatewayException ex)
        {
            return MapGlobalAdministratorsException(ex);
        }
    }

    public async Task<TenantListSnapshot> ListTenantsAsync(
        TenantListRequest request,
        TenantListSnapshot? previous,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            SubmitQueryRequest query = CreateListQuery(request);
            EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await gatewayClient
                .SubmitQueryAsync<PaginatedResult<TenantSummary>>(query, request.ETag, cancellationToken)
                .ConfigureAwait(false);

            if (result.IsNotModified)
            {
                return previous is null
                    ? TenantListSnapshot.Degraded([], "Tenant list was unchanged, but no cached server snapshot is available.")
                    : previous with
                    {
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
        catch (EventStoreGatewayException ex) when (IsUnavailableOrInvalid(ex))
        {
            return TenantListSnapshot.Error("Tenant query gateway is unavailable.");
        }
    }

    private static SubmitQueryRequest CreateListQuery(TenantListRequest request)
        => new(
            SystemTenant,
            ListTenantsQuery.Domain,
            TenantIndexAggregateId,
            ListTenantsQuery.QueryType,
            ProjectionType: ListTenantsQuery.ProjectionType,
            Payload: JsonSerializer.SerializeToElement(new
            {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

    private static SubmitQueryRequest CreateUserTenantsQuery(string authenticatedUserId, UserTenantMembershipRequest request)
        => new(
            authenticatedUserId,
            GetUserTenantsQuery.Domain,
            TenantIndexAggregateId,
            GetUserTenantsQuery.QueryType,
            ProjectionType: GetUserTenantsQuery.ProjectionType,
            Payload: JsonSerializer.SerializeToElement(new
            {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: request.TargetUserId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

    private static SubmitQueryRequest CreateGlobalAdministratorsQuery(GlobalAdministratorsRequest request)
        => new(
            SystemTenant,
            GetGlobalAdministratorsQuery.Domain,
            GlobalAdministratorsAggregateId,
            GetGlobalAdministratorsQuery.QueryType,
            ProjectionType: GetGlobalAdministratorsQuery.ProjectionType,
            Payload: JsonSerializer.SerializeToElement(new
            {
                cursor = request.Cursor,
                pageSize = request.PageSize,
            }),
            EntityId: GlobalAdministratorsAggregateId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

    private async Task<(IReadOnlyList<TenantListRow> Rows, bool IsDegraded)> EnrichRowsAsync(
        IReadOnlyList<TenantSummary> summaries,
        TenantFreshnessState freshness,
        CancellationToken cancellationToken)
    {
        List<TenantListRow> rows = new(summaries.Count);
        bool degraded = false;

        foreach (TenantSummary summary in summaries)
        {
            TenantListRow row = TenantListRow.FromSummary(summary) with
            {
                Freshness = freshness,
            };

            try
            {
                TenantDetail? detail = await LoadTenantDetailAsync(summary.TenantId, cancellationToken)
                    .ConfigureAwait(false);
                if (detail is not null)
                {
                    int memberCount = detail.Members.Count;
                    int ownerCount = detail.Members.Count(static m => m.Role == TenantRole.TenantOwner);
                    row = row with
                    {
                        MemberCount = TenantCountValue.Known(memberCount),
                        OwnerCount = TenantCountValue.Known(ownerCount),
                    };
                }
            }
            catch (EventStoreGatewayException ex) when (ex.StatusCode is (int)HttpStatusCode.Forbidden or (int)HttpStatusCode.NotFound or (int)HttpStatusCode.ServiceUnavailable)
            {
                degraded = true;
            }

            rows.Add(row);
        }

        return (rows, degraded);
    }

    private async Task<TenantDetail?> LoadTenantDetailAsync(string tenantId, CancellationToken cancellationToken)
    {
        EventStoreQueryResult<TenantDetail> result = await gatewayClient
            .SubmitQueryAsync<TenantDetail>(CreateDetailQuery(tenantId), ifNoneMatch: null, cancellationToken)
            .ConfigureAwait(false);

        return result.Payload;
    }

    private static SubmitQueryRequest CreateDetailQuery(string tenantId)
        => new(
            SystemTenant,
            GetTenantQuery.Domain,
            tenantId,
            GetTenantQuery.QueryType,
            ProjectionType: GetTenantQuery.ProjectionType,
            Payload: JsonSerializer.SerializeToElement(new { }),
            EntityId: tenantId,
            ProjectionActorType: TenantProjectionRouting.ActorTypeName);

    private static TenantFreshnessState ResolveFreshness(QueryResponseMetadata? metadata, string? eTag)
    {
        if (metadata?.IsDegraded == true)
        {
            return TenantFreshnessState.Unknown;
        }

        if (metadata?.IsStale == true)
        {
            return TenantFreshnessState.Stale;
        }

        return metadata?.IsNotModified == true
            || !string.IsNullOrWhiteSpace(eTag)
            || !string.IsNullOrWhiteSpace(metadata?.ProjectionVersion)
            || metadata?.ServedAt is not null
                ? TenantFreshnessState.Current
                : TenantFreshnessState.Unknown;
    }

    private static bool IsUnavailableOrInvalid(EventStoreGatewayException exception)
        => exception.StatusCode is >= 400;

    private static TenantDetailSnapshot MapDetailException(string tenantId, EventStoreGatewayException exception)
        => exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden => TenantDetailSnapshot.Unauthorized(tenantId),
            (int)HttpStatusCode.NotFound => TenantDetailSnapshot.NotFound(tenantId),
            (int)HttpStatusCode.BadRequest or (int)HttpStatusCode.ServiceUnavailable => TenantDetailSnapshot.Unavailable("Tenant detail query gateway is unavailable."),
            _ => TenantDetailSnapshot.Degraded(null, "Tenant detail query gateway returned a safe degraded state."),
        };

    private static UserTenantMembershipSnapshot MapUserTenantException(EventStoreGatewayException exception, string? targetUserId)
        => exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => UserTenantMembershipSnapshot.Unauthorized(targetUserId: targetUserId),
            (int)HttpStatusCode.BadRequest
                => UserTenantMembershipSnapshot.Invalid(targetUserId: targetUserId),
            (int)HttpStatusCode.ServiceUnavailable
                => UserTenantMembershipSnapshot.Unavailable(targetUserId: targetUserId),
            _ => UserTenantMembershipSnapshot.Degraded([], UserTenantMembershipReason.GatewayFailure, targetUserId: targetUserId),
        };

    private static GlobalAdministratorsSnapshot MapGlobalAdministratorsException(EventStoreGatewayException exception)
        => exception.StatusCode switch
        {
            (int)HttpStatusCode.Unauthorized or (int)HttpStatusCode.Forbidden
                => GlobalAdministratorsSnapshot.Unauthorized(),
            (int)HttpStatusCode.BadRequest
                => GlobalAdministratorsSnapshot.Invalid(),
            (int)HttpStatusCode.NotFound or (int)HttpStatusCode.NotImplemented or (int)HttpStatusCode.ServiceUnavailable
                => GlobalAdministratorsSnapshot.Unavailable(),
            _ => GlobalAdministratorsSnapshot.Degraded([], GlobalAdministratorsReason.GatewayFailure),
        };
}
