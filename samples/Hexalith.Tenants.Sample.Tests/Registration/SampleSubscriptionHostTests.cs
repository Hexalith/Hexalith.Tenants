using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests.Registration;

public sealed class SampleSubscriptionHostTests
{
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
