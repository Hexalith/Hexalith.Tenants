using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantIndexReadModelTests {
    // IX1: Apply TenantCreated adds entry to Tenants
    [Fact]
    public void Apply_TenantCreated_AddsEntryToTenants() {
        var model = new TenantIndexReadModel();

        model.Apply(new TenantCreated("acme", "Acme Corp", "Test", DateTimeOffset.UtcNow));

        model.Tenants.ShouldContainKey("acme");
        model.Tenants["acme"].Name.ShouldBe("Acme Corp");
        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
        model.UserTenants.ShouldBeEmpty();
    }

    // IX2: Apply TenantUpdated updates name in index
    [Fact]
    public void Apply_TenantUpdated_UpdatesNameInIndex() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new TenantUpdated("acme", "Acme Updated", "new desc"));

        model.Tenants["acme"].Name.ShouldBe("Acme Updated");
        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
    }

    // IX3: Apply TenantDisabled updates status
    [Fact]
    public void Apply_TenantDisabled_UpdatesStatusToDisabled() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Disabled);
    }

    // IX4: Apply TenantEnabled updates status
    [Fact]
    public void Apply_TenantEnabled_UpdatesStatusToActive() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Apply(new TenantEnabled("acme", DateTimeOffset.UtcNow));

        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
    }

    // IX5: Apply UserAddedToTenant adds user-tenant mapping
    [Fact]
    public void Apply_UserAddedToTenant_AddsUserTenantMapping() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));

        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));

        model.UserTenants.ShouldContainKey("user1");
        model.UserTenants["user1"].ShouldContainKey("acme");
        model.UserTenants["user1"]["acme"].ShouldBe(TenantRole.TenantOwner);
    }

    // IX6: Apply UserRemovedFromTenant removes user-tenant mapping
    [Fact]
    public void Apply_UserRemovedFromTenant_RemovesUserTenantMapping() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));

        model.Apply(new UserRemovedFromTenant("acme", "user1"));

        model.UserTenants.ShouldNotContainKey("user1");
    }

    // IX7: Multiple tenants in index
    [Fact]
    public void Apply_MultipleTenantCreated_AllTenantsInIndex() {
        var model = new TenantIndexReadModel();

        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.UtcNow));

        model.Tenants.Count.ShouldBe(3);
        model.Tenants["acme"].Name.ShouldBe("Acme Corp");
        model.Tenants["beta"].Name.ShouldBe("Beta Inc");
        model.Tenants["gamma"].Name.ShouldBe("Gamma LLC");
    }

    // IX8: User in multiple tenants
    [Fact]
    public void Apply_UserAddedToMultipleTenants_AllMappingsPresent() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.UtcNow));

        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));
        model.Apply(new UserAddedToTenant("beta", "user1", TenantRole.TenantReader));
        model.Apply(new UserAddedToTenant("gamma", "user1", TenantRole.TenantContributor));

        model.UserTenants["user1"].Count.ShouldBe(3);
    }

    // IX9: Remove user from one of multiple tenants
    [Fact]
    public void Apply_UserRemovedFromOneOfMultipleTenants_OtherMappingsRemain() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));
        model.Apply(new UserAddedToTenant("beta", "user1", TenantRole.TenantReader));
        model.Apply(new UserAddedToTenant("gamma", "user1", TenantRole.TenantContributor));

        model.Apply(new UserRemovedFromTenant("beta", "user1"));

        model.UserTenants["user1"].Count.ShouldBe(2);
        model.UserTenants["user1"].ShouldNotContainKey("beta");
        model.UserTenants["user1"].ShouldContainKey("acme");
        model.UserTenants["user1"].ShouldContainKey("gamma");
    }

    // IX10: Apply TenantDisabled when tenant not in index (out-of-order)
    [Fact]
    public void Apply_TenantDisabledWhenNotInIndex_NoException() {
        var model = new TenantIndexReadModel();

        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Tenants.ShouldBeEmpty();
    }

    // IX10b: Apply TenantUpdated when tenant not in index (out-of-order)
    [Fact]
    public void Apply_TenantUpdatedWhenNotInIndex_NoException() {
        var model = new TenantIndexReadModel();

        model.Apply(new TenantUpdated("acme", "Acme Updated", "desc"));

        model.Tenants.ShouldBeEmpty();
    }

    // IX11: Apply UserRemovedFromTenant when user not in index
    [Fact]
    public void Apply_UserRemovedFromTenantWhenNotInIndex_NoException() {
        var model = new TenantIndexReadModel();

        model.Apply(new UserRemovedFromTenant("acme", "user1"));

        model.UserTenants.ShouldBeEmpty();
    }

    // IX11b: Apply UserRoleChanged when user not in index (out-of-order)
    [Fact]
    public void Apply_UserRoleChangedWhenNotInIndex_NoException() {
        var model = new TenantIndexReadModel();

        model.Apply(new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantContributor));

        model.UserTenants.ShouldBeEmpty();
    }

    // IX12: Apply UserRoleChanged updates role in user-tenant mapping
    [Fact]
    public void Apply_UserRoleChanged_UpdatesRoleInMapping() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantReader));

        model.Apply(new UserRoleChanged("acme", "user1", TenantRole.TenantReader, TenantRole.TenantContributor));

        model.UserTenants["user1"]["acme"].ShouldBe(TenantRole.TenantContributor);
    }

    [Fact]
    public void Apply_DuplicateTenantCreated_PreservesExistingTenantState() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantUpdated("acme", "Acme Updated", "updated desc"));
        model.Apply(new TenantDisabled("acme", DateTimeOffset.UtcNow));

        model.Apply(new TenantCreated("acme", "Stale Name", null, DateTimeOffset.UtcNow));

        model.Tenants["acme"].Name.ShouldBe("Acme Updated");
        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Disabled);
    }

    [Fact]
    public void Apply_UserAddedToTenantWhenTenantNotInIndex_IgnoresEvent() {
        var model = new TenantIndexReadModel();

        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));

        model.UserTenants.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_UserRoleChangedWhenTenantMappingMissing_DoesNotCreateMembership() {
        var model = new TenantIndexReadModel();
        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow));
        model.Apply(new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantReader));

        model.Apply(new UserRoleChanged("beta", "user1", TenantRole.TenantReader, TenantRole.TenantContributor));

        model.UserTenants["user1"].Count.ShouldBe(1);
        model.UserTenants["user1"]["acme"].ShouldBe(TenantRole.TenantReader);
        model.UserTenants["user1"].ShouldNotContainKey("beta");
    }

    // IX13: Full lifecycle test across multiple tenants and users
    [Fact]
    public void Apply_FullLifecycle_CorrectFinalState() {
        var model = new TenantIndexReadModel();

        model.Apply(new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.Parse("2026-01-01T00:00:00Z")));
        model.Apply(new TenantCreated("beta", "Beta Inc", "desc", DateTimeOffset.Parse("2026-01-02T00:00:00Z")));
        model.Apply(new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.Parse("2026-01-03T00:00:00Z")));
        model.Apply(new TenantUpdated("acme", "Acme Updated", "new desc"));
        model.Apply(new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner));
        model.Apply(new UserAddedToTenant("beta", "user1", TenantRole.TenantReader));
        model.Apply(new UserAddedToTenant("acme", "user2", TenantRole.TenantContributor));
        model.Apply(new UserAddedToTenant("gamma", "user3", TenantRole.TenantOwner));
        model.Apply(new UserRoleChanged("beta", "user1", TenantRole.TenantReader, TenantRole.TenantContributor));
        model.Apply(new UserRemovedFromTenant("acme", "user2"));
        model.Apply(new TenantDisabled("gamma", DateTimeOffset.UtcNow));

        // Tenants assertions
        model.Tenants.Count.ShouldBe(3);
        model.Tenants["acme"].Name.ShouldBe("Acme Updated");
        model.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
        model.Tenants["beta"].Status.ShouldBe(TenantStatus.Active);
        model.Tenants["gamma"].Status.ShouldBe(TenantStatus.Disabled);

        // UserTenants assertions
        model.UserTenants.Count.ShouldBe(2); // user1, user3 (user2 removed)
        model.UserTenants["user1"].Count.ShouldBe(2); // acme, beta
        model.UserTenants["user1"]["beta"].ShouldBe(TenantRole.TenantContributor); // role changed
        model.UserTenants["user3"]["gamma"].ShouldBe(TenantRole.TenantOwner);
        model.UserTenants.ShouldNotContainKey("user2"); // removed, cleaned up
    }

    // Null guard test
    [Fact]
    public void Apply_NullTenantCreated_ThrowsArgumentNullException() {
        var model = new TenantIndexReadModel();
        _ = Should.Throw<ArgumentNullException>(() => model.Apply((TenantCreated)null!));
    }

    // IX18: Canary — TenantIndexReadModel must have exactly 7 Apply methods
    [Fact]
    public void TenantIndexReadModel_HasExactly7ApplyMethods() {
        var applyMethods = typeof(TenantIndexReadModel)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
            .ToList();

        applyMethods.Count.ShouldBe(7, "TenantIndexReadModel should handle 7 event types: TenantCreated, TenantUpdated, TenantDisabled, TenantEnabled, UserAddedToTenant, UserRemovedFromTenant, UserRoleChanged");
    }
}
