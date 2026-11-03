#if HEXALITH_DEVELOPMENT_CATALOG_SOURCE_REFERENCE
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Routing;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;
using StackExchange.Redis;
using StoredEventEnvelope = Hexalith.EventStore.Server.Events.EventEnvelope;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Mandatory standalone source-runtime proof; model/socket fixtures and package compatibility hosts cannot satisfy it.</summary>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class SourceCatalogOwnerProfileTests(AspireTopologyFixture fixture)
{
    [Fact(Timeout = 360000)]
    public async Task ActualOwnerCohortCommitsCompleteV2AndPreservesOwnedBootstrapAndRestEffects()
    {
        AspireTopologyFixture.IsSourceCatalogProfile.ShouldBeTrue();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        // The actual source profile performs several bounded native reads and an Admin UI proxy check.
        // Keep one finite qualification window around the whole sequence.
        deadline.CancelAfter(TimeSpan.FromMinutes(5));
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
        using var scoped = AspireTopologyTests.CreateIsolatedTenantsApiClient(fixture.TenantsApiClient, AspireTopologyTests.CreateDemoJwt(tenantId), _ => { });
        var rest = new TenantsRestQueryClient(scoped);
        var list = await ReadWithTransientRetryAsync(ct => rest.ListTenantsAsync(new ListTenantsQuery { PageSize = 100 }, null, ct), deadline.Token);
        list.IsSuccess.ShouldBeTrue(list.ToString());
        var detail = await ReadWithTransientRetryAsync(ct => rest.GetTenantAsync(new GetTenantQuery { TenantId = tenantId }, null, ct), deadline.Token);
        detail.IsSuccess.ShouldBeTrue(detail.ToString());
        var tenantUsers = await ReadWithTransientRetryAsync(ct => rest.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = tenantId, PageSize = 100 }, null, ct), deadline.Token);
        tenantUsers.IsSuccess.ShouldBeTrue(tenantUsers.ToString());
        var userTenants = await ReadWithTransientRetryAsync(ct => rest.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = AuthenticatedRouteState.CallerId, PageSize = 100 }, null, ct), deadline.Token);
        userTenants.IsSuccess.ShouldBeTrue(userTenants.ToString());
        var tenantAudit = await ReadWithTransientRetryAsync(ct => rest.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = tenantId, PageSize = 100 }, null, ct), deadline.Token);
        tenantAudit.IsSuccess.ShouldBeTrue(tenantAudit.ToString());
        tenantAudit.Payload.ShouldNotBeNull().Items.ShouldContain(item => item.EventType == nameof(TenantCreated) && item.TenantId == tenantId);
        var globalAdministrators = await ReadWithTransientRetryAsync(ct => rest.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 100 }, null, ct), deadline.Token);
        globalAdministrators.IsSuccess.ShouldBeTrue(globalAdministrators.ToString());
        // The public platform route must still read the retained global owner without exposing system as a request scope.
        var globalAudit = await ReadWithTransientRetryAsync(ct => rest.GetTenantAuditAsync(new GetTenantAuditQuery
        {
            TenantId = "system", Stream = "global-administrators", PageSize = 100,
        }, null, ct), deadline.Token);
        globalAudit.IsSuccess.ShouldBeTrue(globalAudit.ToString());
        globalAudit.Payload.ShouldNotBeNull().Items.ShouldContain(item => item.EventType == nameof(GlobalAdministratorSet) && item.TenantId == "system");
        using var publicSystem = new HttpRequestMessage(HttpMethod.Get, "/api/tenants/system/audit");
        publicSystem.Headers.Add("dapr-app-id", "tenants-api");
        publicSystem.Headers.Authorization = new("Bearer", token);
        using var deniedSystem = await fixture.TenantsUiDaprClient.SendAsync(publicSystem, deadline.Token);
        deniedSystem.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        detail.Metadata.ETag.ShouldNotBeNullOrWhiteSpace();
        var conditionalRead = await ReadWithTransientRetryAsync(ct => rest.GetTenantAsync(new GetTenantQuery { TenantId = tenantId }, detail.Metadata.ETag, ct), deadline.Token);
        conditionalRead.IsNotModified.ShouldBeTrue(conditionalRead.ToString());
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
        string tenantAuditKey = TenantAuditStreams.GetTenantKey(tenantId);
        tenantAuditKey.ShouldNotBe(TenantAuditStreams.GlobalAdministratorsKey);
        (await redis.GetDatabase().KeyExistsAsync("tenants||" + tenantAuditKey).WaitAsync(deadline.Token)).ShouldBeTrue();
        (await redis.GetDatabase().KeyExistsAsync("tenants||" + TenantAuditStreams.GlobalAdministratorsKey).WaitAsync(deadline.Token)).ShouldBeTrue();
        // The public tenant stream route reads its retained actor through EventStore's Dapr
        // sidecar and must return the historical storage identity unchanged.
        using var historyRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/streams/read")
        {
            Content = JsonContent.Create(new StreamReadRequest("platform:tenants", "tenants", tenantId)),
        };
        historyRequest.Headers.Authorization = new("Bearer", token);
        historyRequest.Headers.Add("X-Hexalith-Scope", "platform:tenants");
        using HttpResponseMessage historyResponse = await fixture.CommandApiClient.SendAsync(historyRequest, deadline.Token);
        historyResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tenantHistory = (await historyResponse.Content.ReadFromJsonAsync<StreamReadPage>(cancellationToken: deadline.Token)).ShouldNotBeNull();
        tenantHistory.Tenant.ShouldBe("system"); tenantHistory.Domain.ShouldBe("tenants"); tenantHistory.AggregateId.ShouldBe(tenantId);
        tenantHistory.Events.ShouldContain(item => item.EventTypeName == typeof(TenantCreated).FullName);
        // The fresh Dapr Redis backend retains both actor histories under separate legacy
        // owner identities. Read only the first persisted event from each actor namespace.
        async Task<StoredEventEnvelope> ReadRetainedEventAsync(AggregateIdentity identity)
        {
            string key = $"eventstore||AggregateActor||{identity.ActorId}||{identity.EventStreamKeyPrefix}1";
            RedisValue value = await redis.GetDatabase().HashGetAsync(key, "data").WaitAsync(deadline.Token);
            value.IsNullOrEmpty.ShouldBeFalse();
            return JsonSerializer.Deserialize<StoredEventEnvelope>(value.ToString(), JsonSerializerOptions.Web).ShouldNotBeNull();
        }
        var tenantIdentity = TenantIdentity.ForTenant(tenantId);
        var globalIdentity = TenantIdentity.ForGlobalAdministrators();
        var tenantStoredEvent = await ReadRetainedEventAsync(tenantIdentity);
        var globalStoredEvent = await ReadRetainedEventAsync(globalIdentity);
        tenantStoredEvent.TenantId.ShouldBe("system"); tenantStoredEvent.Domain.ShouldBe("tenants"); tenantStoredEvent.AggregateId.ShouldBe(tenantId);
        tenantStoredEvent.EventTypeName.ShouldBe(typeof(TenantCreated).FullName);
        globalStoredEvent.TenantId.ShouldBe("system"); globalStoredEvent.Domain.ShouldBe("global-administrators");
        globalStoredEvent.AggregateId.ShouldBe(TenantIdentity.GlobalAdministratorsAggregateId);
        globalStoredEvent.EventTypeName.ShouldBe(typeof(GlobalAdministratorSet).FullName);
        // Dapr Redis pub/sub retains topic entries as a Redis stream. The fresh backend has
        // only this qualification's traffic, so each family must appear on the shared legacy topic.
        var topic = await redis.GetDatabase().StreamRangeAsync("tenants.events").WaitAsync(deadline.Token);
        topic.ShouldContain(entry => entry.Values.Any(field => field.Value.ToString().Contains(tenantId, StringComparison.Ordinal)));
        topic.ShouldContain(entry => entry.Values.Any(field => field.Value.ToString().Contains(nameof(GlobalAdministratorSet), StringComparison.Ordinal)));
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
            realRestReads = 7, conditionalRead = true, distinctAuditOwnerReadback = true, nativeLegacyHistories = true,
            sharedLegacyTopicReadback = true, publicSystemDenied = true,
            actualAdminUiProxyAndOwnerRead = true, refusedDescendant = true, persistedTenant = true, outstandingLeases = 0,
        });
        var resultStream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await using (resultStream.ConfigureAwait(false)) await resultStream.WriteAsync(result, deadline.Token);
    }

    private static async Task<TenantsRestQueryResponse<TPayload>> ReadWithTransientRetryAsync<TPayload>(
        Func<CancellationToken, Task<TenantsRestQueryResponse<TPayload>>> read,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; ; attempt++)
        {
            TenantsRestQueryResponse<TPayload> response = await read(cancellationToken);
            if (response.IsSuccess || response.StatusCode is not (500 or 502 or 503 or 504) || attempt == 3)
            {
                return response;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200 * (attempt + 1)), cancellationToken);
        }
    }
}
#endif
