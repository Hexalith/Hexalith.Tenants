using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.UI.Services.Gateways;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantCommands;

using Shouldly;

namespace Hexalith.Tenants.UI.Tests.Services.Gateways;

public sealed class GlobalAdministratorAuditProofLookupTests
{
    private static readonly DateTimeOffset Original = DateTimeOffset.Parse("2026-06-01T10:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    private static readonly DateTimeOffset Submitted = Original.AddMinutes(1);

    [Theory]
    [InlineData("tracking-safe", 2L, 5, true)]
    [InlineData("other-command", 2L, 5, false)]
    [InlineData("tracking-safe", 3L, 5, false)]
    [InlineData("tracking-safe", 2L, 0, false)]
    [InlineData("tracking-safe", 2L, 1, true)]
    public async Task Requires_exact_provenance_and_post_submit_time_before_returning_redacted_row(
        string correlationId,
        long sequence,
        int minutesAfterOriginal,
        bool expected)
    {
        TenantAuditRow row = Row(correlationId, sequence, Original.AddMinutes(minutesAfterOriginal));
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) => Task.FromResult(Snapshot(request, [row])),
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        complete.ShouldBeTrue();
        (proof is not null).ShouldBe(expected);
        if (proof is not null)
        {
            proof.Row.EventReference.ShouldBe("event-corrective");
            proof.Row.CommandCorrelationId.ShouldBeNull();
            proof.Row.AggregateSequence.ShouldBeNull();
        }
    }

    [Fact]
    public async Task Rejects_duplicate_exact_matches_even_when_each_has_valid_provenance()
    {
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) => Task.FromResult(Snapshot(request,
                [Row("tracking-safe", 2, Submitted.AddMinutes(1)), Row("tracking-safe", 2, Submitted.AddMinutes(2))])),
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        proof.ShouldBeNull();
        complete.ShouldBeFalse();
    }

    [Fact]
    public async Task Corrective_search_begins_at_submission_boundary()
    {
        DateTimeOffset? requestedFrom = null;
        _ = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                requestedFrom = request.From;
                return Task.FromResult(Snapshot(request, [Row("tracking-safe", 2, Submitted.AddMinutes(1))]));
            },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        requestedFrom.ShouldBe(Submitted);
    }

    [Fact]
    public async Task Current_audit_below_committed_sequence_stays_incomplete_until_projection_catches_up()
    {
        string projectionVersion = "tenant-sequence:1";
        TenantAuditRow corrective = Row("tracking-safe", 2, Submitted.AddMinutes(1));
        Task<(GlobalAdministratorAuditProof? Proof, bool Complete)> Lookup()
            => GlobalAdministratorAuditProofLookup.FindAsync(
                (request, _) => Task.FromResult(Snapshot(request, [corrective]) with
                {
                    ProjectionVersion = projectionVersion,
                }),
                "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
                CancellationToken.None);

        (GlobalAdministratorAuditProof? earlyProof, bool earlyComplete) = await Lookup();
        earlyProof.ShouldBeNull();
        earlyComplete.ShouldBeFalse();
        projectionVersion = "tenant-sequence:2";
        (GlobalAdministratorAuditProof? currentProof, bool currentComplete) = await Lookup();
        currentComplete.ShouldBeTrue();
        currentProof.ShouldNotBeNull();
    }

    [Fact]
    public async Task Mismapped_non_access_global_event_cannot_become_corrective_proof()
    {
        TenantAuditRow mismapped = Row("tracking-safe", 2, Submitted.AddMinutes(1)) with
        {
            Category = AuditEventCategory.Administrative,
        };
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) => Task.FromResult(Snapshot(request, [mismapped])),
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        complete.ShouldBeTrue();
        proof.ShouldBeNull();
    }

    [Fact]
    public void Circuit_registry_retains_more_than_twenty_pairs_without_losing_old_receipt_links()
    {
        var registry = new GlobalAdministratorCorrectionProofRegistry();
        for (int index = 0; index < 25; index++)
        {
            string original = $"original-{index}";
            string corrective = $"corrective-{index}";
            TenantCorrectionStartIntent intent = new(
                original, "system", "admin-user", "GlobalAdministratorRemoved", "projection",
                TenantCorrectionCommandDomain.GlobalAdministrators,
                TenantCorrectionCommandType.SetGlobalAdministrator, null, [],
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["userId"] = "admin-user",
                    ["originalTimestamp"] = Original.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });
            GlobalAdministratorCorrectionSnapshot snapshot = GlobalAdministratorCorrectionSnapshot.FromIntent(intent) with
            {
                LifecycleState = TenantCommandLifecycleState.Confirmed,
                AuditState = TenantCommandAuditState.AuditAvailable,
                CorrelationId = $"tracking-{index}",
                CommittedEventSequence = index + 1,
                SubmittedAtUtc = Submitted,
                ProofLink = new TenantCorrectionProofLink(original, corrective, Original, Submitted.AddMinutes(1), "safe"),
            };
            registry.Remember(snapshot);
        }

        registry.Find("original-0").ShouldNotBeNull();
        registry.Find("corrective-0").ShouldNotBeNull();
        registry.Find("original-1").ShouldNotBeNull();
        registry.Find("corrective-24").ShouldNotBeNull();
    }

    [Fact]
    public void Circuit_registry_retains_both_links_for_a_receipt_in_successive_corrections()
    {
        var registry = new GlobalAdministratorCorrectionProofRegistry();
        foreach ((string original, string corrective) in new[] { ("event-a", "event-b"), ("event-b", "event-c") })
        {
            TenantCorrectionStartIntent intent = new(
                original, "system", "admin-user", "GlobalAdministratorRemoved", "projection",
                TenantCorrectionCommandDomain.GlobalAdministrators,
                TenantCorrectionCommandType.SetGlobalAdministrator, null, [],
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["userId"] = "admin-user",
                    ["originalTimestamp"] = Original.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
                });
            registry.Remember(GlobalAdministratorCorrectionSnapshot.FromIntent(intent) with
            {
                LifecycleState = TenantCommandLifecycleState.Confirmed,
                AuditState = TenantCommandAuditState.AuditAvailable,
                CorrelationId = $"tracking-{corrective}",
                CommittedEventSequence = 2,
                SubmittedAtUtc = Submitted,
                ProofLink = new TenantCorrectionProofLink(original, corrective, Original, Submitted.AddMinutes(1), "safe"),
            });
        }

        registry.FindAll("event-b").Select(snapshot => snapshot.ProofLink!.OriginalAuditReference)
            .ShouldBe(["event-a", "event-b"]);
    }

    private static TenantAuditSnapshot Snapshot(TenantAuditRequest request, IReadOnlyList<TenantAuditRow> rows)
        => TenantAuditSnapshot.Ready(rows, null, false, "\"etag\"", ReadModelFreshnessState.Current, request) with
        {
            Lifecycle = ProjectionLifecycleState.Current,
            ProjectionVersion = "tenant-sequence:2",
        };

    private static TenantAuditRow Row(string correlationId, long sequence, DateTimeOffset timestamp)
        => new("event-corrective", "GlobalAdministratorSet", AuditEventCategory.Access,
            "actor-user", timestamp, "system", "admin-user", "system", "GlobalAdministratorSet",
            "userId: admin-user", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked, new TenantAuditNarrative(UserId: "admin-user"))
        {
            CommandCorrelationId = correlationId,
            AggregateSequence = sequence,
        };
}
