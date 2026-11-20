using System.Net;
using System.Text.Json;

using Hexalith.Tenants.Client.Projections;
using Hexalith.Tenants.Contracts.Enums;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Registration;

public sealed class SampleSubscriptionHostTests
{
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Staging", HttpStatusCode.NotFound)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task TeachingRoutesExposeSeededProjectionOnlyInDevelopment(string environment, HttpStatusCode expected)
    {
        await using var factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment(environment));
        using HttpClient client = factory.CreateClient();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await factory.Services.GetRequiredService<ITenantProjectionStore>().SaveAsync(new TenantLocalState
        {
            TenantId = "sample-tenant",
            Status = TenantStatus.Active,
            Members = { ["reader"] = TenantRole.TenantReader },
            Configuration = { ["sample.theme"] = "blue", ["other.plan"] = "hidden" },
        }, cancellationToken);

        using HttpResponseMessage access = await client.GetAsync("/access/sample-tenant/reader", cancellationToken);
        using HttpResponseMessage configuration = await client.GetAsync("/configuration/sample-tenant/sample", cancellationToken);
        access.StatusCode.ShouldBe(expected);
        configuration.StatusCode.ShouldBe(expected);
        if (expected == HttpStatusCode.OK)
        {
            using JsonDocument decision = JsonDocument.Parse(await access.Content.ReadAsStringAsync(cancellationToken));
            decision.RootElement.GetProperty("access").GetString().ShouldBe("granted");
            using JsonDocument settings = JsonDocument.Parse(await configuration.Content.ReadAsStringAsync(cancellationToken));
            settings.RootElement.GetProperty("configuration").EnumerateObject().Single().Value.GetString().ShouldBe("blue");
        }

        using HttpResponseMessage health = await client.GetAsync("/alive", cancellationToken);
        health.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SubscriptionDiscoveryRequiresTheConfiguredSidecarChannelToken()
    {
        const string channelToken = "sample-test-channel-token";
        await using var factory = new WebApplicationFactory<global::Program>()
            .WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["APP_API_TOKEN"] = channelToken,
                })));
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage missing = await client.GetAsync("/dapr/subscribe", TestContext.Current.CancellationToken);
        missing.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var wrongRequest = new HttpRequestMessage(HttpMethod.Get, "/dapr/subscribe");
        wrongRequest.Headers.TryAddWithoutValidation("dapr-api-token", "wrong-token").ShouldBeTrue();
        using HttpResponseMessage wrong = await client.SendAsync(wrongRequest, TestContext.Current.CancellationToken);
        wrong.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/dapr/subscribe");
        validRequest.Headers.TryAddWithoutValidation("dapr-api-token", channelToken).ShouldBeTrue();
        using HttpResponseMessage valid = await client.SendAsync(validRequest, TestContext.Current.CancellationToken);
        valid.StatusCode.ShouldBe(HttpStatusCode.OK);
        using JsonDocument subscriptions = JsonDocument.Parse(
            await valid.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        subscriptions.RootElement.ValueKind.ShouldBe(JsonValueKind.Array);
        subscriptions.RootElement.EnumerateArray().Any(subscription =>
            subscription.GetProperty("pubsubName").GetString() == "pubsub"
            && subscription.GetProperty("topic").GetString() == "tenants.events"
            && subscription.GetProperty("route").GetString()?.TrimStart('/') == "tenants/events").ShouldBeTrue();

        using var eventRequest = new HttpRequestMessage(HttpMethod.Post, "/tenants/events")
        {
            Content = new StringContent(
                "{\"specversion\":\"1.0\",\"type\":\"hexalith.test\",\"source\":\"test\",\"id\":\"test-event\",\"data\":{}}",
                System.Text.Encoding.UTF8,
                "application/cloudevents+json"),
        };
        using HttpResponseMessage eventWithoutChannel = await client.SendAsync(
            eventRequest,
            TestContext.Current.CancellationToken);
        eventWithoutChannel.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
