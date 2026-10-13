using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class AuditStreamIsolationTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData("tenant", "tenant", true)]
    [InlineData("global-administrators", "global-administrators", true)]
    [InlineData(null, "tenant", false)]
    [InlineData(null, "global-administrators", false)]
    [InlineData("tenant", null, false)]
    [InlineData("tenant", "global-administrators", false)]
    [InlineData("global-administrators", null, false)]
    [InlineData("global-administrators", "tenant", false)]
    public void RetainedAuditSnapshotRequiresTheSameStream(string? original, string? requested, bool expected)
    {
        var request = new TenantAuditRequest("system", Stream: original);
        TenantAuditSnapshot snapshot = TenantAuditSnapshot.Ready(
            [], null, false, "validator", ReadModelFreshnessState.Current, request) with
        {
            CallerScope = "current-caller",
        };

        snapshot.MatchesScope(request with { Stream = requested }).ShouldBe(expected);
        snapshot.MatchesScope(request with { Stream = requested }, "current-caller").ShouldBe(expected);
        snapshot.MatchesScope(request with { Stream = requested }, "previous-caller").ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("tenant")]
    [InlineData("global-administrators")]
    public void FailureSnapshotsPreserveTheRequestedStream(string? stream)
    {
        var request = new TenantAuditRequest("system", Stream: stream);
        TenantAuditSnapshot[] snapshots =
        [
            TenantAuditSnapshot.Empty(false, ReadModelFreshnessState.Current, "validator", request),
            TenantAuditSnapshot.Unavailable(request),
            TenantAuditSnapshot.Unauthorized(request),
            TenantAuditSnapshot.InvalidCursor(request),
            TenantAuditSnapshot.Error(request),
        ];

        foreach (TenantAuditSnapshot snapshot in snapshots)
        {
            snapshot.MatchesScope(request).ShouldBeTrue();
            snapshot.MatchesScope(request with { Stream = stream == "tenant" ? "global-administrators" : "tenant" })
                .ShouldBeFalse();
        }
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("tenant", false)]
    [InlineData("global-administrators", true)]
    public async Task GlobalProofRejectsAResponseFromAnotherStream(string? responseStream, bool expectedComplete)
    {
        DateTimeOffset original = new(2026, 10, 13, 10, 0, 0, TimeSpan.Zero);
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                request.TenantId.ShouldBe("system");
                request.Stream.ShouldBe("global-administrators");
                TenantAuditRequest responseRequest = request with { Stream = responseStream };
                return Task.FromResult(TenantAuditSnapshot.Ready(
                    [], null, false, "validator", ReadModelFreshnessState.Current, responseRequest) with
                {
                    Lifecycle = ProjectionLifecycleState.Current,
                    ProjectionVersion = "tenant-sequence:2",
                });
            },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", original, original.AddMinutes(1),
            TestContext.Current.CancellationToken);

        proof.ShouldBeNull();
        complete.ShouldBe(expectedComplete);
    }
}
