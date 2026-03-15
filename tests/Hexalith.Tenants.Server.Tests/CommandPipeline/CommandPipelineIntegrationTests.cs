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

namespace Hexalith.Tenants.Server.Tests.CommandPipeline;

/// <summary>
/// End-to-end command pipeline integration tests exercising the full
/// ProcessAsync path (the same path the AggregateActor takes at Step 4).
/// Validates AC #10 from Story 2.4.
/// </summary>
public class CommandPipelineIntegrationTests
{
    // --- Bootstrap pipeline (AC #10, Task 4.1) ---

    [Fact]
    public async Task Bootstrap_then_second_bootstrap_produces_rejection()
    {
        var aggregate = new GlobalAdministratorsAggregate();

        // First bootstrap — should succeed
        CommandEnvelope firstCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-1"));
        DomainResult firstResult = await aggregate.ProcessAsync(firstCmd, currentState: null);
        firstResult.IsSuccess.ShouldBeTrue();

        // Apply events to build state
        var state = new GlobalAdministratorsState();
        state.Apply((GlobalAdministratorSet)firstResult.Events[0]);

        // Second bootstrap — should be rejected
        CommandEnvelope secondCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-2"));
        DomainResult secondResult = await aggregate.ProcessAsync(secondCmd, currentState: state);
        secondResult.IsRejection.ShouldBeTrue();
        secondResult.Events[0].ShouldBeOfType<GlobalAdminAlreadyBootstrappedRejection>();
    }

    // --- CreateTenant end-to-end (AC #10, Task 4.2) ---

    [Fact]
    public async Task CreateTenant_end_to_end_produces_TenantCreated_event()
    {
        var aggregate = new TenantAggregate();

        CommandEnvelope cmd = CreateTenantCommand(new CreateTenant("acme", "Acme Corp", "Enterprise tenant"));
        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: null);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        TenantCreated evt = result.Events[0].ShouldBeOfType<TenantCreated>();
        evt.TenantId.ShouldBe("acme");
        evt.Name.ShouldBe("Acme Corp");
        evt.Description.ShouldBe("Enterprise tenant");
    }

    // --- DisableTenant / EnableTenant end-to-end (AC #10, Task 4.3) ---

    [Fact]
    public async Task DisableTenant_end_to_end_produces_TenantDisabled_event()
    {
        var aggregate = new TenantAggregate();

        // Setup: create tenant first
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateTenantCommand(new DisableTenant("acme"));
        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        TenantDisabled evt = result.Events[0].ShouldBeOfType<TenantDisabled>();
        evt.TenantId.ShouldBe("acme");
    }

    [Fact]
    public async Task EnableTenant_end_to_end_produces_TenantEnabled_event()
    {
        var aggregate = new TenantAggregate();

        // Setup: create and disable tenant
        var state = new TenantState();
        state.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.UtcNow));
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        CommandEnvelope cmd = CreateTenantCommand(new EnableTenant("acme"));
        DomainResult result = await aggregate.ProcessAsync(cmd, currentState: state);

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        TenantEnabled evt = result.Events[0].ShouldBeOfType<TenantEnabled>();
        evt.TenantId.ShouldBe("acme");
    }

    [Fact]
    public async Task Full_tenant_lifecycle_end_to_end()
    {
        var tenantAggregate = new TenantAggregate();
        var globalAggregate = new GlobalAdministratorsAggregate();

        // Step 1: Bootstrap global admin
        CommandEnvelope bootstrapCmd = CreateGlobalAdminCommand(new BootstrapGlobalAdmin("admin-1"));
        DomainResult bootstrapResult = await globalAggregate.ProcessAsync(bootstrapCmd, currentState: null);
        bootstrapResult.IsSuccess.ShouldBeTrue();

        // Step 2: Create tenant
        CommandEnvelope createCmd = CreateTenantCommand(new CreateTenant("acme", "Acme Corp", "Enterprise"));
        DomainResult createResult = await tenantAggregate.ProcessAsync(createCmd, currentState: null);
        createResult.IsSuccess.ShouldBeTrue();

        var tenantState = new TenantState();
        tenantState.Apply((TenantCreated)createResult.Events[0]);
        tenantState.Status.ShouldBe(TenantStatus.Active);

        // Step 3: Disable tenant
        CommandEnvelope disableCmd = CreateTenantCommand(new DisableTenant("acme"));
        DomainResult disableResult = await tenantAggregate.ProcessAsync(disableCmd, currentState: tenantState);
        disableResult.IsSuccess.ShouldBeTrue();

        tenantState.Apply((TenantDisabled)disableResult.Events[0]);
        tenantState.Status.ShouldBe(TenantStatus.Disabled);

        // Step 4: Enable tenant
        CommandEnvelope enableCmd = CreateTenantCommand(new EnableTenant("acme"));
        DomainResult enableResult = await tenantAggregate.ProcessAsync(enableCmd, currentState: tenantState);
        enableResult.IsSuccess.ShouldBeTrue();

        tenantState.Apply((TenantEnabled)enableResult.Events[0]);
        tenantState.Status.ShouldBe(TenantStatus.Active);
    }

    private static CommandEnvelope CreateGlobalAdminCommand<T>(T command)
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

    private static CommandEnvelope CreateTenantCommand<T>(T command)
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
}
