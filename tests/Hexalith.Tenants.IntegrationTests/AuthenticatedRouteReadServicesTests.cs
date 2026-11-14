using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Exercises the live fixture's actual production bearer relay without a Dapr backend.</summary>
public sealed class AuthenticatedRouteReadServicesTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("sub")]
    [InlineData("tenant_id")]
    [InlineData(FrontComposerTokenSession.ClaimType)]
    public async Task LiveReadServicesRelayOnlyTheOriginalAuthenticatedSession(string? changedClaim)
    {
        var observed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication endpoint = builder.Build();
        endpoint.Run(context =>
        {
            observed.TrySetResult(context.Request.Headers.Authorization.ToString());
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        });
        await endpoint.StartAsync(TestContext.Current.CancellationToken);

        string address = endpoint.Services.GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
        using ServiceProvider services = AuthenticatedTenantsRouteTests.CreateProductionReadServices(
            new Uri(address), "live-read-session-token");
        if (changedClaim is not null)
        {
            ClaimsIdentity identity = services.GetRequiredService<IHttpContextAccessor>().HttpContext!
                .User.Identity.ShouldBeOfType<ClaimsIdentity>();
            identity.RemoveClaim(identity.FindFirst(changedClaim).ShouldNotBeNull());
            identity.AddClaim(new Claim(changedClaim, "another-context"));
        }

        using IServiceScope scope = services.CreateScope();
        _ = await scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>()
            .ListTenantsAsync(new ListTenantsQuery { PageSize = 20 }, null, TestContext.Current.CancellationToken);

        (await observed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken))
            .ShouldBe(changedClaim is null ? "Bearer live-read-session-token" : string.Empty);
    }
}
