using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.Services.Gateways;

internal sealed class TenantQueryGateway(IEventStoreGatewayClient gatewayClient) : ITenantQueryGateway
{
    private const string SystemTenant = "system";
    private const string TenantIndexAggregateId = "index";

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
            }));

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
        SubmitQueryRequest query = new(
            SystemTenant,
            GetTenantQuery.Domain,
            tenantId,
            GetTenantQuery.QueryType,
            ProjectionType: GetTenantQuery.ProjectionType,
            Payload: JsonSerializer.SerializeToElement(new { }),
            EntityId: tenantId);

        EventStoreQueryResult<TenantDetail> result = await gatewayClient
            .SubmitQueryAsync<TenantDetail>(query, ifNoneMatch: null, cancellationToken)
            .ConfigureAwait(false);

        return result.Payload;
    }

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
}
