using System.Net;
using System.Net.Http.Json;
using System.Globalization;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.EventStore.Server.Events;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.IdentityModel.Tokens.Jwt;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Client.Streams;
using Hexalith.EventStore.Contracts.Identity;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.ServiceDefaults.Routing;
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
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Projections;
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
        using var redis = await CatalogRedisBackend.ConnectAsync(timeout.Token);
        var bytes = await CatalogRedisBackend.Database(redis).HashGetAsync("tenants||projection:global-administrators:singleton", "data").WaitAsync(timeout.Token);
        var granting = JsonSerializer.Deserialize<GlobalAdministratorReadModel>(bytes.ToString(), JsonSerializerOptions.Web).ShouldNotBeNull();
        granting.Administrators.ShouldContain(administrator);
        var retained = new InMemoryReadModelStore();
        await retained.SaveAsync("statestore", "projection:global-administrators:singleton", granting, timeout.Token);

        // Use the same bound proof, genuine workload issuer, owner gateway and durable admission as production.
        using ServiceProvider nativeReads = await fixture.CreateNativeOwnerReadServicesAsync(timeout.Token);
        var identity = new AggregateIdentity("system", "global-administrators", "global-administrators");
        var cursor = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "live-native-revocation");
        var telemetry = new TenantTelemetry(new EventStoreDomainDiagnostics("tenants"));
        var envelope = new QueryEnvelope("system", "global-administrators", "global-administrators",
            GetGlobalAdministratorsQuery.QueryType, [], "live-native-revocation", administrator, isGlobalAdmin: true);
        var before = await ReadCommittedAsync(grant.CommittedEventSequence!.Value);
        before.Event.EventTypeName.ShouldBe(typeof(GlobalAdministratorSet).FullName);
        JsonSerializer.Deserialize<GlobalAdministratorSet>(before.Event.Payload, JsonSerializerOptions.Web)
            .ShouldNotBeNull().UserId.ShouldBe(administrator);
        QueryResult granted = await ReadRetainedProjectionAsync();
        granted.Success.ShouldBeTrue();
        granted.PayloadBytes.ShouldNotBeNull();

        var removal = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient,
            AspireTopologyTests.CreateCommand("global-administrators", "global-administrators", nameof(RemoveGlobalAdministrator),
                new RemoveGlobalAdministrator(administrator)), token, timeout.Token);
        removal.Status.ShouldBe("Completed");
        long removalSequence = removal.CommittedEventSequence.ShouldNotBeNull();
        removalSequence.ShouldBeGreaterThan(before.Proof.HeadSequence);
        var after = await ReadCommittedAsync(removalSequence);
        after.Event.EventTypeName.ShouldBe(typeof(GlobalAdministratorRemoved).FullName);
        JsonSerializer.Deserialize<GlobalAdministratorRemoved>(after.Event.Payload, JsonSerializerOptions.Web)
            .ShouldNotBeNull().UserId.ShouldBe(administrator);
        after.Proof.SourceIncarnation.ShouldBe(before.Proof.SourceIncarnation);
        after.Proof.CumulativeDigest.ShouldNotBe(before.Proof.CumulativeDigest);
        QueryResult refused = await ReadRetainedProjectionAsync();
        refused.Success.ShouldBeFalse();
        refused.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        refused.PayloadBytes.ShouldBeNull();
        (await retained.GetAsync<GlobalAdministratorReadModel>("statestore", "projection:global-administrators:singleton", timeout.Token))
            .Value.ShouldNotBeNull().Administrators.ShouldContain(administrator);

        async Task<(StreamReadEvent Event, NativeSourceProof Proof)> ReadCommittedAsync(long sequence)
        {
            using IServiceScope scope = nativeReads.CreateScope();
            IServiceProvider services = scope.ServiceProvider;
            var stream = services.GetRequiredKeyedService<IAuthoritativeEventStreamReader>("native-tenant-source");
            var metadata = services.GetRequiredService<INativeSourceMetadataReader>().ShouldBeOfType<GatewayNativeSourceMetadataReader>();
            return await services.GetRequiredService<RouteCatalogInvocationScope>().ExecuteNativeSourceReadAsync("eventstore", async () =>
            {
                AuthoritativeStreamReadResult result = await stream.ReadAsync(identity, timeout.Token);
                result.IsAuthoritative.ShouldBeTrue(result.FailureReason);
                var source = result.Stream.ShouldNotBeNull();
                source.Identity.ShouldBe(identity);
                NativeSourceProof proof = source.NativeProof.ShouldNotBeNull();
                proof.HeadSequence.ShouldBeGreaterThanOrEqualTo(sequence);
                NativeSourceObservation observed = (await metadata.ReadAsync(identity, timeout.Token)).ShouldNotBeNull();
                observed.Proof.ShouldBe(proof);
                observed.ObservedAt.ShouldBeGreaterThanOrEqualTo(source.ObservedAt);
                return (source.Events.Single(item => item.SequenceNumber == sequence), proof);
            }, timeout.Token);
        }

        async Task<QueryResult> ReadRetainedProjectionAsync()
        {
            using IServiceScope scope = nativeReads.CreateScope();
            var handler = new GetGlobalAdministratorsQueryHandler(retained,
                scope.ServiceProvider.GetRequiredService<ITenantsGlobalAdministratorAuthority>(), cursor, telemetry,
                NullLogger<GetGlobalAdministratorsQueryHandler>.Instance);
            return await handler.ExecuteAsync(envelope, timeout.Token);
        }
    }

    [DaprFact]
    public async Task AuthenticatedOwnerRecoveryPersistsExactSnapshotsAndPermissionRefusalDoesNotAppendEvents()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        using var redis = await CatalogRedisBackend.ConnectAsync(timeout.Token);
        var database = CatalogRedisBackend.Database(redis);
#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
        using var consumer = await TenantRecoveryConsumerProbe.CreateAsync(database, timeout.Token);
#endif
        // The existing fixture owns the complete topology and its teardown; this test creates
        // only its unique tenant and disposes every request, Redis connection and caller scope.
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        string adminToken = AspireTopologyFixture.IsSourceCatalogProfile ? AspireTopologyTests.CreateDemoJwt() : state.Token;
        var promote = AspireTopologyTests.CreateCommand("tenants", state.TenantId, nameof(ChangeUserRole),
            new ChangeUserRole(state.TenantId, state.TargetUserId, TenantRole.TenantOwner));
        var promoted = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, promote, adminToken, timeout.Token);
        promoted.Status.ShouldBe("Completed");
        const string configurationKey = "sample.theme";
        const string configurationValue = "recovery-regression-value";
        var configure = AspireTopologyTests.CreateCommand("tenants", state.TenantId, nameof(SetTenantConfiguration),
            new SetTenantConfiguration(state.TenantId, configurationKey, configurationValue));
        var configured = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, configure, adminToken, timeout.Token);
        configured.Status.ShouldBe("Completed");
        long sequence = configured.CommittedEventSequence.ShouldNotBeNull();
        string ownerToken = OwnerToken(true);
        string deniedToken = OwnerToken(false);
        using ServiceProvider reads = CreateProductionReadServices(state.Token);
        using IServiceScope readScope = reads.CreateScope();
        var readsClient = readScope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
        var identity = new AggregateIdentity("system", "tenants", state.TenantId);
        string eventPrefix = $"eventstore||AggregateActor||{identity.ActorId}||{identity.EventStreamKeyPrefix}";
        foreach (bool disabled in new[] { false, true })
        {
            if (disabled)
            {
                var remove = AspireTopologyTests.CreateCommand("tenants", state.TenantId, nameof(RemoveUserFromTenant),
                    new RemoveUserFromTenant(state.TenantId, AuthenticatedRouteState.CallerId));
                var removed = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, remove, adminToken, timeout.Token);
                removed.Status.ShouldBe("Completed");
                var disable = AspireTopologyTests.CreateCommand("tenants", state.TenantId, nameof(DisableTenant), new DisableTenant(state.TenantId));
                var status = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, disable, adminToken, timeout.Token);
                status.Status.ShouldBe("Completed");
                sequence = status.CommittedEventSequence.ShouldNotBeNull();
            }
            var before = await AspireTopologyTests.WaitForPersistedTenantAsync(state.TenantId, ProjectionVersion(sequence), timeout.Token);
            before.Status.ShouldBe(disabled ? TenantStatus.Disabled : TenantStatus.Active);
            before.Configuration[configurationKey].ShouldBe(configurationValue);
            before.Members.ContainsKey(AuthenticatedRouteState.CallerId).ShouldBe(!disabled);
            before.Members[state.TargetUserId].ShouldBe(TenantRole.TenantOwner);
#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
            await consumer.ConsumeThroughAsync(state.TenantId, sequence, timeout.Token);
            await consumer.AssertProjectionAsync(before, sequence, timeout.Token);
#endif
            var request = AspireTopologyTests.CreateCommand(RefreshTenantAccess.Domain, state.TenantId, RefreshTenantAccess.CommandType,
                new RefreshTenantAccess(state.TenantId));
            using (var denied = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commands") { Content = JsonContent.Create(request) })
            {
                denied.Headers.Authorization = new AuthenticationHeaderValue("Bearer", deniedToken);
                denied.Headers.Add("X-Hexalith-Scope", request.Tenant);
                using HttpResponseMessage response = await fixture.CommandApiClient.SendAsync(denied, timeout.Token);
                response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
            }
            (await database.HashGetAsync(eventPrefix + (sequence + 1), "data").WaitAsync(timeout.Token)).IsNull.ShouldBeTrue();
            var recovered = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, request, ownerToken, timeout.Token);
            recovered.Status.ShouldBe("Completed");
            recovered.EventCount.ShouldBe(1);
            recovered.CommittedEventSequence.ShouldBe(sequence + 1);
            sequence++;
            var bytes = await database.HashGetAsync(eventPrefix + sequence, "data").WaitAsync(timeout.Token);
            bytes.IsNull.ShouldBeFalse();
            EventEnvelope stored = JsonSerializer.Deserialize<EventEnvelope>(bytes.ToString(), JsonSerializerOptions.Web).ShouldNotBeNull();
            stored.Identity.ShouldBe(identity);
            stored.SequenceNumber.ShouldBe(sequence);
            stored.EventTypeName.ShouldBe(typeof(TenantAccessRefreshed).FullName);
            stored.UserId.ShouldBe(state.TargetUserId);
            stored.CorrelationId.ShouldBe(recovered.CorrelationId);
            stored.CausationId.ShouldBe(request.MessageId);
            TenantAccessRefreshed snapshot = JsonSerializer.Deserialize<TenantAccessRefreshed>(stored.Payload, JsonSerializerOptions.Web).ShouldNotBeNull();
            snapshot.TenantId.ShouldBe(state.TenantId);
            snapshot.Status.ShouldBe(before.Status);
            snapshot.Members.OrderBy(member => member.Key, StringComparer.Ordinal)
                .ShouldBe(before.Members.OrderBy(member => member.Key, StringComparer.Ordinal));
            snapshot.ConfigurationKeys.ShouldBe([configurationKey]);
            Encoding.UTF8.GetString(stored.Payload).ShouldNotContain(configurationValue);
            TenantReadModel after = await AspireTopologyTests.WaitForPersistedTenantAsync(state.TenantId, ProjectionVersion(sequence), timeout.Token);
            after.Name.ShouldBe(before.Name);
            after.Description.ShouldBe(before.Description);
            after.Status.ShouldBe(before.Status);
            after.Members.OrderBy(member => member.Key, StringComparer.Ordinal)
                .ShouldBe(before.Members.OrderBy(member => member.Key, StringComparer.Ordinal));
            after.Configuration.OrderBy(item => item.Key, StringComparer.Ordinal)
                .ShouldBe(before.Configuration.OrderBy(item => item.Key, StringComparer.Ordinal));
            (await database.HashGetAsync(eventPrefix + (sequence + 1), "data").WaitAsync(timeout.Token)).IsNull.ShouldBeTrue();
#if HEXALITH_CHECKPOINTED_TENANT_PROJECTION
            var delivery = await consumer.ConsumeThroughAsync(state.TenantId, sequence, timeout.Token);
            delivery.MessageId.ShouldBe(stored.MessageId);
            delivery.CorrelationId.ShouldBe(stored.CorrelationId);
            delivery.CausationId.ShouldBe(stored.CausationId);
            delivery.TenantId.ShouldBe(stored.TenantId);
            delivery.Domain.ShouldBe(stored.Domain);
            delivery.EventTypeName.ShouldBe(stored.EventTypeName);
            delivery.Payload.ShouldBe(stored.Payload);
            await consumer.AssertProjectionAsync(after, sequence, timeout.Token);
#endif

            var audit = await AssertReadAsync(tag => readsClient.GetTenantAuditAsync(
                new GetTenantAuditQuery { TenantId = state.TenantId, PageSize = 100 }, tag, timeout.Token),
                page => page.Items.Any(item => item.EventId == stored.MessageId), expectedVersion: after.ProjectionVersion);
            TenantAuditEntry receipt = audit.Items.Single(item => item.EventId == stored.MessageId);
            receipt.EventType.ShouldBe(nameof(TenantAccessRefreshed));
            receipt.ActorId.ShouldBe(state.TargetUserId);
            receipt.NarrativePayload.ShouldHaveSingleItem().ShouldBe(KeyValuePair.Create("operation", RefreshTenantAccess.CommandType));
        }

        static string ProjectionVersion(long sequence)
            => TenantProjectionVersionFormat.SequencePrefix + sequence.ToString(CultureInfo.InvariantCulture);

        string OwnerToken(bool mayRefresh)
        {
            JwtSecurityToken original = new JwtSecurityTokenHandler().ReadJwtToken(state.Token);
            var claims = original.Claims.Where(claim => claim.Type is not ("sub" or "roles" or "eventstore:permission"))
                .Append(new Claim("sub", state.TargetUserId)).Append(new Claim("eventstore:permission", "tenants:read")).ToList();
            if (mayRefresh) claims.Add(new Claim("eventstore:permission", "tenants:refresh-access"));
            var token = new JwtSecurityToken(original.Issuer, original.Audiences.Single(), claims,
                expires: DateTime.UtcNow.AddMinutes(5), signingCredentials: new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(AspireTopologyFixture.DemoSigningKey)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
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
        var globalAudit = await AssertReadAsync(tag => client.GetTenantAuditAsync(
            new GetTenantAuditQuery { TenantId = "system", Stream = "global-administrators", PageSize = 100 }, tag, timeout.Token),
            page => page.Items.Any(item => item.EventType == nameof(GlobalAdministratorSet)));
        globalAudit.Items.ShouldAllBe(item => item.TenantId == "system"
            && (item.EventType == nameof(GlobalAdministratorSet) || item.EventType == nameof(GlobalAdministratorRemoved)));
        using var deniedRequest = new HttpRequestMessage(HttpMethod.Get, "/api/tenants/system/audit");
        deniedRequest.Headers.Add("dapr-app-id", "tenants-api");
        deniedRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", state.Token);
        using HttpResponseMessage denied = await fixture.TenantsUiDaprClient.SendAsync(deniedRequest, timeout.Token);
        denied.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
        await state.WaitForListPageAsync(fixture.TenantsApiClient, 100, timeout.Token);
        string corpusToken = await state.CreateCorpusReadTokenAsync(fixture.TenantsApiClient, timeout.Token);
        using ServiceProvider provider = CreateProductionReadServices(corpusToken);
        using IServiceScope scope = provider.CreateScope();
        ITenantsRestQueryClient client = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
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
        // The gateway may reject this UI-surface token during authentication or
        // after authenticating it; either boundary must refuse the wrong peer.
        new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }.ShouldContain(response.StatusCode);
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
        using var seedTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(7));
        string token = AspireTopologyTests.CreateDemoJwt();
        var tenantIds = new List<string>(20);
        for (int index = 0; index < 20; index++)
        {
            string tenantId = $"populated-page-{Guid.NewGuid():N}";
            tenantIds.Add(tenantId);
            var command = AspireTopologyTests.CreateCommand("tenants", tenantId,
                nameof(Hexalith.Tenants.Contracts.Commands.CreateTenant),
                new Hexalith.Tenants.Contracts.Commands.CreateTenant(tenantId, "Populated page control", "Native authority capacity control"));
            var status = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(fixture.CommandApiClient, command, token, seedTimeout.Token);
            status.Status.ShouldBe("Completed");
            await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId, Hexalith.Tenants.Contracts.Projections.TenantProjectionVersionFormat.SequencePrefix + "1", seedTimeout.Token);
        }

        // Start the measured page after the unchanged one-second consumer window used by command preparation.
        await Task.Delay(TimeSpan.FromSeconds(2), seedTimeout.Token);
        TestContext.Current.TestOutputHelper?.WriteLine("Populated page control: starting one production 20-row load.");
        using var readTimeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        using ServiceProvider provider = CreateProductionReadServices(AspireTopologyTests.CreateDemoJwt(tenantIds));
        using IServiceScope scope = provider.CreateScope();
        using var gateway = new TenantQueryGateway(
            scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>(), new LiveRouteUserContext(),
            scope.ServiceProvider.GetRequiredService<MemoriesClient>(),
            scope.ServiceProvider.GetRequiredService<ITenantSearchCursorCodec>());
        var elapsed = System.Diagnostics.Stopwatch.StartNew();
        TenantListSnapshot result = await gateway.ListTenantsAsync(new TenantListRequest(PageSize: 20), null, readTimeout.Token);
        TestContext.Current.TestOutputHelper?.WriteLine($"Populated page outcome: {result}, KnownCounts={result.Rows.Count(row => row.MemberCount.IsKnown && row.OwnerCount.IsKnown)}, ElapsedMilliseconds={elapsed.Elapsed.TotalMilliseconds:F2}, Cancelled={readTimeout.IsCancellationRequested}.");
        if (result.Rows.Count != 20)
        {
            var raw = await scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>()
                .ListTenantsAsync(new ListTenantsQuery { PageSize = 20 }, null, readTimeout.Token);
            TestContext.Current.TestOutputHelper?.WriteLine($"Populated page transport: HTTP={raw.StatusCode}, Failure={raw.FailureKind}, Provenance={raw.Metadata.Provenance}, Lifecycle={raw.Metadata.Lifecycle}.");
        }
        if (result.IsDegraded)
        {
            ITenantsRestQueryClient reads = scope.ServiceProvider.GetRequiredService<ITenantsRestQueryClient>();
            foreach (string tenantId in result.Rows.Take(3).Select(static row => row.TenantId))
            {
                var detail = await reads.GetTenantAsync(new GetTenantQuery { TenantId = tenantId }, null, readTimeout.Token);
                TestContext.Current.TestOutputHelper?.WriteLine($"Populated page detail: TenantId={tenantId}, HTTP={detail.StatusCode}, Failure={detail.FailureKind}, PayloadPresent={detail.Payload is not null}, Provenance={detail.Metadata.Provenance}, Lifecycle={detail.Metadata.Lifecycle}.");
            }
        }
        result.Rows.Count.ShouldBe(20);
        result.IsDegraded.ShouldBeFalse();
        result.Rows.All(row => row.MemberCount.IsKnown && row.OwnerCount.IsKnown).ShouldBeTrue();
    }

    private ServiceProvider CreateProductionReadServices(string token)
        => CreateProductionReadServices(fixture.TenantsUiDaprClient.BaseAddress!, token);

    /// <summary>Creates the same production readers for live and owned-loopback qualification.</summary>
    /// <param name="tenantsUiDaprEndpoint">The fixture-owned UI invocation endpoint.</param>
    /// <param name="token">The caller's token to relay.</param>
    internal static ServiceProvider CreateProductionReadServices(Uri tenantsUiDaprEndpoint, string token)
    {
        ArgumentNullException.ThrowIfNull(tenantsUiDaprEndpoint);
        // The local sign-in context does not select the gateway's explicit tenant/platform scope.
        FrontComposerTokenSession session = new(Guid.NewGuid().ToString("N"), AuthenticatedRouteState.CallerId, "live-route");
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["DAPR_HTTP_ENDPOINT"] = tenantsUiDaprEndpoint.AbsoluteUri,
        }).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddHexalithFrontComposerTokenRelay();
        services.Configure<FrontComposerAuthenticationOptions>(options => options.TenantClaimTypes.Add("tenant_id"));
        services.AddHexalithTenantsUiModule(configuration, enableGatewayAuthorization: true);
        services.AddSingleton<IHttpContextAccessor>(new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", session.UserId),
                    new Claim("tenant_id", session.TenantId),
                    new Claim(FrontComposerTokenSession.ClaimType, session.SessionId),
                ], "live-route")),
            },
        });
        ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<FrontComposerUserTokenStore>().Set(session, token, DateTimeOffset.UtcNow.AddMinutes(5));
        return provider;
    }

    private static async Task<T> AssertReadAsync<T>(Func<string?, Task<TenantsRestQueryResponse<T>>> read, Func<T, bool>? projected = null, string? expectedVersion = null)
        where T : class
    {
        TenantsRestQueryResponse<T> first = await read(null);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while ((first.Payload is null || (projected is not null && !projected(first.Payload))) && DateTimeOffset.UtcNow < deadline)
        {
            if (first.FailureKind != TenantsRestQueryFailureKind.None)
                TestContext.Current.TestOutputHelper?.WriteLine($"Live production read: HTTP={first.StatusCode}, Failure={first.FailureKind}, Provenance={first.Metadata.Provenance}, Lifecycle={first.Metadata.Lifecycle}.");
            first.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            first = await read(null);
        }

        if (first.FailureKind != TenantsRestQueryFailureKind.None)
            TestContext.Current.TestOutputHelper?.WriteLine($"Live production read: HTTP={first.StatusCode}, Failure={first.FailureKind}, Provenance={first.Metadata.Provenance}, Lifecycle={first.Metadata.Lifecycle}.");
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
