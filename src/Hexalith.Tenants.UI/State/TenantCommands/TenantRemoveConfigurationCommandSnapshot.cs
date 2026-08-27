using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>Reduces support-safe evidence for one tracked remove-configuration attempt.</summary>
public sealed record TenantRemoveConfigurationCommandSnapshot(
    TenantCommandLifecycleState State,
    TenantRemoveConfigurationIntent? Intent = null,
    TenantRemoveConfigurationPreview? Preview = null,
    TenantConfigurationProjectionProof? LastConfigurationProof = null,
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
    public static TenantRemoveConfigurationCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    /// <summary>Creates a fail-closed state before any dispatch identity exists.</summary>
    public static TenantRemoveConfigurationCommandSnapshot Blocked(
        string safeMessage,
        TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    /// <summary>Captures one complete immutable BFF preview.</summary>
    public TenantRemoveConfigurationCommandSnapshot Previewed(TenantRemoveConfigurationPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = preview.Intent,
            Preview = preview,
            LastConfigurationProof = null,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>Records one logical dispatch before transport is attempted.</summary>
    public TenantRemoveConfigurationCommandSnapshot RequestSent(
        TenantRemoveConfigurationPreview preview,
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
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
        };
    }

    /// <summary>Applies an accepted result only when it matches the retained message identity.</summary>
    public TenantRemoveConfigurationCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (string.IsNullOrWhiteSpace(MessageId)
            || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(result.CorrelationId))
        {
            return UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.TrackingMismatch");
        }

        return this with
        {
            State = TenantCommandLifecycleState.Accepted,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>Retains ownership when transport could have delivered the command.</summary>
    public TenantRemoveConfigurationCommandSnapshot AmbiguousSubmission(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            CorrelationId = string.IsNullOrWhiteSpace(CorrelationId) ? MessageId : CorrelationId,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    /// <summary>Merges aggregate-aware status evidence without collapsing lifecycle states.</summary>
    public TenantRemoveConfigurationCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
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
                    SafeMessageKey = "Tenants.Configuration.Remove.Status.Pending",
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                }
                : UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.Status") with
                {
                    StatusObservationCount = StatusObservationCount + 1,
                };
        }

        if (!status.HasVerifiedCommandIdentity)
        {
            return UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.TrackingMismatch") with
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
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.EventsStored or CommandStatus.EventsPublished => this with
            {
                State = TenantCommandLifecycleState.ProjectionPending,
                HasCommandEventEvidence = true,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditPending,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.Completed when status.EventCount is not > 0 && !HasCommandEventEvidence
                => UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.MissingEventEvidence") with
                {
                    StatusObservationCount = StatusObservationCount + 1,
                },
            CommandStatus.Completed => this with
            {
                State = TenantCommandLifecycleState.ProjectionPending,
                HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditPending,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            },
            CommandStatus.Rejected when HasCommandEventEvidence
                || State is TenantCommandLifecycleState.Degraded => this with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
            CommandStatus.Rejected => this with
            {
                State = TenantCommandLifecycleState.Rejected,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Configuration.Remove.Status.Rejected",
                RejectionCode = status.RejectionCode,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            CommandStatus.PublishFailed => this with
            {
                State = TenantCommandLifecycleState.Degraded,
                HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0,
                StatusObservationCount = StatusObservationCount + 1,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Configuration.Remove.Status.PublishFailed",
                AuditState = TenantCommandAuditState.AuditDelayed,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
            CommandStatus.TimedOut when HasCommandEventEvidence
                || State is TenantCommandLifecycleState.Degraded => this with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
            CommandStatus.TimedOut => UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.StatusTimeout") with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
            _ => UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.Status") with
            {
                StatusObservationCount = StatusObservationCount + 1,
            },
        };
    }

    /// <summary>Records a SignalR nudge without changing command or projection truth.</summary>
    public TenantRemoveConfigurationCommandSnapshot SignalRNudge()
        => this with { FocusTarget = TenantCommandFocusTarget.Refresh };

    /// <summary>Preserves command/event truth when projection verification itself is unavailable.</summary>
    public TenantRemoveConfigurationCommandSnapshot ProjectionVerificationFailed(string safeMessageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessageKey);
        return this with
        {
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
    }

    /// <summary>Confirms only from exact absence, event evidence, and an advanced ordered version.</summary>
    public TenantRemoveConfigurationCommandSnapshot ConfirmProjection(TenantConfigurationProjectionProof? proof)
    {
        if (Intent is null || proof is null || !proof.Matches(Intent))
        {
            // A proof bound to another tenant or another exact key is not this attempt's evidence, so it is
            // discarded rather than retained. Keeping it left the attempt tracker's Merge ordering reading a
            // foreign ProjectionVersion when it chose between a retained and an incoming snapshot.
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (proof.Kind is not TenantConfigurationProjectionProofKind.RemoveConfirmed)
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
                AuditState = TenantCommandAuditState.AuditPending,
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
    public TenantRemoveConfigurationCommandSnapshot Abandon()
        => UnableToVerify("Tenants.Configuration.Remove.UnableToVerify.Abandoned") with
        {
            MessageId = null,
            CorrelationId = null,
            AttemptStartedAtUtc = null,
        };

    /// <summary>Returns a support-safe diagnostic shape without tracking or projection identifiers.</summary>
    /// <returns>A fixed description containing lifecycle classifications and Boolean evidence only.</returns>
    public override string ToString()
        => $"{nameof(TenantRemoveConfigurationCommandSnapshot)} {{ State = {State}, HasIntent = {Intent is not null}, HasPreview = {Preview is not null}, HasTracking = {!string.IsNullOrWhiteSpace(MessageId)}, HasCommandEventEvidence = {HasCommandEventEvidence}, AuditState = {AuditState} }}";

    private TenantRemoveConfigurationCommandSnapshot UnableToVerify(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
}
