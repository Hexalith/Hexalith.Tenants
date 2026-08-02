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
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    /// <summary>
    /// Returns a support-safe description that omits identities, correlation data, and message identifiers.
    /// </summary>
    /// <returns>A bounded support-safe command-snapshot description.</returns>
    public override string ToString()
        => $"{nameof(GlobalAdministratorGrantCommandSnapshot)} {{ State = {State}, HasIntent = {Intent is not null}, AuditState = {AuditState}, RejectionCode = {RejectionCode}, FocusTarget = {FocusTarget}, LiveRegionPoliteness = {LiveRegionPoliteness} }}";

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

        // IsMutationEvidenceBacked, not Freshness alone: a Current classified only from the legacy
        // X-Hexalith-Is-Stale compatibility signal carries no projection lifecycle evidence, so it cannot
        // certify that the grant reached the projection.
        if (!snapshot.IsMutationEvidenceBacked
            || snapshot.Kind is not (GlobalAdministratorsSurfaceKind.Ready or GlobalAdministratorsSurfaceKind.Empty)) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Current projection evidence is required before confirming the global administrator grant.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
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

                // The re-query reads page one only. On a deployment with more global administrators than one
                // page holds, a granted user id that sorts past the page boundary is never in this payload,
                // so "did not confirm" would report a permanent false negative for an outcome this page
                // simply cannot see. Both arms stay UnableToVerify -- the difference is honesty about why.
                SafeMessage = snapshot.IsCompleteEvidence
                    ? "Projection re-query did not confirm the target global administrator."
                    : "The projection re-query covers only the first page of global administrators, which does not include this user, so the grant cannot be confirmed from this page. Confirm the outcome from the tenant audit trail.",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            }
            : this;
    }
}
