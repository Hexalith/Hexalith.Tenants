using System.Net;
using System.Net.Http.Headers;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Memories.Client.Rest;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.IntegrationTests.Fixtures;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantList;
using Hexalith.Tenants.UI.State.TenantUsers;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests;

/// <summary>Qualifies the six production REST reads against authenticated live Aspire sockets.</summary>
[Collection("AspireTopology")]
[DaprTestSerialization]
[Trait("Category", "Integration")]
public sealed class AuthenticatedTenantsRouteTests(AspireTopologyFixture fixture) : IDisposable
{
    private readonly IDisposable _daprTestLease = DaprTestExecutionGate.Enter();

    /// <inheritdoc/>
    public void Dispose() => _daprTestLease.Dispose();

    [DaprFact]
    public async Task SixAuthenticatedDaprRestReadsMatchPersistedState()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        EntityTagHeaderValue? observed = null;
        using HttpClient http = state.CreateClient(fixture.TenantsUiDaprClient, value => observed = value);
        http.DefaultRequestHeaders.Add("dapr-app-id", "tenants-api");
        var client = new TenantsRestQueryClient(http);

        string? cursor = await state.WaitForListPageAsync(fixture.TenantsApiClient, 100, timeout.Token);
        bool found = false;
        do
        {
            var query = new ListTenantsQuery { PageSize = 100, Cursor = cursor };
            var list = await AssertReadAsync(tag => client.ListTenantsAsync(query, tag, timeout.Token));
            found |= list.Items.Any(item => item.TenantId == state.TenantId && item.Name == state.Persisted.Name);
            cursor = list.HasMore ? list.Cursor : null;
        }
        while (!found && cursor is not null);
        found.ShouldBeTrue();

        TenantDetail detail = await AssertReadAsync(tag => client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, tag, timeout.Token), expectedVersion: state.Persisted.ProjectionVersion);
        detail.TenantId.ShouldBe(state.Persisted.TenantId);
        detail.Name.ShouldBe(state.Persisted.Name);
        detail.Description.ShouldBe(state.Persisted.Description);
        detail.Status.ShouldBe(state.Persisted.Status);
        detail.Members.Select(static member => (member.UserId, member.Role)).OrderBy(static member => member.UserId)
            .ShouldBe(state.Persisted.Members.Select(static member => (UserId: member.Key, Role: member.Value)).OrderBy(static member => member.UserId));
        observed.ShouldNotBeNull().IsWeak.ShouldBeFalse();

        var users = await AssertReadAsync(tag => client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, PageSize = 100 }, tag, timeout.Token));
        users.Items.Select(static member => (member.UserId, member.Role)).OrderBy(static member => member.UserId)
            .ShouldBe(state.Persisted.Members.Select(static member => (UserId: member.Key, Role: member.Value)).OrderBy(static member => member.UserId));
        var memberships = await AssertReadAsync(tag => client.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = state.TargetUserId, PageSize = 100 }, tag, timeout.Token), page => page.Items.Any(item => item.TenantId == state.TenantId));
        memberships.Items.ShouldHaveSingleItem().ShouldBe(new UserTenantMembership(state.Persisted.TenantId, state.Persisted.Name,
            state.Persisted.Status, state.Persisted.Members[state.TargetUserId]));
        var audit = await AssertReadAsync(tag => client.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = state.TenantId, PageSize = 100 }, tag, timeout.Token), page => page.Items.Count >= 3, expectedVersion: state.Persisted.ProjectionVersion);
        audit.Items.Count.ShouldBe(3);
        audit.Items.All(item => item.TenantId == state.TenantId).ShouldBeTrue();
        audit.Items.Select(static item => item.EventId).Distinct(StringComparer.Ordinal).Count().ShouldBe(3);
        foreach (var expected in state.SeededAuditEvents)
        {
            TenantAuditEntry entry = audit.Items.Single(item => item.EventType == expected.EventType && item.Target == expected.Target);
            entry.EventType.ShouldBe(expected.EventType);
            entry.Target.ShouldBe(expected.Target);
            entry.ActorId.ShouldBe(AuthenticatedRouteState.CallerId);
            // These event types deliberately withhold wire sequences. Verify the isolated ordered
            // prefix against actual committed 1/2/3 outcomes instead of expanding that contract.
            entry.AggregateSequence.ShouldBeNull();
            if (state.Persisted.Members.TryGetValue(expected.Target, out var role))
            {
                entry.NarrativePayload["role"].ShouldBe(role.ToString());
            }
        }
        state.SeededAuditEvents.Select(static item => item.Sequence).ShouldBe([1L, 2L, 3L]);
        audit.Items.Select(static item => (item.EventType, item.Target))
            .ShouldBe(state.SeededAuditEvents.OrderBy(static item => item.Sequence).Select(static item => (item.EventType, item.Target)));
        var administrators = await AssertReadAsync(tag => client.GetGlobalAdministratorsAsync(new GetGlobalAdministratorsQuery { PageSize = 100 }, tag, timeout.Token));
        administrators.Items.ShouldContain(item => item.UserId == AuthenticatedRouteState.CallerId);
    }

    [DaprFact]
    public async Task ConditionalDetailReadRecognizesPersistedAuthority()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        using HttpClient http = state.CreateClient(fixture.TenantsApiClient);
        var client = new TenantsRestQueryClient(http);
        TenantsRestQueryResponse<TenantDetail> first = await client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, null, timeout.Token);
        first.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        first.Payload.ShouldNotBeNull().TenantId.ShouldBe(state.Persisted.TenantId);
        AssertAuthority(first.Metadata);
        first.Metadata.ProjectionVersion.ShouldBe(state.Persisted.ProjectionVersion);

        TenantsRestQueryResponse<TenantDetail> conditional = await client.GetTenantAsync(new GetTenantQuery { TenantId = state.TenantId }, first.Metadata.ETag, timeout.Token);
        conditional.StatusCode.ShouldBe((int)HttpStatusCode.NotModified);
        conditional.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        conditional.Metadata.IsNotModified.ShouldBe(true);
        conditional.Metadata.ETag.ShouldBe(first.Metadata.ETag);
        conditional.Metadata.ProjectionVersion.ShouldBe(first.Metadata.ProjectionVersion);
        AssertAuthority(conditional.Metadata);
    }

    [DaprFact]
    public async Task InvalidProtectedCursorHasRealSentinelAndRecoversPageOneThroughProductionGateway()
    {
        fixture.SkipIfUnavailable();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        AuthenticatedRouteState state = await AuthenticatedRouteState.CreateAsync(fixture.CommandApiClient, timeout.Token);
        using HttpClient http = state.CreateClient(fixture.TenantsApiClient);
        var client = new TenantsRestQueryClient(http);
        await state.WaitForListPageAsync(fixture.TenantsApiClient, 100, timeout.Token);
        var invalid = await client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, Cursor = "foreign-protected-cursor", PageSize = 1 }, null, timeout.Token);
        invalid.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        invalid.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);
        var issuedMembers = await client.GetTenantUsersAsync(new GetTenantUsersQuery { TenantId = state.TenantId, PageSize = 1 }, null, timeout.Token);
        issuedMembers.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        var issuedPage = issuedMembers.Payload.ShouldNotBeNull();
        issuedPage.HasMore.ShouldBeTrue();
        issuedPage.Cursor.ShouldNotBeNullOrWhiteSpace();
        var incompatibleQuery = await client.ListTenantsAsync(new ListTenantsQuery { Cursor = issuedPage.Cursor, PageSize = 1 }, null, timeout.Token);
        incompatibleQuery.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        incompatibleQuery.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);

        using var memoriesHttp = new HttpClient { BaseAddress = new Uri("https://unused.invalid") };
        var memories = new MemoriesClient(memoriesHttp, Options.Create(new MemoriesClientOptions()), NullLogger<MemoriesClient>.Instance);
        using var gateway = new TenantQueryGateway(client, new LiveRouteUserContext(), memories, new TenantSearchCursorCodec(new EphemeralDataProtectionProvider()));
        var invalidList = await client.ListTenantsAsync(new ListTenantsQuery { Cursor = "foreign-protected-cursor", PageSize = 1 }, null, timeout.Token);
        invalidList.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        invalidList.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidCursor);
        var baselineList = await gateway.ListTenantsAsync(new TenantListRequest(PageSize: 1), null, timeout.Token);
        baselineList.Kind.ShouldBe(TenantListSurfaceKind.Ready);
        var recoveredList = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: issuedPage.Cursor, PageSize: 1), null, timeout.Token);
        recoveredList.Notice.ShouldBe(TenantListReason.ListRefreshed);
        recoveredList.Rows.ShouldBe(baselineList.Rows);
        recoveredList.HasMore.ShouldBe(baselineList.HasMore);
        string.IsNullOrWhiteSpace(recoveredList.NextCursor).ShouldBe(string.IsNullOrWhiteSpace(baselineList.NextCursor));
        recoveredList.RequestCursor.ShouldBeNull();
        recoveredList.RequestPageSize.ShouldBe(baselineList.RequestPageSize);
        if (baselineList.HasMore)
        {
            var baselineNext = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: baselineList.NextCursor, PageSize: 1), null, timeout.Token);
            var recoveredNext = await gateway.ListTenantsAsync(new TenantListRequest(Cursor: recoveredList.NextCursor, PageSize: 1), null, timeout.Token);
            recoveredNext.Rows.ShouldBe(baselineNext.Rows);
            recoveredNext.HasMore.ShouldBe(baselineNext.HasMore);
        }
        var recovered = await gateway.GetTenantUsersAsync(new TenantUsersRequest(state.TenantId, Cursor: "foreign-protected-cursor", PageSize: 1), null, timeout.Token);
        recovered.PagingRecovered.ShouldBeTrue();
        recovered.Rows.ShouldHaveSingleItem().UserId.ShouldBe(state.Persisted.Members.Keys.Order().First());

        // An invalid typed query generates a real, unrelated 400 and must retain InvalidRequest.
        var unrelated = await client.GetTenantAuditAsync(new GetTenantAuditQuery { TenantId = state.TenantId, From = DateTimeOffset.UtcNow, To = DateTimeOffset.UtcNow.AddDays(-1) }, null, timeout.Token);
        unrelated.StatusCode.ShouldBe((int)HttpStatusCode.BadRequest);
        unrelated.FailureKind.ShouldBe(TenantsRestQueryFailureKind.InvalidRequest);
    }

    [DaprFact]
    public async Task AnonymousRestRequestCannotReadCommandSeededTenant()
    {
        fixture.SkipIfUnavailable();
        using HttpResponseMessage response = await fixture.TenantsApiClient.GetAsync("/api/tenants", TestContext.Current.CancellationToken);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<T> AssertReadAsync<T>(Func<string?, Task<TenantsRestQueryResponse<T>>> read, Func<T, bool>? projected = null, string? expectedVersion = null)
        where T : class
    {
        TenantsRestQueryResponse<T> first = await read(null);
        DateTimeOffset deadline = DateTimeOffset.UtcNow.AddSeconds(30);
        while ((first.Payload is null || (projected is not null && !projected(first.Payload))) && DateTimeOffset.UtcNow < deadline)
        {
            first.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
            await Task.Delay(TimeSpan.FromMilliseconds(250));
            first = await read(null);
        }

        first.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
        first.StatusCode.ShouldBe((int)HttpStatusCode.OK);
        T payload = first.Payload.ShouldNotBeNull();
        if (projected is not null)
        {
            projected(payload).ShouldBeTrue();
        }
        AssertAuthority(first.Metadata);
        if (expectedVersion is not null)
        {
            first.Metadata.ProjectionVersion.ShouldBe(expectedVersion);
        }
        return payload;
    }

    private static void AssertAuthority(QueryResponseMetadata metadata)
    {
        metadata.Provenance.ShouldBe(QueryResponseProvenance.ProjectionBacked);
        metadata.Lifecycle.ShouldBe(ProjectionLifecycleState.Current);
        metadata.ETag.ShouldNotBeNullOrWhiteSpace();
        metadata.IsStale.ShouldBe(false);
        metadata.IsDegraded.ShouldNotBe(true);
        metadata.ServedAt.ShouldNotBeNull();
        metadata.ServedAt.Value.ShouldBeInRange(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddSeconds(5));
    }
}
