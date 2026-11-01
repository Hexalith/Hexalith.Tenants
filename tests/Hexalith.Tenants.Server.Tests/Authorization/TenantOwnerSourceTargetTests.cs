using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.Tenants.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

/// <summary>Exercises the production SDK invocation chain without changing logical workload or proof scope.</summary>
public sealed class TenantOwnerSourceTargetTests
{
    [Theory]
    [InlineData(null, "eventstore", null)]
    [InlineData("eventstore-isolated", "eventstore-isolated", "tenants-isolated")]
    [InlineData("", null, null)]
    [InlineData(" ", null, null)]
    public async Task ActualSourceTransportUsesOnlyTheSelectedPhysicalGateway(string? selected, string? expected, string? caller)
    {
        using RSA rsa = RSA.Create(2048);
        IConfiguration config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Tenants:EventStoreAppId"] = selected,
            ["Authentication:WorkloadIssuer:Workload"] = caller,
            ["Authentication:BoundStreamRead:PrivateKey"] = Convert.ToBase64String(rsa.ExportPkcs8PrivateKey()),
        }).Build();
        var issuer = Substitute.For<IWorkloadAssertionIssuer>();
        issuer.IssueAsync(Arg.Any<WorkloadAssertionRequest>(), Arg.Any<CancellationToken>()).Returns("owner-assertion");
        var services = new ServiceCollection(); services.AddLogging(); services.AddSingleton(config);
        services.AddSingleton(issuer); services.AddSingleton(TimeProvider.System);
        using var handler = new OwnerSourceTargetHttpHandler(expected, caller ?? "tenants", Convert.ToBase64String(rsa.ExportSubjectPublicKeyInfo()));
        services.AddTenantOwnerSourceGateway().ConfigurePrimaryHttpMessageHandler(() => handler);
        await using var provider = services.BuildServiceProvider();
        var gateway = provider.GetRequiredService<TenantOwnerSourceGateway>();
        var request = new StreamReadRequest("system", "global-administrators", "global-administrators");
        if (expected is null)
        {
            await Should.ThrowAsync<ArgumentException>(() => gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken));
            handler.Calls.ShouldBe(0);
        }
        else
        {
            var page = await gateway.ReadStreamAsync(request, TestContext.Current.CancellationToken);
            page.Domain.ShouldBe(request.Domain); handler.Calls.ShouldBe(1);
            await issuer.Received(1).IssueAsync(Arg.Is<WorkloadAssertionRequest>(value => value.Audience == "eventstore" && value.Operation == "eventstore:stream-read"), Arg.Any<CancellationToken>());
        }
    }
}
