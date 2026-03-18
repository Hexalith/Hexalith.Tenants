using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantReadModelTests
{
    // P1: Apply TenantCreated sets all properties
    [Fact]
    public void Apply_TenantCreated_SetsAllProperties()
    {
        var model = new TenantReadModel();
        var createdAt = DateTimeOffset.Parse("2026-01-15T10:30:00+00:00");

        model.Apply(new TenantCreated("acme", "Acme Corp", "Test tenant", createdAt));

        model.TenantId.ShouldBe("acme");
        model.Name.ShouldBe("Acme Corp");
        model.Description.ShouldBe("Test tenant");
        model.Status.ShouldBe(TenantStatus.Active);
        model.CreatedAt.ShouldBe(createdAt);
        model.Members.ShouldBeEmpty();
        model.Configuration.ShouldBeEmpty();
    }

    // P2: Apply TenantUpdated updates name and description
    [Fact]
    public void Apply_TenantUpdated_UpdatesNameAndDescription()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", "Original", DateTimeOffset.UtcNow));

        model.Apply(new TenantUpdated("acme", "Acme Updated", "New desc"));

        model.Name.ShouldBe("Acme Updated");
        model.Description.ShouldBe("New desc");
    }

    // P3: Apply TenantDisabled sets status
    [Fact]
    public void Apply_TenantDisabled_SetsStatusToDisabled()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Status.ShouldBe(TenantStatus.Disabled);
    }

    // P4: Apply TenantEnabled sets status
    [Fact]
    public void Apply_TenantEnabled_SetsStatusToActive()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Apply(new TenantEnabled("acme", DateTimeOffset.UtcNow));

        model.Status.ShouldBe(TenantStatus.Active);
    }

    // P5: Apply UserAddedToTenant adds member
    [Fact]
    public void Apply_UserAddedToTenant_AddsMember()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));

        model.Members.ShouldContainKey("user1");
        model.Members["user1"].ShouldBe(TenantRole.TenantOwner);
    }

    // P6: Apply UserRemovedFromTenant removes member
    [Fact]
    public void Apply_UserRemovedFromTenant_RemovesMember()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));

        model.Apply(new UserRemovedFromTenant("acme", "user1"));

        model.Members.ShouldNotContainKey("user1");
    }

    // P7: Apply UserRoleChanged updates role
    [Fact]
    public void Apply_UserRoleChanged_UpdatesRole()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantReader));

        model.Apply(new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantContributor));

        model.Members["user1"].ShouldBe(TenantRole.TenantContributor);
    }

    // P8: Apply TenantConfigurationSet adds config
    [Fact]
    public void Apply_TenantConfigurationSet_AddsConfig()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        model.Configuration.ShouldContainKey("theme");
        model.Configuration["theme"].ShouldBe("dark");
    }

    // P9: Apply TenantConfigurationRemoved removes config
    [Fact]
    public void Apply_TenantConfigurationRemoved_RemovesConfig()
    {
        var model = new TenantReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantConfigurationSet("acme", "theme", "dark"));

        model.Apply(new TenantConfigurationRemoved("acme", "theme"));

        model.Configuration.ShouldNotContainKey("theme");
    }

    // P10: Apply multiple events in sequence — full lifecycle
    [Fact]
    public void Apply_MultipleEventsInSequence_ReflectsAllMutations()
    {
        var model = new TenantReadModel();
        var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        model.Apply(new TenantCreated("acme", "Acme Corp", "Original", createdAt));
        model.Apply(new TenantUpdated("acme", "Acme Updated", "New desc"));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));
        model.Apply(new UserAddedToTenant("acme", "user2", TenantRole.TenantReader));
        model.Apply(new UserRoleChanged("acme", "user2", TenantRole.TenantReader, TenantRole.TenantContributor));
        model.Apply(new UserRemovedFromTenant("acme", "user1"));
        model.Apply(new TenantConfigurationSet("acme", "theme", "dark"));
        model.Apply(new TenantConfigurationRemoved("acme", "theme"));
        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.TenantId.ShouldBe("acme");
        model.Name.ShouldBe("Acme Updated");
        model.Description.ShouldBe("New desc");
        model.Status.ShouldBe(TenantStatus.Disabled);
        model.CreatedAt.ShouldBe(createdAt);
        model.Members.Count.ShouldBe(1);
        model.Members.ShouldContainKey("user2");
        model.Members["user2"].ShouldBe(TenantRole.TenantContributor);
        model.Members.ShouldNotContainKey("user1");
        model.Configuration.ShouldBeEmpty();
    }

    // Null guard test
    [Fact]
    public void Apply_NullTenantCreated_ThrowsArgumentNullException()
    {
        var model = new TenantReadModel();
        Should.Throw<ArgumentNullException>(() => model.Apply((TenantCreated)null!));
    }

    // P20: Canary — TenantReadModel must have exactly 9 Apply methods
    [Fact]
    public void TenantReadModel_HasExactly9ApplyMethods()
    {
        var applyMethods = typeof(TenantReadModel)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
            .ToList();

        applyMethods.Count.ShouldBe(9, "TenantReadModel should handle all 9 tenant event types");
    }
}
