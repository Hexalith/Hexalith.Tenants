using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;
using Microsoft.IdentityModel.Tokens;
using NSubstitute;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.FrontComposer.Shell.Extensions;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.Extensions;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantUsers;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Qualifies the six production REST reads against authenticated live Aspire sockets.</summary>
[Collection("AuthenticatedRouteTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class AuthenticatedTenantsRouteTests(AuthenticatedRouteTopologyFixture fixture) : IDisposable
{
    private readonly IDisposable _daprTestLease = DaprTestExecutionGate.Enter();

    /// <inheritdoc/>
    public void Dispose() => _daprTestLease.Dispose();

    [DaprFact]
    public async Task CommittedNativeRemovalRefusesARetainedGrantingProjectionThroughProductionReaders()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        string administrator = "native-revocation-" + Guid.NewGuid().ToString("N");
        string token = AspireTopologyTests.CreateDemoJwt();
        var grant = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient,
            AspireTopologyTests.CreateCommand("global-administrators", "global-administrators", nameof(SetGlobalAdministrator),
                new SetGlobalAdministrator(administrator)), token, timeout.Token);
        grant.Status.ShouldBe("Completed"); grant.CommittedEventSequence.ShouldNotBeNull();
        using ServiceProvider reads = CreateProductionReadServices(token);
        using IServiceScope scope = reads.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
        await AssertReadAsync(tag => client.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 100 }, tag, timeout.Token),
            page => page.Items.Any(item => item.UserId == administrator));

        // Retain the actual persisted granting projection, without changing the live store or its workers.
        using var redis = await StackExchange.Redis.ConnectionMultiplexer.ConnectAsync(new StackExchange.Redis.ConfigurationOptions
        {
            EndPoints = { $"localhost:{DaprDiagnostics.ResolveRedisPort()}" },
            ConnectTimeout = 5_000, SyncTimeout = 5_000, AbortOnConnectFail = true,
        });
        var bytes = await redis.GetDatabase().HashGetAsync("tenants||projection:global-administrators:singleton", "data").WaitAsync(timeout.Token);
        var granting = JsonSerializer.Deserialize<GlobalAdministratorReadModel>(bytes.ToString(), JsonSerializerOptions.Web).ShouldNotBeNull();
        granting.Administrators.ShouldContain(administrator);
        var retained = new InMemoryReadModelStore();
        await retained.SaveAsync("statestore", "projection:global-administrators:singleton", granting, timeout.Token);

        // Only test credentials are supplied here. Both source readers and the source itself are production
        // implementations over the running EventStore/Dapr topology; no native reader or metadata fake is registered.
        string assertion = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken("hexalith-dev", "eventstore",
            [new("azp", "tenants"), new("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64),
                new(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, "eventstore:stream-read"),
                new(EventStoreWorkloadAuthenticationDefaults.TenantBindingClaimType, "system"),
                new(EventStoreWorkloadAuthenticationDefaults.DomainBindingClaimType, "global-administrators")],
            expires: DateTime.UtcNow.AddSeconds(60), signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AspireTopologyFixture.DemoSigningKey)), SecurityAlgorithms.HmacSha256)));
        var credentials = Substitute.For<IEventStoreGatewayWorkloadAssertionSource>();
        credentials.IssueAsync("eventstore:stream-read", "system", "global-administrators", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult<string?>(assertion));
        using var http = new HttpClient { BaseAddress = fixture.TenantsDaprClient.BaseAddress };
        http.DefaultRequestHeaders.Add("dapr-app-id", "eventstore");
        var gateway = new EventStoreGatewayClient(http, Options.Create(new EventStoreGatewayClientOptions { UseWorkloadTransport = true }), credentials);
        var stream = new AuthoritativeEventStreamReader(gateway, TimeProvider.System, TimeSpan.FromSeconds(10), requireNativeProof: true);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["DAPR_HTTP_ENDPOINT"] = fixture.TenantsDaprClient.BaseAddress!.AbsoluteUri }).Build();
        using var metadataHttp = new HttpClient();
        var metadata = new DaprNativeSourceMetadataReader(metadataHttp, configuration, TimeProvider.System);
        var cursor = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "live-native-revocation");
        var telemetry = new TenantTelemetry(new EventStoreDomainDiagnostics("tenants"));
        var envelope = new QueryEnvelope("system", "global-administrators", "global-administrators",
            GetGlobalAdministratorsQuery.QueryType, [], "live-native-revocation", administrator, isGlobalAdmin: true);
        GetGlobalAdministratorsQueryHandler Handler() => new(retained,
            new TenantsGlobalAdministratorAuthority(stream, metadata, TimeProvider.System), cursor, telemetry,
            NullLogger<GetGlobalAdministratorsQueryHandler>.Instance);
        var beforeSource = await stream.ReadAsync(new AggregateIdentity("system", "global-administrators", "global-administrators"), timeout.Token);
        beforeSource.IsAuthoritative.ShouldBeTrue(beforeSource.FailureReason);
        (await Handler().ExecuteAsync(envelope, timeout.Token)).Success.ShouldBeTrue();

        var removal = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient,
            AspireTopologyTests.CreateCommand("global-administrators", "global-administrators", nameof(RemoveGlobalAdministrator),
                new RemoveGlobalAdministrator(administrator)), token, timeout.Token);
        removal.Status.ShouldBe("Completed"); removal.CommittedEventSequence.ShouldNotBeNull().ShouldBeGreaterThan(grant.CommittedEventSequence!.Value);
        var source = (await stream.ReadAsync(new AggregateIdentity("system", "global-administrators", "global-administrators"), timeout.Token))
            .Stream.ShouldNotBeNull();
        var committed = source.Events.Single(item => item.SequenceNumber == removal.CommittedEventSequence);
        committed.EventTypeName.ShouldContain(nameof(GlobalAdministratorRemoved));
        JsonSerializer.Deserialize<GlobalAdministratorRemoved>(committed.Payload, JsonSerializerOptions.Web)!.UserId.ShouldBe(administrator);
        source.NativeProof.ShouldNotBeNull().HeadSequence.ShouldBeGreaterThanOrEqualTo(removal.CommittedEventSequence!.Value);
        var refused = await Handler().ExecuteAsync(envelope, timeout.Token);
        refused.Success.ShouldBeFalse(); refused.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        (await retained.GetAsync<GlobalAdministratorReadModel>("statestore", "projection:global-administrators:singleton", timeout.Token))
            .Value.ShouldNotBeNull().Administrators.ShouldContain(administrator);
        TestContext.Current.TestOutputHelper?.WriteLine("Native removal control: committed removal present, production stream/final metadata readers verified, retained granting projection refused.");
    }

    [DaprFact]
    public async Task SixAuthenticatedDaprRestReadsMatchPersistedState()
    {
        fixture.SkipIfUnavailable();
        fixture.ProjectionWorkersEnabledAtStartup.ShouldBeFalse("this independent fixture qualifies the existing request-path profile");
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        using ServiceProvider provider = CreateProductionReadServices(state.Token);
        using IServiceScope scope = provider.CreateScope();
        ITenantsRestQueryClient client = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();

        string? cursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 100, timeout.Token);
        bool found = false;
        do
        {
            var query = new ListTenantsQuery { PageSize = 100, Cursor = cursor };
            var list = await AssertReadAsync(tag => client.ListTenantsAsync(query, tag, timeout.Token));
            found |= list.Items.Any(item => item.TenantId == state.TenantId && item.Name == state.Persisted.Name);
            cursor = list.HasMore ? list.Cursor : null;
        }
        while (!found && cursor is not null);
        found.ShouldBeTrue();

        TenantDetail detail = await AssertReadAsync(tag => client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, tag, timeout.Token), expectedVersion: state.Persisted.ProjectionVersion);
        detail.TenantId.ShouldBe(state.Persisted.TenantId);
        detail.Name.ShouldBe(state.Persisted.Name);
        detail.Description.ShouldBe(state.Persisted.Description);
        detail.Status.ShouldBe(state.Persisted.Status);
        detail.Members.Select(static member => (member.UserId, member.Role)).OrderBy(static member => member.UserId)
            .ShouldBe(state.Persisted.Members.Select(static member => (UserId: member.Key, Role: member.Value)).OrderBy(static member => member.UserId));

        var users = await AssertReadAsync(tag => client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, PageSize = 100 }, tag, timeout.Token));
        users.Items.Select(static member => (member.UserId, member.Role)).OrderBy(static member => member.UserId)
            .ShouldBe(state.Persisted.Members.Select(static member => (UserId: member.Key, Role: member.Value)).OrderBy(static member => member.UserId));
        var memberships = await AssertReadAsync(tag => client.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = state.TargetUserId, PageSize = 100 }, tag, timeout.Token), page => page.Items.Any(item => item.TenantId == state.TenantId));
        memberships.Items.ShouldHaveSingleItem().ShouldBe(new UserTenantMembership(state.Persisted.TenantId, state.Persisted.Name,
            state.Persisted.Status, state.Persisted.Members[state.TargetUserId]));
        var audit = await AssertReadAsync(tag => client.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = state.TenantId, PageSize = 100 }, tag, timeout.Token), page => page.Items.Count >= 3, expectedVersion: state.Persisted.ProjectionVersion);
        audit.Items.Count.ShouldBe(3);
        audit.Items.All(item => item.TenantId == state.TenantId).ShouldBeTrue();
        audit.Items.Select(static item => item.EventId).Distinct(StringComparer.Ordinal).Count().ShouldBe(3);
        foreach (var expected in state.SeededAuditEvents)
        {
            TenantAuditEntry entry = audit.Items.Single(item => item.EventType == expected.EventType && item.Target == expected.Target);
            entry.EventType.ShouldBe(expected.EventType);
            entry.Target.ShouldBe(expected.Target);
            entry.ActorId.ShouldBe(AuthenticatedRouteState.CallerId);
            // These event types deliberately withhold wire sequences. Verify the isolated ordered
            // prefix against actual committed 1/2/3 outcomes instead of expanding that contract.
            entry.AggregateSequence.ShouldBeNull();
            if (state.Persisted.Members.TryGetValue(expected.Target, out var role))
            {
                entry.NarrativePayload["role"].ShouldBe(role.ToString());
            }
        }
        state.SeededAuditEvents.Select(static item => item.Sequence).ShouldBe([1L, 2L, 3L]);
        audit.Items.Select(static item => (item.EventType, item.Target))
            .ShouldBe(state.SeededAuditEvents.OrderBy(static item => item.Sequence).Select(static item => (item.EventType, item.Target)));
        var administrators = await AssertReadAsync(tag => client.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 100 }, tag, timeout.Token));
        administrators.Items.ShouldContain(item => item.UserId == AuthenticatedRouteState.CallerId);
    }

    [DaprFact]
    public async Task ConditionalDetailReadRecognizesPersistedAuthority()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        using ServiceProvider provider = CreateProductionReadServices(state.Token);
        using IServiceScope scope = provider.CreateScope();
        ITenantsRestQueryClient client = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
        TenantsRestQueryResponse<TenantDetail> first = await client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, null, timeout.Token);
        first.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        first.Payload.ShouldNotBeNull().TenantId.ShouldBe(state.Persisted.TenantId);
        AssertAuthority(first.Metadata);
        first.Metadata.ProjectionVersion.ShouldBe(state.Persisted.ProjectionVersion);

        TenantsRestQueryResponse<TenantDetail> conditional = await client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, first.Metadata.ETag, timeout.Token);
        conditional.StatusCode.ShouldBe((int)HttpStatusCode.NotModified);
        conditional.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        conditional.Metadata.IsNotModified.ShouldBe(true);
        conditional.Metadata.ETag.ShouldBe(first.Metadata.ETag);
        conditional.Metadata.ProjectionVersion.ShouldBe(first.Metadata.ProjectionVersion);
        AssertAuthority(conditional.Metadata);
    }

    [DaprFact]
    public async Task InvalidProtectedCursorHasRealSentinelAndRecoversPageOneThroughProductionGateway()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        using ServiceProvider provider = CreateProductionReadServices(state.Token);
        using IServiceScope scope = provider.CreateScope();
        ITenantsRestQueryClient client = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
        await state.WaitForListPageAsync(fixture.TenantsApiClient, 100, timeout.Token);
        var invalid = await client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, Cursor = "foreign-protected-cursor", PageSize = 1 }, null, timeout.Token);
        invalid.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        invalid.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);
        var issuedMembers = await client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, PageSize = 1 }, null, timeout.Token);
        issuedMembers.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        var issuedPage = issuedMembers.Payload.ShouldNotBeNull();
        issuedPage.HasMore.ShouldBeTrue();
        issuedPage.Cursor.ShouldNotBeNullOrWhiteSpace();
        var incompatibleQuery = await client.ListTenantsAsync(new ListTenantsQuery { Cursor = issuedPage.Cursor, PageSize = 1 }, null, timeout.Token);
        incompatibleQuery.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        incompatibleQuery.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);

        using var memoriesHttp = new HttpClient { BaseAddress = new Uri("https://unused.invalid") };
        var memories = new MemoriesClient(memoriesHttp, Options.Create(new MemoriesClientOptions()), NullLogger<MemoriesClient>.Instance);
        using var gateway = new TenantQueryGateway(client, new LiveRouteUserContext(), memories, new TenantSearchCursorCodec(new EphemeralDataProtectionProvider()));
        var invalidList = await client.ListTenantsAsync(new ListTenantsQuery { Cursor = "foreign-protected-cursor", PageSize = 1 }, null, timeout.Token);
        invalidList.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        invalidList.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);
        var baselineList = await gateway.ListTenantsAsync(new TenantListRequest(PageSize: 1), null, timeout.Token);
        baselineList.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        var recoveredList = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: issuedPage.Cursor, PageSize: 1), null, timeout.Token);
        recoveredList.Notice.ShouldBe(TenantListReason.ListRefreshed);
        recoveredList.Rows.ShouldBe(baselineList.Rows);
        recoveredList.HasMore.ShouldBe(baselineList.HasMore);
        string.IsNullOrWhiteSpace(recoveredList.NextCursor).ShouldBe(string.IsNullOrWhiteSpace(baselineList.NextCursor));
        recoveredList.RequestCursor.ShouldBeNull();
        recoveredList.RequestPageSize.ShouldBe(baselineList.RequestPageSize);
        if (baselineList.HasMore)
        {
            var baselineNext = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: baselineList.NextCursor, PageSize: 1), null, timeout.Token);
            var recoveredNext = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: recoveredList.NextCursor, PageSize: 1), null, timeout.Token);
            recoveredNext.Rows.ShouldBe(baselineNext.Rows);
            recoveredNext.HasMore.ShouldBe(baselineNext.HasMore);
        }
        var recovered = await gateway.GetTenantUsersAsync(new TenantUsersRequest(state.TenantId, Cursor: "foreign-protected-cursor", PageSize: 1), null, timeout.Token);
        recovered.PagingRecovered.ShouldBeTrue();
        recovered.Rows.ShouldHaveSingleItem().UserId.ShouldBe(state.Persisted.Members.Keys.Order().First());

        // An invalid typed query generates a real, unrelated 400 and must retain InvalidRequest.
        var unrelated = await client.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = state.TenantId, From = DateTimeOffset.UtcNow, To = DateTimeOffset.UtcNow.AddDays(-1) }, null, timeout.Token);
        unrelated.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        unrelated.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidRequest);
    }

    [DaprFact]
    public async Task AnonymousRestRequestCannotReadCommandSeededTenant()
    {
        fixture.SkipIfUnavailable();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants");
        request.Headers.Add("dapr-app-id", "tenants-api");
        using HttpResponseMessage response = await fixture.TenantsUiDaprClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [DaprFact]
    public async Task AuthenticatedWrongPeerCannotInvokeTheReadReceiver()
    {
        fixture.SkipIfUnavailable();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenants");
        request.Headers.Add("dapr-app-id", "tenants-api");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AspireTopologyTests.CreateDemoJwt());
        using HttpResponseMessage response = await fixture.EventStoreDaprClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DaprFact]
    public async Task AuthenticatedReadCallerCannotInvokeADisallowedVerb()
    {
        fixture.SkipIfUnavailable();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/tenants");
        request.Headers.Add("dapr-app-id", "tenants-api");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AspireTopologyTests.CreateDemoJwt());
        using HttpResponseMessage response = await fixture.TenantsUiDaprClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [DaprFact]
    public async Task PopulatedTwentyRowPageHydratesWithoutActorQuotaFailure()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        string token = AspireTopologyTests.CreateDemoJwt();
        for (int index = 0; index < 20; index++)
        {
            string tenantId = $"populated-page-{Guid.NewGuid():N}";
            var command = AspireTopologyTests.CreateCommand("tenants", tenantId,
                nameof(Hexalith.Tenants.Contracts.Commands.CreateTenant),
                new Hexalith.Tenants.Contracts.Commands.CreateTenant(tenantId, "Populated page control", "Native authority capacity control"));
            var status = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, command, token, timeout.Token);
            status.Status.ShouldBe("Completed");
            await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId, Hexalith.Tenants.Contracts.Projections.TenantProjectionVersionFormat.SequencePrefix + "1", timeout.Token);
        }

        // Start the measured page after the unchanged one-second consumer window used by command preparation.
        await Task.Delay(TimeSpan.FromSeconds(2), timeout.Token);
        TestContext.Current.TestOutputHelper?.WriteLine("Populated page control: starting one production 20-row load.");
        using ServiceProvider provider = CreateProductionReadServices(token);
        using IServiceScope scope = provider.CreateScope();
        using var gateway = new TenantQueryGateway(
            scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>(), new LiveRouteUserContext(),
            scope.ServiceProvider.GetRequiredService<MemoriesClient>(),
            scope.ServiceProvider.GetRequiredService<ITenantSearchCursorCodec>());
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        TenantListSnapshot result = await gateway.ListTenantsAsync(new TenantListRequest(PageSize: 20), null, timeout.Token);
        TestContext.Current.TestOutputHelper?.WriteLine($"Populated page outcome: Kind={result.Kind}, Rows={result.Rows.Count}, Degraded={result.IsDegraded}, KnownCounts={result.Rows.Count(row => row.MemberCount.IsKnown && row.OwnerCount.IsKnown)}, ElapsedMilliseconds={elapsed.Elapsed.TotalMilliseconds:F2}, Cancelled={timeout.IsCancellationRequested}.");
        result.Rows.Count.ShouldBe(20);
        result.IsDegraded.ShouldBeFalse();
        result.Rows.All(row => row.MemberCount.IsKnown && row.OwnerCount.IsKnown).ShouldBeTrue();
    }

    private ServiceProvider CreateProductionReadServices(string token)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = fixture.TenantsUiDaprClient.BaseAddress!.AbsoluteUri,
        }).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHexalithFrontComposerTokenRelay();
        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: true);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", AuthenticatedRouteState.CallerId)], "live-route")),
            },
        });
        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<FrontComposerUserTokenStore>().Set(AuthenticatedRouteState.CallerId, token, DateTimeOffset.UtcNow.AddMinutes(5));
        return provider;
    }

    private static async Task<T> AssertReadAsync<T>(Func<string?, Task<TenantsRestQueryResponse<T>>> read, Func<T, bool>? projected = null, string? expectedVersion = null)
        where T : class
    {
        TenantsRestQueryResponse<T> first = await read(null);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while ((first.Payload is null || (projected is not null && !projected(first.Payload))) && DateTimeOffset.UtcNow < deadline)
        {
            first.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            first = await read(null);
        }

        first.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        first.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        T payload = first.Payload.ShouldNotBeNull();
        if (projected is not null)
        {
            projected(payload).ShouldBeTrue();
        }
        AssertAuthority(first.Metadata);
        if (expectedVersion is not null)
        {
            first.Metadata.ProjectionVersion.ShouldBe(expectedVersion);
        }
        return payload;
    }

    private static void AssertAuthority(QueryResponseMetadata metadata)
    {
        metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        metadata.ETag.ShouldNotBeNullOrWhiteSpace();
        metadata.IsStale.ShouldBe(false);
        metadata.IsDegraded.ShouldNotBe(true);
        metadata.ServedAt.ShouldNotBeNull();
        metadata.ServedAt.Value.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddSeconds(5));
    }
}
