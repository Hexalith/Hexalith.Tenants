using Hexalith.Tenants.Testing.Fakes;

using Shouldly;

namespace Hexalith.Tenants.Testing.Tests;

public class ScaffoldingSmokeTests {
    [Fact]
    public void Test_project_is_discoverable() {
        typeof(InMemoryTenantService).Assembly.GetName().Name.ShouldBe("Hexalith.Tenants.Testing");
    }
}
