using Hexalith.EventStore.Client.Conventions;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class TenantProjectionTests {
    [Fact]
    public void GetDomainName_TenantProjection_ResolvesToTenants() {
        string domainName = NamingConventionEngine.GetDomainName(typeof(TenantProjection));

        domainName.ShouldBe("tenants");
    }

    // P11: Project returns TenantReadModel from events
    [Fact]
    public void Project_WithTenantCreatedAndUserAdded_ReturnsCorrectState() {
        var projection = new TenantProjection();
        object[] events = new object[]
        {
            new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
        };

        TenantReadModel result = projection.Project(events);

        result.TenantId.ShouldBe("acme");
        result.Name.ShouldBe("Acme Corp");
        result.Members.ShouldContainKey("user1");
        result.Members["user1"].ShouldBe(TenantRole.TenantOwner);
    }

    // P12: Project handles all 9 event types with correct final state
    [Fact]
    public void Project_AllNineEventTypes_ProducesCorrectFinalState() {
        var projection = new TenantProjection();
        var createdAt = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var disabledAt = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        var enabledAt = DateTimeOffset.Parse("2026-01-03T00:00:00Z");
        object[] events = new object[]
        {
            new TenantCreated("acme", "Acme Corp", "Original", createdAt),
            new TenantUpdated("acme", "Acme Updated", "New desc"),
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            new UserRoleChanged("acme", "user1", TenantRole.TenantOwner, TenantRole.TenantContributor),
            new UserRemovedFromTenant("acme", "user1"),
            new TenantConfigurationSet("acme", "theme", "dark"),
            new TenantConfigurationRemoved("acme", "theme"),
            new TenantDisabled("acme", disabledAt),
            new TenantEnabled("acme", enabledAt),
        };

        TenantReadModel result = projection.Project(events);

        result.TenantId.ShouldBe("acme");
        result.Name.ShouldBe("Acme Updated");
        result.Description.ShouldBe("New desc");
        result.Status.ShouldBe(TenantStatus.Active);
        result.CreatedAt.ShouldBe(createdAt);
        result.Members.ShouldBeEmpty();
        result.Members.ShouldNotContainKey("user1");
        result.Configuration.ShouldBeEmpty();
    }

    // P13: Project with empty event list returns default model
    [Fact]
    public void Project_EmptyEventList_ReturnsDefaultModel() {
        var projection = new TenantProjection();

        TenantReadModel result = projection.Project(Array.Empty<object>());

        result.TenantId.ShouldBe(string.Empty);
        result.Name.ShouldBe(string.Empty);
        result.Description.ShouldBeNull();
        result.Members.ShouldBeEmpty();
        result.Configuration.ShouldBeEmpty();
    }

    // P19: Project skips null events gracefully
    [Fact]
    public void Project_WithNullEvents_SkipsNullsAndAppliesValid() {
        var projection = new TenantProjection();
        object?[] events = new object?[]
        {
            new TenantCreated("acme", "Acme Corp", null, DateTimeOffset.UtcNow),
            null,
            new UserAddedToTenant("acme", "user1", TenantRole.TenantOwner),
            null,
        };

        TenantReadModel result = projection.Project(events);

        result.TenantId.ShouldBe("acme");
        result.Members.ShouldContainKey("user1");
    }
}
