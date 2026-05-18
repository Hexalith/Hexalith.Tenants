using Hexalith.Tenants.Queries;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public class TenantQueryPaginationPolicyTests {
    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 1)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]
    public void ClampStandardPageSize_preserves_standard_query_bounds(int requestedPageSize, int expectedPageSize)
        => TenantQueryPaginationPolicy.ClampStandardPageSize(requestedPageSize).ShouldBe(expectedPageSize);

    [Theory]
    [InlineData(0, 100)]
    [InlineData(-1, 100)]
    [InlineData(1, 1)]
    [InlineData(1000, 1000)]
    [InlineData(1001, 1000)]
    public void ClampAuditPageSize_preserves_audit_query_bounds(int requestedPageSize, int expectedPageSize)
        => TenantQueryPaginationPolicy.ClampAuditPageSize(requestedPageSize).ShouldBe(expectedPageSize);

    [Fact]
    public void Policy_exposes_one_source_of_truth_for_standard_and_audit_bounds() {
        TenantQueryPaginationPolicy.StandardDefaultPageSize.ShouldBe(20);
        TenantQueryPaginationPolicy.StandardMaximumPageSize.ShouldBe(100);
        TenantQueryPaginationPolicy.AuditDefaultPageSize.ShouldBe(100);
        TenantQueryPaginationPolicy.AuditMaximumPageSize.ShouldBe(1000);
    }
}
