using System.Diagnostics;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Testing.Fakes;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Fakes;

public class InMemoryTenantServiceTests
{
    // ─── 3.2: CreateTenant produces TenantCreated event ───

    [Fact]
    public void CreateTenant_produces_TenantCreated_event()
    {
        var svc = new InMemoryTenantService();
        var result = svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", "A test tenant"));

        result.IsSuccess.ShouldBeTrue();
        result.Events.Count.ShouldBe(1);
        result.Events[0].ShouldBeOfType<TenantCreated>();
        var evt = (TenantCreated)result.Events[0];
        evt.TenantId.ShouldBe("acme");
        evt.Name.ShouldBe("Acme Corp");
        evt.Description.ShouldBe("A test tenant");
    }

    // ─── 3.3: CreateTenant + AddUserToTenant produces correct events with maintained state ───

    [Fact]
    public void CreateTenant_then_AddUser_produces_correct_events_and_maintains_state()
    {
        var svc = new InMemoryTenantService();
        var createResult = svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
        createResult.IsSuccess.ShouldBeTrue();

        var addResult = svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
            userId: "owner",
            isGlobalAdmin: true);

        addResult.IsSuccess.ShouldBeTrue();
        addResult.Events.Count.ShouldBe(1);
        addResult.Events[0].ShouldBeOfType<UserAddedToTenant>();

        // State should be maintained
        var state = svc.GetTenantState("acme");
        state.ShouldNotBeNull();
        state.TenantId.ShouldBe("acme");
        state.Users.ShouldContainKey("alice");
        state.Users["alice"].ShouldBe(TenantRole.TenantContributor);

        // EventHistory should contain both events
        svc.EventHistory.Count.ShouldBe(2);
        svc.EventHistory[0].ShouldBeOfType<TenantCreated>();
        svc.EventHistory[1].ShouldBeOfType<UserAddedToTenant>();
    }

    // ─── 3.4: Duplicate tenant creation returns TenantAlreadyExistsRejection ───

    [Fact]
    public void Duplicate_CreateTenant_returns_TenantAlreadyExistsRejection()
    {
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));

        var result = svc.ProcessCommand(new CreateTenant("acme", "Acme Again", null));

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantAlreadyExistsRejection>();
    }

    // ─── 3.5: AddUserToTenant on disabled tenant returns TenantDisabledRejection ───

    [Fact]
    public void AddUser_on_disabled_tenant_returns_TenantDisabledRejection()
    {
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        var result = svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantReader),
            userId: "owner",
            isGlobalAdmin: true);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<TenantDisabledRejection>();
    }

    // ─── 3.6: Duplicate user add returns UserAlreadyInTenantRejection ───

    [Fact]
    public void Duplicate_AddUser_returns_UserAlreadyInTenantRejection()
    {
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
        svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
            userId: "owner",
            isGlobalAdmin: true);

        var result = svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantReader),
            userId: "owner",
            isGlobalAdmin: true);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<UserAlreadyInTenantRejection>();
    }

    // ─── 3.7: Role escalation returns RoleEscalationRejection ───

    [Fact]
    public void Invalid_role_returns_RoleEscalationRejection()
    {
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));

        var result = svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", (TenantRole)999),
            userId: "owner",
            isGlobalAdmin: true);

        result.IsRejection.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<RoleEscalationRejection>();
    }

    // ─── 3.8: Cross-tenant isolation ───

    [Fact]
    public void Cross_tenant_isolation_guarantees_no_data_leaks()
    {
        var svc = new InMemoryTenantService();

        // Create two tenants
        svc.ProcessCommand(new CreateTenant("tenant-a", "Tenant A", null));
        svc.ProcessCommand(new CreateTenant("tenant-b", "Tenant B", null));

        // Add users to each tenant
        svc.ProcessCommand(
            new AddUserToTenant("tenant-a", "alice", TenantRole.TenantOwner),
            userId: "admin",
            isGlobalAdmin: true);
        svc.ProcessCommand(
            new AddUserToTenant("tenant-b", "bob", TenantRole.TenantOwner),
            userId: "admin",
            isGlobalAdmin: true);

        // Verify isolation
        var stateA = svc.GetTenantState("tenant-a");
        var stateB = svc.GetTenantState("tenant-b");

        stateA.ShouldNotBeNull();
        stateB.ShouldNotBeNull();

        stateA.TenantId.ShouldBe("tenant-a");
        stateB.TenantId.ShouldBe("tenant-b");

        stateA.Users.ShouldContainKey("alice");
        stateA.Users.ShouldNotContainKey("bob");

        stateB.Users.ShouldContainKey("bob");
        stateB.Users.ShouldNotContainKey("alice");

        stateA.Name.ShouldBe("Tenant A");
        stateB.Name.ShouldBe("Tenant B");
    }

    // ─── 3.9: GlobalAdmin commands ───

    [Fact]
    public void BootstrapGlobalAdmin_then_Set_then_Remove_works_correctly()
    {
        var svc = new InMemoryTenantService();

        // Bootstrap
        var bootstrapResult = svc.ProcessCommand(new BootstrapGlobalAdmin("admin1"));
        bootstrapResult.IsSuccess.ShouldBeTrue();
        bootstrapResult.Events[0].ShouldBeOfType<GlobalAdministratorSet>();

        var gaState = svc.GetGlobalAdminState();
        gaState.ShouldNotBeNull();
        gaState.Administrators.ShouldContain("admin1");
        gaState.Bootstrapped.ShouldBeTrue();

        // Set another admin
        var setResult = svc.ProcessCommand(new SetGlobalAdministrator("admin2"));
        setResult.IsSuccess.ShouldBeTrue();
        setResult.Events[0].ShouldBeOfType<GlobalAdministratorSet>();

        gaState = svc.GetGlobalAdminState();
        gaState!.Administrators.Count.ShouldBe(2);
        gaState.Administrators.ShouldContain("admin2");

        // Remove first admin (should succeed — 2 admins remain after adding admin2)
        var removeResult = svc.ProcessCommand(new RemoveGlobalAdministrator("admin1"));
        removeResult.IsSuccess.ShouldBeTrue();
        removeResult.Events[0].ShouldBeOfType<GlobalAdministratorRemoved>();

        gaState = svc.GetGlobalAdminState();
        gaState!.Administrators.Count.ShouldBe(1);
        gaState.Administrators.ShouldNotContain("admin1");
        gaState.Administrators.ShouldContain("admin2");
    }

    // ─── 3.10: Performance test ───

    [Fact]
    [Trait("Category", "Performance")]
    public void Commands_execute_within_10ms_p95()
    {
        const int totalIterations = 100;
        const int warmupIterations = 5;
        const double maxP95Ms = 10.0;

        var timings = new List<double>(totalIterations);
        var sw = new Stopwatch();

        for (int i = 0; i < totalIterations; i++)
        {
            var svc = new InMemoryTenantService();

            sw.Restart();
            svc.ProcessCommand(new CreateTenant($"t-{i}", $"Tenant {i}", null));
            svc.ProcessCommand(
                new AddUserToTenant($"t-{i}", $"user-{i}", TenantRole.TenantContributor),
                userId: "admin",
                isGlobalAdmin: true);
            sw.Stop();

            timings.Add(sw.Elapsed.TotalMilliseconds);
        }

        // Skip warmup iterations
        var measured = timings.Skip(warmupIterations).OrderBy(t => t).ToList();
        measured.Count.ShouldBeGreaterThan(0, "Insufficient iterations collected");

        int p95Index = (int)Math.Ceiling(measured.Count * 0.95) - 1;
        p95Index = Math.Max(0, Math.Min(p95Index, measured.Count - 1));
        double p95 = measured[p95Index];

        p95.ShouldBeLessThan(maxP95Ms, $"P95 latency was {p95:F3}ms (limit: {maxP95Ms}ms)");
    }

    // ─── Additional: NoOp and Rejection do NOT mutate state ───

    [Fact]
    public void NoOp_result_does_not_mutate_state()
    {
        var svc = new InMemoryTenantService();
        svc.ProcessCommand(new CreateTenant("acme", "Acme Corp", null));
        svc.ProcessCommand(new DisableTenant("acme"));

        int eventCountBefore = svc.EventHistory.Count;

        // DisableTenant on already disabled tenant → NoOp
        var result = svc.ProcessCommand(new DisableTenant("acme"));
        result.IsNoOp.ShouldBeTrue();

        // EventHistory should not have grown
        svc.EventHistory.Count.ShouldBe(eventCountBefore);
    }

    [Fact]
    public void Rejection_result_does_not_mutate_state()
    {
        var svc = new InMemoryTenantService();

        int eventCountBefore = svc.EventHistory.Count;

        // AddUserToTenant on nonexistent tenant → Rejection
        var result = svc.ProcessCommand(
            new AddUserToTenant("nonexistent", "alice", TenantRole.TenantReader),
            userId: "admin",
            isGlobalAdmin: true);
        result.IsRejection.ShouldBeTrue();

        // No state should have been created
        svc.GetTenantState("nonexistent").ShouldBeNull();
        svc.EventHistory.Count.ShouldBe(eventCountBefore);
    }
}
