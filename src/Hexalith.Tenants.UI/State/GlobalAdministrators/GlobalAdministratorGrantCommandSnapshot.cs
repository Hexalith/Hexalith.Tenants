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
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static GlobalAdministratorGrantCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static GlobalAdministratorGrantCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public GlobalAdministratorGrantCommandSnapshot RequestSent(SetGlobalAdministrator intent)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            LastConfirmedProjection = null,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public GlobalAdministratorGrantCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public GlobalAdministratorGrantCommandSnapshot ApplySubmission(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return result.State is TenantCommandLifecycleState.Accepted
            ? Accepted(result)
            : this with {
                State = result.State,
                SafeMessage = result.SafeMessage,
                RejectionCode = result.RejectionCode,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
    }

    public GlobalAdministratorGrantCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
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
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
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
            _ => this,
        };
    }

    public GlobalAdministratorGrantCommandSnapshot SignalRNudge()
        => State is TenantCommandLifecycleState.Accepted
            or TenantCommandLifecycleState.RequestSent
            or TenantCommandLifecycleState.ProjectionPending
            ? this with {
                State = TenantCommandLifecycleState.ProjectionPending,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            }
            : this;

    public GlobalAdministratorGrantCommandSnapshot ConfirmProjection(GlobalAdministratorsSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Intent is null || State is TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Degraded
            or TenantCommandLifecycleState.UnableToVerify) {
            return this;
        }

        GlobalAdministratorRow? row = snapshot.Rows.FirstOrDefault(row => string.Equals(row.UserId, Intent.UserId, StringComparison.Ordinal));
        if (row is not null) {
            return this with {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfirmedProjection = row,
                SafeMessage = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return State is TenantCommandLifecycleState.ProjectionPending
            ? this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Projection re-query did not confirm the target global administrator.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            }
            : this;
    }
}
