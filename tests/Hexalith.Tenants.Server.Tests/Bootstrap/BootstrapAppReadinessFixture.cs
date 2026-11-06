using System.Net;

using Dapr.Client;

using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Bootstrap;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Hexalith.Tenants.Server.Tests.Bootstrap;

/// <summary>Supplies an explicitly ready physical sidecar to tests that isolate other bootstrap contracts.</summary>
internal static class BootstrapAppReadinessFixture
{
    /// <summary>Gets a physical identity deliberately different from the workload issuer.</summary>
    internal const string AppId = "tenants-physical-17";

    /// <summary>Registers the SDK identity and a controlled configured sidecar.</summary>
    internal static void AddSidecar(IServiceCollection services)
    {
        services.AddSingleton<IOptions<DomainProjectionIdentityOptions>>(Options.Create(new DomainProjectionIdentityOptions { AppId = AppId }));
        services.AddScoped(_ => CreateDapr());
        AddProbe(services);
    }

    /// <summary>Creates a sidecar double whose invocation envelopes come from the real SDK.</summary>
    internal static DaprClient CreateDapr(string appId = AppId, string endpoint = "http://127.0.0.1:3500")
    {
        var dapr = Substitute.For<DaprClient>();
        dapr.GetMetadataAsync(Arg.Any<CancellationToken>()).Returns(Task.FromResult(new DaprMetadata(appId, [], new Dictionary<string, string>(), [])));
        dapr.CreateInvokeMethodRequest(Arg.Any<HttpMethod>(), Arg.Any<string>(), Arg.Any<string>()).Returns(call =>
        {
            using DaprClient concrete = new DaprClientBuilder().UseHttpEndpoint(endpoint).Build();
            return concrete.CreateInvokeMethodRequest(call.ArgAt<HttpMethod>(0), call.ArgAt<string>(1), call.ArgAt<string>(2));
        });
        return dapr;
    }

    /// <summary>Creates an independent successful health transport without consuming command expectations.</summary>
    internal static TenantBootstrapAppReadiness CreateProbe()
        => new(new BootstrapCommandHttpMessageHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK))));

    /// <summary>Registers a controlled private health transport independently of command factory policies.</summary>
    internal static void AddProbe(IServiceCollection services)
        => services.AddSingleton(_ => CreateProbe());
}
