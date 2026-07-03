using System.Globalization;
using System.Net;
using System.Text.Json;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.UserTenants;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using Shouldly;

// Memories.Contracts.V1 also defines TenantStatus/TenantSummary, so alias the search DTOs used here.
using MemoriesErrorResponse = Hexalith.Memories.Contracts.V1.ErrorResponse;
using MemoriesScoredResult = Hexalith.Memories.Contracts.V1.ScoredResult;
using MemoriesSearchResult = Hexalith.Memories.Contracts.V1.SearchResult;
using MemoriesSourceType = Hexalith.Memories.Contracts.V1.SourceType;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class TenantQueryGatewayTests
{
    [Fact]
    public async Task Get_tenant_submits_literal_detail_query_and_maps_counts_source()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: new QueryResponseMetadata(IsStale: false, ServedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetTenantQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantQuery.QueryType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Fact]
    public async Task Get_tenant_uses_previous_snapshot_for_not_modified_response()
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.ETag.ShouldBe("\"known\"");
    }

    [Fact]
    public async Task Get_tenant_applies_stale_freshness_from_not_modified_response()
    {
        TenantDetailSnapshot previous = TenantDetailSnapshot.Ready(
            Detail("tenant.alpha"),
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"", isStale: true);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Stale);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Fact]
    public async Task Get_tenant_without_previous_snapshot_reports_degraded_not_modified_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueDetailNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha", ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Degraded);
        snapshot.Detail.ShouldBeNull();
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task Get_tenant_with_etag_but_no_freshness_metadata_reports_unknown()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            eTag: "\"tenant-etag\"",
            metadata: new QueryResponseMetadata(ETag: "\"tenant-etag\""));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantDetailSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(401, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(403, TenantDetailSurfaceKind.Unauthorized)]
    [InlineData(404, TenantDetailSurfaceKind.NotFound)]
    [InlineData(503, TenantDetailSurfaceKind.Unavailable)]
    public async Task Get_tenant_maps_gateway_status_to_safe_detail_state(int statusCode, TenantDetailSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        string errorMessage = snapshot.ErrorMessage.ShouldNotBeNull();
        errorMessage.ShouldNotContain("raw payload", Case.Insensitive);
        errorMessage.ShouldNotContain("token", Case.Insensitive);
        errorMessage.ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, TenantDetailSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, TenantDetailSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_tenant_maps_stale_and_degraded_metadata_to_safe_states(
        bool isStale,
        bool isDegraded,
        TenantDetailSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("tenant.alpha"),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantDetailSnapshot snapshot = await gateway
            .GetTenantAsync(new TenantDetailRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Detail.ShouldNotBeNull().TenantId.ShouldBe("tenant.alpha");
    }

    [Fact]
    public async Task List_tenants_passes_cursor_without_offset_conversion()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            "next-cursor",
            true));
        client.EnqueueQueryResult(new TenantDetail(
            "tenant.alpha",
            "Alpha",
            null,
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(Cursor: "opaque-cursor", PageSize: 10), null, CancellationToken.None);

        SubmittedQuery listQuery = client.SubmittedQueries[0];
        listQuery.Request.Tenant.ShouldBe("system");
        listQuery.Request.QueryType.ShouldBe(ListTenantsQuery.QueryType);
        JsonElement payload = listQuery.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.NextCursor.ShouldBe("next-cursor");
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.ShouldBe(TenantCountValue.Known(2));
        snapshot.Rows[0].OwnerCount.ShouldBe(TenantCountValue.Known(1));
    }

    [Fact]
    public async Task List_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.ErrorMessage.ShouldBeNull();
    }

    [Fact]
    public async Task Get_global_administrators_submits_fixed_platform_scope_query()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<GlobalAdministratorSummary>(
            [new GlobalAdministratorSummary("admin-1")],
            "next-cursor",
            true));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(Cursor: "opaque-cursor", PageSize: 10, ETag: "\"known\""), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetGlobalAdministratorsQuery.Domain);
        query.Request.AggregateId.ShouldBe("global-administrators");
        query.Request.EntityId.ShouldBe("global-administrators");
        query.Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetGlobalAdministratorsQuery.ProjectionType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(10);
        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Ready);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        snapshot.NextCursor.ShouldBe("next-cursor");
        snapshot.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task Get_global_administrators_preserves_previous_rows_for_not_modified()
    {
        GlobalAdministratorsSnapshot previous = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueGlobalAdministratorsNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Ready);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
        snapshot.ETag.ShouldBe("\"known\"");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Fact]
    public async Task Get_global_administrators_applies_stale_freshness_from_not_modified_response()
    {
        GlobalAdministratorsSnapshot previous = GlobalAdministratorsSnapshot.Ready(
            [new GlobalAdministratorRow("admin-1", ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current);
        CapturingGatewayClient client = new();
        client.EnqueueGlobalAdministratorsNotModified("\"known\"", isStale: true);

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(GlobalAdministratorsSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(GlobalAdministratorsReason.ProjectionStale);
    }

    [Theory]
    [InlineData(true, false, GlobalAdministratorsSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, GlobalAdministratorsSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_global_administrators_maps_stale_and_degraded_metadata_without_losing_rows(
        bool isStale,
        bool isDegraded,
        GlobalAdministratorsSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<GlobalAdministratorSummary>([new GlobalAdministratorSummary("admin-1")], null, false),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Rows.ShouldHaveSingleItem().UserId.ShouldBe("admin-1");
    }

    [Theory]
    [InlineData(401, GlobalAdministratorsSurfaceKind.Unauthorized)]
    [InlineData(403, GlobalAdministratorsSurfaceKind.Unauthorized)]
    [InlineData(400, GlobalAdministratorsSurfaceKind.Invalid)]
    [InlineData(404, GlobalAdministratorsSurfaceKind.Unavailable)]
    [InlineData(501, GlobalAdministratorsSurfaceKind.Unavailable)]
    [InlineData(503, GlobalAdministratorsSurfaceKind.Unavailable)]
    public async Task Get_global_administrators_maps_gateway_status_to_safe_snapshot_state(
        int statusCode,
        GlobalAdministratorsSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 cursor etag"));

        TenantQueryGateway gateway = CreateGateway(client);

        GlobalAdministratorsSnapshot snapshot = await gateway
            .GetGlobalAdministratorsAsync(new GlobalAdministratorsRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Reason.ToString().ShouldNotContain("raw", Case.Insensitive);
        snapshot.Reason.ToString().ShouldNotContain("token", Case.Insensitive);
        client.SubmittedQueries.Count.ShouldBe(1);
        client.SubmittedQueries[0].Request.QueryType.ShouldBe(GetGlobalAdministratorsQuery.QueryType);
        string[] tenantSubstituteQueries = ["list-tenants", "get-tenant", "get-user-tenants", "get-tenant-users"];
        client.SubmittedQueries
            .Any(q => tenantSubstituteQueries.Contains(q.Request.QueryType, StringComparer.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Get_tenant_audit_submits_exact_audit_query_shape_and_preserves_opaque_cursor()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [AuditEntry("event-1", AuditEventCategory.Access)],
            "next-audit-cursor",
            true));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(
                new TenantAuditRequest(
                    "tenant.alpha",
                    From: DateTimeOffset.Parse("2026-06-01T00:00:00Z", CultureInfo.InvariantCulture),
                    To: DateTimeOffset.Parse("2026-06-02T00:00:00Z", CultureInfo.InvariantCulture),
                    Category: AuditEventCategory.Access,
                    Cursor: "opaque-audit-cursor",
                    PageSize: 25,
                    ETag: "\"known\""),
                null,
                CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries.ShouldHaveSingleItem();
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetTenantAuditQuery.Domain);
        query.Request.AggregateId.ShouldBe("tenant.alpha");
        query.Request.EntityId.ShouldBe("tenant.alpha");
        query.Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        query.Request.ProjectionType.ShouldBe(GetTenantAuditQuery.ProjectionType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("from").GetDateTimeOffset().ShouldBe(DateTimeOffset.Parse("2026-06-01T00:00:00Z", CultureInfo.InvariantCulture));
        payload.GetProperty("to").GetDateTimeOffset().ShouldBe(DateTimeOffset.Parse("2026-06-02T00:00:00Z", CultureInfo.InvariantCulture));
        payload.GetProperty("category").GetString().ShouldBe(nameof(AuditEventCategory.Access));
        payload.GetProperty("cursor").GetString().ShouldBe("opaque-audit-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(25);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        payload.TryGetProperty("limit", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Ready);
        snapshot.NextCursor.ShouldBe("next-audit-cursor");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().ReferenceContext.ShouldContain("userId: target-user");
    }

    [Fact]
    public async Task Get_tenant_audit_requeries_page_one_for_invalid_cursor_and_reports_list_refreshed()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            400,
            "Bad request",
            reasonCode: "invalid-cursor",
            detail: "cursor raw payload token correlation-123"));
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [AuditEntry("event-2", AuditEventCategory.Administrative)],
            "fresh-cursor",
            true));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(
                new TenantAuditRequest(
                    "tenant.alpha",
                    Category: AuditEventCategory.Administrative,
                    Cursor: "expired-protected-cursor",
                    PageSize: 25),
                null,
                CancellationToken.None);

        client.SubmittedQueries.Count.ShouldBe(2);
        client.SubmittedQueries[0].Request.Payload.ShouldNotBeNull().GetProperty("cursor").GetString().ShouldBe("expired-protected-cursor");
        client.SubmittedQueries[1].Request.Payload.ShouldNotBeNull().GetProperty("cursor").ValueKind.ShouldBe(JsonValueKind.Null);
        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.ListRefreshed);
        snapshot.Reason.ShouldBe(TenantAuditReason.ListRefreshed);
        snapshot.NextCursor.ShouldBe("fresh-cursor");
        snapshot.ToString().ShouldNotContain("expired-protected-cursor", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, TenantAuditSurfaceKind.Stale, ReadModelFreshnessState.Stale)]
    [InlineData(false, true, TenantAuditSurfaceKind.Degraded, ReadModelFreshnessState.Unknown)]
    public async Task Get_tenant_audit_maps_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        TenantAuditSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantAuditEntry>([AuditEntry("event-3", AuditEventCategory.Access)], null, false),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Fact]
    public async Task Get_tenant_audit_reuses_not_modified_snapshot_only_for_same_scope()
    {
        TenantAuditRequest originalRequest = new("tenant.alpha", Category: AuditEventCategory.Access, ETag: "\"known\"");
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-4", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            originalRequest);
        CapturingGatewayClient client = new();
        client.EnqueueAuditNotModified("\"known\"");
        client.EnqueueAuditNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot sameScope = await gateway
            .GetTenantAuditAsync(originalRequest, previous, CancellationToken.None);
        TenantAuditSnapshot differentScope = await gateway
            .GetTenantAuditAsync(originalRequest with { Category = AuditEventCategory.Administrative }, previous, CancellationToken.None);

        sameScope.Rows.ShouldHaveSingleItem().EventReference.ShouldBe("event-4");
        differentScope.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        differentScope.Reason.ShouldBe(TenantAuditReason.NotModifiedWithoutSnapshot);
        differentScope.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_tenant_audit_applies_stale_freshness_from_not_modified_response()
    {
        TenantAuditRequest request = new("tenant.alpha", Category: AuditEventCategory.Access, ETag: "\"known\"");
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-4", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            request);
        CapturingGatewayClient client = new();
        client.EnqueueAuditNotModified("\"known\"", isStale: true);
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(request, previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(TenantAuditReason.ProjectionStale);
    }

    [Fact]
    public async Task Get_tenant_audit_maps_missing_payload_to_safe_degraded_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult<PaginatedResult<TenantAuditEntry>?>(null, metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(TenantAuditReason.MissingPayload);
        snapshot.Rows.ShouldBeEmpty();
        client.SubmittedQueries.ShouldHaveSingleItem().Request.QueryType.ShouldBe(GetTenantAuditQuery.QueryType);
        string[] tenantSubstituteQueries = ["list-tenants", "get-tenant", "get-user-tenants", "get-tenant-users"];
        client.SubmittedQueries
            .Any(q => tenantSubstituteQueries.Contains(q.Request.QueryType, StringComparer.Ordinal))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Get_tenant_audit_preserves_previous_rows_for_missing_payload_when_scope_matches()
    {
        TenantAuditRequest request = new("tenant.alpha", Category: AuditEventCategory.Access);
        TenantAuditSnapshot previous = TenantAuditSnapshot.Ready(
            [TenantAuditRow.FromEntry(AuditEntry("event-5", AuditEventCategory.Access), ReadModelFreshnessState.Current)],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            request);
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult<PaginatedResult<TenantAuditEntry>?>(null, metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(request, previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantAuditSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(TenantAuditReason.MissingPayload);
        snapshot.Rows.ShouldHaveSingleItem().EventReference.ShouldBe("event-5");
    }

    [Theory]
    [InlineData(401, TenantAuditSurfaceKind.Unauthorized)]
    [InlineData(403, TenantAuditSurfaceKind.Unauthorized)]
    [InlineData(404, TenantAuditSurfaceKind.Unavailable)]
    [InlineData(503, TenantAuditSurfaceKind.Unavailable)]
    [InlineData(500, TenantAuditSurfaceKind.Error)]
    public async Task Get_tenant_audit_maps_gateway_status_to_safe_snapshot_state(int statusCode, TenantAuditSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 EventStore metadata cursor etag"));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.Rows.ShouldBeEmpty();
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("stack trace", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public async Task Get_tenant_audit_maps_only_support_safe_narrative_fields()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [
                new TenantAuditEntry(
                    "event-safe-reference",
                    "TenantConfigurationSet",
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.UtcNow,
                    "tenant.alpha",
                    new Dictionary<string, string>
                    {
                        ["userId"] = "target-user",
                        ["key"] = "billing.mode",
                        ["rawPayload"] = "raw payload token secret",
                        ["correlationId"] = "correlation-123",
                        ["etag"] = "\"etag\"",
                    }),
            ],
            null,
            false));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        TenantAuditRow row = snapshot.Rows.ShouldHaveSingleItem();
        row.ReferenceContext.ShouldContain("userId: target-user");
        row.ReferenceContext.ShouldContain("key: billing.mode");
        row.ReferenceContext.ShouldNotContain("raw payload", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("token", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("correlation-123", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("etag", Case.Insensitive);
    }

    [Fact]
    public async Task Get_tenant_audit_scrubs_unsafe_row_fields_before_rendering()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantAuditEntry>(
            [
                new TenantAuditEntry(
                    "event-safe-reference",
                    "stack trace internal detail",
                    AuditEventCategory.Administrative,
                    "actor-user",
                    DateTimeOffset.UtcNow,
                    "cursor protected value",
                    new Dictionary<string, string>
                    {
                        ["userId"] = "raw payload token secret",
                        ["key"] = "billing.mode",
                    }),
            ],
            null,
            false));
        TenantQueryGateway gateway = CreateGateway(client);

        TenantAuditSnapshot snapshot = await gateway
            .GetTenantAuditAsync(new TenantAuditRequest("tenant.alpha"), null, CancellationToken.None);

        TenantAuditRow row = snapshot.Rows.ShouldHaveSingleItem();
        row.Target.ShouldBeEmpty();
        row.Scope.ShouldBeEmpty();
        row.Outcome.ShouldBeEmpty();
        row.ReferenceContext.ShouldContain("key: billing.mode");
        row.ReferenceContext.ShouldNotContain("raw payload", Case.Insensitive);
        row.ReferenceContext.ShouldNotContain("token", Case.Insensitive);
        row.Target.ShouldNotContain("raw payload", Case.Insensitive);
        row.Scope.ShouldNotContain("cursor", Case.Insensitive);
        row.Outcome.ShouldNotContain("stack trace", Case.Insensitive);
    }

    [Fact]
    public async Task List_tenants_reports_unknown_freshness_when_no_evidence_exists()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            eTag: null,
            metadata: null,
            emitDefaultMetadata: false);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
    }

    [Fact]
    public async Task List_tenants_does_not_treat_served_at_as_freshness_evidence()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            eTag: null,
            metadata: new QueryResponseMetadata(ServedAt: DateTimeOffset.UtcNow));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Empty);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task List_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Current);
    }

    [Fact]
    public async Task List_tenants_not_modified_preserves_previous_unknown_freshness_without_freshness_header()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: null,
            hasMore: false,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Unknown,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"");

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task List_tenants_not_modified_uses_stale_header_from_conditional_response()
    {
        TenantListSnapshot previous = TenantListSnapshot.Ready(
            [TenantListRow.FromSummary(new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active))],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            isDegraded: false);
        CapturingGatewayClient client = new();
        client.EnqueueNotModified("\"known\"", isStale: true);

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10, ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
    }

    [Fact]
    public async Task List_tenants_stale_empty_response_surfaces_stale_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<TenantSummary>([], null, false),
            metadata: new QueryResponseMetadata(IsStale: true));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Fact]
    public async Task Detail_enrichment_failure_keeps_unknown_counts_and_degraded_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>(
            [new TenantSummary("tenant.alpha", "Alpha", TenantStatus.Active)],
            null,
            false));
        client.EnqueueException(new EventStoreGatewayException(403, "Forbidden"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.Rows.ShouldHaveSingleItem().MemberCount.IsKnown.ShouldBeFalse();
        snapshot.Rows[0].OwnerCount.IsKnown.ShouldBeFalse();
    }

    [Theory]
    [InlineData(401, TenantListSurfaceKind.Unauthorized)]
    [InlineData(403, TenantListSurfaceKind.Unauthorized)]
    [InlineData(400, TenantListSurfaceKind.Error)]
    [InlineData(503, TenantListSurfaceKind.Error)]
    public async Task List_tenants_maps_gateway_status_to_safe_state(int statusCode, TenantListSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));

        TenantQueryGateway gateway = CreateGateway(client);

        TenantListSnapshot snapshot = await gateway
            .ListTenantsAsync(new TenantListRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
    }

    [Fact]
    public async Task Get_my_tenants_submits_self_user_query_with_cursor_payload()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>(
            [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantOwner)],
            "opaque-next",
            true));
        TenantQueryGateway gateway = CreateGateway(client, "user.self");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(Cursor: "signed-cursor", PageSize: 12), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("user.self");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("signed-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(12);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.NextCursor.ShouldBe("opaque-next");
        snapshot.Rows.ShouldHaveSingleItem().Role.ShouldBe(TenantRole.TenantOwner);
    }

    [Fact]
    public async Task Get_my_tenants_keeps_signed_in_user_as_target_even_when_request_has_target()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client, "user.self");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "user.other"), null, CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.EntityId.ShouldBe("user.self");
        snapshot.TargetUserId.ShouldBe("user.self");
    }

    [Fact]
    public async Task Get_user_tenants_submits_authenticated_requester_and_explicit_target_user_query()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>(
            [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader)],
            "opaque-next",
            true));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(
                new UserTenantMembershipRequest(
                    TargetUserId: "target.user@example",
                    Cursor: "signed-target-cursor",
                    PageSize: 12,
                    ETag: "\"known\""),
                null,
                CancellationToken.None);

        SubmittedQuery query = client.SubmittedQueries[0];
        query.Request.Tenant.ShouldBe("system");
        query.Request.Domain.ShouldBe(GetUserTenantsQuery.Domain);
        query.Request.ProjectionType.ShouldBe(GetUserTenantsQuery.ProjectionType);
        query.Request.AggregateId.ShouldBe("index");
        query.Request.EntityId.ShouldBe("target.user@example");
        query.Request.QueryType.ShouldBe(GetUserTenantsQuery.QueryType);
        query.IfNoneMatch.ShouldBe("\"known\"");
        JsonElement payload = query.Request.Payload.ShouldNotBeNull();
        payload.GetProperty("cursor").GetString().ShouldBe("signed-target-cursor");
        payload.GetProperty("pageSize").GetInt32().ShouldBe(12);
        payload.TryGetProperty("offset", out _).ShouldBeFalse();
        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Ready);
        snapshot.TargetUserId.ShouldBe("target.user@example");
        snapshot.NextCursor.ShouldBe("opaque-next");
    }

    [Fact]
    public async Task Get_user_tenants_reuses_not_modified_snapshot_only_for_same_target_user()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "target.one");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot sameTarget = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.one", ETag: "\"known\""), previous, CancellationToken.None);
        UserTenantMembershipSnapshot differentTarget = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.two", ETag: "\"known\""), previous, CancellationToken.None);

        sameTarget.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        sameTarget.TargetUserId.ShouldBe("target.one");
        differentTarget.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Degraded);
        differentTarget.Reason.ShouldBe(UserTenantMembershipReason.NotModifiedWithoutSnapshot);
        differentTarget.TargetUserId.ShouldBe("target.two");
    }

    [Fact]
    public async Task Get_user_tenants_applies_stale_freshness_from_not_modified_response()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "target.one");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"", isStale: true);
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.one", ETag: "\"known\""), previous, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.ProjectionStale);
    }

    [Fact]
    public async Task Get_user_tenants_rejects_missing_target_without_backend_call()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: ""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Invalid);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.MissingTargetUser);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_user_tenants_maps_authorization_scoped_empty_without_disclosing_hidden_memberships()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user", PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.Rows.ShouldBeEmpty();
        snapshot.ToString().ShouldNotContain("hidden", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("missing user", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("orphan", Case.Insensitive);
    }

    [Theory]
    [InlineData(true, false, UserTenantMembershipSurfaceKind.Stale, ReadModelFreshnessState.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(false, true, UserTenantMembershipSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, UserTenantMembershipReason.ProjectionDegraded)]
    public async Task Get_user_tenants_maps_target_lookup_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        UserTenantMembershipSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness,
        UserTenantMembershipReason expectedReason)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user", PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(401, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(403, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(400, UserTenantMembershipSurfaceKind.Invalid)]
    [InlineData(503, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(500, UserTenantMembershipSurfaceKind.Degraded)]
    public async Task Get_user_tenants_maps_target_lookup_gateway_failures_to_sanitized_states(
        int statusCode,
        UserTenantMembershipSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123 EventStore metadata"));
        TenantQueryGateway gateway = CreateGateway(client, "operator-user");

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetUserTenantsAsync(new UserTenantMembershipRequest(TargetUserId: "target.user"), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.TargetUserId.ShouldBe("target.user");
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("stack trace", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("EventStore metadata", Case.Insensitive);
    }

    [Fact]
    public async Task Get_my_tenants_requires_authenticated_user_context()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, userId: null);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Unauthorized);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.MissingAuthenticatedUser);
        client.SubmittedQueries.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_my_tenants_maps_authorized_empty_without_error()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<UserTenantMembership>([], null, false));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Empty);
        snapshot.IsAuthorizationScopedEmpty.ShouldBeTrue();
        snapshot.Rows.ShouldBeEmpty();
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.None);
    }

    [Fact]
    public async Task Get_my_tenants_uses_previous_snapshot_for_not_modified_response()
    {
        UserTenantMembershipSnapshot previous = UserTenantMembershipSnapshot.Ready(
            [new UserTenantMembershipRow("tenant.alpha", "Alpha", TenantStatus.Active, TenantRole.TenantReader, ReadModelFreshnessState.Current)],
            nextCursor: "next",
            hasMore: true,
            eTag: "\"known\"",
            freshness: ReadModelFreshnessState.Current,
            targetUserId: "operator-user");
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), previous, CancellationToken.None);

        client.SubmittedQueries[0].IfNoneMatch.ShouldBe("\"known\"");
        snapshot.Rows.ShouldHaveSingleItem().TenantId.ShouldBe("tenant.alpha");
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.ETag.ShouldBe("\"known\"");
    }

    [Fact]
    public async Task Get_my_tenants_without_previous_snapshot_reports_degraded_not_modified_state()
    {
        CapturingGatewayClient client = new();
        client.EnqueueUserTenantsNotModified("\"known\"");
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(ETag: "\"known\""), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(UserTenantMembershipSurfaceKind.Degraded);
        snapshot.Reason.ShouldBe(UserTenantMembershipReason.NotModifiedWithoutSnapshot);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Theory]
    [InlineData(true, false, UserTenantMembershipSurfaceKind.Stale, ReadModelFreshnessState.Stale, UserTenantMembershipReason.ProjectionStale)]
    [InlineData(false, true, UserTenantMembershipSurfaceKind.Degraded, ReadModelFreshnessState.Unknown, UserTenantMembershipReason.ProjectionDegraded)]
    public async Task Get_my_tenants_maps_stale_and_degraded_metadata_to_distinct_states(
        bool isStale,
        bool isDegraded,
        UserTenantMembershipSurfaceKind expectedKind,
        ReadModelFreshnessState expectedFreshness,
        UserTenantMembershipReason expectedReason)
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            new PaginatedResult<UserTenantMembership>(
                [new UserTenantMembership("tenant.alpha", "Alpha", TenantStatus.Disabled, TenantRole.TenantReader)],
                "next",
                true),
            metadata: new QueryResponseMetadata(IsStale: isStale, IsDegraded: isDegraded));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(PageSize: 10), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expectedKind);
        snapshot.Freshness.ShouldBe(expectedFreshness);
        snapshot.Reason.ShouldBe(expectedReason);
        snapshot.NextCursor.ShouldBe("next");
        snapshot.HasMore.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(expectedFreshness);
    }

    [Theory]
    [InlineData(401, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(403, UserTenantMembershipSurfaceKind.Unauthorized)]
    [InlineData(400, UserTenantMembershipSurfaceKind.Invalid)]
    [InlineData(503, UserTenantMembershipSurfaceKind.Unavailable)]
    [InlineData(500, UserTenantMembershipSurfaceKind.Degraded)]
    public async Task Get_my_tenants_maps_gateway_failures_to_sanitized_states(int statusCode, UserTenantMembershipSurfaceKind expected)
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(
            statusCode,
            "Problem title",
            detail: "raw payload token secret stack trace correlation-123"));
        TenantQueryGateway gateway = CreateGateway(client);

        UserTenantMembershipSnapshot snapshot = await gateway
            .GetMyTenantsAsync(new UserTenantMembershipRequest(), null, CancellationToken.None);

        snapshot.Kind.ShouldBe(expected);
        snapshot.ToString().ShouldNotContain("raw payload", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("token", Case.Insensitive);
        snapshot.ToString().ShouldNotContain("correlation-123", Case.Insensitive);
    }

    [Fact]
    public async Task List_search_calls_memories_and_maps_source_uri_to_ordered_tenant_ids()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"));
        client.EnqueueQueryResult(Detail("beta"));
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha", "beta"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Rows.Select(r => r.TenantId).ShouldBe(["alpha", "beta"]); // BM25 score order preserved
        client.SubmittedQueries.Count.ShouldBe(2); // one ETag-fresh detail hydration per match-set id
        await memories.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(r => r != null && r.TenantId == "tenants-index" && r.Axis == "syntactic" && r.Query == "term"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_search_hydrates_counts_and_freshness_from_detail_path()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"), eTag: "\"alpha-etag\"");
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        TenantListRow row = snapshot.Rows.ShouldHaveSingleItem();
        row.MemberCount.ShouldBe(TenantCountValue.Known(2));
        row.OwnerCount.ShouldBe(TenantCountValue.Known(1));
        row.Freshness.ShouldBe(ReadModelFreshnessState.Current); // from detail freshness metadata, never from Memories
    }

    [Fact]
    public async Task List_search_marks_snapshot_stale_when_hydrated_detail_is_stale()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"), metadata: new QueryResponseMetadata(IsStale: true));
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Stale);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Stale);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(ReadModelFreshnessState.Stale);
    }

    [Fact]
    public async Task List_search_marks_snapshot_unknown_when_hydrated_detail_freshness_is_unknown()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(
            Detail("alpha"),
            eTag: "\"alpha-etag\"",
            metadata: new QueryResponseMetadata(ETag: "\"alpha-etag\""));
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        snapshot.Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
        snapshot.Rows.ShouldHaveSingleItem().Freshness.ShouldBe(ReadModelFreshnessState.Unknown);
    }

    [Fact]
    public async Task List_search_drops_malformed_source_uris()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"));
        MemoriesSearchResult result = new()
        {
            Results =
            [
                new MemoriesScoredResult { MemoryUnitId = "mu1", Score = 10, ContentSnippet = "x", SourceUri = "not-a-tenant-uri", SourceType = MemoriesSourceType.Event },
                new MemoriesScoredResult { MemoryUnitId = "mu2", Score = 9, ContentSnippet = "y", SourceUri = "tenant:alpha", SourceType = MemoriesSourceType.Event },
            ],
            TotalCount = 2,
            HasIndexedMemoryUnits = true,
            Query = "term",
        };
        TenantQueryGateway gateway = CreateGateway(client, memories: CreateSearchingMemoriesClient(result));

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Rows.Select(r => r.TenantId).ShouldBe(["alpha"]);
    }

    [Fact]
    public async Task List_search_status_filter_applies_to_hydrated_detail_status()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(DetailWithStatus("alpha", TenantStatus.Active));
        client.EnqueueQueryResult(DetailWithStatus("beta", TenantStatus.Disabled));
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha", "beta"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term", Status: TenantStatus.Active), previous: null, CancellationToken.None);

        snapshot.Rows.Select(r => r.TenantId).ShouldBe(["alpha"]); // beta (Disabled) filtered out
        await memories.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(request => HasActiveStatusFilter(request)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_search_uses_memories_offset_cursor_for_paging()
    {
        CapturingGatewayClient firstPageClient = new();
        firstPageClient.EnqueueQueryResult(Detail("alpha"));
        firstPageClient.EnqueueQueryResult(Detail("beta"));
        MemoriesClient firstPageMemories = CreateSearchingMemoriesClient(SearchHits("alpha", "beta") with { TotalCount = 3 });
        TenantQueryGateway firstPageGateway = CreateGateway(firstPageClient, memories: firstPageMemories);

        TenantListSnapshot firstPage = await firstPageGateway.ListTenantsAsync(
            new TenantListRequest(PageSize: 2, Search: "term"), previous: null, CancellationToken.None);

        firstPage.HasMore.ShouldBeTrue();
        firstPage.NextCursor.ShouldBe("memories-search:2");
        await firstPageMemories.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(request => request != null && request.MaxResults == 2 && request.Offset == 0),
            Arg.Any<CancellationToken>());

        CapturingGatewayClient secondPageClient = new();
        secondPageClient.EnqueueQueryResult(Detail("gamma"));
        MemoriesClient secondPageMemories = CreateSearchingMemoriesClient(SearchHits("gamma") with { TotalCount = 3 });
        TenantQueryGateway secondPageGateway = CreateGateway(secondPageClient, memories: secondPageMemories);

        TenantListSnapshot secondPage = await secondPageGateway.ListTenantsAsync(
            new TenantListRequest(Cursor: firstPage.NextCursor, PageSize: 2, Search: "term"), previous: null, CancellationToken.None);

        secondPage.HasMore.ShouldBeFalse();
        secondPage.NextCursor.ShouldBeNull();
        await secondPageMemories.Received(1).SearchAsync(
            Arg.Is<SearchRequest>(request => request != null && request.MaxResults == 2 && request.Offset == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_empty_search_uses_cursor_path_and_skips_memories()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([new TenantSummary("alpha", "Alpha", TenantStatus.Active)], null, false));
        client.EnqueueQueryResult(Detail("alpha"));
        MemoriesClient memories = CreateMemoriesClient();
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "   "), previous: null, CancellationToken.None); // whitespace term

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        await memories.DidNotReceive().SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task List_search_memories_unavailable_falls_back_to_degraded_cursor_list()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([new TenantSummary("alpha", "Alpha", TenantStatus.Active)], null, false));
        client.EnqueueQueryResult(Detail("alpha"));
        MemoriesClient memories = CreateMemoriesClient();
        memories.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Throws(new MemoriesRemoteException(HttpStatusCode.ServiceUnavailable, new MemoriesErrorResponse("UNAVAILABLE", "down", "retry")));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.IsDegraded.ShouldBeTrue();
        snapshot.Rows.ShouldHaveSingleItem(); // unfiltered cursor list still rendered
        snapshot.ErrorMessage.ShouldNotBeNull();
    }

    [Fact]
    public async Task List_search_degraded_axis_falls_back_to_cursor_list()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([new TenantSummary("alpha", "Alpha", TenantStatus.Active)], null, false));
        client.EnqueueQueryResult(Detail("alpha"));
        MemoriesSearchResult degraded = SearchHits("alpha") with { Degraded = true, UnavailableAxes = ["syntactic"] };
        TenantQueryGateway gateway = CreateGateway(client, memories: CreateSearchingMemoriesClient(degraded));

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.Rows.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task List_search_drops_forbidden_match_set_id_as_degraded()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(Detail("alpha"));
        client.EnqueueException(new EventStoreGatewayException(403, "Forbidden")); // beta hydrates forbidden
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("alpha", "beta"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Rows.Select(r => r.TenantId).ShouldBe(["alpha"]);
        snapshot.IsDegraded.ShouldBeTrue(); // dropped id is a degradation, not an error
    }

    [Fact]
    public async Task List_search_not_found_match_set_id_renders_degraded()
    {
        CapturingGatewayClient client = new();
        client.EnqueueException(new EventStoreGatewayException(404, "Not found"));
        MemoriesClient memories = CreateSearchingMemoriesClient(SearchHits("missing"));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.Degraded);
        snapshot.IsDegraded.ShouldBeTrue();
        snapshot.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task List_search_empty_match_set_renders_filtered_empty()
    {
        CapturingGatewayClient client = new();
        TenantQueryGateway gateway = CreateGateway(client, memories: CreateSearchingMemoriesClient(SearchHits()));

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "nomatch"), previous: null, CancellationToken.None);

        snapshot.Kind.ShouldBe(TenantListSurfaceKind.FilteredEmpty);
    }

    [Fact]
    public async Task List_search_unavailable_snapshot_excludes_secrets()
    {
        CapturingGatewayClient client = new();
        client.EnqueueQueryResult(new PaginatedResult<TenantSummary>([], null, false));
        MemoriesClient memories = CreateMemoriesClient();
        memories.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Throws(new MemoriesRemoteException(HttpStatusCode.ServiceUnavailable, new MemoriesErrorResponse("UNAVAILABLE", "bearer-token-leak", "etag-secret")));
        TenantQueryGateway gateway = CreateGateway(client, memories: memories);

        TenantListSnapshot snapshot = await gateway.ListTenantsAsync(
            new TenantListRequest(Search: "term"), previous: null, CancellationToken.None);

        string rendered = snapshot.ToString();
        rendered.ShouldNotContain("bearer-token-leak", Case.Insensitive);
        rendered.ShouldNotContain("etag-secret", Case.Insensitive);
    }

    private static TenantQueryGateway CreateGateway(CapturingGatewayClient client, string? userId = "operator-user", MemoriesClient? memories = null)
        => CreateGateway((IEventStoreGatewayClient)client, userId, memories);

    private static TenantQueryGateway CreateGateway(IEventStoreGatewayClient client, string? userId = "operator-user", MemoriesClient? memories = null)
    {
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        userContext.UserId.Returns(userId);
        userContext.TenantId.Returns("tenant.context");

        return new TenantQueryGateway(client, userContext, memories ?? CreateMemoriesClient());
    }

    // A substituted MemoriesClient (SearchAsync is virtual). Cursor-path tests never call SearchAsync;
    // search-path tests pass one with SearchAsync stubbed.
    private static MemoriesClient CreateMemoriesClient()
        => Substitute.For<MemoriesClient>(
            new HttpClient { BaseAddress = new Uri("https://memories.example/") },
            Options.Create(new MemoriesClientOptions()),
            Substitute.For<ILogger<MemoriesClient>>());

    private static MemoriesClient CreateSearchingMemoriesClient(MemoriesSearchResult result)
    {
        MemoriesClient memories = CreateMemoriesClient();
        memories.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult(result));
        return memories;
    }

    private static MemoriesSearchResult SearchHits(params string[] tenantIds)
        => new()
        {
            Results = tenantIds
                .Select(static (id, index) => new MemoriesScoredResult
                {
                    MemoryUnitId = $"mu-{id}",
                    Score = 100 - index,
                    ContentSnippet = "snippet",
                    SourceUri = $"tenant:{id}",
                    SourceType = MemoriesSourceType.Event,
                })
                .ToList(),
            TotalCount = tenantIds.Length,
            HasIndexedMemoryUnits = true,
            Query = "term",
        };

    private sealed class CapturingGatewayClient : IEventStoreGatewayClient
    {
        private readonly Queue<object> _responses = new();

        public List<SubmittedQuery> SubmittedQueries { get; } = [];

        public Task<EventStoreQueryResult<T>> SubmitQueryAsync<T>(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
        {
            SubmittedQueries.Add(new SubmittedQuery(request, ifNoneMatch));
            object next = _responses.Dequeue();
            if (next is Exception exception)
            {
                throw exception;
            }

            return Task.FromResult((EventStoreQueryResult<T>)next);
        }

        public Task<SubmitCommandResponse> SubmitCommandAsync(SubmitCommandRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<EventStoreQueryResult> SubmitQueryAsync(
            SubmitQueryRequest request,
            string? ifNoneMatch = null,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<StreamReadPage> ReadStreamAsync(StreamReadRequest request, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void EnqueueQueryResult<T>(
            T payload,
            string? eTag = "\"etag\"",
            QueryResponseMetadata? metadata = null,
            bool emitDefaultMetadata = true)
            => _responses.Enqueue(new EventStoreQueryResult<T>(
                "correlation",
                payload,
                IsNotModified: false,
                eTag)
            {
                Metadata = metadata ?? (emitDefaultMetadata ? new QueryResponseMetadata(ETag: eTag, IsStale: false) : null),
            });

        public void EnqueueNotModified(string? eTag, bool? isStale = null)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<TenantSummary>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true, IsStale: isStale),
            });

        public void EnqueueDetailNotModified(string? eTag, bool? isStale = null)
            => _responses.Enqueue(new EventStoreQueryResult<TenantDetail>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true, IsStale: isStale),
            });

        public void EnqueueUserTenantsNotModified(string? eTag, bool? isStale = null)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<UserTenantMembership>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true, IsStale: isStale),
            });

        public void EnqueueGlobalAdministratorsNotModified(string? eTag, bool? isStale = null)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<GlobalAdministratorSummary>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true, IsStale: isStale),
            });

        public void EnqueueAuditNotModified(string? eTag, bool? isStale = null)
            => _responses.Enqueue(new EventStoreQueryResult<PaginatedResult<TenantAuditEntry>>(
                null,
                null,
                IsNotModified: true,
                eTag)
            {
                Metadata = new QueryResponseMetadata(ETag: eTag, IsNotModified: true, IsStale: isStale),
            });

        public void EnqueueException(Exception exception)
            => _responses.Enqueue(exception);
    }

    private sealed record SubmittedQuery(SubmitQueryRequest Request, string? IfNoneMatch);

    private static bool HasActiveStatusFilter(SearchRequest? request)
        => request?.AttributeFilters is { } filters
        && filters.TryGetValue("status", out string? value)
        && string.Equals(value, TenantStatus.Active.ToString(), StringComparison.Ordinal);

    private static TenantDetail Detail(string tenantId)
        => new(
            tenantId,
            "Alpha",
            "Tenant alpha description",
            TenantStatus.Active,
            [
                new TenantMember("owner-user", TenantRole.TenantOwner),
                new TenantMember("reader-user", TenantRole.TenantReader),
            ],
            new Dictionary<string, string>
            {
                ["billing.mode"] = "trial",
            },
            DateTimeOffset.UtcNow);

    private static TenantDetail DetailWithStatus(string tenantId, TenantStatus status)
        => new(
            tenantId,
            $"Name-{tenantId}",
            null,
            status,
            [new TenantMember("owner-user", TenantRole.TenantOwner)],
            new Dictionary<string, string>(),
            DateTimeOffset.UtcNow);

    private static TenantAuditEntry AuditEntry(string eventId, AuditEventCategory category)
        => new(
            eventId,
            category is AuditEventCategory.Access ? "UserAddedToTenant" : "TenantConfigurationSet",
            category,
            "actor-user",
            DateTimeOffset.UtcNow,
            "tenant.alpha",
            new Dictionary<string, string>
            {
                ["userId"] = "target-user",
                ["key"] = "billing.mode",
                ["role"] = "TenantReader",
            });
}
