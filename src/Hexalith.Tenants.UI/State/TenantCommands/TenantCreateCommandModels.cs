using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record CreateTenantCommandRequest(
    string TenantId,
    string Name,
    string? Description);

public sealed record TenantCommandTrackingHandle(
    string MessageId,
    string CorrelationId);

public enum TenantCommandLifecycleState
{
    Idle,
    RequestSent,
    Accepted,
    ProjectionPending,
    Confirmed,
    Rejected,
    Failed,
    Degraded,
    UnableToVerify,
}

public enum TenantCommandAuditState
{
    NotStarted,
    AuditPending,
    AuditUnavailable,
    MissingSupport,
}

public enum TenantCommandFocusTarget
{
    TenantId,
    Name,
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
    string? RejectionCode = null)
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
