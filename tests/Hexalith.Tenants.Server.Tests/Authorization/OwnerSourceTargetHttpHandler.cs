using System.Net;
using System.Net.Http.Json;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Authorization;

/// <summary>Observes the final SDK-routed owner request at the actual primary transport.</summary>
internal sealed class OwnerSourceTargetHttpHandler(string? expected, string caller, string publicKey, Uri? runtimeAddress = null, string? rootDigest = null, string? bindingDigest = null) : HttpMessageHandler
{
    internal int Calls { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (runtimeAddress is not null)
            request.RequestUri.ShouldBe(new Uri(runtimeAddress, "api/v1/streams/read/workload"));
        if (rootDigest is not null)
        {
            request.Headers.GetValues(RouteCatalogInvocationHandler.RootHeader).ShouldHaveSingleItem().ShouldBe(rootDigest);
            request.Headers.GetValues(RouteCatalogInvocationHandler.BindingHeader).ShouldHaveSingleItem().ShouldBe(bindingDigest);
            request.Headers.GetValues(RouteCatalogInvocationHandler.EpochHeader).ShouldHaveSingleItem().ShouldBe("1");
        }
        request.Headers.GetValues("dapr-app-id").ShouldHaveSingleItem().ShouldBe(expected);
        request.Headers.GetValues(EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName).ShouldHaveSingleItem().ShouldBe("owner-assertion");
        string proof = request.Headers.GetValues("X-Hexalith-Bound-Stream-Read-Proof").ShouldHaveSingleItem();
        StreamReadRequest source = (await request.Content!.ReadFromJsonAsync<StreamReadRequest>(cancellationToken)).ShouldNotBeNull();
        BoundStreamReadProof.Verify(proof, source, caller, publicKey, DateTimeOffset.UtcNow).ShouldBeTrue();
        BoundStreamReadProof.Verify(proof, source, caller + "-foreign", publicKey, DateTimeOffset.UtcNow).ShouldBeFalse();
        request.RequestUri!.AbsolutePath.ShouldBe("/api/v1/streams/read/workload");
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new StreamReadPage("system", "global-administrators", "global-administrators", [], new(0, 0, null, 0, 0, false, null))),
        };
    }
}
