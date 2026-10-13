using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using Shouldly;

using static Hexalith.Tenants.UI.Tests.TenantCommandProofFixtures;

namespace Hexalith.Tenants.UI.Tests.State;

public sealed class TenantCommandAuthoritativeProofTests
{
    public static IEnumerable<object[]> Flows()
        => new[] { "create", "add", "role", "remove", "metadata" }.Select(flow => new object[] { flow });

    [Theory]
    [MemberData(nameof(Flows))]
    public void UnverifiedStatusCannotAuthorizeSuccessRejectionOrNoOp(string flow)
    {
        foreach (CommandStatus status in new[] { CommandStatus.EventsStored, CommandStatus.Completed, CommandStatus.Rejected })
        {
            object result = Apply(Start(flow), new TenantCommandStatusResult(status, RejectionCode: "UserNotInTenant", EventCount: 0));
            View(result).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
            View(result).Events.ShouldBeFalse();
            View(result).Audit.ShouldBe(TenantCommandAuditState.AuditUnavailable);
            View(Confirm(result, Proof(Detail(flow)))).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        }
    }

    [Theory]
    [MemberData(nameof(Flows))]
    public void PendingAndRetryableLookupRetainAcceptedAndStoredTruth(string flow)
    {
        foreach (TenantCommandStatusResult transient in new[]
        {
            TenantCommandStatusResult.Pending("Pending"), TenantCommandStatusResult.RetryableFailure("Retry")
        })
        {
            object accepted = Apply(Start(flow), transient);
            var acceptedView = View(accepted);
            var originalView = View(Start(flow));
            (acceptedView.State, acceptedView.Events, acceptedView.Message, acceptedView.Correlation)
                .ShouldBe((originalView.State, originalView.Events, originalView.Message, originalView.Correlation));
            acceptedView.Audit.ShouldBe(transient.IsPending ? originalView.Audit : TenantCommandAuditState.AuditUnavailable);
            object stored = Apply(Start(flow), new TenantCommandStatusResult(CommandStatus.EventsStored, HasVerifiedCommandIdentity: true));
            var storedView = View(stored);
            var retriedView = View(Apply(stored, transient));
            (retriedView.State, retriedView.Events, retriedView.Message, retriedView.Correlation)
                .ShouldBe((storedView.State, storedView.Events, storedView.Message, storedView.Correlation));
            retriedView.Audit.ShouldBe(transient.IsPending ? storedView.Audit : TenantCommandAuditState.AuditUnavailable);
        }
    }

    [Theory]
    [MemberData(nameof(Flows))]
    public void ReceivedAndProcessingPreserveStoredEvidenceAndExactAttemptBeforeConfirmation(string flow)
    {
        foreach (CommandStatus earlyStatus in new[] { CommandStatus.Received, CommandStatus.Processing })
        {
            object stored = Apply(Start(flow), new TenantCommandStatusResult(CommandStatus.EventsStored, HasVerifiedCommandIdentity: true));
            object retained = Apply(stored, new TenantCommandStatusResult(earlyStatus, HasVerifiedCommandIdentity: true));
            var retainedView = View(retained);
            (retainedView.State, retainedView.Events, retainedView.Message, retainedView.Correlation)
                .ShouldBe((TenantCommandLifecycleState.ProjectionPending, true, "message-1", "correlation-1"));
            if (flow == "remove")
            {
                // Stored-event truth survives the early status, but removal still waits for its exact commit.
                View(Confirm(retained, Proof(Detail(flow)))).State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
                retained = Apply(Apply(retained, Completed()), new TenantCommandStatusResult(earlyStatus, HasVerifiedCommandIdentity: true));
                ((TenantRemoveMemberCommandSnapshot)retained).CommittedEventSequence.ShouldBe(2);
                retainedView = View(retained);
                (retainedView.State, retainedView.Events, retainedView.Message, retainedView.Correlation)
                    .ShouldBe((TenantCommandLifecycleState.ProjectionPending, true, "message-1", "correlation-1"));
            }

            View(Confirm(retained, Proof(Detail(flow)))).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        }
    }

    [Fact]
    public void RemovalRejectsConflictingPositiveCommitAndKeepsTheEstablishedSequence()
    {
        TenantRemoveMemberCommandSnapshot established = (TenantRemoveMemberCommandSnapshot)Apply(Start("remove"), Completed());
        TenantRemoveMemberCommandSnapshot conflicting = established.ApplyStatus(Completed() with { CommittedEventSequence = 3 });
        conflicting.State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        conflicting.HasCommandEventEvidence.ShouldBeTrue();
        conflicting.CommittedEventSequence.ShouldBe(2);
        conflicting.MessageId.ShouldBe("message-1");
        conflicting.CorrelationId.ShouldBe("correlation-1");
        conflicting.ConfirmProjection(Proof(Detail("remove"), "tenant-sequence:3"))
            .State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        var otherEvent = new Hexalith.Tenants.UI.State.TenantAudit.TenantAuditRow("evt-other-sequence", "UserRemovedFromTenant",
            AuditEventCategory.Access, "actor", DateTimeOffset.UtcNow, "tenant.alpha", "reader", "tenant.alpha",
            "UserRemovedFromTenant", "userId: reader", ReadModelFreshnessState.Current)
            { CommandCorrelationId = "correlation-1", AggregateSequence = 3 };
        TenantRemoveMemberCommandSnapshot.IsMatchingRemovalProof(otherEvent, "tenant.alpha", "reader",
            conflicting.CorrelationId, conflicting.CommittedEventSequence).ShouldBeFalse();
        conflicting.AuditState.ShouldNotBe(TenantCommandAuditState.AuditAvailable);

        TenantRemoveMemberCommandSnapshot repeated = established.ApplyStatus(Completed());
        repeated.CommittedEventSequence.ShouldBe(2);
        repeated.ConfirmProjection(Proof(Detail("remove"))).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        established.ApplyStatus(Completed() with { CommittedEventSequence = null }).CommittedEventSequence.ShouldBe(2);
    }

    [Theory]
    [MemberData(nameof(Flows))]
    public void ContradictoryStatusFlagsFailClosed(string flow)
    {
        foreach (TenantCommandStatusResult status in new[]
        {
            new TenantCommandStatusResult(null, IsPending: true, IsRetryableFailure: true),
            new TenantCommandStatusResult(CommandStatus.Completed, EventCount: 1, IsPending: true, HasVerifiedCommandIdentity: true),
            new TenantCommandStatusResult(CommandStatus.Rejected, IsRetryableFailure: true, HasVerifiedCommandIdentity: true)
        })
        {
            View(Apply(Start(flow), status)).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        }
    }

    [Theory]
    [MemberData(nameof(Flows))]
    public void MatchingPayloadWithNoncurrentOrMissingMetadataCannotConfirm(string flow)
    {
        object pending = Apply(Start(flow), Completed());
        TenantDetail detail = Detail(flow);
        foreach (TenantDetailSnapshot proof in new[]
        {
            TenantDetailSnapshot.Ready(detail, "etag", ReadModelFreshnessState.Stale, ProjectionLifecycleState.Current, "tenant-sequence:2"),
            TenantDetailSnapshot.Ready(detail, "etag", ReadModelFreshnessState.Unknown, ProjectionLifecycleState.Current, "tenant-sequence:2"),
            TenantDetailSnapshot.Ready(detail, "etag", ReadModelFreshnessState.Current, ProjectionLifecycleState.Rebuilding, "tenant-sequence:2"),
            TenantDetailSnapshot.Ready(detail, "etag", ReadModelFreshnessState.Current, ProjectionLifecycleState.Unknown, "tenant-sequence:2"),
            TenantDetailSnapshot.Ready(detail, "etag", ReadModelFreshnessState.Current, ProjectionLifecycleState.Current, null),
            TenantDetailSnapshot.Degraded(detail, "Failed refresh", "etag", ProjectionLifecycleState.Current, "tenant-sequence:2")
        })
        {
            View(Confirm(pending, proof)).State.ShouldNotBe(TenantCommandLifecycleState.Confirmed);
        }
    }

    [Theory]
    [MemberData(nameof(Flows))]
    public void ExactVerifiedAttemptWithAuthoritativeProofConfirms(string flow)
    {
        object result = Confirm(Apply(Start(flow), Completed()), Proof(Detail(flow)));
        View(result).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        View(result).Audit.ShouldNotBe(TenantCommandAuditState.AuditAvailable);
    }

    [Theory]
    [InlineData(null, "tenant-sequence:2")]
    [InlineData(3L, "tenant-sequence:2")]
    [InlineData(1L, "tenant-sequence:2")]
    [InlineData(2L, "opaque-2")]
    public void RemovalRequiresDetailToReachTheExactCommittedSequence(long? committed, string version)
    {
        TenantCommandStatusResult status = Completed() with { CommittedEventSequence = committed };
        object result = Confirm(Apply(Start("remove"), status), Proof(Detail("remove"), version));
        View(result).State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    [Fact]
    public void CachedListWithMissingLifecycleCannotProveCreate()
    {
        TenantCreateCommandSnapshot pending = (TenantCreateCommandSnapshot)Apply(Start("create"), Completed());
        TenantListSnapshot current = ListProof(new TenantSummary("tenant.alpha", "New", TenantStatus.Active), "tenant-sequence:2")!;
        pending.ConfirmProjection(current with { Lifecycle = ProjectionLifecycleState.Unknown }, null).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(current with { Freshness = ReadModelFreshnessState.Stale }, null).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(current with { ProjectionVersion = null }, null).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(current with { Rows = [current.Rows[0] with { Freshness = ReadModelFreshnessState.Stale }] }, null).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(current with { Rows = [current.Rows[0] with { Lifecycle = ProjectionLifecycleState.Unknown }] }, null).State
            .ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(current, null).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
    }

    [Fact]
    public void FirstTenantWithoutBaselineStillRequiresQualifiedFirstAppearance()
    {
        TenantCreateCommandSnapshot pending = TenantCreateCommandSnapshot.Idle()
            .RequestSent(new CreateTenant("tenant.alpha", "New", null), null, baselineTenantAbsent: true)
            .Accepted(TenantCommandSubmissionResult.Accepted("message-1", "correlation-1"))
            .ApplyStatus(Completed());
        pending.ConfirmProjection(null, Proof(Detail("create"))).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        pending.ConfirmProjection(null, Proof(Detail("create"), "tenant-sequence:bad")).State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
        pending.ConfirmProjection(null, TenantDetailSnapshot.Ready(Detail("create"), null, ReadModelFreshnessState.Current))
            .State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
    }

    private static TenantCommandStatusResult Completed()
        => new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true) { CommittedEventSequence = 2 };

    private static object Start(string flow)
    {
        TenantCommandSubmissionResult accepted = TenantCommandSubmissionResult.Accepted("message-1", "correlation-1");
        return flow switch
        {
            "create" => TenantCreateCommandSnapshot.Idle().RequestSent(new CreateTenant("tenant.alpha", "New", null), "tenant-sequence:1", true).Accepted(accepted),
            "add" => TenantAddMemberCommandSnapshot.Idle().RequestSent(new AddUserToTenant("tenant.alpha", "reader", TenantRole.TenantReader), "tenant-sequence:1").Accepted(accepted),
            "role" => TenantChangeRoleCommandSnapshot.Idle().RequestSent(new ChangeUserRole("tenant.alpha", "reader", TenantRole.TenantReader), TenantRole.TenantContributor, 1, "tenant-sequence:1").Accepted(accepted),
            "remove" => TenantRemoveMemberCommandSnapshot.Idle().Previewed(new RemoveUserFromTenant("tenant.alpha", "reader"), TenantRole.TenantReader, 1, false, Detail("add")).RequestSent("tenant-sequence:1").Accepted(accepted),
            _ => TenantUpdateMetadataCommandSnapshot.Idle("Old").RequestSent(new UpdateTenant("tenant.alpha", "New", null), "tenant-sequence:1").Accepted(accepted),
        };
    }

    private static object Apply(object snapshot, TenantCommandStatusResult status)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => s.ApplyStatus(status),
            TenantAddMemberCommandSnapshot s => s.ApplyStatus(status),
            TenantChangeRoleCommandSnapshot s => s.ApplyStatus(status),
            TenantRemoveMemberCommandSnapshot s => s.ApplyStatus(status),
            TenantUpdateMetadataCommandSnapshot s => s.ApplyStatus(status),
            _ => throw new InvalidOperationException(),
        };

    private static object Confirm(object snapshot, TenantDetailSnapshot? proof)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => s.ConfirmProjection(null, proof),
            TenantAddMemberCommandSnapshot s => s.ConfirmProjection(proof),
            TenantChangeRoleCommandSnapshot s => s.ConfirmProjection(proof),
            TenantRemoveMemberCommandSnapshot s => s.ConfirmProjection(proof),
            TenantUpdateMetadataCommandSnapshot s => s.ConfirmProjection(proof),
            _ => throw new InvalidOperationException(),
        };

    private static (TenantCommandLifecycleState State, TenantCommandAuditState Audit, bool Events, string? Message, string? Correlation) View(object snapshot)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => (s.State, s.AuditState, s.HasCommandEventEvidence, s.MessageId, s.CorrelationId),
            TenantAddMemberCommandSnapshot s => (s.State, s.AuditState, s.HasCommandEventEvidence, s.MessageId, s.CorrelationId),
            TenantChangeRoleCommandSnapshot s => (s.State, s.AuditState, s.HasCommandEventEvidence, s.MessageId, s.CorrelationId),
            TenantRemoveMemberCommandSnapshot s => (s.State, s.AuditState, s.HasCommandEventEvidence, s.MessageId, s.CorrelationId),
            TenantUpdateMetadataCommandSnapshot s => (s.State, s.AuditState, s.HasCommandEventEvidence, s.MessageId, s.CorrelationId),
            _ => throw new InvalidOperationException(),
        };

    private static TenantDetail Detail(string flow)
        => new("tenant.alpha", "New", null, TenantStatus.Active,
            flow is "add" or "role" ? [new TenantMember("reader", TenantRole.TenantReader)] : [],
            new Dictionary<string, string>(), DateTimeOffset.UtcNow);
}
