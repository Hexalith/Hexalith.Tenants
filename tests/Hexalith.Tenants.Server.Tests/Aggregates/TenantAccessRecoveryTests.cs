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

    [Fact]
    public async Task RejectionOnlyHistoryStillReturnsNotFoundAndPreservesPayloadTargetRefusal()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantNotFoundRejection("acme"));
        state.TenantId.ShouldBeEmpty();
        string before = JsonSerializer.Serialize(state);
        var result = await aggregate.ProcessAsync(Envelope("owner"), state);
        result.IsRejection.ShouldBeTrue();
        result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantNotFoundRejection>().TenantId.ShouldBe("acme");
        CommandEnvelope substituted = Envelope("owner") with { Payload = JsonSerializer.SerializeToUtf8Bytes(new RefreshTenantAccess("foreign")) };
        (await aggregate.ProcessAsync(substituted, state)).Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InsufficientPermissionsRejection>();
        JsonSerializer.Serialize(state).ShouldBe(before);

        var firstResult = await aggregate.ProcessAsync(Envelope("owner"), currentState: null);
        firstResult.IsRejection.ShouldBeTrue();
        firstResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantNotFoundRejection>().TenantId.ShouldBe("acme");

        var replayedResult = await aggregate.ProcessAsync(Envelope("owner"), currentState: firstResult.Events);
        replayedResult.IsRejection.ShouldBeTrue();
        replayedResult.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantNotFoundRejection>().TenantId.ShouldBe("acme");

        var replayedSubstituted = await aggregate.ProcessAsync(substituted, currentState: firstResult.Events);
        replayedSubstituted.IsRejection.ShouldBeTrue();
        replayedSubstituted.Events.ShouldHaveSingleItem().ShouldBeOfType<InsufficientPermissionsRejection>().TenantId.ShouldBe("acme");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ExactEntryBudgetIsInclusiveAndRejectsTheNextEntryAtomically(int offset)
    {
        TenantState state = State();
        state.Users.Clear();
        state.Users["owner"] = TenantRole.TenantOwner;
        int target = ProjectionSnapshotLimits.Default.MaxEntries + offset;
        for (int i = 1; state.Users.Count + state.Configuration.Count < target; i++)
        {
            state.Users["reader-" + i] = TenantRole.TenantReader;
        }
        string before = JsonSerializer.Serialize(state);
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);
        result.IsSuccess.ShouldBe(offset <= 0);
        if (offset <= 0)
        {
            TenantAccessRefreshed snapshot = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantAccessRefreshed>();
            (snapshot.Members.Count + snapshot.ConfigurationKeys.Count).ShouldBe(target);
            state.Apply(snapshot);
        }
        else
        {
            TenantAccessSnapshotLimitExceededRejection rejection = result.Events.ShouldHaveSingleItem()
                .ShouldBeOfType<TenantAccessSnapshotLimitExceededRejection>();
            rejection.Limit.ShouldBe("entries");
            rejection.Actual.ShouldBe(target);
            rejection.Maximum.ShouldBe(ProjectionSnapshotLimits.Default.MaxEntries);
            state.Apply(rejection);
        }
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    public async Task ExactSerializedByteBudgetUsesTheActualSnapshotJsonAndRejectsAtomically(int offset)
    {
        TenantState state = State();
        // ASCII padding grows the actual default event JSON by exactly one UTF-8 byte per character.
        state.Users["padding"] = TenantRole.TenantReader;
        int initial = JsonSerializer.SerializeToUtf8Bytes(new TenantAccessRefreshed(
            "acme", state.Status, state.Users, state.Configuration.Keys.ToArray())).Length;
        state.Users.Remove("padding");
        int target = ProjectionSnapshotLimits.Default.MaxSerializedBytes + offset;
        state.Users["padding" + new string('a', target - initial)] = TenantRole.TenantReader;
        JsonSerializer.SerializeToUtf8Bytes(new TenantAccessRefreshed(
            "acme", state.Status, state.Users, state.Configuration.Keys.ToArray())).Length.ShouldBe(target);
        string before = JsonSerializer.Serialize(state);
        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);
        result.IsSuccess.ShouldBe(offset <= 0);
        if (offset <= 0)
        {
            TenantAccessRefreshed snapshot = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantAccessRefreshed>();
            JsonSerializer.SerializeToUtf8Bytes(snapshot).Length.ShouldBe(target);
            state.Apply(snapshot);
        }
        else
        {
            TenantAccessSnapshotLimitExceededRejection rejection = result.Events.ShouldHaveSingleItem()
                .ShouldBeOfType<TenantAccessSnapshotLimitExceededRejection>();
            rejection.Limit.ShouldBe("serialized-bytes");
            rejection.Actual.ShouldBe(target);
            rejection.Maximum.ShouldBe(ProjectionSnapshotLimits.Default.MaxSerializedBytes);
            state.Apply(rejection);
        }
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Theory]
    [InlineData(TenantStatus.Unknown)]
    [InlineData((TenantStatus)99)]
    public async Task MalformedHistoricalStatusRejectsWithoutEmittingSnapshot(TenantStatus malformedStatus)
    {
        TenantState state = State(malformedStatus);
        string before = JsonSerializer.Serialize(state);

        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);

        result.IsRejection.ShouldBeTrue();
        TenantAccessStateMalformedRejection rejection = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<TenantAccessStateMalformedRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.Field.ShouldBe("status");
        JsonSerializer.Serialize(rejection).ShouldNotContain("secret-value");
        state.Apply(rejection);
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Theory]
    [InlineData("empty-key")]
    [InlineData("whitespace-key")]
    [InlineData("unknown-role")]
    [InlineData("undefined-role")]
    [InlineData("null-users")]
    public async Task MalformedHistoricalMembersRejectWithoutEmittingSnapshotOrLeakingRawKeys(string shape)
    {
        TenantState state = State();
        bool useGlobalAdmin = false;
        switch (shape)
        {
            case "empty-key":
                state.Users[string.Empty] = TenantRole.TenantReader;
                break;
            case "whitespace-key":
                state.Users["   "] = TenantRole.TenantReader;
                break;
            case "unknown-role":
                state.Users["corrupt-member-key"] = TenantRole.Unknown;
                break;
            case "undefined-role":
                state.Users["corrupt-member-key"] = (TenantRole)99;
                break;
            case "null-users":
                state.Users = null!;
                useGlobalAdmin = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }

        string before = JsonSerializer.Serialize(state);

        var result = await new TenantAggregate().ProcessAsync(
            Envelope(useGlobalAdmin ? "administrator" : "owner", hint: useGlobalAdmin),
            state);

        result.IsRejection.ShouldBeTrue();
        TenantAccessStateMalformedRejection rejection = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<TenantAccessStateMalformedRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.Field.ShouldBe("members");
        string serialized = JsonSerializer.Serialize(rejection);
        serialized.ShouldNotContain("corrupt-member-key");
        serialized.ShouldNotContain("secret-value");
        state.Apply(rejection);
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Theory]
    [InlineData("empty-key")]
    [InlineData("whitespace-key")]
    [InlineData("null-configuration")]
    public async Task MalformedHistoricalConfigurationKeysRejectWithoutEmittingSnapshotOrLeakingSecrets(string shape)
    {
        TenantState state = State();
        switch (shape)
        {
            case "empty-key":
                state.Configuration[string.Empty] = "top-secret-config-value";
                break;
            case "whitespace-key":
                state.Configuration[" \t "] = "top-secret-config-value";
                break;
            case "null-configuration":
                state.Configuration = null!;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape));
        }

        string before = JsonSerializer.Serialize(state);

        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);

        result.IsRejection.ShouldBeTrue();
        TenantAccessStateMalformedRejection rejection = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<TenantAccessStateMalformedRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.Field.ShouldBe("configurationKeys");
        string serialized = JsonSerializer.Serialize(rejection);
        serialized.ShouldNotContain("top-secret-config-value");
        serialized.ShouldNotContain("secret-value");
        state.Apply(rejection);
        JsonSerializer.Serialize(state).ShouldBe(before);
    }

    [Fact]
    public async Task UnprivilegedCallerStillReceivesInsufficientPermissionsEvenOnMalformedState()
    {
        TenantState state = State(TenantStatus.Unknown);
        state.Users = null!;

        var result = await new TenantAggregate().ProcessAsync(Envelope("reader"), state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.ActorRole.ShouldBeNull();
    }

    [Theory]
    [InlineData("reader", TenantRole.TenantReader)]
    [InlineData("unknown-member", TenantRole.Unknown)]
    public async Task UnprivilegedOrUnknownRoleCallerOnNonNullMalformedUsersReceivesInsufficientPermissions(
        string actor,
        TenantRole expectedRole)
    {
        TenantState state = State(TenantStatus.Unknown);
        state.Users["unknown-member"] = TenantRole.Unknown;
        state.Users[" "] = TenantRole.TenantReader;

        var result = await new TenantAggregate().ProcessAsync(Envelope(actor), state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events.ShouldHaveSingleItem()
            .ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.ActorRole.ShouldBe(expectedRole);
    }

    [Fact]
    public async Task ReplayingMalformedStateRejectionThroughProcessAsyncPreservesAuthoritativeState()
    {
        DateTimeOffset createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        object[] history =
        [
            new TenantCreated("acme", "Acme", "unchanged", createdAt),
            new UserAddedToTenant("acme", "owner", TenantRole.TenantOwner),
            new TenantConfigurationSet("acme", "projects.allowed", "true"),
            new TenantAccessStateMalformedRejection("acme", "members"),
        ];

        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), currentState: history);

        result.IsSuccess.ShouldBeTrue();
        TenantAccessRefreshed snapshot = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantAccessRefreshed>();
        snapshot.TenantId.ShouldBe("acme");
        snapshot.Status.ShouldBe(TenantStatus.Active);
        snapshot.Members.ShouldBe(new Dictionary<string, TenantRole> { ["owner"] = TenantRole.TenantOwner });
        snapshot.ConfigurationKeys.ShouldBe(["projects.allowed"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MalformedStatusPrecedesEntryBudgetWhileEntryBudgetPrecedesElementLevelValidation(bool malformedStatus)
    {
        TenantState state = State(malformedStatus ? TenantStatus.Unknown : TenantStatus.Active);
        state.Users[" "] = TenantRole.Unknown;
        state.Configuration[" "] = "secret-value";
        for (int i = 0; i <= ProjectionSnapshotLimits.Default.MaxEntries; i++)
        {
            state.Users["reader-" + i] = TenantRole.TenantReader;
        }

        var result = await new TenantAggregate().ProcessAsync(Envelope("owner"), state);

        result.IsRejection.ShouldBeTrue();
        if (malformedStatus)
        {
            TenantAccessStateMalformedRejection rejection = result.Events.ShouldHaveSingleItem()
                .ShouldBeOfType<TenantAccessStateMalformedRejection>();
            rejection.TenantId.ShouldBe("acme");
            rejection.Field.ShouldBe("status");
        }
        else
        {
            TenantAccessSnapshotLimitExceededRejection rejection = result.Events.ShouldHaveSingleItem()
                .ShouldBeOfType<TenantAccessSnapshotLimitExceededRejection>();
            rejection.TenantId.ShouldBe("acme");
            rejection.Limit.ShouldBe("entries");
        }
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
