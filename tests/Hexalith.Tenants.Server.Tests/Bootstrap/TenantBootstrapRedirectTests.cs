using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

using Hexalith.Tenants.Bootstrap;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Characterizes existing credential redirects with real loopback HTTP transports.</summary>
public sealed class TenantBootstrapRedirectTests
{
    [Theory]
    [InlineData(301, false, false)]
    [InlineData(302, false, false)]
    [InlineData(303, false, false)]
    [InlineData(307, false, false)]
    [InlineData(308, false, false)]
    [InlineData(301, true, false)]
    [InlineData(302, true, false)]
    [InlineData(303, true, false)]
    [InlineData(307, true, false)]
    [InlineData(308, true, false)]
    [InlineData(301, false, true)]
    [InlineData(302, false, true)]
    [InlineData(303, false, true)]
    [InlineData(307, false, true)]
    [InlineData(308, false, true)]
    [InlineData(301, true, true)]
    [InlineData(302, true, true)]
    [InlineData(303, true, true)]
    [InlineData(307, true, true)]
    [InlineData(308, true, true)]
    [InlineData(200, false, true)]
    public async Task RealHandlerUsesExactCredentialDestinationMethodAndSyntheticBody(int status, bool crossOrigin, bool rejectRedirects)
    {
        const string administrator = "loopback-administrator";
        const string syntheticForm = "grant_type=password&client_id=loopback-client&username=loopback-user&password=loopback-password";
        string token = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>(StringComparer.Ordinal) { ["sub"] = administrator },
        });
        var requests = new ConcurrentQueue<(string Destination, string Method, string Body)>();
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication destination = builder.Build();
        destination.Run(async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            requests.Enqueue(($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}", context.Request.Method,
                await reader.ReadToEndAsync(context.RequestAborted)));
            await context.Response.WriteAsJsonAsync(new { access_token = token }, context.RequestAborted);
        });
        await destination.StartAsync();
        string destinationAddress = destination.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();

        var originBuilder = WebApplication.CreateBuilder();
        originBuilder.Logging.ClearProviders();
        originBuilder.WebHost.UseUrls("http://127.0.0.1:0");
        await using WebApplication origin = originBuilder.Build();
        string? redirect = null;
        origin.Run(async context =>
        {
            using var reader = new StreamReader(context.Request.Body);
            requests.Enqueue(($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}", context.Request.Method,
                await reader.ReadToEndAsync(context.RequestAborted)));
            if (context.Request.Path == "/token" && status != 200)
            {
                context.Response.StatusCode = status;
                context.Response.Headers.Location = redirect;
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new { access_token = token }, context.RequestAborted);
            }
        });
        await origin.StartAsync();
        string originAddress = origin.Services.GetRequiredService<IServer>().Features
            .Get<IServerAddressesFeature>()!.Addresses.Single();
        redirect = (crossOrigin ? destinationAddress : originAddress) + "/destination";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventStore:Authentication:Authority"] = originAddress,
            ["EventStore:Authentication:TokenEndpoint"] = originAddress + "/token",
            ["EventStore:Authentication:ClientId"] = "loopback-client",
            ["EventStore:Authentication:Username"] = "loopback-user",
            ["EventStore:Authentication:Password"] = "loopback-password",
        }).Build();
        IHostEnvironment environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Development);
        var provider = new TenantBootstrapCredentialProvider(configuration, environment, TimeProvider.System, NullLogger.Instance);
        // Compare the production credential registration with the unchanged default-client policy.
        // No command is ever constructed or forwarded.
        var services = new ServiceCollection();
        services.AddTenantBootstrap();
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();
        using HttpClient client = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(
            rejectRedirects ? TenantBootstrapServiceCollectionExtensions.CredentialClientName : string.Empty);
        client.Timeout = TimeSpan.FromSeconds(5);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        string? result = await provider.AcquireAsync(administrator, client, deadline.Token);
        bool refused = rejectRedirects && status != 200;
        result.ShouldBe(refused ? null : token);

        requests.Count.ShouldBe(refused || status == 200 ? 1 : 2);
        var observed = requests.ToArray();
        observed[0].ShouldBe((originAddress + "/token", "POST", syntheticForm));
        if (!refused && status != 200)
        {
            observed[1].ShouldBe((redirect, status is 307 or 308 ? "POST" : "GET", status is 307 or 308 ? syntheticForm : string.Empty));
        }
    }
}
