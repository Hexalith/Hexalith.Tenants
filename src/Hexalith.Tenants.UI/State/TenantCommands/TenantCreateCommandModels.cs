using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantList;

// The Tenants.UI.State.TenantDetail namespace is a sibling of this namespace, so the bare name
// `TenantDetail` resolves to that namespace instead of the query DTO. Alias the projection DTO and
// fully-qualify TenantDetailSnapshot to keep both reachable without the CS0118 collision.
using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record CreateTenantCommandRequest(
    string TenantId,
    string Name,
    string? Description);

public sealed record AddUserToTenantCommandRequest(
    string TenantId,
    string UserId,
    TenantRole Role);

public sealed record ChangeUserRoleCommandRequest(
    string TenantId,
    string UserId,
    TenantRole NewRole);

public sealed record RemoveUserFromTenantCommandRequest(
    string TenantId,
    string UserId);

public sealed record UpdateTenantCommandRequest(
    string TenantId,
    string Name,
    string? Description);

public sealed record TenantCommandTrackingHandle(
    string MessageId,
    string CorrelationId);

public enum TenantCommandLifecycleState
{
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

public enum TenantCommandAuditState
{
    NotStarted,
    AuditPending,
    AuditDelayed,
    AuditUnavailable,
    MissingSupport,
}

public enum TenantCommandFocusTarget
{
    TenantId,
    UserId,
    Role,
    Name,
    Description,
    Submit,
    Refresh,
    Lifecycle,
}

public enum TenantCommandLiveRegionPoliteness
{
    Polite,
    Assertive,
}

public sealed record TenantCommandSubmissionResult(
    TenantCommandLifecycleState State,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null)
{
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
    int? EventCount = null)
{
    public static TenantCommandStatusResult Unknown(string safeMessage)
        => new(null, safeMessage);
}

public sealed record TenantCreateCommandSnapshot(
    TenantCommandLifecycleState State,
    CreateTenantCommandRequest? Intent = null,
    TenantSummary? LastConfirmedListEvidence = null,
    TenantDetailSnapshot? LastConfirmedDetailEvidence = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
    public static TenantCreateCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantCreateCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantCreateCommandSnapshot RequestSent(CreateTenantCommandRequest intent)
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantCreateCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
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

    public TenantCreateCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantCreateCommandSnapshot SignalRNudge()
        => this with
        {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantCreateCommandSnapshot ConfirmProjection(TenantSummary? listEvidence, TenantDetailSnapshot? detailEvidence)
    {
        if (Intent is null)
        {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this;
        }

        bool listProvesTenant = string.Equals(listEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool detailProvesTenant = string.Equals(detailEvidence?.Detail?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!listProvesTenant && !detailProvesTenant)
        {
            // No authoritative projection evidence yet: keep the lifecycle state exactly as the
            // command status set it so Accepted (received/processing) and ProjectionPending
            // (events stored/completed but not yet visible) stay distinct per AC4, instead of
            // collapsing every unverified re-query into ProjectionPending. Only nudge focus to
            // the refresh recovery action.
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with
        {
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
    AddUserToTenantCommandRequest? Intent = null,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
    public static TenantAddMemberCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantAddMemberCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public TenantAddMemberCommandSnapshot RequestSent(AddUserToTenantCommandRequest intent)
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantAddMemberCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
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

    public TenantAddMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantAddMemberCommandSnapshot SignalRNudge()
        => this with
        {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantAddMemberCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence)
    {
        if (Intent is null)
        {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool memberMatches = tenantMatches
            && detailEvidence!.Members.Any(member =>
                string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal)
                && member.Role == Intent.Role);

        if (!memberMatches)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedMemberProjection = detailEvidence,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantChangeRoleCommandSnapshot(
    TenantCommandLifecycleState State,
    ChangeUserRoleCommandRequest? Intent = null,
    TenantRole CurrentConfirmedRole = TenantRole.Unknown,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    int OwnerCount = 0,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
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
        ChangeUserRoleCommandRequest intent,
        TenantRole currentConfirmedRole,
        int ownerCount)
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantChangeRoleCommandSnapshot AlreadyApplied(
        ChangeUserRoleCommandRequest intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        string safeMessage)
        => this with
        {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            SafeMessage = safeMessage,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantChangeRoleCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
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

    public TenantChangeRoleCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed when status.EventCount == 0
                => this with
                {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    SafeMessage = "The requested role was already applied.",
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantChangeRoleCommandSnapshot SignalRNudge()
        => this with
        {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantChangeRoleCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence)
    {
        if (Intent is null)
        {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantMember? targetMember = detailEvidence!.Members.FirstOrDefault(member =>
            string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal));
        if (targetMember is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "The member projection no longer contains the target user.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        if (targetMember.Role != Intent.NewRole)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with
        {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedMemberProjection = detailEvidence,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }
}

public sealed record TenantRemoveMemberCommandSnapshot(
    TenantCommandLifecycleState State,
    RemoveUserFromTenantCommandRequest? Intent = null,
    TenantRole CurrentConfirmedRole = TenantRole.Unknown,
    int OwnerCount = 0,
    bool TargetGlobalAdministratorFriction = false,
    bool IsPreviewComplete = false,
    TenantDetailProjection? LastConfirmedMemberProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
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
        RemoveUserFromTenantCommandRequest intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        bool targetGlobalAdministratorFriction,
        TenantDetailProjection lastConfirmedMemberProjection)
        => this with
        {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            TargetGlobalAdministratorFriction = targetGlobalAdministratorFriction,
            IsPreviewComplete = true,
            LastConfirmedMemberProjection = lastConfirmedMemberProjection,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Submit,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot AlreadyApplied(
        RemoveUserFromTenantCommandRequest intent,
        TenantRole currentConfirmedRole,
        int ownerCount,
        TenantDetailProjection? lastConfirmedMemberProjection,
        string safeMessage)
        => this with
        {
            State = TenantCommandLifecycleState.AlreadyApplied,
            Intent = intent,
            CurrentConfirmedRole = currentConfirmedRole,
            OwnerCount = ownerCount,
            LastConfirmedMemberProjection = lastConfirmedMemberProjection,
            SafeMessage = safeMessage,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with
        {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantRemoveMemberCommandSnapshot RequestSent()
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
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

    public TenantRemoveMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantRemoveMemberCommandSnapshot SignalRNudge()
        => this with
        {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantRemoveMemberCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence)
    {
        if (Intent is null)
        {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        if (!tenantMatches)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        bool targetStillPresent = detailEvidence!.Members.Any(member =>
            string.Equals(member.UserId, Intent.UserId, StringComparison.Ordinal));

        if (targetStillPresent)
        {
            return this with
            {
                LastConfirmedMemberProjection = detailEvidence,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        if (State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.ProjectionPending)
        {
            return this with
            {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = null,
                RejectionCode = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (State is TenantCommandLifecycleState.Rejected
            && string.Equals(RejectionCode, "UserNotInTenant", StringComparison.Ordinal))
        {
            return this with
            {
                State = TenantCommandLifecycleState.AlreadyApplied,
                LastConfirmedMemberProjection = detailEvidence,
                SafeMessage = "Projection evidence confirms the target user is already absent; no command result or audit proof is asserted.",
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
    UpdateTenantCommandRequest? Intent = null,
    string? LastConfirmedName = null,
    string? LastConfirmedDescription = null,
    TenantDetailProjection? LastConfirmedDetailProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite)
{
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

    public TenantUpdateMetadataCommandSnapshot RequestSent(UpdateTenantCommandRequest intent)
        => this with
        {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantUpdateMetadataCommandSnapshot Accepted(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return this with
        {
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

    public TenantUpdateMetadataCommandSnapshot ApplyStatus(TenantCommandStatusResult status)
    {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null)
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch
        {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Rejected
                => this with
                {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditState.AuditUnavailable,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with
                {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with
                {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    AuditState = TenantCommandAuditState.AuditDelayed,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantUpdateMetadataCommandSnapshot SignalRNudge()
        => this with
        {
            State = State is TenantCommandLifecycleState.Accepted or TenantCommandLifecycleState.RequestSent
                ? TenantCommandLifecycleState.ProjectionPending
                : State,
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantUpdateMetadataCommandSnapshot ConfirmProjection(TenantDetailProjection? detailEvidence)
    {
        if (Intent is null)
        {
            return this;
        }

        if (State is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this;
        }

        bool tenantMatches = string.Equals(detailEvidence?.TenantId, Intent.TenantId, StringComparison.Ordinal);
        bool metadataMatches = tenantMatches
            && string.Equals(detailEvidence!.Name, Intent.Name, StringComparison.Ordinal)
            && string.Equals(detailEvidence.Description, Intent.Description, StringComparison.Ordinal);

        if (!metadataMatches)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        return this with
        {
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
