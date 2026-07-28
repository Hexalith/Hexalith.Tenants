using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.GlobalAdministrators;

public sealed record GlobalAdministratorRemoveCommandSnapshot(
    TenantCommandLifecycleState State,
    RemoveGlobalAdministrator? Intent = null,
    IReadOnlyList<GlobalAdministratorRow>? PreviewRows = null,
    GlobalAdministratorRow? LastConfirmedProjection = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? SafeMessage = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static GlobalAdministratorRemoveCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static GlobalAdministratorRemoveCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditState.MissingSupport,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    public GlobalAdministratorRemoveCommandSnapshot Preview(
        RemoveGlobalAdministrator intent,
        IReadOnlyList<GlobalAdministratorRow> rows) {
        ArgumentNullException.ThrowIfNull(intent);
        ArgumentNullException.ThrowIfNull(rows);

        GlobalAdministratorRow? target = rows.FirstOrDefault(row => string.Equals(row.UserId, intent.UserId, StringComparison.Ordinal));
        if (target is null) {
            return Blocked("The target global administrator is not visible in the current projection. Refresh before removing platform authority.", TenantCommandFocusTarget.Refresh)
                with { Intent = intent, PreviewRows = rows };
        }

        if (rows.Count <= 1) {
            return Blocked("The last global administrator cannot be removed.", TenantCommandFocusTarget.Submit)
                with { Intent = intent, PreviewRows = rows, LastConfirmedProjection = target };
        }

        return this with {
            State = TenantCommandLifecycleState.Previewed,
            Intent = intent,
            PreviewRows = rows,
            LastConfirmedProjection = target,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public GlobalAdministratorRemoveCommandSnapshot RequestSent()
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public GlobalAdministratorRemoveCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public GlobalAdministratorRemoveCommandSnapshot ApplySubmission(TenantCommandSubmissionResult result) {
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

    public GlobalAdministratorRemoveCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
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

    public GlobalAdministratorRemoveCommandSnapshot SignalRNudge()
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

    public GlobalAdministratorRemoveCommandSnapshot ConfirmProjection(GlobalAdministratorsSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);

        if (Intent is null || State is TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.Failed
            or TenantCommandLifecycleState.Degraded
            or TenantCommandLifecycleState.UnableToVerify) {
            return this;
        }

        GlobalAdministratorRow? row = snapshot.Rows.FirstOrDefault(row => string.Equals(row.UserId, Intent.UserId, StringComparison.Ordinal));
        if (row is null && snapshot.IsCompleteEvidence) {
            return this with {
                State = TenantCommandLifecycleState.Confirmed,
                LastConfirmedProjection = null,
                SafeMessage = null,
                AuditState = TenantCommandAuditState.AuditPending,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (row is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedProjection = null,
                SafeMessage = "Current complete projection evidence is required before confirming global administrator removal.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return State is TenantCommandLifecycleState.ProjectionPending
            ? this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                LastConfirmedProjection = row,
                SafeMessage = "Projection re-query still shows the target global administrator. Do not treat removal as complete.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            }
            : this;
    }
}
