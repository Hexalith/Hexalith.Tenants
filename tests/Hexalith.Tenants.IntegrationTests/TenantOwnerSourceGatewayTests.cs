using System.Diagnostics;
using Hexalith.EventStore.Client.Streams;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Authorization;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Preserves the owner-source wire contract through the production HTTP registration.</summary>
[Collection("BootstrapReadiness")]
public sealed class TenantOwnerSourceGatewayTests
{
    [Theory]
    [InlineData("tenants", "owner-source")]
    [InlineData("global-administrators", "global-administrators")]
    public async Task ProductionRegistration_PreservesBoundWorkloadRequestAndPerSendSidecarToken(string domain, string aggregate)
    {
        using RSA key = RSA.Create(2048);
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        string assertion = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(assertion));
        var terminal = new OwnerSourceCaptureHandler();
        string? previousToken = Environment.GetEnvironmentVariable("DAPR_API_TOKEN");
        string? previousEndpoint = Environment.GetEnvironmentVariable("DAPR_HTTP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", "http://localhost:3579");
            using var factory = new WebApplicationFactory<TenantOwnerSourceGateway>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Development");
                    builder.UseSetting("Tenants:BootstrapGlobalAdminEnabled", "false");
                    builder.UseSetting("Authentication:BoundStreamRead:PrivateKey", Convert.ToBase64String(key.ExportPkcs8PrivateKey()));
                    builder.ConfigureTestServices(services =>
                    {
                        services.RemoveAll<IWorkloadAssertionIssuer>();
                        services.AddSingleton(issuer);
                        services.AddHttpClient<TenantOwnerSourceGateway>()
                            .ConfigurePrimaryHttpMessageHandler(() => terminal);
                    });
                });
            TenantOwnerSourceGateway gateway = factory.Services.GetRequiredService<TenantOwnerSourceGateway>();
            string[]? previousProof = null;
            string?[] tokens = [Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), Convert.ToHexString(RandomNumberGenerator.GetBytes(16)), null];
            foreach (string? token in tokens)
            {
                Environment.SetEnvironmentVariable("DAPR_API_TOKEN", token);
                var request = new StreamReadRequest("system", domain, aggregate, FromSequence: 7, ToSequence: 12, PageSize: 2)
                {
                    RequireNativeProof = true,
                };
                StreamReadPage page = await gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken);
                page.AggregateId.ShouldBe(request.AggregateId);
                terminal.Method.ShouldBe(HttpMethod.Post);
                terminal.Uri.ShouldBe(new Uri("http://127.0.0.1:3579/api/v1/streams/read/workload"));
                terminal.Headers["dapr-app-id"].ShouldBe(["eventstore"]);
                if (token is null) terminal.Headers.ContainsKey("dapr-api-token").ShouldBeFalse();
                else terminal.Headers["dapr-api-token"].ShouldBe([token]);
                terminal.Headers[EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName].ShouldBe([assertion]);
                JsonSerializer.Deserialize<StreamReadRequest>(terminal.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldBe(request);
                string[] proof = terminal.Headers["X-Hexalith-Bound-Stream-Read-Proof"].Single().Split('.');
                key.VerifyData(Encoding.ASCII.GetBytes(proof[0]), Decode(proof[1]), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).ShouldBeTrue();
                using JsonDocument payload = JsonDocument.Parse(Decode(proof[0]));
                payload.RootElement.GetProperty("version").GetInt32().ShouldBe(1);
                payload.RootElement.GetProperty("caller").GetString().ShouldBe("tenants");
                payload.RootElement.GetProperty("tenant").GetString().ShouldBe("system");
                payload.RootElement.GetProperty("domain").GetString().ShouldBe(domain);
                payload.RootElement.GetProperty("aggregateId").GetString().ShouldBe(aggregate);
                (payload.RootElement.GetProperty("expiresAt").GetInt64() - payload.RootElement.GetProperty("issuedAt").GetInt64()).ShouldBe(30);
                if (previousProof is not null) proof[0].ShouldNotBe(previousProof[0]);
                previousProof = proof;
            }

            await issuer.Received(3).IssueAsync(Arg.Is<WorkloadAssertionRequest>(request =>
                request.Audience == "eventstore" && request.Operation == "eventstore:stream-read" && request.Bindings == null), Arg.Any<CancellationToken>());
        }
        finally
        {
            Environment.SetEnvironmentVariable("DAPR_API_TOKEN", previousToken);
            Environment.SetEnvironmentVariable("DAPR_HTTP_ENDPOINT", previousEndpoint);
        }
    }

    [Theory]
    [InlineData("system", "global-administrators", "another-aggregate")]
    [InlineData("another-tenant", "global-administrators", "global-administrators")]
    [InlineData("system", "another-domain", "global-administrators")]
    public async Task NeighboringAuthorityScopesAreRefusedBeforeAnyAssertionOrTransport(string tenant, string domain, string aggregate)
    {
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        using var http = new HttpClient(new OwnerSourceCaptureHandler());
        using RSA scopeKey = RSA.Create(2048);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(scopeKey.ExportPkcs8PrivateKey()) }).Build();
        var gateway = new TenantOwnerSourceGateway(http, issuer, configuration, TimeProvider.System);
        await Should.ThrowAsync<InvalidOperationException>(() => gateway.ReadStreamAsync(new(tenant, domain, aggregate)));
        await issuer.DidNotReceiveWithAnyArgs().IssueAsync(default!);
    }

    [Fact]
    public async Task RepresentativeNativeReplayIncludesEverySignedHttpWindowAndFinalProof()
    {
        var source = new MutableGlobalAdministratorSource("admin", "keeper");
        for (int i = 0; i < 4999; i++)
        {
            source.Append(new GlobalAdministratorSet("system", "cost"));
            source.Append(new GlobalAdministratorRemoved("system", "cost"));
        }
        using RSA scopeKey = RSA.Create(2048);
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>("controlled-workload-assertion"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(scopeKey.ExportPkcs8PrivateKey()) }).Build();
        var terminal = new OwnerSourceCaptureHandler(source.Snapshot(), scopeKey);
        using var http = new HttpClient(terminal) { BaseAddress = new Uri("http://localhost/") };
        var gateway = new TenantOwnerSourceGateway(http, issuer, configuration, TimeProvider.System);
        var reader = new AuthoritativeEventStreamReader(gateway, TimeProvider.System, TimeSpan.FromSeconds(10), requireNativeProof: true);
        var authority = new TenantsGlobalAdministratorAuthority(reader, source, TimeProvider.System);
        var timer = Stopwatch.StartNew();
        (await authority.IsCurrentGlobalAdministratorAsync("admin", TestContext.Current.CancellationToken)).ShouldBeTrue();
        terminal.ReadCount.ShouldBe(102);
        TestContext.Current.TestOutputHelper!.WriteLine($"10000 native events through {terminal.ReadCount} signed/verified in-process HTTP windows, copy/digest validation, fold and final proof: {timer.Elapsed.TotalMilliseconds:F2} ms. External workload-token issuance and Dapr/network excluded.");
    }

    private static byte[] Decode(string value)
    {
        string encoded = value.Replace('-', '+').Replace('_', '/');
        return Convert.FromBase64String(encoded.PadRight((encoded.Length + 3) / 4 * 4, '='));
    }
}
