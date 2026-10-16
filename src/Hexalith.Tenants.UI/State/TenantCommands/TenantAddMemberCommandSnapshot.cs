using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record TenantAddMemberCommandSnapshot(
    TenantCommandLifecycleState State,
    AddUserToTenant? Intent = null,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    bool BaselinePostconditionMet = false,
    bool HasCommandEventEvidence = false,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    /// <summary>Gets whether the retained membership projection wait has expired without terminal evidence.</summary>
    public bool ProjectionWaitExpired { get; init; }

    public static TenantAddMemberCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantAddMemberCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditStates.NotStarted,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantAddMemberCommandSnapshot RequestSent(
        AddUserToTenant intent,
        string? baselineProjectionVersion = null,
        bool baselinePostconditionMet = false)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselinePostconditionMet = baselinePostconditionMet,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantAddMemberCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantAddMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (State is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied or TenantCommandLifecycleState.Failed)
        {
            return this;
        }


        if ((status.IsPending && status.IsRetryableFailure)
            || (status.Status is not null && (status.IsPending || status.IsRetryableFailure))
            || (status.Status is not null && !status.HasVerifiedCommandIdentity))
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                RejectionCode = null,
                AuditState = TenantCommandAuditStates.Unverifiable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        if (status.IsPending || status.IsRetryableFailure)
        {
            // A temporary status nudge does not revoke previously qualified command-event evidence.
            // Keep the expiry uncertainty eligible for the same projection proof as before expiry.
            bool expiredWait = State is TenantCommandLifecycleState.UnableToVerify && ProjectionWaitExpired
                && SafeMessageKey == TenantMembershipProjectionWaitPolicy.ExpiredMessageKey;
            return this with
            {
                SafeMessage = expiredWait ? null : status.SafeMessage,
                SafeMessageKey = expiredWait ? TenantMembershipProjectionWaitPolicy.ExpiredMessageKey : status.SafeMessageKey,
                AuditState = TenantCommandAuditStates.FromStatusLookup(status, AuditState),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                // A 404 before the first status is a wait that keeps the audit state; any other missing status is unverifiable.
                AuditState = TenantCommandAuditStates.FromStatusLookup(status, AuditState),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = HasCommandEventEvidence ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = status.SafeMessageKey ?? "Tenants.Commands.Status.Unverified",
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantAddMemberCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantAddMemberCommandSnapshot ConfirmProjection(
        TenantDetailSnapshot? proof) {
        if (Intent is null) {
            return this;
        }

        // Only status-driven ProjectionPending (Completed/Events*) may confirm; Accepted alone never does.
        if (State is not TenantCommandLifecycleState.ProjectionPending
            && !(State is TenantCommandLifecycleState.UnableToVerify && ProjectionWaitExpired
                && SafeMessageKey == TenantMembershipProjectionWaitPolicy.ExpiredMessageKey)) {
            return this;
        }

        if (proof is null || !TenantMembershipCommandProvenance.IsAuthoritative(proof))
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantDetailProjection? detailEvidence = proof!.Detail;
        string? currentProjectionVersion = proof.ProjectionVersion;
        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool memberMatches = tenantMatches
            && detailEvidence!.Members.Any(member =>
                string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal)
                && member.Role == Intent.Role);

        if (!memberMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (BaselinePostconditionMet || string.IsNullOrWhiteSpace(BaselineProjectionVersion)) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                SafeMessageKey = "Tenants.AddMember.Confirm.UnableToVerify.MissingProvenance",
                // The projection was read but did not prove this attempt, and no audit read happened: AuditState is kept.
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        if (!TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                BaselineProjectionVersion,
                currentProjectionVersion,
                HasCommandEventEvidence)) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedMemberProjection = detailEvidence,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            // A projection confirmation carries no attempt-specific audit proof, and this flow has no in-panel
            // audit verification: report proof as unsupported rather than pending forever.
            AuditState = TenantCommandAuditStates.ConfirmedWithoutProof,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}
