using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;
using Hexalith.Tenants.UI.State.TenantDetail;

using Shouldly;

using static Hexalith.Tenants.UI.Tests.TenantCommandProofFixtures;

namespace Hexalith.Tenants.UI.Tests.State;

/// <summary>Preserves command, projection and audit truth across temporary status-store lookups.</summary>
public sealed class TenantCommandTemporaryLookupTests
{
    private const string MessageId = "01JARYZ6S41R0CKFWRD8C5MP47";
    private const string CorrelationId = "01JARYZ6S41R0CKFWRD8C5MP48";
    private const string BaselineVersion = "tenant-sequence:1";

    /// <summary>Gets each independently implemented command snapshot affected by temporary lookups.</summary>
    public static TheoryData<string> Flows => new() { "create", "add", "role", "remove", "metadata" };

    /// <summary>Temporary feedback can change without discarding any other field of the retained attempt.</summary>
    /// <param name="flow">The command snapshot under test.</param>
    [Theory]
    [MemberData(nameof(Flows))]
    public void TemporaryFeedbackPreservesEntireAcceptedAndProjectionPendingAttempt(string flow)
    {
        object accepted = Start(flow);
        object stored = Apply(accepted, Completed());
        foreach (object original in new[] { accepted, stored })
        {
            foreach (TenantCommandStatusResult lookup in TemporaryLookups())
            {
                object result = Apply(original, lookup);
                TenantCommandAuditState expectedAudit = lookup.IsRetryableFailure
                    ? TenantCommandAuditState.AuditUnavailable
                    : View(original).Audit;

                // Whole-record equality also pins intent, baseline, timestamps, confirmed values and
                // removal commit proof, rather than checking only the shared identity fields.
                result.ShouldBe(WithLookupFeedback(original, lookup, expectedAudit));
                View(result).State.ShouldBe(View(original).State);
            }
        }
    }

    /// <summary>A temporary lookup grants no success but permits later verified status and projection proof.</summary>
    /// <param name="flow">The command snapshot under test.</param>
    [Theory]
    [MemberData(nameof(Flows))]
    public void TemporaryLookupNeedsVerifiedStoredEventsAndAuthoritativeProjectionBeforeConfirmation(string flow)
    {
        foreach (TenantCommandStatusResult lookup in TemporaryLookups())
        {
            object accepted = Apply(Start(flow), lookup);
            View(Confirm(accepted, Proof(Detail(flow)))).State.ShouldBe(TenantCommandLifecycleState.Accepted);

            object stored = Apply(accepted, Completed());
            View(stored).State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);
            object waiting = Apply(stored, lookup);
            View(Confirm(waiting, null)).State.ShouldBe(TenantCommandLifecycleState.ProjectionPending);

            object confirmed = Confirm(waiting, Proof(Detail(flow)));
            View(confirmed).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
            View(confirmed).Audit.ShouldNotBe(TenantCommandAuditState.AuditAvailable);
        }
    }

    /// <summary>Unknown, contradictory and unverified responses remain distinct from propagation waits.</summary>
    /// <param name="flow">The command snapshot under test.</param>
    [Theory]
    [MemberData(nameof(Flows))]
    public void UnknownAndContradictoryLookupsCannotInheritThePendingSuccessPath(string flow)
    {
        TenantCommandStatusResult[] invalid =
        [
            TenantCommandStatusResult.UnknownWithKey("Tenants.Commands.Status.Unverified"),
            new(null, IsPending: true, IsRetryableFailure: true),
            Completed() with { IsPending = true },
            new(CommandStatus.Rejected, IsRetryableFailure: true, HasVerifiedCommandIdentity: true),
            Completed() with { HasVerifiedCommandIdentity = false },
        ];
        object waiting = Apply(Start(flow), TenantCommandStatusResult.Pending("Waiting"));
        foreach (TenantCommandStatusResult lookup in invalid)
        {
            object uncertain = Apply(waiting, lookup);

            View(uncertain).ShouldBe((TenantCommandLifecycleState.UnableToVerify,
                TenantCommandAuditState.AuditUnavailable, TenantCommandLiveRegionPoliteness.Assertive));
            View(Confirm(uncertain, Proof(Detail(flow)))).State.ShouldBe(TenantCommandLifecycleState.UnableToVerify);
        }
    }

    /// <summary>Late temporary lookups cannot replace an established success or rejection.</summary>
    /// <param name="flow">The command snapshot under test.</param>
    [Theory]
    [MemberData(nameof(Flows))]
    public void TerminalEvidenceCannotRegressToWaiting(string flow)
    {
        object accepted = Start(flow);
        object confirmed = Confirm(Apply(accepted, Completed()), Proof(Detail(flow)));
        object rejected = Apply(accepted, new(CommandStatus.Rejected,
            SafeMessage: "Rejected", HasVerifiedCommandIdentity: true));
        View(confirmed).State.ShouldBe(TenantCommandLifecycleState.Confirmed);
        View(rejected).State.ShouldBe(TenantCommandLifecycleState.Rejected);

        foreach (object terminal in new[] { confirmed, rejected })
        {
            foreach (TenantCommandStatusResult lookup in TemporaryLookups())
            {
                Apply(terminal, lookup).ShouldBeSameAs(terminal);
            }
        }
    }

    private static TenantCommandStatusResult[] TemporaryLookups()
        => [
            TenantCommandStatusResult.Pending("The command status is still pending."),
            TenantCommandStatusResult.RetryableFailure("The status lookup can be retried."),
            TenantCommandStatusResult.PendingWithKey("Tenants.Commands.Status.Pending"),
            TenantCommandStatusResult.RetryableFailureWithKey("Tenants.Commands.Status.Unavailable"),
        ];

    private static TenantCommandStatusResult Completed()
        => new(CommandStatus.Completed, EventCount: 1, HasVerifiedCommandIdentity: true)
        { CommittedEventSequence = 2 };

    private static object Start(string flow)
    {
        TenantCommandSubmissionResult accepted = TenantCommandSubmissionResult.Accepted(MessageId, CorrelationId);
        return flow switch
        {
            "create" => TenantCreateCommandSnapshot.Idle()
                .RequestSent(new CreateTenant("Tenant.Mixed", "Updated", null), BaselineVersion, true,
                    baselineDetailProjectionVersion: BaselineVersion, attemptStartedAtUtc: DateTimeOffset.UnixEpoch)
                .Accepted(accepted),
            "add" => TenantAddMemberCommandSnapshot.Idle()
                .RequestSent(new AddUserToTenant("Tenant.Mixed", "User.Mixed", TenantRole.TenantReader), BaselineVersion)
                .Accepted(accepted),
            "role" => TenantChangeRoleCommandSnapshot.Idle()
                .RequestSent(new ChangeUserRole("Tenant.Mixed", "User.Mixed", TenantRole.TenantReader),
                    TenantRole.TenantContributor, 2, BaselineVersion)
                .Accepted(accepted),
            "remove" => TenantRemoveMemberCommandSnapshot.Idle()
                .Previewed(new RemoveUserFromTenant("Tenant.Mixed", "User.Mixed"),
                    TenantRole.TenantReader, 2, true, Detail("add"))
                .RequestSent(BaselineVersion, attemptStartedAtUtc: DateTimeOffset.UnixEpoch)
                .Accepted(accepted),
            "metadata" => TenantUpdateMetadataCommandSnapshot.Idle("Original", "Original description")
                .RequestSent(new UpdateTenant("Tenant.Mixed", "Updated", null), BaselineVersion, DateTimeOffset.UnixEpoch)
                .Accepted(accepted),
            _ => throw new ArgumentOutOfRangeException(nameof(flow)),
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
            _ => throw new ArgumentException("Expected a tenant command snapshot.", nameof(snapshot)),
        };

    private static object Confirm(object snapshot, TenantDetailSnapshot? proof)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => s.ConfirmProjection(null, proof),
            TenantAddMemberCommandSnapshot s => s.ConfirmProjection(proof),
            TenantChangeRoleCommandSnapshot s => s.ConfirmProjection(proof),
            TenantRemoveMemberCommandSnapshot s => s.ConfirmProjection(proof),
            TenantUpdateMetadataCommandSnapshot s => s.ConfirmProjection(proof),
            _ => throw new ArgumentException("Expected a tenant command snapshot.", nameof(snapshot)),
        };

    private static (TenantCommandLifecycleState State, TenantCommandAuditState Audit,
        TenantCommandLiveRegionPoliteness Politeness) View(object snapshot)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => (s.State, s.AuditState, s.LiveRegionPoliteness),
            TenantAddMemberCommandSnapshot s => (s.State, s.AuditState, s.LiveRegionPoliteness),
            TenantChangeRoleCommandSnapshot s => (s.State, s.AuditState, s.LiveRegionPoliteness),
            TenantRemoveMemberCommandSnapshot s => (s.State, s.AuditState, s.LiveRegionPoliteness),
            TenantUpdateMetadataCommandSnapshot s => (s.State, s.AuditState, s.LiveRegionPoliteness),
            _ => throw new ArgumentException("Expected a tenant command snapshot.", nameof(snapshot)),
        };

    private static object WithLookupFeedback(object snapshot, TenantCommandStatusResult status,
        TenantCommandAuditState audit)
        => snapshot switch
        {
            TenantCreateCommandSnapshot s => s with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = audit,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            TenantAddMemberCommandSnapshot s => s with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = audit,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            TenantChangeRoleCommandSnapshot s => s with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = audit,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            TenantRemoveMemberCommandSnapshot s => s with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = audit,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            TenantUpdateMetadataCommandSnapshot s => s with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = audit,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            _ => throw new ArgumentException("Expected a tenant command snapshot.", nameof(snapshot)),
        };

    private static TenantDetail Detail(string flow)
        => new("Tenant.Mixed", "Updated", null, TenantStatus.Active,
            flow is "add" or "role" ? [new TenantMember("User.Mixed", TenantRole.TenantReader)] : [],
            new Dictionary<string, string>(), DateTimeOffset.UnixEpoch);
}
