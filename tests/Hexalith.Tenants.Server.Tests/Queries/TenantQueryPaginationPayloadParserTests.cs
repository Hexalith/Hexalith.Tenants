using System.Text;

using Hexalith.Tenants.Queries;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public class TenantQueryPaginationPayloadParserTests {
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
    [InlineData("[]")]
    [InlineData("[{\"cursor\":\"x\"}]")]
    [InlineData("42")]
    [InlineData("\"hello\"")]
    [InlineData("true")]
    [InlineData("null")]
    public void DeserializeStandardPayload_returns_default_first_page_for_non_object_root(string json) {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes(json));

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
