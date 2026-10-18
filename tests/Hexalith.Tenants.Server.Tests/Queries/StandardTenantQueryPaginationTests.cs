using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>
/// Exercises standard pagination validation through every production handler that consumes it.
/// </summary>
public sealed class StandardTenantQueryPaginationTests
{
    private const string Caller = "item-000";
    private static readonly string[] _queryTypes =
    [
        ListTenantsQuery.QueryType,
        GetTenantUsersQuery.QueryType,
        GetUserTenantsQuery.QueryType,
        GetGlobalAdministratorsQuery.QueryType,
    ];

    /// <summary>Gets malformed escaped cursors for each standard query.</summary>
    public static TheoryData<string, string> MalformedCursors
    {
        get
        {
            TheoryData<string, string> data = [];
            foreach (string queryType in _queryTypes)
            {
                foreach (string cursor in new[]
                {
                    @"\ud800", @"\udbff", @"\udc00", @"\udfff",
                    @"prefix\ud800suffix", @"prefix\udc00suffix",
                    @"\udc00\ud800", @"\ud800text\udc00",
                })
                {
                    data.Add(queryType, cursor);
                }
            }

            return data;
        }
    }

    /// <summary>Gets malformed UTF-8 byte sequences for each standard query.</summary>
    public static TheoryData<string, string> MalformedUtf8Cursors
    {
        get
        {
            TheoryData<string, string> data = [];
            foreach (string queryType in _queryTypes)
            {
                foreach (string bytes in new[] { "80", "C0AF", "EDA080", "F4908080" })
                {
                    data.Add(queryType, bytes);
                }
            }

            return data;
        }
    }

    /// <summary>Verifies undecodable escaped cursors fail before codec invocation.</summary>
    [Theory]
    [MemberData(nameof(MalformedCursors))]
    public async Task MalformedUnicodeCursorReturnsInvalidEnvelopeAsync(string queryType, string cursor)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        byte[] payload = Encoding.UTF8.GetBytes($$"""{"cursor":"{{cursor}}","pageSize":1}""");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, CreateEnvelope(queryType, payload));

        AssertInvalidEnvelope(result);
        codec.ReceivedCalls().ShouldBeEmpty();
        AssertReadsBeforeParsing(store, queryType);
    }

    /// <summary>Verifies raw ill-formed UTF-8 is contained at cursor decoding.</summary>
    [Theory]
    [MemberData(nameof(MalformedUtf8Cursors))]
    public async Task MalformedUtf8CursorReturnsInvalidEnvelopeAsync(string queryType, string hex)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        byte[] payload = [.. Encoding.UTF8.GetBytes("{\"cursor\":\"prefix"), .. Convert.FromHexString(hex), .. Encoding.UTF8.GetBytes("suffix\",\"pageSize\":1}")];

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, CreateEnvelope(queryType, payload));

        AssertInvalidEnvelope(result);
        codec.ReceivedCalls().ShouldBeEmpty();
        AssertReadsBeforeParsing(store, queryType);
    }

    /// <summary>Verifies identity rejection and cancellation still precede malformed-payload parsing.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task MissingIdentityAndPreCancellationRetainPrecedenceAsync(string queryType)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        QueryEnvelope envelope = CreateEnvelope(queryType, Encoding.UTF8.GetBytes("{\"cursor\":\"\\ud800\"}")) with { UserId = " " };

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, envelope);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        await Should.ThrowAsync<OperationCanceledException>(() => TenantQueryTestHarness.ExecuteAsync(
            store, codec, envelope, new CancellationToken(canceled: true)));
        codec.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies invalid global scope is rejected before cursor parsing.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvalidGlobalRouteRetainsPrecedenceAsync(bool wrongTenant)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        QueryEnvelope envelope = CreateEnvelope(GetGlobalAdministratorsQuery.QueryType, Encoding.UTF8.GetBytes("{\"cursor\":\"\\udfff\"}"));
        envelope = wrongTenant ? envelope with { TenantId = "other" } : envelope with { AggregateId = "other" };

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, envelope);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
        codec.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Verifies cross-user visibility evaluation remains before pagination parsing.</summary>
    [Theory]
    [InlineData(Caller, false)]
    [InlineData("outsider", false)]
    [InlineData(Caller, true)]
    [InlineData("outsider", true)]
    public async Task CrossUserLookupPreservesExistingReadBoundaryAsync(string caller, bool targetPresent)
    {
        IReadModelStore store = CreateStore(targetPresent);
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        QueryEnvelope envelope = CreateEnvelope(GetUserTenantsQuery.QueryType, Encoding.UTF8.GetBytes("{\"cursor\":\"\\udc00\"}"))
            with { UserId = caller, EntityId = "other-user" };

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, envelope);

        AssertInvalidEnvelope(result);
        codec.ReceivedCalls().ShouldBeEmpty();
        store.ReceivedCalls().Select(call => call.GetArguments()[1]).ShouldBe(new object[]
        {
            TenantQueryHandlerBase.TenantIndexProjectionKey,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
        });
    }

    /// <summary>Verifies a malformed final duplicate supersedes a valid protected cursor without codec use.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task MalformedFinalDuplicateRejectsEarlierProtectedCursorAsync(string queryType)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        string token = JsonSerializer.Serialize(CreateCodec().Encode(queryType, GetScope(queryType), "item-000"));
        byte[] payload = Encoding.UTF8.GetBytes($$"""{"cursor":{{token}},"cursor":"\ud800","pageSize":1}""");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(store, codec, CreateEnvelope(queryType, payload));

        AssertInvalidEnvelope(result);
        codec.ReceivedCalls().ShouldBeEmpty();
        AssertReadsBeforeParsing(store, queryType);
    }

    /// <summary>Verifies decoded Unicode still reaches ordinary protected-cursor validation.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task DecodedUnicodeCursorRetainsInvalidCursorResultAsync(string queryType)
    {
        byte[] payload = Encoding.UTF8.GetBytes("{\"cursor\":\"\\ud83d\\ude00\"}");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(CreateStore(), CreateCodec(), CreateEnvelope(queryType, payload));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("invalid-cursor");
        result.PayloadBytes.ShouldBeNull();
    }

    /// <summary>Verifies malformed JSON defaults and unrelated undecodable strings stay compatible.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task MalformedJsonAndUnrelatedUnicodeRemainCompatibleAsync(string queryType)
    {
        foreach ((string json, int expectedCount) in new[]
        {
            ("{\"cursor\":\"\\ud800\",", 20),
            ("{\"ignored\":\"\\ud800\",\"pageSize\":1}", 1),
        })
        {
            byte[] payload = Encoding.UTF8.GetBytes(json);
            JsonElement page = ReadPage(await TenantQueryTestHarness.ExecuteAsync(CreateStore(), CreateCodec(), CreateEnvelope(queryType, payload)));

            page.GetProperty("items").GetArrayLength().ShouldBe(expectedCount);
            GetFirstId(page, queryType).ShouldBe("item-000");
        }
    }

    /// <summary>Verifies downstream faults are never converted into payload validation failures.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task CodecInvalidOperationExceptionStillPropagatesAsync(string queryType)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();
        var failure = new InvalidOperationException("test codec failure");
        codec.TryDecode(Arg.Any<string?>(), Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string?>(), out Arg.Any<string?>())
            .Returns(_ => throw failure);
        byte[] payload = Encoding.UTF8.GetBytes("{\"cursor\":\"decoded-cursor\"}");

        InvalidOperationException actual = await Should.ThrowAsync<InvalidOperationException>(() =>
            TenantQueryTestHarness.ExecuteAsync(store, codec, CreateEnvelope(queryType, payload)));

        actual.ShouldBeSameAs(failure);
        codec.ReceivedCalls().Count().ShouldBe(1);
        AssertReadsBeforeParsing(store, queryType);
    }

    /// <summary>Verifies final duplicate cursors determine validation and continuation.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task FinalDuplicateCursorPreservesSelectionAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        string token = codec.Encode(queryType, GetScope(queryType), "item-000");
        string validCursor = JsonSerializer.Serialize(token);
        string[] finalCursors = ["\"\\ud800\"", validCursor, "null"];
        foreach (string finalCursor in finalCursors)
        {
            byte[] payload = Encoding.UTF8.GetBytes($$"""{"cursor":"\udc00","\u0063ursor":{{finalCursor}},"pageSize":1}""");
            QueryResult result = await TenantQueryTestHarness.ExecuteAsync(CreateStore(), codec, CreateEnvelope(queryType, payload));

            if (finalCursor == finalCursors[0])
            {
                AssertInvalidEnvelope(result);
            }
            else
            {
                JsonElement page = ReadPage(result);
                page.GetProperty("items").GetArrayLength().ShouldBe(1);
                GetFirstId(page, queryType).ShouldBe(finalCursor == "null" ? "item-000" : "item-001");
            }
        }
    }

    /// <summary>Verifies bounded first pages and escaped protected continuation keep their behavior.</summary>
    [Theory]
    [InlineData("list-tenants")]
    [InlineData("get-tenant-users")]
    [InlineData("get-user-tenants")]
    [InlineData("get-global-administrators")]
    public async Task PageBoundsAndProtectedContinuationRemainCompatibleAsync(string queryType)
    {
        IQueryCursorCodec codec = CreateCodec();
        foreach ((int requested, int expected) in new[] { (-1, 20), (0, 20), (1, 1), (20, 20), (100, 100), (101, 100), (int.MaxValue, 100) })
        {
            IReadModelStore store = CreateStore();
            QueryEnvelope envelope = CreateEnvelope(queryType, JsonSerializer.SerializeToUtf8Bytes(new { pageSize = requested }));
            JsonElement first = ReadPage(await TenantQueryTestHarness.ExecuteAsync(store, codec, envelope));
            first.GetProperty("items").GetArrayLength().ShouldBe(expected);
            GetFirstId(first, queryType).ShouldBe("item-000");
            first.GetProperty("hasMore").GetBoolean().ShouldBeTrue();
            string token = first.GetProperty("cursor").GetString().ShouldNotBeNull();
            codec.TryDecode(token, queryType, GetScope(queryType), out string? position, out _).ShouldBeTrue();
            position.ShouldBe($"item-{expected - 1:D3}");

            string escapedToken = $"\\u{(int)token[0]:x4}{token[1..]}";
            byte[] continuationPayload = Encoding.UTF8.GetBytes($$"""{"cursor":"{{escapedToken}}","pageSize":{{requested}}}""");
            JsonElement next = ReadPage(await TenantQueryTestHarness.ExecuteAsync(store, codec, envelope with { Payload = continuationPayload }));
            next.GetProperty("items").GetArrayLength().ShouldBe(Math.Min(expected, 105 - expected));
            GetFirstId(next, queryType).ShouldBe($"item-{expected:D3}");

            string wrongScopeToken = codec.Encode(queryType, "other-scope", position!);
            QueryResult wrongScope = await TenantQueryTestHarness.ExecuteAsync(store, codec,
                envelope with { Payload = JsonSerializer.SerializeToUtf8Bytes(new { cursor = wrongScopeToken, pageSize = requested }) });
            wrongScope.Success.ShouldBeFalse();
            wrongScope.ErrorMessage.ShouldBe("invalid-cursor");
            wrongScope.PayloadBytes.ShouldBeNull();
        }
    }

    private static void AssertInvalidEnvelope(QueryResult result)
    {
        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
    }

    private static void AssertReadsBeforeParsing(IReadModelStore store, string queryType)
    {
        object?[] keys = store.ReceivedCalls().Select(call => call.GetArguments()[1]).ToArray();
        if (queryType == GetUserTenantsQuery.QueryType)
        {
            keys.ShouldBe(new object[] { TenantQueryHandlerBase.TenantIndexProjectionKey });
        }
        else
        {
            keys.ShouldBeEmpty();
        }
    }

    private static IQueryCursorCodec CreateCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static QueryEnvelope CreateEnvelope(string queryType, byte[] payload)
        => new(
            TenantIdentity.DefaultTenantId,
            queryType == GetGlobalAdministratorsQuery.QueryType ? GetGlobalAdministratorsQuery.Domain : ListTenantsQuery.Domain,
            queryType == GetGlobalAdministratorsQuery.QueryType ? TenantIdentity.GlobalAdministratorsAggregateId : "tenant.alpha",
            queryType,
            payload,
            "correlation-1",
            Caller,
            Caller);

    private static string GetScope(string queryType)
        => queryType switch
        {
            "list-tenants" => TenantQueryCursorScopes.ListTenants(Caller),
            "get-tenant-users" => TenantQueryCursorScopes.GetTenantUsers("tenant.alpha"),
            "get-user-tenants" => TenantQueryCursorScopes.GetUserTenants(Caller, Caller),
            "get-global-administrators" => TenantQueryCursorScopes.GetGlobalAdministrators(Caller),
            _ => throw new ArgumentOutOfRangeException(nameof(queryType)),
        };

    private static JsonElement ReadPage(QueryResult result)
    {
        result.Success.ShouldBeTrue(result.ErrorMessage);
        using var document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        return document.RootElement.Clone();
    }

    private static string? GetFirstId(JsonElement page, string queryType)
        => page.GetProperty("items")[0].GetProperty(queryType is "get-tenant-users" or "get-global-administrators" ? "userId" : "tenantId").GetString();

    private static IReadModelStore CreateStore(bool targetPresent = false)
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        var index = new TenantIndexReadModel();
        var tenant = new TenantReadModel { TenantId = "tenant.alpha", Name = "Tenant Alpha", Status = TenantStatus.Active };
        var admins = new GlobalAdministratorReadModel();
        Dictionary<string, TenantRole> memberships = new(StringComparer.Ordinal);
        index.UserTenants[Caller] = memberships;
        for (int i = 0; i < 105; i++)
        {
            string id = $"item-{i:D3}";
            index.Tenants[id] = new TenantIndexEntry(id, TenantStatus.Active);
            memberships[id] = TenantRole.TenantReader;
            tenant.Members[id] = TenantRole.TenantReader;
            admins.Administrators.Add(id);
        }

        if (targetPresent)
        {
            index.UserTenants["other-user"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal)
            {
                ["item-000"] = TenantRole.TenantReader,
            };
        }

        store.GetAsync<TenantIndexReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.TenantIndexProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantIndexReadModel>(index, "index-etag"));
        store.GetAsync<TenantReadModel>(TenantQueryHandlerBase.StateStoreName, "projection:tenants:tenant.alpha", Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantReadModel>(tenant, "tenant-etag"));
        store.GetAsync<GlobalAdministratorReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.GlobalAdminProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<GlobalAdministratorReadModel>(admins, "admin-etag"));
        store.ClearReceivedCalls();
        return store;
    }
}
