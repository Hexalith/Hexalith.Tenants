using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Server.Aggregates;
using System.Net.Http.Json;
using System.Text.Json;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.Server.Projections;
using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

public partial class CommandApiRuntimeIntegrationTests
{
    [Fact]
    public async Task NativeRemovalRefusesTheRealProcessAndQueryBoundariesWhilePersistedProjectionStillGrants()
    {
        var source = new MutableGlobalAdministratorSource("test-user", "external-global-admin");
        await using var factory = new CommandApiWebApplicationFactory(administratorSource: source);
        using HttpClient client = factory.CreateClient();
        var granted = await PostProcessAsync(client, CreateProcessRequest(nameof(CreateTenant),
            JsonSerializer.SerializeToUtf8Bytes(new CreateTenant("granted", "Granted", null)), null,
            "test-user", "granted", GlobalAdminExtensions()));
        granted.IsRejection.ShouldBeFalse();
        granted.Events.ShouldContain(item => item.EventTypeName.EndsWith("TenantCreated", StringComparison.Ordinal));
        var grantedQuery = new QueryEnvelope("system", GetGlobalAdministratorsQuery.Domain, "global-administrators",
            GetGlobalAdministratorsQuery.QueryType, [], "before-removal-query", "test-user", isGlobalAdmin: true);
        using (var before = await client.PostAsJsonAsync("/query", grantedQuery))
        {
            before.EnsureSuccessStatusCode();
            (await before.Content.ReadFromJsonAsync<QueryResult>()).ShouldNotBeNull().Success.ShouldBeTrue();
        }
        source.Append(new GlobalAdministratorRemoved("system", "test-user"));
        var request = CreateProcessRequest(nameof(CreateTenant),
            JsonSerializer.SerializeToUtf8Bytes(new CreateTenant("revoked", "Revoked", null)), null,
            "test-user", "revoked", GlobalAdminExtensions());
        var result = await PostProcessAsync(client, request);
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldAllBe(item => !item.EventTypeName.EndsWith("TenantCreated", StringComparison.Ordinal));

        var query = new QueryEnvelope("system", GetGlobalAdministratorsQuery.Domain, "global-administrators",
            GetGlobalAdministratorsQuery.QueryType, [], "revocation-query", "test-user", isGlobalAdmin: true);
        using var response = await client.PostAsJsonAsync("/query", query);
        response.EnsureSuccessStatusCode();
        var read = (await response.Content.ReadFromJsonAsync<QueryResult>()).ShouldNotBeNull();
        read.Success.ShouldBeFalse();
        read.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        var persisted = factory.ReadModelStore.Snapshot<GlobalAdministratorReadModel>("statestore", "projection:global-administrators:singleton");
        persisted.ShouldNotBeNull().Administrators.ShouldContain("test-user");
        factory.ReadModelStore.Snapshot<TenantReadModel>("statestore", "projection:tenants:revoked").ShouldBeNull();
    }
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task IndependentOwnerQueriesSurviveUnavailableGlobalSource(bool users, bool asserted)
    {
        var source = new MutableGlobalAdministratorSource("test-user") { Available = false };
        await using var factory = new CommandApiWebApplicationFactory(administratorSource: source);
        factory.ReadModelStore.SeedRaw("statestore", "projection:tenants:owner-tenant",
            new TenantReadModel
            {
                TenantId = "owner-tenant",
                Name = "Protected owner tenant",
                Members = { ["test-user"] = Hexalith.Tenants.Contracts.Enums.TenantRole.TenantOwner }
            });
        using HttpClient client = factory.CreateClient();
        var query = new QueryEnvelope("system", "tenants", "owner-tenant",
            users ? GetTenantUsersQuery.QueryType : GetTenantQuery.QueryType, [], "owner-source-query", "test-user", isGlobalAdmin: asserted);
        using var response = await client.PostAsJsonAsync("/query", query);
        response.EnsureSuccessStatusCode();
        (await response.Content.ReadFromJsonAsync<QueryResult>()).ShouldNotBeNull().Success.ShouldBeTrue();
        source.ReadCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("list", false)]
    [InlineData("list", true)]
    [InlineData("detail", false)]
    [InlineData("detail", true)]
    [InlineData("users", false)]
    [InlineData("users", true)]
    [InlineData("memberships", false)]
    [InlineData("memberships", true)]
    [InlineData("audit", false)]
    [InlineData("audit", true)]
    [InlineData("administrators", false)]
    [InlineData("administrators", true)]
    public async Task EveryQueryGateRefusesUnavailableGlobalSourceDespiteSpoofedHints(string gate, bool asserted)
    {
        var source = new MutableGlobalAdministratorSource("test-user") { Available = false };
        await using var factory = new CommandApiWebApplicationFactory(administratorSource: source);
        factory.ReadModelStore.SeedRaw("statestore", "projection:tenants:protected-tenant",
            new TenantReadModel { TenantId = "protected-tenant", Name = "Protected tenant" });
        factory.ReadModelStore.SeedRaw("statestore", "projection:tenant-index:singleton",
            new TenantIndexReadModel { Tenants = { ["protected-tenant"] = new("Protected tenant", Hexalith.Tenants.Contracts.Enums.TenantStatus.Active) } });
        using HttpClient client = factory.CreateClient();
        string type = gate switch
        {
            "list" => ListTenantsQuery.QueryType,
            "detail" => GetTenantQuery.QueryType,
            "users" => GetTenantUsersQuery.QueryType,
            "memberships" => GetUserTenantsQuery.QueryType,
            "audit" => GetTenantAuditQuery.QueryType,
            _ => GetGlobalAdministratorsQuery.QueryType
        };
        byte[] payload = gate == "memberships" ? JsonSerializer.SerializeToUtf8Bytes(new GetUserTenantsQuery { UserId = "different-user" }) : [];
        var query = new QueryEnvelope("system", gate == "administrators" ? "global-administrators" : "tenants",
            gate is "list" or "memberships" ? "index" : gate == "administrators" ? "global-administrators" : "protected-tenant",
            type, payload, "unavailable-source-query", "test-user", isGlobalAdmin: asserted);
        using var response = await client.PostAsJsonAsync("/query", query);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.InternalServerError);
        string body = await response.Content.ReadAsStringAsync();
        body.ShouldNotContain("test-user"); body.ShouldNotContain("Protected tenant");
        source.ReadCount.ShouldBe(1);
    }

    [Fact]
    public async Task OriginalNoncanonicalOwnerAuthorityHintRemainsRefusedAfterSdkStripping()
    {
        var source = new MutableGlobalAdministratorSource("test-user");
        await using var factory = new CommandApiWebApplicationFactory(administratorSource: source);
        using HttpClient client = factory.CreateClient();
        var query = new QueryEnvelope("managed-tenant", "tenants", "managed-tenant", ResolveTenantReadAuthority.QueryType,
            "{}"u8.ToArray(), "canonical-owner-query", "test-user", isGlobalAdmin: true)
        {
            IsDelegated = true,
            OriginalActorId = "test-user",
            AuthenticatedWorkloadId = "conversations",
            CallerAuthority = new("delegated-owner-query", [], [])
            { OriginatingContext = new("test-user", "original", false, null, ["projects.read"], ["hexalith-eventstore"]) },
        };
        var claims = Hexalith.EventStore.ServiceDefaults.Authentication.QueryCallerAuthorityBinding.Bind(query)
            .ToDictionary(pair => pair.Key, pair => (object)pair.Value, StringComparer.Ordinal);
        claims["eventstore:surface"] = "service";
        claims[Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.CallerClaimType] = "eventstore";
        claims[Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.OperationClaimType] = "domain-service:query";
        var descriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Issuer = JwtIssuer,
            Audience = WorkloadAudience,
            IssuedAt = DateTime.UtcNow,
            NotBefore = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(4),
            Claims = claims,
            SigningCredentials = new(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(JwtSigningKey)),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256),
        };
        string header = Hexalith.EventStore.ServiceDefaults.Authentication.EventStoreWorkloadAuthenticationDefaults.AssertionHeaderName;
        client.DefaultRequestHeaders.Remove(header);
        client.DefaultRequestHeaders.Add(header, new Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler().CreateToken(descriptor));
        using var response = await client.PostAsJsonAsync("/query", query);
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<QueryResult>()).ShouldNotBeNull();
        result.Success.ShouldBeFalse(); result.ErrorMessage.ShouldBe("safe-denial");
        source.ReadCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("current", true)]
    [InlineData("removed", false)]
    [InlineData("spoofed", false)]
    public async Task GlobalManagementUsesLockedSourceStateWithoutSelfCallsOrWireHintGrants(string actor, bool allowed)
    {
        var source = new MutableGlobalAdministratorSource("current");
        await using var factory = new CommandApiWebApplicationFactory(administratorSource: source);
        using HttpClient client = factory.CreateClient();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "current"));
        state.Apply(new GlobalAdministratorSet("system", "removed"));
        state.Apply(new GlobalAdministratorRemoved("system", "removed"));
        factory.ReadModelStore.SeedRaw("statestore", "projection:global-administrators:singleton",
            new GlobalAdministratorReadModel { Administrators = ["current", "removed", "spoofed"] });
        var request = new DomainServiceRequest(new CommandEnvelope("self-call-message", "system", "global-administrators",
            "global-administrators", nameof(SetGlobalAdministrator), JsonSerializer.SerializeToUtf8Bytes(new SetGlobalAdministrator("target")),
            "self-call-correlation", null, actor, GlobalAdminExtensions()), state);
        var result = await PostProcessAsync(client, request);
        result.IsRejection.ShouldBe(!allowed);
        source.ReadCount.ShouldBe(0);
        if (allowed) result.Events.ShouldHaveSingleItem().EventTypeName.ShouldEndWith("GlobalAdministratorSet");
        else result.Events.ShouldAllBe(item => !item.EventTypeName.EndsWith("GlobalAdministratorSet", StringComparison.Ordinal));
        factory.ReadModelStore.Snapshot<GlobalAdministratorReadModel>("statestore", "projection:global-administrators:singleton")!
            .Administrators.ShouldContain("removed");
    }
}
