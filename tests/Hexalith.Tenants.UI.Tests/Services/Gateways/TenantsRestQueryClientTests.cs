using System.Net;
using System.Text;
using System.Text.Json;

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
            Success("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"description\":null,\"status\":\"Active\",\"members\":[],\"configuration\":{},\"createdAt\":\"2026-07-28T08:00:00Z\"}"),
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
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", Cursor = "users cursor/+", PageSize = 20 },
            "users-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user.alpha", Cursor = "membership cursor/+", PageSize = 12 },
            null,
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery
            {
                TenantId = "tenant.alpha",
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
            "/api/tenants/tenant.alpha",
            "/api/tenants/tenant.alpha/users?cursor=users%20cursor%2F%2B&pageSize=20",
            "/api/users/user.alpha/tenants?cursor=membership%20cursor%2F%2B&pageSize=12",
            "/api/tenants/tenant.alpha/audit?from=2026-07-01T00%3A00%3A00.0000000%2B00%3A00&to=2026-07-28T00%3A00%3A00.0000000%2B00%3A00&category=Access&cursor=audit%20cursor%2F%2B&pageSize=30",
            "/api/global-administrators?cursor=admin%20cursor%2F%2B&pageSize=10",
        ]);
        handler.Requests[0].IfNoneMatch.ShouldBe("\"list-etag\"");
        handler.Requests[2].IfNoneMatch.ShouldBe("\"users-etag\"");
        handler.Requests.ShouldAllBe(static request => request.Method == HttpMethod.Get);
    }

    [Fact]
    public async Task Six_typed_reads_deserialize_their_success_payloads()
    {
        var handler = new RecordingHandler(
            Success("{\"items\":[{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"status\":\"Active\"}],\"cursor\":\"list-next\",\"hasMore\":true}"),
            Success("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"description\":null,\"status\":\"Active\",\"members\":[{\"userId\":\"user.alpha\",\"role\":\"TenantOwner\"}],\"configuration\":{},\"createdAt\":\"2026-07-28T08:00:00Z\"}"),
            Success("{\"items\":[{\"userId\":\"user.alpha\",\"role\":\"TenantOwner\"}],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"status\":\"Active\",\"role\":\"TenantOwner\"}],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[{\"eventId\":\"event-1\",\"eventType\":\"TenantUpdated\",\"category\":\"Administrative\",\"actorId\":\"user.alpha\",\"timestamp\":\"2026-07-28T08:00:00Z\",\"tenantId\":\"tenant.alpha\",\"narrativePayload\":{}}],\"cursor\":null,\"hasMore\":false}"),
            Success("{\"items\":[{\"userId\":\"admin.alpha\"}],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> tenants = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 }, null, TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<TenantDetail> detail = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" }, null, TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<TenantMember>> users = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 }, null, TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>> memberships = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user.alpha", PageSize = 20 }, null, TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>> audit = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery { TenantId = "tenant.alpha", PageSize = 20 }, null, TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> administrators = await client.GetGlobalAdministratorsAsync(
            new GetGlobalAdministratorsQuery { PageSize = 20 }, null, TestContext.Current.CancellationToken);

        tenants.Payload!.Items.Single().TenantId.ShouldBe("tenant.alpha");
        detail.Payload!.TenantId.ShouldBe("tenant.alpha");
        users.Payload!.Items.Single().UserId.ShouldBe("user.alpha");
        memberships.Payload!.Items.Single().TenantId.ShouldBe("tenant.alpha");
        audit.Payload!.Items.Single().EventId.ShouldBe("event-1");
        administrators.Payload!.Items.Single().UserId.ShouldBe("admin.alpha");
        new[] { tenants.IsSuccess, detail.IsSuccess, users.IsSuccess, memberships.IsSuccess, audit.IsSuccess, administrators.IsSuccess }
            .ShouldAllBe(static success => success);
    }

    [Fact]
    public async Task Tenant_audit_rejects_rows_outside_the_requested_tenant_scope()
    {
        var handler = new RecordingHandler(Success(
            "{\"items\":[{\"eventId\":\"event-1\",\"eventType\":\"TenantUpdated\",\"category\":\"Administrative\",\"actorId\":\"user.alpha\",\"timestamp\":\"2026-07-28T08:00:00Z\",\"tenantId\":\"tenant.beta\",\"narrativePayload\":{}}],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>> result = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery { TenantId = "tenant.alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Fact]
    public async Task Slash_route_identifiers_fail_closed_without_sending_an_ambiguous_request()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> detail = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant/alpha" },
            null,
            TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<TenantMember>> users = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant/alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>> memberships = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user/alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>> audit = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery { TenantId = "tenant/alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests.ShouldBeEmpty();
        new[] { detail.FailureKind, users.FailureKind, memberships.FailureKind, audit.FailureKind }
            .ShouldAllBe(static kind => kind == TenantsRestQueryFailureKind.Unavailable);
        new[] { detail.StatusCode, users.StatusCode, memberships.StatusCode, audit.StatusCode }
            .ShouldAllBe(static status => status == (int)HttpStatusCode.ServiceUnavailable);
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
    [InlineData("projectionbacked", "Current")]
    [InlineData("1", "Current")]
    [InlineData("ProjectionBacked", "current")]
    [InlineData("ProjectionBacked", "1")]
    public async Task Provenance_and_lifecycle_require_exact_canonical_enum_names(
        string provenance,
        string lifecycle)
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        response.Headers.Add("X-Hexalith-Query-Provenance", provenance);
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", lifecycle);
        response.Headers.Add("X-Hexalith-Is-Stale", "false");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        if (!string.Equals(provenance, "ProjectionBacked", StringComparison.Ordinal))
        {
            result.Metadata.Provenance.ShouldBe(QueryResponseProvenance.Unknown);
        }
        else
        {
            result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Degraded_header_is_honoured_on_ok_and_not_modified_responses(bool notModified)
    {
        HttpResponseMessage response = notModified
            ? ValidNotModified("list-etag")
            : Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        if (!notModified)
        {
            response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
            response.Headers.Add("X-Hexalith-Projection-Version", "projection-v1");
            response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
            response.Headers.Add("X-Hexalith-Is-Stale", "false");
        }

        response.Headers.Add("X-Hexalith-Is-Degraded", "true");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        if (notModified)
        {
            result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidMetadata);
            result.IsNotModified.ShouldBeFalse();
        }
        else
        {
            result.IsSuccess.ShouldBeTrue();
            result.Metadata.IsDegraded.ShouldBe(true);
            result.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        }
    }

    [Fact]
    public async Task Lifecycle_absent_not_modified_can_prove_current_from_projection_stale_false()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "projection-v1");
        response.Headers.Add("X-Hexalith-Is-Stale", "false");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeTrue();
        result.Freshness.ShouldBe(ReadModelFreshnessState.Current);
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        result.Metadata.IsStale.ShouldBe(false);
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
    [InlineData(null, "list-etag", false)]
    [InlineData("sent-etag", "different-etag", false)]
    [InlineData("sent-etag", "sent-etag", true)]
    public async Task Not_modified_requires_the_exact_strong_validator_sent_on_this_request(
        string? requestETag,
        string responseETag,
        bool expectedNotModified)
    {
        var handler = new RecordingHandler(ValidNotModified(responseETag));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            requestETag,
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBe(expectedNotModified);
        result.FailureKind.ShouldBe(expectedNotModified
            ? TenantsRestQueryFailureKind.None
            : TenantsRestQueryFailureKind.InvalidMetadata);
    }

    /// <summary>
    /// A base address carrying a path prefix must keep that prefix on every read.
    /// </summary>
    /// <remarks>
    /// The URI was previously built from the authority alone, so a gateway or reverse-proxy address such as
    /// https://host/tenants-api/ silently retargeted all six reads at https://host/api/... . Those 404s map
    /// to NotFound, which renders as authorization-safe absence — a misconfiguration presented as "no data".
    /// Every existing test used a path-less base address, so the case was unobservable.
    /// </remarks>
    [Fact]
    public async Task Configured_base_address_path_prefix_is_preserved_on_direct_reads()
    {
        var handler = new RecordingHandler(Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://gateway.invalid/tenants-api/"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests[0].PathAndQuery.ShouldBe("/tenants-api/api/tenants?pageSize=20");
    }

    /// <summary>
    /// Page-one recovery follows only an explicit contract signal, never a bare 400.
    /// </summary>
    /// <remarks>
    /// The service states an invalid cursor through the Problem Details <c>reason</c> extension carrying the
    /// shared <c>invalid-cursor</c> sentinel. Without reading it the client emitted "InvalidRequest" for
    /// every 400, so the gateway's recovery guards — which match "invalid-cursor" — could never fire in
    /// production even though their tests passed by constructing the reason code by hand.
    /// </remarks>
    [Fact]
    public async Task Explicit_invalid_cursor_reason_is_distinguished_from_a_plain_bad_request()
    {
        var signalled = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"type\":\"about:blank\",\"title\":\"Bad Request\",\"status\":400,\"reason\":\"invalid-cursor\"}",
                Encoding.UTF8,
                "application/problem+json"),
        };
        var plain = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                "{\"type\":\"about:blank\",\"title\":\"Bad Request\",\"status\":400}",
                Encoding.UTF8,
                "application/problem+json"),
        };
        var handler = new RecordingHandler(signalled, plain);
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> signalledResult = await client.ListTenantsAsync(
            new ListTenantsQuery { Cursor = "expired", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);
        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> plainResult = await client.ListTenantsAsync(
            new ListTenantsQuery { Cursor = "expired", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        signalledResult.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);
        plainResult.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidRequest);
    }

    [Theory]
    [InlineData("cancel", TenantsRestQueryFailureKind.Timeout)]
    [InlineData("http", TenantsRestQueryFailureKind.Unavailable)]
    [InlineData("io", TenantsRestQueryFailureKind.Unavailable)]
    public async Task Bad_request_body_transport_failures_keep_transport_classification(
        string failure,
        TenantsRestQueryFailureKind expected)
    {
        Exception exception = failure switch
        {
            "cancel" => new OperationCanceledException("unsafe-problem-timeout"),
            "http" => new HttpRequestException("unsafe-problem-http"),
            _ => new IOException("unsafe-problem-io"),
        };
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new ThrowingContent(exception),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { Cursor = "expired", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(expected);
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Chunked_problem_details_body_is_bounded_by_actual_bytes()
    {
        var contentStream = new RepeatingReadStream(TenantsRestQueryClient.MaximumProblemDetailsLength + 1L);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StreamContent(contentStream),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { Cursor = "expired", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidRequest);
        contentStream.BytesRead.ShouldBe(TenantsRestQueryClient.MaximumProblemDetailsLength + 1L);
    }

    [Fact]
    public async Task Request_etag_over_the_bound_is_not_sent()
    {
        string oversized = new('a', 1025);
        var handler = new RecordingHandler(Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            oversized,
            TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().IfNoneMatch.ShouldBeNull();
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Response_etag_over_the_bound_is_discarded_independently_of_validator_matching()
    {
        string oversized = new('a', 1025);
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        response.Headers.TryAddWithoutValidation("ETag", $"\"{oversized}\"").ShouldBeTrue();
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "projection-v1");
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
        response.Headers.Add("X-Hexalith-Is-Stale", "false");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        result.Freshness.ShouldBe(ReadModelFreshnessState.Current);
        result.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task Dot_only_route_identifiers_are_percent_encoded_instead_of_being_normalized_as_path_navigation()
    {
        var handler = new RecordingHandler(Success("{}"), Success("{}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.GetTenantAsync(new GetTenantQuery { TenantId = "." }, null, TestContext.Current.CancellationToken);
        _ = await client.GetTenantAsync(new GetTenantQuery { TenantId = ".." }, null, TestContext.Current.CancellationToken);

        handler.Requests.Select(static request => request.PathAndQuery).ShouldBe(
        [
            "/api/tenants/%2E",
            "/api/tenants/%2E%2E",
        ]);
    }

    [Theory]
    [InlineData("{\"items\":null,\"cursor\":null,\"hasMore\":false}")]
    [InlineData("{\"items\":[null],\"cursor\":null,\"hasMore\":false}")]
    [InlineData("{\"items\":[{\"tenantId\":null,\"name\":\"Alpha\",\"status\":\"Active\"}],\"cursor\":null,\"hasMore\":false}")]
    [InlineData("{\"items\":[],\"cursor\":null,\"hasMore\":true}")]
    [InlineData("{\"items\":[],\"cursor\":\"   \",\"hasMore\":true}")]
    public async Task Paginated_payload_shape_requires_items_and_a_cursor_when_has_more_is_true(string json)
    {
        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    // The TenantDetail arm of HasValidPayloadShape was reachable but never driven with a malformed detail:
    // every payload-shape test fed a PaginatedResult. A detail that deserializes with a null/blank tenant id,
    // a null Members collection, a null member, a blank member id, or a null Configuration would reach
    // TenantDetailSnapshot.Ready and then MemberAccessReview's OwnerCount/Detail.Members enumeration.
    [Theory]
    [InlineData("{\"tenantId\":null,\"name\":\"Alpha\",\"members\":[],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"   \",\"name\":\"Alpha\",\"members\":[],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":null,\"members\":[],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"members\":null,\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"members\":[null],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"members\":[{\"userId\":null,\"role\":\"TenantOwner\"}],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"members\":[{\"userId\":\"   \",\"role\":\"TenantOwner\"}],\"configuration\":{}}")]
    [InlineData("{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\",\"members\":[],\"configuration\":null}")]
    public async Task Tenant_detail_payload_shape_is_rejected_when_identity_members_or_configuration_are_malformed(string json)
    {
        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Fact]
    public async Task Well_formed_tenant_detail_payload_is_accepted()
    {
        var handler = new RecordingHandler(Success(
            "{\"tenantId\":\"tenant.alpha\",\"name\":\"Alpha\","
            + "\"members\":[{\"userId\":\"user.alpha\",\"role\":\"TenantOwner\"}],\"configuration\":{}}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        result.Payload.ShouldNotBeNull();
        result.Payload!.TenantId.ShouldBe("tenant.alpha");
        result.Payload.Members.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Numeric_payload_enum_values_are_rejected_instead_of_becoming_privileged_roles()
    {
        var handler = new RecordingHandler(Success(
            "{\"items\":[{\"userId\":\"user.alpha\",\"role\":1}],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantMember>> result = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Fact]
    public void Failure_kind_defaults_to_unknown_and_serializes_by_name()
    {
        default(TenantsRestQueryFailureKind).ShouldBe(TenantsRestQueryFailureKind.Unknown);
        JsonSerializer.Serialize(TenantsRestQueryFailureKind.Unavailable).ShouldBe("\"Unavailable\"");
    }

    [Fact]
    public async Task A_non_200_success_status_is_not_accepted_as_a_payload_response()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.Unavailable);
        result.StatusCode.ShouldBe((int)HttpStatusCode.NoContent);
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

    [Fact]
    public async Task Chunked_success_body_is_bounded_by_actual_bytes()
    {
        var contentStream = new RepeatingReadStream(TenantsRestQueryClient.MaximumPayloadLength + 1L);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StreamContent(contentStream),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        contentStream.BytesRead.ShouldBe(TenantsRestQueryClient.MaximumPayloadLength + 1L);
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
    public async Task Header_phase_io_failure_maps_to_unavailable()
    {
        using var httpClient = new HttpClient(new ThrowingHandler(new IOException("unsafe-io-detail")))
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.Unavailable);
    }

    [Theory]
    [InlineData(false, TenantsRestQueryFailureKind.Unavailable)]
    [InlineData(true, TenantsRestQueryFailureKind.Timeout)]
    public async Task Body_phase_transport_failures_map_to_fixed_support_safe_categories(
        bool timeout,
        TenantsRestQueryFailureKind expected)
    {
        Exception exception = timeout
            ? new TaskCanceledException("unsafe-body-timeout")
            : new IOException("unsafe-body-io");
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent(exception),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(expected);
        result.Payload.ShouldBeNull();
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Plain_non_caller_operation_cancellation_at_headers_maps_to_timeout()
    {
        using var httpClient = new HttpClient(new ThrowingHandler(new OperationCanceledException("unsafe-timeout")))
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.Timeout);
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Plain_non_caller_operation_cancellation_during_body_maps_to_timeout()
    {
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ThrowingContent(new OperationCanceledException("unsafe-body-timeout")),
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.Timeout);
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Linked_transport_deadline_covers_success_and_problem_details_bodies(bool badRequest)
    {
        var handler = new RecordingHandler(new HttpResponseMessage(
            badRequest ? HttpStatusCode.BadRequest : HttpStatusCode.OK)
    {
            Content = new StreamContent(new BlockingReadStream()),
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.invalid"),
            Timeout = TimeSpan.FromMilliseconds(100),
        };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.Timeout);
        result.StatusCode.ShouldBe((int)HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task Caller_cancellation_before_request_propagates()
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

    [Fact]
    public async Task Caller_cancellation_during_response_body_read_propagates()
    {
        using var cancellation = new CancellationTokenSource();
        var content = new CallerCancellingContent(cancellation);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = content,
        });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await Should.ThrowAsync<OperationCanceledException>(() => client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            cancellation.Token));

        handler.Requests.Count.ShouldBe(1);
        content.WasRead.ShouldBeTrue();
    }

    private static HttpResponseMessage Success(string json)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };

    private static HttpResponseMessage ValidNotModified(string eTag)
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{eTag}\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "projection-v1");
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
        response.Headers.Add("X-Hexalith-Is-Stale", "false");
        return response;
    }

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

    private sealed class ThrowingContent(Exception exception) : HttpContent
    {
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
            => Task.FromException(exception);

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class CallerCancellingContent(CancellationTokenSource cancellation) : HttpContent
    {
        public bool WasRead { get; private set; }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            WasRead = true;
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class RepeatingReadStream : Stream
    {
        private readonly long _length;
        private long _remaining;

        public RepeatingReadStream(long length)
        {
            _length = length;
            _remaining = length;
        }

        public long BytesRead { get; private set; }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => _length;

        public override long Position
        {
            get => _length - _remaining;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int read = (int)Math.Min(count, _remaining);
            Array.Fill(buffer, (byte)'x', offset, read);
            _remaining -= read;
            BytesRead += read;
            return read;
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            {
            cancellationToken.ThrowIfCancellationRequested();
            int read = (int)Math.Min(buffer.Length, _remaining);
            buffer.Span[..read].Fill((byte)'x');
            _remaining -= read;
            BytesRead += read;
            return ValueTask.FromResult(read);
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class BlockingReadStream : Stream
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
            {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return 0;
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
