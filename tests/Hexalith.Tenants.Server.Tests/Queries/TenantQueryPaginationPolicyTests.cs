using System.Text;

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

    [Fact]
    public void DeserializeStandardPayload_returns_default_first_page_for_empty_payload() {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(null);

        payload.Cursor.ShouldBeNull();
        payload.PageSize.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
    }

    [Fact]
    public void DeserializeStandardPayload_returns_default_first_page_for_malformed_json() {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes("{ not json"));

        payload.Cursor.ShouldBeNull();
        payload.PageSize.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
    }

    [Theory]
    [InlineData("""{"cursor":"cursor-1","pageSize":5}""", "cursor-1", 5)]
    [InlineData("""{"cursor":null,"pageSize":5}""", null, 5)]
    [InlineData("""{"cursor":42,"pageSize":5}""", null, 5)]
    [InlineData("""{"cursor":"cursor-1","pageSize":"5"}""", "cursor-1", 20)]
    [InlineData("""{"cursor":"cursor-1","pageSize":0}""", "cursor-1", 20)]
    [InlineData("""{"cursor":"cursor-1","pageSize":101}""", "cursor-1", 100)]
    public void DeserializeStandardPayload_preserves_existing_standard_field_handling(
        string json,
        string? expectedCursor,
        int expectedPageSize) {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes(json));

        payload.Cursor.ShouldBe(expectedCursor);
        payload.PageSize.ShouldBe(expectedPageSize);
    }
}
