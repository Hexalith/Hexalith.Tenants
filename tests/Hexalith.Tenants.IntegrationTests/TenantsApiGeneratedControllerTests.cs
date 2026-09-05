#pragma warning disable CA2007

extern alias TenantsApi;

using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public sealed class TenantsApiGeneratedControllerTests
{
    private const string JwtAudience = "hexalith-eventstore";
    private const string JwtIssuer = "hexalith-dev";
    private const string JwtSigningKey = "this-is-a-generated-api-test-signing-key-minimum-32-chars";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    [Fact]
    public async Task ListTenants_requires_authorization_before_gateway_submission()
    {
        CapturingEventStoreGatewayClient gateway = new();
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/api/tenants");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        gateway.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListTenants_generated_route_submits_index_query_and_forwards_freshness_headers()
    {
        DateTimeOffset servedAt = DateTimeOffset.Parse("2026-07-03T05:00:00Z", CultureInfo.InvariantCulture);
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>(
                [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
                "next-cursor",
                true),
            eTag: "index-etag-2",
            metadata: new QueryResponseMetadata(
                ETag: "index-etag-2",
                IsStale: true,
                ProjectionVersion: "index-v2",
                ServedAt: servedAt)
            {
                Provenance = QueryResponseProvenance.ProjectionBacked,
            });
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants?cursor=opaque&pageSize=25");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe("\"index-etag-2\"");
        response.Headers.GetValues("X-Hexalith-Projection-Version").ShouldHaveSingleItem().ShouldBe("index-v2");
        response.Headers.GetValues("X-Hexalith-Served-At").ShouldHaveSingleItem().ShouldBe(servedAt.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.GetValues("X-Hexalith-Is-Stale").ShouldHaveSingleItem().ShouldBe("true");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("items").GetArrayLength().ShouldBe(1);
        body.GetProperty("items")[0].GetProperty("tenantId").GetString().ShouldBe("tenant.alpha");

        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(ListTenantsQuery.Domain);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBeNull();
        query.Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(ListTenantsQuery.ProjectionType);
        query.IfNoneMatch.ShouldBeNull();
        query.CancellationToken.CanBeCanceled.ShouldBeTrue();
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(25);
    }

    [Fact]
    public async Task Generated_query_route_suppresses_projection_headers_for_handler_computed_result()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(
            new TenantDetail(
                "tenant.alpha",
                "Alpha",
                "Tenant Alpha",
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(StringComparer.Ordinal),
                DateTimeOffset.Parse("2026-07-03T05:20:00Z", CultureInfo.InvariantCulture)),
            eTag: "opaque-store-etag",
            metadata: new QueryResponseMetadata(
                ETag: "opaque-store-etag",
                IsNotModified: false,
                IsStale: false,
                ProjectionVersion: "tenant-sequence:42")
            {
                Provenance = QueryResponseProvenance.HandlerComputed,
                Lifecycle = ProjectionLifecycleState.Current,
            });
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants/tenant.alpha");
        request.Headers.IfNoneMatch.ParseAdd("\"conflicting-validator\"");

        using HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.GetValues("X-Hexalith-Query-Provenance").ShouldHaveSingleItem().ShouldBe("HandlerComputed");
        response.Headers.ETag.ShouldBeNull();
        response.Headers.Contains("X-Hexalith-Projection-Version").ShouldBeFalse();
        response.Headers.Contains("X-Hexalith-Is-Stale").ShouldBeFalse();
        response.Headers.Contains(ProjectionLifecyclePolicy.HeaderName).ShouldBeFalse();

        TenantDetail? detail = await response.Content.ReadFromJsonAsync<TenantDetail>(
            JsonOptions,
            TestContext.Current.CancellationToken);
        detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        gateway.SubmittedQueries.ShouldHaveSingleItem().IfNoneMatch.ShouldBe("\"conflicting-validator\"");
    }

    [Fact]
    public async Task UserTenants_generated_absolute_route_submits_index_query_for_target_user_entity()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/users/user.alpha/tenants?pageSize=12");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("user.alpha");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.Payload.ShouldNotBeNull().GetProperty("pageSize").GetInt32().ShouldBe(12);
    }

    [Fact]
    public async Task TenantDetail_generated_route_submits_tenant_scoped_query_for_route_tenant()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new TenantDetail(
            "tenant.alpha",
            "Alpha",
            "Tenant Alpha",
            TenantStatus.Active,
            [],
            new Dictionary<string, string>(StringComparer.Ordinal),
            DateTimeOffset.Parse("2026-07-03T05:20:00Z", CultureInfo.InvariantCulture)));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant.alpha");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantQuery.ProjectionType);
        query.Request.Payload.ShouldBeNull();
    }

    [Fact]
    public async Task TenantUsers_generated_route_submits_tenant_scoped_query_with_paging_payload()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<TenantMember>(
            [new TenantMember("user.alpha", TenantRole.TenantReader)],
            "users-next",
            true));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants/tenant.alpha/users?cursor=user-cursor&pageSize=20");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantUsersQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantUsersQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantUsersQuery.ProjectionType);
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("user-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(20);
    }

    [Fact]
    public async Task GlobalAdministrators_generated_absolute_route_submits_fixed_platform_scope_query()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<GlobalAdministratorSummary>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/global-administrators?pageSize=10");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetGlobalAdministratorsQuery.Domain);
        query.Request.AggregateId.ShouldBe("global-administrators");
        query.Request.EntityId.ShouldBe("global-administrators");
        query.Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetGlobalAdministratorsQuery.ProjectionType);
    }

    [Fact]
    public async Task TenantAudit_generated_route_submits_tenant_scoped_filter_payload()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync(
            "/api/tenants/tenant.alpha/audit?category=Access&cursor=audit-cursor&pageSize=30");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        SubmittedQuery query = gateway.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantAuditQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantAuditQuery.ProjectionType);
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("category").GetString().ShouldBe(nameof(AuditEventCategory.Access));
        payload.GetProperty("cursor").GetString().ShouldBe("audit-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(30);
    }

    [Fact]
    public async Task Generated_query_route_preserves_if_none_match_and_returns_not_modified_with_freshness_headers()
    {
        DateTimeOffset servedAt = DateTimeOffset.Parse("2026-07-03T05:15:00Z", CultureInfo.InvariantCulture);
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueNotModified(
            "index-etag-2",
            new QueryResponseMetadata(
                ETag: "index-etag-2",
                IsNotModified: true,
                IsStale: false,
                ProjectionVersion: "index-v2",
                ServedAt: servedAt)
            {
                Provenance = QueryResponseProvenance.ProjectionBacked,
            });
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants?pageSize=25");
        request.Headers.IfNoneMatch.ParseAdd("\"index-etag-1\"");

        HttpResponseMessage response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        response.Headers.ETag.ShouldNotBeNull().Tag.ShouldBe("\"index-etag-2\"");
        response.Headers.GetValues("X-Hexalith-Projection-Version").ShouldHaveSingleItem().ShouldBe("index-v2");
        response.Headers.GetValues("X-Hexalith-Served-At").ShouldHaveSingleItem().ShouldBe(servedAt.ToString("O", CultureInfo.InvariantCulture));
        response.Headers.GetValues("X-Hexalith-Is-Stale").ShouldHaveSingleItem().ShouldBe("false");
        gateway.SubmittedQueries.ShouldHaveSingleItem().IfNoneMatch.ShouldBe("\"index-etag-1\"");
    }

    [Fact]
    public async Task Generated_query_route_returns_safe_problem_when_not_modified_has_no_strong_etag()
    {
        await AssertNotModifiedWithoutStrongETagMapsToBadGatewayAsync(null);
    }

    [Fact]
    public async Task Generated_query_route_returns_safe_problem_when_not_modified_has_weak_etag()
    {
        await AssertNotModifiedWithoutStrongETagMapsToBadGatewayAsync("W/\"index-etag-2\"");
    }

    [Fact]
    public async Task Generated_query_route_returns_safe_problem_when_not_modified_is_not_projection_backed()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueNotModified(
            "index-etag-2",
            new QueryResponseMetadata(ETag: "index-etag-2", IsNotModified: true));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants");
        request.Headers.IfNoneMatch.ParseAdd("\"index-etag-2\"");

        HttpResponseMessage response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        response.Headers.Contains("ETag").ShouldBeFalse();
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(
            cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("title").GetString().ShouldBe("Bad Gateway");
    }

    [Theory]
    [InlineData("wrong-issuer", JwtAudience)]
    [InlineData(JwtIssuer, "wrong-audience")]
    public async Task ListTenants_rejects_token_with_untrusted_issuer_or_audience(string issuer, string audience)
    {
        CapturingEventStoreGatewayClient gateway = new();
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt("test-user", "queries:*", issuer, audience));

        HttpResponseMessage response = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        gateway.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task ListTenants_authority_mode_uses_discovery_signing_keys_and_validates_issuer()
    {
        const string authority = "https://identity.example.test";
        var discoveryConfiguration = new OpenIdConnectConfiguration
        {
            Issuer = authority,
        };
        discoveryConfiguration.SigningKeys.Add(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)));

        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));
        await using var factory = new TenantsApiWebApplicationFactory(
            gateway,
            new Dictionary<string, string?>
            {
                ["EventStore:Authentication:Authority"] = authority,
                ["EventStore:Authentication:Audience"] = JwtAudience,
                ["EventStore:Authentication:RequireHttpsMetadata"] = "false",
            },
            services => services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options => options.ConfigurationManager =
                    new StaticConfigurationManager<OpenIdConnectConfiguration>(discoveryConfiguration)));
        using HttpClient client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt("test-user", "queries:*", authority, JwtAudience));
        HttpResponseMessage accepted = await client.GetAsync(
            "/api/tenants",
            TestContext.Current.CancellationToken);

        accepted.StatusCode.ShouldBe(HttpStatusCode.OK);
        gateway.SubmittedQueries.Count.ShouldBe(1);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            CreateJwt("test-user", "queries:*", "https://untrusted.example.test", JwtAudience));
        HttpResponseMessage rejected = await client.GetAsync(
            "/api/tenants",
            TestContext.Current.CancellationToken);

        rejected.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        gateway.SubmittedQueries.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(null, "EventStore:Authentication:SigningKey is required when Authority is not configured.")]
    [InlineData("", "EventStore:Authentication:SigningKey is required when Authority is not configured.")]
    [InlineData("   ", "EventStore:Authentication:SigningKey is required when Authority is not configured.")]
    [InlineData("short-key", "EventStore:Authentication:SigningKey must be at least 32 bytes (256 bits) for HS256 token validation.")]
    public void Symmetric_key_mode_rejects_missing_blank_or_short_signing_key(
        string? signingKey,
        string expectedMessage)
    {
        CapturingEventStoreGatewayClient gateway = new();
        using var factory = new TenantsApiWebApplicationFactory(
            gateway,
            new Dictionary<string, string?>
            {
                ["EventStore:Authentication:Issuer"] = JwtIssuer,
                ["EventStore:Authentication:Audience"] = JwtAudience,
                ["EventStore:Authentication:SigningKey"] = signingKey,
            });

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            factory.Services
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme));

        exception.Message.ShouldBe(expectedMessage);
    }

    [Fact]
    public void Symmetric_key_mode_accepts_exactly_32_bytes()
    {
        string signingKey = new('a', 32);
        CapturingEventStoreGatewayClient gateway = new();
        using var factory = new TenantsApiWebApplicationFactory(
            gateway,
            new Dictionary<string, string?>
            {
                ["EventStore:Authentication:Issuer"] = JwtIssuer,
                ["EventStore:Authentication:Audience"] = JwtAudience,
                ["EventStore:Authentication:SigningKey"] = signingKey,
            });

        JwtBearerOptions options = factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);

        SymmetricSecurityKey key = options.TokenValidationParameters.IssuerSigningKey
            .ShouldBeOfType<SymmetricSecurityKey>();
        key.KeySize.ShouldBe(256);
    }

    private static async Task AssertNotModifiedWithoutStrongETagMapsToBadGatewayAsync(string? eTag)
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueNotModified(
            eTag,
            new QueryResponseMetadata(
                ETag: eTag,
                IsNotModified: true)
            {
                Provenance = QueryResponseProvenance.ProjectionBacked,
            });
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadGateway);
        response.Headers.Contains("ETag").ShouldBeFalse();
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("title").GetString().ShouldBe("Bad Gateway");
    }

    [Fact]
    public async Task Generated_query_route_maps_gateway_failure_to_safe_problem_details()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueQueryFailure(new EventStoreGatewayException(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            detail: "Access denied.",
            correlationId: "01KTESTCORRELATION00000",
            tenantId: TenantIdentity.DefaultTenantId,
            reasonCode: "tenant-forbidden"));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);

        HttpResponseMessage response = await client.GetAsync("/api/tenants", TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.GetProperty("correlationId").GetString().ShouldBe("01KTESTCORRELATION00000");
        problem.GetProperty("tenantId").GetString().ShouldBe(TenantIdentity.DefaultTenantId);
        problem.GetProperty("reasonCode").GetString().ShouldBe("tenant-forbidden");
    }

    [Fact]
    public async Task Generated_query_route_propagates_request_cancellation_to_gateway()
    {
        CapturingEventStoreGatewayClient gateway = new()
        {
            BlockQueriesUntilCancellation = true,
        };
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory);
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        Task<HttpResponseMessage> request = client.GetAsync("/api/tenants", cancellation.Token);
        CancellationToken gatewayToken = await gateway.QueryStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        cancellation.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => request);
        gatewayToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Generated_command_routes_submit_all_external_command_families_through_gateway()
    {
        foreach (CommandRouteCase commandCase in CommandRouteCases())
        {
            string statusId = UniqueIdHelper.GenerateSortableUniqueStringId();
            CapturingEventStoreGatewayClient gateway = new();
            gateway.EnqueueCommandResult(statusId);
            await using var factory = new TenantsApiWebApplicationFactory(gateway);
            using HttpClient client = CreateAuthenticatedClient(factory, "commands:*");

            HttpResponseMessage response = await SendJsonAsync(client, commandCase);

            response.StatusCode.ShouldBe(HttpStatusCode.Accepted, commandCase.Name);
            response.Headers.RetryAfter.ShouldNotBeNull().Delta.ShouldBe(TimeSpan.FromSeconds(1));
            response.Headers.Location.ShouldBeNull(commandCase.Name);
            SubmitCommandResponse? body = await response.Content.ReadFromJsonAsync<SubmitCommandResponse>(
                cancellationToken: TestContext.Current.CancellationToken);
            body.ShouldNotBeNull().CorrelationId.ShouldBe(statusId);

            SubmittedCommand command = gateway.SubmittedCommands.ShouldHaveSingleItem();
            command.Request.Tenant.ShouldBe(TenantIdentity.DefaultTenantId, commandCase.Name);
            command.Request.Domain.ShouldBe(commandCase.ExpectedDomain, commandCase.Name);
            command.Request.AggregateId.ShouldBe(commandCase.ExpectedAggregateId, commandCase.Name);
            command.Request.CommandType.ShouldBe(commandCase.ExpectedCommandType, commandCase.Name);
            command.Request.MessageId.ShouldNotBeNullOrWhiteSpace();
            _ = UniqueIdHelper.ExtractTimestamp(command.Request.MessageId);
            command.Request.Payload.GetProperty(commandCase.IdentityPropertyName).GetString().ShouldBe(commandCase.ExpectedIdentityValue);
            command.Request.Payload.GetRawText().ShouldBe(
                JsonSerializer.SerializeToElement(commandCase.Body, JsonOptions).GetRawText(),
                commandCase.Name);
            command.CancellationToken.CanBeCanceled.ShouldBeTrue(commandCase.Name);
        }
    }

    [Fact]
    public async Task Generated_command_route_rejects_null_body_before_gateway_call()
    {
        CapturingEventStoreGatewayClient gateway = new();
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory, "commands:*");
        using var content = new StringContent("null", Encoding.UTF8, "application/json");

        HttpResponseMessage response = await client.PostAsync(
            "/api/tenants/tenant.alpha/enable",
            content,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("detail").GetString().ShouldBe("Request body is required.");
        gateway.SubmittedCommands.ShouldBeEmpty();
    }

    [Fact]
    public async Task Generated_command_route_rejects_route_body_mismatch_before_gateway_call()
    {
        foreach (CommandMismatchCase mismatchCase in CommandMismatchCases())
        {
            CapturingEventStoreGatewayClient gateway = new();
            await using var factory = new TenantsApiWebApplicationFactory(gateway);
            using HttpClient client = CreateAuthenticatedClient(factory, "commands:*");

            using var request = new HttpRequestMessage(mismatchCase.Method, mismatchCase.Route)
            {
                Content = JsonContent.Create(mismatchCase.Body, options: JsonOptions),
            };

            HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken);

            response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, mismatchCase.Name);
            JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
            problem.GetProperty("detail").GetString().ShouldNotBeNull().ShouldContain("does not match");
            gateway.SubmittedCommands.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Generated_command_route_propagates_request_cancellation_to_gateway()
    {
        CapturingEventStoreGatewayClient gateway = new()
        {
            BlockCommandsUntilCancellation = true,
        };
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory, "commands:*");
        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(
            TestContext.Current.CancellationToken);

        Task<HttpResponseMessage> request = client.PostAsJsonAsync(
            "/api/tenants/tenant.alpha/enable",
            new EnableTenant("tenant.alpha"),
            JsonOptions,
            cancellation.Token);
        CancellationToken gatewayToken = await gateway.CommandStarted.Task.WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);
        cancellation.Cancel();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => request);
        gatewayToken.IsCancellationRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Generated_command_route_maps_gateway_failure_to_safe_problem_details()
    {
        CapturingEventStoreGatewayClient gateway = new();
        gateway.EnqueueCommandFailure(new EventStoreGatewayException(
            StatusCodes.Status403Forbidden,
            "Forbidden",
            detail: "Access denied.",
            correlationId: "01KTESTCORRELATION00000",
            tenantId: TenantIdentity.DefaultTenantId,
            reasonCode: "tenant-forbidden"));
        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient client = CreateAuthenticatedClient(factory, "commands:*");

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/tenants/tenant.alpha/enable",
            new EnableTenant("tenant.alpha"),
            JsonOptions,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        JsonElement problem = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        problem.GetProperty("title").GetString().ShouldBe("Forbidden");
        problem.GetProperty("correlationId").GetString().ShouldBe("01KTESTCORRELATION00000");
        problem.GetProperty("tenantId").GetString().ShouldBe(TenantIdentity.DefaultTenantId);
        problem.GetProperty("reasonCode").GetString().ShouldBe("tenant-forbidden");
        gateway.SubmittedCommands.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Cross-checks the real <see cref="TenantsRestQueryClient"/> against the real generated controllers:
    /// the client builds the URIs, the controllers answer them, and the client parses what they emit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Story 1.10's whole point is that the six UI reads go direct to these routes over this typed client.
    /// Every other test either drives the controller with a hand-written URL, or drives the client against a
    /// stub handler; the one live-stack read test substitutes an adapter that re-implements the pre-1.10
    /// SubmitQueryAsync path. Nothing checked the client's own route construction, or its header parsing,
    /// against the service that actually answers it -- so a renamed header or a changed route shape would
    /// break every read in a real deployment with the suite green.
    /// </para>
    /// <para>
    /// The client cannot be pointed straight at <c>factory.CreateClient()</c>: it builds request URIs with
    /// <c>UriCreationOptions.DangerousDisablePathAndQueryCanonicalization</c> so that dot-only tenant ids
    /// cannot be normalized into a different resource, and <c>Microsoft.AspNetCore.TestHost</c> throws
    /// <c>InvalidOperationException</c> on such a URI in <c>PathString.FromUriComponent</c>. Exercising the
    /// client in-process against the controllers therefore requires this three-stage cross-check rather than
    /// a single call; a real-socket harness would be needed to collapse it into one.
    /// </para>
    /// </remarks>
    [Fact]
    public async Task Direct_rest_client_routes_match_the_generated_controllers_and_parse_their_real_headers()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTimeOffset servedAt = DateTimeOffset.Parse("2026-07-29T10:00:00Z", CultureInfo.InvariantCulture);

        // Stage 1: let the production client build every route, and capture exactly what it would send.
        var recorder = new RouteRecordingHandler();
        using (var recordingClient = new HttpClient(recorder) { BaseAddress = new Uri("https://tenants.invalid") })
        {
            var probe = new TenantsRestQueryClient(recordingClient);
            _ = await probe.ListTenantsAsync(new ListTenantsQuery { Cursor = "opaque", PageSize = 25 }, null, cancellationToken);
            _ = await probe.GetTenantAsync(new GetTenantQuery { TenantId = "tenant.alpha" }, null, cancellationToken);
            _ = await probe.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 }, null, cancellationToken);
            _ = await probe.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = "user.alpha", PageSize = 12 }, null, cancellationToken);
            _ = await probe.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = "tenant.alpha", PageSize = 50 }, null, cancellationToken);
            _ = await probe.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 20 }, null, cancellationToken);
        }

        recorder.Paths.Count.ShouldBe(6);

        // The paths must carry their query parameters, not merely be routable. Asserting only the count and
        // the QueryType order let a regression that stopped appending query parameters yield six routable
        // paths in the same order while every paged read silently lost its cursor and page size.
        recorder.Paths[0].ShouldContain("pageSize=25");
        recorder.Paths[0].ShouldContain("cursor=opaque");
        recorder.Paths[2].ShouldContain("pageSize=20");
        recorder.Paths[3].ShouldContain("pageSize=12");
        recorder.Paths[4].ShouldContain("pageSize=50");
        recorder.Paths[5].ShouldContain("pageSize=20");

        // Stage 2: the generated controllers must answer those exact paths -- not hand-written equivalents.
        CapturingEventStoreGatewayClient gateway = new();
        // Lifecycle is set, not left at Unknown. The emitter writes X-Hexalith-Projection-Lifecycle only for
        // a non-Unknown value, so the whole malformed/contradiction/normalize block in TenantsRestQueryClient
        // was bypassed and no assertion below read Metadata.Lifecycle at all.
        QueryResponseMetadata Metadata(string version) => new(
            ETag: version,
            IsStale: false,
            ProjectionVersion: version,
            ServedAt: servedAt)
        {
            Provenance = QueryResponseProvenance.ProjectionBacked,
            Lifecycle = ProjectionLifecycleState.Current,
        };

        gateway.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)], null, false),
            eTag: "list-v1",
            Metadata("list-v1"));
        gateway.EnqueueQueryResult(
            new TenantDetail(
                "tenant.alpha",
                "Alpha",
                "Alpha description",
                TenantStatus.Active,
                [new TenantMember("user.alpha", TenantRole.TenantOwner)],
                new Dictionary<string, string>(StringComparer.Ordinal),
                servedAt),
            eTag: "detail-v1",
            Metadata("detail-v1"));
        gateway.EnqueueQueryResult(
            new PaginatedResult<TenantMember>([new TenantMember("user.alpha", TenantRole.TenantOwner)], null, false),
            eTag: "members-v1",
            Metadata("members-v1"));
        gateway.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>([], null, false),
            eTag: "user-tenants-v1",
            Metadata("user-tenants-v1"));
        gateway.EnqueueQueryResult(
            new PaginatedResult<TenantAuditEntry>([], null, false),
            eTag: "audit-v1",
            Metadata("audit-v1"));
        gateway.EnqueueQueryResult(
            new PaginatedResult<GlobalAdministratorSummary>([new GlobalAdministratorSummary("admin.alpha")], null, false),
            eTag: "admins-v1",
            Metadata("admins-v1"));

        await using var factory = new TenantsApiWebApplicationFactory(gateway);
        using HttpClient serviceClient = CreateAuthenticatedClient(factory);

        List<HttpResponseMessage> realResponses = [];
        foreach (string path in recorder.Paths)
        {
            HttpResponseMessage response = await serviceClient.GetAsync(path, cancellationToken);
            response.StatusCode.ShouldBe(HttpStatusCode.OK, $"the generated controllers must serve the route the client builds: {path}");
            realResponses.Add(response);
        }

        gateway.SubmittedQueries.Select(static query => query.Request.QueryType).ShouldBe(
        [
            ListTenantsQuery.QueryType,
            GetTenantQuery.QueryType,
            GetTenantUsersQuery.QueryType,
            GetUserTenantsQuery.QueryType,
            GetTenantAuditQuery.QueryType,
            GetGlobalAdministratorsQuery.QueryType,
        ]);

        // Stage 3: replay each REAL response -- real status, real X-Hexalith-* headers, real body -- through
        // the production client, so its metadata parsing is proven against what the service actually emits.
        async Task<TenantsRestQueryResponse<TPayload>> ReplayAsync<TPayload>(
            int index,
            Func<TenantsRestQueryClient, Task<TenantsRestQueryResponse<TPayload>>> read)
        {
            using var replay = new HttpClient(new ReplayHandler(realResponses[index]))
            {
                BaseAddress = new Uri("https://tenants.invalid"),
            };
            return await read(new TenantsRestQueryClient(replay));
        }

        TenantsRestQueryResponse<PaginatedResult<TenantSummary>> list = await ReplayAsync(
            0, client => client.ListTenantsAsync(new ListTenantsQuery { Cursor = "opaque", PageSize = 25 }, null, cancellationToken));
        list.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        list.Payload.ShouldNotBeNull().Items.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        list.Metadata.ShouldNotBeNull().ProjectionVersion.ShouldBe("list-v1");
        list.Metadata!.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        list.Metadata!.Lifecycle.ShouldBe(
            ProjectionLifecycleState.Current,
            "the client must parse the lifecycle header the real emitter writes");
        // GetStrongETag returns the unquoted strong tag content, so this is the controller's ETag as the
        // client stores it -- the same shape TenantsRestQueryClientTests pins against a stub handler.
        list.ETag.ShouldBe("list-v1");

        TenantsRestQueryResponse<TenantDetail> detail = await ReplayAsync(
            1, client => client.GetTenantAsync(new GetTenantQuery { TenantId = "tenant.alpha" }, null, cancellationToken));
        detail.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        detail.Payload.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        detail.Metadata.ShouldNotBeNull().ProjectionVersion.ShouldBe("detail-v1");

        TenantsRestQueryResponse<PaginatedResult<TenantMember>> members = await ReplayAsync(
            2, client => client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = "tenant.alpha", PageSize = 20 }, null, cancellationToken));
        members.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        members.Payload.ShouldNotBeNull().Items.ShouldHaveSingleItem().UserId.ShouldBe("user.alpha");
        members.Metadata.ShouldNotBeNull().ProjectionVersion.ShouldBe("members-v1");

        TenantsRestQueryResponse<PaginatedResult<UserTenantMembership>> userTenants = await ReplayAsync(
            3, client => client.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = "user.alpha", PageSize = 12 }, null, cancellationToken));
        userTenants.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        userTenants.Payload.ShouldNotBeNull().Items.ShouldBeEmpty();

        TenantsRestQueryResponse<PaginatedResult<TenantAuditEntry>> audit = await ReplayAsync(
            4, client => client.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = "tenant.alpha", PageSize = 50 }, null, cancellationToken));
        audit.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        audit.Payload.ShouldNotBeNull().Items.ShouldBeEmpty();

        TenantsRestQueryResponse<PaginatedResult<GlobalAdministratorSummary>> admins = await ReplayAsync(
            5, client => client.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 20 }, null, cancellationToken));
        admins.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        admins.Payload.ShouldNotBeNull().Items.ShouldHaveSingleItem().UserId.ShouldBe("admin.alpha");
        admins.Metadata.ShouldNotBeNull().ProjectionVersion.ShouldBe("admins-v1");

        // Stage 4: the conditional path, end to end. The controller's 304 behaviour and the client's 304
        // parsing were each covered separately -- against hand-written URLs and against a stub handler -- so
        // the one seam neither covered was the one that matters: IsSupportedNotModified needs an exact strong
        // ETag plus a projection version plus projection-backed provenance, all from the real emitter.
        gateway.EnqueueNotModified(
            "list-v1",
            new QueryResponseMetadata(
                ETag: "list-v1",
                IsNotModified: true,
                IsStale: false,
                ProjectionVersion: "list-v1",
                ServedAt: servedAt)
            {
                Provenance = QueryResponseProvenance.ProjectionBacked,
                Lifecycle = ProjectionLifecycleState.Current,
            });

        using var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, recorder.Paths[0]);
        conditionalRequest.Headers.IfNoneMatch.ParseAdd("\"list-v1\"");
        using HttpResponseMessage notModified = await serviceClient.SendAsync(conditionalRequest, cancellationToken);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);

        using (var replay = new HttpClient(new ReplayHandler(notModified))
        {
            BaseAddress = new Uri("https://tenants.invalid"),
        })
        {
            TenantsRestQueryResponse<PaginatedResult<TenantSummary>> conditional =
                await new TenantsRestQueryClient(replay).ListTenantsAsync(
                    new ListTenantsQuery { Cursor = "opaque", PageSize = 25 },
                    "list-v1",
                    cancellationToken);

            conditional.FailureKind.ShouldBe(
                TenantsRestQueryFailureKind.None,
                "a 304 from the real emitter must satisfy every clause of IsSupportedNotModified");
            conditional.Metadata.ShouldNotBeNull().IsNotModified.ShouldBe(true);
            conditional.Metadata!.ProjectionVersion.ShouldBe("list-v1");
            conditional.Metadata!.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
            conditional.Metadata!.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
            conditional.ETag.ShouldBe("list-v1");
        }

        foreach (HttpResponseMessage response in realResponses)
        {
            response.Dispose();
        }
    }

    /// <summary>Captures the paths the production client builds, without answering them meaningfully.</summary>
    private sealed class RouteRecordingHandler : HttpMessageHandler
    {
        public List<string> Paths { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Paths.Add(request.RequestUri!.PathAndQuery);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        }
    }

    /// <summary>Replays one captured real service response so the client parses genuine headers and body.</summary>
    private sealed class ReplayHandler(HttpResponseMessage captured) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            byte[] body = await captured.Content.ReadAsByteArrayAsync(cancellationToken);
            var replayed = new HttpResponseMessage(captured.StatusCode)
            {
                Content = new ByteArrayContent(body),
            };
            foreach (KeyValuePair<string, IEnumerable<string>> header in captured.Headers)
            {
                _ = replayed.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in captured.Content.Headers)
            {
                _ = replayed.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return replayed;
        }
    }

    private static HttpClient CreateAuthenticatedClient(
        TenantsApiWebApplicationFactory factory,
        string permission = "queries:*")
    {
        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", CreateJwt("test-user", permission));
        return client;
    }

    private static string CreateJwt(
        string subject,
        string permission,
        string issuer = JwtIssuer,
        string audience = JwtAudience)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSigningKey)),
            SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim("sub", subject),
                new Claim("permissions", JsonSerializer.Serialize(new[] { permission }), JsonClaimValueTypes.JsonArray),
                new Claim("tenants", JsonSerializer.Serialize(new[] { "system", "tenant.alpha" }), JsonClaimValueTypes.JsonArray),
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<HttpResponseMessage> SendJsonAsync(HttpClient client, CommandRouteCase commandCase)
    {
        using var request = new HttpRequestMessage(commandCase.Method, commandCase.Route)
        {
            Content = JsonContent.Create(commandCase.Body, options: JsonOptions),
        };

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static CommandRouteCase[] CommandRouteCases()
        =>
        [
            new(
                "create tenant",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha",
                new CreateTenant("tenant.alpha", "Alpha", "Tenant Alpha"),
                TenantIdentity.Domain,
                "tenant.alpha",
                CreateTenant.CommandType,
                "tenantId",
                "tenant.alpha"),
            new(
                "update tenant",
                HttpMethod.Put,
                "/api/tenants/tenant.alpha",
                new UpdateTenant("tenant.alpha", "Alpha Updated", "Updated tenant"),
                TenantIdentity.Domain,
                "tenant.alpha",
                UpdateTenant.CommandType,
                "tenantId",
                "tenant.alpha"),
            new(
                "enable tenant",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/enable",
                new EnableTenant("tenant.alpha"),
                TenantIdentity.Domain,
                "tenant.alpha",
                EnableTenant.CommandType,
                "tenantId",
                "tenant.alpha"),
            new(
                "disable tenant",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/disable",
                new DisableTenant("tenant.alpha"),
                TenantIdentity.Domain,
                "tenant.alpha",
                DisableTenant.CommandType,
                "tenantId",
                "tenant.alpha"),
            new(
                "add tenant user",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/users/user.alpha/add",
                new AddUserToTenant("tenant.alpha", "user.alpha", TenantRole.TenantReader),
                TenantIdentity.Domain,
                "tenant.alpha",
                AddUserToTenant.CommandType,
                "userId",
                "user.alpha"),
            new(
                "remove tenant user",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/users/user.alpha/remove",
                new RemoveUserFromTenant("tenant.alpha", "user.alpha"),
                TenantIdentity.Domain,
                "tenant.alpha",
                RemoveUserFromTenant.CommandType,
                "userId",
                "user.alpha"),
            new(
                "change tenant user role",
                HttpMethod.Patch,
                "/api/tenants/tenant.alpha/users/user.alpha/role",
                new ChangeUserRole("tenant.alpha", "user.alpha", TenantRole.TenantContributor),
                TenantIdentity.Domain,
                "tenant.alpha",
                ChangeUserRole.CommandType,
                "userId",
                "user.alpha"),
            new(
                "set tenant configuration",
                HttpMethod.Put,
                "/api/tenants/tenant.alpha/configuration/billing.plan",
                new SetTenantConfiguration("tenant.alpha", "billing.plan", "pro"),
                TenantIdentity.Domain,
                "tenant.alpha",
                SetTenantConfiguration.CommandType,
                "key",
                "billing.plan"),
            new(
                "remove tenant configuration",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/configuration/billing.plan/remove",
                new RemoveTenantConfiguration("tenant.alpha", "billing.plan"),
                TenantIdentity.Domain,
                "tenant.alpha",
                RemoveTenantConfiguration.CommandType,
                "key",
                "billing.plan"),
            new(
                "set global administrator",
                HttpMethod.Post,
                "/api/global-administrators/user.alpha/set",
                new SetGlobalAdministrator("user.alpha"),
                TenantIdentity.GlobalAdministratorsDomain,
                TenantIdentity.GlobalAdministratorsAggregateId,
                SetGlobalAdministrator.CommandType,
                "userId",
                "user.alpha"),
            new(
                "remove global administrator",
                HttpMethod.Post,
                "/api/global-administrators/user.alpha/remove",
                new RemoveGlobalAdministrator("user.alpha"),
                TenantIdentity.GlobalAdministratorsDomain,
                TenantIdentity.GlobalAdministratorsAggregateId,
                RemoveGlobalAdministrator.CommandType,
                "userId",
                "user.alpha"),
        ];

    private static CommandMismatchCase[] CommandMismatchCases()
        =>
        [
            new(
                "tenant id route mismatch",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/enable",
                new EnableTenant("tenant.beta")),
            new(
                "tenant-user tenant id route mismatch",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/users/user.alpha/add",
                new AddUserToTenant("tenant.beta", "user.alpha", TenantRole.TenantReader)),
            new(
                "tenant-user user id route mismatch",
                HttpMethod.Post,
                "/api/tenants/tenant.alpha/users/user.alpha/add",
                new AddUserToTenant("tenant.alpha", "user.beta", TenantRole.TenantReader)),
            new(
                "configuration tenant id route mismatch",
                HttpMethod.Put,
                "/api/tenants/tenant.alpha/configuration/billing.plan",
                new SetTenantConfiguration("tenant.beta", "billing.plan", "pro")),
            new(
                "configuration key route mismatch",
                HttpMethod.Put,
                "/api/tenants/tenant.alpha/configuration/billing.plan",
                new SetTenantConfiguration("tenant.alpha", "billing.mode", "pro")),
            new(
                "global administrator user id route mismatch",
                HttpMethod.Post,
                "/api/global-administrators/user.alpha/set",
                new SetGlobalAdministrator("user.beta")),
        ];

    private sealed class TenantsApiWebApplicationFactory(
        CapturingEventStoreGatewayClient gateway,
        IReadOnlyDictionary<string, string?>? authenticationConfiguration = null,
        Action<IServiceCollection>? configureServices = null)
        : WebApplicationFactory<TenantsApi::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            _ = builder.UseEnvironment("Development");
            _ = builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.Sources.Clear();
                configuration.AddInMemoryCollection(authenticationConfiguration is null
                    ? new Dictionary<string, string?>
                    {
                        ["EventStore:Authentication:Issuer"] = JwtIssuer,
                        ["EventStore:Authentication:Audience"] = JwtAudience,
                        ["EventStore:Authentication:SigningKey"] = JwtSigningKey,
                    }
                    : authenticationConfiguration);
            });
            _ = builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEventStoreGatewayClient>();
                services.AddSingleton<IEventStoreGatewayClient>(gateway);
                configureServices?.Invoke(services);
            });
        }
    }

    private sealed class CapturingEventStoreGatewayClient : IEventStoreGatewayClient
    {
        private readonly Queue<object> _responses = new();

        public List<SubmittedCommand> SubmittedCommands { get; } = [];

        public List<SubmittedQuery> SubmittedQueries { get; } = [];

        public bool BlockCommandsUntilCancellation { get; init; }

        public bool BlockQueriesUntilCancellation { get; init; }

        public TaskCompletionSource<CancellationToken> CommandStarted { get; }
            = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<CancellationToken> QueryStarted { get; }
            = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<SubmitCommandResponse> SubmitCommandAsync(
            SubmitCommandRequest request,
            CancellationToken cancellationToken = default)
        {
            SubmittedCommands.Add(new SubmittedCommand(request, cancellationToken));
            if (BlockCommandsUntilCancellation)
            {
                _ = CommandStarted.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The blocked command completed without cancellation.");
            }

            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return (SubmitCommandResponse)next;
        }

        public async Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQueries.Add(new SubmittedQuery(request, ifNoneMatch, cancellationToken));
            if (BlockQueriesUntilCancellation)
            {
                _ = QueryStarted.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                throw new InvalidOperationException("The blocked query completed without cancellation.");
            }

            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return (EventStoreQueryResult)next;
        }

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnqueueCommandResult(string correlationId)
            => _responses.Enqueue(new SubmitCommandResponse(correlationId));

        public void EnqueueCommandFailure(Exception exception)
            => _responses.Enqueue(exception);

        public void EnqueueQueryResult<T>(
            T payload,
            string? eTag = "etag",
            QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult(
                "correlation",
                JsonSerializer.SerializeToElement(payload, JsonOptions),
                IsNotModified: false,
                eTag)
            {
                Metadata = metadata ?? new QueryResponseMetadata(ETag: eTag, IsStale: false),
            });

        public void EnqueueNotModified(string? eTag, QueryResponseMetadata? metadata = null)
            => _responses.Enqueue(new EventStoreQueryResult(null, null, IsNotModified: true, eTag)
            {
                Metadata = metadata ?? new QueryResponseMetadata(ETag: eTag, IsNotModified: true),
            });

        public void EnqueueQueryFailure(Exception exception)
            => _responses.Enqueue(exception);
    }

    private sealed record SubmittedQuery(SubmitQueryRequest Request, string? IfNoneMatch, CancellationToken CancellationToken);

    private sealed record SubmittedCommand(SubmitCommandRequest Request, CancellationToken CancellationToken);

    private sealed record CommandRouteCase(
        string Name,
        HttpMethod Method,
        string Route,
        object Body,
        string ExpectedDomain,
        string ExpectedAggregateId,
        string ExpectedCommandType,
        string IdentityPropertyName,
        string ExpectedIdentityValue);

    private sealed record CommandMismatchCase(
        string Name,
        HttpMethod Method,
        string Route,
        object Body);
}
