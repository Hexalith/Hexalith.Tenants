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
    private static CommandEnvelope CreateCommand<T>(T command)
        where T : notnull
        => new(
            "system",
            "tenants",
            ((dynamic)command).TenantId,
            typeof(T).Name,
            JsonSerializer.SerializeToUtf8Bytes(command),
            Guid.NewGuid().ToString(),
            null,
            "test-user",
            null);

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
}
