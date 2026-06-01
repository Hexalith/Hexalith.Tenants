using System.Reflection;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Fakes;

public class Story62ProductionAggregateParityTests {
    [Fact]
    public void ProcessTenantCommand_matches_TenantAggregate_for_representative_success_paths() {
        var service = new InMemoryTenantService();
        TenantState? aggregateState = null;

        (object Command, string ActorUserId, bool IsGlobalAdmin)[] steps = [
            (new CreateTenant("acme", "Acme", null), "admin", true),
            (new AddUserToTenant("acme", "owner", TenantRole.TenantOwner), "admin", true),
            (new UpdateTenant("acme", "Acme Inc", "Updated"), "owner", false),
            (new AddUserToTenant("acme", "reader", TenantRole.TenantReader), "owner", false),
            (new ChangeUserRole("acme", "reader", TenantRole.TenantContributor), "owner", false),
            (new SetTenantConfiguration("acme", "theme", "dark"), "owner", false),
            (new RemoveTenantConfiguration("acme", "theme"), "owner", false),
            (new DisableTenant("acme"), "admin", true),
            (new EnableTenant("acme"), "admin", true),
        ];

        foreach ((object command, string actorUserId, bool isGlobalAdmin) in steps) {
            CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(
                command,
                aggregateId: "acme",
                userId: actorUserId,
                isGlobalAdmin);

            DomainResult aggregateResult = HandleTenantCommand(command, aggregateState, envelope);
            DomainResult fakeResult = service.ProcessTenantCommand(command, envelope);

            AssertResultsMatchIgnoringTimestamps(aggregateResult, fakeResult);
            aggregateState = ApplyTenantEvents(aggregateState, aggregateResult);
        }

        TenantState? fakeState = service.GetTenantState("acme");
        _ = fakeState.ShouldNotBeNull();
        fakeState.Name.ShouldBe(aggregateState!.Name);
        fakeState.Description.ShouldBe(aggregateState.Description);
        fakeState.Status.ShouldBe(aggregateState.Status);
        fakeState.Users.ShouldBe(aggregateState.Users);
        fakeState.Configuration.ShouldBe(aggregateState.Configuration);
    }

    [Fact]
    public void ProcessCommand_matches_GlobalAdministratorsAggregate_for_success_paths() {
        var service = new InMemoryTenantService();
        GlobalAdministratorsState? aggregateState = null;

        DomainResult bootstrapAggregateResult = GlobalAdministratorsAggregate.Handle(
            new BootstrapGlobalAdmin("admin1"),
            aggregateState);
        DomainResult bootstrapFakeResult = service.ProcessCommand(new BootstrapGlobalAdmin("admin1"));
        AssertResultsMatchIgnoringTimestamps(bootstrapAggregateResult, bootstrapFakeResult);
        aggregateState = ApplyGlobalAdminEvents(aggregateState, bootstrapAggregateResult);

        var setCommand = new SetGlobalAdministrator("admin2");
        CommandEnvelope setEnvelope = TenantTestHelpers.CreateCommandEnvelope(
            setCommand,
            TenantIdentity.GlobalAdministratorsAggregateId,
            "admin1");
        DomainResult setAggregateResult = GlobalAdministratorsAggregate.Handle(setCommand, aggregateState, setEnvelope);
        DomainResult setFakeResult = service.ProcessCommand(setCommand, "admin1");
        AssertResultsMatchIgnoringTimestamps(setAggregateResult, setFakeResult);
        aggregateState = ApplyGlobalAdminEvents(aggregateState, setAggregateResult);

        var removeCommand = new RemoveGlobalAdministrator("admin2");
        CommandEnvelope removeEnvelope = TenantTestHelpers.CreateCommandEnvelope(
            removeCommand,
            TenantIdentity.GlobalAdministratorsAggregateId,
            "admin1");
        DomainResult removeAggregateResult = GlobalAdministratorsAggregate.Handle(removeCommand, aggregateState, removeEnvelope);
        DomainResult removeFakeResult = service.ProcessCommand(removeCommand, "admin1");
        AssertResultsMatchIgnoringTimestamps(removeAggregateResult, removeFakeResult);
        aggregateState = ApplyGlobalAdminEvents(aggregateState, removeAggregateResult);

        GlobalAdministratorsState? fakeState = service.GetGlobalAdminState();
        _ = fakeState.ShouldNotBeNull();
        fakeState.Bootstrapped.ShouldBe(aggregateState!.Bootstrapped);
        fakeState.Administrators.ShouldBe(aggregateState.Administrators);
    }

    [Fact]
    public void GlobalAdministrator_rejection_matches_aggregate_and_does_not_append_event_history() {
        var service = new InMemoryTenantService();
        _ = service.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var aggregateState = new GlobalAdministratorsState();
        aggregateState.Apply(new GlobalAdministratorSet(TenantIdentity.DefaultTenantId, "admin1"));
        int eventCountBefore = service.EventHistory.Count;

        var command = new RemoveGlobalAdministrator("admin1");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(
            command,
            TenantIdentity.GlobalAdministratorsAggregateId,
            "admin1");

        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, aggregateState, envelope);
        DomainResult fakeResult = service.ProcessCommand(command, "admin1");

        AssertResultsMatchIgnoringTimestamps(aggregateResult, fakeResult);
        LastGlobalAdministratorRejection rejection = fakeResult.Events[0].ShouldBeOfType<LastGlobalAdministratorRejection>();
        rejection.TenantId.ShouldBe(TenantIdentity.DefaultTenantId);
        rejection.UserId.ShouldBe("admin1");
        service.EventHistory.Count.ShouldBe(eventCountBefore);
        service.GetGlobalAdminState()!.Administrators.ShouldBe(["admin1"]);
    }

    [Fact]
    public void ProcessTenantCommand_uses_envelope_aggregate_id_when_command_payload_tenant_id_differs() {
        var service = new InMemoryTenantService();
        _ = service.ProcessCommand(new CreateTenant("envelope-tenant", "Envelope Tenant", null));

        var aggregateState = new TenantState();
        aggregateState.Apply(new TenantCreated("envelope-tenant", "Envelope Tenant", null, DateTimeOffset.UtcNow));

        var command = new AddUserToTenant("payload-tenant", "alice", TenantRole.TenantReader);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(
            command,
            aggregateId: "envelope-tenant",
            userId: "admin",
            isGlobalAdmin: true);

        DomainResult aggregateResult = TenantAggregate.Handle(command, aggregateState, envelope);
        DomainResult fakeResult = service.ProcessTenantCommand(command, envelope);

        AssertResultsMatchIgnoringTimestamps(aggregateResult, fakeResult);
        UserAddedToTenant evt = fakeResult.Events[0].ShouldBeOfType<UserAddedToTenant>();
        evt.TenantId.ShouldBe("envelope-tenant");
        service.GetTenantState("payload-tenant").ShouldBeNull();
        service.GetTenantState("envelope-tenant")!.Users["alice"].ShouldBe(TenantRole.TenantReader);
    }

    [Fact]
    public void Rejection_and_noop_results_match_aggregate_and_do_not_append_event_history() {
        var service = new InMemoryTenantService();
        _ = service.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = service.ProcessCommand(
            new AddUserToTenant("acme", "owner", TenantRole.TenantOwner),
            userId: "admin",
            isGlobalAdmin: true);
        _ = service.ProcessCommand(
            new SetTenantConfiguration("acme", "theme", "dark"),
            userId: "owner");

        var aggregateState = new TenantState();
        aggregateState.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        aggregateState.Apply(new UserAddedToTenant("acme", "owner", TenantRole.TenantOwner));
        aggregateState.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        int eventCountBefore = service.EventHistory.Count;

        var rejectedCommand = new AddUserToTenant("acme", "reader", TenantRole.TenantReader);
        CommandEnvelope rejectedEnvelope = TenantTestHelpers.CreateCommandEnvelope(
            rejectedCommand,
            "acme",
            "outsider");
        DomainResult aggregateRejection = TenantAggregate.Handle(rejectedCommand, aggregateState, rejectedEnvelope);
        DomainResult fakeRejection = service.ProcessTenantCommand(rejectedCommand, rejectedEnvelope);

        AssertResultsMatchIgnoringTimestamps(aggregateRejection, fakeRejection);
        InsufficientPermissionsRejection rejection = fakeRejection.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.ActorUserId.ShouldBe("outsider");
        rejection.CommandName.ShouldBe(nameof(AddUserToTenant));
        service.EventHistory.Count.ShouldBe(eventCountBefore);
        service.GetTenantState("acme")!.Users.ShouldNotContainKey("reader");

        var noOpCommand = new SetTenantConfiguration("acme", "theme", "dark");
        CommandEnvelope noOpEnvelope = TenantTestHelpers.CreateCommandEnvelope(
            noOpCommand,
            "acme",
            "owner");
        DomainResult aggregateNoOp = TenantAggregate.Handle(noOpCommand, aggregateState, noOpEnvelope);
        DomainResult fakeNoOp = service.ProcessTenantCommand(noOpCommand, noOpEnvelope);

        AssertResultsMatchIgnoringTimestamps(aggregateNoOp, fakeNoOp);
        fakeNoOp.IsNoOp.ShouldBeTrue();
        service.EventHistory.Count.ShouldBe(eventCountBefore);
        service.GetTenantState("acme")!.Configuration["theme"].ShouldBe("dark");
    }

    private static DomainResult HandleTenantCommand(object command, TenantState? state, CommandEnvelope envelope)
        => command switch {
            CreateTenant c => TenantAggregate.Handle(c, state, envelope),
            DisableTenant c => TenantAggregate.Handle(c, state, envelope),
            EnableTenant c => TenantAggregate.Handle(c, state, envelope),
            UpdateTenant c => TenantAggregate.Handle(c, state, envelope),
            AddUserToTenant c => TenantAggregate.Handle(c, state, envelope),
            RemoveUserFromTenant c => TenantAggregate.Handle(c, state, envelope),
            ChangeUserRole c => TenantAggregate.Handle(c, state, envelope),
            SetTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
            RemoveTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
            _ => throw new InvalidOperationException($"Unsupported tenant command type {command.GetType().Name}."),
        };

    private static TenantState? ApplyTenantEvents(TenantState? state, DomainResult result) {
        if (!result.IsSuccess) {
            return state;
        }

        state ??= new TenantState();

        foreach (IEventPayload evt in result.Events) {
            switch (evt) {
                case TenantCreated e:
                    state.Apply(e);
                    break;
                case TenantUpdated e:
                    state.Apply(e);
                    break;
                case TenantDisabled e:
                    state.Apply(e);
                    break;
                case TenantEnabled e:
                    state.Apply(e);
                    break;
                case UserAddedToTenant e:
                    state.Apply(e);
                    break;
                case UserRemovedFromTenant e:
                    state.Apply(e);
                    break;
                case UserRoleChanged e:
                    state.Apply(e);
                    break;
                case TenantConfigurationSet e:
                    state.Apply(e);
                    break;
                case TenantConfigurationRemoved e:
                    state.Apply(e);
                    break;
            }
        }

        return state;
    }

    private static GlobalAdministratorsState? ApplyGlobalAdminEvents(
        GlobalAdministratorsState? state,
        DomainResult result) {
        if (!result.IsSuccess) {
            return state;
        }

        state ??= new GlobalAdministratorsState();

        foreach (IEventPayload evt in result.Events) {
            switch (evt) {
                case GlobalAdministratorSet e:
                    state.Apply(e);
                    break;
                case GlobalAdministratorRemoved e:
                    state.Apply(e);
                    break;
            }
        }

        return state;
    }

    private static void AssertResultsMatchIgnoringTimestamps(DomainResult expected, DomainResult actual) {
        expected.Events.Count.ShouldBe(actual.Events.Count);
        expected.IsSuccess.ShouldBe(actual.IsSuccess);
        expected.IsRejection.ShouldBe(actual.IsRejection);
        expected.IsNoOp.ShouldBe(actual.IsNoOp);

        for (int i = 0; i < expected.Events.Count; i++) {
            IEventPayload expectedEvent = expected.Events[i];
            IEventPayload actualEvent = actual.Events[i];
            actualEvent.GetType().ShouldBe(expectedEvent.GetType());

            foreach (PropertyInfo property in expectedEvent.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)) {
                if (property.PropertyType == typeof(DateTimeOffset) || property.PropertyType == typeof(DateTimeOffset?)) {
                    continue;
                }

                property.GetValue(actualEvent).ShouldBe(
                    property.GetValue(expectedEvent),
                    $"Property {property.Name} on {expectedEvent.GetType().Name} did not match.");
            }
        }
    }
}
