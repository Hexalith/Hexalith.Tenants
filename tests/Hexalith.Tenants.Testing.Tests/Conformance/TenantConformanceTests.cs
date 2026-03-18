using System.Reflection;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Events;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Server.Aggregates;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;

using Shouldly;

using Xunit.Abstractions;

namespace Hexalith.Tenants.Testing.Tests.Conformance;

/// <summary>
/// Conformance test suite proving InMemoryTenantService produces identical event sequences
/// as TenantAggregate and GlobalAdministratorsAggregate for every command type.
/// Uses reflection-based command discovery to automatically include new commands.
/// </summary>
[Trait("Category", "Conformance")]
public sealed class TenantConformanceTests
{
    private readonly ITestOutputHelper _output;

    public TenantConformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // ─── 4.2 / 4.3: Reflection-based command type discovery ───

    [Fact]
    public void AllCommandTypesDiscovered()
    {
        // Arrange
        Assembly contractsAssembly = typeof(CreateTenant).Assembly;
        List<Type> commandTypes = contractsAssembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract
        && t.Namespace != null && t.Namespace.StartsWith("Hexalith.Tenants.Contracts.Commands", StringComparison.Ordinal))
            .OrderBy(t => t.Name)
            .ToList();

        // Output all discovered types for debugging
        _output.WriteLine($"Discovered {commandTypes.Count} command types:");
        foreach (Type t in commandTypes)
        {
            _output.WriteLine($"  - {t.Name}");
        }

        // Assert — exactly 12 command types expected
        commandTypes.Count.ShouldBe(12, $"Expected 12 command types but found {commandTypes.Count}. " +
            "If a command was added or removed, update this count and the conformance tests.");
    }

    // ═══════════════════════════════════════════════════════════
    // 4.4: Tenant command conformance tests (9 commands)
    // ═══════════════════════════════════════════════════════════

    // ─── CreateTenant (null state, no envelope) ───

    [Fact]
    public void Conformance_CreateTenant_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new CreateTenant("acme", "Acme Corp", "A test tenant");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act — aggregate path
        DomainResult aggregateResult = TenantAggregate.Handle(command, null);
        // Act — service path
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── DisableTenant (state-only, no envelope) ───

    [Fact]
    public void Conformance_DisableTenant_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new DisableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── EnableTenant (state-only, no envelope) ───

    [Fact]
    public void Conformance_EnableTenant_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new EnableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── UpdateTenant (envelope-required) ───

    [Fact]
    public void Conformance_UpdateTenant_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new UpdateTenant("acme", "Acme Inc", "Updated");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_UpdateTenant_Success_NonAdmin_Contributor()
    {
        // Arrange — contributor role should succeed for UpdateTenant
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new UpdateTenant("acme", "Acme Inc", "Updated");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "alice", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── AddUserToTenant (envelope-required) ───

    [Fact]
    public void Conformance_AddUserToTenant_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new AddUserToTenant("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_AddUserToTenant_Success_NonAdmin_Owner()
    {
        // Arrange — owner role with membership history should succeed
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner1", TenantRole.TenantOwner));

        var command = new AddUserToTenant("acme", "bob", TenantRole.TenantReader);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "owner1", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── RemoveUserFromTenant (envelope-required) ───

    [Fact]
    public void Conformance_RemoveUserFromTenant_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new RemoveUserFromTenant("acme", "alice");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_RemoveUserFromTenant_Success_NonAdmin_Owner()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner1", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new RemoveUserFromTenant("acme", "alice");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "owner1", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── ChangeUserRole (envelope-required) ───

    [Fact]
    public void Conformance_ChangeUserRole_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantReader));

        var command = new ChangeUserRole("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_ChangeUserRole_Success_NonAdmin_Owner()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner1", TenantRole.TenantOwner));
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantReader));

        var command = new ChangeUserRole("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "owner1", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── SetTenantConfiguration (envelope-required) ───

    [Fact]
    public void Conformance_SetTenantConfiguration_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new SetTenantConfiguration("acme", "theme", "dark");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_SetTenantConfiguration_Success_NonAdmin_Owner()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner1", TenantRole.TenantOwner));

        var command = new SetTenantConfiguration("acme", "theme", "dark");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "owner1", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── RemoveTenantConfiguration (envelope-required) ───

    [Fact]
    public void Conformance_RemoveTenantConfiguration_Success_GlobalAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        var command = new RemoveTenantConfiguration("acme", "theme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_RemoveTenantConfiguration_Success_NonAdmin_Owner()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner1", TenantRole.TenantOwner));
        state.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        var command = new RemoveTenantConfiguration("acme", "theme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "owner1", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // 4.5: Global admin command conformance tests (3 commands)
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Conformance_BootstrapGlobalAdmin_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new BootstrapGlobalAdmin("admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, null);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_SetGlobalAdministrator_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new SetGlobalAdministrator("admin2");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_RemoveGlobalAdministrator_Success()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));
        svc.ProcessCommand(new SetGlobalAdministrator("admin2"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));
        gaState.Apply(new GlobalAdministratorSet("system", "admin2"));

        var command = new RemoveGlobalAdministrator("admin2");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // 4.6: Rejection conformance tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Rejection_CreateTenant_AlreadyExists()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new CreateTenant("acme", "Acme Again", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_DisableTenant_NotFound()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new DisableTenant("nonexistent");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_EnableTenant_NotFound()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new EnableTenant("nonexistent");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new AddUserToTenant("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveUserFromTenant_NotMember()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new RemoveUserFromTenant("acme", "ghost");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_ChangeUserRole_NotMember()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new ChangeUserRole("acme", "ghost", TenantRole.TenantOwner);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_UpdateTenant_NotFound()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new UpdateTenant("nonexistent", "Name", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_NotFound()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new SetTenantConfiguration("nonexistent", "key", "value");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveTenantConfiguration_NotFound()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new RemoveTenantConfiguration("nonexistent", "key");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_BootstrapGlobalAdmin_AlreadyBootstrapped()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new BootstrapGlobalAdmin("admin2");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveGlobalAdministrator_LastAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new RemoveGlobalAdministrator("admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_AlreadyMember()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new AddUserToTenant("acme", "alice", TenantRole.TenantOwner);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_UpdateTenant_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new UpdateTenant("acme", "New Name", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "owner", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "owner", TenantRole.TenantOwner));

        var command = new AddUserToTenant("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveUserFromTenant_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new RemoveUserFromTenant("acme", "ghost");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_ChangeUserRole_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new ChangeUserRole("acme", "ghost", TenantRole.TenantOwner);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new SetTenantConfiguration("acme", "key", "val");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveTenantConfiguration_InsufficientPermissions()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new RemoveTenantConfiguration("acme", "key");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "hacker", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_MaxKeyLengthExceeded()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new SetTenantConfiguration("acme", new string('K', 257), "value");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_MaxValueLengthExceeded()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new SetTenantConfiguration("acme", "key", new string('V', 1025));
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_MaxConfigurationKeysExceeded()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        for (int i = 0; i < 100; i++)
        {
            var key = $"key{i}";
            var val = "val";
            var setCmd = new SetTenantConfiguration("acme", key, val);
            var setEnv = TenantTestHelpers.CreateCommandEnvelope(setCmd, "acme", "admin", isGlobalAdmin: true);
            svc.ProcessTenantCommand(setCmd, setEnv);
            state.Apply(new TenantConfigurationSet("acme", key, val));
        }

        var command = new SetTenantConfiguration("acme", "key101", "val");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_RoleEscalation()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new AddUserToTenant("acme", "alice", (TenantRole)999);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_ChangeUserRole_RoleEscalation()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);
        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new ChangeUserRole("acme", "alice", (TenantRole)999);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_UpdateTenant_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new UpdateTenant("acme", "New", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveUserFromTenant_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new RemoveUserFromTenant("acme", "ghost");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_ChangeUserRole_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new ChangeUserRole("acme", "ghost", TenantRole.TenantOwner);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_SetTenantConfiguration_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new SetTenantConfiguration("acme", "key", "val");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveTenantConfiguration_Disabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new RemoveTenantConfiguration("acme", "key");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // 4.7: NoOp conformance tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void NoOp_DisableTenant_AlreadyDisabled()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new DisableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_EnableTenant_AlreadyActive()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new EnableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_SetTenantConfiguration_SameValue()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        var command = new SetTenantConfiguration("acme", "theme", "dark");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_RemoveTenantConfiguration_KeyNotPresent()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new RemoveTenantConfiguration("acme", "nonexistent-key");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_ChangeUserRole_SameRole()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        var command = new ChangeUserRole("acme", "alice", TenantRole.TenantContributor);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_SetGlobalAdministrator_AlreadyAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new SetGlobalAdministrator("admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void NoOp_RemoveGlobalAdministrator_NotAdmin()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new RemoveGlobalAdministrator("ghost");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState);
        DomainResult serviceResult = svc.ProcessCommand(command);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a TenantState by applying a TenantCreated event manually.
    /// Used to build the manual aggregate state for side-by-side comparison.
    /// </summary>
    private static TenantState CreateTenantState(string tenantId, string name, string? description)
    {
        var state = new TenantState();
        state.Apply(new TenantCreated(tenantId, name, description, DateTimeOffset.UtcNow));
        return state;
    }

    /// <summary>
    /// Asserts that two DomainResults have identical outcomes and event sequences.
    /// Special-cases events with DateTimeOffset fields to avoid timestamp flakiness.
    /// </summary>
    private static void AssertEventsEqual(DomainResult expected, DomainResult actual)
    {
        expected.Events.Count.ShouldBe(actual.Events.Count);
        expected.IsSuccess.ShouldBe(actual.IsSuccess);
        expected.IsRejection.ShouldBe(actual.IsRejection);
        expected.IsNoOp.ShouldBe(actual.IsNoOp);

        for (int i = 0; i < expected.Events.Count; i++)
        {
            IEventPayload e1 = expected.Events[i];
            IEventPayload e2 = actual.Events[i];
            e1.GetType().ShouldBe(e2.GetType());

            // For robust evaluation, we compare properties using reflection, skipping DateTimeOffset fields
            // which can differ by a few ticks between paths.
            PropertyInfo[] properties = e1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties)
            {
                if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTimeOffset?) || prop.PropertyType == typeof(DateTime?))
                {
                    continue; // Skip timestamp fields
                }

                object? val1 = prop.GetValue(e1);
                object? val2 = prop.GetValue(e2);
                val1.ShouldBe(val2, $"Property {prop.Name} on {e1.GetType().Name} did not match.");
            }
        }
    }
}

