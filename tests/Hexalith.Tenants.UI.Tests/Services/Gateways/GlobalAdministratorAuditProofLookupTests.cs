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
    public async Task Requires_exact_provenance_and_producer_event_order_before_returning_redacted_row(
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

    [Theory]
    [InlineData(-120, false)]
    [InlineData(120, false)]
    [InlineData(-120, true)]
    [InlineData(120, true)]
    public async Task Exact_causal_proof_survives_a_Bff_clock_ahead_of_or_behind_the_event_producer(int bffOffsetMinutes, bool revoke)
    {
        DateTimeOffset eventTime = Original.AddSeconds(30);
        DateTimeOffset bffTime = eventTime.AddMinutes(bffOffsetMinutes);
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                request.From.ShouldBe(Original);
                return Task.FromResult(Snapshot(request, [Row("tracking-safe", 2, eventTime, revoke)]));
            },
            "tracking-safe", 2, "admin-user", revoke ? "GlobalAdministratorRemoved" : "GlobalAdministratorSet", Original, bffTime,
            CancellationToken.None);

        complete.ShouldBeTrue();
        proof.ShouldNotBeNull().Row.Timestamp.ShouldBe(eventTime);
        proof.Row.CommandCorrelationId.ShouldBeNull();
        proof.Row.AggregateSequence.ShouldBeNull();
    }

    [Fact]
    public async Task Old_receipt_proof_can_complete_more_than_fifty_pages()
    {
        int pages = 0;
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                pages++;
                request.PageSize.ShouldBe(50);
                TenantAuditRow[] rows = Enumerable.Range(0, 50).Select(index =>
                    Row($"other-command-{pages}-{index}", (pages - 1) * 50 + index + 2,
                        Submitted.AddMinutes(2).AddTicks((pages - 1) * 50 + index)) with
                    {
                        EventReference = $"event-{pages}-{index}",
                    }).ToArray();
                if (pages == 1)
                {
                    rows[0] = Row("tracking-safe", 2, Submitted.AddMinutes(1));
                }

                TenantAuditSnapshot page = Snapshot(request, rows);
                return Task.FromResult(page with
                {
                    ProjectionVersion = "tenant-sequence:3001",
                    HasMore = pages < 60,
                    NextCursor = pages < 60 ? $"cursor-{pages}" : null,
                });
            },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted, CancellationToken.None);

        pages.ShouldBe(60);
        complete.ShouldBeTrue();
        proof.ShouldNotBeNull();
    }

    [Fact]
    public async Task Lookup_deadline_bounds_a_stalled_read_even_when_the_reader_ignores_cancellation()
    {
        var clock = new ManualTimeProvider();
        var stalled = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationToken readToken = default;
        Task<(GlobalAdministratorAuditProof? Proof, bool Complete)> lookup = GlobalAdministratorAuditProofLookup.FindAsync(
            (_, token) => { readToken = token; return stalled.Task; },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted, CancellationToken.None, clock);

        clock.Advance(TimeSpan.FromSeconds(10));
        (GlobalAdministratorAuditProof? proof, bool complete) = await lookup.WaitAsync(TimeSpan.FromSeconds(5));
        proof.ShouldBeNull();
        complete.ShouldBeFalse();
        readToken.IsCancellationRequested.ShouldBeTrue();
        clock.PendingTimerCount.ShouldBe(0);
        stalled.SetResult(Snapshot(new TenantAuditRequest("system"), []));
    }

    [Fact]
    public async Task Caller_cancellation_remains_observable_instead_of_becoming_incomplete_proof()
    {
        using var source = new CancellationTokenSource();
        var stalled = new TaskCompletionSource<TenantAuditSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<(GlobalAdministratorAuditProof? Proof, bool Complete)> lookup = GlobalAdministratorAuditProofLookup.FindAsync(
            (_, _) => stalled.Task,
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted, source.Token);
        source.Cancel();
        await Should.ThrowAsync<OperationCanceledException>(async () => await lookup);
        stalled.SetResult(Snapshot(new TenantAuditRequest("system"), []));
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
    public async Task Duplicate_exact_corrective_rows_on_separate_pages_never_prove_a_pair()
    {
        int pagesRead = 0;
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                pagesRead++;
                TenantAuditRow row = Row("tracking-safe", 2, Submitted.AddMinutes(pagesRead));
                return Task.FromResult(pagesRead == 1
                    ? TenantAuditSnapshot.Ready([row], "next-page", true, "\"etag\"",
                        ReadModelFreshnessState.Current, request) with
                    {
                        Lifecycle = ProjectionLifecycleState.Current,
                        ProjectionVersion = "tenant-sequence:2",
                    }
                    : Snapshot(request, [row with { EventReference = "event-second-corrective" }]));
            },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        pagesRead.ShouldBe(2);
        proof.ShouldBeNull();
        complete.ShouldBeFalse();
    }

    [Fact]
    public async Task Corrective_search_rejects_projection_version_drift_between_pages()
    {
        int pagesRead = 0;
        (GlobalAdministratorAuditProof? proof, bool complete) = await GlobalAdministratorAuditProofLookup.FindAsync(
            (request, _) =>
            {
                pagesRead++;
                return Task.FromResult(pagesRead == 1
                    ? TenantAuditSnapshot.Ready(
                        [Row("tracking-safe", 2, Submitted.AddMinutes(1))],
                        "next-page", true, "\"etag\"", ReadModelFreshnessState.Current, request) with
                    {
                        Lifecycle = ProjectionLifecycleState.Current,
                        ProjectionVersion = "tenant-sequence:2",
                    }
                    : Snapshot(request, []) with { ProjectionVersion = "tenant-sequence:3" });
            },
            "tracking-safe", 2, "admin-user", "GlobalAdministratorSet", Original, Submitted,
            CancellationToken.None);

        pagesRead.ShouldBe(2);
        proof.ShouldBeNull();
        complete.ShouldBeFalse();
    }

    [Fact]
    public async Task Corrective_search_begins_at_original_producer_timestamp()
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

        requestedFrom.ShouldBe(Original);
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

    private static TenantAuditRow Row(string correlationId, long sequence, DateTimeOffset timestamp, bool revoke = false)
        => new("event-corrective", revoke ? "GlobalAdministratorRemoved" : "GlobalAdministratorSet", AuditEventCategory.Access,
            "actor-user", timestamp, "system", "admin-user", "system", revoke ? "GlobalAdministratorRemoved" : "GlobalAdministratorSet",
            "userId: admin-user", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current,
            QueryResponseProvenance.ProjectionBacked, new TenantAuditNarrative(UserId: "admin-user"))
        {
            CommandCorrelationId = correlationId,
            AggregateSequence = sequence,
        };
}
