using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantsQueryApiClientTests
{
    [Fact]
    public async Task SendAsync_sends_get_with_if_none_match_and_reads_payload_etag_and_freshness_metadata()
    {
        var handler = new CapturingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new PaginatedResult<TenantSummary>(
                    [new TenantSummary("tenant.alpha", "Tenant Alpha", TenantStatus.Active)],
                    null,
                    false))),
            };
            response.Headers.ETag = new EntityTagHeaderValue("\"index-etag-2\"");
            response.Headers.Add("X-Hexalith-Projection-Version", "index-etag-2");
            response.Headers.Add("X-Hexalith-Served-At", "2026-06-07T08:00:00.0000000+00:00");
            response.Headers.Add("X-Hexalith-Is-Stale", "true");
            return response;
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await client.SendAsync<PaginatedResult<TenantSummary>>(
            new TenantsQueryApiRequest("/api/tenants?cursor=opaque&pageSize=10", "list-tenants"),
            "\"index-etag-1\"");

        handler.Requests.ShouldHaveSingleItem().RequestUri!.PathAndQuery.ShouldBe("/api/tenants?cursor=opaque&pageSize=10");
        handler.Requests[0].Method.ShouldBe(HttpMethod.Get);
        handler.Requests[0].Headers.IfNoneMatch.ShouldHaveSingleItem().Tag.ShouldBe("\"index-etag-1\"");
        result.IsNotModified.ShouldBeFalse();
        result.ETag.ShouldBe("\"index-etag-2\"");
        result.Metadata.ShouldNotBeNull().ProjectionVersion.ShouldBe("index-etag-2");
        result.Metadata.IsStale.ShouldBe(true);
        result.Metadata.ServedAt.ShouldBe(DateTimeOffset.Parse("2026-06-07T08:00:00.0000000+00:00", System.Globalization.CultureInfo.InvariantCulture));
        result.Payload.ShouldNotBeNull().Items.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
    }

    [Fact]
    public async Task SendAsync_not_modified_returns_etag_metadata_without_reading_a_body()
    {
        var handler = new CapturingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.NotModified);
            response.Headers.ETag = new EntityTagHeaderValue("\"index-etag-1\"");
            response.Headers.Add("X-Hexalith-Projection-Version", "index-etag-1");
            return response;
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreQueryResult<PaginatedResult<TenantSummary>> result = await client.SendAsync<PaginatedResult<TenantSummary>>(
            new TenantsQueryApiRequest("/api/tenants", "list-tenants"),
            "\"index-etag-1\"");

        result.IsNotModified.ShouldBeTrue();
        result.Payload.ShouldBeNull();
        result.ETag.ShouldBe("\"index-etag-1\"");
        result.Metadata.ShouldNotBeNull().IsNotModified.ShouldBe(true);
        result.Metadata.ProjectionVersion.ShouldBe("index-etag-1");
    }

    [Fact]
    public async Task SendAsync_non_success_throws_gateway_exception_without_requiring_raw_payload_exposure()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("""
                {
                  "title": "Forbidden",
                  "detail": "raw payload bearer-token stack trace correlation-123",
                  "reasonCode": "insufficient-permission"
                }
                """),
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreGatewayException ex = await Should.ThrowAsync<EventStoreGatewayException>(() => client.SendAsync<TenantDetail>(
            new TenantsQueryApiRequest("/api/tenants/tenant.alpha", "get-tenant")));

        ex.StatusCode.ShouldBe((int)HttpStatusCode.Forbidden);
        ex.ReasonCode.ShouldBe("insufficient-permission");
    }

    [Theory]
    [InlineData("W/\"index-etag-1\"")]
    [InlineData("*")]
    [InlineData("\"index-etag-1\", \"index-etag-2\"")]
    [InlineData("index etag 1")]
    public async Task SendAsync_omits_if_none_match_for_unsupported_cached_tags(string ifNoneMatch)
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new TenantDetail(
                "tenant.alpha",
                "Tenant Alpha",
                null,
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.Parse("2026-06-19T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture)))),
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreQueryResult<TenantDetail> result = await client.SendAsync<TenantDetail>(
            new TenantsQueryApiRequest("/api/tenants/tenant.alpha", "get-tenant"),
            ifNoneMatch);

        result.Payload.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        handler.Requests.ShouldHaveSingleItem().Headers.IfNoneMatch.ShouldBeEmpty();
    }

    [Fact]
    public async Task SendAsync_preserves_escaped_strong_if_none_match()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.NotModified)
        {
            Headers =
            {
                ETag = new EntityTagHeaderValue("\"tenant-\\\"etag\""),
            },
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreQueryResult<TenantDetail> result = await client.SendAsync<TenantDetail>(
            new TenantsQueryApiRequest("/api/tenants/tenant.alpha", "get-tenant"),
            "\"tenant-\\\"etag\"");

        result.IsNotModified.ShouldBeTrue();
        handler.Requests.ShouldHaveSingleItem().Headers.IfNoneMatch.ShouldHaveSingleItem().Tag.ShouldBe("\"tenant-\\\"etag\"");
    }

    [Fact]
    public async Task SendAsync_success_without_etag_returns_unmarked_metadata()
    {
        var handler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new TenantDetail(
                "tenant.alpha",
                "Tenant Alpha",
                null,
                TenantStatus.Active,
                [],
                new Dictionary<string, string>(),
                DateTimeOffset.Parse("2026-06-19T08:00:00Z", System.Globalization.CultureInfo.InvariantCulture)))),
        });
        var client = new TenantsQueryApiClient(new HttpClient(handler)
        {
            BaseAddress = new Uri("https://tenants.example/"),
        });

        EventStoreQueryResult<TenantDetail> result = await client.SendAsync<TenantDetail>(
            new TenantsQueryApiRequest("/api/tenants/tenant.alpha", "get-tenant"));

        result.ETag.ShouldBeNull();
        result.Metadata.ShouldNotBeNull().ETag.ShouldBeNull();
        result.Metadata.ProjectionVersion.ShouldBeNull();
        result.Metadata.ServedAt.ShouldBeNull();
    }

    private sealed class CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responder(request));
        }
    }
}
