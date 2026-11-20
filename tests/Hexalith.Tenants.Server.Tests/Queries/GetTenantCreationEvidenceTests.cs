using System.Text.Json;

using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Testing.Fakes;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Projections;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Telemetry;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;

using NSubstitute;

using Shouldly;

using Xunit;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>Separates a retained rejection checkpoint from evidence that a tenant was created.</summary>
public sealed class GetTenantCreationEvidenceTests
{
    private const string Tenant = "vector-create";

    /// <summary>Retains rejection checkpoints while requiring actual creation before a tenant read succeeds.</summary>
    [Fact]
    public async Task RejectedCreationRemainsAbsentUntilActualCreationAndLaterRejectionPreservesIt()
    {
        var store = new InMemoryReadModelStore();
        var projection = new TenantProjectionHandler(store);
        QueryResult before = await QueryAsync(store, administrator: true);
        before.Success.ShouldBeFalse();
        before.ErrorMessage.ShouldBe("Tenant not found");

        ProjectionEventDto rejection = Event(new InsufficientPermissionsRejection(Tenant, "nonmember", null, "CreateTenant"), 1);
        await projection.ProjectAsync(new("system", "tenants", Tenant, [rejection]), TestContext.Current.CancellationToken);
        TenantReadModel checkpoint = (await store.GetAsync<TenantReadModel>("statestore", "projection:tenants:" + Tenant, TestContext.Current.CancellationToken)).Value.ShouldNotBeNull();
        checkpoint.TenantId.ShouldBeEmpty();
        checkpoint.ProjectionVersion.ShouldBe("tenant-sequence:1");
        (await store.GetAsync<TenantIndexReadModel>("statestore", "projection:tenant-index:singleton", TestContext.Current.CancellationToken))
            .Value.ShouldNotBeNull().AggregateSequences[Tenant].ShouldBe(1);
        QueryResult rejected = await QueryAsync(store, administrator: true);
        rejected.Success.ShouldBeFalse();
        rejected.ErrorMessage.ShouldBe("Tenant not found");
        rejected.PayloadBytes.ShouldBeNull();

        ProjectionEventDto creation = Event(new TenantCreated(Tenant, "Created later", null, DateTimeOffset.UnixEpoch), 2);
        await projection.ProjectAsync(new("system", "tenants", Tenant, [rejection, creation]), TestContext.Current.CancellationToken);
        QueryResult created = await QueryAsync(store, administrator: true);
        created.Success.ShouldBeTrue();
        JsonElement detail = JsonSerializer.Deserialize<JsonElement>(created.PayloadBytes!);
        detail.GetProperty("tenantId").GetString().ShouldBe(Tenant);
        detail.GetProperty("name").GetString().ShouldBe("Created later");

        await projection.ProjectAsync(new("system", "tenants", Tenant, [rejection, creation,
            Event(new InsufficientPermissionsRejection(Tenant, "nonmember", null, "UpdateTenant"), 3)]), TestContext.Current.CancellationToken);
        QueryResult afterLaterRejection = await QueryAsync(store, administrator: true);
        afterLaterRejection.Success.ShouldBeTrue();
        JsonSerializer.Deserialize<JsonElement>(afterLaterRejection.PayloadBytes!).GetProperty("name").GetString().ShouldBe("Created later");
    }

    /// <summary>Preserves the existing forbidden response for a caller without administrator authority.</summary>
    [Fact]
    public async Task RejectionCheckpointDoesNotRevealExistenceToNonAdministrator()
    {
        var store = new InMemoryReadModelStore();
        QueryResult before = await QueryAsync(store, administrator: false);
        before.Success.ShouldBeFalse();
        before.ErrorMessage.ShouldBe("Forbidden");
        await new TenantProjectionHandler(store).ProjectAsync(new("system", "tenants", Tenant,
            [Event(new InsufficientPermissionsRejection(Tenant, "nonmember", null, "CreateTenant"), 1)]), TestContext.Current.CancellationToken);

        QueryResult after = await QueryAsync(store, administrator: false);
        after.Success.ShouldBeFalse();
        after.ErrorMessage.ShouldBe("Forbidden");
        after.PayloadBytes.ShouldBeNull();
    }

    /// <summary>Only a created tenant exposes a roster and only to an authorized caller.</summary>
    [Theory]
    [InlineData(true, "Tenant not found")]
    [InlineData(false, "Forbidden")]
    public async Task RosterRequiresCreationAndCurrentAuthority(bool administrator, string expectedError)
    {
        var store = new InMemoryReadModelStore();
        var projection = new TenantProjectionHandler(store);
        var cancellationToken = TestContext.Current.CancellationToken;

        QueryResult absent = await QueryAsync(store, administrator, roster: true);
        absent.Success.ShouldBeFalse();
        absent.ErrorMessage.ShouldBe(expectedError);
        absent.PayloadBytes.ShouldBeNull();

        ProjectionEventDto rejection = Event(new InsufficientPermissionsRejection(Tenant, "principal", null, "CreateTenant"), 1);
        await projection.ProjectAsync(new("system", "tenants", Tenant, [rejection]), cancellationToken);
        QueryResult rejected = await QueryAsync(store, administrator, roster: true);
        rejected.Success.ShouldBeFalse();
        rejected.ErrorMessage.ShouldBe(expectedError);
        rejected.PayloadBytes.ShouldBeNull();

        ProjectionEventDto creation = Event(new TenantCreated(Tenant, "Created later", null, DateTimeOffset.UnixEpoch), 2);
        ProjectionEventDto membership = Event(new UserAddedToTenant(Tenant, "principal", TenantRole.TenantReader), 3);
        await projection.ProjectAsync(new("system", "tenants", Tenant, [rejection, creation, membership]), cancellationToken);
        QueryResult created = await QueryAsync(store, administrator, roster: true);
        created.Success.ShouldBeTrue();
        JsonElement members = JsonSerializer.Deserialize<JsonElement>(created.PayloadBytes!).GetProperty("items");
        members.GetArrayLength().ShouldBe(1);
        members[0].GetProperty("userId").GetString().ShouldBe("principal");
    }

    private static async Task<QueryResult> QueryAsync(InMemoryReadModelStore store, bool administrator, bool roster = false)
    {
        ITenantsGlobalAdministratorAuthority authority = Substitute.For<ITenantsGlobalAdministratorAuthority>();
        authority.IsCurrentGlobalAdministratorAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(administrator);
        var codec = new QueryCursorCodec(new EphemeralDataProtectionProvider(), "tenant-creation-evidence");
        var telemetry = new TenantTelemetry(new EventStoreDomainDiagnostics("tenants"));
        TenantQueryHandlerBase handler = roster
            ? new GetTenantUsersQueryHandler(store, authority, codec, telemetry, NullLogger<GetTenantUsersQueryHandler>.Instance)
            : new GetTenantQueryHandler(store, authority, codec, telemetry, NullLogger<GetTenantQueryHandler>.Instance);
        return await handler.ExecuteAsync(new QueryEnvelope(Tenant, handler.Domain, Tenant,
            handler.QueryType, [], "query-correlation", "principal", Tenant, isGlobalAdmin: true), TestContext.Current.CancellationToken);
    }

    private static ProjectionEventDto Event(object payload, long sequence)
        => new(payload.GetType().FullName!, JsonSerializer.SerializeToUtf8Bytes(payload, payload.GetType()),
            "json", sequence, DateTimeOffset.UnixEpoch, "projection-correlation", "message-" + sequence, "actor");
}
