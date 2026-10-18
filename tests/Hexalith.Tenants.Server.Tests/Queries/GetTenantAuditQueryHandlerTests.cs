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
        => TenantQueryTestHarness.ExecuteAsync(store, codec, new QueryEnvelope(
            "system",
            GetTenantAuditQuery.Domain,
            "tenant-1",
            GetTenantAuditQuery.QueryType,
            Encoding.UTF8.GetBytes(payload),
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
