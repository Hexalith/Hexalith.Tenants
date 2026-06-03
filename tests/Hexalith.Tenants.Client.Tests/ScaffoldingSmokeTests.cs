using Hexalith.Tenants.Client.Handlers;

using Shouldly;

namespace Hexalith.Tenants.Client.Tests;

public class ScaffoldingSmokeTests {
    [Fact]
    public void Client_test_project_references_client_library() {
        typeof(TenantProjectionEventHandler).Assembly.GetName().Name.ShouldBe("Hexalith.Tenants.Client");
    }
}
