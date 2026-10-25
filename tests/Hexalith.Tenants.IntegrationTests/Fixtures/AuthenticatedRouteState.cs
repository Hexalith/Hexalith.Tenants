using System.Globalization;
using System.Net.Http.Headers;

using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Server.Projections;
using Hexalith.Tenants.Contracts.Projections;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.UI.Services.Gateways;

using Shouldly;

namespace Hexalith.Tenants.IntegrationTests.Fixtures;

/// <summary>Command-seeded, isolated state shared by the live REST and UI qualifications.</summary>
/// <param name="TenantId">The isolated tenant.</param>
/// <param name="TargetUserId">The isolated member.</param>
/// <param name="Token">The validated caller's access token, retained only inside the test process.</param>
/// <param name="Persisted">The confirmed persisted projection.</param>
internal sealed record AuthenticatedRouteState(string TenantId, string TargetUserId, string Token, TenantReadModel Persisted)
{
    internal const string CallerId = "11111111-1111-1111-1111-111111111111";

    /// <summary>Gets the committed sequence, event type and target for the isolated audit prefix.</summary>
    internal IReadOnlyList<(long Sequence, string EventType, string Target)> SeededAuditEvents { get; init; } = [];

    /// <summary>Creates state only through the command gateway and reads its persisted end state.</summary>
    internal static async Task<AuthenticatedRouteState> CreateAsync(HttpClient commandApi, CancellationToken cancellationToken)
    {
        string token = AspireTopologyTests.CreateDemoJwt();
        string tenantId = $"live-routes-{Guid.NewGuid():N}";
        string target = $"live-member-{Guid.NewGuid():N}";
        var bootstrap = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(
            commandApi,
            AspireTopologyTests.CreateCommand("global-administrators", "global-administrators", nameof(BootstrapGlobalAdmin), new BootstrapGlobalAdmin(CallerId)),
            token, cancellationToken, allowAlreadyBootstrappedConflict: true);
        (bootstrap.Status == "Completed" || bootstrap.RejectionEventType == "GlobalAdminAlreadyBootstrappedRejection").ShouldBeTrue();
        TestContext.Current.TestOutputHelper?.WriteLine("Live route seed: bootstrap checked.");

        var createRequest = AspireTopologyTests.CreateCommand("tenants", tenantId, nameof(CreateTenant), new CreateTenant(tenantId, "Live authenticated routes", "Command-seeded route qualification"));
        var created = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(
            commandApi,
            createRequest,
            token, cancellationToken);
        created.Status.ShouldBe("Completed");
        TestContext.Current.TestOutputHelper?.WriteLine("Live route seed: tenant creation completed.");
        created.CommittedEventSequence.ShouldNotBeNull();
        created.EventCount.ShouldBe(1);
        List<(long Sequence, string EventType, string Target)> auditEvents =
            [(created.CommittedEventSequence.Value, nameof(TenantCreated), tenantId)];
        await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId,
            TenantProjectionVersionFormat.SequencePrefix + created.CommittedEventSequence.Value.ToString(CultureInfo.InvariantCulture), cancellationToken);
        long? finalSequence = null;
        foreach ((string userId, TenantRole role) in new[] { (CallerId, TenantRole.TenantOwner), (target, TenantRole.TenantReader) })
        {
            var addRequest = AspireTopologyTests.CreateCommand("tenants", tenantId, nameof(AddUserToTenant), new AddUserToTenant(tenantId, userId, role));
            var added = await AspireTopologyTests.SubmitAndWaitForTerminalStatusAsync(
                commandApi,
                addRequest,
                token, cancellationToken);
            added.Status.ShouldBe("Completed");
            TestContext.Current.TestOutputHelper?.WriteLine("Live route seed: member command completed.");
            finalSequence = added.CommittedEventSequence;
            finalSequence.ShouldNotBeNull();
            added.EventCount.ShouldBe(1);
            auditEvents.Add((finalSequence.Value, nameof(UserAddedToTenant), userId));
            await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId,
                TenantProjectionVersionFormat.SequencePrefix + finalSequence.Value.ToString(CultureInfo.InvariantCulture), cancellationToken);
        }

        finalSequence.ShouldNotBeNull();
        string expectedVersion = TenantProjectionVersionFormat.SequencePrefix + finalSequence.Value.ToString(CultureInfo.InvariantCulture);
        TenantReadModel persisted = await AspireTopologyTests.WaitForPersistedTenantAsync(tenantId, expectedVersion, cancellationToken);
        TestContext.Current.TestOutputHelper?.WriteLine("Live route seed: final projection version persisted.");
        persisted.Members.Count.ShouldBe(2);
        persisted.Members[CallerId].ShouldBe(TenantRole.TenantOwner);
        persisted.Members[target].ShouldBe(TenantRole.TenantReader);
        persisted.ProjectedAt.ShouldNotBeNull();
        persisted.ProjectionVersion.ShouldNotBeNullOrWhiteSpace();
        auditEvents.Select(static item => item.Sequence).ShouldBe([1L, 2L, 3L]);
        auditEvents.Select(static item => item.Sequence).Distinct().Count().ShouldBe(3);
        return new(tenantId, target, token, persisted) { SeededAuditEvents = auditEvents };
    }

    /// <summary>Creates a production-client transport without mutating the fixture's shared headers.</summary>
    internal HttpClient CreateClient(HttpClient shared, Action<EntityTagHeaderValue?>? observeETag = null)
        => AspireTopologyTests.CreateIsolatedTenantsApiClient(shared, Token, observeETag ?? (_ => { }));

    /// <summary>Waits for the real list index and returns the page cursor containing the seeded tenant.</summary>
    internal async Task<string?> WaitForListPageAsync(HttpClient shared, int pageSize, CancellationToken cancellationToken)
    {
        using HttpClient http = CreateClient(shared);
        var client = new TenantsRestQueryClient(http);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        while (true)
        {
            string? cursor = null;
            do
            {
                var response = await client.ListTenantsAsync(new ListTenantsQuery { Cursor = cursor, PageSize = pageSize }, null, timeout.Token);
                response.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
                var page = response.Payload.ShouldNotBeNull();
                if (page.Items.Any(item => item.TenantId == TenantId && item.Name == Persisted.Name && item.Status == Persisted.Status))
                {
                    return cursor;
                }

                if (page.HasMore)
                {
                    page.Cursor.ShouldNotBeNullOrWhiteSpace();
                }

                cursor = page.HasMore ? page.Cursor : null;
            }
            while (cursor is not null);

            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
        }
    }

    /// <summary>Finds the actual caller-membership page containing this seed without assuming page one.</summary>
    internal async Task<string?> WaitForCallerMembershipPageAsync(HttpClient shared, int pageSize, CancellationToken cancellationToken)
    {
        using HttpClient http = CreateClient(shared);
        var client = new TenantsRestQueryClient(http);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMinutes(1));
        while (true)
        {
            string? cursor = null;
            do
            {
                var response = await client.GetUserTenantsAsync(new GetUserTenantsQuery { UserId = CallerId, Cursor = cursor, PageSize = pageSize }, null, timeout.Token);
                response.FailureKind.ShouldBe(TenantsRestQueryFailureKind.None);
                var page = response.Payload.ShouldNotBeNull();
                if (page.Items.Any(item => item.TenantId == TenantId && item.Name == Persisted.Name && item.Role == Persisted.Members[CallerId]))
                {
                    return cursor;
                }

                if (page.HasMore)
                {
                    page.Cursor.ShouldNotBeNullOrWhiteSpace();
                }

                cursor = page.HasMore ? page.Cursor : null;
            }
            while (cursor is not null);

            await Task.Delay(TimeSpan.FromMilliseconds(250), timeout.Token);
        }
    }

    /// <summary>Omits caller credentials and literal scope from incidental diagnostic output.</summary>
    public override string ToString() => nameof(AuthenticatedRouteState);
}
