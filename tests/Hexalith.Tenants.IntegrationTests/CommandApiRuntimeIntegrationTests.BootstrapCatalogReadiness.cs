using Dapr.Client;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Bootstrap;
using Hexalith.Tenants.IntegrationTests.Fixtures;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises private self-health alongside the actual SDK factory filter and committed owner authority.</summary>
public partial class CommandApiRuntimeIntegrationTests
{
    /// <summary>Health reaches the configured sidecar while the unchanged command factory refuses unlisted invocations.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BoundBootstrapSelfHealthPreservesCommittedFactoryRouting(bool bootstrapFirst)
    {
        const string appId = "tenants-physical-17";
        const string apiToken = "synthetic-sidecar-api-token";
        int healthRequests = 0;
        int unexpectedRequests = 0;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        WebApplicationBuilder sidecarBuilder = WebApplication.CreateBuilder();
        sidecarBuilder.Logging.ClearProviders();
        sidecarBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication sidecar = sidecarBuilder.Build();
        sidecar.Run(async context =>
        {
            if (context.Request.Method != "GET" || context.Request.Path != $"/v1.0/invoke/{appId}/method/alive")
            {
                Interlocked.Increment(ref unexpectedRequests);
                context.Response.StatusCode = 500;
                return;
            }
            context.Request.ContentLength.GetValueOrDefault().ShouldBe(0L);
            context.Request.Headers.Authorization.Count.ShouldBe(0);
            context.Request.Headers["dapr-api-token"].ToString().ShouldBe(apiToken);
            context.Request.Headers.ContainsKey(BoundWorkloadCommandProof.HeaderName).ShouldBeFalse();
            context.Request.Headers.ContainsKey(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldBeFalse();
            context.Request.Headers.ContainsKey(RouteCatalogInvocationHandler.RootHeader).ShouldBeFalse();
            if (Interlocked.Increment(ref healthRequests) == 1)
            {
                context.Response.StatusCode = 503;
                return;
            }
            entered.TrySetResult();
            await release.Task.WaitAsync(context.RequestAborted);
            context.Response.StatusCode = 204;
        });
        await sidecar.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            string address = sidecar.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
            {
                DisableDefaults = true,
                EnvironmentName = Environments.Development,
            });
            builder.Logging.ClearProviders();
            if (bootstrapFirst) builder.Services.AddTenantBootstrap();
            builder.AddServiceDefaults();
            builder.Services.AddEventStoreRouteCatalog(builder.Configuration);
            if (!bootstrapFirst) builder.Services.AddTenantBootstrap();
            // This is the same real, durably committed SDK authority used by generated HTTP tests.
            builder.Services.AddSingleton(TenantOwnerCatalogTestAuthority.Create(TenantOwnerCatalogTestAuthority.CreateSnapshot(appId)));
            builder.Services.AddSingleton<IOptions<DomainProjectionIdentityOptions>>(Options.Create(new DomainProjectionIdentityOptions { AppId = appId }));
            await using ServiceProvider provider = builder.Services.BuildServiceProvider();
            using DaprClient concrete = new DaprClientBuilder().UseHttpEndpoint(address).UseDaprApiToken(apiToken).Build();
            using DaprClient dapr = Substitute.For<DaprClient>();
            dapr.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DaprMetadata(appId, [], new Dictionary<string, string>(), [])));
            dapr.CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
                concrete.CreateInvokeMethodRequest(call.ArgAt<HttpMethod>(0), call.ArgAt<string>(1), call.ArgAt<string>(2)));
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(10));
            using HttpClient commands = provider.GetRequiredService<IHttpClientFactory>().CreateClient(TenantBootstrapServiceCollectionExtensions.CommandClientName);
            using HttpRequestMessage factoryProbe = dapr.CreateInvokeMethodRequest(HttpMethod.Get, appId, "alive");

            RouteCatalogUnavailableException factoryRefusal = await Should.ThrowAsync<RouteCatalogUnavailableException>(() => commands.SendAsync(factoryProbe, deadline.Token));
            factoryRefusal.Message.ShouldContain("outside the committed catalog");
            healthRequests.ShouldBe(0);

            Task readiness = provider.GetRequiredService<TenantBootstrapAppReadiness>().WaitAsync(provider, dapr, deadline.Token);
            await entered.Task.WaitAsync(deadline.Token);
            readiness.IsCompleted.ShouldBeFalse();
            healthRequests.ShouldBe(2);
            unexpectedRequests.ShouldBe(0);
            release.TrySetResult();
            await readiness;

            using HttpRequestMessage forbidden = dapr.CreateInvokeMethodRequest(HttpMethod.Post, appId, "unlisted-bootstrap-command");
            forbidden.Content = new StringContent("synthetic-command");
            RouteCatalogUnavailableException commandRefusal = await Should.ThrowAsync<RouteCatalogUnavailableException>(() => commands.SendAsync(forbidden, deadline.Token));
            commandRefusal.Message.ShouldContain("outside the committed catalog");
            unexpectedRequests.ShouldBe(0);
            healthRequests.ShouldBe(2);
            await dapr.Received(1).GetMetadataAsync(Arg.Any<CancellationToken>());
            (await provider.GetRequiredService<IRouteCatalogAuthority>().ReadbackAsync(deadline.Token)).Serving.ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
            await sidecar.StopAsync(CancellationToken.None);
        }
    }
}
