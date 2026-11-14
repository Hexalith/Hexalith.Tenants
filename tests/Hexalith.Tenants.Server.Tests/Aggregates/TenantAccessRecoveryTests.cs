using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.Tenants.Authorization;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;

using Microsoft.Extensions.DependencyInjection;

using NSubstitute;
using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Aggregates;

public sealed class TenantAccessRecoveryTests
{
    [Theory]
    [InlineData(TenantStatus.Active)]
    [InlineData(TenantStatus.Disabled)]
    public async Task CurrentOwnerEmitsExactBoundedSnapshotAndReplayPreservesEveryBusinessField(TenantStatus status)
    {
        TenantState state = State(status);
        string before = JsonSerializer.Serialize(state);
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);
        result.IsSuccess.ShouldBeTrue();
        TenantAccessRefreshed snapshot = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantAccessRefreshed>();
        snapshot.TenantId.ShouldBe("acme");
        snapshot.Status.ShouldBe(status);
        snapshot.Members.OrderBy(member => member.Key).ShouldBe(state.Users.OrderBy(member => member.Key));
        snapshot.ConfigurationKeys.ShouldBe(["billing.secret", "projects.allowed"]);
        JsonSerializer.Serialize(snapshot).ShouldNotContain("secret-value");
        state.Apply(snapshot);
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Theory]
    [InlineData("contributor")]
    [InlineData("reader")]
    [InlineData("foreign")]
    public async Task UnprivilegedActorsCannotEmitRecoveryEvidence(string actor)
    {
        var result = await new TenantAggregate().ProcessAsync(Envelope(actor), State());
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    [Fact]
    public void AnonymousIdentityCannotFormAnAdmittedCommandEnvelope()
        => Should.Throw<ArgumentException>(() => Envelope(string.Empty));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AdministratorWireHintIsRebuiltFromVerifiedAuthorityBeforeDispatch(bool verified)
    {
        var authority = Substitute.For<ITenantsGlobalAdministratorAuthority>();
        authority.IsCurrentGlobalAdministratorAsync("administrator", Arg.Any<CancellationToken>()).Returns(verified);
        using var provider = new ServiceCollection()
            .AddSingleton(authority)
            .AddSingleton<IDomainServiceAdministratorVerifier, TenantsGlobalAdministratorVerifier>()
            .BuildServiceProvider();
        var request = new DomainServiceRequest(Envelope("administrator", true), new object());
        DomainServiceRequest rebuilt = await DomainServiceAdministratorAssertions.RebuildAsync(provider, request, TestContext.Current.CancellationToken);
        var result = await new TenantAggregate().ProcessAsync(rebuilt.Command, State(TenantStatus.Disabled));
        result.IsSuccess.ShouldBe(verified);
        result.Events.Any(payload => payload is TenantAccessRefreshed).ShouldBe(verified);
        if (verified) result.Events[0].ShouldBeOfType<TenantAccessRefreshed>().Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public async Task RecoveryCannotSubstituteTheAdmittedTenantEvenForAnOwner()
    {
        CommandEnvelope command = Envelope("owner") with { Payload = JsonSerializer.SerializeToUtf8Bytes(new RefreshTenantAccess("foreign")) };
        var result = await new TenantAggregate().ProcessAsync(command, State());
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    [Fact]
    public async Task RecoveryCannotReadForeignAggregateStateEvenWhenTheActorIsItsOwner()
    {
        TenantState state = State();
        state.TenantId = "foreign";
        string before = JsonSerializer.Serialize(state);
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<InsufficientPermissionsRejection>().TenantId.ShouldBe("acme");
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Fact]
    public async Task UnknownTenantCannotBeManufacturedByRecovery()
    {
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), null);
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantNotFoundRejection>();
    }

    [Theory]
    [InlineData("entries")]
    [InlineData("serialized-bytes")]
    public async Task OversizedSnapshotRejectsAtomicallyWithoutChangingBusinessState(string budget)
    {
        TenantState state = State();
        if (budget == "entries")
        {
            for (int i = 0; i <= ProjectionSnapshotLimits.Default.MaxEntries; i++) state.Users["reader-" + i] = TenantRole.TenantReader;
        }
        else
        {
            state.Users[new string('中', ProjectionSnapshotLimits.Default.MaxSerializedBytes)] = TenantRole.TenantReader;
        }

        string before = JsonSerializer.Serialize(state);
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);
        result.IsRejection.ShouldBeTrue();
        TenantAccessSnapshotLimitExceededRejection rejection = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantAccessSnapshotLimitExceededRejection>();
        rejection.Limit.ShouldBe(budget);
        state.Apply(rejection);
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    private static TenantState State(TenantStatus status = TenantStatus.Active)
        => new()
        {
            TenantId = "acme", Name = "Acme", Description = "unchanged", Status = status,
            CreatedAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z"), HasMembershipHistory = true,
            Users = new() { ["owner"] = TenantRole.TenantOwner, ["contributor"] = TenantRole.TenantContributor, ["reader"] = TenantRole.TenantReader },
            Configuration = new() { ["billing.secret"] = "secret-value", ["projects.allowed"] = "true" },
        };

    private static CommandEnvelope Envelope(string actor, bool hint = false)
        => new("01ARZ3NDEKTSV4RRFFQ69G5FAV", "system", "tenants", "acme", nameof(RefreshTenantAccess),
            JsonSerializer.SerializeToUtf8Bytes(new RefreshTenantAccess("acme")), "01ARZ3NDEKTSV4RRFFQ69G5FAW", null, actor,
            hint ? new Dictionary<string, string> { ["actor:globalAdmin"] = "true" } : null);
}
