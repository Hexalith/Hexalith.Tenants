using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantQueryGatewayTests
{
    [Fact]
    public async Task Get_tenant_submits_literal_detail_query_and_maps_counts_source()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("tenant.alpha"), metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = new(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Domain.ShouldBe(GetTenantQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetTenantQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.Request.ProjectionActorType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        query.IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Current);
    }

    [Fact]
    public async Task Get_tenant_uses_previous_snapshot_for_not_modified_response()
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: TenantFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");

        TenantQueryGateway gateway = new(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.ETag.ShouldBe("\"known\"");
    }

    [Fact]
    public async Task Get_tenant_without_previous_snapshot_reports_degraded_not_modified_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");

        TenantQueryGateway gateway = new(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Degraded);
        snapshot.Detail.ShouldBeNull();
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(401, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(403, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(404, TenantDetailSurfaceKind.NotFound)]
    [InlineData(503, TenantDetailSurfaceKind.Unavailable)]
    public async Task Get_tenant_maps_gateway_status_to_safe_detail_state(int statusCode, TenantDetailSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));

        TenantQueryGateway gateway = new(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        string errorMessage = snapshot.ErrorMessage.ShouldNotBeNull();
        errorMessage.ShouldNotContain("raw payload", Case.Insensitive);
        errorMessage.ShouldNotContain("token", Case.Insensitive);
        errorMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, TenantDetailSurfaceKind.Stale, TenantFreshnessState.Stale)]
    [InlineData(false, true, TenantDetailSurfaceKind.Degraded, TenantFreshnessState.Unknown)]
    public async Task Get_tenant_maps_stale_and_degraded_metadata_to_safe_states(
        bool isStale,
        bool isDegraded,
        TenantDetailSurfaceKind expectedKind,
        TenantFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));

        TenantQueryGateway gateway = new(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
    }

    [Fact]
    public async Task List_tenants_passes_cursor_without_offset_conversion()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            "next-cursor",
            true));
        client.EnqueueQueryResult(new TenantDetail(
            "tenant.alpha",
            "Alpha",
            null,
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = new(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(Cursor: "opaque-cursor", PageSize: 10), null, CancellationToken.None);

        SubmittedQuery listQuery = client.SubmittedQueries[0];
        listQuery.Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        listQuery.Request.ProjectionActorType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        listQuery.Request.Paging.ShouldBeNull();
        JsonElement payload = listQuery.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.NextCursor.ShouldBe("next-cursor");
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.ShouldBe(TenantCountValue.Known(2));
        snapshot.Rows[0].OwnerCount.ShouldBe(TenantCountValue.Known(1));
    }

    [Fact]
    public async Task List_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));

        TenantQueryGateway gateway = new(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task List_tenants_reports_unknown_freshness_when_no_evidence_exists()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            eTag: null,
            metadata: null);

        TenantQueryGateway gateway = new(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
    }

    [Fact]
    public async Task List_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: TenantFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"");

        TenantQueryGateway gateway = new(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Current);
    }

    [Fact]
    public async Task Detail_enrichment_failure_keeps_unknown_counts_and_degraded_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            null,
            false));
        client.EnqueueException(new EventStoreGatewayException(403, "Forbidden"));

        TenantQueryGateway gateway = new(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();
        snapshot.Rows[0].OwnerCount.IsKnown.ShouldBeFalse();
    }

    private sealed class CapturingGatewayClient : IEventStoreGatewayClient
    {
        private readonly Queue<object> _responses = new();

        public List<SubmittedQuery> SubmittedQueries { get; } = [];

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQueries.Add(new SubmittedQuery(request, ifNoneMatch));
            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((EventStoreQueryResult<T>)next);
        }

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnqueueQueryResult<T>(
            T payload,
            string? eTag = "\"etag\"",
            QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult<T>(
                "correlation",
                payload,
                IsNotModified: false,
                eTag)
            {
                Metadata = metadata,
            });

        public void EnqueueNotModified(string? eTag)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<TenantSummary>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true),
            });

        public void EnqueueDetailNotModified(string? eTag)
            => _responses.Enqueue(new EventStoreQueryResult<TenantDetail>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true),
            });

        public void EnqueueException(Exception exception)
            => _responses.Enqueue(exception);
    }

    private sealed record SubmittedQuery(SubmitQueryRequest Request, string? IfNoneMatch);

    private static TenantDetail Detail(string tenantId)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>
            {
                ["billing.mode"] = "trial",
            },
            DateTimeOffset.UtcNow);
}
