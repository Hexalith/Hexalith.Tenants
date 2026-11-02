#if HEXALITH_DEVELOPMENT_CATALOG_COHORT
using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;
using StackExchange.Redis;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Mandatory standalone source-runtime proof; model/socket fixtures and package compatibility hosts cannot satisfy it.</summary>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class SourceCatalogOwnerProfileTests(AspireTopologyFixture fixture)
{
    [Fact(Timeout = 180000)]
    public async Task ActualOwnerCohortCommitsCompleteV2AndPreservesOwnedBootstrapAndRestEffects()
    {
        AspireTopologyFixture.IsSourceCatalogProfile.ShouldBeTrue();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromMinutes(2));
        string token = AspireTopologyTests.CreateDemoJwt();
        var (state, directory) = await SourceCatalogBootstrapEvidence.ReadCommittedAsync(deadline.Token);
        var activation = state.Committed.ShouldNotBeNull();
        var catalog = RouteCatalogCodec.Decode(activation.CanonicalCatalog, activation.RootDigest);
        catalog.SchemaVersion.ShouldBe(3);
        activation.RequiredHosts.Select(host => (host.HostId, host.AppId)).Order().ShouldBe(
            new[] { ("eventstore", "eventstore"), ("eventstore-admin", "eventstore-admin"), ("tenants", "tenants"), ("tenants-api", "tenants-api") }.Order());
        catalog.Routes.ShouldAllBe(route => route.AppId != "tenants-api");
        var operations = CompiledRouteCatalogDeclarations.ReadOperations([typeof(TenantCatalogDeclarations).Assembly]);
        catalog.Operations!.Select(operation => (operation.RouteId, operation.Digest)).Order().ShouldBe(operations.Select(operation => (operation.RouteId, operation.Digest)).Order());
        activation.TechnicalHttpReceivers.ShouldNotBeNull().Select(receiver => receiver.AppId).Order().ShouldBe(["eventstore", "eventstore-admin"]);
        string binding = RouteCatalogActivationBinding.ComputeDigest(activation);
        foreach (var host in activation.RequiredHosts)
        {
            string path = Path.Combine(directory, host.HostId + ".committed.json");
            while (!File.Exists(path)) await Task.Delay(50, deadline.Token);
            var held = JsonSerializer.Deserialize<RouteCatalogGenerationReadback>(SourceCatalogBootstrapEvidence.ReadPrivate(path, 1024 * 1024)).ShouldNotBeNull();
            held.Serving.ShouldBeTrue(); held.Epoch.ShouldBe(state.Epoch); held.Generation.ShouldBe(catalog.Generation);
            held.RootDigest.ShouldBe(activation.RootDigest); held.AttemptId.ShouldBe(activation.AttemptId); held.ActivationBindingDigest.ShouldBe(binding);
            held.RouteFacetDigest.ShouldBe(catalog.RouteFacetDigest); held.IdempotencyFacetDigest.ShouldBe(catalog.IdempotencyFacetDigest); held.Profile.ShouldBe(catalog.Profile);
            held.RequiredHosts.OrderBy(peer => peer.HostId, StringComparer.Ordinal).ShouldBe(activation.RequiredHosts.OrderBy(peer => peer.HostId, StringComparer.Ordinal));
        }
        string tenantId = "source-catalog-" + Guid.NewGuid().ToString("N");
        var status = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient,
            AspireTopologyTests.CreateCommand("tenants", tenantId, nameof(CreateTenant), new CreateTenant(tenantId, "Source catalog proof", "Owned source-runtime qualification")),
            token, deadline.Token);
        status.Status.ShouldBe("Completed"); status.EventCount.HasValue.ShouldBeTrue(); status.EventCount.Value.ShouldBeGreaterThan(0);
        var persisted = await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId, null, deadline.Token);
        persisted.ShouldNotBeNull();
        using var scoped = AspireTopologyTests.CreateIsolatedTenantsApiClient(fixture.TenantsApiClient, token, _ => { });
        var rest = new TenantsRestQueryClient(scoped);
        var list = await rest.ListTenantsAsync(new ListTenantsQuery { PageSize = 100 }, null, deadline.Token);
        list.IsSuccess.ShouldBeTrue(list.ToString());
        var detail = await rest.GetTenantAsync(new GetTenantQuery { TenantId = tenantId }, null, deadline.Token); detail.IsSuccess.ShouldBeTrue();
        (await rest.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = tenantId, PageSize = 100 }, null, deadline.Token)).IsSuccess.ShouldBeTrue();
        (await rest.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = AuthenticatedRouteState.CallerId, PageSize = 100 }, null, deadline.Token)).IsSuccess.ShouldBeTrue();
        (await rest.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = tenantId, PageSize = 100 }, null, deadline.Token)).IsSuccess.ShouldBeTrue();
        (await rest.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 100 }, null, deadline.Token)).IsSuccess.ShouldBeTrue();
        detail.Metadata.ETag.ShouldNotBeNullOrWhiteSpace();
        (await rest.GetTenantAsync(new GetTenantQuery { TenantId = tenantId }, detail.Metadata.ETag, deadline.Token)).IsNotModified.ShouldBeTrue();
        // This SSR page calls the actual Admin UI issuer and named Dapr client, then
        // the marked Admin receiver and its independently bound Gateway owner reader.
        using var adminPage = await fixture.AdminUiClient.GetAsync("/dapr", deadline.Token);
        adminPage.StatusCode.ShouldBe(HttpStatusCode.OK);
        string adminHtml = await adminPage.Content.ReadAsStringAsync(deadline.Token);
        adminHtml.ShouldContain("DAPR Infrastructure"); adminHtml.ShouldContain("statestore");
        adminHtml.ShouldNotContain("Unable to load DAPR infrastructure");
        adminHtml.ShouldNotContain("EventStore sidecar unreachable");
        adminHtml.ShouldNotContain("EventStore sidecar metadata invalid");
        adminHtml.ShouldNotContain("DAPR component inventory not configured");
        // The unmarked descendant is still refused by the real UI-sidecar invocation path.
        using var refused = new HttpRequestMessage(HttpMethod.Get, "/v1.0/invoke/tenants-api/method/api/tenants/" + tenantId + "/foreign-child");
        refused.Headers.Authorization = new("Bearer", token);
        using var denied = await fixture.TenantsUiDaprClient.SendAsync(refused, deadline.Token);
        denied.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        using var redis = await ConnectionMultiplexer.ConnectAsync("localhost:6379,abortConnect=false,connectTimeout=5000,asyncTimeout=5000").WaitAsync(deadline.Token);
        (await redis.GetDatabase().KeyExistsAsync("tenants||projection:tenants:" + tenantId).WaitAsync(deadline.Token)).ShouldBeTrue();
        while (true)
        {
            var final = await SourceCatalogBootstrapEvidence.ReadCommittedAsync(deadline.Token);
            final.State.Committed!.AttemptId.ShouldBe(activation.AttemptId);
            if (final.State.Leases.Count == 0) break;
            await Task.Delay(100, deadline.Token);
        }
        string output = Environment.GetEnvironmentVariable("TENANTS_SOURCE_CATALOG_RESULT") ?? throw new InvalidOperationException("source-catalog-result-unconfigured");
        byte[] result = JsonSerializer.SerializeToUtf8Bytes(new
        {
            sdkSource = Environment.GetEnvironmentVariable("TENANTS_SOURCE_SDK_SHA"),
            sdkProducerVersion = Environment.GetEnvironmentVariable("TENANTS_SOURCE_SDK_PRODUCER_VERSION"), activation.AttemptId, activation.RootDigest,
            activationBindingDigest = binding, state.Epoch, generation = catalog.Generation, requiredHosts = activation.RequiredHosts,
            operations = operations.Count, ownedBootstrapTerminalAndProjectedMembership = true,
            realRestReads = 6, conditionalRead = true, actualAdminUiProxyAndOwnerRead = true, refusedDescendant = true, persistedTenant = true, outstandingLeases = 0,
        });
        var resultStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using (resultStream.ConfigureAwait(false)) await resultStream.WriteAsync(result, deadline.Token);
    }
}
#endif
