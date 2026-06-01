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


namespace Hexalith.Tenants.Testing.Tests.Conformance;

/// <summary>
/// Conformance test suite proving InMemoryTenantService produces identical event sequences
/// as TenantAggregate and GlobalAdministratorsAggregate for every command type.
/// Uses reflection-based command discovery to automatically include new commands.
/// </summary>
[Trait("Category", "Conformance")]
public sealed class TenantConformanceTests {
    private readonly ITestOutputHelper _output;

    public TenantConformanceTests(ITestOutputHelper output) => _output = output;

    // ─── 4.2 / 4.3: Reflection-based command type discovery ───

    [Fact]
    public void All_command_types_have_intentional_conformance_coverage() {
        // Arrange
        List<Type> commandTypes = DiscoverTenantCommandTypes().ToList();
        List<Type> coveredTypes = ConformanceScenarios()
            .Select(row => row[0].ShouldBeOfType<ConformanceScenario>().CoveredCommandType)
            .Distinct()
            .OrderBy(t => t.Name)
            .ToList();

        _output.WriteLine($"Discovered {commandTypes.Count} command contracts:");
        foreach (Type t in commandTypes) {
            _output.WriteLine($"  - {t.Name}");
        }

        List<string> uncovered = commandTypes
            .Except(coveredTypes)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        uncovered.ShouldBeEmpty($"Uncovered tenant command contract(s): {string.Join(", ", uncovered)}.");

        List<string> stale = coveredTypes
            .Except(commandTypes)
            .Select(t => t.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        stale.ShouldBeEmpty($"Conformance scenario coverage references removed command contract(s): {string.Join(", ", stale)}.");
    }

    [Theory]
    [MemberData(nameof(ConformanceScenarios))]
    public void Command_sequences_match_production_aggregate_and_fake(ConformanceScenario scenario) {
        ArgumentNullException.ThrowIfNull(scenario);
        var context = new ConformanceContext();

        scenario.Execute(context);
        ScenarioResult result = context.ToResult();

        result.Steps.ShouldNotBeEmpty($"Scenario '{scenario.Name}' must execute at least one command.");
        foreach (CommandStepResult step in result.Steps) {
            AssertDomainResultsEqual(
                $"{scenario.Name} -> {step.CommandName}",
                step.AggregateResult,
                step.ServiceResult);
        }

        AssertTenantStatesEqual(scenario.Name, result.AggregateTenantStates, result.Service);
        AssertGlobalAdministratorStatesEqual(scenario.Name, result.AggregateGlobalAdminState, result.Service.GetGlobalAdminState());
    }

    public static IEnumerable<object[]> ConformanceScenarios() {
        yield return Scenario("CreateTenant global-admin success", typeof(CreateTenant), ctx =>
            ctx.ExecuteTenant(new CreateTenant("acme", "Acme", null), "acme", "admin", isGlobalAdmin: true));
        yield return Scenario("CreateTenant non-global-admin rejection", typeof(CreateTenant), ctx =>
            ctx.ExecuteTenant(new CreateTenant("acme", "Acme", null), "acme", "member", isGlobalAdmin: false));
        yield return Scenario("CreateTenant duplicate rejection", typeof(CreateTenant), ctx => {
            ctx.ExecuteTenant(new CreateTenant("acme", "Acme", null), "acme", "admin", isGlobalAdmin: true);
            ctx.ExecuteTenant(new CreateTenant("acme", "Acme Again", null), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("DisableTenant global-admin success", typeof(DisableTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new DisableTenant("acme"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("DisableTenant non-global-admin rejection", typeof(DisableTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new DisableTenant("acme"), "acme", "owner", isGlobalAdmin: false);
        });
        yield return Scenario("DisableTenant missing tenant rejection", typeof(DisableTenant), ctx =>
            ctx.ExecuteTenant(new DisableTenant("missing"), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("DisableTenant already-disabled rejection", typeof(DisableTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new DisableTenant("acme"), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("EnableTenant global-admin success", typeof(EnableTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new EnableTenant("acme"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("EnableTenant non-global-admin rejection", typeof(EnableTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new EnableTenant("acme"), "acme", "owner", isGlobalAdmin: false);
        });
        yield return Scenario("EnableTenant missing tenant rejection", typeof(EnableTenant), ctx =>
            ctx.ExecuteTenant(new EnableTenant("missing"), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("EnableTenant already-active rejection", typeof(EnableTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new EnableTenant("acme"), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("UpdateTenant global-admin success", typeof(UpdateTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new UpdateTenant("acme", "Acme Inc", "Updated"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("UpdateTenant contributor success", typeof(UpdateTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "contributor", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new UpdateTenant("acme", "Acme Inc", "Updated"), "acme", "contributor");
        });
        yield return Scenario("UpdateTenant unauthorized rejection", typeof(UpdateTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new UpdateTenant("acme", "Nope", null), "acme", "outsider");
        });
        yield return Scenario("UpdateTenant missing tenant rejection", typeof(UpdateTenant), ctx =>
            ctx.ExecuteTenant(new UpdateTenant("missing", "Nope", null), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("UpdateTenant disabled tenant rejection", typeof(UpdateTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new UpdateTenant("acme", "Nope", null), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("AddUserToTenant global-admin success", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("AddUserToTenant owner success", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), "acme", "owner");
        });
        yield return Scenario("AddUserToTenant empty-tenant first-user bootstrap success", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new AddUserToTenant("acme", "owner", TenantRole.TenantOwner), "acme", "owner");
        });
        yield return Scenario("AddUserToTenant unauthorized rejection", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), "acme", "outsider");
        });
        yield return Scenario("AddUserToTenant missing tenant rejection", typeof(AddUserToTenant), ctx =>
            ctx.ExecuteTenant(new AddUserToTenant("missing", "alice", TenantRole.TenantReader), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("AddUserToTenant disabled tenant rejection", typeof(AddUserToTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("AddUserToTenant duplicate user rejection", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "alice", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", TenantRole.TenantOwner), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("AddUserToTenant invalid role rejection", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new AddUserToTenant("acme", "alice", (TenantRole)999), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("AddUserToTenant uses envelope aggregate identity", typeof(AddUserToTenant), ctx => {
            ctx.CreateActiveTenant("envelope-tenant");
            ctx.ExecuteTenant(new AddUserToTenant("payload-tenant", "alice", TenantRole.TenantReader), "envelope-tenant", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("RemoveUserFromTenant global-admin success", typeof(RemoveUserFromTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "alice", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new RemoveUserFromTenant("acme", "alice"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("RemoveUserFromTenant owner success", typeof(RemoveUserFromTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.AddUser("acme", "alice", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new RemoveUserFromTenant("acme", "alice"), "acme", "owner");
        });
        yield return Scenario("RemoveUserFromTenant unauthorized rejection", typeof(RemoveUserFromTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new RemoveUserFromTenant("acme", "alice"), "acme", "outsider");
        });
        yield return Scenario("RemoveUserFromTenant missing tenant rejection", typeof(RemoveUserFromTenant), ctx =>
            ctx.ExecuteTenant(new RemoveUserFromTenant("missing", "alice"), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("RemoveUserFromTenant disabled tenant rejection", typeof(RemoveUserFromTenant), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new RemoveUserFromTenant("acme", "alice"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("RemoveUserFromTenant missing member rejection", typeof(RemoveUserFromTenant), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new RemoveUserFromTenant("acme", "ghost"), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("ChangeUserRole global-admin success", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "alice", TenantRole.TenantReader);
            ctx.ExecuteTenant(new ChangeUserRole("acme", "alice", TenantRole.TenantContributor), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("ChangeUserRole owner success", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.AddUser("acme", "alice", TenantRole.TenantReader);
            ctx.ExecuteTenant(new ChangeUserRole("acme", "alice", TenantRole.TenantContributor), "acme", "owner");
        });
        yield return Scenario("ChangeUserRole unauthorized rejection", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new ChangeUserRole("acme", "ghost", TenantRole.TenantOwner), "acme", "outsider");
        });
        yield return Scenario("ChangeUserRole missing tenant rejection", typeof(ChangeUserRole), ctx =>
            ctx.ExecuteTenant(new ChangeUserRole("missing", "alice", TenantRole.TenantOwner), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("ChangeUserRole disabled tenant rejection", typeof(ChangeUserRole), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new ChangeUserRole("acme", "alice", TenantRole.TenantOwner), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("ChangeUserRole missing member rejection", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new ChangeUserRole("acme", "ghost", TenantRole.TenantOwner), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("ChangeUserRole invalid role rejection", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "alice", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new ChangeUserRole("acme", "alice", (TenantRole)999), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("ChangeUserRole same role no-op", typeof(ChangeUserRole), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "alice", TenantRole.TenantContributor);
            ctx.ExecuteTenant(new ChangeUserRole("acme", "alice", TenantRole.TenantContributor), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("SetTenantConfiguration global-admin success", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("SetTenantConfiguration owner success", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "owner");
        });
        yield return Scenario("SetTenantConfiguration unauthorized rejection", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "outsider");
        });
        yield return Scenario("SetTenantConfiguration missing tenant rejection", typeof(SetTenantConfiguration), ctx =>
            ctx.ExecuteTenant(new SetTenantConfiguration("missing", "theme", "dark"), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("SetTenantConfiguration disabled tenant rejection", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("SetTenantConfiguration idempotent same-value no-op", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("SetTenantConfiguration key length rejection", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", new string('K', 257), "value"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("SetTenantConfiguration value length rejection", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "key", new string('V', 1025)), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("SetTenantConfiguration max key count rejection", typeof(SetTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            for (int i = 0; i < 100; i++) {
                ctx.ExecuteTenant(new SetTenantConfiguration("acme", $"key{i}", "value"), "acme", "admin", isGlobalAdmin: true);
            }

            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "key101", "value"), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("RemoveTenantConfiguration global-admin success", typeof(RemoveTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
            ctx.ExecuteTenant(new RemoveTenantConfiguration("acme", "theme"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("RemoveTenantConfiguration owner success", typeof(RemoveTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.AddUser("acme", "owner", TenantRole.TenantOwner);
            ctx.ExecuteTenant(new SetTenantConfiguration("acme", "theme", "dark"), "acme", "admin", isGlobalAdmin: true);
            ctx.ExecuteTenant(new RemoveTenantConfiguration("acme", "theme"), "acme", "owner");
        });
        yield return Scenario("RemoveTenantConfiguration unauthorized rejection", typeof(RemoveTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new RemoveTenantConfiguration("acme", "theme"), "acme", "outsider");
        });
        yield return Scenario("RemoveTenantConfiguration missing tenant rejection", typeof(RemoveTenantConfiguration), ctx =>
            ctx.ExecuteTenant(new RemoveTenantConfiguration("missing", "theme"), "missing", "admin", isGlobalAdmin: true));
        yield return Scenario("RemoveTenantConfiguration disabled tenant rejection", typeof(RemoveTenantConfiguration), ctx => {
            ctx.CreateDisabledTenant("acme");
            ctx.ExecuteTenant(new RemoveTenantConfiguration("acme", "theme"), "acme", "admin", isGlobalAdmin: true);
        });
        yield return Scenario("RemoveTenantConfiguration missing key rejection", typeof(RemoveTenantConfiguration), ctx => {
            ctx.CreateActiveTenant("acme");
            ctx.ExecuteTenant(new RemoveTenantConfiguration("acme", "missing"), "acme", "admin", isGlobalAdmin: true);
        });

        yield return Scenario("BootstrapGlobalAdmin success", typeof(BootstrapGlobalAdmin), ctx =>
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1")));
        yield return Scenario("BootstrapGlobalAdmin already bootstrapped rejection", typeof(BootstrapGlobalAdmin), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin2"));
        });
        yield return Scenario("SetGlobalAdministrator success", typeof(SetGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new SetGlobalAdministrator("admin2"), "admin1");
        });
        yield return Scenario("SetGlobalAdministrator duplicate rejection", typeof(SetGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new SetGlobalAdministrator("admin1"), "admin1");
        });
        yield return Scenario("SetGlobalAdministrator unauthorized rejection", typeof(SetGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new SetGlobalAdministrator("admin2"), "outsider");
        });
        yield return Scenario("RemoveGlobalAdministrator success", typeof(RemoveGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new SetGlobalAdministrator("admin2"), "admin1");
            ctx.ExecuteGlobal(new RemoveGlobalAdministrator("admin2"), "admin1");
        });
        yield return Scenario("RemoveGlobalAdministrator not-found rejection", typeof(RemoveGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new RemoveGlobalAdministrator("ghost"), "admin1");
        });
        yield return Scenario("RemoveGlobalAdministrator unauthorized rejection", typeof(RemoveGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new RemoveGlobalAdministrator("admin1"), "outsider");
        });
        yield return Scenario("RemoveGlobalAdministrator last-admin rejection", typeof(RemoveGlobalAdministrator), ctx => {
            ctx.ExecuteGlobal(new BootstrapGlobalAdmin("admin1"));
            ctx.ExecuteGlobal(new RemoveGlobalAdministrator("admin1"), "admin1");
        });
    }

    private static object[] Scenario(string name, Type coveredCommandType, Action<ConformanceContext> execute)
        => [new ConformanceScenario(name, coveredCommandType, execute)];

    private static IEnumerable<Type> DiscoverTenantCommandTypes() {
        Assembly contractsAssembly = typeof(CreateTenant).Assembly;
        return contractsAssembly
            .GetTypes()
            .Where(t => t.IsClass
                && !t.IsAbstract
                && string.Equals(t.Namespace, "Hexalith.Tenants.Contracts.Commands", StringComparison.Ordinal))
            .OrderBy(t => t.Name);
    }

    // ═══════════════════════════════════════════════════════════
    // 4.4: Tenant command conformance tests (9 commands)
    // ═══════════════════════════════════════════════════════════

    // ─── CreateTenant (null state, no envelope) ───

    [Fact]
    public void Conformance_CreateTenant_Success() {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new CreateTenant("acme", "Acme Corp", "A test tenant");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act — aggregate path
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        // Act — service path
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── DisableTenant (envelope-required) ───

    [Fact]
    public void Conformance_DisableTenant_Success() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new DisableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── EnableTenant (envelope-required) ───

    [Fact]
    public void Conformance_EnableTenant_Success() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new EnableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ─── UpdateTenant (envelope-required) ───

    [Fact]
    public void Conformance_UpdateTenant_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Conformance_UpdateTenant_Success_NonAdmin_Contributor() {
        // Arrange — contributor role should succeed for UpdateTenant
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_AddUserToTenant_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Conformance_AddUserToTenant_Success_NonAdmin_Owner() {
        // Arrange — owner role with membership history should succeed
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_RemoveUserFromTenant_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_RemoveUserFromTenant_Success_NonAdmin_Owner() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_ChangeUserRole_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_ChangeUserRole_Success_NonAdmin_Owner() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantReader), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_SetTenantConfiguration_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Conformance_SetTenantConfiguration_Success_NonAdmin_Owner() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_RemoveTenantConfiguration_Success_GlobalAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_RemoveTenantConfiguration_Success_NonAdmin_Owner() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner1", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);
        _ = svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

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
    public void Conformance_BootstrapGlobalAdmin_Success() {
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
    public void Conformance_SetGlobalAdministrator_Success() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new SetGlobalAdministrator("admin2");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "global-administrators", "admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState, envelope);
        DomainResult serviceResult = svc.ProcessCommand(command, "admin1");

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Conformance_RemoveGlobalAdministrator_Success() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));
        _ = svc.ProcessCommand(new SetGlobalAdministrator("admin2"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));
        gaState.Apply(new GlobalAdministratorSet("system", "admin2"));

        var command = new RemoveGlobalAdministrator("admin2");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "global-administrators", "admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState, envelope);
        DomainResult serviceResult = svc.ProcessCommand(command, "admin1");

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // 4.6: Rejection conformance tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Rejection_CreateTenant_AlreadyExists() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new CreateTenant("acme", "Acme Again", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_DisableTenant_NotFound() {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new DisableTenant("nonexistent");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_EnableTenant_NotFound() {
        // Arrange
        var svc = new InMemoryTenantService();
        var command = new EnableTenant("nonexistent");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "nonexistent", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, null, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    public void Rejection_RemoveUserFromTenant_NotMember() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_ChangeUserRole_NotMember() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_UpdateTenant_NotFound() {
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
    public void Rejection_SetTenantConfiguration_NotFound() {
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
    public void Rejection_RemoveTenantConfiguration_NotFound() {
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
    public void Rejection_BootstrapGlobalAdmin_AlreadyBootstrapped() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

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
    public void Rejection_RemoveGlobalAdministrator_LastAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new RemoveGlobalAdministrator("admin1");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "global-administrators", "admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState, envelope);
        DomainResult serviceResult = svc.ProcessCommand(command, "admin1");

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_AddUserToTenant_AlreadyMember() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

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
    public void Rejection_UpdateTenant_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_UpdateTenant_CrossTenantRoleDoesNotTransfer() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("tenant-a", "Tenant A", null));
        _ = svc.ProcessCommand(new CreateTenant("tenant-b", "Tenant B", null));
        _ = svc.ProcessCommand(new AddUserToTenant("tenant-a", "shared-user", TenantRole.TenantReader), userId: "admin", isGlobalAdmin: true);
        _ = svc.ProcessCommand(new AddUserToTenant("tenant-b", "shared-user", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

        TenantState state = CreateTenantState("tenant-a", "Tenant A", null);
        state.Apply(new UserAddedToTenant("tenant-a", "shared-user", TenantRole.TenantReader));

        var command = new UpdateTenant("tenant-a", "Tenant A Updated", null);
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "tenant-a", "shared-user", isGlobalAdmin: false);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
        InsufficientPermissionsRejection rejection = serviceResult.Events[0].ShouldBeOfType<InsufficientPermissionsRejection>();
        rejection.ActorRole.ShouldBe(TenantRole.TenantReader);
        ValuesEqual(rejection.TenantId, "tenant-a").ShouldBeTrue("Cross-tenant role rejection must identify the target tenant.");
        svc.GetTenantState("tenant-b")!.Users["shared-user"].ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public void Rejection_AddUserToTenant_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "owner", TenantRole.TenantOwner), userId: "admin", isGlobalAdmin: true);

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
    public void Rejection_RemoveUserFromTenant_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_ChangeUserRole_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_SetTenantConfiguration_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_RemoveTenantConfiguration_InsufficientPermissions() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_SetTenantConfiguration_MaxKeyLengthExceeded() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_SetTenantConfiguration_MaxValueLengthExceeded() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

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
    public void Rejection_SetTenantConfiguration_MaxConfigurationKeysExceeded() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        for (int i = 0; i < 100; i++) {
            string key = $"key{i}";
            string val = "val";
            var setCmd = new SetTenantConfiguration("acme", key, val);
            CommandEnvelope setEnv = TenantTestHelpers.CreateCommandEnvelope(setCmd, "acme", "admin", isGlobalAdmin: true);
            _ = svc.ProcessTenantCommand(setCmd, setEnv);
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
    public void Rejection_AddUserToTenant_RoleEscalation() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
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
    public void Rejection_ChangeUserRole_RoleEscalation() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);
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
    public void Rejection_UpdateTenant_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    public void Rejection_RemoveUserFromTenant_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    public void Rejection_ChangeUserRole_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    public void Rejection_SetTenantConfiguration_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    public void Rejection_RemoveTenantConfiguration_Disabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

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
    // 4.7: NoOp and duplicate lifecycle conformance tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void Rejection_DisableTenant_AlreadyDisabled() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new DisableTenant("acme"));

        TenantState state = CreateTenantState("acme", "Acme", null);
        state.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        var command = new DisableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
        TenantLifecycleStateAlreadySetRejection rejection = aggregateResult.Events[0].ShouldBeOfType<TenantLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(TenantStatus.Disabled);
        rejection.RequestedStatus.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Rejection_EnableTenant_AlreadyActive() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new EnableTenant("acme");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
        TenantLifecycleStateAlreadySetRejection rejection = aggregateResult.Events[0].ShouldBeOfType<TenantLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(TenantStatus.Active);
        rejection.RequestedStatus.ShouldBe(TenantStatus.Active);
    }

    [Fact]
    public void NoOp_SetTenantConfiguration_SameValue() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new SetTenantConfiguration("acme", "theme", "dark"), userId: "admin", isGlobalAdmin: true);

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
    public void Rejection_RemoveTenantConfiguration_KeyNotPresent() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));

        TenantState state = CreateTenantState("acme", "Acme", null);

        var command = new RemoveTenantConfiguration("acme", "nonexistent-key");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "acme", "admin", isGlobalAdmin: true);

        // Act
        DomainResult aggregateResult = TenantAggregate.Handle(command, state, envelope);
        DomainResult serviceResult = svc.ProcessTenantCommand(command, envelope);

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
        serviceResult.IsRejection.ShouldBeTrue();
        ConfigurationKeyNotFoundRejection rejection = serviceResult.Events[0].ShouldBeOfType<ConfigurationKeyNotFoundRejection>();
        ValuesEqual(rejection.TenantId, "acme").ShouldBeTrue("Configuration key rejection must identify the target tenant.");
        ValuesEqual(rejection.Key, "nonexistent-key").ShouldBeTrue("Configuration key rejection must identify the missing key.");
    }

    [Fact]
    public void NoOp_ChangeUserRole_SameRole() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new CreateTenant("acme", "Acme", null));
        _ = svc.ProcessCommand(new AddUserToTenant("acme", "alice", TenantRole.TenantContributor), userId: "admin", isGlobalAdmin: true);

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
    public void Rejection_SetGlobalAdministrator_AlreadyAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new SetGlobalAdministrator("admin1");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "global-administrators", "admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState, envelope);
        DomainResult serviceResult = svc.ProcessCommand(command, "admin1");

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    [Fact]
    public void Rejection_RemoveGlobalAdministrator_NotAdmin() {
        // Arrange
        var svc = new InMemoryTenantService();
        _ = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));

        var gaState = new GlobalAdministratorsState();
        gaState.Apply(new GlobalAdministratorSet("system", "admin1"));

        var command = new RemoveGlobalAdministrator("ghost");
        CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, "global-administrators", "admin1");

        // Act
        DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, gaState, envelope);
        DomainResult serviceResult = svc.ProcessCommand(command, "admin1");

        // Assert
        AssertEventsEqual(aggregateResult, serviceResult);
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    public sealed record ConformanceScenario(
        string Name,
        Type CoveredCommandType,
        Action<ConformanceContext> Execute) {
        public override string ToString() => Name;
    }

    public sealed record CommandStepResult(
        string CommandName,
        DomainResult AggregateResult,
        DomainResult ServiceResult);

    public sealed record ScenarioResult(
        IReadOnlyList<CommandStepResult> Steps,
        IReadOnlyDictionary<string, TenantState?> AggregateTenantStates,
        GlobalAdministratorsState? AggregateGlobalAdminState,
        InMemoryTenantService Service);

    public sealed class ConformanceContext {
        private const string GlobalAdministratorsAggregateId = "global-administrators";

        private readonly Dictionary<string, TenantState> _aggregateTenantStates = [];
        private readonly HashSet<string> _observedTenantIds = [];
        private readonly List<CommandStepResult> _steps = [];
        private GlobalAdministratorsState? _aggregateGlobalAdminState;

        public InMemoryTenantService Service { get; } = new();

        public void CreateActiveTenant(string tenantId)
            => ExecuteTenant(new CreateTenant(tenantId, tenantId, null), tenantId, "admin", isGlobalAdmin: true);

        public void CreateDisabledTenant(string tenantId) {
            CreateActiveTenant(tenantId);
            ExecuteTenant(new DisableTenant(tenantId), tenantId, "admin", isGlobalAdmin: true);
        }

        public void AddUser(string tenantId, string userId, TenantRole role)
            => ExecuteTenant(new AddUserToTenant(tenantId, userId, role), tenantId, "admin", isGlobalAdmin: true);

        public void ExecuteTenant<T>(T command, string aggregateId, string actorUserId, bool isGlobalAdmin = false)
            where T : notnull {
            ArgumentNullException.ThrowIfNull(command);
            CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, aggregateId, actorUserId, isGlobalAdmin);
            TenantState? state = GetAggregateTenantState(aggregateId);

            DomainResult aggregateResult = command switch {
                CreateTenant c => TenantAggregate.Handle(c, state, envelope),
                DisableTenant c => TenantAggregate.Handle(c, state, envelope),
                EnableTenant c => TenantAggregate.Handle(c, state, envelope),
                UpdateTenant c => TenantAggregate.Handle(c, state, envelope),
                AddUserToTenant c => TenantAggregate.Handle(c, state, envelope),
                RemoveUserFromTenant c => TenantAggregate.Handle(c, state, envelope),
                ChangeUserRole c => TenantAggregate.Handle(c, state, envelope),
                SetTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
                RemoveTenantConfiguration c => TenantAggregate.Handle(c, state, envelope),
                _ => throw new InvalidOperationException($"Unknown tenant command type: {command.GetType().Name}."),
            };

            ApplyAggregateTenantEvents(aggregateId, aggregateResult);
            DomainResult serviceResult = Service.ProcessTenantCommand(command, envelope);
            TrackTenantIds(command, aggregateId);
            _steps.Add(new CommandStepResult(command.GetType().Name, aggregateResult, serviceResult));
        }

        public void ExecuteGlobal(BootstrapGlobalAdmin command) {
            ArgumentNullException.ThrowIfNull(command);
            DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, _aggregateGlobalAdminState);
            ApplyAggregateGlobalAdminEvents(aggregateResult);
            DomainResult serviceResult = Service.ProcessCommand(command);
            _steps.Add(new CommandStepResult(nameof(BootstrapGlobalAdmin), aggregateResult, serviceResult));
        }

        public void ExecuteGlobal(SetGlobalAdministrator command, string actorUserId) {
            ArgumentNullException.ThrowIfNull(command);
            CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, GlobalAdministratorsAggregateId, actorUserId);
            DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, _aggregateGlobalAdminState, envelope);
            ApplyAggregateGlobalAdminEvents(aggregateResult);
            DomainResult serviceResult = Service.ProcessCommand(command, actorUserId);
            _steps.Add(new CommandStepResult(nameof(SetGlobalAdministrator), aggregateResult, serviceResult));
        }

        public void ExecuteGlobal(RemoveGlobalAdministrator command, string actorUserId) {
            ArgumentNullException.ThrowIfNull(command);
            CommandEnvelope envelope = TenantTestHelpers.CreateCommandEnvelope(command, GlobalAdministratorsAggregateId, actorUserId);
            DomainResult aggregateResult = GlobalAdministratorsAggregate.Handle(command, _aggregateGlobalAdminState, envelope);
            ApplyAggregateGlobalAdminEvents(aggregateResult);
            DomainResult serviceResult = Service.ProcessCommand(command, actorUserId);
            _steps.Add(new CommandStepResult(nameof(RemoveGlobalAdministrator), aggregateResult, serviceResult));
        }

        public ScenarioResult ToResult() {
            var aggregateStates = _observedTenantIds
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToDictionary(id => id, GetAggregateTenantState, StringComparer.Ordinal);
            return new ScenarioResult(_steps, aggregateStates, _aggregateGlobalAdminState, Service);
        }

        private void TrackTenantIds<T>(T command, string aggregateId)
            where T : notnull {
            _ = _observedTenantIds.Add(aggregateId);
            string? payloadTenantId = command switch {
                CreateTenant c => c.TenantId,
                DisableTenant c => c.TenantId,
                EnableTenant c => c.TenantId,
                UpdateTenant c => c.TenantId,
                AddUserToTenant c => c.TenantId,
                RemoveUserFromTenant c => c.TenantId,
                ChangeUserRole c => c.TenantId,
                SetTenantConfiguration c => c.TenantId,
                RemoveTenantConfiguration c => c.TenantId,
                _ => null,
            };
            if (!string.IsNullOrWhiteSpace(payloadTenantId)) {
                _ = _observedTenantIds.Add(payloadTenantId);
            }
        }

        private TenantState? GetAggregateTenantState(string tenantId)
            => _aggregateTenantStates.TryGetValue(tenantId, out TenantState? state) ? state : null;

        private void ApplyAggregateTenantEvents(string tenantId, DomainResult result) {
            if (!result.IsSuccess) {
                return;
            }

            if (!_aggregateTenantStates.TryGetValue(tenantId, out TenantState? state)) {
                state = new TenantState();
                _aggregateTenantStates[tenantId] = state;
            }

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
        }

        private void ApplyAggregateGlobalAdminEvents(DomainResult result) {
            if (!result.IsSuccess) {
                return;
            }

            _aggregateGlobalAdminState ??= new GlobalAdministratorsState();
            foreach (IEventPayload evt in result.Events) {
                switch (evt) {
                    case GlobalAdministratorSet e:
                        _aggregateGlobalAdminState.Apply(e);
                        break;
                    case GlobalAdministratorRemoved e:
                        _aggregateGlobalAdminState.Apply(e);
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Creates a TenantState by applying a TenantCreated event manually.
    /// Used to build the manual aggregate state for side-by-side comparison.
    /// </summary>
    private static TenantState CreateTenantState(string tenantId, string name, string? description) {
        var state = new TenantState();
        state.Apply(new TenantCreated(tenantId, name, description, DateTimeOffset.UtcNow));
        return state;
    }

    /// <summary>
    /// Asserts that two DomainResults have identical outcomes and event sequences.
    /// Special-cases events with DateTimeOffset fields to avoid timestamp flakiness.
    /// </summary>
    private static void AssertEventsEqual(DomainResult expected, DomainResult actual) {
        AssertDomainResultsEqual("legacy conformance assertion", expected, actual);
    }

    private static void AssertDomainResultsEqual(string scenarioStep, DomainResult expected, DomainResult actual) {
        expected.Events.Count.ShouldBe(actual.Events.Count, $"{scenarioStep}: event/rejection count differed.");
        GetResultKind(expected).ShouldBe(GetResultKind(actual), $"{scenarioStep}: result kind differed.");

        List<string> expectedTypes = expected.Events.Select(e => e.GetType().Name).ToList();
        List<string> actualTypes = actual.Events.Select(e => e.GetType().Name).ToList();
        actualTypes.ShouldBe(expectedTypes, $"{scenarioStep}: event/rejection type sequence differed. Expected {string.Join(", ", expectedTypes)}; actual {string.Join(", ", actualTypes)}.");

        for (int i = 0; i < expected.Events.Count; i++) {
            IEventPayload e1 = expected.Events[i];
            IEventPayload e2 = actual.Events[i];
            e1.GetType().ShouldBe(e2.GetType(), $"{scenarioStep}: event/rejection type differed at position {i}.");

            // For robust evaluation, we compare properties using reflection, skipping DateTimeOffset fields
            // which can differ by a few ticks between paths.
            PropertyInfo[] properties = e1.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo prop in properties) {
                if (prop.PropertyType == typeof(DateTimeOffset) || prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTimeOffset?) || prop.PropertyType == typeof(DateTime?)) {
                    continue; // Skip timestamp fields
                }

                object? val1 = prop.GetValue(e1);
                object? val2 = prop.GetValue(e2);
                ValuesEqual(val1, val2).ShouldBeTrue($"{scenarioStep}: {e1.GetType().Name}.{prop.Name} did not match.");
            }
        }
    }

    private static void AssertTenantStatesEqual(
        string scenarioName,
        IReadOnlyDictionary<string, TenantState?> expectedStates,
        InMemoryTenantService service) {
        foreach ((string tenantId, TenantState? expected) in expectedStates) {
            TenantState? actual = service.GetTenantState(tenantId);
            (actual is null).ShouldBe(expected is null, $"{scenarioName}: tenant state presence differed for '{tenantId}'.");
            if (expected is null || actual is null) {
                continue;
            }

            ValuesEqual(actual.TenantId, expected.TenantId).ShouldBeTrue($"{scenarioName}: TenantState.TenantId differed for '{tenantId}'.");
            ValuesEqual(actual.Name, expected.Name).ShouldBeTrue($"{scenarioName}: TenantState.Name differed for '{tenantId}'.");
            ValuesEqual(actual.Description, expected.Description).ShouldBeTrue($"{scenarioName}: TenantState.Description differed for '{tenantId}'.");
            actual.Status.ShouldBe(expected.Status, $"{scenarioName}: TenantState.Status differed for '{tenantId}'.");
            actual.HasMembershipHistory.ShouldBe(expected.HasMembershipHistory, $"{scenarioName}: TenantState.HasMembershipHistory differed for '{tenantId}'.");
            DictionariesEqual(actual.Users, expected.Users).ShouldBeTrue($"{scenarioName}: TenantState.Users differed for '{tenantId}'.");
            DictionariesEqual(actual.Configuration, expected.Configuration).ShouldBeTrue($"{scenarioName}: TenantState.Configuration differed for '{tenantId}'.");
        }
    }

    private static void AssertGlobalAdministratorStatesEqual(
        string scenarioName,
        GlobalAdministratorsState? expected,
        GlobalAdministratorsState? actual) {
        (actual is null).ShouldBe(expected is null, $"{scenarioName}: global-administrator state presence differed.");
        if (expected is null || actual is null) {
            return;
        }

        actual.Bootstrapped.ShouldBe(expected.Bootstrapped, $"{scenarioName}: global-administrator bootstrapped flag differed.");
        SetsEqual(actual.Administrators, expected.Administrators)
            .ShouldBeTrue($"{scenarioName}: global-administrator set differed.");
    }

    private static string GetResultKind(DomainResult result)
        => result.IsSuccess
            ? "Success"
            : result.IsRejection
                ? "Rejection"
                : result.IsNoOp ? "NoOp" : "Unknown";

    private static bool ValuesEqual(object? actual, object? expected)
        => actual switch {
            null => expected is null,
            _ => actual.Equals(expected),
        };

    private static bool DictionariesEqual<TKey, TValue>(
        IReadOnlyDictionary<TKey, TValue> actual,
        IReadOnlyDictionary<TKey, TValue> expected)
        where TKey : notnull
        => actual.Count == expected.Count
           && expected.All(kvp => actual.TryGetValue(kvp.Key, out TValue? actualValue)
                                  && EqualityComparer<TValue>.Default.Equals(actualValue, kvp.Value));

    private static bool SetsEqual(HashSet<string> actual, HashSet<string> expected)
        => actual.SetEquals(expected);
}
