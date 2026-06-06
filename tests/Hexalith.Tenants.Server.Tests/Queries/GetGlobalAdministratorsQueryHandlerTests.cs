using System.Text.Json;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Queries;
using Hexalith.EventStore.Contracts.Queries;
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

public sealed class GetGlobalAdministratorsQueryHandlerTests {
    [Fact]
    public async Task Authorized_global_administrator_receives_paginated_literal_user_ids() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupGlobalAdministrators(store, "admin-2", "admin-1", "admin-3");
        IQueryCursorCodec cursorCodec = CreateCursorCodec();

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            cursorCodec,
            CreateEnvelope(userId: "admin-1", pageSize: 2));

        result.Success.ShouldBeTrue();
        result.ProjectionType.ShouldBe(GetGlobalAdministratorsQuery.ProjectionType);
        PaginatedResult<GlobalAdministratorSummary> page = DeserializePayload<PaginatedResult<GlobalAdministratorSummary>>(result);
        page.Items.Select(static i => i.UserId).ShouldBe(["admin-1", "admin-2"]);
        page.HasMore.ShouldBeTrue();
        page.Cursor.ShouldNotBeNullOrWhiteSpace();
        cursorCodec
            .TryDecode(
                page.Cursor,
                GetGlobalAdministratorsQuery.QueryType,
                TenantQueryCursorScopes.GetGlobalAdministrators("admin-1"),
                out string? decodedCursor,
                out string? failureReason)
            .ShouldBeTrue();
        decodedCursor.ShouldBe("admin-2");
        failureReason.ShouldBeNull();

        _ = await store.Received(1).GetAsync<GlobalAdministratorReadModel>(
            TenantQueryHandlerBase.StateStoreName,
            TenantQueryHandlerBase.GlobalAdminProjectionKey,
            Arg.Any<CancellationToken>());
        _ = await store.DidNotReceive().GetAsync<TenantReadModel>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        _ = await store.DidNotReceive().GetAsync<TenantIndexReadModel>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Non_administrator_receives_forbidden_without_hidden_admin_payload() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupGlobalAdministrators(store, "admin-1");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(userId: "tenant-owner"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task Missing_global_administrator_projection_fails_closed_as_forbidden() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupNoGlobalAdministrators(store);

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            CreateEnvelope(userId: "admin-1"));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        result.PayloadBytes.ShouldBeNull();
    }

    [Fact]
    public async Task Missing_authenticated_user_is_rejected_before_state_access() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        QueryEnvelope envelope = CreateEnvelope(userId: "admin-1") with { UserId = "" };

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            CreateCursorCodec(),
            envelope);

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe(QueryAdapterFailureReason.Forbidden);
        _ = await store.DidNotReceive().GetAsync<GlobalAdministratorReadModel>(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cursor_is_rejected_when_signed_for_different_query_or_requester_scope() {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        SetupGlobalAdministrators(store, "admin-1", "admin-2");
        IQueryCursorCodec cursorCodec = CreateCursorCodec();
        string cursor = cursorCodec.Encode(
            ListTenantsQuery.QueryType,
            TenantQueryCursorScopes.ListTenants("admin-1"),
            "admin-1");

        QueryResult result = await TenantQueryTestHarness.ExecuteAsync(
            store,
            cursorCodec,
            CreateEnvelope(userId: "admin-1", cursor: cursor));

        result.Success.ShouldBeFalse();
        result.ErrorMessage.ShouldBe("Invalid cursor.");
    }

    private static QueryEnvelope CreateEnvelope(
        string userId,
        string? cursor = null,
        int pageSize = TenantQueryPaginationPolicy.StandardDefaultPageSize)
        => new(
            TenantIdentity.DefaultTenantId,
            GetGlobalAdministratorsQuery.Domain,
            TenantIdentity.GlobalAdministratorsAggregateId,
            GetGlobalAdministratorsQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new { cursor, pageSize }),
            "correlation-1",
            userId,
            TenantIdentity.GlobalAdministratorsAggregateId);

    private static T DeserializePayload<T>(QueryResult result)
        where T : class {
        _ = result.PayloadBytes.ShouldNotBeNull();
        T? payload = JsonSerializer.Deserialize<T>(
            result.PayloadBytes,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return payload.ShouldNotBeNull();
    }

    private static void SetupGlobalAdministrators(IReadModelStore store, params string[] administratorIds) {
        var model = new GlobalAdministratorReadModel {
            Administrators = administratorIds.ToHashSet(StringComparer.Ordinal),
        };

        _ = store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(model, "etag-1")));
    }

    private static void SetupNoGlobalAdministrators(IReadModelStore store)
        => store.GetAsync<GlobalAdministratorReadModel>(
                TenantQueryHandlerBase.StateStoreName,
                TenantQueryHandlerBase.GlobalAdminProjectionKey,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ReadModelEntry<GlobalAdministratorReadModel>(null, null)));

    private static IQueryCursorCodec CreateCursorCodec()
        => new QueryCursorCodec(new EphemeralDataProtectionProvider(), "Hexalith.Tenants.QueryCursor.v1");
}
