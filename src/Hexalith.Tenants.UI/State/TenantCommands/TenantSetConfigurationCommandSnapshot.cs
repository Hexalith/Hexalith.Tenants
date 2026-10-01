using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Reduces support-safe evidence for one tracked set-configuration attempt.</summary>
public sealed record TenantSetConfigurationCommandSnapshot(
    TenantCommandLifecycleState State,
    TenantSetConfigurationIntent? Intent = null,
    TenantSetConfigurationPreview? Preview = null,
    TenantConfigurationProjectionProof? LastConfigurationProof = null,
    bool CompletedWithoutEvents = false,
    bool HasCommandEventEvidence = false,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    DateTimeOffset? AttemptStartedAtUtc = null,
    int PendingStatusPollCount = 0,
    int StatusObservationCount = 0,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
    /// <summary>Maximum circuit-local ownership window for an ambiguous attempt.</summary>
    internal static readonly TimeSpan MaximumRetainedAttemptDuration = TimeSpan.FromMinutes(5);

    /// <summary>Gets whether this attempt still owns the aggregate and may only be reconciled.</summary>
    public bool RetainsAttempt
        => Intent is not null
            && !string.IsNullOrWhiteSpace(MessageId)
            && AttemptStartedAtUtc is not null
            && State is TenantCommandLifecycleState.RequestSent
                or TenantCommandLifecycleState.Accepted
                or TenantCommandLifecycleState.ProjectionPending
                or TenantCommandLifecycleState.Degraded
                or TenantCommandLifecycleState.UnableToVerify;

    /// <summary>Creates the idle reducer state.</summary>
    public static TenantSetConfigurationCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    /// <summary>Creates a fail-closed state before any dispatch identity exists.</summary>
    public static TenantSetConfigurationCommandSnapshot Blocked(
        string safeMessage,
        TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditStates.NotStarted,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    /// <summary>Captures one complete immutable BFF preview.</summary>
    public TenantSetConfigurationCommandSnapshot Previewed(TenantSetConfigurationPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = preview.Intent,
            Preview = preview,
            LastConfigurationProof = null,
            CompletedWithoutEvents = false,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>Records an exact authoritative match without dispatching.</summary>
    public TenantSetConfigurationCommandSnapshot AlreadyApplied(
        TenantSetConfigurationPreview preview,
        string safeMessage)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return Previewed(preview) with
        {
            State = TenantCommandLifecycleState.AlreadyApplied,
            CompletedWithoutEvents = true,
            SafeMessage = safeMessage,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
        };
    }

    /// <summary>Records one logical dispatch before transport is attempted.</summary>
    public TenantSetConfigurationCommandSnapshot RequestSent(
        TenantSetConfigurationPreview preview,
        string messageId,
        DateTimeOffset attemptStartedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(preview.ProjectionVersion);
        return Previewed(preview) with
        {
            State = TenantCommandLifecycleState.RequestSent,
            MessageId = messageId,
            BaselineProjectionVersion = preview.ProjectionVersion,
            AttemptStartedAtUtc = attemptStartedAtUtc.ToUniversalTime(),
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
        };
    }

    /// <summary>Applies an accepted result only when it matches the retained message identity.</summary>
    public TenantSetConfigurationCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(MessageId)
            || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(result.CorrelationId))
        {
            return UnableToVerify("Tenants.Configuration.Set.UnableToVerify.TrackingMismatch");
        }

        return this with
        {
            State = TenantCommandLifecycleState.Accepted,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>Retains ownership when transport could have delivered the command.</summary>
    public TenantSetConfigurationCommandSnapshot AmbiguousSubmission(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            CorrelationId = string.IsNullOrWhiteSpace(CorrelationId) ? MessageId : CorrelationId,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            // Dispatch was attempted but its delivery cannot be proven, so the status is unknown.
            AuditState = TenantCommandAuditStates.Unverifiable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    /// <summary>Merges aggregate-aware status evidence without collapsing lifecycle states.</summary>
    public TenantSetConfigurationCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Status is null)
        {
            return status.IsPending || status.IsRetryableFailure
                ? this with
                {
                    PendingStatusPollCount = PendingStatusPollCount + 1,
                    StatusObservationCount = StatusObservationCount + 1,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Configuration.Set.Status.Pending",
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                }
                : UnableToVerify("Tenants.Configuration.Set.UnableToVerify.Status") with
                {
                    StatusObservationCount = StatusObservationCount + 1,
                };
        }

        if (!status.HasVerifiedCommandIdentity)
        {
            return UnableToVerify("Tenants.Configuration.Set.UnableToVerify.TrackingMismatch") with
            {
                StatusObservationCount = StatusObservationCount + 1,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                when State is TenantCommandLifecycleState.ProjectionPending
                    or TenantCommandLifecycleState.Degraded => this with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
            CommandStatus.Received or CommandStatus.Processing => this with
            {
                State = TenantCommandLifecycleState.Accepted,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.EventsStored or CommandStatus.EventsPublished => this with
            {
                State = TenantCommandLifecycleState.ProjectionPending,
                CompletedWithoutEvents = false,
                HasCommandEventEvidence = true,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.Completed when status.EventCount is null or < 0
                => UnableToVerify("Tenants.Configuration.Set.UnableToVerify.Status") with
                {
                    StatusObservationCount = StatusObservationCount + 1,
                },
            // A zero-event completion after stored events is a stale status: it cannot unstore them, so the
            // earlier event evidence stays and the attempt is never marked completed without events.
            CommandStatus.Completed => this with
            {
                State = TenantCommandLifecycleState.ProjectionPending,
                CompletedWithoutEvents = !HasCommandEventEvidence && status.EventCount == 0,
                HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount > 0,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.Rejected => this with
            {
                State = TenantCommandLifecycleState.Rejected,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Configuration.Set.Status.Rejected",
                RejectionCode = status.RejectionCode,
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            CommandStatus.PublishFailed => this with
            {
                State = TenantCommandLifecycleState.Degraded,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Configuration.Set.Status.PublishFailed",
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            CommandStatus.TimedOut => UnableToVerify("Tenants.Configuration.Set.UnableToVerify.StatusTimeout") with
            {
                StatusObservationCount = StatusObservationCount + 1,
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
            },
            _ => UnableToVerify("Tenants.Configuration.Set.UnableToVerify.Status") with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
        };
    }

    /// <summary>Records a SignalR nudge without changing command or projection truth.</summary>
    public TenantSetConfigurationCommandSnapshot SignalRNudge()
        => this with { FocusTarget = TenantCommandFocusTarget.Refresh };

    /// <summary>Confirms only from an exact value match and qualifying causal provenance.</summary>
    public TenantSetConfigurationCommandSnapshot ConfirmProjection(TenantConfigurationProjectionProof? proof)
    {
        if (Intent is null
            || proof is null
            || !proof.Matches(Intent)
            || proof.Kind is not TenantConfigurationProjectionProofKind.SetConfirmed)
        {
            return this with
            {
                LastConfigurationProof = proof,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        if (State is not TenantCommandLifecycleState.Accepted
            and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this with { LastConfigurationProof = proof };
        }

        if (CompletedWithoutEvents)
        {
            TenantLifecycleSequenceRelation baselineRelation = TenantLifecycleProjectionVersion.CompareSequences(
                proof.ProjectionVersion,
                BaselineProjectionVersion);
            if (HasCommandEventEvidence
                || baselineRelation is not TenantLifecycleSequenceRelation.Equal
                    and not TenantLifecycleSequenceRelation.IncomingNewer)
            {
                return this with
                {
                    LastConfigurationProof = proof,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                };
            }

            return this with
            {
                State = TenantCommandLifecycleState.AlreadyApplied,
                LastConfigurationProof = proof,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Configuration.Set.AlreadyApplied.NoOp",
                AuditState = TenantCommandAuditStates.NotStarted,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        bool advanced = HasCommandEventEvidence
            && TenantLifecycleProjectionVersion.Compare(BaselineProjectionVersion, proof.ProjectionVersion)
                is TenantLifecycleProjectionVersionComparison.Advanced;
        return advanced
            ? this with
            {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfigurationProof = proof,
                SafeMessage = null,
                SafeMessageKey = null,
                RejectionCode = null,
                // A projection confirmation carries no attempt-specific audit proof, and this flow has no
                // in-panel audit verification: report proof as unsupported rather than pending forever.
                AuditState = TenantCommandAuditStates.ConfirmedWithoutProof,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            }
            : this with
            {
                State = TenantCommandLifecycleState.ProjectionPending,
                LastConfigurationProof = proof,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
    }

    /// <summary>Gets whether the bounded attempt ownership window elapsed.</summary>
    public bool IsRetentionExpired(DateTimeOffset observedAtUtc)
        => AttemptStartedAtUtc is { } started
            && (observedAtUtc.ToUniversalTime() < started.ToUniversalTime()
                || observedAtUtc.ToUniversalTime() - started.ToUniversalTime()
                    >= MaximumRetainedAttemptDuration);

    /// <summary>Stops local reconciliation and releases aggregate ownership.</summary>
    public TenantSetConfigurationCommandSnapshot Abandon()
        => UnableToVerify("Tenants.Configuration.Set.UnableToVerify.Abandoned") with
        {
            MessageId = null,
            CorrelationId = null,
            AttemptStartedAtUtc = null,
        };

    /// <summary>
    /// Releases an attempt that outlived its retention window. The command stays unverified exactly as an
    /// abandonment, but the audit dimension is delayed rather than unavailable.
    /// </summary>
    /// <returns>The released snapshot with a delayed audit dimension.</returns>
    public TenantSetConfigurationCommandSnapshot ExpireRetention()
        => Abandon() with { AuditState = TenantCommandAuditStates.Delayed };

    /// <summary>Returns a support-safe diagnostic shape without tracking or projection identifiers.</summary>
    /// <returns>A fixed description containing lifecycle classifications and Boolean evidence only.</returns>
    public override string ToString()
        => $"{nameof(TenantSetConfigurationCommandSnapshot)} {{ State = {State}, HasIntent = {Intent is not null}, HasPreview = {Preview is not null}, HasTracking = {!string.IsNullOrWhiteSpace(MessageId)}, HasCommandEventEvidence = {HasCommandEventEvidence}, CompletedWithoutEvents = {CompletedWithoutEvents}, AuditState = {AuditState} }}";

    private TenantSetConfigurationCommandSnapshot UnableToVerify(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            AuditState = TenantCommandAuditStates.Unverifiable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
}
