using Hexalith.EventStore.Client.Conventions;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantIndexProjectionTests {
    // IX19: NamingConventionEngine derives correct domain name
    // Convention strips "Projection" suffix, converts "TenantIndex" to "tenant-index".
    // Must NOT be "tenants" — that's TenantProjection's domain, and the scanner rejects duplicates.
    [Fact]
    public void GetDomainName_TenantIndexProjection_ResolvesToTenantIndex() {
        string domainName = NamingConventionEngine.GetDomainName(typeof(TenantIndexProjection));

        domainName.ShouldBe("tenant-index");
    }

    // IX14: Project returns TenantIndexReadModel from events
    [Fact]
    public void Project_WithTenantCreatedAndUserAdded_ReturnsCorrectState() {
        var projection = new TenantIndexProjection();
        object[] events = new object[]
        {
            new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
            new TenantCreated("beta", "Beta Inc", null, DateTimeOffset.UtcNow),
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            new UserAddedToTenant("beta", "user1", TenantRole.TenantReader),
        };

        TenantIndexReadModel result = projection.Project(events);

        result.Tenants.Count.ShouldBe(2);
        result.UserTenants["user1"].Count.ShouldBe(2);
    }

    // IX15: Project handles all event types with correct final state
    [Fact]
    public void Project_AllSevenEventTypes_ProducesCorrectFinalState() {
        var projection = new TenantIndexProjection();
        object[] events = new object[]
        {
            new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.Parse("2026-01-01T00:00:00Z")),
            new TenantCreated("beta", "Beta Inc", "desc", DateTimeOffset.Parse("2026-01-02T00:00:00Z")),
            new TenantCreated("gamma", "Gamma LLC", null, DateTimeOffset.Parse("2026-01-03T00:00:00Z")),
            new TenantUpdated("acme", "Acme Updated", "new desc"),
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            new UserAddedToTenant("beta", "user1", TenantRole.TenantReader),
            new UserAddedToTenant("acme", "user2", TenantRole.TenantContributor),
            new UserAddedToTenant("gamma", "user3", TenantRole.TenantOwner),
            new UserRoleChanged("beta", "user1", TenantRole.TenantReader, TenantRole.TenantContributor),
            new UserRemovedFromTenant("acme", "user2"),
            new TenantDisabled("gamma", DateTimeOffset.UtcNow),
        };

        TenantIndexReadModel result = projection.Project(events);

        // Tenants assertions
        result.Tenants.Count.ShouldBe(3);
        result.Tenants["acme"].Name.ShouldBe("Acme Updated");
        result.Tenants["acme"].Status.ShouldBe(TenantStatus.Active);
        result.Tenants["beta"].Status.ShouldBe(TenantStatus.Active);
        result.Tenants["gamma"].Status.ShouldBe(TenantStatus.Disabled);

        // UserTenants assertions
        result.UserTenants.Count.ShouldBe(2); // user1, user3 (user2 removed)
        result.UserTenants["user1"].Count.ShouldBe(2); // acme, beta
        result.UserTenants["user1"]["beta"].ShouldBe(TenantRole.TenantContributor); // role changed
        result.UserTenants["user3"]["gamma"].ShouldBe(TenantRole.TenantOwner);
        result.UserTenants.ShouldNotContainKey("user2"); // removed, cleaned up
    }

    // IX16: Project with empty event list returns default model
    [Fact]
    public void Project_EmptyEventList_ReturnsDefaultModel() {
        var projection = new TenantIndexProjection();

        TenantIndexReadModel result = projection.Project(Array.Empty<object>());

        result.Tenants.ShouldBeEmpty();
        result.UserTenants.ShouldBeEmpty();
    }

    // IX17: Project skips null events gracefully
    [Fact]
    public void Project_WithNullEvents_SkipsNullsAndAppliesValid() {
        var projection = new TenantIndexProjection();
        object?[] events = new object?[]
        {
            new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
            null,
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            null,
        };

        TenantIndexReadModel result = projection.Project(events);

        result.Tenants.ShouldContainKey("acme");
        result.UserTenants["user1"].ShouldContainKey("acme");
    }
}
