using System.Net;

using Dapr;
using Dapr.Client;

using Grpc.Core;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Exercises exact physical self invocation and fail-closed, credential-free read-only readiness.</summary>
public sealed class TenantBootstrapAppReadinessTests
{
    /// <summary>Uses the configured SDK client and retries only transient read-only health observations.</summary>
    [Fact]
    public async Task PhysicalSelfHealthUsesGetWithoutAdministratorAuthorityOrBody()
    {
        var services = new ServiceCollection();
        BootstrapAppReadinessFixture.AddSidecar(services);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr(endpoint: "http://127.0.0.1:43123");
        int probes = 0;
        using var probe = new TenantBootstrapAppReadiness(new BootstrapCommandHttpMessageHandler((request, _) =>
        {
            request.Method.ShouldBe(HttpMethod.Get);
            request.RequestUri!.AbsoluteUri.ShouldBe("http://127.0.0.1:43123/v1.0/invoke/tenants-physical-17/method/alive");
            request.Content.ShouldBeNull();
            request.Headers.Authorization.ShouldBeNull();
            request.Headers.Contains("X-Hexalith-Scope").ShouldBeFalse();
            request.Headers.Contains(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeFalse();
            request.Headers.Contains(BoundWorkloadCommandProof.HeaderName).ShouldBeFalse();
            int current = Interlocked.Increment(ref probes);
            if (current == 1) throw new HttpRequestException(HttpRequestError.ConnectionError, "Synthetic sidecar connection refusal.");
            return Task.FromResult(new HttpResponseMessage(current switch
            {
                2 => HttpStatusCode.InternalServerError,
                3 => HttpStatusCode.ServiceUnavailable,
                _ => HttpStatusCode.NoContent,
            }));
        }));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(5));

        await probe.WaitAsync(provider, dapr, deadline.Token);

        probes.ShouldBe(4);
        await dapr.Received(1).GetMetadataAsync(Arg.Any<CancellationToken>());
    }

    /// <summary>Healthy headers suffice without reading or buffering an arbitrary health response body.</summary>
    [Fact]
    public async Task HealthProbeDoesNotConsumeResponseBodies()
    {
        var services = new ServiceCollection();
        BootstrapAppReadinessFixture.AddSidecar(services);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr();
        using var body = new BootstrapTokenResponseStream([], blockReads: true);
        using var probe = new TenantBootstrapAppReadiness(new BootstrapCommandHttpMessageHandler((_, _) => Task.FromResult(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = new StreamContent(body) })));
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(2));

        await probe.WaitAsync(provider, dapr, deadline.Token);

        body.BytesRead.ShouldBe(0);
        body.IsDisposed.ShouldBeTrue();
    }

    /// <summary>Missing, malformed and foreign physical identities cannot send even a health probe.</summary>
    [Theory]
    [InlineData("missing-options")]
    [InlineData("blank")]
    [InlineData("path")]
    [InlineData("invalid-version")]
    [InlineData("foreign")]
    [InlineData("blank-metadata")]
    public async Task InvalidOrForeignPhysicalIdentityRefusesBeforeProbing(string scenario)
    {
        var services = new ServiceCollection();
        if (scenario != "missing-options")
            services.AddSingleton<IOptions<DomainProjectionIdentityOptions>>(Options.Create(new DomainProjectionIdentityOptions
            {
                AppId = scenario == "blank" ? " " : scenario == "path" ? "tenants/../other" : BootstrapAppReadinessFixture.AppId,
                ServiceVersion = scenario == "invalid-version" ? "" : "v1",
            }));
        await using ServiceProvider provider = services.BuildServiceProvider();
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr(scenario == "foreign" ? "another-physical-app" : scenario == "blank-metadata" ? "" : BootstrapAppReadinessFixture.AppId);
        int probes = 0;
        using var probe = new TenantBootstrapAppReadiness(new BootstrapCommandHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref probes);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }));

        await Should.ThrowAsync<Exception>(() => probe.WaitAsync(provider, dapr, TestContext.Current.CancellationToken));

        probes.ShouldBe(0);
        dapr.DidNotReceive().CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>());
    }

    /// <summary>SDK metadata connection unavailability is retried, while authentication denial is terminal.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MetadataRetriesOnlyExplicitTransportUnavailability(bool denied)
    {
        var services = new ServiceCollection();
        BootstrapAppReadinessFixture.AddSidecar(services);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr();
        int observations = 0;
        dapr.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            if (Interlocked.Increment(ref observations) == 1)
                throw new DaprException("Synthetic metadata transport outcome.", new RpcException(new Status(denied ? StatusCode.PermissionDenied : StatusCode.Unavailable, "Synthetic status.")));
            return Task.FromResult(new DaprMetadata(BootstrapAppReadinessFixture.AppId, [], new Dictionary<string, string>(), []));
        });
        using TenantBootstrapAppReadiness probe = BootstrapAppReadinessFixture.CreateProbe();
        if (denied)
        {
            await Should.ThrowAsync<DaprException>(() => probe.WaitAsync(provider, dapr, TestContext.Current.CancellationToken));
            observations.ShouldBe(1);
            dapr.DidNotReceive().CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>());
        }
        else
        {
            await probe.WaitAsync(provider, dapr, TestContext.Current.CancellationToken);
            observations.ShouldBe(2);
        }
    }

    /// <summary>Responses outside the documented app-unhealthy and unavailable statuses cannot be retried into success.</summary>
    [Theory]
    [InlineData(301)]
    [InlineData(302)]
    [InlineData(303)]
    [InlineData(307)]
    [InlineData(308)]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(408)]
    [InlineData(429)]
    [InlineData(502)]
    [InlineData(504)]
    public async Task PermanentHealthResponsesRefuseWithoutRetry(int status)
    {
        var services = new ServiceCollection();
        BootstrapAppReadinessFixture.AddSidecar(services);
        await using ServiceProvider provider = services.BuildServiceProvider();
        using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr();
        int probes = 0;
        using var probe = new TenantBootstrapAppReadiness(new BootstrapCommandHttpMessageHandler((_, _) =>
        {
            Interlocked.Increment(ref probes);
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status));
        }));

        HttpRequestException failure = await Should.ThrowAsync<HttpRequestException>(() => probe.WaitAsync(provider, dapr, TestContext.Current.CancellationToken));

        failure.StatusCode.ShouldBe((HttpStatusCode)status);
        probes.ShouldBe(1);
    }

    /// <summary>Actual private probe transports reject every redirect without contacting same- or cross-origin destinations.</summary>
    [Theory]
    [InlineData(301, false)]
    [InlineData(302, false)]
    [InlineData(303, false)]
    [InlineData(307, false)]
    [InlineData(308, false)]
    [InlineData(301, true)]
    [InlineData(302, true)]
    [InlineData(303, true)]
    [InlineData(307, true)]
    [InlineData(308, true)]
    public async Task PrivateProbeNeverFollowsActualLoopbackRedirects(int status, bool crossOrigin)
    {
        int destinations = 0;
        int origins = 0;
        var destinationBuilder = WebApplication.CreateBuilder();
        destinationBuilder.Logging.ClearProviders();
        destinationBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication destination = destinationBuilder.Build();
        destination.Run(context =>
        {
            Interlocked.Increment(ref destinations);
            context.Response.StatusCode = 200;
            return Task.CompletedTask;
        });
        await destination.StartAsync(TestContext.Current.CancellationToken);
        string destinationAddress = destination.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        string? location = null;
        var originBuilder = WebApplication.CreateBuilder();
        originBuilder.Logging.ClearProviders();
        originBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication origin = originBuilder.Build();
        origin.Run(context =>
        {
            if (context.Request.Path == "/destination") Interlocked.Increment(ref destinations);
            else
            {
                Interlocked.Increment(ref origins);
                context.Request.Method.ShouldBe("GET");
                context.Request.ContentLength.GetValueOrDefault().ShouldBe(0L);
                context.Request.Headers.Authorization.Count.ShouldBe(0);
                context.Response.StatusCode = status;
                context.Response.Headers.Location = location;
            }
            return Task.CompletedTask;
        });
        await origin.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            string address = origin.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            location = (crossOrigin ? destinationAddress : address) + "/destination";
            var services = new ServiceCollection();
            services.AddLogging();
            BootstrapAppReadinessFixture.AddSidecar(services);
            services.AddTenantBootstrap();
            await using ServiceProvider provider = services.BuildServiceProvider();
            using DaprClient dapr = BootstrapAppReadinessFixture.CreateDapr(endpoint: address);
            using var probe = new TenantBootstrapAppReadiness();

            await Should.ThrowAsync<HttpRequestException>(() => probe.WaitAsync(provider, dapr, TestContext.Current.CancellationToken));

            origins.ShouldBe(1);
            destinations.ShouldBe(0);
        }
        finally
        {
            await origin.StopAsync(CancellationToken.None);
            await destination.StopAsync(CancellationToken.None);
        }
    }
}
