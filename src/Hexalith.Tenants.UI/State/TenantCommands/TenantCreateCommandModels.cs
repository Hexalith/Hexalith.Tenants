using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
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
    string CorrelationId);

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
    string? RejectionCode = null) {
    public static TenantCommandSubmissionResult Accepted(string messageId, string correlationId)
        => new(TenantCommandLifecycleState.Accepted, messageId, correlationId);

    public static TenantCommandSubmissionResult Rejected(string safeMessage, string? rejectionCode = null)
        => new(TenantCommandLifecycleState.Rejected, SafeMessage: safeMessage, RejectionCode: rejectionCode);

    public static TenantCommandSubmissionResult Failed(string safeMessage)
        => new(TenantCommandLifecycleState.Failed, SafeMessage: safeMessage);

    public TenantCommandTrackingHandle ToTrackingHandle()
        => new(
            MessageId ?? throw new InvalidOperationException("Accepted command is missing a message id."),
            CorrelationId ?? throw new InvalidOperationException("Accepted command is missing a correlation id."));
}

public sealed record TenantCommandStatusResult(
    CommandStatus? Status,
    string? SafeMessage = null,
    string? RejectionCode = null,
    int? EventCount = null) {
    public static TenantCommandStatusResult Unknown(string safeMessage)
        => new(null, safeMessage);
}

public sealed record TenantCreateCommandSnapshot(
    TenantCommandLifecycleState State,
    CreateTenant? Intent = null,
    TenantSummary? LastConfirmedListEvidence = null,
    TenantDetailSnapshot? LastConfirmedDetailEvidence = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
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

    public TenantCreateCommandSnapshot RequestSent(CreateTenant intent)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            SafeMessage = null,
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
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
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

    public TenantCreateCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantCreateCommandSnapshot ConfirmProjection(TenantSummary? listEvidence, TenantDetailSnapshot? detailEvidence) {
        if (Intent is null) {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        bool listProvesTenant = string.Equals(listEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool detailProvesTenant = string.Equals(detailEvidence?.Detail?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!listProvesTenant && !detailProvesTenant) {
            // No authoritative projection evidence yet: keep the lifecycle state exactly as the
            // command status set it so Accepted (received/processing) and ProjectionPending
            // (events stored/completed but not yet visible) stay distinct per AC4, instead of
            // collapsing every unverified re-query into ProjectionPending. Only nudge focus to
            // the refresh recovery action.
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedListEvidence = listEvidence,
            LastConfirmedDetailEvidence = detailEvidence,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantAddMemberCommandSnapshot(
    TenantCommandLifecycleState State,
    AddUserToTenant? Intent = null,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    bool BaselinePostconditionMet = false,
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
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
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

    public TenantAddMemberCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
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
                currentProjectionVersion)) {
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
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed when status.EventCount == 0
                => this with {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    SafeMessage = "The requested role was already applied.",
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
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

    public TenantChangeRoleCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
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
                currentProjectionVersion)) {
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
            AuditState = TenantCommandAuditState.MissingSupport,
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
        bool baselinePostconditionMet = false)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselinePostconditionMet = baselinePostconditionMet,
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
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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

    public TenantRemoveMemberCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
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

            if (!TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                    BaselineProjectionVersion,
                    currentProjectionVersion)) {
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
}

public sealed record TenantUpdateMetadataCommandSnapshot(
    TenantCommandLifecycleState State,
    UpdateTenant? Intent = null,
    string? LastConfirmedName = null,
    string? LastConfirmedDescription = null,
    TenantDetailProjection? LastConfirmedDetailProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
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

    public TenantUpdateMetadataCommandSnapshot RequestSent(UpdateTenant intent)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            SafeMessage = null,
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
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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

    public TenantUpdateMetadataCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantUpdateMetadataCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence) {
        if (Intent is null) {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool metadataMatches = tenantMatches
            && string.Equals(detailEvidence!.Name, Intent.Name, StringComparison.Ordinal)
            && string.Equals(detailEvidence.Description, Intent.Description, StringComparison.Ordinal);

        if (!metadataMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedName = detailEvidence!.Name,
            LastConfirmedDescription = detailEvidence.Description,
            LastConfirmedDetailProjection = detailEvidence,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantSetConfigurationCommandSnapshot(
    TenantCommandLifecycleState State,
    SetTenantConfiguration? Intent = null,
    TenantConfigurationProjectionProof? LastConfigurationProof = null,
    bool IsPreviewComplete = false,
    bool CompletedWithoutEvents = false,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantSetConfigurationCommandSnapshot Idle(TenantConfigurationProjectionProof? lastConfigurationProof = null)
        => new(TenantCommandLifecycleState.Idle, LastConfigurationProof: lastConfigurationProof);

    public static TenantSetConfigurationCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantSetConfigurationCommandSnapshot Previewed(SetTenantConfiguration intent)
        => this with {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            LastConfigurationProof = null,
            IsPreviewComplete = true,
            CompletedWithoutEvents = false,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantSetConfigurationCommandSnapshot AlreadyApplied(
        SetTenantConfiguration intent,
        string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            LastConfigurationProof = null,
            IsPreviewComplete = true,
            CompletedWithoutEvents = true,
            SafeMessage = safeMessage,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantSetConfigurationCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantSetConfigurationCommandSnapshot RequestSent()
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            RejectionCode = null,
            CompletedWithoutEvents = false,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantSetConfigurationCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public TenantSetConfigurationCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
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
                    CompletedWithoutEvents = status.EventCount == 0,
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

    public TenantSetConfigurationCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantSetConfigurationCommandSnapshot ConfirmProjection(TenantConfigurationProjectionProof? proof) {
        if (Intent is null) {
            return this;
        }

        bool tenantMatches = string.Equals(proof?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (proof!.Kind is not TenantConfigurationProjectionProofKind.SetConfirmed) {
            return this with {
                LastConfigurationProof = proof,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        if (State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.ProjectionPending) {
            return this with {
                State = CompletedWithoutEvents ? TenantCommandLifecycleState.AlreadyApplied : TenantCommandLifecycleState.Confirmed,
                LastConfigurationProof = proof,
                SafeMessage = CompletedWithoutEvents
                    ? "Projection evidence confirms this configuration was already applied; no configuration-set success is asserted."
                    : null,
                RejectionCode = null,
                AuditState = CompletedWithoutEvents ? TenantCommandAuditState.MissingSupport : TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this with { LastConfigurationProof = proof };
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
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantLifecycleCommandSnapshot Idle(TenantDetailProjection? lastConfirmedProjection = null)
        => new(
            TenantCommandLifecycleState.Idle,
            LastConfirmedStatus: lastConfirmedProjection?.Status ?? TenantStatus.Unknown,
            LastConfirmedProjection: lastConfirmedProjection);

    public static TenantLifecycleCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantLifecycleCommandSnapshot Previewed(
        TenantLifecycleCommandRequest intent,
        TenantDetailProjection lastConfirmedProjection) {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(lastConfirmedProjection);

        return this with {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            LastConfirmedStatus = lastConfirmedProjection.Status,
            LastConfirmedProjection = lastConfirmedProjection,
            IsPreviewComplete = true,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantLifecycleCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantLifecycleCommandSnapshot RequestSent()
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantLifecycleCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public TenantLifecycleCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
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
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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

    public TenantLifecycleCommandSnapshot SignalRNudge()
        => this with {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantLifecycleCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence) {
        if (Intent is null) {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantStatus intendedStatus = Intent.Operation is TenantLifecycleOperation.EnableTenant
            ? TenantStatus.Active
            : TenantStatus.Disabled;

        if (detailEvidence!.Status != intendedStatus) {
            return this with {
                LastConfirmedStatus = detailEvidence.Status,
                LastConfirmedProjection = detailEvidence,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        if (State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.ProjectionPending) {
            return this with {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfirmedStatus = detailEvidence.Status,
                LastConfirmedProjection = detailEvidence,
                SafeMessage = null,
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this with {
            LastConfirmedStatus = detailEvidence.Status,
            LastConfirmedProjection = detailEvidence,
        };
    }
}
