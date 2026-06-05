using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(IEventStoreGatewayClient gatewayClient) : ITenantQueryGateway
{
    private const string SystemTenant = "system";
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
}
