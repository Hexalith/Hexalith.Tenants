using Hexalith.EventStore.Client.Conventions;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class GlobalAdministratorProjectionTests {
    [Fact]
    public void GetDomainName_GlobalAdministratorProjection_ResolvesToGlobalAdministrators() {
        string domainName = NamingConventionEngine.GetDomainName(typeof(GlobalAdministratorProjection));

        domainName.ShouldBe("global-administrators");
    }

    // P18: Project returns GlobalAdministratorReadModel from events
    [Fact]
    public void Project_WithAdminEvents_ReturnsCorrectState() {
        var projection = new GlobalAdministratorProjection();
        object[] events = new object[]
        {
            new GlobalAdministratorSet("system", "admin1"),
            new GlobalAdministratorSet("system", "admin2"),
            new GlobalAdministratorRemoved("system", "admin1"),
        };

        GlobalAdministratorReadModel result = projection.Project(events);

        result.Administrators.Count.ShouldBe(1);
        result.Administrators.ShouldContain("admin2");
        result.Administrators.ShouldNotContain("admin1");
    }

    // Project with empty events returns default model
    [Fact]
    public void Project_EmptyEventList_ReturnsDefaultModel() {
        var projection = new GlobalAdministratorProjection();

        GlobalAdministratorReadModel result = projection.Project(Array.Empty<object>());

        result.Administrators.ShouldBeEmpty();
    }
}
