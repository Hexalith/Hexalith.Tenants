using System.Reflection;
using System.Text.Json;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Aggregates;

public class TenantAggregateTests
{
    private static CommandEnvelope CreateCommand<T>(
        T command,
        string actorUserId = "test-user",
        bool isGlobalAdmin = false)
        where T : notnull
        => new(
            Guid.NewGuid().ToString(),
            "system",
            "tenants",
            ((dynamic)command).TenantId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            actorUserId,
            isGlobalAdmin
                ? new Dictionary<string, string> { ["actor:globalAdmin"] = "true" }
                : null);

    private static TenantState CreateStateWithRoles()
    {
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "owner-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "contributor-user", TenantRole.TenantContributor));
        state.Apply(new UserAddedToTenant("acme", "reader-user", TenantRole.TenantReader));
        return state;
    }

    // ===== Story 2.3: Tenant Lifecycle Handle Method Tests =====

    // Test 1: CreateTenant with no prior state → Success (AC #1)
    [Fact]
    public async Task CreateTenant_with_no_prior_state_produces_TenantCreated()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test tenant"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<TenantCreated>();
        ((TenantCreated)evt).TenantId.ShouldBe("acme");
        ((TenantCreated)evt).Name.ShouldBe("Acme Corp");
        ((TenantCreated)evt).Description.ShouldBe("Test tenant");
        ((TenantCreated)evt).CreatedAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    // Test 2: CreateTenant when tenant already exists → Rejection (AC #2)
    [Fact]
    public async Task CreateTenant_when_tenant_exists_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));

        CommandEnvelope cmd = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantAlreadyExistsRejection>();
    }

    // Test 3: UpdateTenant on active tenant → Success (AC #3)
    [Fact]
    public async Task UpdateTenant_on_active_tenant_produces_TenantUpdated()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantContributor));

        CommandEnvelope cmd = CreateCommand(new UpdateTenant("acme", "New Name", "New Desc"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<TenantUpdated>();
        ((TenantUpdated)evt).TenantId.ShouldBe("acme");
        ((TenantUpdated)evt).Name.ShouldBe("New Name");
        ((TenantUpdated)evt).Description.ShouldBe("New Desc");
    }

    // Test 4: UpdateTenant on non-existent tenant → Rejection (AC #7)
    [Fact]
    public async Task UpdateTenant_on_nonexistent_tenant_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new UpdateTenant("acme", "Name", "Desc"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 5: UpdateTenant on disabled tenant → Rejection (AC #5)
    [Fact]
    public async Task UpdateTenant_on_disabled_tenant_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new UpdateTenant("acme", "New Name", "New Desc"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // Test 6: DisableTenant on active tenant → Success (AC #4)
    [Fact]
    public async Task DisableTenant_on_active_tenant_produces_TenantDisabled()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));

        CommandEnvelope cmd = CreateCommand(new DisableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<TenantDisabled>();
        ((TenantDisabled)evt).TenantId.ShouldBe("acme");
        ((TenantDisabled)evt).DisabledAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    // Test 7: DisableTenant on non-existent tenant → Rejection (AC #7)
    [Fact]
    public async Task DisableTenant_on_nonexistent_tenant_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new DisableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 8: DisableTenant on already disabled tenant → NoOp (AC #4 idempotent)
    [Fact]
    public async Task DisableTenant_on_already_disabled_tenant_produces_NoOp()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new DisableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 9: EnableTenant on disabled tenant → Success (AC #6)
    [Fact]
    public async Task EnableTenant_on_disabled_tenant_produces_TenantEnabled()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new EnableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<TenantEnabled>();
        ((TenantEnabled)evt).TenantId.ShouldBe("acme");
        ((TenantEnabled)evt).EnabledAt.ShouldBeInRange(
            DateTimeOffset.UtcNow.AddSeconds(-5),
            DateTimeOffset.UtcNow.AddSeconds(1));
    }

    // Test 10: EnableTenant on non-existent tenant → Rejection (AC #7)
    [Fact]
    public async Task EnableTenant_on_nonexistent_tenant_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new EnableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 11: EnableTenant on already active tenant → NoOp (AC #6 idempotent)
    [Fact]
    public async Task EnableTenant_on_already_active_tenant_produces_NoOp()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));

        CommandEnvelope cmd = CreateCommand(new EnableTenant("acme"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 12: State replay — Create → Update → Disable → Enable (AC #8)
    [Fact]
    public async Task State_replay_tracks_tenant_lifecycle_correctly()
    {
        var aggregate = new TenantAggregate();

        // Step 1: Create tenant
        CommandEnvelope createCmd = CreateCommand(new CreateTenant("acme", "Acme Corp", "Test tenant"));
        DomainResult createResult = await aggregate.ProcessAsync(createCmd, currentState: null);
        createResult.IsSuccess.ShouldBeTrue();

        var state = new TenantState();
        state.Apply((TenantCreated)createResult.Events[0]);
        state.TenantId.ShouldBe("acme");
        state.Name.ShouldBe("Acme Corp");
        state.Description.ShouldBe("Test tenant");
        state.Status.ShouldBe(TenantStatus.Active);
        state.CreatedAt.ShouldNotBe(default);

        // Add test-user as Contributor so UpdateTenant RBAC passes
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantContributor));

        // Step 2: Update tenant
        CommandEnvelope updateCmd = CreateCommand(new UpdateTenant("acme", "Updated Name", "Updated Desc"));
        DomainResult updateResult = await aggregate.ProcessAsync(updateCmd, currentState: state);
        updateResult.IsSuccess.ShouldBeTrue();

        state.Apply((TenantUpdated)updateResult.Events[0]);
        state.Name.ShouldBe("Updated Name");
        state.Description.ShouldBe("Updated Desc");

        // Step 3: Disable tenant
        CommandEnvelope disableCmd = CreateCommand(new DisableTenant("acme"));
        DomainResult disableResult = await aggregate.ProcessAsync(disableCmd, currentState: state);
        disableResult.IsSuccess.ShouldBeTrue();

        state.Apply((TenantDisabled)disableResult.Events[0]);
        state.Status.ShouldBe(TenantStatus.Disabled);

        // Step 4: Enable tenant
        CommandEnvelope enableCmd = CreateCommand(new EnableTenant("acme"));
        DomainResult enableResult = await aggregate.ProcessAsync(enableCmd, currentState: state);
        enableResult.IsSuccess.ShouldBeTrue();

        state.Apply((TenantEnabled)enableResult.Events[0]);
        state.Status.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public void TenantState_apply_methods_update_users_and_configuration()
    {
        var state = new TenantState();

        state.Apply(new TenantCreated("acme", "Acme Corp", "Test tenant", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));
        state.Apply(new UserRoleChanged("acme", "user-1", TenantRole.TenantReader, TenantRole.TenantContributor));
        state.Apply(new TenantConfigurationSet("acme", "billing.plan", "pro"));

        state.Users["user-1"].ShouldBe(TenantRole.TenantContributor);
        state.Configuration["billing.plan"].ShouldBe("pro");

        state.Apply(new UserRemovedFromTenant("acme", "user-1"));
        state.Apply(new TenantConfigurationRemoved("acme", "billing.plan"));

        state.Users.ContainsKey("user-1").ShouldBeFalse();
        state.Configuration.ContainsKey("billing.plan").ShouldBeFalse();
    }

    // ===== Story 3.1: User-Role Management Handle Method Tests =====

    // Test 13: AddUserToTenant on active tenant → Success for all 3 roles (AC #1)
    // Note: Empty Users dict → bootstrap exception applies, RBAC is skipped
    [Theory]
    [InlineData(TenantRole.TenantOwner)]
    [InlineData(TenantRole.TenantContributor)]
    [InlineData(TenantRole.TenantReader)]
    public async Task AddUserToTenant_on_active_tenant_produces_UserAddedToTenant(TenantRole role)
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));

        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", role));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<UserAddedToTenant>();
        ((UserAddedToTenant)evt).TenantId.ShouldBe("acme");
        ((UserAddedToTenant)evt).UserId.ShouldBe("user-1");
        ((UserAddedToTenant)evt).Role.ShouldBe(role);
    }

    // Test 14: AddUserToTenant on null state → TenantNotFoundRejection (AC #1)
    [Fact]
    public async Task AddUserToTenant_on_null_state_produces_TenantNotFoundRejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", TenantRole.TenantReader));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 15: AddUserToTenant on disabled tenant → TenantDisabledRejection (AC #1)
    [Fact]
    public async Task AddUserToTenant_on_disabled_tenant_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", TenantRole.TenantReader));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // Test 16: AddUserToTenant when user already member → UserAlreadyInTenantRejection (AC #2)
    [Fact]
    public async Task AddUserToTenant_when_user_already_member_produces_UserAlreadyInTenantRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", TenantRole.TenantOwner));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        UserAlreadyInTenantRejection rejection = result.Events[0].ShouldBeOfType<UserAlreadyInTenantRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.UserId.ShouldBe("user-1");
        rejection.ExistingRole.ShouldBe(TenantRole.TenantReader);
    }

    // Test 17: AddUserToTenant with undefined role → RoleEscalationRejection (AC #6)
    // Note: Empty Users dict → bootstrap exception applies, RBAC is skipped → undefined role check fires
    [Fact]
    public async Task AddUserToTenant_with_undefined_role_produces_RoleEscalationRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));

        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", (TenantRole)99));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<RoleEscalationRejection>();
    }

    // Test 18: RemoveUserFromTenant when user is member → Success (AC #3)
    [Fact]
    public async Task RemoveUserFromTenant_when_user_is_member_produces_UserRemovedFromTenant()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(new RemoveUserFromTenant("acme", "user-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        IEventPayload evt = result.Events[0].ShouldBeOfType<UserRemovedFromTenant>();
        ((UserRemovedFromTenant)evt).TenantId.ShouldBe("acme");
        ((UserRemovedFromTenant)evt).UserId.ShouldBe("user-1");
    }

    // Test 19: RemoveUserFromTenant on null state → TenantNotFoundRejection (AC #3)
    [Fact]
    public async Task RemoveUserFromTenant_on_null_state_produces_TenantNotFoundRejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new RemoveUserFromTenant("acme", "user-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 20: RemoveUserFromTenant on disabled tenant → TenantDisabledRejection (AC #3)
    [Fact]
    public async Task RemoveUserFromTenant_on_disabled_tenant_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new RemoveUserFromTenant("acme", "user-1"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // Test 21: RemoveUserFromTenant when user not member → UserNotInTenantRejection (AC #4)
    [Fact]
    public async Task RemoveUserFromTenant_when_user_not_member_produces_UserNotInTenantRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));

        CommandEnvelope cmd = CreateCommand(new RemoveUserFromTenant("acme", "user-2"));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserNotInTenantRejection>();
    }

    // Test 22: ChangeUserRole with valid new role → Success (AC #5)
    [Fact]
    public async Task ChangeUserRole_with_valid_new_role_produces_UserRoleChanged()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-1", TenantRole.TenantContributor));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        var roleEvt = (UserRoleChanged)result.Events[0];
        roleEvt.TenantId.ShouldBe("acme");
        roleEvt.UserId.ShouldBe("user-1");
        roleEvt.OldRole.ShouldBe(TenantRole.TenantReader);
        roleEvt.NewRole.ShouldBe(TenantRole.TenantContributor);
    }

    // Test 23: ChangeUserRole on null state → TenantNotFoundRejection (AC #5)
    [Fact]
    public async Task ChangeUserRole_on_null_state_produces_TenantNotFoundRejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-1", TenantRole.TenantContributor));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // Test 24: ChangeUserRole on disabled tenant → TenantDisabledRejection (AC #5)
    [Fact]
    public async Task ChangeUserRole_on_disabled_tenant_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-1", TenantRole.TenantContributor));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // Test 25: ChangeUserRole when user not member → UserNotInTenantRejection (AC #5)
    [Fact]
    public async Task ChangeUserRole_when_user_not_member_produces_UserNotInTenantRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));

        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-2", TenantRole.TenantContributor));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserNotInTenantRejection>();
    }

    // Test 26: ChangeUserRole with same role → NoOp (AC #5)
    [Fact]
    public async Task ChangeUserRole_with_same_role_produces_NoOp()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-1", TenantRole.TenantReader));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // Test 27: ChangeUserRole with undefined role → RoleEscalationRejection (AC #6)
    [Fact]
    public async Task ChangeUserRole_with_undefined_role_produces_RoleEscalationRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "test-user", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(new ChangeUserRole("acme", "user-1", (TenantRole)99));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<RoleEscalationRejection>();
    }

    // Test 28: AddUserToTenant on disabled tenant with existing member → TenantDisabledRejection (verifies switch arm ordering) (AC #1, #2)
    [Fact]
    public async Task AddUserToTenant_on_disabled_tenant_with_existing_member_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "user-1", TenantRole.TenantReader));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateCommand(new AddUserToTenant("acme", "user-1", TenantRole.TenantOwner));

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // ===== Story 3.2: Role Behavior Enforcement (RBAC) Tests =====

    // R1: AddUserToTenant by Reader → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task RBAC_AddUserToTenant_by_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "new-user", TenantRole.TenantReader),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.ActorUserId.ShouldBe("reader-user");
        rejection.ActorRole.ShouldBe(TenantRole.TenantReader);
        rejection.CommandName.ShouldBe(nameof(AddUserToTenant));
    }

    // R2: AddUserToTenant by Contributor → InsufficientPermissionsRejection (AC #2)
    [Fact]
    public async Task RBAC_AddUserToTenant_by_contributor_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "new-user", TenantRole.TenantReader),
            actorUserId: "contributor-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorRole.ShouldBe(TenantRole.TenantContributor);
    }

    // R3: AddUserToTenant by Owner → Success (AC #3)
    [Fact]
    public async Task RBAC_AddUserToTenant_by_owner_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "new-user", TenantRole.TenantReader),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserAddedToTenant>();
    }

    // R4: AddUserToTenant by GlobalAdmin (not in Users) → Success (AC #6)
    [Fact]
    public async Task RBAC_AddUserToTenant_by_globalAdmin_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "new-user", TenantRole.TenantReader),
            actorUserId: "global-admin",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserAddedToTenant>();
    }

    // R5: AddUserToTenant by non-member → InsufficientPermissionsRejection (AC #5)
    [Fact]
    public async Task RBAC_AddUserToTenant_by_nonMember_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "new-user", TenantRole.TenantReader),
            actorUserId: "unknown-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorUserId.ShouldBe("unknown-user");
        rejection.ActorRole.ShouldBeNull();
    }

    // R6: RemoveUserFromTenant by Reader → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task RBAC_RemoveUserFromTenant_by_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new RemoveUserFromTenant("acme", "contributor-user"),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    // R7: RemoveUserFromTenant by Contributor → InsufficientPermissionsRejection (AC #2)
    [Fact]
    public async Task RBAC_RemoveUserFromTenant_by_contributor_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new RemoveUserFromTenant("acme", "reader-user"),
            actorUserId: "contributor-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    // R8: RemoveUserFromTenant by Owner → Success (AC #3)
    [Fact]
    public async Task RBAC_RemoveUserFromTenant_by_owner_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new RemoveUserFromTenant("acme", "reader-user"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserRemovedFromTenant>();
    }

    // R9: RemoveUserFromTenant by GlobalAdmin → Success (AC #6)
    [Fact]
    public async Task RBAC_RemoveUserFromTenant_by_globalAdmin_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new RemoveUserFromTenant("acme", "reader-user"),
            actorUserId: "global-admin",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserRemovedFromTenant>();
    }

    // R10: ChangeUserRole by Reader → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task RBAC_ChangeUserRole_by_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new ChangeUserRole("acme", "contributor-user", TenantRole.TenantOwner),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    // R11: ChangeUserRole by Contributor → InsufficientPermissionsRejection (AC #2)
    [Fact]
    public async Task RBAC_ChangeUserRole_by_contributor_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new ChangeUserRole("acme", "reader-user", TenantRole.TenantOwner),
            actorUserId: "contributor-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    // R12: ChangeUserRole by Owner → Success (AC #3)
    [Fact]
    public async Task RBAC_ChangeUserRole_by_owner_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new ChangeUserRole("acme", "reader-user", TenantRole.TenantContributor),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        UserRoleChanged evt = result.Events[0].ShouldBeOfType<UserRoleChanged>();
        evt.OldRole.ShouldBe(TenantRole.TenantReader);
        evt.NewRole.ShouldBe(TenantRole.TenantContributor);
    }

    // R13: ChangeUserRole by GlobalAdmin → Success (AC #6)
    [Fact]
    public async Task RBAC_ChangeUserRole_by_globalAdmin_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new ChangeUserRole("acme", "reader-user", TenantRole.TenantContributor),
            actorUserId: "global-admin",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserRoleChanged>();
    }

    // R14: UpdateTenant by Reader → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task RBAC_UpdateTenant_by_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new UpdateTenant("acme", "New Name", "New Desc"),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.CommandName.ShouldBe(nameof(UpdateTenant));
    }

    // R15: UpdateTenant by Contributor → Success (AC #4)
    [Fact]
    public async Task RBAC_UpdateTenant_by_contributor_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new UpdateTenant("acme", "New Name", "New Desc"),
            actorUserId: "contributor-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantUpdated>();
    }

    // R16: UpdateTenant by Owner → Success (AC #3)
    [Fact]
    public async Task RBAC_UpdateTenant_by_owner_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new UpdateTenant("acme", "New Name", "New Desc"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantUpdated>();
    }

    // R17: UpdateTenant by GlobalAdmin → Success (AC #6)
    [Fact]
    public async Task RBAC_UpdateTenant_by_globalAdmin_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new UpdateTenant("acme", "New Name", "New Desc"),
            actorUserId: "global-admin",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantUpdated>();
    }

    // R18: Owner self-removal → Success (AC #3)
    [Fact]
    public async Task RBAC_owner_self_removal_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new RemoveUserFromTenant("acme", "owner-user"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserRemovedFromTenant>();
    }

    // R19: Owner self-demotion → Success (AC #3)
    [Fact]
    public async Task RBAC_owner_self_demotion_succeeds()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRoles();

        CommandEnvelope cmd = CreateCommand(
            new ChangeUserRole("acme", "owner-user", TenantRole.TenantReader),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        UserRoleChanged evt = result.Events[0].ShouldBeOfType<UserRoleChanged>();
        evt.OldRole.ShouldBe(TenantRole.TenantOwner);
        evt.NewRole.ShouldBe(TenantRole.TenantReader);
    }

    // R20: AddUserToTenant on empty tenant (bootstrap) by non-member → Success (AC #5 exception)
    [Fact]
    public async Task RBAC_AddUserToTenant_on_empty_tenant_bootstrap_succeeds()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        // Users dict is empty — first user bootstrap

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "first-owner", TenantRole.TenantOwner),
            actorUserId: "any-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserAddedToTenant>();
    }

    // R20b: After bootstrap, non-owner is rejected
    [Fact]
    public async Task RBAC_after_bootstrap_non_owner_AddUser_is_rejected()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        // Bootstrap: first user becomes Reader (not Owner)
        state.Apply(new UserAddedToTenant("acme", "first-user", TenantRole.TenantReader));

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "second-user", TenantRole.TenantReader),
            actorUserId: "first-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
    }

    [Fact]
    public async Task RBAC_previously_populated_tenant_cannot_reopen_bootstrap_after_becoming_empty()
    {
        var aggregate = new TenantAggregate();
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.Parse("2026-01-15T10:30:00+00:00")));
        state.Apply(new UserAddedToTenant("acme", "first-owner", TenantRole.TenantOwner));
        state.Apply(new UserRemovedFromTenant("acme", "first-owner"));

        CommandEnvelope cmd = CreateCommand(
            new AddUserToTenant("acme", "second-user", TenantRole.TenantOwner),
            actorUserId: "unknown-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorUserId.ShouldBe("unknown-user");
        rejection.ActorRole.ShouldBeNull();
    }

    // ===== Story 3.3: Tenant Configuration Management Tests =====

    private static TenantState CreateStateWithRolesAndConfig()
    {
        TenantState state = CreateStateWithRoles();
        state.Apply(new TenantConfigurationSet("acme", "billing.plan", "pro"));
        return state;
    }

    private static TenantState CreateStateWith100ConfigKeys()
    {
        TenantState state = CreateStateWithRolesAndConfig(); // already has billing.plan
        for (int i = 1; i < 100; i++)
        {
            state.Apply(new TenantConfigurationSet("acme", $"key.{i}", $"value-{i}"));
        }

        // state.Configuration.Count == 100 (billing.plan + key.1..key.99)
        return state;
    }

    // C1: SetTenantConfiguration success — new key (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_new_key_produces_TenantConfigurationSet()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "theme.color", "blue"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        TenantConfigurationSet evt = result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
        evt.TenantId.ShouldBe("acme");
        evt.Key.ShouldBe("theme.color");
        evt.Value.ShouldBe("blue");
    }

    // C2: SetTenantConfiguration overwrite existing key (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_overwrite_existing_key_produces_TenantConfigurationSet()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "billing.plan", "enterprise"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        TenantConfigurationSet evt = result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
        evt.Key.ShouldBe("billing.plan");
        evt.Value.ShouldBe("enterprise");
    }

    // C3: SetTenantConfiguration dot-delimited key (AC #3)
    [Fact]
    public async Task SetTenantConfiguration_dot_delimited_key_is_accepted()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "parties.maxContacts", "500"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        TenantConfigurationSet evt = result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
        evt.Key.ShouldBe("parties.maxContacts");
        evt.Value.ShouldBe("500");
    }

    // C4: SetTenantConfiguration null state → TenantNotFoundRejection (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_null_state_produces_TenantNotFoundRejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", "value"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // C5: SetTenantConfiguration disabled tenant → TenantDisabledRejection (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_disabled_tenant_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", "value"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // C6: SetTenantConfiguration reader → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", "value"),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorUserId.ShouldBe("reader-user");
        rejection.ActorRole.ShouldBe(TenantRole.TenantReader);
        rejection.CommandName.ShouldBe(nameof(SetTenantConfiguration));
    }

    // C7: SetTenantConfiguration contributor → InsufficientPermissionsRejection (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_contributor_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", "value"),
            actorUserId: "contributor-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorUserId.ShouldBe("contributor-user");
        rejection.ActorRole.ShouldBe(TenantRole.TenantContributor);
    }

    // C8: SetTenantConfiguration global admin bypasses RBAC (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_global_admin_bypasses_RBAC()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "theme.color", "red"),
            actorUserId: "admin-user",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
    }

    // C9: SetTenantConfiguration key exceeds 256 chars → ConfigurationLimitExceededRejection (AC #6)
    [Fact]
    public async Task SetTenantConfiguration_key_exceeding_max_length_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        string longKey = new('k', 257);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", longKey, "value"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        ConfigurationLimitExceededRejection rejection = result.Events[0].ShouldBeOfType<ConfigurationLimitExceededRejection>();
        rejection.TenantId.ShouldBe("acme");
        rejection.LimitType.ShouldBe("KeyLength");
        rejection.CurrentCount.ShouldBe(257);
        rejection.MaxAllowed.ShouldBe(256);
    }

    // C10: SetTenantConfiguration value exceeds 1024 chars → ConfigurationLimitExceededRejection (AC #5)
    [Fact]
    public async Task SetTenantConfiguration_value_exceeding_max_length_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        string longValue = new('v', 1025);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", longValue),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        ConfigurationLimitExceededRejection rejection = result.Events[0].ShouldBeOfType<ConfigurationLimitExceededRejection>();
        rejection.LimitType.ShouldBe("ValueSize");
        rejection.CurrentCount.ShouldBe(1025);
        rejection.MaxAllowed.ShouldBe(1024);
    }

    // C11: SetTenantConfiguration 101st key → ConfigurationLimitExceededRejection (AC #4)
    [Fact]
    public async Task SetTenantConfiguration_101st_key_produces_rejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWith100ConfigKeys();
        state.Configuration.Count.ShouldBe(100);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "new.key", "value"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        ConfigurationLimitExceededRejection rejection = result.Events[0].ShouldBeOfType<ConfigurationLimitExceededRejection>();
        rejection.LimitType.ShouldBe("KeyCount");
        rejection.CurrentCount.ShouldBe(100);
        rejection.MaxAllowed.ShouldBe(100);
    }

    // C12: SetTenantConfiguration overwrite existing key when at 100 keys → Success (AC #4)
    [Fact]
    public async Task SetTenantConfiguration_overwrite_at_100_keys_produces_success()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWith100ConfigKeys();
        state.Configuration.Count.ShouldBe(100);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "billing.plan", "enterprise"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        TenantConfigurationSet evt = result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
        evt.Value.ShouldBe("enterprise");
    }

    // C13: SetTenantConfiguration same value → NoOp (AC #1)
    [Fact]
    public async Task SetTenantConfiguration_same_value_produces_NoOp()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "billing.plan", "pro"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // C14: RemoveTenantConfiguration success (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_existing_key_produces_TenantConfigurationRemoved()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "billing.plan"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        TenantConfigurationRemoved evt = result.Events[0].ShouldBeOfType<TenantConfigurationRemoved>();
        evt.TenantId.ShouldBe("acme");
        evt.Key.ShouldBe("billing.plan");
    }

    // C15: RemoveTenantConfiguration null state (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_null_state_produces_TenantNotFoundRejection()
    {
        var aggregate = new TenantAggregate();
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "billing.plan"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantNotFoundRejection>();
    }

    // C16: RemoveTenantConfiguration disabled tenant (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_disabled_tenant_produces_TenantDisabledRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "billing.plan"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // C17: RemoveTenantConfiguration reader → InsufficientPermissionsRejection (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_reader_produces_InsufficientPermissionsRejection()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "billing.plan"),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        InsufficientPermissionsRejection rejection = result.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorUserId.ShouldBe("reader-user");
        rejection.CommandName.ShouldBe(nameof(RemoveTenantConfiguration));
    }

    // C18: RemoveTenantConfiguration non-existent key → NoOp (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_nonexistent_key_produces_NoOp()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "nonexistent"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsNoOp.ShouldBeTrue();
        result.Events.Count.ShouldBe(0);
    }

    // C19: RemoveTenantConfiguration global admin bypasses RBAC (AC #2)
    [Fact]
    public async Task RemoveTenantConfiguration_global_admin_bypasses_RBAC()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        CommandEnvelope cmd = CreateCommand(
            new RemoveTenantConfiguration("acme", "billing.plan"),
            actorUserId: "admin-user",
            isGlobalAdmin: true);

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantConfigurationRemoved>();
    }

    // C20: SetTenantConfiguration disabled tenant by reader → TenantDisabledRejection (switch arm ordering)
    [Fact]
    public async Task SetTenantConfiguration_disabled_tenant_by_reader_produces_TenantDisabledRejection_not_RBAC()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", "value"),
            actorUserId: "reader-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // C21: SetTenantConfiguration key exactly 256 chars → Success (boundary test, AC #6)
    [Fact]
    public async Task SetTenantConfiguration_key_exactly_at_max_length_produces_success()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        string maxKey = new('k', 256);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", maxKey, "value"),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
    }

    // C22: SetTenantConfiguration value exactly 1024 chars → Success (boundary test, AC #5)
    [Fact]
    public async Task SetTenantConfiguration_value_exactly_at_max_length_produces_success()
    {
        var aggregate = new TenantAggregate();
        TenantState state = CreateStateWithRolesAndConfig();
        string maxValue = new('v', 1024);
        CommandEnvelope cmd = CreateCommand(
            new SetTenantConfiguration("acme", "key", maxValue),
            actorUserId: "owner-user");

        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantConfigurationSet>();
    }

    // Task 3.7: TenantRole enum ordinal regression test
    [Fact]
    public void TenantRole_ordinal_values_maintain_privilege_hierarchy()
    {
        ((int)TenantRole.TenantOwner).ShouldBeLessThan((int)TenantRole.TenantContributor);
        ((int)TenantRole.TenantContributor).ShouldBeLessThan((int)TenantRole.TenantReader);
        Enum.GetValues<TenantRole>().Length.ShouldBe(3);
    }

    // Task 3.10: 3-param Handle method discovery guard
    [Fact]
    public void TenantAggregate_exposes_three_param_Handle_methods()
    {
        MethodInfo[] handleMethods = typeof(TenantAggregate)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(m => m.Name == "Handle" && m.GetParameters().Length == 3)
            .ToArray();

        handleMethods.Length.ShouldBeGreaterThanOrEqualTo(1,
            "TenantAggregate must have at least one 3-param Handle(Command, State?, CommandEnvelope) method for RBAC enforcement");
    }
}
