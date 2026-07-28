using System.Net;
using System.Text;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantsRestQueryClientTests
{
    [Fact]
    public async Task Six_typed_reads_use_only_the_canonical_direct_routes_and_query_fields()
    {
        var handler = new RecordingHandler(
            Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"tenantId\":\"tenant/alpha\",\"name\":\"Alpha\",\"description\":null,\"status\":\"Active\",\"members\":[],\"configuration\":{},\"createdAt\":\"2026-07-28T08:00:00Z\"}"),
            Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.ListTenantsAsync(
            new ListTenantsQuery { Cursor = "list cursor/+", PageSize = 25 },
            "list-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant/alpha" },
            null,
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant/alpha", Cursor = "users cursor/+", PageSize = 20 },
            "users-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user/alpha", Cursor = "membership cursor/+", PageSize = 12 },
            null,
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery
            {
                TenantId = "tenant/alpha",
                From = DateTimeOffset.Parse("2026-07-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                To = DateTimeOffset.Parse("2026-07-28T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture),
                Category = AuditEventCategory.Access,
                Cursor = "audit cursor/+",
                PageSize = 30,
            },
            null,
            TestContext.Current.CancellationToken);
        _ = await client.GetGlobalAdministratorsAsync(
            new GetGlobalAdministratorsQuery { Cursor = "admin cursor/+", PageSize = 10 },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests.Select(static request => request.PathAndQuery).ShouldBe(
        [
            "/api/tenants?cursor=list%20cursor%2F%2B&pageSize=25",
            "/api/tenants/tenant%2Falpha",
            "/api/tenants/tenant%2Falpha/users?cursor=users%20cursor%2F%2B&pageSize=20",
            "/api/users/user%2Falpha/tenants?cursor=membership%20cursor%2F%2B&pageSize=12",
            "/api/tenants/tenant%2Falpha/audit?from=2026-07-01T00%3A00%3A00.0000000%2B00%3A00&to=2026-07-28T00%3A00%3A00.0000000%2B00%3A00&category=Access&cursor=audit%20cursor%2F%2B&pageSize=30",
            "/api/global-administrators?cursor=admin%20cursor%2F%2B&pageSize=10",
        ]);
        handler.Requests[0].IfNoneMatch.ShouldBe("\"list-etag\"");
        handler.Requests[2].IfNoneMatch.ShouldBe("\"users-etag\"");
        handler.Requests.ShouldAllBe(static request => request.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task Projection_backed_metadata_is_preserved_without_using_served_at_as_freshness()
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "index-v7");
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Stale");
        response.Headers.Add("X-Hexalith-Is-Stale", "true");
        response.Headers.Add("X-Hexalith-Served-At", "2099-01-01T00:00:00.0000000+00:00");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.ETag.ShouldBe("list-etag");
        result.Metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        result.Metadata.ProjectionVersion.ShouldBe("index-v7");
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Stale);
        result.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Theory]
    [InlineData(null, null, null)]
    [InlineData("HandlerComputed", "Current", "false")]
    [InlineData("ProjectionBacked", "Current", "true")]
    [InlineData("ProjectionBacked", "not-a-lifecycle", "false")]
    public async Task Missing_non_projection_or_contradictory_metadata_fails_closed_to_unknown(
        string? provenance,
        string? lifecycle,
        string? isStale)
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        AddHeader(response, "X-Hexalith-Query-Provenance", provenance);
        AddHeader(response, "X-Hexalith-Projection-Lifecycle", lifecycle);
        AddHeader(response, "X-Hexalith-Is-Stale", isStale);
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    [Theory]
    [InlineData(false, true, true, true)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, false)]
    public async Task Not_modified_requires_strong_etag_projection_provenance_version_and_freshness(
        bool strongETag,
        bool projectionBacked,
        bool projectionVersion,
        bool freshness)
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = strongETag
            ? new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"")
            : new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"", isWeak: true);
        response.Headers.Add("X-Hexalith-Query-Provenance", projectionBacked ? "ProjectionBacked" : "HandlerComputed");
        if (projectionVersion)
        {
            response.Headers.Add("X-Hexalith-Projection-Version", "index-v7");
        }

        if (freshness)
        {
            response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
            response.Headers.Add("X-Hexalith-Is-Stale", "false");
        }

        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        if (strongETag && projectionBacked && projectionVersion && freshness)
        {
            result.IsNotModified.ShouldBeTrue();
            result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        }
        else
        {
            result.IsSuccess.ShouldBeFalse();
            result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidMetadata);
            result.ETag.ShouldBeNull();
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, TenantsRestQueryFailureKind.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden, TenantsRestQueryFailureKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, TenantsRestQueryFailureKind.NotFound)]
    [InlineData(HttpStatusCode.BadRequest, TenantsRestQueryFailureKind.InvalidRequest)]
    [InlineData(HttpStatusCode.InternalServerError, TenantsRestQueryFailureKind.Unavailable)]
    public async Task Http_failures_map_to_fixed_support_safe_categories(
        HttpStatusCode statusCode,
        TenantsRestQueryFailureKind expected)
    {
        var handler = new RecordingHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent("{\"detail\":\"unsafe-secret\"}", Encoding.UTF8, "application/problem+json"),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(expected);
        result.ToString().ShouldBe(
            $"TenantsRestQueryResponse {{ IsSuccess = False, IsNotModified = False, Freshness = Unknown, FailureKind = {expected}, StatusCode = {(int)statusCode} }}");
    }

    [Fact]
    public async Task Invalid_json_maps_to_a_fixed_invalid_payload_failure()
    {
        var handler = new RecordingHandler(Success("{not-json"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
        result.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(false, TenantsRestQueryFailureKind.Unavailable)]
    [InlineData(true, TenantsRestQueryFailureKind.Timeout)]
    public async Task Transport_failures_map_to_fixed_support_safe_categories(
        bool timeout,
        TenantsRestQueryFailureKind expected)
    {
        Exception exception = timeout
            ? new TaskCanceledException("unsafe-timeout-detail")
            : new HttpRequestException("unsafe-network-detail");
        using var httpClient = new HttpClient(new ThrowingHandler(exception))
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(expected);
        result.ToString().ShouldBe(
            $"TenantsRestQueryResponse {{ IsSuccess = False, IsNotModified = False, Freshness = Unknown, FailureKind = {expected}, StatusCode = 503 }}");
    }

    [Fact]
    public async Task Caller_cancellation_propagates()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            cancellation.Token));
    }

    private static HttpResponseMessage Success(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static void AddHeader(HttpResponseMessage response, string name, string? value)
    {
        if (value is not null)
        {
            response.Headers.Add(name, value);
        }
    }

    private sealed class RecordingHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requests.Add(new(
                request.Method,
                request.RequestUri?.PathAndQuery ?? string.Empty,
                request.Headers.IfNoneMatch.SingleOrDefault()?.ToString()));
            if (_responses.Count == 0)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                });
            }

            return Task.FromResult(_responses.Dequeue());
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string PathAndQuery, string? IfNoneMatch);

    private sealed class ThrowingHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }
}
