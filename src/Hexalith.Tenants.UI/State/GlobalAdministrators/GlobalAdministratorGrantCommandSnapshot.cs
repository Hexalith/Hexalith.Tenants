using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorGrantCommandSnapshot(
    TenantCommandLifecycleState State,
    SetGlobalAdministrator? Intent = null,
    GlobalAdministratorRow? LastConfirmedProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
    GlobalAdministratorGrantPreview? PreviewEvidence = null,
    bool HasCommandEventEvidence = false,
    bool IsSubmissionAmbiguous = false,
    string? SafeMessageKey = null,
    string? SafeRecoveryKey = null)
{
    /// <summary>Gets the complete projection version captured before dispatch.</summary>
    public string? BaselineProjectionVersion => PreviewEvidence?.ProjectionVersion;

    /// <summary>Gets the complete fixed-scope count captured before dispatch.</summary>
    public int? BaselineAdministratorCount => PreviewEvidence?.CurrentAdministratorCount;

    /// <summary>Gets whether this attempt owns a complete preview and a valid caller-owned message id.</summary>
    public bool HasTrackedPreview
        => PreviewEvidence?.IsComplete == true && IsValidMessageId(MessageId);

    /// <summary>Returns a support-safe description that omits identities and tracking values.</summary>
    /// <returns>A bounded support-safe command-snapshot description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorGrantCommandSnapshot)} {{ State = {State}, HasIntent = {Intent is not null}, HasTrackedPreview = {HasTrackedPreview}, HasCommandEventEvidence = {HasCommandEventEvidence}, IsSubmissionAmbiguous = {IsSubmissionAmbiguous}, AuditState = {AuditState}, RejectionCode = {RejectionCode}, FocusTarget = {FocusTarget}, LiveRegionPoliteness = {LiveRegionPoliteness} }}";

    public static GlobalAdministratorGrantCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static GlobalAdministratorGrantCommandSnapshot Blocked(
        string safeMessage,
        TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    /// <summary>Surfaces a BFF-composed unavailable preview and its associated recovery.</summary>
    /// <param name="preview">Fail-closed preview evidence.</param>
    /// <returns>A support-safe state that retains no command identity.</returns>
    public static GlobalAdministratorGrantCommandSnapshot UnavailablePreview(
        GlobalAdministratorGrantPreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new(
            TenantCommandLifecycleState.UnableToVerify,
            Intent: new SetGlobalAdministrator(preview.TargetUserId),
            PreviewEvidence: preview,
            SafeMessageKey: preview.UnavailableReasonKey
                ?? "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Evidence",
            SafeRecoveryKey: preview.RecoveryKey
                ?? "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);
    }

    /// <summary>Captures one complete preview and caller-owned ULID before any dispatch occurs.</summary>
    /// <param name="preview">BFF-composed preview facts.</param>
    /// <param name="messageId">Exact caller-owned ULID.</param>
    /// <returns>The previewed attempt, or a fail-closed state.</returns>
    public GlobalAdministratorGrantCommandSnapshot Preview(
        GlobalAdministratorGrantPreview preview,
        string messageId)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsComplete || !IsValidMessageId(messageId))
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                Intent = new SetGlobalAdministrator(preview.TargetUserId),
                PreviewEvidence = preview,
                MessageId = IsValidMessageId(messageId) ? messageId : null,
                SafeMessageKey = preview.UnavailableReasonKey
                    ?? "Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Evidence",
                SafeRecoveryKey = preview.RecoveryKey
                    ?? "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Submit,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = new SetGlobalAdministrator(preview.TargetUserId),
            PreviewEvidence = preview,
            MessageId = messageId,
            CorrelationId = null,
            LastConfirmedProjection = null,
            SafeMessage = null,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
            RejectionCode = null,
            HasCommandEventEvidence = false,
            IsSubmissionAmbiguous = false,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>Invalidates a preview without replacing an already reserved command identity.</summary>
    /// <param name="reasonKey">Whole-string localized invalidation reason.</param>
    /// <returns>A fail-closed recoverable state.</returns>
    public GlobalAdministratorGrantCommandSnapshot InvalidatePreview(string reasonKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = reasonKey,
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    /// <summary>Moves the retained previewed intent to request-sent without changing its identity.</summary>
    /// <returns>The request-sent snapshot, or a fail-closed state when tracking evidence is incomplete.</returns>
    public GlobalAdministratorGrantCommandSnapshot RequestSent()
        => Intent is null || !HasTrackedPreview
            ? InvalidatePreview("Tenants.GlobalAdministrators.Grant.Preview.Unavailable.Evidence")
            : this with
            {
                State = TenantCommandLifecycleState.RequestSent,
                SafeMessage = null,
                SafeMessageKey = null,
                SafeRecoveryKey = null,
                RejectionCode = null,
                IsSubmissionAmbiguous = false,
                AuditState = TenantCommandAuditState.NotStarted,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };

    // Compatibility path for existing non-previewed callers. The production page uses Preview(...).RequestSent().
    public GlobalAdministratorGrantCommandSnapshot RequestSent(SetGlobalAdministrator intent)
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            LastConfirmedProjection = null,
            SafeMessage = null,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public GlobalAdministratorGrantCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        bool requiresExactMessage = PreviewEvidence is not null;
        if (requiresExactMessage
            && (!IsValidMessageId(MessageId)
                || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
                || string.IsNullOrWhiteSpace(result.CorrelationId)))
        {
            return AmbiguousTrackingFailure();
        }

        return this with
        {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = MessageId ?? result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
            RejectionCode = null,
            IsSubmissionAmbiguous = false,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public GlobalAdministratorGrantCommandSnapshot ApplySubmission(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsAmbiguousFailure)
        {
            return !IsValidMessageId(MessageId)
                || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
                ? AmbiguousTrackingFailure()
                : this with
                {
                    State = TenantCommandLifecycleState.RequestSent,
                    SafeMessage = null,
                    SafeMessageKey = result.SafeMessageKey
                        ?? "Tenants.GlobalAdministrators.Grant.SubmissionEvidence.Ambiguous",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
                    RejectionCode = null,
                    IsSubmissionAmbiguous = true,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                };
        }

        return result.State is TenantCommandLifecycleState.Accepted
            ? Accepted(result)
            : this with
            {
                State = result.State,
                SafeMessage = result.SafeMessage,
                SafeMessageKey = result.SafeMessageKey,
                SafeRecoveryKey = null,
                RejectionCode = result.RejectionCode,
                IsSubmissionAmbiguous = false,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
    }

    public GlobalAdministratorGrantCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return UnableToVerify(status.IsPending
                ? "Tenants.GlobalAdministrators.Grant.Status.Pending"
                : "Tenants.GlobalAdministrators.Grant.Status.Unknown");
        }

        if (PreviewEvidence is not null && !status.HasVerifiedCommandIdentity)
        {
            return UnableToVerify("Tenants.GlobalAdministrators.Grant.UnableToVerify.TrackingMismatch");
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with
                {
                    State = HasCommandEventEvidence
                        || State is TenantCommandLifecycleState.ProjectionPending
                        ? TenantCommandLifecycleState.ProjectionPending
                        : TenantCommandLifecycleState.Accepted,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null,
                    IsSubmissionAmbiguous = false,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                when status.EventCount is > 0
                => this with
                {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = true,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null,
                    IsSubmissionAmbiguous = false,
                    AuditState = TenantCommandAuditState.AuditPending,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => UnableToVerify("Tenants.GlobalAdministrators.Grant.UnableToVerify.EventEvidence"),
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => UnableToVerify("Tenants.GlobalAdministrators.Grant.UnableToVerify.StatusTimeout") with
                {
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                },
            _ => this,
        };
    }

    public GlobalAdministratorGrantCommandSnapshot SignalRNudge()
        => State is TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.ProjectionPending
            ? this with
            {
                FocusTarget = TenantCommandFocusTarget.Refresh,
            }
            : this;

    public GlobalAdministratorGrantCommandSnapshot ConfirmProjection(GlobalAdministratorsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Intent is null || State is TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Degraded
            or TenantCommandLifecycleState.UnableToVerify)
        {
            return this;
        }

        if (State is not TenantCommandLifecycleState.ProjectionPending
            || !HasCommandEventEvidence
            || PreviewEvidence?.IsComplete != true
            || !snapshot.IsCompleteEvidence
            || !snapshot.IsMutationEvidenceBacked
            || snapshot.Kind is not (GlobalAdministratorsSurfaceKind.Ready or GlobalAdministratorsSurfaceKind.Empty)
            || string.IsNullOrWhiteSpace(snapshot.ProjectionVersion))
        {
            return UnableToVerify("Tenants.GlobalAdministrators.Grant.Confirm.EvidenceRequired");
        }

        GlobalAdministratorRow? row = snapshot.Rows.FirstOrDefault(candidate =>
            string.Equals(candidate.UserId, Intent.UserId, StringComparison.Ordinal));
        if (row is null)
        {
            return this with
            {
                SafeMessage = null,
                SafeMessageKey = "Tenants.GlobalAdministrators.Grant.Confirm.DidNotConfirm",
                SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (!TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                PreviewEvidence.ProjectionVersion,
                snapshot.ProjectionVersion))
        {
            return this with
            {
                SafeMessage = null,
                SafeMessageKey = "Tenants.GlobalAdministrators.Grant.Confirm.VersionNotAdvanced",
                SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedProjection = row,
            SafeMessage = null,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    private GlobalAdministratorGrantCommandSnapshot UnableToVerify(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    private GlobalAdministratorGrantCommandSnapshot AmbiguousTrackingFailure()
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            SafeMessageKey = "Tenants.GlobalAdministrators.Grant.UnableToVerify.TrackingMismatch",
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Grant.Preview.Recovery.Refresh",
            IsSubmissionAmbiguous = true,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    private static bool IsValidMessageId(string? messageId)
        => !string.IsNullOrWhiteSpace(messageId)
            && NUlid.Ulid.TryParse(messageId, out NUlid.Ulid parsed)
            && string.Equals(messageId, parsed.ToString(), StringComparison.Ordinal);
}
