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
            "detail-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", Cursor = "users cursor/+", PageSize = 20 },
            "users-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user.alpha", Cursor = "membership cursor/+", PageSize = 12 },
            "membership-etag",
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
            "audit-etag",
            TestContext.Current.CancellationToken);
        _ = await client.GetGlobalAdministratorsAsync(
            new GetGlobalAdministratorsQuery { Cursor = "admin cursor/+", PageSize = 10 },
            "admins-etag",
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
        // All six, not just the list and member reads. Previously only Requests[0] and Requests[2] were
        // asserted and the other four were called with a null eTag, so replacing the eTag argument with
        // null inside GetTenantAsync, GetUserTenantsAsync, GetTenantAuditAsync or
        // GetGlobalAdministratorsAsync passed the suite -- those reads would silently full-fetch on every
        // refresh and every 304-retention behaviour built on them becomes unreachable.
        handler.Requests.Select(static request => request.IfNoneMatch).ShouldBe(
        [
            "\"list-etag\"",
            "\"detail-etag\"",
            "\"users-etag\"",
            "\"membership-etag\"",
            "\"audit-etag\"",
            "\"admins-etag\"",
        ]);
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
    public async Task Global_administrators_reject_duplicate_user_identities()
    {
        var handler = new RecordingHandler(Success(
            "{\"items\":[{\"userId\":\"admin.alpha\"},{\"userId\":\"admin.alpha\"}],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> result = await client
            .GetGlobalAdministratorsAsync(
                new GetGlobalAdministratorsQuery { PageSize = 20 },
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

        // UnsupportedRouteIdentifier, not InvalidRequest or Unavailable: no request was sent, so this must
        // remain distinguishable from both a server-issued 400 and the Tenants API being down.
        new[] { detail.FailureKind, users.FailureKind, memberships.FailureKind, audit.FailureKind }
            .ShouldAllBe(static kind => kind == TenantsRestQueryFailureKind.UnsupportedRouteIdentifier);
        new[] { detail.StatusCode, users.StatusCode, memberships.StatusCode, audit.StatusCode }
            .ShouldAllBe(static status => status == (int)HttpStatusCode.BadRequest);
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

        // AC3: ServedAt never determines projection age. The header is parsed and carried, but a far-future
        // value cannot pull a Stale projection to Current. Without these two assertions the header line
        // above could be deleted with the test unchanged, and making ResolveFreshness consume ServedAt
        // would break nothing.
        result.Metadata.ServedAt.ShouldBe(
            DateTimeOffset.Parse("2099-01-01T00:00:00.0000000+00:00", System.Globalization.CultureInfo.InvariantCulture));
        result.Metadata.IsStale.ShouldBe(true);
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

        // This IS the load-bearing assertion for the client's freshness resolver: the supported-304 gate
        // accepts only Current or Stale, so a retained 304 on Lifecycle=Unknown + IsStale=false proves the
        // resolver classified it Current. The response no longer publishes a Freshness member, because the
        // rendered value comes from TenantQueryGateway.ResolveFreshness, not from here.
        result.IsNotModified.ShouldBeTrue();
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
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    [Theory]
    // The all-true row is the acceptance polarity. Without it the `if (strongETag && projectionBacked &&
    // projectionVersion && freshness)` arm below never executed, so the theory read as proving both
    // polarities while proving only rejection.
    [InlineData(true, true, true, true)]
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
        // Four times the bound, not bound + 1. With a stream of exactly bound + 1 the assertion
        // `BytesRead == bound + 1` is the stream's own length, so "the cap stopped the read" and "the
        // stream ran out" were indistinguishable and the cap could be deleted with the test green.
        var contentStream = new RepeatingReadStream(TenantsRestQueryClient.MaximumProblemDetailsLength * 4L);
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
        contentStream.BytesRead.ShouldBeGreaterThan(TenantsRestQueryClient.MaximumProblemDetailsLength);
        contentStream.BytesRead.ShouldBeLessThan(TenantsRestQueryClient.MaximumProblemDetailsLength * 2L);
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
        result.ETag.ShouldBeNull();
    }

    [Theory]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("...")]
    [InlineData("\\")]
    [InlineData("tenant\\alpha")]
    public async Task Dot_only_and_separator_route_identifiers_are_rejected_without_sending_a_request(string tenantId)
    {
        // Superseded: these used to be percent-escaped to %2E and sent. Escaping is not a durable guarantee
        // -- the upstream API may normalize %2E during routing, and a resolved ".." under a future DAPR
        // invoke path could traverse out of the /v1.0/invoke/{appId}/method/ prefix. A route identity that
        // is only dots, or carries a separator, is not a usable identity, so it is refused here.
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<TenantDetail> result = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = tenantId },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests.ShouldBeEmpty();
        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.UnsupportedRouteIdentifier);
        result.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task An_identifier_containing_dots_among_other_characters_is_still_escaped_and_sent()
    {
        // The rejection is for identities that are ONLY dots; ordinary dotted ids must keep working.
        var handler = new RecordingHandler(Success("{}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = "tenant.alpha" },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests.Select(static request => request.PathAndQuery).ShouldBe(["/api/tenants/tenant.alpha"]);
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
            $"TenantsRestQueryResponse {{ IsSuccess = False, IsNotModified = False, FailureKind = {expected}, StatusCode = {(int)statusCode} }}");
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
    }

    [Fact]
    public async Task Chunked_success_body_is_bounded_by_actual_bytes()
    {
        // Four times the bound, not bound + 1 -- the same correction already applied to the problem-details
        // twin. With a stream of exactly bound + 1 the assertion `BytesRead == bound + 1` is the stream's
        // own length, so "the cap stopped the read" and "the stream ran out" are indistinguishable and the
        // cap can be deleted with the test green.
        var contentStream = new RepeatingReadStream(TenantsRestQueryClient.MaximumPayloadLength * 4L);
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
        contentStream.BytesRead.ShouldBeGreaterThan(TenantsRestQueryClient.MaximumPayloadLength);
        contentStream.BytesRead.ShouldBeLessThan(TenantsRestQueryClient.MaximumPayloadLength * 2L);
    }

    [Fact]
    public async Task A_declared_content_length_over_the_bound_is_rejected_without_reading_the_body()
    {
        // The chunked path proves the streaming cap. The declared-Content-Length fast path is a separate
        // branch and had no test at all: an oversized body that announces its own size must be rejected
        // before any of it is read, not merely capped part-way through.
        var contentStream = new RepeatingReadStream(TenantsRestQueryClient.MaximumPayloadLength * 4L);
        var content = new StreamContent(contentStream);
        content.Headers.ContentLength = TenantsRestQueryClient.MaximumPayloadLength + 1L;
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
        contentStream.BytesRead.ShouldBe(0L);
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
            $"TenantsRestQueryResponse {{ IsSuccess = False, IsNotModified = False, FailureKind = {expected}, StatusCode = 503 }}");
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

        // Bounded on purpose. The blocking stream awaits only the token production supplies, so removing
        // the linked transport deadline makes this read block forever -- and under `-parallel none` with
        // no per-test timeout that surfaces as a stuck CI job with no attribution rather than a red test.
        // WaitAsync turns the regression into a failure the runner can name.
        Task<TenantsRestQueryResponse<PaginatedResult<TenantSummary>>> read = client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result =
            await read.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData("alpha?pageSize=9999", "/api/tenants/alpha%3FpageSize%3D9999")]
    [InlineData("alpha#fragment", "/api/tenants/alpha%23fragment")]
    [InlineData("alpha beta", "/api/tenants/alpha%20beta")]
    [InlineData("alpha&x=1", "/api/tenants/alpha%26x%3D1")]
    public async Task Route_identifiers_are_percent_escaped_before_the_uri_is_built(
        string tenantId,
        string expectedPath)
    {
        // The only prior escaping test used "tenant.alpha", where every character is unreserved and
        // Uri.EscapeDataString is the identity function -- so the escape call was unpinned. It matters
        // because TryEscapeRouteValue rejects only '/', '\\' and all-dot values, and CreateRequestUri runs
        // with DangerousDisablePathAndQueryCanonicalization = true: without the escape, a caller-supplied
        // tenant id splices a query parameter into the request or truncates it at a fragment.
        var handler = new RecordingHandler(Success("{\"tenantId\":\"alpha\",\"name\":\"Alpha\",\"members\":[],\"configuration\":{}}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.GetTenantAsync(
            new GetTenantQuery { TenantId = tenantId },
            null,
            TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().PathAndQuery.ShouldBe(expectedPath);
    }

    [Theory]
    [InlineData("Rebuilding")]
    [InlineData("Degraded")]
    [InlineData("Unavailable")]
    [InlineData("LocalOnly")]
    public async Task Not_modified_is_rejected_for_lifecycle_states_that_are_not_current_or_stale(string lifecycle)
    {
        // Every other fixture in this file uses Current, Stale or an absent/garbage header, so the
        // `_ => Unknown` fail-closed arm of ResolveFreshness could be changed to `_ => Current` with the
        // suite green -- accepting a 304 received while the projection is Rebuilding and rendering the
        // retained page as current. All four are real emitted values with EN and FR resources.
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "index-v7");
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", lifecycle);
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeFalse();
        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidMetadata);
    }

    [Fact]
    public async Task Contradictory_not_modified_metadata_cannot_prove_retention_through_is_stale()
    {
        // The sibling contradiction theory builds 200s and asserts only Metadata.Lifecycle, so the
        // `isStale = null` half of the contradiction reset was unpinned. Without it, Lifecycle collapses to
        // Unknown but isStale stays true, ResolveFreshness returns Stale through the Unknown arm, and
        // IsSupportedNotModified accepts Stale -- retaining the old payload on evidence the client just
        // declared contradictory.
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.Add("X-Hexalith-Projection-Version", "index-v7");
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
        response.Headers.Add("X-Hexalith-Is-Stale", "true");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeFalse();
        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidMetadata);
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
        result.Metadata.IsStale.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("oversized")]
    public async Task A_blank_or_over_bound_projection_version_header_is_discarded_and_fails_the_gate(string shape)
    {
        // GetBoundedHeader returns null for a blank value or one over the metadata bound, and
        // IsSupportedNotModified gates on ProjectionVersion is not null. No test emitted either shape: the
        // ETag-bound test covers a different header, and the gateway-side blank-version test injects at the
        // metadata level, never exercising the header parser. Either shape turns every conditional 304 on
        // all six reads into a rejected response.
        string value = shape == "oversized" ? new string('v', 4097) : shape;
        var response = new HttpResponseMessage(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"list-etag\"");
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        response.Headers.TryAddWithoutValidation("X-Hexalith-Projection-Version", value);
        response.Headers.Add("X-Hexalith-Projection-Lifecycle", "Current");
        response.Headers.Add("X-Hexalith-Is-Stale", "false");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            "list-etag",
            TestContext.Current.CancellationToken);

        result.Metadata.ProjectionVersion.ShouldBeNull();
        result.IsNotModified.ShouldBeFalse();
        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidMetadata);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task A_duplicated_metadata_header_is_discarded_rather_than_arbitrarily_chosen(
        bool projectionBackedFirst)
    {
        // A multi-valued X-Hexalith-Query-Provenance is a real hostile-proxy state. Without the
        // duplicate-value rejection the client would pick one arbitrarily and treat it as authoritative.
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"items\":[],\"cursor\":null,\"hasMore\":false}", Encoding.UTF8, "application/json"),
        };
        string[] values = projectionBackedFirst
            ? ["ProjectionBacked", "HandlerComputed"]
            : ["HandlerComputed", "ProjectionBacked"];
        foreach (string value in values)
        {
            response.Headers.Add("X-Hexalith-Query-Provenance", value);
        }
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Metadata.Provenance.ShouldBe(QueryResponseProvenance.Unknown);
        result.Metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Unknown);
    }

    [Theory]
    [InlineData("*")]
    [InlineData("\"\"")]
    public async Task A_wildcard_or_empty_response_etag_is_not_retained(string eTagHeader)
    {
        // GetStrongETag rejects `ETag: *` and an empty tag; neither had a test, so both rejections could be
        // deleted and a meaningless validator retained and later sent back as If-None-Match.
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"items\":[],\"cursor\":null,\"hasMore\":false}", Encoding.UTF8, "application/json"),
        };
        response.Headers.TryAddWithoutValidation("ETag", eTagHeader);
        response.Headers.Add("X-Hexalith-Query-Provenance", "ProjectionBacked");
        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.ETag.ShouldBeNull();
    }

    [Fact]
    public async Task A_json_null_body_is_not_accepted_as_a_payload()
    {
        // No fixture returned a JSON null body. Without the payload-null guard HasValidPayloadShape(null)
        // falls to its `_ => true` arm and the client reports IsSuccess with a null payload -- producible
        // whenever the generated controller's handler value is null.
        var handler = new RecordingHandler(Success("null"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeFalse();
        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public async Task Member_page_shape_requires_a_user_identity_on_every_row(string userId)
    {
        string json = $$"""
            {"items":[{"userId":{{userId}},"role":"TenantOwner"}],"cursor":null,"hasMore":false}
            """;
        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantMember>> result = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Theory]
    [InlineData("null", "\"Alpha\"")]
    [InlineData("\"\"", "\"Alpha\"")]
    [InlineData("\"   \"", "\"Alpha\"")]
    [InlineData("\"tenant.alpha\"", "null")]
    public async Task Membership_page_shape_requires_a_tenant_identity_and_non_null_name_on_every_row(
        string tenantId,
        string name)
    {
        string json = $$"""
            {"items":[{"tenantId":{{tenantId}},"name":{{name}},"role":"TenantOwner"}],"cursor":null,"hasMore":false}
            """;
        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>> result = await client.GetUserTenantsAsync(
            new GetUserTenantsQuery { UserId = "user.alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("\"\"")]
    [InlineData("\"   \"")]
    public async Task Global_administrator_page_shape_requires_a_user_identity_on_every_row(string userId)
    {
        // Only the list and detail reads had negative shape fixtures, so the membership, member, audit and
        // global-administrator predicates could each be replaced with `true` and nothing failed.
        string json = $$"""
            {"items":[{"userId":{{userId}}}],"cursor":null,"hasMore":false}
            """;
        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> result = await client.GetGlobalAdministratorsAsync(
            new GetGlobalAdministratorsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    [Fact]
    public async Task A_paged_read_other_than_the_list_rejects_has_more_without_a_cursor()
    {
        // IsValidPage's HasMore-without-cursor rule was proved only for the list read.
        var handler = new RecordingHandler(Success("{\"items\":[],\"cursor\":null,\"hasMore\":true}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantMember>> result = await client.GetTenantUsersAsync(
            new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    /// <summary>
    /// The audit page predicate is the largest of the six and had no negative fixture at all.
    /// </summary>
    /// <remarks>
    /// Every other read gained one; the audit predicate's five clauses could each be replaced with
    /// <c>true</c> with the suite green, so a row missing its event id, event type, actor, tenant scope or
    /// narrative payload reached the surface as authoritative audit evidence.
    /// </remarks>
    [Theory]
    [InlineData("event id", "null", "\"TenantUpdated\"", "\"actor.user\"", "\"tenant.alpha\"", "{}")]
    [InlineData("event type", "\"event-1\"", "\" \"", "\"actor.user\"", "\"tenant.alpha\"", "{}")]
    [InlineData("actor", "\"event-1\"", "\"TenantUpdated\"", "null", "\"tenant.alpha\"", "{}")]
    [InlineData("tenant scope", "\"event-1\"", "\"TenantUpdated\"", "\"actor.user\"", "\"\"", "{}")]
    [InlineData("narrative payload", "\"event-1\"", "\"TenantUpdated\"", "\"actor.user\"", "\"tenant.alpha\"", "null")]
    public async Task Audit_page_shape_requires_every_evidence_field_on_every_row(
        string because,
        string eventId,
        string eventType,
        string actorId,
        string tenantId,
        string narrativePayload)
    {
        string json = $$"""
            {"items":[{"eventId":{{eventId}},"eventType":{{eventType}},"category":"Administrative","actorId":{{actorId}},"occurredAt":"2026-07-28T08:00:00Z","tenantId":{{tenantId}},"narrativePayload":{{narrativePayload}}}],"cursor":null,"hasMore":false}
            """;

        var handler = new RecordingHandler(Success(json));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>> result = await client.GetTenantAuditAsync(
            new GetTenantAuditQuery { TenantId = "tenant.alpha", PageSize = 50 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(
            TenantsRestQueryFailureKind.InvalidPayload,
            $"a row with no {because} is not audit evidence");
        result.Payload.ShouldBeNull();
    }

    /// <summary>
    /// The <c>item.Name is not null</c> clause on the list and membership pages had no fixture.
    /// </summary>
    /// <remarks>
    /// Replacing it with <c>true</c> survived: every existing shape fixture varies the identifier, never the
    /// name. A null name reaches the row renderer, which is the one field the list surface displays beside
    /// the tenant identity.
    /// </remarks>
    [Fact]
    public async Task List_page_shape_requires_a_non_null_name_on_every_row()
    {
        var handler = new RecordingHandler(Success(
            "{\"items\":[{\"tenantId\":\"tenant.alpha\",\"name\":null,\"status\":\"Active\"}],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidPayload);
        result.Payload.ShouldBeNull();
    }

    /// <summary>
    /// The remaining header-hardening branches, each individually deletable with the suite green.
    /// </summary>
    /// <remarks>
    /// Covered before this: ETag length bounds, weak-ETag rejection, duplicate
    /// <c>X-Hexalith-Query-Provenance</c>, <c>ETag: *</c> and the empty tag. Still uncovered: a duplicate
    /// <c>ETag</c> header, and control-character rejection in <c>GetBoundedHeader</c> and
    /// <c>GetStrongETag</c>. A multi-valued or control-bearing header is a real hostile-proxy state.
    /// </remarks>
    [Fact]
    public async Task A_duplicate_etag_header_is_not_retained()
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        response.Headers.TryAddWithoutValidation("ETag", "\"first\"");
        response.Headers.TryAddWithoutValidation("ETag", "\"second\"");
        AddHeader(response, "X-Hexalith-Query-Provenance", "ProjectionBacked");
        AddHeader(response, "X-Hexalith-Projection-Version", "projection-v1");

        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.ETag.ShouldBeNull("two ETag headers name two different entities; neither may be retained");
    }

    [Fact]
    public async Task A_control_character_in_a_metadata_header_is_rejected()
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        AddHeader(response, "X-Hexalith-Query-Provenance", "ProjectionBacked");
        AddHeader(response, "X-Hexalith-Projection-Version", "projectionv1");

        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Metadata.ProjectionVersion.ShouldBeNull();
    }

    [Fact]
    public async Task A_control_character_in_the_etag_is_rejected()
    {
        HttpResponseMessage response = Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}");
        response.Headers.TryAddWithoutValidation("ETag", "\"listv1\"");
        AddHeader(response, "X-Hexalith-Query-Provenance", "ProjectionBacked");

        var handler = new RecordingHandler(response);
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> result = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            null,
            TestContext.Current.CancellationToken);

        result.ETag.ShouldBeNull();
    }

    /// <summary>
    /// A validator the client cannot normalize is never sent, rather than sent malformed.
    /// </summary>
    /// <remarks>
    /// A gateway-level ETag never contains quotes -- <c>GetStrongETag</c> strips them -- so a value that
    /// does is either a caller defect or an injection attempt, and control characters cannot occupy a header
    /// at all. Both branches were individually deletable with the suite green.
    /// </remarks>
    [Theory]
    [InlineData("has\"quote")]
    [InlineData("hascontrol")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task An_unnormalizable_request_validator_is_not_sent(string eTag)
    {
        var handler = new RecordingHandler(Success("{\"items\":[],\"cursor\":null,\"hasMore\":false}"));
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://tenants.invalid") };
        var client = new TenantsRestQueryClient(httpClient);

        _ = await client.ListTenantsAsync(
            new ListTenantsQuery { PageSize = 20 },
            eTag,
            TestContext.Current.CancellationToken);

        handler.Requests.ShouldHaveSingleItem().IfNoneMatch.ShouldBeNull();
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
