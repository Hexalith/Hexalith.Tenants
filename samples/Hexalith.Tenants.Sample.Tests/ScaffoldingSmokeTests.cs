using Hexalith.Tenants.Sample.Endpoints;

using Shouldly;

namespace Hexalith.Tenants.Sample.Tests;

public class ScaffoldingSmokeTests {
    [Fact]
    public void Sample_test_project_references_sample_application() {
        typeof(AccessCheckEndpoints).Assembly.GetName().Name.ShouldBe("Hexalith.Tenants.Sample");
    }
}
