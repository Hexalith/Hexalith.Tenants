using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

/// <summary>Tracks one causally qualified global-administrator removal attempt.</summary>
/// <param name="State">Current monotonic command lifecycle state.</param>
/// <param name="Intent">Literal removal intent.</param>
/// <param name="LastConfirmedProjection">Last confirmed target row, when still present.</param>
/// <param name="MessageId">Caller-owned canonical ULID.</param>
/// <param name="CorrelationId">Accepted command correlation identifier, when available.</param>
/// <param name="SafeMessage">Legacy support-safe message; interactive consumers prefer resource keys.</param>
/// <param name="RejectionCode">Structured rejection code.</param>
/// <param name="AuditState">Current audit evidence state.</param>
/// <param name="FocusTarget">Next accessible focus target.</param>
/// <param name="LiveRegionPoliteness">Live-region politeness for the current state.</param>
/// <param name="PreviewEvidence">Immutable complete BFF removal preview.</param>
/// <param name="HasCommandEventEvidence">Whether exact-command status proved positive event evidence.</param>
/// <param name="IsSubmissionAmbiguous">Whether delivery may have reached the server without acceptance evidence.</param>
/// <param name="SafeMessageKey">Removal-specific localized lifecycle explanation.</param>
/// <param name="SafeRecoveryKey">Removal-specific localized recovery.</param>
public sealed record GlobalAdministratorRemoveCommandSnapshot(
    TenantCommandLifecycleState State,
    RemoveGlobalAdministrator? Intent = null,
    GlobalAdministratorRow? LastConfirmedProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
    GlobalAdministratorRemovePreview? PreviewEvidence = null,
    bool HasCommandEventEvidence = false,
    bool IsSubmissionAmbiguous = false,
    string? SafeMessageKey = null,
    string? SafeRecoveryKey = null)
{
    /// <summary>Gets the complete projection version captured before dispatch.</summary>
    public string? BaselineProjectionVersion => PreviewEvidence?.ProjectionVersion;

    /// <summary>Gets the complete fixed-scope count captured before dispatch.</summary>
    public int? BaselineAdministratorCount => PreviewEvidence?.CurrentAdministratorCount;

    /// <summary>Gets whether this attempt owns a complete preview and valid caller-owned message id.</summary>
    public bool HasTrackedPreview
        => PreviewEvidence?.IsComplete == true && IsValidMessageId(MessageId);

    /// <summary>Returns a support-safe description without identity or tracking values.</summary>
    /// <returns>A bounded diagnostic description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorRemoveCommandSnapshot)} {{ State = {State}, HasIntent = {Intent is not null}, HasTrackedPreview = {HasTrackedPreview}, HasCommandEventEvidence = {HasCommandEventEvidence}, IsSubmissionAmbiguous = {IsSubmissionAmbiguous}, AuditState = {AuditState}, RejectionCode = {RejectionCode}, FocusTarget = {FocusTarget}, LiveRegionPoliteness = {LiveRegionPoliteness} }}";

    /// <summary>Creates an idle removal lifecycle.</summary>
    /// <returns>An idle snapshot.</returns>
    public static GlobalAdministratorRemoveCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    /// <summary>Creates a fail-closed state before command dispatch.</summary>
    /// <param name="safeMessage">Localized message or resource key.</param>
    /// <param name="focusTarget">Next accessible focus target.</param>
    /// <returns>A blocked snapshot.</returns>
    public static GlobalAdministratorRemoveCommandSnapshot Blocked(
        string safeMessage,
        TenantCommandFocusTarget focusTarget)
    {
        ArgumentNullException.ThrowIfNull(safeMessage);
        return safeMessage.StartsWith("Tenants.", StringComparison.Ordinal)
            ? new(
                TenantCommandLifecycleState.UnableToVerify,
                SafeMessageKey: safeMessage,
                SafeRecoveryKey: "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
                AuditState: TenantCommandAuditState.MissingSupport,
                FocusTarget: focusTarget,
                LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive)
            : new(
                TenantCommandLifecycleState.UnableToVerify,
                SafeMessage: safeMessage,
                SafeRecoveryKey: "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
                AuditState: TenantCommandAuditState.MissingSupport,
                FocusTarget: focusTarget,
                LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);
    }

    /// <summary>Surfaces a BFF-composed unavailable preview and its associated recovery.</summary>
    /// <param name="preview">Fail-closed preview evidence.</param>
    /// <returns>A support-safe state that retains no command identity.</returns>
    public static GlobalAdministratorRemoveCommandSnapshot UnavailablePreview(
        GlobalAdministratorRemovePreview preview)
    {
        ArgumentNullException.ThrowIfNull(preview);
        return new(
            TenantCommandLifecycleState.UnableToVerify,
            Intent: new RemoveGlobalAdministrator(preview.TargetUserId),
            PreviewEvidence: preview,
            SafeMessageKey: preview.UnavailableReasonKey
                ?? "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
            SafeRecoveryKey: preview.RecoveryKey
                ?? "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);
    }

    /// <summary>Captures one complete preview and caller-owned ULID before dispatch.</summary>
    /// <param name="preview">BFF-composed removal preview.</param>
    /// <param name="messageId">Exact caller-owned canonical ULID.</param>
    /// <returns>The previewed attempt, or a fail-closed snapshot.</returns>
    public GlobalAdministratorRemoveCommandSnapshot Preview(
        GlobalAdministratorRemovePreview preview,
        string messageId)
    {
        ArgumentNullException.ThrowIfNull(preview);
        if (!preview.IsComplete || !IsValidMessageId(messageId))
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                Intent = new RemoveGlobalAdministrator(preview.TargetUserId),
                PreviewEvidence = preview,
                MessageId = IsValidMessageId(messageId) ? messageId : null,
                SafeMessage = null,
                SafeMessageKey = preview.UnavailableReasonKey
                    ?? "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
                SafeRecoveryKey = preview.RecoveryKey
                    ?? "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = new RemoveGlobalAdministrator(preview.TargetUserId),
            LastConfirmedProjection = null,
            MessageId = messageId,
            CorrelationId = null,
            SafeMessage = null,
            SafeMessageKey = null,
            SafeRecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            PreviewEvidence = preview,
            HasCommandEventEvidence = false,
            IsSubmissionAmbiguous = false,
        };
    }

    /// <summary>Invalidates an undispatched preview with associated localized evidence.</summary>
    /// <param name="reasonKey">Localized reason key.</param>
    /// <param name="recoveryKey">Localized recovery key.</param>
    /// <returns>A fail-closed snapshot.</returns>
    public GlobalAdministratorRemoveCommandSnapshot InvalidatePreview(
        string reasonKey,
        string recoveryKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = reasonKey,
            SafeRecoveryKey = recoveryKey,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    /// <summary>Retains an ambiguous dispatched attempt when a retry preflight fails closed.</summary>
    /// <param name="reasonKey">Current localized preflight reason.</param>
    /// <param name="recoveryKey">Current localized preflight recovery.</param>
    /// <returns>The same retainable request-sent attempt.</returns>
    public GlobalAdministratorRemoveCommandSnapshot RetainAmbiguousPreflight(
        string reasonKey,
        string recoveryKey)
        => this with
        {
            State = State is TenantCommandLifecycleState.UnableToVerify && IsSubmissionAmbiguous
                ? TenantCommandLifecycleState.UnableToVerify
                : TenantCommandLifecycleState.RequestSent,
            CorrelationId = null,
            SafeMessage = null,
            SafeMessageKey = reasonKey,
            SafeRecoveryKey = recoveryKey,
            IsSubmissionAmbiguous = true,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    /// <summary>Moves the retained preview to request-sent without changing command identity.</summary>
    /// <returns>The request-sent attempt.</returns>
    public GlobalAdministratorRemoveCommandSnapshot RequestSent()
        => Intent is null || !HasTrackedPreview
            ? InvalidatePreview(
                "Tenants.GlobalAdministrators.Remove.Preview.Unavailable.Evidence",
                "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh")
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

    /// <summary>Applies accepted submission evidence only when it exactly matches the retained attempt.</summary>
    /// <param name="result">Gateway submission result.</param>
    /// <returns>The accepted or ambiguous tracking state.</returns>
    public GlobalAdministratorRemoveCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (!IsValidMessageId(MessageId)
            || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(result.CorrelationId))
        {
            return AmbiguousTrackingFailure();
        }

        return this with
        {
            State = TenantCommandLifecycleState.Accepted,
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

    /// <summary>Applies one gateway submission result without accepting unsupported lifecycle states.</summary>
    /// <param name="result">Gateway result.</param>
    /// <returns>The monotonic removal lifecycle.</returns>
    public GlobalAdministratorRemoveCommandSnapshot ApplySubmission(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsAmbiguousFailure)
        {
            return !IsValidMessageId(MessageId)
                || !string.Equals(MessageId, result.MessageId, StringComparison.Ordinal)
                    ? AmbiguousTrackingFailure()
                    : this with
                    {
                        // An unsupported delivery result is already an UnableToVerify state. A later
                        // ambiguous same-id retry must remain monotonic so the lease-backed completion
                        // token can be cleared without regressing to RequestSent and stranding recovery.
                        State = State is TenantCommandLifecycleState.UnableToVerify
                                && IsSubmissionAmbiguous
                            ? TenantCommandLifecycleState.UnableToVerify
                            : TenantCommandLifecycleState.RequestSent,
                        CorrelationId = null,
                        SafeMessage = null,
                        SafeMessageKey = result.SafeMessageKey
                            ?? "Tenants.GlobalAdministrators.Remove.SubmissionEvidence.Ambiguous",
                        SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery",
                        RejectionCode = null,
                        IsSubmissionAmbiguous = true,
                        AuditState = TenantCommandAuditState.AuditDelayed,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    };
        }

        if (result.State is TenantCommandLifecycleState.Accepted)
        {
            return Accepted(result);
        }

        if (result.State is TenantCommandLifecycleState.Rejected or TenantCommandLifecycleState.Failed)
        {
            return this with
            {
                State = result.State,
                SafeMessage = null,
                SafeMessageKey = result.State is TenantCommandLifecycleState.Rejected
                    ? RejectionMessageKey(result.RejectionCode)
                    : "Tenants.GlobalAdministrators.Remove.Status.Failed",
                SafeRecoveryKey = result.State is TenantCommandLifecycleState.Rejected
                    ? "Tenants.GlobalAdministrators.Remove.Recovery.Rejected"
                    : "Tenants.GlobalAdministrators.Remove.Recovery.Failed",
                RejectionCode = result.RejectionCode,
                IsSubmissionAmbiguous = false,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return UnableToVerify("Tenants.GlobalAdministrators.Remove.UnableToVerify.UnsupportedSubmission") with
        {
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery",
            IsSubmissionAmbiguous = true,
            AuditState = TenantCommandAuditState.AuditDelayed,
        };
    }

    /// <summary>Applies exact command-status evidence.</summary>
    /// <param name="status">Verified or fail-closed status result.</param>
    /// <returns>The monotonic removal lifecycle.</returns>
    public GlobalAdministratorRemoveCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (status.Status is null)
        {
            return UnableToVerify(status.IsPending
                ? "Tenants.GlobalAdministrators.Remove.Status.Pending"
                : "Tenants.GlobalAdministrators.Remove.Status.Unknown");
        }

        if (!status.HasVerifiedCommandIdentity)
        {
            return UnableToVerify("Tenants.GlobalAdministrators.Remove.UnableToVerify.TrackingMismatch");
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
                    SafeMessage = null,
                    SafeMessageKey = null,
                    SafeRecoveryKey = null,
                    IsSubmissionAmbiguous = false,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => WithPositiveEventEvidence(),
            CommandStatus.Completed when status.EventCount is > 0
                => WithPositiveEventEvidence(),
            CommandStatus.Completed
                => UnableToVerify("Tenants.GlobalAdministrators.Remove.UnableToVerify.EventEvidence"),
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = null,
                    SafeMessageKey = RejectionMessageKey(status.RejectionCode),
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Recovery.Rejected",
                    RejectionCode = status.RejectionCode,
                    IsSubmissionAmbiguous = false,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.GlobalAdministrators.Remove.Status.PublishFailed",
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Recovery.PublishFailed",
                    IsSubmissionAmbiguous = false,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => UnableToVerify("Tenants.GlobalAdministrators.Remove.UnableToVerify.StatusTimeout") with
                {
                    SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Recovery.TimedOut",
                    AuditState = TenantCommandAuditState.AuditDelayed,
                },
            _ => UnableToVerify("Tenants.GlobalAdministrators.Remove.UnableToVerify.UnsupportedSubmission"),
        };
    }

    /// <summary>Records a notification as a requery nudge without changing command proof.</summary>
    /// <returns>The same lifecycle with refresh focus.</returns>
    public GlobalAdministratorRemoveCommandSnapshot SignalRNudge()
        => State is TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.ProjectionPending
                ? this with { FocusTarget = TenantCommandFocusTarget.Refresh }
                : this;

    /// <summary>Confirms removal only from exact command-event evidence and complete causal absence.</summary>
    /// <param name="snapshot">Complete current fixed-scope projection.</param>
    /// <returns>The confirmed or fail-closed lifecycle.</returns>
    public GlobalAdministratorRemoveCommandSnapshot ConfirmProjection(GlobalAdministratorsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (Intent is null
            || State is TenantCommandLifecycleState.Rejected
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
            || snapshot.Kind is not GlobalAdministratorsSurfaceKind.Ready
            || string.IsNullOrWhiteSpace(snapshot.ProjectionVersion))
        {
            return UnableToVerify("Tenants.GlobalAdministrators.Remove.Confirm.EvidenceRequired");
        }

        GlobalAdministratorRow? row = snapshot.Rows.FirstOrDefault(candidate =>
            string.Equals(candidate.UserId, Intent.UserId, StringComparison.Ordinal));
        if (row is not null)
        {
            return this with
            {
                LastConfirmedProjection = row,
                SafeMessage = null,
                SafeMessageKey = "Tenants.GlobalAdministrators.Remove.Confirm.StillPresent",
                SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (!TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                PreviewEvidence.ProjectionVersion,
                snapshot.ProjectionVersion,
                HasCommandEventEvidence))
        {
            return this with
            {
                LastConfirmedProjection = null,
                SafeMessage = null,
                SafeMessageKey = "Tenants.GlobalAdministrators.Remove.Confirm.VersionNotAdvanced",
                SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedProjection = null,
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

    private GlobalAdministratorRemoveCommandSnapshot WithPositiveEventEvidence()
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
        };

    private GlobalAdministratorRemoveCommandSnapshot UnableToVerify(string safeMessageKey)
        => this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.Preview.Recovery.Refresh",
            IsSubmissionAmbiguous = false,
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    private GlobalAdministratorRemoveCommandSnapshot AmbiguousTrackingFailure()
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            CorrelationId = null,
            SafeMessage = null,
            SafeMessageKey = "Tenants.GlobalAdministrators.Remove.UnableToVerify.TrackingMismatch",
            SafeRecoveryKey = "Tenants.GlobalAdministrators.Remove.DeliveryRetry.Recovery",
            IsSubmissionAmbiguous = true,
            AuditState = TenantCommandAuditState.AuditDelayed,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    private static string RejectionMessageKey(string? rejectionCode)
        => rejectionCode switch
        {
            "LastGlobalAdministrator" => "Tenants.GlobalAdministrators.Remove.Status.Rejected.LastAdministrator",
            "GlobalAdministratorNotFound" => "Tenants.GlobalAdministrators.Remove.Status.Rejected.NotFound",
            "InsufficientPermissions" => "Tenants.GlobalAdministrators.Remove.Status.Rejected.Permission",
            _ => "Tenants.GlobalAdministrators.Remove.Status.Rejected",
        };

    private static bool IsValidMessageId(string? messageId)
        => !string.IsNullOrWhiteSpace(messageId)
            && NUlid.Ulid.TryParse(messageId, out NUlid.Ulid parsed)
            && string.Equals(messageId, parsed.ToString(), StringComparison.Ordinal);
}
