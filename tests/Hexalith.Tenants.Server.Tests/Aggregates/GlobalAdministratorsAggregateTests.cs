using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Aggregates;

public class GlobalAdministratorsAggregateTests
{
    private static CommandEnvelope CreateCommand<T>(T command)
        where T : notnull
        => new(
            "system",
            "tenants",
            "global-administrators",
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            "test-user",
            null);

    // Test 1: Bootstrap with no prior state → Success (AC #1)
    [Fact]
    public async Task Bootstrap_with_no_prior_state_produces_GlobalAdministratorSet()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new BootstrapGlobalAdmin("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        ((GlobalAdministratorSet)evt).TenantId.ShouldBe("system");
        ((GlobalAdministratorSet)evt).UserId.ShouldBe("admin-1");
    }

    // Test 2: Bootstrap when already bootstrapped → Rejection (AC #2)
    [Fact]
    public async Task Bootstrap_when_already_bootstrapped_produces_rejection()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new BootstrapGlobalAdmin("admin-2"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<GlobalAdminAlreadyBootstrappedRejection>();
    }

    // Test 3: Set new administrator → Success (AC #3)
    [Fact]
    public async Task Set_new_administrator_produces_GlobalAdministratorSet()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-2"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        ((GlobalAdministratorSet)evt).TenantId.ShouldBe("system");
        ((GlobalAdministratorSet)evt).UserId.ShouldBe("admin-2");
    }

    // Test 4: Set existing administrator → NoOp (AC #3, idempotency)
    [Fact]
    public async Task Set_existing_administrator_produces_NoOp()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 5: Remove administrator when multiple exist → Success (AC #4)
    [Fact]
    public async Task Remove_administrator_with_multiple_admins_produces_GlobalAdministratorRemoved()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));
        state.Apply(new GlobalAdministratorSet("system", "admin-2"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<GlobalAdministratorRemoved>();
        ((GlobalAdministratorRemoved)evt).TenantId.ShouldBe("system");
        ((GlobalAdministratorRemoved)evt).UserId.ShouldBe("admin-1");
    }

    // Test 6: Remove last administrator → Rejection (AC #5)
    [Fact]
    public async Task Remove_last_administrator_produces_rejection()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<LastGlobalAdministratorRejection>();
    }

    // Test 7: Remove nonexistent administrator → NoOp (AC #4, idempotency)
    [Fact]
    public async Task Remove_nonexistent_administrator_produces_NoOp()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("nonexistent"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 8: Remove with no prior state → NoOp (AC #4, edge)
    [Fact]
    public async Task Remove_with_no_prior_state_produces_NoOp()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("any"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 9: Set with no prior state → Success (AC #3, edge)
    [Fact]
    public async Task Set_with_no_prior_state_produces_GlobalAdministratorSet()
    {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        ((GlobalAdministratorSet)evt).TenantId.ShouldBe("system");
        ((GlobalAdministratorSet)evt).UserId.ShouldBe("admin-1");
    }

    // Test 10: State replay — Bootstrap + Set + Remove verifies state transitions (AC #7)
    [Fact]
    public async Task State_replay_tracks_administrators_correctly()
    {
        var aggregate = new GlobalAdministratorsAggregate();

        // Step 1: Bootstrap
        CommandEnvelope bootstrapCmd = CreateCommand(new BootstrapGlobalAdmin("admin-1"));
        DomainResult bootstrapResult = await aggregate.ProcessAsync(bootstrapCmd, currentState: null);
        bootstrapResult.IsSuccess.ShouldBeTrue();

        // Apply to state
        var state = new GlobalAdministratorsState();
        state.Apply((GlobalAdministratorSet)bootstrapResult.Events[0]);
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
        state.Bootstrapped.ShouldBeTrue();

        // Step 2: Set second admin
        CommandEnvelope setCmd = CreateCommand(new SetGlobalAdministrator("admin-2"));
        DomainResult setResult = await aggregate.ProcessAsync(setCmd, currentState: state);
        setResult.IsSuccess.ShouldBeTrue();

        state.Apply((GlobalAdministratorSet)setResult.Events[0]);
        state.Administrators.Count.ShouldBe(2);
        state.Administrators.ShouldContain("admin-2");

        // Step 3: Remove first admin
        CommandEnvelope removeCmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"));
        DomainResult removeResult = await aggregate.ProcessAsync(removeCmd, currentState: state);
        removeResult.IsSuccess.ShouldBeTrue();

        state.Apply((GlobalAdministratorRemoved)removeResult.Events[0]);
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldNotContain("admin-1");
        state.Administrators.ShouldContain("admin-2");
        state.Bootstrapped.ShouldBeTrue(); // Bootstrapped stays true
    }
}
