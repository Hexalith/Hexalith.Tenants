using System.Text;

using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Queries;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

public class TenantQueryPaginationPayloadParserTests {
    [Fact]
    public void DeserializeStandardPayload_returns_default_first_page_for_empty_payload() {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(null);

        payload.Cursor.ShouldBeNull();
        payload.PageSize.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
        payload.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public void DeserializeStandardPayload_returns_default_first_page_for_malformed_json() {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes("{ not json"));

        payload.Cursor.ShouldBeNull();
        payload.PageSize.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
        payload.ErrorMessage.ShouldBeNull();
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
        payload.ErrorMessage.ShouldBeNull();
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
        payload.ErrorMessage.ShouldBeNull();
    }

    /// <summary>Verifies undecodable strings produce an explicit validation failure.</summary>
    [Theory]
    [InlineData(@"\ud800")]
    [InlineData(@"\udfff")]
    [InlineData(@"prefix\udc00suffix")]
    [InlineData(@"\udc00\ud800")]
    public void MalformedUnicodeCursorReturnsValidationError(string cursor)
    {
        byte[] json = Encoding.UTF8.GetBytes($$"""{"cursor":"{{cursor}}","pageSize":1}""");

        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(json);

        payload.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        payload.Cursor.ShouldBeNull();
    }

    /// <summary>Verifies malformed documents default before undecodable strings are accessed.</summary>
    [Theory]
    [InlineData("{\"cursor\":\"\\ud800\",")]
    [InlineData("{\"cursor\":\"\\udc00\",\"pageSize\":1")]
    public void MalformedJsonWithUndecodableCursorRetainsDefaultPage(string json)
    {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes(json));

        payload.ErrorMessage.ShouldBeNull();
        payload.Cursor.ShouldBeNull();
        payload.PageSize.ShouldBe(TenantQueryPaginationPolicy.StandardDefaultPageSize);
    }

    /// <summary>Verifies successfully decoded strings and final duplicate properties preserve semantics.</summary>
    [Theory]
    [InlineData("{\"cursor\":\"\\ud83d\\ude00\",\"pageSize\":1}", "\U0001F600", 1)]
    [InlineData("{\"cursor\":\"\\u0000\\n\",\"pageSize\":2}", "\0\n", 2)]
    [InlineData("{\"cursor\":\"\",\"pageSize\":3}", "", 3)]
    [InlineData("{\"cursor\":\"\\ud800\",\"cursor\":\"\\u0061bc\",\"pageSize\":4}", "abc", 4)]
    [InlineData("{\"cursor\":\"\\ud800\",\"cursor\":null,\"pageSize\":5}", null, 5)]
    [InlineData("{\"cursor\":\"\\ud800\",\"cursor\":42,\"pageSize\":6}", null, 6)]
    [InlineData("{\"cursor\":\"abc\",\"pageSize\":1.5}", "abc", 20)]
    [InlineData("{\"cursor\":\"abc\",\"pageSize\":2147483648}", "abc", 20)]
    [InlineData("{\"cursor\":\"abc\",\"pageSize\":null}", "abc", 20)]
    [InlineData("{\"cursor\":\"abc\",\"pageSize\":1,\"pageSize\":101}", "abc", 100)]
    public void SuccessfullyDecodedCursorAndPageSizeRemainCompatible(string json, string? expectedCursor, int expectedPageSize)
    {
        TenantQueryPaginationPayload payload = TenantQueryPaginationPayloadParser.DeserializeStandardPayload(Encoding.UTF8.GetBytes(json));

        payload.ErrorMessage.ShouldBeNull();
        payload.Cursor.ShouldBe(expectedCursor);
        payload.PageSize.ShouldBe(expectedPageSize);
    }
}
