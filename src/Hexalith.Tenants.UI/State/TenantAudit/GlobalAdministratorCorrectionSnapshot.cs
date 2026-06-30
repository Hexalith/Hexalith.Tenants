using Hexalith.EventStore.Client.Projections;
using Hexalith.Tenants.UI.State.GlobalAdministrators;
using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantAudit;

// Focused, tenant-role-free correction state for the two fixed global-administrator outcomes:
// GlobalAdministratorRemoved -> SetGlobalAdministrator (restore) and
// GlobalAdministratorSet -> RemoveGlobalAdministrator (revoke). It deliberately mirrors the proven
// TenantCorrectionPreviewSnapshot lifecycle/audit/focus enums but confirms only against the fixed
// global-administrator projection (system / global-administrators / global-administrators) and keeps
// the last-administrator hard stop, so platform authority recovery is never inferred from tenant
// detail, tenant members, or tenant role selection.
public sealed record GlobalAdministratorCorrectionSnapshot(
    TenantCorrectionStartIntent Intent,
    string OriginalAuditReference,
    string TargetUserId,
    TenantCorrectionCommandType CommandType,
    string CurrentProjectionSnapshotReference,
    int CurrentAdministratorCount,
    bool TargetCurrentlyPresent,
    GlobalAdministratorsSnapshot? LastConfirmedProjectionEvidence,
    TenantCorrectionProofLink? ProofLink,
    string? MessageId,
    string? CorrelationId,
    TenantCommandLifecycleState LifecycleState,
    TenantCommandAuditState AuditState,
    TenantCommandFocusTarget FocusTarget,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness,
    string? SafeMessage = null,
    string? RejectionCode = null,
    string? SafeMessageKey = null) {
    public bool IsRestoreAccessAction
        => CommandType is TenantCorrectionCommandType.SetGlobalAdministrator;

    public bool CanSubmit
        => Intent.IsAvailable && LifecycleState is TenantCommandLifecycleState.Previewed;

    public bool HasCommandTracking
        => MessageId is not null && CorrelationId is not null;

    public bool TryGetTrackingHandle(out TenantCommandTrackingHandle handle) {
        if (MessageId is not null && CorrelationId is not null) {
            handle = new(MessageId, CorrelationId);
            return true;
        }

        handle = new(string.Empty, string.Empty);
        return false;
    }

    public static GlobalAdministratorCorrectionSnapshot FromIntent(
        TenantCorrectionStartIntent intent,
        GlobalAdministratorsSnapshot? currentProjection = null) {
        ArgumentNullException.ThrowIfNull(intent);

        // Fail closed if the audit evidence did not resolve a concrete global-administrator command
        // type: defaulting an unknown intent to SetGlobalAdministrator would fail open toward GRANTING
        // platform authority, so an unknown command type stays unavailable and unsubmittable (AC8).
        TenantCorrectionCommandType? intendedCommandType = intent.IntendedCommandType;
        TenantCorrectionCommandType commandType = intendedCommandType
            ?? TenantCorrectionCommandType.SetGlobalAdministrator;
        bool available = intent.IsAvailable && intendedCommandType is not null;
        string targetUserId = intent.RequiredPreviewInputs.TryGetValue("userId", out string? userId)
            ? userId
            : intent.TargetUserId;

        GlobalAdministratorCorrectionSnapshot snapshot = new(
            intent,
            intent.OriginalAuditReference,
            targetUserId,
            commandType,
            intent.CurrentProjectionSnapshotReference,
            CurrentAdministratorCount: 0,
            TargetCurrentlyPresent: false,
            LastConfirmedProjectionEvidence: null,
            ProofLink: null,
            MessageId: null,
            CorrelationId: null,
            available ? TenantCommandLifecycleState.Previewed : TenantCommandLifecycleState.UnableToVerify,
            available ? TenantCommandAuditState.NotStarted : TenantCommandAuditState.MissingSupport,
            available ? TenantCommandFocusTarget.Submit : TenantCommandFocusTarget.Lifecycle,
            available ? TenantCommandLiveRegionPoliteness.Polite : TenantCommandLiveRegionPoliteness.Assertive,
            SafeMessage: null);

        return currentProjection is null ? snapshot : snapshot.EvaluateCurrentProjection(currentProjection);
    }

    public GlobalAdministratorCorrectionSnapshot EvaluateCurrentProjection(GlobalAdministratorsSnapshot projection) {
        ArgumentNullException.ThrowIfNull(projection);

        // An unavailable intent must keep its fail-closed lifecycle: never let an already-applied
        // projection state overwrite the real unavailability reason. The command is unsubmittable
        // regardless (CanSubmit gates on Intent.IsAvailable), but the copy must explain WHY it cannot run.
        if (!Intent.IsAvailable) {
            return this;
        }

        // The start gate already requires a current fixed projection, but the panel must still fail
        // closed if the projection it is handed cannot prove platform authority state honestly.
        if (!ProjectionIsReadable(projection)) {
            return this with {
                LastConfirmedProjectionEvidence = projection,
                CurrentAdministratorCount = projection.Rows.Count,
                TargetCurrentlyPresent = false,
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.Unavailable.CurrentProjectionUnavailable",
            };
        }

        bool present = TargetPresent(projection);
        int count = DistinctAdministratorCount(projection);
        GlobalAdministratorCorrectionSnapshot withEvidence = this with {
            LastConfirmedProjectionEvidence = projection,
            CurrentAdministratorCount = count,
            TargetCurrentlyPresent = present,
        };

        if (CommandType is TenantCorrectionCommandType.SetGlobalAdministrator) {
            // Restore: success is the target appearing in the fixed projection, so a target that is
            // already present is already applied and must not become a second grant command.
            return present
                ? withEvidence with {
                    LifecycleState = TenantCommandLifecycleState.AlreadyApplied,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Correction.GlobalAdmin.AlreadyGranted",
                }
                : withEvidence;
        }

        // Revoke: a target that is already absent is already applied; otherwise the last
        // global administrator is a hard stop before submit (AC6) and must not be submittable.
        if (!present) {
            return withEvidence with {
                LifecycleState = TenantCommandLifecycleState.AlreadyApplied,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.GlobalAdmin.AlreadyRemoved",
            };
        }

        if (count <= 1) {
            return withEvidence with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.GlobalAdmin.LastAdministrator",
            };
        }

        return withEvidence;
    }

    public GlobalAdministratorCorrectionSnapshot RequestSent()
        => this with {
            LifecycleState = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public GlobalAdministratorCorrectionSnapshot Accepted(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            LifecycleState = TenantCommandLifecycleState.Accepted,
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

    public GlobalAdministratorCorrectionSnapshot ApplySubmissionFailure(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            LifecycleState = result.State,
            SafeMessage = result.SafeMessage,
            SafeMessageKey = null,
            RejectionCode = result.RejectionCode,
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
    }

    public GlobalAdministratorCorrectionSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            Hexalith.EventStore.Contracts.Commands.CommandStatus.Received
                or Hexalith.EventStore.Contracts.Commands.CommandStatus.Processing
                    => this with { LifecycleState = TenantCommandLifecycleState.Accepted, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed when status.EventCount == 0
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.AlreadyApplied,
                        SafeMessage = null,
                        SafeMessageKey = "Tenants.Correction.GlobalAdmin.State.AlreadyApplied",
                        AuditState = TenantCommandAuditState.MissingSupport,
                        FocusTarget = TenantCommandFocusTarget.Lifecycle,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.EventsStored
                or Hexalith.EventStore.Contracts.Commands.CommandStatus.EventsPublished
                or Hexalith.EventStore.Contracts.Commands.CommandStatus.Completed
                    => this with { LifecycleState = TenantCommandLifecycleState.ProjectionPending, AuditState = TenantCommandAuditState.AuditPending, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.Rejected
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.Rejected,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        RejectionCode = status.RejectionCode,
                        AuditState = TenantCommandAuditState.AuditUnavailable,
                        FocusTarget = TenantCommandFocusTarget.Lifecycle,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.PublishFailed
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.Degraded,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        AuditState = TenantCommandAuditState.AuditDelayed,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.TimedOut
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        AuditState = TenantCommandAuditState.AuditDelayed,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            _ => this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.GlobalAdmin.State.UnableToVerify",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public GlobalAdministratorCorrectionSnapshot ConfirmProjection(GlobalAdministratorsSnapshot? projection) {
        // Confirmation may only come from an authoritative, current, genuinely-readable projection.
        // An Empty/auth-scoped-empty or Stale projection is never proof of a successful correction:
        // a successful revoke can never empty the fixed projection (the last administrator is blocked
        // before submit), so treating "target absent from an empty/unauthorized read" as success would
        // be a fail-open. This mirrors the start gate's current-freshness requirement (AC5/AC6/AC8).
        if (projection is null
            || projection.Kind is not GlobalAdministratorsSurfaceKind.Ready
            || projection.Freshness is not ReadModelFreshnessState.Current
            || LifecycleState is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        bool present = TargetPresent(projection);
        bool projectionProvesCorrection = IsRestoreAccessAction ? present : !present;

        if (!projectionProvesCorrection) {
            return this with {
                LastConfirmedProjectionEvidence = projection,
                CurrentAdministratorCount = DistinctAdministratorCount(projection),
                TargetCurrentlyPresent = present,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        return this with {
            LifecycleState = TenantCommandLifecycleState.Confirmed,
            LastConfirmedProjectionEvidence = projection,
            CurrentAdministratorCount = DistinctAdministratorCount(projection),
            TargetCurrentlyPresent = present,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public GlobalAdministratorCorrectionSnapshot WithCorrectiveProof(TenantAuditRow? row) {
        if (LifecycleState is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (row is null
            || !TryParseOriginalTimestamp(Intent, out DateTimeOffset originalTimestamp)
            || row.Timestamp <= originalTimestamp) {
            return this with { AuditState = TenantCommandAuditState.AuditDelayed };
        }

        return this with {
            AuditState = TenantCommandAuditState.NotStarted,
            ProofLink = new(
                OriginalAuditReference,
                row.EventReference,
                originalTimestamp,
                row.Timestamp,
                row.ReferenceContext),
        };
    }

    // The corrective event the system-scope audit must surface to prove the correction: a restore is
    // proven by a GlobalAdministratorSet row, a revoke by a GlobalAdministratorRemoved row.
    public string CorrectiveEventType
        => IsRestoreAccessAction ? "GlobalAdministratorSet" : "GlobalAdministratorRemoved";

    private bool TargetPresent(GlobalAdministratorsSnapshot projection)
        => projection.Rows.Any(row => string.Equals(row.UserId, TargetUserId, StringComparison.Ordinal));

    // Last-administrator safety counts distinct administrator identities, not raw rows, so a duplicate
    // projection row for one user can never inflate the count past the hard-stop threshold (AC6).
    private static int DistinctAdministratorCount(GlobalAdministratorsSnapshot projection)
        => projection.Rows.Select(row => row.UserId).Distinct(StringComparer.Ordinal).Count();

    // Fail closed for a SUBMITTABLE preview exactly the way the confirm gate does: a Stale or non-current
    // fixed projection is never honest evidence for a platform-authority mutation. Only a genuinely
    // current Ready/Empty read can drive a submittable correction; Empty-current is kept so the
    // first-administrator restore path stays reachable (start gate + ConfirmProjection require Current too).
    private static bool ProjectionIsReadable(GlobalAdministratorsSnapshot projection)
        => projection.Kind is GlobalAdministratorsSurfaceKind.Ready or GlobalAdministratorsSurfaceKind.Empty
        && projection.Freshness is ReadModelFreshnessState.Current;

    private static bool TryParseOriginalTimestamp(
        TenantCorrectionStartIntent intent,
        out DateTimeOffset originalTimestamp)
    {
        if (!intent.RequiredPreviewInputs.TryGetValue("originalTimestamp", out string? value))
        {
            originalTimestamp = default;
            return false;
        }

        return DateTimeOffset.TryParse(
            value,
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind,
            out originalTimestamp);
    }
}
