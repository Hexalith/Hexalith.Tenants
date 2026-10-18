using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>Verifies direct audit filter validation through the production query handler.</summary>
public sealed class GetTenantAuditQueryHandlerTests
{
    /// <summary>Gets malformed category Unicode sequences with cursor and authorization variants.</summary>
    public static TheoryData<string, bool, string> MalformedUnicodeCategories
    {
        get
        {
            TheoryData<string, bool, string> cases = [];
            foreach (string category in new[]
            {
                "\\ud800", "\\udbff", "\\udc00", "\\udfff",
                "access\\ud800", "\\udc00access", "\\ud800x\\udc00", "\\udc00\\ud800",
            })
            {
                foreach (bool withCursor in new[] { false, true })
                {
                    cases.Add(category, withCursor, "admin-1");
                    cases.Add(category, withCursor, "non-admin");
                }
            }

            return cases;
        }
    }

    /// <summary>Contains Unicode decoding failures after authorization and before cursor or audit access.</summary>
    [Theory]
    [MemberData(nameof(MalformedUnicodeCategories))]
    public async Task MalformedUnicodeCategoryReturnsFailureBeforeCursorOrAuditReadAsync(
        string category,
        bool withCursor,
        string userId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        string cursor = withCursor ? ",\"cursor\":\"invalid-cursor\"" : string.Empty;

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"category\":\"{category}\"{cursor}}}", userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Gets malformed UTF-8 category bytes without transcoding them through a string.</summary>
    public static TheoryData<byte[], string> MalformedUtf8Categories
    {
        get
        {
            TheoryData<byte[], string> cases = [];
            foreach (byte[] category in new byte[][]
            {
                [0x80], [0xc0, 0xaf], [0xed, 0xa0, 0x80], [0xf4, 0x90, 0x80, 0x80],
            })
            {
                cases.Add(category, "admin-1");
                cases.Add(category, "non-admin");
            }

            return cases;
        }
    }

    /// <summary>Rejects raw malformed category bytes while preserving authority and cursor precedence.</summary>
    [Theory]
    [MemberData(nameof(MalformedUtf8Categories))]
    public async Task MalformedUtf8CategoryReturnsFailureBeforeCursorOrAuditReadAsync(byte[] category, string userId)
    {
        ArgumentNullException.ThrowIfNull(category);
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        byte[] payload =
        [
            .. Encoding.UTF8.GetBytes("{\"category\":\""),
            .. category,
            .. Encoding.UTF8.GetBytes("\",\"cursor\":\"invalid-cursor\"}"),
        ];

        QueryResult result = await ExecuteAsync(store, codec, payload, userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Preserves filtering for successfully decoded escaped category names.</summary>
    [Theory]
    [InlineData("\\u0061ccess", "access")]
    [InlineData("\\u0041CCESS", "access")]
    [InlineData("\\u0030", "access")]
    [InlineData("\\u0020\\u0009", "before,from,access,to,after")]
    public async Task EscapedCategoryStringsRetainExistingFilteringAsync(string category, string expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(expectedEventIds);

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCursorCodec(), $"{{\"category\":\"{category}\"}}");

        ReadEventIds(result).ShouldBe(expectedEventIds.Split(','));
    }

    /// <summary>Distinguishes valid Unicode strings from malformed surrogate decoding.</summary>
    [Theory]
    [InlineData("\\ud83d\\ude00", "\U0001F600")]
    [InlineData("\\u00e9", "\u00e9")]
    public async Task ValidUnicodeUnknownCategoryRetainsExistingDiagnosticAsync(string category, string decodedCategory)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"category\":\"{category}\"}}");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe($"{QueryAdapterFailureReason.InvalidEnvelope}: Invalid audit category: {decodedCategory}");
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Validates only the effective final category without changing duplicate-property semantics.</summary>
    [Theory]
    [InlineData("{\"category\":\"\\ud800\",\"category\":\"access\"}", "access")]
    [InlineData("{\"category\":\"\\udc00\",\"category\":\"access\"}", "access")]
    [InlineData("{\"category\":\"access\",\"category\":\"\\ud800\"}", null)]
    [InlineData("{\"category\":\"access\",\"category\":\"\\udc00\"}", null)]
    [InlineData("{\"category\":\"\\ud800\",\"category\":null}", "before,from,access,to,after")]
    [InlineData("{\"category\":\"\\udc00\",\"category\":null}", "before,from,access,to,after")]
    [InlineData("{\"category\":\"\\ud800\",\"category\":\"   \"}", "before,from,access,to,after")]
    [InlineData("{\"category\":\"\\udc00\",\"category\":\"   \"}", "before,from,access,to,after")]
    [InlineData("{\"category\":\"\\ud800\",\"category\":\"0\"}", "access")]
    [InlineData("{\"category\":\"\\udc00\",\"category\":\"0\"}", "access")]
    [InlineData("{\"category\":\"\\ud800\",\"category\":\"99\"}", "")]
    [InlineData("{\"category\":\"\\udc00\",\"category\":\"99\"}", "")]
    public async Task DuplicateUnicodeCategoryRetainsLastValueSemanticsAsync(string payload, string? expectedEventIds)
    {
        IReadModelStore store = CreateStore();
        bool expectedSuccess = expectedEventIds is not null;
        IQueryCursorCodec codec = expectedSuccess ? CreateCursorCodec() : Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload);

        result.Success.ShouldBe(expectedSuccess);
        if (expectedEventIds is not null)
        {
            ReadEventIds(result).ShouldBe(expectedEventIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }
        else
        {
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
            result.PayloadBytes.ShouldBeNull();
            codec.ReceivedCalls().ShouldBeEmpty();
            await AssertOnlyAuthorityWasReadAsync(store);
        }
    }

    /// <summary>Preserves protected cursor scope across equivalent escaped and plain category spellings.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EquivalentCategorySpellingsResumeProtectedCursorAsync(bool escapedFirst)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCursorCodec();
        string firstCategory = escapedFirst ? "\\u0061dministrative" : "administrative";
        string secondCategory = escapedFirst ? "administrative" : "\\u0061dministrative";

        QueryResult first = await ExecuteAsync(store, codec, $"{{\"category\":\"{firstCategory}\",\"pageSize\":2}}");

        ReadEventIds(first).ShouldBe(["before", "from"]);
        using JsonDocument firstPage = JsonDocument.Parse(first.PayloadBytes.ShouldNotBeNull());
        firstPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        string cursor = firstPage.RootElement.GetProperty("cursor").GetString().ShouldNotBeNull();
        cursor.ShouldNotBeNullOrWhiteSpace();

        QueryResult second = await ExecuteAsync(store, codec,
            $"{{\"category\":\"{secondCategory}\",\"cursor\":{JsonSerializer.Serialize(cursor)},\"pageSize\":2}}");

        ReadEventIds(second).ShouldBe(["to", "after"]);
        using JsonDocument secondPage = JsonDocument.Parse(second.PayloadBytes.ShouldNotBeNull());
        secondPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        secondPage.RootElement.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>Gets malformed cursor and stream Unicode sequences with authorization variants.</summary>
    public static TheoryData<string, string, string> MalformedUnicodeCursorsAndStreams
    {
        get
        {
            TheoryData<string, string, string> cases = [];
            foreach (string propertyName in new[] { "cursor", "stream" })
            {
                foreach (string value in new[] { "\\ud800", "\\udc00" })
                {
                    cases.Add(propertyName, value, "admin-1");
                    cases.Add(propertyName, value, "non-admin");
                }
            }

            return cases;
        }
    }

    /// <summary>Contains cursor and stream Unicode failures after authorization and before codec or audit access.</summary>
    [Theory]
    [MemberData(nameof(MalformedUnicodeCursorsAndStreams))]
    public async Task MalformedUnicodeCursorOrStreamReturnsFailureBeforeCodecOrAuditReadAsync(
        string propertyName,
        string value,
        string userId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(
            store,
            codec,
            $"{{\"category\":\"access\",\"{propertyName}\":\"{value}\"}}",
            userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Gets malformed UTF-8 cursor and stream bytes without transcoding them through a string.</summary>
    public static TheoryData<string, byte[], string> MalformedUtf8CursorsAndStreams
    {
        get
        {
            TheoryData<string, byte[], string> cases = [];
            foreach (string propertyName in new[] { "cursor", "stream" })
            {
                foreach (byte[] value in new byte[][]
                {
                    [0x80], [0xc0, 0xaf], [0xed, 0xa0, 0x80], [0xf4, 0x90, 0x80, 0x80],
                })
                {
                    cases.Add(propertyName, value, "admin-1");
                    cases.Add(propertyName, value, "non-admin");
                }
            }

            return cases;
        }
    }

    /// <summary>Rejects raw malformed cursor and stream bytes before codec use or audit reads.</summary>
    [Theory]
    [MemberData(nameof(MalformedUtf8CursorsAndStreams))]
    public async Task MalformedUtf8CursorOrStreamReturnsFailureBeforeCodecOrAuditReadAsync(
        string propertyName,
        byte[] value,
        string userId)
    {
        ArgumentNullException.ThrowIfNull(value);
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        byte[] payload =
        [
            .. Encoding.UTF8.GetBytes($"{{\"category\":\"access\",\"{propertyName}\":\""),
            .. value,
            .. Encoding.UTF8.GetBytes("\"}"),
        ];

        QueryResult result = await ExecuteAsync(store, codec, payload, userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Contains mixed malformed cursor and stream payloads without changing authorization precedence.</summary>
    [Theory]
    [InlineData("{\"category\":\"\\ud800\",\"cursor\":\"\\udc00\"}", "admin-1")]
    [InlineData("{\"category\":\"\\udc00\",\"cursor\":\"\\ud800\"}", "admin-1")]
    [InlineData("{\"category\":\"\\ud800\",\"stream\":\"\\udc00\"}", "admin-1")]
    [InlineData("{\"category\":\"\\udc00\",\"stream\":\"\\ud800\"}", "admin-1")]
    [InlineData("{\"stream\":\"\\ud800\",\"cursor\":\"invalid-cursor\"}", "admin-1")]
    [InlineData("{\"stream\":\"\\udc00\",\"cursor\":\"invalid-cursor\"}", "admin-1")]
    [InlineData("{\"category\":\"\\ud800\",\"cursor\":\"\\udc00\"}", "non-admin")]
    [InlineData("{\"category\":\"\\ud800\",\"stream\":\"\\udc00\"}", "non-admin")]
    [InlineData("{\"stream\":\"\\ud800\",\"cursor\":\"invalid-cursor\"}", "non-admin")]
    public async Task MixedMalformedCursorOrStreamReturnsFailureBeforeCodecOrAuditReadAsync(string payload, string userId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload, userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Preserves stream selection and protected cursor scope across equivalent spellings.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EquivalentStreamSpellingsResumeProtectedCursorAsync(bool escapedFirst)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCursorCodec();
        string firstStream = escapedFirst ? "\\u0074enant" : "tenant";
        string secondStream = escapedFirst ? "tenant" : "\\u0074enant";

        QueryResult first = await ExecuteAsync(store, codec, $"{{\"stream\":\"{firstStream}\",\"pageSize\":3}}");

        ReadEventIds(first).ShouldBe(["before", "from", "access"]);
        using JsonDocument firstPage = JsonDocument.Parse(first.PayloadBytes.ShouldNotBeNull());
        firstPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        string cursor = firstPage.RootElement.GetProperty("cursor").GetString().ShouldNotBeNull();
        cursor.ShouldNotBeNullOrWhiteSpace();
        string cursorJson = escapedFirst ? "\"" + ToJsonUnicodeEscapes(cursor) + "\"" : JsonSerializer.Serialize(cursor);

        QueryResult second = await ExecuteAsync(
            store,
            codec,
            $"{{\"stream\":\"{secondStream}\",\"cursor\":{cursorJson},\"pageSize\":3}}");

        ReadEventIds(second).ShouldBe(["to", "after"]);
        using JsonDocument secondPage = JsonDocument.Parse(second.PayloadBytes.ShouldNotBeNull());
        secondPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        secondPage.RootElement.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>Sends a successfully decoded unprotected cursor to the existing codec rejection.</summary>
    [Fact]
    public async Task DecodedSurrogatePairCursorReturnsInvalidCursorBeforeAuditReadAsync()
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCursorCodec();

        QueryResult result = await ExecuteAsync(store, codec, "{\"cursor\":\"\\ud83d\\ude00\"}");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        result.PayloadBytes.ShouldBeNull();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Gets embedded Unicode date failures with both bound, cursor and authorization variants.</summary>
    public static TheoryData<string, string, bool, string> MalformedUnicodeDates
    {
        get
        {
            TheoryData<string, string, bool, string> cases = [];
            foreach (string propertyName in new[] { "from", "to" })
            {
                foreach (string date in new[]
                {
                    "2026-05-14T10:00:00Z\\ud800", "2026-05-14T10:00:00Z\\udbff",
                    "2026-05-14T10:00:00Z\\udc00", "2026-05-14T10:00:00Z\\udfff",
                    "\\ud8002026-05-14T10:00:00Z", "\\udc002026-05-14T10:00:00Z",
                    "2026-05-14T10:00:00\\ud800Z", "2026-05-14T10:00:00\\udc00Z",
                    "2026-05-14T10:00:00Z\\udc00\\ud800", "2026-05-14T10:00:00Z\\ud800x\\udc00",
                })
                {
                    foreach (bool withCursor in new[] { false, true })
                    {
                        cases.Add(propertyName, date, withCursor, "admin-1");
                        cases.Add(propertyName, date, withCursor, "non-admin");
                    }
                }
            }

            return cases;
        }
    }

    /// <summary>Contains malformed date decoding after authorization and before codec or audit access.</summary>
    [Theory]
    [MemberData(nameof(MalformedUnicodeDates))]
    public async Task MalformedUnicodeDateReturnsFailureBeforeCodecOrAuditReadAsync(
        string propertyName,
        string date,
        bool withCursor,
        string userId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        string cursor = withCursor ? ",\"cursor\":\"invalid-cursor\"" : string.Empty;

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"{propertyName}\":\"{date}\"{cursor}}}", userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Rejects malformed bounds even when the other bound is valid, before cursor or audit access.</summary>
    [Theory]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"to\":\"2026-05-14T11:00:00Z\\udc00\",\"cursor\":\"invalid-cursor\"}", "admin-1")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\\ud800\",\"to\":\"2026-05-14T11:00:00Z\",\"cursor\":\"invalid-cursor\"}", "admin-1")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"to\":\"2026-05-14T11:00:00Z\\udc00\",\"cursor\":\"invalid-cursor\"}", "non-admin")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\\ud800\",\"to\":\"2026-05-14T11:00:00Z\",\"cursor\":\"invalid-cursor\"}", "non-admin")]
    public async Task MixedValidAndMalformedDatesReturnFailureBeforeCodecOrAuditReadAsync(string payload, string userId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload, userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Gets malformed UTF-8 date bytes without transcoding them through a string.</summary>
    public static TheoryData<string, byte[], string> MalformedUtf8Dates
    {
        get
        {
            TheoryData<string, byte[], string> cases = [];
            foreach (string propertyName in new[] { "from", "to" })
            {
                foreach (byte[] dateSuffix in new byte[][]
                {
                    [0x80], [0xc0, 0xaf], [0xed, 0xa0, 0x80], [0xf4, 0x90, 0x80, 0x80],
                })
                {
                    cases.Add(propertyName, dateSuffix, "admin-1");
                    cases.Add(propertyName, dateSuffix, "non-admin");
                }
            }

            return cases;
        }
    }

    /// <summary>Rejects malformed UTF-8 date suffixes while preserving authorization and cursor precedence.</summary>
    [Theory]
    [MemberData(nameof(MalformedUtf8Dates))]
    public async Task MalformedUtf8DateReturnsFailureBeforeCodecOrAuditReadAsync(
        string propertyName,
        byte[] dateSuffix,
        string userId)
    {
        ArgumentNullException.ThrowIfNull(dateSuffix);
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        byte[] payload =
        [
            .. Encoding.UTF8.GetBytes($"{{\"{propertyName}\":\"2026-05-14T10:00:00Z"),
            .. dateSuffix,
            .. Encoding.UTF8.GetBytes("\",\"cursor\":\"invalid-cursor\"}"),
        ];

        QueryResult result = await ExecuteAsync(store, codec, payload, userId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(userId == "admin-1"
            ? QueryAdapterFailureReason.InvalidEnvelope
            : QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Preserves inclusive offset bounds and protected cursor scope across escaped date spellings.</summary>
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    public async Task EquivalentDateSpellingsResumeProtectedCursorAsync(bool escapedFirst, bool normalizeOffset)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCursorCodec();
        string from = "2026-05-14T12:00:00+02:00";
        string to = "2026-05-14T11:00:00Z";
        string firstFrom = escapedFirst ? ToJsonUnicodeEscapes(from) : from;
        string firstTo = escapedFirst ? ToJsonUnicodeEscapes(to) : to;
        string nextFrom = normalizeOffset ? "2026-05-14T10:00:00Z" : from;
        string nextTo = normalizeOffset ? "2026-05-14T13:00:00+02:00" : to;
        string secondFrom = escapedFirst ? nextFrom : ToJsonUnicodeEscapes(nextFrom);
        string secondTo = escapedFirst ? nextTo : ToJsonUnicodeEscapes(nextTo);

        QueryResult first = await ExecuteAsync(store, codec,
            $"{{\"from\":\"{firstFrom}\",\"to\":\"{firstTo}\",\"pageSize\":2}}");

        ReadEventIds(first).ShouldBe(["from", "access"]);
        using JsonDocument firstPage = JsonDocument.Parse(first.PayloadBytes.ShouldNotBeNull());
        firstPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
        string cursor = firstPage.RootElement.GetProperty("cursor").GetString().ShouldNotBeNull();
        cursor.ShouldNotBeNullOrWhiteSpace();

        QueryResult second = await ExecuteAsync(store, codec,
            $"{{\"from\":\"{secondFrom}\",\"to\":\"{secondTo}\",\"cursor\":{JsonSerializer.Serialize(cursor)},\"pageSize\":2}}");

        ReadEventIds(second).ShouldBe(["to"]);
        using JsonDocument secondPage = JsonDocument.Parse(second.PayloadBytes.ShouldNotBeNull());
        secondPage.RootElement.GetProperty("hasMore").GetBoolean().ShouldBeFalse();
        secondPage.RootElement.GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    /// <summary>Validates only the final duplicate date member, including malformed earlier strings.</summary>
    [Theory]
    [InlineData("from", false, false)]
    [InlineData("from", false, true)]
    [InlineData("from", true, false)]
    [InlineData("from", true, true)]
    [InlineData("to", false, false)]
    [InlineData("to", false, true)]
    [InlineData("to", true, false)]
    [InlineData("to", true, true)]
    public async Task DuplicateUnicodeDateRetainsLastValueSemanticsAsync(string propertyName, bool malformedLast, bool useNull)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = malformedLast ? Substitute.For<IQueryCursorCodec>() : CreateCursorCodec();
        string malformed = "\"2026-05-14T10:00:00Z\\ud800\"";
        string valid = useNull ? "null" : "\"2026-05-14T10:00:00Z\"";
        string first = malformedLast ? valid : malformed;
        string last = malformedLast ? malformed : valid;

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"{propertyName}\":{first},\"{propertyName}\":{last}}}");

        if (malformedLast)
        {
            result.Success.ShouldBeFalse();
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
            result.PayloadBytes.ShouldBeNull();
            codec.ReceivedCalls().ShouldBeEmpty();
            await AssertOnlyAuthorityWasReadAsync(store);
        }
        else
        {
            string[] expected = useNull
                ? ["before", "from", "access", "to", "after"]
                : propertyName == "from" ? ["from", "access", "to", "after"] : ["before", "from"];
            ReadEventIds(result).ShouldBe(expected);
        }
    }

    /// <summary>Gets invalid timestamp strings and JSON kinds for both inclusive bounds.</summary>
    public static TheoryData<string, string> InvalidDateFilters
    {
        get
        {
            TheoryData<string, string> cases = [];
            foreach (string propertyName in new[] { "from", "to" })
            {
                foreach (string value in new[]
                {
                    "\"not-a-date\"", "\"\"", "\"   \"", "\"2026-02-30T10:00:00Z\"",
                    "\"2026-05-14T25:00:00Z\"", "\"\\ud800\"", "\"\\udc00\"",
                    "42", "true", "false", "{}", "[]",
                })
                {
                    cases.Add(propertyName, value);
                }
            }

            return cases;
        }
    }

    /// <summary>Rejects malformed bounds before decoding cursors or reading audit rows.</summary>
    [Theory]
    [MemberData(nameof(InvalidDateFilters))]
    public async Task MalformedDateFilterReturnsInvalidEnvelopeBeforeAuditReadAsync(string field, string value)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"{field}\":{value}}}");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Rejects supplied categories of every non-string, non-null JSON kind.</summary>
    [Theory]
    [InlineData("42")]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("{}")]
    [InlineData("[]")]
    public async Task NonStringCategoryReturnsInvalidEnvelopeBeforeAuditReadAsync(string value)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, $"{{\"category\":{value}}}");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Rejects malformed filters before considering a supplied invalid cursor.</summary>
    [Theory]
    [InlineData("{\"from\":\"not-a-date\",\"cursor\":\"invalid-cursor\"}")]
    [InlineData("{\"to\":42,\"cursor\":\"invalid-cursor\"}")]
    [InlineData("{\"category\":42,\"cursor\":\"invalid-cursor\"}")]
    public async Task MalformedFilterTakesPrecedenceOverSuppliedCursorAsync(string payload)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    /// <summary>Preserves optional null filters and the existing blank-category convention.</summary>
    [Theory]
    [InlineData("{}")]
    [InlineData("{\"from\":null}")]
    [InlineData("{\"to\":null}")]
    [InlineData("{\"category\":null}")]
    [InlineData("{\"from\":null,\"to\":null,\"category\":null}")]
    [InlineData("{\"category\":\"\"}")]
    [InlineData("{\"category\":\"   \"}")]
    public async Task OptionalFiltersReturnTenantScopedRowsAsync(string payload)
    {
        QueryResult result = await ExecuteAsync(CreateStore(), CreateCursorCodec(), payload);

        ReadEventIds(result).ShouldBe(["before", "from", "access", "to", "after"]);
    }

    /// <summary>Preserves active filters when other filters are absent, null or blank.</summary>
    [Theory]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\"}", "from,access,to,after")]
    [InlineData("{\"to\":\"2026-05-14T11:00:00Z\"}", "before,from,access,to")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"to\":null,\"category\":\"\"}", "from,access,to,after")]
    [InlineData("{\"from\":null,\"to\":\"2026-05-14T11:00:00Z\",\"category\":\"   \"}", "before,from,access,to")]
    [InlineData("{\"from\":null,\"to\":null,\"category\":\"access\"}", "access")]
    [InlineData("{\"from\":\"2026-05-14T10:00:00Z\",\"to\":null,\"category\":\"access\"}", "access")]
    public async Task OptionalFiltersDoNotRemoveOtherActiveFiltersAsync(string payload, string expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(expectedEventIds);
        QueryResult result = await ExecuteAsync(CreateStore(), CreateCursorCodec(), payload);

        ReadEventIds(result).ShouldBe(expectedEventIds.Split(','));
    }

    /// <summary>Retains numeric string category parsing while rejecting numeric JSON values separately.</summary>
    [Theory]
    [InlineData("0", "access")]
    [InlineData("1", "before,from,to,after")]
    [InlineData("99", "")]
    public async Task NumericStringCategoriesRetainExistingFilteringAsync(string category, string expectedEventIds)
    {
        ArgumentNullException.ThrowIfNull(expectedEventIds);
        QueryResult result = await ExecuteAsync(CreateStore(), CreateCursorCodec(), JsonSerializer.Serialize(new { category }));

        ReadEventIds(result).ShouldBe(expectedEventIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>Validates the final value of repeated filter members under existing JSON parsing semantics.</summary>
    [Theory]
    [InlineData("{\"from\":\"bad\",\"from\":null}", true)]
    [InlineData("{\"category\":42,\"category\":null}", true)]
    [InlineData("{\"from\":null,\"from\":\"bad\"}", false)]
    [InlineData("{\"category\":null,\"category\":42}", false)]
    public async Task DuplicateFilterMembersRetainLastValueSemanticsAsync(string payload, bool expectedSuccess)
    {
        IReadModelStore store = CreateStore();

        QueryResult result = await ExecuteAsync(store, CreateCursorCodec(), payload);

        result.Success.ShouldBe(expectedSuccess);
        if (expectedSuccess)
        {
            ReadEventIds(result).ShouldBe(["before", "from", "access", "to", "after"]);
        }
        else
        {
            result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
            result.PayloadBytes.ShouldBeNull();
            await AssertOnlyAuthorityWasReadAsync(store);
        }
    }

    /// <summary>Retains date offsets, inclusive boundaries and case-insensitive category filtering.</summary>
    [Theory]
    [InlineData("administrative")]
    [InlineData("Administrative")]
    public async Task ValidFiltersReturnOnlyRowsWithinInclusiveBoundsAsync(string category)
    {
        string payload = JsonSerializer.Serialize(new
        {
            from = "2026-05-14T12:00:00+02:00",
            to = "2026-05-14T11:00:00Z",
            category,
        });

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCursorCodec(), payload);

        ReadEventIds(result).ShouldBe(["from", "to"]);
    }

    /// <summary>Preserves authorization precedence over malformed filter validation.</summary>
    [Theory]
    [InlineData("{\"from\":\"not-a-date\"}")]
    [InlineData("{\"from\":42}")]
    [InlineData("{\"to\":\"not-a-date\"}")]
    [InlineData("{\"to\":true}")]
    [InlineData("{\"category\":42}")]
    public async Task UnauthorizedCallerReceivesForbiddenBeforeFilterValidationAsync(string payload)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, payload, "non-admin");

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        await AssertOnlyAuthorityWasReadAsync(store);
    }

    private static Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec codec,
        string payload,
        string userId = "admin-1")
        => ExecuteAsync(store, codec, Encoding.UTF8.GetBytes(payload), userId);

    private static Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec codec,
        byte[] payload,
        string userId = "admin-1")
        => TenantQueryTestHarness.ExecuteAsync(store, codec, new QueryEnvelope(
            "system",
            GetTenantAuditQuery.Domain,
            "tenant-1",
            GetTenantAuditQuery.QueryType,
            payload,
            "audit-filter-validation",
            userId,
            "tenant-1"));

    private static async Task AssertOnlyAuthorityWasReadAsync(IReadModelStore store)
    {
        _ = await store.Received(1).GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>());
        store.ReceivedCalls().Count().ShouldBe(1);
    }

    private static string ToJsonUnicodeEscapes(string value)
    {
        var builder = new StringBuilder(value.Length * 6);
        foreach (char character in value)
        {
            _ = builder.Append("\\u");
            _ = builder.Append(((int)character).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }

    private static string[] ReadEventIds(QueryResult result)
    {
        result.Success.ShouldBeTrue();
        result.ErrorMessage.ShouldBeNull();
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        return document.RootElement.GetProperty("items").EnumerateArray()
            .Select(entry => entry.GetProperty("eventId").GetString()!)
            .ToArray();
    }

    private static IReadModelStore CreateStore()
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        _ = store.GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(
                new GlobalAdministratorReadModel { Administrators = ["admin-1"] }, "authority-etag")));
        DateTimeOffset from = new(2026, 5, 14, 10, 0, 0, TimeSpan.Zero);
        var model = new TenantAuditReadModel
        {
            Entries =
            [
                CreateEntry("before", from.AddTicks(-1)),
                CreateEntry("from", from),
                CreateEntry("access", from.AddMinutes(15), AuditEventCategory.Access),
                CreateEntry("foreign", from.AddMinutes(30)) with { TenantId = "other-tenant" },
                CreateEntry("to", from.AddHours(1)),
                CreateEntry("after", from.AddHours(1).AddTicks(1)),
            ],
        };
        _ = store.GetAsync<TenantAuditReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.TenantAuditProjectionKeyPrefix + "tenant-1",
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<TenantAuditReadModel>(model, "audit-etag")));
        return store;
    }

    private static TenantAuditEntry CreateEntry(
        string eventId,
        DateTimeOffset timestamp,
        AuditEventCategory category = AuditEventCategory.Administrative)
        => new(eventId, "TenantUpdated", category, "actor-1", timestamp, "tenant-1", new Dictionary<string, string>());

    private static IQueryCursorCodec CreateCursorCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");
}
