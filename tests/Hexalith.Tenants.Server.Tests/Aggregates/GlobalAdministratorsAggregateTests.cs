using System.Text.Json;

using Hexalith.Commons.UniqueIds;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Server.Aggregates;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Aggregates;

public class GlobalAdministratorsAggregateTests {
    private static CommandEnvelope CreateCommand<T>(T command, string actorUserId = "test-user")
        where T : notnull
        => new(
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            TenantIdentity.DefaultTenantId,
            TenantIdentity.GlobalAdministratorsDomain,
            TenantIdentity.GlobalAdministratorsAggregateId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            UniqueIdHelper.GenerateSortableUniqueStringId(),
            null,
            actorUserId,
            null);

    private static EventEnvelope CreateEventEnvelope<T>(T payload, long sequence)
        where T : notnull
        => new(
            new EventMetadata(
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                TenantIdentity.GlobalAdministratorsDomain,
                "GlobalAdministrators",
                TenantIdentity.DefaultTenantId,
                TenantIdentity.GlobalAdministratorsDomain,
                sequence,
                sequence,
                DateTimeOffset.UtcNow,
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                UniqueIdHelper.GenerateSortableUniqueStringId(),
                "test-user",
                "v1",
                typeof(T).FullName ?? typeof(T).Name,
                1,
                "json"),
            JsonSerializer.SerializeToUtf8Bytes(payload),
            null);

    // Test 1: Bootstrap with no prior state → Success (AC #1)
    [Fact]
    public async Task Bootstrap_with_no_prior_state_produces_GlobalAdministratorSet() {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new BootstrapGlobalAdmin("admin-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        ((GlobalAdministratorSet)evt).TenantId.ShouldBe("system");
        ((GlobalAdministratorSet)evt).UserId.ShouldBe("admin-1");
    }

    [Fact]
    public async Task Two_bootstrap_submissions_against_same_aggregate_produce_one_success_and_one_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope firstCommand = CreateCommand(new BootstrapGlobalAdmin("admin-1"));
        CommandEnvelope secondCommand = CreateCommand(new BootstrapGlobalAdmin("admin-2"));

        DomainResult firstResult = await aggregate.ProcessAsync(firstCommand, currentState: null);
        var state = new GlobalAdministratorsState();
        state.Apply(firstResult.Events[0].ShouldBeOfType<GlobalAdministratorSet>());

        DomainResult secondResult = await aggregate.ProcessAsync(secondCommand, currentState: state);

        firstResult.IsSuccess.ShouldBeTrue();
        firstResult.Events.Count.ShouldBe(1);
        GlobalAdministratorSet created = firstResult.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        created.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        created.UserId.ShouldBe("admin-1");
        secondResult.IsRejection.ShouldBeTrue();
        secondResult.Events.Count.ShouldBe(1);
        _ = secondResult.Events[0].ShouldBeOfType<GlobalAdminAlreadyBootstrappedRejection>();
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
        state.Administrators.ShouldNotContain("admin-2");
    }

    // Test 1b: Bootstrap handler invoked with a literal null state → Success.
    // Exercises the null short-circuit of the `state?.Bootstrapped` guard (the `?.` jump).
    // This is the genuine first-admin bootstrap when the aggregate has never been created.
    [Fact]
    public void Handle_BootstrapGlobalAdmin_with_null_state_produces_GlobalAdministratorSet() {
        DomainResult result = GlobalAdministratorsAggregate.Handle(new BootstrapGlobalAdmin("admin-1"), state: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorSet evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        evt.TenantId.ShouldBe("system");
        evt.UserId.ShouldBe("admin-1");
    }

    // Test 1c: Bootstrap with a non-null state that was never bootstrapped → Success.
    // Exercises the false outcome of the `== true` comparison for a present-but-unbootstrapped
    // state (the other half of the guard's branch, complementing the already-bootstrapped rejection).
    [Fact]
    public void Handle_BootstrapGlobalAdmin_with_unbootstrapped_state_produces_GlobalAdministratorSet() {
        DomainResult result = GlobalAdministratorsAggregate.Handle(new BootstrapGlobalAdmin("admin-1"), state: new GlobalAdministratorsState());

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorSet evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        evt.TenantId.ShouldBe("system");
        evt.UserId.ShouldBe("admin-1");
    }

    // Test 2: Bootstrap when already bootstrapped → Rejection (AC #2)
    [Fact]
    public async Task Bootstrap_when_already_bootstrapped_produces_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new BootstrapGlobalAdmin("admin-2"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<GlobalAdminAlreadyBootstrappedRejection>();
    }

    // Test 3: Set new administrator → Success (AC #3)
    [Fact]
    public async Task Set_new_administrator_produces_GlobalAdministratorSet() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-2"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorSet evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        evt.TenantId.ShouldBe("system");
        evt.UserId.ShouldBe("admin-2");
        evt.ActorUserId.ShouldBe("admin-1");
        evt.SetAt.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    // Test 4: Set existing administrator → duplicate rejection
    [Fact]
    public async Task Set_existing_administrator_produces_duplicate_rejection_without_mutating_state() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-1"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorAlreadyExistsRejection rejection = result.Events[0].ShouldBeOfType<GlobalAdministratorAlreadyExistsRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.UserId.ShouldBe("admin-1");
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
    }

    // Test 5: Remove administrator when multiple exist → Success (AC #4)
    [Fact]
    public async Task Remove_administrator_with_multiple_admins_produces_GlobalAdministratorRemoved() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));
        state.Apply(new GlobalAdministratorSet("system", "admin-2"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"), "admin-2");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorRemoved evt = result.Events[0].ShouldBeOfType<GlobalAdministratorRemoved>();
        evt.TenantId.ShouldBe("system");
        evt.UserId.ShouldBe("admin-1");
        evt.ActorUserId.ShouldBe("admin-2");
        evt.RemovedAt.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));
    }

    [Fact]
    public async Task Removed_administrator_cannot_manage_assignments_in_subsequent_command() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-1"));
        state.Apply(new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-2"));

        CommandEnvelope removeCommand = CreateCommand(new RemoveGlobalAdministrator("admin-1"), "admin-2");
        DomainResult removeResult = await aggregate.ProcessAsync(removeCommand, currentState: state);
        removeResult.IsSuccess.ShouldBeTrue();
        state.Apply(removeResult.Events[0].ShouldBeOfType<GlobalAdministratorRemoved>());

        CommandEnvelope setCommand = CreateCommand(new SetGlobalAdministrator("admin-3"), "admin-1");
        DomainResult setResult = await aggregate.ProcessAsync(setCommand, currentState: state);

        setResult.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = setResult.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.ActorUserId.ShouldBe("admin-1");
        rejection.CommandName.ShouldBe(nameof(SetGlobalAdministrator));
        state.Administrators.ShouldNotContain("admin-1");
        state.Administrators.ShouldNotContain("admin-3");
        state.Administrators.ShouldContain("admin-2");
    }

    // Test 6: Remove last administrator → Rejection (AC #5)
    [Fact]
    public async Task Remove_last_administrator_produces_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<LastGlobalAdministratorRejection>();
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
    }

    // Test 7: Remove nonexistent administrator → not-found rejection
    [Fact]
    public async Task Remove_nonexistent_administrator_produces_not_found_rejection_without_mutating_state() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("nonexistent"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        GlobalAdministratorNotFoundRejection rejection = result.Events[0].ShouldBeOfType<GlobalAdministratorNotFoundRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.UserId.ShouldBe("nonexistent");
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
    }

    // Test 8: Remove with no prior state → not-found rejection
    [Fact]
    public async Task Remove_with_no_prior_state_produces_not_found_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new RemoveGlobalAdministrator("any"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        GlobalAdministratorNotFoundRejection rejection = result.Events[0].ShouldBeOfType<GlobalAdministratorNotFoundRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.UserId.ShouldBe("any");
    }

    // Test 9: Set with no prior state → rejection; bootstrap owns first admin
    [Fact]
    public async Task Set_with_no_prior_state_produces_insufficient_permissions_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-1"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.ActorUserId.ShouldBe("admin-1");
        rejection.CommandName.ShouldBe(nameof(SetGlobalAdministrator));
    }

    // Test 10: State replay — Bootstrap + Set + Remove verifies state transitions (AC #7)
    [Fact]
    public async Task State_replay_tracks_administrators_correctly() {
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
        CommandEnvelope setCmd = CreateCommand(new SetGlobalAdministrator("admin-2"), "admin-1");
        DomainResult setResult = await aggregate.ProcessAsync(setCmd, currentState: state);
        setResult.IsSuccess.ShouldBeTrue();

        state.Apply((GlobalAdministratorSet)setResult.Events[0]);
        state.Administrators.Count.ShouldBe(2);
        state.Administrators.ShouldContain("admin-2");

        // Step 3: Remove first admin
        CommandEnvelope removeCmd = CreateCommand(new RemoveGlobalAdministrator("admin-1"), "admin-2");
        DomainResult removeResult = await aggregate.ProcessAsync(removeCmd, currentState: state);
        removeResult.IsSuccess.ShouldBeTrue();

        state.Apply((GlobalAdministratorRemoved)removeResult.Events[0]);
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldNotContain("admin-1");
        state.Administrators.ShouldContain("admin-2");
        state.Bootstrapped.ShouldBeTrue(); // Bootstrapped stays true
    }

    [Fact]
    public async Task Rehydrate_replays_persisted_rejection_events_without_mutating_state() {
        var aggregate = new GlobalAdministratorsAggregate();
        var currentState = new DomainServiceCurrentState(
            SnapshotState: null,
            Events: [
                CreateEventEnvelope(new GlobalAdministratorSet("system", "admin-1"), 1),
                CreateEventEnvelope(new GlobalAdminAlreadyBootstrappedRejection("system"), 2),
                CreateEventEnvelope(new GlobalAdministratorAlreadyExistsRejection("system", "admin-1"), 3),
                CreateEventEnvelope(new GlobalAdministratorNotFoundRejection("system", "ghost"), 4),
                CreateEventEnvelope(new LastGlobalAdministratorRejection("system", "admin-1"), 5),
            ],
            LastSnapshotSequence: 0,
            CurrentSequence: 5);

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-2"), "admin-1");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState);

        result.IsSuccess.ShouldBeTrue();
        GlobalAdministratorSet evt = result.Events[0].ShouldBeOfType<GlobalAdministratorSet>();
        evt.UserId.ShouldBe("admin-2");
    }

    [Fact]
    public void Applying_persisted_rejection_preserves_bootstrapped_state() {
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin-1"));

        state.Apply(new GlobalAdminAlreadyBootstrappedRejection(TenantIdentity.DefaultTenantId));
        state.Apply(new GlobalAdministratorAlreadyExistsRejection(TenantIdentity.DefaultTenantId, "admin-1"));
        state.Apply(new GlobalAdministratorNotFoundRejection(TenantIdentity.DefaultTenantId, "ghost"));
        state.Apply(new LastGlobalAdministratorRejection(TenantIdentity.DefaultTenantId, "admin-1"));

        state.Bootstrapped.ShouldBeTrue();
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
    }

    [Fact]
    public async Task Set_by_non_administrator_actor_produces_insufficient_permissions_rejection() {
        var aggregate = new GlobalAdministratorsAggregate();
        var state = new GlobalAdministratorsState();
        state.Apply(new GlobalAdministratorSet("system", "admin-1"));

        CommandEnvelope cmd = CreateCommand(new SetGlobalAdministrator("admin-2"), "outsider");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.ActorUserId.ShouldBe("outsider");
        rejection.CommandName.ShouldBe(nameof(SetGlobalAdministrator));
        state.Administrators.Count.ShouldBe(1);
        state.Administrators.ShouldContain("admin-1");
        state.Administrators.ShouldNotContain("admin-2");
    }
}
