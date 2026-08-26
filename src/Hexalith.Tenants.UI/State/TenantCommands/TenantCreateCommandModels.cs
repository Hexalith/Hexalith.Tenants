using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;
using Hexalith.Tenants.UI.State.TenantDetail;

// The Tenants.UI.State.TenantDetail namespace is a sibling of this namespace, so the bare name
// `TenantDetail` resolves to that namespace instead of the query DTO. Alias the projection DTO and
// fully-qualify TenantDetailSnapshot to keep both reachable without the CS0118 collision.
using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

// CreateTenant, AddUserToTenant, ChangeUserRole, RemoveUserFromTenant, UpdateTenant,
// SetTenantConfiguration, and RemoveTenantConfiguration intents reuse the domain command records
// from Hexalith.Tenants.Contracts.Commands directly instead of mirroring them with UI-only request
// records. TenantLifecycleCommandRequest stays UI-owned because it carries a lifecycle operation
// the gateway resolves to either EnableTenant or DisableTenant.
public sealed record TenantLifecycleCommandRequest(
    string TenantId,
    TenantLifecycleOperation Operation);

public sealed record TenantCommandTrackingHandle(
    string MessageId,
    string CorrelationId,
    string? AggregateId = null);

public enum TenantCommandLifecycleState {
    Idle,
    Previewed,
    RequestSent,
    Accepted,
    ProjectionPending,
    Confirmed,
    Rejected,
    AlreadyApplied,
    DuplicatePrevented,
    Failed,
    Degraded,
    UnableToVerify,
}

public enum TenantCommandAuditState {
    NotStarted,
    AuditPending,
    AuditDelayed,
    AuditUnavailable,
    AuditAvailable,
    MissingSupport,
}

public enum TenantCommandFocusTarget {
    TenantId,
    UserId,
    Role,
    Name,
    Description,
    Namespace,
    Key,
    Value,
    Submit,
    Refresh,
    Lifecycle,
}

public enum TenantCommandLiveRegionPoliteness {
    Polite,
    Assertive,
}

public sealed record TenantCommandSubmissionResult(
    TenantCommandLifecycleState State,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    string? SafeMessageKey = null,
    bool IsAmbiguousFailure = false) {
    public static TenantCommandSubmissionResult Accepted(string messageId, string correlationId)
        => new(TenantCommandLifecycleState.Accepted, messageId, correlationId);

    public static TenantCommandSubmissionResult Rejected(string safeMessage, string? rejectionCode = null)
        => new(TenantCommandLifecycleState.Rejected, SafeMessage: safeMessage, RejectionCode: rejectionCode);

    public static TenantCommandSubmissionResult Failed(string safeMessage)
        => new(TenantCommandLifecycleState.Failed, SafeMessage: safeMessage);

    /// <summary>
    /// Fails with a Tenants resource key instead of literal text, so gateway-detected faults stay
    /// culture-aware. Flows resolve the key through the localizer and keep EN/FR parity.
    /// </summary>
    /// <param name="safeMessageKey">Tenants resource key describing the failure.</param>
    /// <returns>A failed submission result carrying the key.</returns>
    public static TenantCommandSubmissionResult FailedWithKey(string safeMessageKey)
        => new(TenantCommandLifecycleState.Failed, SafeMessageKey: safeMessageKey);

    /// <summary>
    /// Reports a submission outcome whose delivery cannot be proven. Retrying the same message id is safe,
    /// while minting another identity is not.
    /// </summary>
    /// <param name="messageId">Stable message id that may be redispatched.</param>
    /// <param name="safeMessageKey">Localized support-safe explanation.</param>
    /// <returns>An ambiguous submission result that retains dispatch ownership.</returns>
    public static TenantCommandSubmissionResult Ambiguous(string messageId, string safeMessageKey)
        => new(
            TenantCommandLifecycleState.RequestSent,
            MessageId: messageId,
            SafeMessageKey: safeMessageKey,
            IsAmbiguousFailure: true);

}

public sealed record TenantCommandStatusResult(
    CommandStatus? Status,
    string? SafeMessage = null,
    string? RejectionCode = null,
    int? EventCount = null,
    bool IsPending = false,
    bool IsRetryableFailure = false,
    bool HasVerifiedCommandIdentity = false) {
    public static TenantCommandStatusResult Unknown(string safeMessage)
        => new(null, safeMessage);

    /// <summary>
    /// Represents normal status-store propagation lag without terminating the tracked command attempt.
    /// </summary>
    /// <param name="safeMessage">Support-safe pending status text.</param>
    /// <returns>A pending status lookup result.</returns>
    public static TenantCommandStatusResult Pending(string safeMessage)
        => new(null, safeMessage, IsPending: true);

    /// <summary>
    /// Represents a transient transport or response-parsing fault that may be retried within the attempt deadline.
    /// </summary>
    /// <param name="safeMessage">Support-safe retryable failure text.</param>
    /// <returns>A retryable status lookup result.</returns>
    public static TenantCommandStatusResult RetryableFailure(string safeMessage)
        => new(null, safeMessage, IsRetryableFailure: true);
}

public sealed record TenantCreateCommandSnapshot(
    TenantCommandLifecycleState State,
    CreateTenant? Intent = null,
    TenantSummary? LastConfirmedListEvidence = null,
    TenantDetailSnapshot? LastConfirmedDetailEvidence = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    string? BaselineDetailProjectionVersion = null,
    bool BaselineTenantAbsent = false,
    bool HasCommandEventEvidence = false,
    DateTimeOffset? AttemptStartedAtUtc = null,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantCreateCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantCreateCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    /// <summary>
    /// Whether the attempt reached a terminal outcome, so a differing submit is a deliberate new
    /// attempt rather than a retry of this one.
    /// </summary>
    public bool IsTerminal
        => State is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.Failed;

    /// <summary>
    /// Surfaces a blocking reason without discarding the tracked attempt and without collapsing its
    /// lifecycle state. The static <see cref="Blocked"/> factory builds a fresh record, so using it
    /// while an attempt is tracked would drop MessageId/CorrelationId and disable the refresh
    /// recovery the blocking copy names.
    /// </summary>
    /// <param name="safeMessage">Support-safe reason to render.</param>
    /// <returns>The same attempt carrying a blocking reason.</returns>
    public TenantCreateCommandSnapshot BlockedWithTracking(string safeMessage)
        => this with {
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantCreateCommandSnapshot RequestSent(
        CreateTenant intent,
        string? baselineProjectionVersion,
        bool baselineTenantAbsent,
        string? baselineDetailProjectionVersion = null,
        DateTimeOffset? attemptStartedAtUtc = null)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselineDetailProjectionVersion = baselineDetailProjectionVersion,
            BaselineTenantAbsent = baselineTenantAbsent,
            HasCommandEventEvidence = false,
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantCreateCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantCreateCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        // Every branch clears SafeMessageKey: a keyed reason belongs to the transition that set it, so
        // leaving it in place would render a stale provenance sentence underneath a later state.
        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    // A SignalR notification is a freshness nudge only. It must never advance the lifecycle into
    // ProjectionPending, which is the single state ConfirmProjection trusts: doing so would let a
    // notification stand in for the command-status evidence that proves events were stored.
    public TenantCreateCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantCreateCommandSnapshot ConfirmProjection(
        TenantSummary? listEvidence,
        TenantDetailSnapshot? detailEvidence,
        string? currentListProjectionVersion = null) {
        if (Intent is null) {
            return this;
        }

        // Only a command status that reached event storage/completion may be reconciled into confirmation.
        // Acceptance means receipt or processing only, not proof that the requested create occurred.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        // Detail evidence is authoritative only when the detail surface is itself Ready and current.
        // A Stale, Degraded, Unknown, Unavailable, or Unauthorized read can echo matching metadata
        // without proving the projection applied this create.
        TenantDetailSnapshot? authoritativeDetail =
            detailEvidence is not null
            && detailEvidence.Kind is TenantDetailSurfaceKind.Ready
            && detailEvidence.Freshness is Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current
                ? detailEvidence
                : null;

        TenantSummary? list = listEvidence;
        TenantDetailProjection? detail = authoritativeDetail?.Detail;
        // TenantSummary intentionally omits Description, so list evidence can prove the complete submitted
        // metadata only when the submitted description is absent. A non-null description requires detail.
        // Empty and whitespace descriptions normalise to absent on both sides so a textarea that was typed
        // into and cleared still reconciles against a projection that stores no description.
        string? intentDescription = NormalizeDescription(Intent.Description);
        bool listMatches = intentDescription is null
            && string.Equals(list?.TenantId, Intent.TenantId, StringComparison.Ordinal)
            && string.Equals(list?.Name, Intent.Name, StringComparison.Ordinal);
        bool detailMatches = detail is not null
            && string.Equals(detail.TenantId, Intent.TenantId, StringComparison.Ordinal)
            && string.Equals(detail.Name, Intent.Name, StringComparison.Ordinal)
            && string.Equals(NormalizeDescription(detail.Description), intentDescription, StringComparison.Ordinal);
        if (!listMatches && !detailMatches) {
            // No authoritative projection evidence yet: keep the lifecycle state exactly as the command
            // status set it and only nudge focus to the refresh recovery action.
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (!BaselineTenantAbsent) {
            return UnableToVerify();
        }

        // Compare like for like. A tenant-detail version token is not a comparable successor to a
        // tenant-list baseline, so each projection is measured against its own captured baseline.
        (string? baselineVersion, string? currentVersion) = detailMatches && authoritativeDetail is not null
            ? (BaselineDetailProjectionVersion, authoritativeDetail.ProjectionVersion)
            : (BaselineProjectionVersion, currentListProjectionVersion);

        // The causal overload additionally requires event evidence for this exact tracked command and an
        // ordered advancement, so opaque-token churn and version regressions fail closed.
        bool versionAdvanced = TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
            baselineVersion,
            currentVersion,
            HasCommandEventEvidence);
        bool firstAppearanceWithVersion = HasCommandEventEvidence
            && string.IsNullOrWhiteSpace(baselineVersion)
            && !string.IsNullOrWhiteSpace(currentVersion);
        if (!versionAdvanced && !firstAppearanceWithVersion) {
            return UnableToVerify();
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedListEvidence = listEvidence,
            LastConfirmedDetailEvidence = authoritativeDetail,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    private static string? NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    // Leaves the LastConfirmed* slots untouched: evidence that failed the provenance gate was never
    // confirmed, so recording it would blur in-flight intent into confirmed truth.
    private TenantCreateCommandSnapshot UnableToVerify()
        => this with {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = "Tenants.Create.Confirm.UnableToVerify.MissingProvenance",
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
}

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
    public static TenantAddMemberCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantAddMemberCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
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
            AuditState = TenantCommandAuditState.NotStarted,
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
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantAddMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
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
        TenantDetailProjection? detailEvidence,
        string? currentProjectionVersion = null) {
        if (Intent is null) {
            return this;
        }

        // Only status-driven ProjectionPending (Completed/Events*) may confirm; Accepted alone never does.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

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
                AuditState = TenantCommandAuditState.AuditUnavailable,
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
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantChangeRoleCommandSnapshot(
    TenantCommandLifecycleState State,
    ChangeUserRole? Intent = null,
    TenantRole CurrentConfirmedRole = TenantRole.Unknown,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    int OwnerCount = 0,
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
    public static TenantChangeRoleCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantChangeRoleCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantChangeRoleCommandSnapshot RequestSent(
        ChangeUserRole intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        string? baselineProjectionVersion = null,
        bool baselinePostconditionMet = false)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselinePostconditionMet = baselinePostconditionMet,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantChangeRoleCommandSnapshot AlreadyApplied(
        ChangeUserRole intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantChangeRoleCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantChangeRoleCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed when status.EventCount == 0
                => this with {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    SafeMessage = "The requested role was already applied.",
                    SafeMessageKey = null,
                    RejectionCode = null,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantChangeRoleCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantChangeRoleCommandSnapshot ConfirmProjection(
        TenantDetailProjection? detailEvidence,
        string? currentProjectionVersion = null) {
        if (Intent is null) {
            return this;
        }

        // Only status-driven ProjectionPending (Completed/Events*) may confirm; Accepted alone never does.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantMember? targetMember = detailEvidence!.Members.FirstOrDefault(member =>
            string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal));
        if (targetMember is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = null,
                SafeMessageKey = "Tenants.ChangeRole.Confirm.UnableToVerify.MissingTarget",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        if (targetMember.Role != Intent.NewRole) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (BaselinePostconditionMet) {
            return this with {
                State = TenantCommandLifecycleState.AlreadyApplied,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                SafeMessageKey = "Tenants.ChangeRole.Confirm.AlreadyApplied.PreExisting",
                RejectionCode = null,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (string.IsNullOrWhiteSpace(BaselineProjectionVersion)) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                SafeMessageKey = "Tenants.ChangeRole.Confirm.UnableToVerify.MissingBaseline",
                AuditState = TenantCommandAuditState.AuditUnavailable,
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
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantRemoveMemberCommandSnapshot(
    TenantCommandLifecycleState State,
    RemoveUserFromTenant? Intent = null,
    TenantRole CurrentConfirmedRole = TenantRole.Unknown,
    int OwnerCount = 0,
    bool TargetGlobalAdministratorFriction = false,
    bool IsPreviewComplete = false,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    bool BaselinePostconditionMet = false,
    bool HasCommandEventEvidence = false,
    DateTimeOffset? AttemptStartedAtUtc = null,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantRemoveMemberCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantRemoveMemberCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantRemoveMemberCommandSnapshot Previewed(
        RemoveUserFromTenant intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        bool targetGlobalAdministratorFriction,
        TenantDetailProjection lastConfirmedMemberProjection)
        => this with {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            TargetGlobalAdministratorFriction = targetGlobalAdministratorFriction,
            IsPreviewComplete = true,
            LastConfirmedMemberProjection = lastConfirmedMemberProjection,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot AlreadyApplied(
        RemoveUserFromTenant intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        TenantDetailProjection? lastConfirmedMemberProjection,
        string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            LastConfirmedMemberProjection = lastConfirmedMemberProjection,
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantRemoveMemberCommandSnapshot RequestSent(
        string? baselineProjectionVersion = null,
        bool baselinePostconditionMet = false,
        DateTimeOffset? attemptStartedAtUtc = null)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselinePostconditionMet = baselinePostconditionMet,
            HasCommandEventEvidence = false,
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantRemoveMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        // A projection-confirmed or already-applied removal is terminal access evidence. A later status
        // poll -- including the Refresh rendered on the WP-2A receipt -- must never regress it to
        // ProjectionPending or reset AuditAvailable back to AuditPending: confirmed access and audit
        // proof are distinct, non-collapsing states, and the confirmed outcome survives audit flaps.
        if (State is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.AlreadyApplied) {
            return this;
        }

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantRemoveMemberCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantRemoveMemberCommandSnapshot ConfirmProjection(
        TenantDetailProjection? detailEvidence,
        string? currentProjectionVersion = null) {
        if (Intent is null) {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        bool targetStillPresent = detailEvidence!.Members.Any(member =>
            string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal));

        if (targetStillPresent) {
            return this with {
                LastConfirmedMemberProjection = detailEvidence,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        // Only status-driven ProjectionPending (Completed/Events*) may confirm; Accepted alone never does.
        if (State is TenantCommandLifecycleState.ProjectionPending) {
            if (BaselinePostconditionMet) {
                return this with {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    LastConfirmedMemberProjection = detailEvidence,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.RemoveMember.Confirm.AlreadyApplied.PreExisting",
                    RejectionCode = null,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                };
            }

            if (string.IsNullOrWhiteSpace(BaselineProjectionVersion)) {
                return this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    LastConfirmedMemberProjection = detailEvidence,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.RemoveMember.Confirm.UnableToVerify.MissingBaseline",
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                };
            }

            bool versionAdvanced = TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                BaselineProjectionVersion,
                currentProjectionVersion,
                HasCommandEventEvidence);
            if (!versionAdvanced) {
                return this with {
                    LastConfirmedMemberProjection = detailEvidence,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                };
            }

            return this with {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                SafeMessageKey = null,
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (State is TenantCommandLifecycleState.Rejected
            && string.Equals(RejectionCode, "UserNotInTenant", StringComparison.Ordinal)) {
            return this with {
                State = TenantCommandLifecycleState.AlreadyApplied,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                SafeMessageKey = "Tenants.RemoveMember.Confirm.AlreadyApplied.RejectedAbsence",
                RejectionCode = null,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this;
    }

    /// <summary>
    /// Promotes confirmed removal to <see cref="TenantCommandAuditState.AuditAvailable"/> only when a
    /// matching WP-2A removal audit row is supplied. Never invents available from empty or mismatched evidence.
    /// Once available, unmatched rematches keep Available (confirmed outcome survives audit flaps).
    /// </summary>
    public TenantRemoveMemberCommandSnapshot ApplyRemovalProofMatch(
        bool matched,
        bool hasCurrentLifecycleBackedEvidence = false,
        bool hasReadyReceipt = false) {
        if (State is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (matched && hasCurrentLifecycleBackedEvidence && hasReadyReceipt) {
            return this with {
                AuditState = TenantCommandAuditState.AuditAvailable,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        // Keep Available / Delayed / Unavailable across unmatched rematches; only Pending is the default wait state.
        if (AuditState is TenantCommandAuditState.AuditAvailable
            or TenantCommandAuditState.AuditDelayed
            or TenantCommandAuditState.AuditUnavailable) {
            return this;
        }

        return this with {
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>
    /// Maps audit-query surface failures after confirmation without reversing the confirmed access outcome.
    /// </summary>
    public TenantRemoveMemberCommandSnapshot ApplyRemovalProofQueryFailure(TenantCommandAuditState failureState) {
        if (State is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (failureState is not (TenantCommandAuditState.AuditDelayed or TenantCommandAuditState.AuditUnavailable)) {
            return this;
        }

        // Keep an already-matched available proof; later flaps must not silently downgrade success.
        if (AuditState is TenantCommandAuditState.AuditAvailable) {
            return this;
        }

        return this with {
            AuditState = failureState,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = failureState is TenantCommandAuditState.AuditUnavailable
                ? TenantCommandLiveRegionPoliteness.Assertive
                : TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public static bool IsMatchingRemovalProof(
        TenantAuditRow? row,
        string tenantId,
        string targetUserId,
        DateTimeOffset? attemptStartedAtUtc)
        => row is not null
        && string.Equals(row.EventType, "UserRemovedFromTenant", StringComparison.Ordinal)
        && string.Equals(row.TenantId, tenantId, StringComparison.Ordinal)
        && string.Equals(row.Target, targetUserId, StringComparison.Ordinal)
        && TenantMembershipCommandProvenance.HasQualifyingAuditProvenance(attemptStartedAtUtc, row.Timestamp);

    /// <summary>
    /// Enumerates qualifying removal-proof rows newest first. The flow walks this sequence so it can
    /// continue past weak matches to a later current, projection-backed receipt; keeping the ordering
    /// here means the unit tests exercise the exact selection production uses.
    /// </summary>
    /// <param name="rows">Audit rows returned by the authorized audit read.</param>
    /// <param name="tenantId">Tenant the removal targeted.</param>
    /// <param name="targetUserId">Removed member identifier.</param>
    /// <param name="attemptStartedAtUtc">Causal lower bound captured when the attempt was submitted.</param>
    /// <returns>Matching rows ordered newest first.</returns>
    public static IEnumerable<TenantAuditRow> EnumerateMatchingRemovalProofs(
        IReadOnlyList<TenantAuditRow> rows,
        string tenantId,
        string targetUserId,
        DateTimeOffset? attemptStartedAtUtc)
        => rows
            .Where(row => IsMatchingRemovalProof(row, tenantId, targetUserId, attemptStartedAtUtc))
            .OrderByDescending(row => row.Timestamp);

    public static TenantAuditRow? FindMatchingRemovalProof(
        IReadOnlyList<TenantAuditRow> rows,
        string tenantId,
        string targetUserId,
        DateTimeOffset? attemptStartedAtUtc)
        => EnumerateMatchingRemovalProofs(rows, tenantId, targetUserId, attemptStartedAtUtc)
            .FirstOrDefault();
}

public sealed record TenantUpdateMetadataCommandSnapshot(
    TenantCommandLifecycleState State,
    UpdateTenant? Intent = null,
    string? LastConfirmedName = null,
    string? LastConfirmedDescription = null,
    TenantDetailProjection? LastConfirmedDetailProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    DateTimeOffset? AttemptStartedAtUtc = null,
    bool HasCommandEventEvidence = false,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantUpdateMetadataCommandSnapshot Idle(
        string? lastConfirmedName = null,
        string? lastConfirmedDescription = null,
        TenantDetailProjection? lastConfirmedDetailProjection = null)
        => new(
            TenantCommandLifecycleState.Idle,
            LastConfirmedName: lastConfirmedName,
            LastConfirmedDescription: lastConfirmedDescription,
            LastConfirmedDetailProjection: lastConfirmedDetailProjection);

    public static TenantUpdateMetadataCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantUpdateMetadataCommandSnapshot RequestSent(
        UpdateTenant intent,
        string? baselineProjectionVersion,
        DateTimeOffset? attemptStartedAtUtc = null)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            BaselineProjectionVersion = baselineProjectionVersion,
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantUpdateMetadataCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantUpdateMetadataCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with {
                    State = TenantCommandLifecycleState.Accepted,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = true,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditPending,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.Completed
                => this with {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditPending,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantUpdateMetadataCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantUpdateMetadataCommandSnapshot ConfirmProjection(
        TenantDetailProjection? detailEvidence,
        string? currentProjectionVersion = null,
        TenantAuditRow? auditEvidence = null) {
        if (Intent is null) {
            return this;
        }

        // Accepted alone never confirms. ProjectionPending is also reachable via SignalRNudge, which carries no
        // status evidence, so causal confirmation additionally requires HasCommandEventEvidence below.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool metadataMatches = tenantMatches
            && string.Equals(detailEvidence!.Name, Intent.Name, StringComparison.Ordinal)
            && string.Equals(detailEvidence.Description, Intent.Description, StringComparison.Ordinal);

        if (!metadataMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        // Same-value updates still require provenance; never confirm ambient Name+Description match alone.
        if (string.IsNullOrWhiteSpace(BaselineProjectionVersion)) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedName = LastConfirmedName,
                LastConfirmedDescription = LastConfirmedDescription,
                LastConfirmedDetailProjection = LastConfirmedDetailProjection,
                SafeMessage = null,
                SafeMessageKey = "Tenants.EditMetadata.Confirm.UnableToVerify.MissingBaseline",
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        bool versionAdvanced = TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
            BaselineProjectionVersion,
            currentProjectionVersion,
            HasCommandEventEvidence);
        bool submittedMetadataIsSameAsLastConfirmed = IsSameValueAttempt;
        bool hasQualifyingAuditProvenance = IsMatchingUpdateAuditProof(
            auditEvidence,
            Intent,
            MessageId,
            AttemptStartedAtUtc);
        // Version advancement is aggregate-wide and can therefore be caused by a different command. It is
        // sufficient when the submitted metadata changed (the matching new values are a postcondition), but
        // identical values need command-specific audit provenance to distinguish this update from ambient churn.
        bool hasSafeCommandSpecificProvenance = hasQualifyingAuditProvenance
            || (!submittedMetadataIsSameAsLastConfirmed && versionAdvanced);
        if (!hasSafeCommandSpecificProvenance) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedName = LastConfirmedName,
                LastConfirmedDescription = LastConfirmedDescription,
                LastConfirmedDetailProjection = LastConfirmedDetailProjection,
                SafeMessage = null,
                SafeMessageKey = "Tenants.EditMetadata.Confirm.UnableToVerify.MissingProvenance",
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedName = detailEvidence!.Name,
            LastConfirmedDescription = detailEvidence.Description,
            LastConfirmedDetailProjection = detailEvidence,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = hasQualifyingAuditProvenance
                ? TenantCommandAuditState.AuditAvailable
                : TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    internal bool IsSameValueAttempt {
        get {
            if (Intent is null) {
                return false;
            }

            string? lastConfirmedName = LastConfirmedDetailProjection?.Name ?? LastConfirmedName;
            string? lastConfirmedDescription = LastConfirmedDetailProjection is { } lastConfirmedProjection
                ? lastConfirmedProjection.Description
                : LastConfirmedDescription;
            return string.Equals(Intent.Name, lastConfirmedName, StringComparison.Ordinal)
                && string.Equals(Intent.Description, lastConfirmedDescription, StringComparison.Ordinal);
        }
    }

    public static bool IsMatchingUpdateAuditProof(
        TenantAuditRow? row,
        UpdateTenant intent,
        string? messageId,
        DateTimeOffset? attemptStartedAtUtc) {
        ArgumentNullException.ThrowIfNull(intent);

        return row is not null
            && !string.IsNullOrWhiteSpace(messageId)
            && string.Equals(row.EventReference, messageId, StringComparison.Ordinal)
            && string.Equals(row.EventType, nameof(TenantUpdated), StringComparison.Ordinal)
            && row.Category is AuditEventCategory.Administrative
            && string.Equals(row.TenantId, intent.TenantId, StringComparison.Ordinal)
            && string.Equals(row.Target, intent.TenantId, StringComparison.Ordinal)
            && string.Equals(row.Scope, intent.TenantId, StringComparison.Ordinal)
            && string.Equals(row.Outcome, nameof(TenantUpdated), StringComparison.Ordinal)
            && !string.IsNullOrWhiteSpace(row.ActorId)
            && row.Freshness is Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current
            && row.Lifecycle is ProjectionLifecycleState.Current
            && row.Provenance is Hexalith.EventStore.Contracts.Queries.QueryResponseProvenance.ProjectionBacked
            && TenantMembershipCommandProvenance.HasQualifyingAuditProvenance(attemptStartedAtUtc, row.Timestamp);
    }
}

public sealed record TenantRemoveConfigurationCommandSnapshot(
    TenantCommandLifecycleState State,
    RemoveTenantConfiguration? Intent = null,
    TenantConfigurationProjectionProof? LastConfigurationProof = null,
    bool IsPreviewComplete = false,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantRemoveConfigurationCommandSnapshot Idle(TenantConfigurationProjectionProof? lastConfigurationProof = null)
        => new(TenantCommandLifecycleState.Idle, LastConfigurationProof: lastConfigurationProof);

    public static TenantRemoveConfigurationCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantRemoveConfigurationCommandSnapshot Previewed(RemoveTenantConfiguration intent)
        => this with {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            LastConfigurationProof = null,
            IsPreviewComplete = true,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    /// <summary>
    /// Records a safely proven, authorized missing-key outcome without representing a command dispatch.
    /// </summary>
    /// <param name="intent">The exact reviewed removal intent.</param>
    /// <param name="safeMessage">Support-safe expected-outcome text.</param>
    /// <returns>A terminal no-dispatch snapshot that retains the reviewed intent.</returns>
    public TenantRemoveConfigurationCommandSnapshot ExpectedMissing(
        RemoveTenantConfiguration intent,
        string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            LastConfigurationProof = null,
            IsPreviewComplete = true,
            SafeMessage = safeMessage,
            RejectionCode = "ConfigurationKeyNotFound",
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveConfigurationCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantRemoveConfigurationCommandSnapshot RequestSent()
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveConfigurationCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            State = TenantCommandLifecycleState.Accepted,
            MessageId = result.MessageId,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantRemoveConfigurationCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    AuditState = TenantCommandAuditState.AuditPending,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantRemoveConfigurationCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantRemoveConfigurationCommandSnapshot ConfirmProjection(TenantConfigurationProjectionProof? proof) {
        if (Intent is null) {
            return this;
        }

        bool tenantMatches = string.Equals(proof?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (proof!.Kind is not TenantConfigurationProjectionProofKind.RemoveConfirmed) {
            return this with {
                LastConfigurationProof = proof,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        if (State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.ProjectionPending) {
            return this with {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfigurationProof = proof,
                SafeMessage = null,
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this with { LastConfigurationProof = proof };
    }
}

public sealed record TenantLifecycleCommandSnapshot(
    TenantCommandLifecycleState State,
    TenantLifecycleCommandRequest? Intent = null,
    TenantStatus LastConfirmedStatus = TenantStatus.Unknown,
    TenantDetailProjection? LastConfirmedProjection = null,
    bool IsPreviewComplete = false,
    string? MessageId = null,
    string? CorrelationId = null,
    string? PreviewProjectionVersion = null,
    string? BaselineProjectionVersion = null,
    bool HasCommandEventEvidence = false,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
    DateTimeOffset? AttemptStartedAtUtc = null,
    int PendingStatusPollCount = 0,
    string? RecoveryKey = null,
    string? LastObservedProjectionVersion = null,
    long EvidenceRevision = 0)
{
    internal static readonly TimeSpan MaximumRetainedAttemptDuration = TimeSpan.FromMinutes(5);

    public static TenantLifecycleCommandSnapshot Idle(TenantDetailProjection? lastConfirmedProjection = null)
        => new(
            TenantCommandLifecycleState.Idle,
            LastConfirmedStatus: lastConfirmedProjection?.Status ?? TenantStatus.Unknown,
            LastConfirmedProjection: lastConfirmedProjection);

    /// <summary>Gets whether this logical attempt must survive component dismissal or remount.</summary>
    public bool RetainsAttempt
        => State is TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.ProjectionPending;

    /// <summary>Gets whether aggregate ownership may be released for this attempt.</summary>
    public bool HasTerminalOwnership
        => State is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied
            or TenantCommandLifecycleState.DuplicatePrevented
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Degraded
            or TenantCommandLifecycleState.UnableToVerify;

    public TenantLifecycleCommandSnapshot Previewed(
        TenantLifecycleCommandRequest intent,
        TenantDetailProjection lastConfirmedProjection,
        string previewProjectionVersion)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(lastConfirmedProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(previewProjectionVersion);

        return this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            LastConfirmedStatus = lastConfirmedProjection.Status,
            LastConfirmedProjection = lastConfirmedProjection,
            PreviewProjectionVersion = previewProjectionVersion,
            LastObservedProjectionVersion = previewProjectionVersion,
            IsPreviewComplete = true,
            SafeMessage = null,
            SafeMessageKey = null,
            RecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    public TenantLifecycleCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with
        {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            EvidenceRevision = NextEvidenceRevision(),
        };

    /// <summary>
    /// Starts one lifecycle attempt from fresh authoritative detail evidence and a stable message id.
    /// </summary>
    /// <param name="intent">Exact lifecycle intent.</param>
    /// <param name="lastConfirmedProjection">Fresh pre-submit tenant projection.</param>
    /// <param name="baselineProjectionVersion">Ordered pre-submit tenant projection version.</param>
    /// <param name="messageId">Stable message id used by every dispatch or recovery of this attempt.</param>
    /// <returns>The request-sent snapshot.</returns>
    public TenantLifecycleCommandSnapshot RequestSent(
        TenantLifecycleCommandRequest intent,
        TenantDetailProjection lastConfirmedProjection,
        string baselineProjectionVersion,
        string messageId,
        DateTimeOffset? attemptStartedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(lastConfirmedProjection);
        ArgumentException.ThrowIfNullOrWhiteSpace(baselineProjectionVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        return this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            LastConfirmedStatus = lastConfirmedProjection.Status,
            LastConfirmedProjection = lastConfirmedProjection,
            MessageId = messageId,
            CorrelationId = null,
            PreviewProjectionVersion = baselineProjectionVersion,
            BaselineProjectionVersion = baselineProjectionVersion,
            LastObservedProjectionVersion = baselineProjectionVersion,
            HasCommandEventEvidence = false,
            SafeMessage = null,
            SafeMessageKey = null,
            RecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            AttemptStartedAtUtc = (attemptStartedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime(),
            PendingStatusPollCount = 0,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    public TenantLifecycleCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (State is not TenantCommandLifecycleState.RequestSent
            || result.State is not TenantCommandLifecycleState.Accepted
            || result.IsAmbiguousFailure
            || string.IsNullOrWhiteSpace(MessageId)
            || !string.Equals(result.MessageId, MessageId, StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(result.CorrelationId))
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.TrackingMismatch");
        }

        return this with
        {
            State = TenantCommandLifecycleState.Accepted,
            CorrelationId = result.CorrelationId,
            SafeMessage = null,
            SafeMessageKey = null,
            RecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            AttemptStartedAtUtc = AttemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            PendingStatusPollCount = 0,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    /// <summary>Gets whether the bounded dispatch/reconciliation window has elapsed.</summary>
    /// <param name="observedAtUtc">UTC observation time.</param>
    /// <returns><see langword="true"/> when ownership must be released.</returns>
    internal bool IsRetentionExpired(DateTimeOffset observedAtUtc)
    {
        if (AttemptStartedAtUtc is null)
        {
            return true;
        }

        DateTimeOffset normalizedObserved = observedAtUtc.ToUniversalTime();
        DateTimeOffset normalizedStart = AttemptStartedAtUtc.Value.ToUniversalTime();
        return normalizedObserved < normalizedStart
            || normalizedObserved - normalizedStart >= MaximumRetainedAttemptDuration;
    }

    /// <summary>Retains an ambiguous submission for same-message redispatch.</summary>
    /// <param name="safeMessageKey">Localized support-safe ambiguity explanation.</param>
    /// <returns>The retained request-sent snapshot.</returns>
    public TenantLifecycleCommandSnapshot AmbiguousSubmission(string safeMessageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessageKey);
        return this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            RecoveryKey = "Tenants.Lifecycle.Dispatch.Recovery",
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    public TenantLifecycleCommandSnapshot ApplyStatus(
        TenantCommandStatusResult status,
        DateTimeOffset? observedAtUtc = null)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (!RetainsAttempt)
        {
            return this;
        }

        if ((status.IsPending && status.IsRetryableFailure)
            || (status.Status is not null && (status.IsPending || status.IsRetryableFailure))
            || (status.Status is not null && !status.HasVerifiedCommandIdentity))
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.TrackingMismatch");
        }

        DateTimeOffset observed = (observedAtUtc ?? DateTimeOffset.UtcNow).ToUniversalTime();
        bool attemptExpired = IsRetentionExpired(observed);

        if (status.IsPending || status.IsRetryableFailure)
        {
            int nextPendingPollCount = PendingStatusPollCount == int.MaxValue
                ? int.MaxValue
                : PendingStatusPollCount + 1;
            if (attemptExpired
                && !(State is TenantCommandLifecycleState.ProjectionPending && HasCommandEventEvidence))
            {
                return UnableToVerify("Tenants.Lifecycle.UnableToVerify.StatusTimeout") with
                {
                    PendingStatusPollCount = nextPendingPollCount,
                };
            }

            return this with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                    ? status.IsPending
                        ? "Tenants.Lifecycle.StatusEvidence.Pending"
                        : "Tenants.Lifecycle.StatusEvidence.RetryableFailure"
                    : null,
                RecoveryKey = "Tenants.Lifecycle.Retained.Recovery",
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                PendingStatusPollCount = nextPendingPollCount,
                EvidenceRevision = NextEvidenceRevision(),
            };
        }

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                    ? "Tenants.Lifecycle.UnableToVerify.Status"
                    : null,
                RecoveryKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                EvidenceRevision = NextEvidenceRevision(),
            };
        }

        if (attemptExpired
            && !(State is TenantCommandLifecycleState.ProjectionPending && HasCommandEventEvidence)
            && status.Status.Value is CommandStatus.Received
                or CommandStatus.Processing)
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.StatusTimeout");
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with
                {
                    State = State is TenantCommandLifecycleState.ProjectionPending
                        ? TenantCommandLifecycleState.ProjectionPending
                        : TenantCommandLifecycleState.Accepted,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RecoveryKey = null,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with
                {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = true,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RecoveryKey = null,
                    AuditState = TenantCommandAuditState.AuditPending,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.Completed
                when status.EventCount is not > 0 && !HasCommandEventEvidence
                => UnableToVerify("Tenants.Lifecycle.UnableToVerify.MissingEventEvidence"),
            CommandStatus.Completed
                => this with
                {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = null,
                    RecoveryKey = null,
                    AuditState = TenantCommandAuditState.AuditPending,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.Rejected
                when string.Equals(
                    status.RejectionCode,
                    "TenantLifecycleStateAlreadySet",
                    StringComparison.Ordinal)
                => this with
                {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                        ? "Tenants.Lifecycle.Message.Rejected.TenantLifecycleStateAlreadySet"
                        : null,
                    RecoveryKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                        ? status.RejectionCode switch
                        {
                            "InsufficientPermissions" => "Tenants.Lifecycle.Message.Rejected.InsufficientPermissions",
                            "TenantDisabled" => "Tenants.Lifecycle.Message.Rejected.TenantDisabled",
                            "TenantNotFound" => "Tenants.Lifecycle.Message.Rejected.TenantNotFound",
                            _ => "Tenants.Lifecycle.Message.Rejected",
                        }
                        : null,
                    RecoveryKey = null,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                        ? "Tenants.Lifecycle.Message.Degraded"
                        : null,
                    RecoveryKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = string.IsNullOrWhiteSpace(status.SafeMessage)
                        ? "Tenants.Lifecycle.Message.UnableToVerify"
                        : null,
                    RecoveryKey = null,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    PendingStatusPollCount = 0,
                    EvidenceRevision = NextEvidenceRevision(),
                },
            _ => UnableToVerify("Tenants.Lifecycle.UnableToVerify.Status"),
        };
    }

    public TenantLifecycleCommandSnapshot SignalRNudge()
        => !RetainsAttempt || FocusTarget is TenantCommandFocusTarget.Refresh
            ? this
            : this with
            {
                FocusTarget = TenantCommandFocusTarget.Refresh,
                EvidenceRevision = NextEvidenceRevision(),
            };

    /// <summary>
    /// Reconciles the tracked attempt from one authoritative tenant-detail proof read.
    /// </summary>
    /// <param name="proof">Detail, freshness, lifecycle, and ordered version from one unconditional read.</param>
    /// <returns>The reconciled lifecycle snapshot.</returns>
    public TenantLifecycleCommandSnapshot ConfirmProjection(TenantDetailSnapshot? proof)
    {
        if (Intent is null)
        {
            return this;
        }

        if (!Enum.IsDefined(Intent.Operation))
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.TrackingMismatch");
        }

        TenantDetailProjection? detailEvidence = proof?.Detail;
        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool authoritative = tenantMatches
            && proof!.Kind is TenantDetailSurfaceKind.Ready
            && proof.Freshness is Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current
            && proof.Lifecycle is ProjectionLifecycleState.Current
            && !string.IsNullOrWhiteSpace(proof.ProjectionVersion);
        if (!authoritative)
        {
            return this;
        }

        string currentProjectionVersion = proof!.ProjectionVersion!;

        if (TenantLifecycleProjectionVersion.CompareSequences(
                currentProjectionVersion,
                LastObservedProjectionVersion) is TenantLifecycleSequenceRelation.IncomingOlder)
        {
            return this;
        }

        if (!Enum.IsDefined(detailEvidence!.Status)
            || detailEvidence.Status is TenantStatus.Unknown)
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.ProofRead");
        }

        TenantStatus intendedStatus = Intent.Operation is TenantLifecycleOperation.EnableTenant
            ? TenantStatus.Active
            : TenantStatus.Disabled;

        if (detailEvidence.Status != intendedStatus)
        {
            return ObserveProjectionEvidence(detailEvidence, currentProjectionVersion);
        }

        if (State is not TenantCommandLifecycleState.ProjectionPending)
        {
            return ObserveProjectionEvidence(detailEvidence, currentProjectionVersion, moveFocus: false);
        }

        if (string.IsNullOrWhiteSpace(BaselineProjectionVersion))
        {
            return UnableToVerify("Tenants.Lifecycle.UnableToVerify.MissingBaseline");
        }

        if (!HasCommandEventEvidence)
        {
            return ObserveProjectionEvidence(
                detailEvidence,
                currentProjectionVersion,
                "Tenants.Lifecycle.ProjectionEvidence.MissingEventEvidence");
        }

        TenantLifecycleProjectionVersionComparison versionComparison
            = TenantLifecycleProjectionVersion.Compare(BaselineProjectionVersion, currentProjectionVersion);
        if (versionComparison is not TenantLifecycleProjectionVersionComparison.Advanced)
        {
            string safeMessageKey = versionComparison switch
            {
                TenantLifecycleProjectionVersionComparison.Invalid
                    => "Tenants.Lifecycle.ProjectionEvidence.InvalidVersion",
                TenantLifecycleProjectionVersionComparison.PrefixMismatch
                    => "Tenants.Lifecycle.ProjectionEvidence.PrefixMismatch",
                _ => "Tenants.Lifecycle.ProjectionEvidence.NotAdvanced",
            };
            return ObserveProjectionEvidence(detailEvidence, currentProjectionVersion, safeMessageKey);
        }

        return this with
        {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedStatus = detailEvidence.Status,
            LastConfirmedProjection = detailEvidence,
            LastObservedProjectionVersion = MergeObservedProjectionVersion(currentProjectionVersion),
            SafeMessage = null,
            SafeMessageKey = null,
            RecoveryKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    /// <summary>
    /// Records a terminal, support-safe proof failure without discarding last-confirmed projection truth.
    /// </summary>
    /// <param name="safeMessageKey">Whole-string localized failure key.</param>
    /// <returns>The unable-to-verify snapshot.</returns>
    public TenantLifecycleCommandSnapshot UnableToVerify(string safeMessageKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessageKey);
        return this with
        {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            RecoveryKey = null,
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    /// <summary>
    /// Surfaces a blocking reason while preserving the tracked attempt and its recovery handle.
    /// </summary>
    /// <param name="safeMessageKey">Whole-string localized blocking reason.</param>
    /// <returns>The same tracked attempt with support-safe copy.</returns>
    public TenantLifecycleCommandSnapshot BlockedWithTracking(
        string safeMessageKey,
        string? recoveryKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessageKey);
        return this with
        {
            SafeMessage = null,
            SafeMessageKey = safeMessageKey,
            RecoveryKey = recoveryKey,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    /// <summary>
    /// Surfaces already-localized blocking copy while preserving the tracked attempt and recovery handle.
    /// </summary>
    /// <param name="safeMessage">Localized support-safe blocking copy.</param>
    /// <returns>The same tracked attempt with support-safe copy.</returns>
    public TenantLifecycleCommandSnapshot BlockedWithTrackingMessage(
        string safeMessage,
        string? recoveryKey = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(safeMessage);
        return this with
        {
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            RecoveryKey = recoveryKey,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    /// <summary>Stops retaining an attempt that the operator explicitly chose to abandon.</summary>
    /// <returns>A terminal snapshot that releases tracked ownership.</returns>
    public TenantLifecycleCommandSnapshot Abandon()
        => UnableToVerify("Tenants.Lifecycle.UnableToVerify.Abandoned");

    private TenantLifecycleCommandSnapshot ObserveProjectionEvidence(
        TenantDetailProjection detailEvidence,
        string projectionVersion,
        string? safeMessageKey = null,
        bool moveFocus = true)
    {
        TenantCommandFocusTarget focusTarget = moveFocus
            ? TenantCommandFocusTarget.Refresh
            : FocusTarget;
        string? observedProjectionVersion = MergeObservedProjectionVersion(projectionVersion);
        string? safeMessage = safeMessageKey is null ? SafeMessage : null;
        string? effectiveSafeMessageKey = safeMessageKey ?? SafeMessageKey;
        string? recoveryKey = safeMessageKey is null
            ? RecoveryKey
            : "Tenants.Lifecycle.Retained.Recovery";
        if (LastConfirmedStatus == detailEvidence.Status
            && Equals(LastConfirmedProjection, detailEvidence)
            && string.Equals(LastObservedProjectionVersion, observedProjectionVersion, StringComparison.Ordinal)
            && string.Equals(SafeMessageKey, effectiveSafeMessageKey, StringComparison.Ordinal)
            && string.Equals(RecoveryKey, recoveryKey, StringComparison.Ordinal)
            && FocusTarget == focusTarget)
        {
            return this;
        }

        return this with
        {
            LastConfirmedStatus = detailEvidence.Status,
            LastConfirmedProjection = detailEvidence,
            LastObservedProjectionVersion = observedProjectionVersion,
            SafeMessage = safeMessage,
            SafeMessageKey = effectiveSafeMessageKey,
            RecoveryKey = recoveryKey,
            FocusTarget = focusTarget,
            EvidenceRevision = NextEvidenceRevision(),
        };
    }

    private string? MergeObservedProjectionVersion(string incoming)
    {
        TenantLifecycleSequenceRelation relation = TenantLifecycleProjectionVersion.CompareSequences(
            incoming,
            LastObservedProjectionVersion);
        return relation is TenantLifecycleSequenceRelation.IncomingNewer
            or TenantLifecycleSequenceRelation.Equal
            ? incoming
            : LastObservedProjectionVersion;
    }

    internal long NextEvidenceRevision()
        => EvidenceRevision == long.MaxValue ? long.MaxValue : EvidenceRevision + 1;
}
