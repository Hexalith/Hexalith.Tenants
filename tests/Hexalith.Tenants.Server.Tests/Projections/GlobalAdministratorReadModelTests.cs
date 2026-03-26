using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Server.Projections;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Projections;

public class GlobalAdministratorReadModelTests {
    // P14: Apply GlobalAdministratorSet adds administrator
    [Fact]
    public void Apply_GlobalAdministratorSet_AddsAdministrator() {
        var model = new GlobalAdministratorReadModel();

        model.Apply(new GlobalAdministratorSet("system", "user1"));

        model.Administrators.ShouldContain("user1");
    }

    // P15: Apply GlobalAdministratorRemoved removes administrator
    [Fact]
    public void Apply_GlobalAdministratorRemoved_RemovesAdministrator() {
        var model = new GlobalAdministratorReadModel();
        model.Apply(new GlobalAdministratorSet("system", "user1"));

        model.Apply(new GlobalAdministratorRemoved("system", "user1"));

        model.Administrators.ShouldNotContain("user1");
    }

    // P16: Apply multiple GlobalAdministratorSet events
    [Fact]
    public void Apply_MultipleGlobalAdministratorSet_AddsAllAdministrators() {
        var model = new GlobalAdministratorReadModel();

        model.Apply(new GlobalAdministratorSet("system", "user1"));
        model.Apply(new GlobalAdministratorSet("system", "user2"));
        model.Apply(new GlobalAdministratorSet("system", "user3"));

        model.Administrators.Count.ShouldBe(3);
        model.Administrators.ShouldContain("user1");
        model.Administrators.ShouldContain("user2");
        model.Administrators.ShouldContain("user3");
    }

    // P17: GlobalAdministratorSet is idempotent (HashSet)
    [Fact]
    public void Apply_DuplicateGlobalAdministratorSet_IsIdempotent() {
        var model = new GlobalAdministratorReadModel();

        model.Apply(new GlobalAdministratorSet("system", "user1"));
        model.Apply(new GlobalAdministratorSet("system", "user1"));

        model.Administrators.Count.ShouldBe(1);
    }

    // Null guard test
    [Fact]
    public void Apply_NullGlobalAdministratorSet_ThrowsArgumentNullException() {
        var model = new GlobalAdministratorReadModel();
        _ = Should.Throw<ArgumentNullException>(() => model.Apply((GlobalAdministratorSet)null!));
    }

    // P21: Canary — GlobalAdministratorReadModel must have exactly 2 Apply methods
    [Fact]
    public void GlobalAdministratorReadModel_HasExactly2ApplyMethods() {
        var applyMethods = typeof(GlobalAdministratorReadModel)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(m => m.Name == "Apply" && m.ReturnType == typeof(void) && m.GetParameters().Length == 1)
            .ToList();

        applyMethods.Count.ShouldBe(2, "GlobalAdministratorReadModel should handle all 2 admin event types");
    }
}
