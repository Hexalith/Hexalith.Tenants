using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Events.Rejections;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;
using Hexalith.Tenants.Testing.Projections;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests.Projections;

/// <summary>
/// Unit tests for <see cref="InMemoryTenantProjection"/>.
/// </summary>
public sealed class InMemoryTenantProjectionTests
{
    // ─── 3.2: TenantCreated ───

    [Fact]
    public void Apply_TenantCreated_StoresTenantWithCorrectFields()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        var evt = new TenantCreated("acme", "Acme Corp", "A test tenant", DateTimeOffset.UtcNow);

        // Act
        projection.Apply(evt);

        // Assert
        var tenant = projection.GetTenant("acme");
        tenant.ShouldNotBeNull();
        tenant.TenantId.ShouldBe("acme");
        tenant.Name.ShouldBe("Acme Corp");
        tenant.Description.ShouldBe("A test tenant");
        tenant.Status.ShouldBe(TenantStatus.Active);
    }

    // ─── 3.3: TenantUpdated ───

    [Fact]
    public void Apply_TenantUpdated_UpdatesNameAndDescription()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        // Act
        projection.Apply(new TenantUpdated("acme", "Acme Inc", "Updated description"));

        // Assert
        var tenant = projection.GetTenant("acme");
        tenant.ShouldNotBeNull();
        tenant.Name.ShouldBe("Acme Inc");
        tenant.Description.ShouldBe("Updated description");
    }

    // ─── 3.4: TenantDisabled / TenantEnabled ───

    [Fact]
    public void Apply_TenantDisabled_TracksStatus()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        projection.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        // Assert
        projection.GetTenant("acme")!.Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Apply_TenantEnabled_TracksStatus()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        projection.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        // Act
        projection.Apply(new TenantEnabled("acme", DateTimeOffset.UtcNow));

        // Assert
        projection.GetTenant("acme")!.Status.ShouldBe(TenantStatus.Active);
    }

    // ─── 3.5: UserAddedToTenant ───

    [Fact]
    public void Apply_UserAddedToTenant_AddsMemberWithRole()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        projection.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        // Assert
        var tenant = projection.GetTenant("acme");
        tenant.ShouldNotBeNull();
        tenant.Members.ShouldContainKey("alice");
        tenant.Members["alice"].ShouldBe(TenantRole.TenantContributor);
    }

    // ─── 3.6: UserRemovedFromTenant ───

    [Fact]
    public void Apply_UserRemovedFromTenant_RemovesMember()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        projection.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantContributor));

        // Act
        projection.Apply(new UserRemovedFromTenant("acme", "alice"));

        // Assert
        projection.GetTenant("acme")!.Members.ShouldNotContainKey("alice");
    }

    // ─── 3.7: UserRoleChanged ───

    [Fact]
    public void Apply_UserRoleChanged_UpdatesMemberRole()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        projection.Apply(new UserAddedToTenant("acme", "alice", TenantRole.TenantReader));

        // Act
        projection.Apply(new UserRoleChanged("acme", "alice", TenantRole.TenantReader, TenantRole.TenantOwner));

        // Assert
        projection.GetTenant("acme")!.Members["alice"].ShouldBe(TenantRole.TenantOwner);
    }

    // ─── 3.8: TenantConfigurationSet / TenantConfigurationRemoved ───

    [Fact]
    public void Apply_TenantConfigurationSet_ManagesConfig()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));

        // Act
        projection.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        // Assert
        var tenant = projection.GetTenant("acme");
        tenant.ShouldNotBeNull();
        tenant.Configuration.ShouldContainKey("theme");
        tenant.Configuration["theme"].ShouldBe("dark");
    }

    [Fact]
    public void Apply_TenantConfigurationRemoved_RemovesConfig()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        projection.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        // Act
        projection.Apply(new TenantConfigurationRemoved("acme", "theme"));

        // Assert
        projection.GetTenant("acme")!.Configuration.ShouldNotContainKey("theme");
    }

    // ─── 3.9: GlobalAdministratorSet / GlobalAdministratorRemoved ───

    [Fact]
    public void Apply_GlobalAdministratorSet_TracksAdmins()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();

        // Act
        projection.Apply(new GlobalAdministratorSet("system", "admin1"));

        // Assert
        projection.GetGlobalAdministrators().Administrators.ShouldContain("admin1");
    }

    [Fact]
    public void Apply_GlobalAdministratorRemoved_RemovesAdmin()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new GlobalAdministratorSet("system", "admin1"));
        projection.Apply(new GlobalAdministratorSet("system", "admin2"));

        // Act
        projection.Apply(new GlobalAdministratorRemoved("system", "admin1"));

        // Assert
        projection.GetGlobalAdministrators().Administrators.ShouldNotContain("admin1");
        projection.GetGlobalAdministrators().Administrators.ShouldContain("admin2");
    }

    // ─── 3.10: GetAllTenants ───

    [Fact]
    public void GetAllTenants_ReturnsAllProjectedTenants()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("acme", "Acme", null, DateTimeOffset.UtcNow));
        projection.Apply(new TenantCreated("contoso", "Contoso", null, DateTimeOffset.UtcNow));

        // Act
        var tenants = projection.GetAllTenants();

        // Assert
        tenants.Count.ShouldBe(2);
        tenants.ShouldContain(t => t.TenantId == "acme");
        tenants.ShouldContain(t => t.TenantId == "contoso");
    }

    // ─── 3.11: Cross-tenant isolation ───

    [Fact]
    public void CrossTenantIsolation_TenantADataNeverAppearsInTenantBQuery()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();
        projection.Apply(new TenantCreated("alpha", "Alpha Corp", null, DateTimeOffset.UtcNow));
        projection.Apply(new TenantCreated("beta", "Beta Corp", null, DateTimeOffset.UtcNow));
        projection.Apply(new UserAddedToTenant("alpha", "alice", TenantRole.TenantOwner));
        projection.Apply(new TenantConfigurationSet("alpha", "key1", "val1"));

        // Act
        var alpha = projection.GetTenant("alpha");
        var beta = projection.GetTenant("beta");

        // Assert
        alpha.ShouldNotBeNull();
        alpha.Members.ShouldContainKey("alice");
        alpha.Configuration.ShouldContainKey("key1");

        beta.ShouldNotBeNull();
        beta.Members.ShouldBeEmpty();
        beta.Configuration.ShouldBeEmpty();
    }

    // ─── 3.12: End-to-end with InMemoryTenantService ───

    [Fact]
    public void EndToEnd_InMemoryTenantServiceCommand_EventsProjection_Query()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        TenantTestHelpers.CreateTenant(svc, "acme", "Acme Corp");
        svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
            userId: "admin",
            isGlobalAdmin: true);

        var projection = new InMemoryTenantProjection();

        // Act
        projection.ApplyEvents(svc.EventHistory);

        // Assert
        var tenant = projection.GetTenant("acme");
        tenant.ShouldNotBeNull();
        tenant.TenantId.ShouldBe("acme");
        tenant.Name.ShouldBe("Acme Corp");
        tenant.Status.ShouldBe(TenantStatus.Active);
        tenant.Members.ShouldContainKey("alice");
        tenant.Members["alice"].ShouldBe(TenantRole.TenantContributor);
    }

    // ─── Additional: Rejection event is silently skipped ───

    [Fact]
    public void Apply_RejectionEvent_IsSilentlySkipped()
    {
        // Arrange
        var projection = new InMemoryTenantProjection();

        // Act — applying a rejection event should not throw
        projection.Apply(new TenantAlreadyExistsRejection("acme"));

        // Assert — no tenant created, no exception
        projection.GetTenant("acme").ShouldBeNull();
    }

    // ─── Additional: GetTenant returns null for unknown tenant ───

    [Fact]
    public void GetTenant_UnknownTenantId_ReturnsNull()
    {
        var projection = new InMemoryTenantProjection();
        projection.GetTenant("nonexistent").ShouldBeNull();
    }

    // ─── Additional: GetGlobalAdministrators returns empty on fresh projection ───

    [Fact]
    public void GetGlobalAdministrators_FreshProjection_ReturnsEmptyModel()
    {
        var projection = new InMemoryTenantProjection();
        var admins = projection.GetGlobalAdministrators();
        admins.ShouldNotBeNull();
        admins.Administrators.ShouldBeEmpty();
    }

    [Fact]
    public void Projection_Replay_IsIdempotent()
    {
        // Arrange
        var svc = new InMemoryTenantService();
        TenantTestHelpers.CreateTenant(svc, "acme", "Acme Corp");
        svc.ProcessCommand(
            new AddUserToTenant("acme", "alice", TenantRole.TenantContributor),
            userId: "admin",
            isGlobalAdmin: true);

        var stream = svc.EventHistory;

        // Act
        var projection1 = new InMemoryTenantProjection();
        projection1.ApplyEvents(stream);

        var projection2 = new InMemoryTenantProjection();
        projection2.ApplyEvents(stream);

        // Assert
        var t1 = projection1.GetTenant("acme");
        var t2 = projection2.GetTenant("acme");

        t1.ShouldNotBeNull();
        t2.ShouldNotBeNull();
        t1.Name.ShouldBe(t2.Name);
        t1.Members.Count.ShouldBe(t2.Members.Count);
        t1.Members["alice"].ShouldBe(t2.Members["alice"]);
    }
}
