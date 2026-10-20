using System.Text;
using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Identity;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Queries.Handlers;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Server.Tests.Support;

using Microsoft.AspNetCore.DataProtection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Tenants.Server.Tests.Queries;

/// <summary>
/// Verifies that <c>get-user-tenants</c> lists the user named by the payload's CLR <c>UserId</c> for generic
/// gateway callers, keeps the existing visibility rules, rejects a contradicting entity identifier, and leaves the
/// REST entity-routed lookup unchanged.
/// </summary>
public sealed class GetUserTenantsPayloadTargetTests
{
    private const string ListAggregateId = "index";

    /// <summary>Gets the requesters and the memberships of alice each one may see.</summary>
    public static TheoryData<string, string?, string> VisibleMemberships
    {
        get
        {
            TheoryData<string, string?, string> data = [];
            foreach (string? entityId in new[] { ListAggregateId, null })
            {
                data.Add("alice", entityId, "t1,t2");
                data.Add("admin-1", entityId, "t1,t2");
                data.Add("bob", entityId, "t1");
                data.Add("carol", entityId, string.Empty);
                data.Add("dave", entityId, string.Empty);
            }

            return data;
        }
    }

    /// <summary>
    /// The payload user is listed when no entity identifier was chosen (absent, or the gateway's default routing to
    /// the aggregate identifier), with the unchanged self, global-administrator and owner visibility rules.
    /// </summary>
    [Theory]
    [MemberData(nameof(VisibleMemberships))]
    public async Task PayloadUserIsListedWithUnchangedVisibilityAsync(string requester, string? entityId, string expectedTenantIds)
    {
        ArgumentNullException.ThrowIfNull(expectedTenantIds);

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCodec(), "{\"UserId\":\"alice\"}", requester, entityId);

        ReadTenantIds(result).ShouldBe(expectedTenantIds.Split(',', StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>An explicit entity identifier that names another user contradicts the payload and is refused before any read.</summary>
    [Theory]
    [InlineData("bob")]
    [InlineData("admin-1")]
    [InlineData("Alice")]
    public async Task ContradictingEntityIdIsInvalidAsync(string entityId)
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = Substitute.For<IQueryCursorCodec>();

        QueryResult result = await ExecuteAsync(store, codec, "{\"UserId\":\"alice\"}", "admin-1", entityId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        store.ReceivedCalls().ShouldBeEmpty();
        codec.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>An entity identifier naming the same user as the payload is accepted.</summary>
    [Fact]
    public async Task MatchingEntityIdIsAcceptedAsync()
    {
        QueryResult result = await ExecuteAsync(CreateStore(), CreateCodec(), "{\"UserId\":\"alice\"}", "bob", "alice");

        ReadTenantIds(result).ShouldBe(["t1"]);
    }

    /// <summary>A payload user that is not a decodable string is refused before any read.</summary>
    [Theory]
    [InlineData("{\"UserId\":42}")]
    [InlineData("{\"UserId\":true}")]
    [InlineData("{\"UserId\":[\"alice\"]}")]
    [InlineData("{\"UserId\":\"\\ud800\"}")]
    public async Task MalformedPayloadUserIsInvalidAsync(string payload)
    {
        IReadModelStore store = CreateStore();

        QueryResult result = await ExecuteAsync(store, CreateCodec(), payload, "admin-1", ListAggregateId);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidEnvelope);
        result.PayloadBytes.ShouldBeNull();
        store.ReceivedCalls().ShouldBeEmpty();
    }

    /// <summary>Without a payload user the REST entity-routed lookup and the self lookup are unchanged.</summary>
    [Theory]
    [InlineData("{}", "bob", "alice", "t1")]
    [InlineData("{\"UserId\":null}", "admin-1", "alice", "t1,t2")]
    [InlineData("{\"UserId\":\"\"}", "admin-1", "alice", "t1,t2")]
    [InlineData("{\"UserId\":\"  \"}", "admin-1", "alice", "t1,t2")]
    [InlineData("{\"cursor\":null,\"pageSize\":20}", "bob", "alice", "t1")]
    [InlineData("{\"userId\":\"carol\"}", "admin-1", "alice", "t1,t2")]
    [InlineData("{}", "alice", null, "t1,t2")]
    [InlineData("", "carol", null, "t3")]
    public async Task LegacyLookupWithoutPayloadUserIsUnchangedAsync(
        string payload,
        string requester,
        string? entityId,
        string expectedTenantIds)
    {
        ArgumentNullException.ThrowIfNull(expectedTenantIds);

        QueryResult result = await ExecuteAsync(CreateStore(), CreateCodec(), payload, requester, entityId);

        ReadTenantIds(result).ShouldBe(expectedTenantIds.Split(','));
    }

    /// <summary>Envelope paging continues the resolved payload user's sequence, and its cursor cannot list another user.</summary>
    [Fact]
    public async Task EnvelopePagingIsBoundToThePayloadUserAsync()
    {
        IReadModelStore store = CreateStore();
        IQueryCursorCodec codec = CreateCodec();

        QueryResult first = await ExecuteAsync(store, codec, "{\"UserId\":\"alice\"}", "admin-1", ListAggregateId, new QueryPagingOptions(PageSize: 1));
        ReadTenantIds(first).ShouldBe(["t1"]);
        string cursor = first.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull().NextCursor.ShouldNotBeNull();

        QueryResult next = await ExecuteAsync(store, codec, "{\"UserId\":\"alice\"}", "admin-1", ListAggregateId, new QueryPagingOptions(Cursor: cursor));
        ReadTenantIds(next).ShouldBe(["t2"]);
        next.Metadata.ShouldNotBeNull().Paging.ShouldNotBeNull().HasMore.ShouldBe(false);

        QueryResult otherUser = await ExecuteAsync(store, codec, "{\"UserId\":\"carol\"}", "admin-1", ListAggregateId, new QueryPagingOptions(Cursor: cursor));
        otherUser.Success.ShouldBeFalse();
        otherUser.ErrorMessage.ShouldBe(QueryAdapterFailureReason.InvalidCursor);
        otherUser.PayloadBytes.ShouldBeNull();
    }

    private static Task<QueryResult> ExecuteAsync(
        IReadModelStore store,
        IQueryCursorCodec codec,
        string payload,
        string requester,
        string? entityId,
        QueryPagingOptions? paging = null)
        => TenantQueryTestHarness.ExecuteAsync(store, codec, new QueryEnvelope(
            TenantIdentity.DefaultTenantId,
            GetUserTenantsQuery.Domain,
            ListAggregateId,
            GetUserTenantsQuery.QueryType,
            Encoding.UTF8.GetBytes(payload),
            "user-tenants-target",
            requester,
            entityId) with { Paging = paging });

    private static string[] ReadTenantIds(QueryResult result)
    {
        result.Success.ShouldBeTrue(result.ErrorMessage);
        using JsonDocument document = JsonDocument.Parse(result.PayloadBytes.ShouldNotBeNull());
        return document.RootElement.GetProperty("items").EnumerateArray()
            .Select(item => item.GetProperty("tenantId").GetString()!)
            .ToArray();
    }

    private static IQueryCursorCodec CreateCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");

    private static IReadModelStore CreateStore()
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        var index = new TenantIndexReadModel();
        foreach (string tenantId in new[] { "t1", "t2", "t3" })
        {
            index.Tenants[tenantId] = new TenantIndexEntry("Tenant " + tenantId, TenantStatus.Active);
        }

        index.UserTenants["alice"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal)
        {
            ["t1"] = TenantRole.TenantReader,
            ["t2"] = TenantRole.TenantContributor,
        };
        index.UserTenants["bob"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal) { ["t1"] = TenantRole.TenantOwner };
        index.UserTenants["carol"] = new Dictionary<string, TenantRole>(StringComparer.Ordinal) { ["t3"] = TenantRole.TenantOwner };
        store.GetAsync<TenantIndexReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.TenantIndexProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<TenantIndexReadModel>(index, "index-etag"));
        store.GetAsync<GlobalAdministratorReadModel>(TenantQueryHandlerBase.StateStoreName, TenantQueryHandlerBase.GlobalAdminProjectionKey, Arg.Any<CancellationToken>())
            .Returns(new ReadModelEntry<GlobalAdministratorReadModel>(new GlobalAdministratorReadModel { Administrators = ["admin-1"] }, "admin-etag"));
        return store;
    }
}
