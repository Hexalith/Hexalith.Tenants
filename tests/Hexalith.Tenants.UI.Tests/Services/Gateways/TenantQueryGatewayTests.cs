using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.UserTenants;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantQueryGatewayTests
{
    [Fact]
    public async Task Get_tenant_submits_literal_detail_query_and_maps_counts_source()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("tenant.alpha"), metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

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

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();
        snapshot.Rows[0].OwnerCount.IsKnown.ShouldBeFalse();
    }

    [Fact]
    public async Task Get_my_tenants_submits_self_user_query_with_cursor_payload()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>(
            [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner)],
            "opaque-next",
            true));
        TenantQueryGateway gateway = CreateGateway(client, "user.self");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(Cursor: "signed-cursor", PageSize: 12), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("user.self");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("user.self");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        query.Request.ProjectionActorType.ShouldBe(TenantProjectionRouting.ActorTypeName);
        query.Request.Paging.ShouldBeNull();
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("signed-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(12);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.NextCursor.ShouldBe("opaque-next");
        snapshot.Rows.ShouldHaveSingleItem().Role.ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task Get_my_tenants_requires_authenticated_user_context()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId: null);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Unauthorized);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.MissingAuthenticatedUser);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_my_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.None);
    }

    [Fact]
    public async Task Get_my_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, TenantFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: TenantFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.ETag.ShouldBe("\"known\"");
    }

    [Fact]
    public async Task Get_my_tenants_without_previous_snapshot_reports_degraded_not_modified_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.NotModifiedWithoutSnapshot);
        snapshot.Freshness.ShouldBe(TenantFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(true, false, UserTenantMembershipSurfaceKind.Stale, TenantFreshnessState.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(false, true, UserTenantMembershipSurfaceKind.Degraded, TenantFreshnessState.Unknown, UserTenantMembershipReason.ProjectionDegraded)]
    public async Task Get_my_tenants_maps_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        UserTenantMembershipSurfaceKind expectedKind,
        TenantFreshnessState expectedFreshness,
        UserTenantMembershipReason expectedReason)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(401, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(403, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(400, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(503, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(500, UserTenantMembershipSurfaceKind.Degraded)]
    public async Task Get_my_tenants_maps_gateway_failures_to_sanitized_states(int statusCode, UserTenantMembershipSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    private static TenantQueryGateway CreateGateway(CapturingGatewayClient client, string? userId = "operator-user")
    {
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns(userId);
        userContext.TenantId.Returns("tenant.context");

        return new TenantQueryGateway(client, userContext);
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

        public void EnqueueUserTenantsNotModified(string? eTag)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<UserTenantMembership>>(
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
