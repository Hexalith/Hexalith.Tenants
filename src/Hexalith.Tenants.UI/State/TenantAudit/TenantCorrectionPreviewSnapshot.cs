using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public sealed record TenantCorrectionProofLink(
    string OriginalAuditReference,
    string CorrectiveAuditReference,
    DateTimeOffset OriginalTimestamp,
    DateTimeOffset CorrectiveTimestamp,
    string Narrative);

public sealed record TenantCorrectionPreviewSnapshot(
    TenantCorrectionStartIntent Intent,
    string OriginalAuditReference,
    string CurrentProjectionSnapshotReference,
    string TenantId,
    string TargetUserId,
    TenantRole CurrentRole,
    TenantRole IntendedRole,
    string IntendedCommandDomain,
    string IntendedCommandType,
    IReadOnlyList<string> KnownConsequences,
    IReadOnlyList<string> KnownUnknowns,
    string AuditEvidenceExpectation,
    string RecoveryPath,
    TenantDetailProjection? LastConfirmedProjectionEvidence,
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
    public bool CanSubmit
        => Intent.IsAvailable
            && LifecycleState is TenantCommandLifecycleState.Previewed
            && IntendedRole is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;

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

    public static TenantCorrectionPreviewSnapshot FromIntent(
        TenantCorrectionStartIntent intent,
        TenantDetailProjection? currentProjection = null) {
        ArgumentNullException.ThrowIfNull(intent);

        TenantRole intendedRole = intent.IntendedRole ?? TenantRole.Unknown;
        TenantRole currentRole = RequiredRole(intent, "currentRole");
        string tenantId = RequiredInput(intent, "tenantId");
        string targetUserId = RequiredInput(intent, "userId");

        TenantCorrectionPreviewSnapshot snapshot = new(
            intent,
            intent.OriginalAuditReference,
            intent.CurrentProjectionSnapshotReference,
            tenantId,
            targetUserId,
            currentRole,
            intendedRole,
            intent.IntendedCommandDomain?.ToString() ?? string.Empty,
            intent.IntendedCommandType?.ToString() ?? string.Empty,
            KnownConsequencesFor(intent),
            KnownUnknownsFor(intent),
            "Corrective audit evidence is expected after the command is accepted and projection truth confirms the intended state.",
            "Refresh status, inspect audit evidence, continue read-only, or start a different correction if current projection truth conflicts.",
            null,
            null,
            null,
            null,
            intent.IsAvailable ? TenantCommandLifecycleState.Previewed : TenantCommandLifecycleState.UnableToVerify,
            intent.IsAvailable ? TenantCommandAuditState.NotStarted : TenantCommandAuditState.MissingSupport,
            intent.IsAvailable ? TenantCommandFocusTarget.Submit : TenantCommandFocusTarget.Role,
            intent.IsAvailable ? TenantCommandLiveRegionPoliteness.Polite : TenantCommandLiveRegionPoliteness.Assertive,
            null);

        return currentProjection is null ? snapshot : snapshot.EvaluateCurrentProjection(currentProjection);
    }

    public TenantCorrectionPreviewSnapshot EvaluateCurrentProjection(TenantDetailProjection projection) {
        ArgumentNullException.ThrowIfNull(projection);

        if (!string.Equals(projection.TenantId, TenantId, StringComparison.Ordinal)
            || projection.Status is TenantStatus.Disabled or TenantStatus.Unknown) {
            return this with {
                LastConfirmedProjectionEvidence = projection,
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.Unavailable.CurrentProjectionUnavailable",
            };
        }

        TenantMember? member = projection.Members.FirstOrDefault(member =>
            string.Equals(member.UserId, TargetUserId, StringComparison.Ordinal));
        TenantRole currentRole = member?.Role ?? TenantRole.Unknown;
        TenantCorrectionPreviewSnapshot withEvidence = this with {
            LastConfirmedProjectionEvidence = projection,
            CurrentRole = currentRole,
        };

        if (Intent.IntendedCommandType is TenantCorrectionCommandType.AddUserToTenant && member is not null) {
            return member.Role == IntendedRole
                ? withEvidence with {
                    LifecycleState = TenantCommandLifecycleState.AlreadyApplied,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Correction.Unavailable.AlreadyApplied",
                }
                : withEvidence with {
                    LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Role,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Correction.Unavailable.CurrentRoleConflict",
                };
        }

        if (Intent.IntendedCommandType is TenantCorrectionCommandType.ChangeUserRole) {
            if (member is null) {
                return withEvidence with {
                    LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Correction.Unavailable.CurrentStateIndeterminate",
                };
            }

            if (member.Role == IntendedRole) {
                return withEvidence with {
                    LifecycleState = TenantCommandLifecycleState.AlreadyApplied,
                    AuditState = TenantCommandAuditState.MissingSupport,
                    FocusTarget = TenantCommandFocusTarget.Lifecycle,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.Correction.Unavailable.AlreadyApplied",
                };
            }
        }

        return withEvidence;
    }

    public TenantCorrectionPreviewSnapshot RequestSent()
        => this with {
            LifecycleState = TenantCommandLifecycleState.RequestSent,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantCorrectionPreviewSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public TenantCorrectionPreviewSnapshot ApplySubmissionFailure(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            LifecycleState = result.State,
            SafeMessage = result.SafeMessage,
            RejectionCode = result.RejectionCode,
            AuditState = TenantCommandAuditState.AuditUnavailable,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
    }

    public TenantCorrectionPreviewSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
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
                        SafeMessageKey = "Tenants.Correction.State.AlreadyApplied",
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
                        RejectionCode = status.RejectionCode,
                        AuditState = TenantCommandAuditState.AuditUnavailable,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.PublishFailed
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.Degraded,
                        SafeMessage = status.SafeMessage,
                        AuditState = TenantCommandAuditState.AuditUnavailable,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.TimedOut
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                        SafeMessage = status.SafeMessage,
                        AuditState = TenantCommandAuditState.AuditUnavailable,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            _ => this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.State.UnableToVerify",
                AuditState = TenantCommandAuditState.AuditUnavailable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantCorrectionPreviewSnapshot ConfirmProjection(TenantDetailProjection? projection) {
        if (projection is null
            || LifecycleState is not TenantCommandLifecycleState.Accepted and not TenantCommandLifecycleState.ProjectionPending) {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantMember? member = projection.Members.FirstOrDefault(member =>
            string.Equals(member.UserId, TargetUserId, StringComparison.Ordinal));
        bool projectionProvesCorrection = string.Equals(projection.TenantId, TenantId, StringComparison.Ordinal)
            && member?.Role == IntendedRole;

        if (!projectionProvesCorrection) {
            return this with {
                LastConfirmedProjectionEvidence = projection,
                FocusTarget = TenantCommandFocusTarget.Refresh,
            };
        }

        return this with {
            LifecycleState = TenantCommandLifecycleState.Confirmed,
            LastConfirmedProjectionEvidence = projection,
            CurrentRole = member!.Role,
            SafeMessage = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantCorrectionPreviewSnapshot WithCorrectiveProof(TenantAuditRow? row) {
        if (LifecycleState is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (row is null) {
            return this with { AuditState = TenantCommandAuditState.AuditDelayed };
        }

        return this with {
            AuditState = TenantCommandAuditState.NotStarted,
            ProofLink = new(
                OriginalAuditReference,
                row.EventReference,
                Intent.RequiredPreviewInputs.TryGetValue("originalTimestamp", out string? originalTimestamp)
                    && DateTimeOffset.TryParse(originalTimestamp, out DateTimeOffset parsed)
                        ? parsed
                        : row.Timestamp,
                row.Timestamp,
                row.ReferenceContext),
        };
    }

    private static string RequiredInput(TenantCorrectionStartIntent intent, string key)
        => intent.RequiredPreviewInputs.TryGetValue(key, out string? value) ? value : string.Empty;

    private static TenantRole RequiredRole(TenantCorrectionStartIntent intent, string key)
        => Enum.TryParse(RequiredInput(intent, key), out TenantRole role) ? role : TenantRole.Unknown;

    private static IReadOnlyList<string> KnownConsequencesFor(TenantCorrectionStartIntent intent)
        => intent.IntendedCommandType switch {
            TenantCorrectionCommandType.AddUserToTenant => ["A new membership event may be appended if current projection truth allows it."],
            TenantCorrectionCommandType.ChangeUserRole => ["A new role-change event may be appended if current projection truth allows it."],
            _ => ["No tenant-domain corrective command will be submitted without reusable support."],
        };

    private static IReadOnlyList<string> KnownUnknownsFor(TenantCorrectionStartIntent intent)
        => intent.IntendedCommandType switch {
            TenantCorrectionCommandType.AddUserToTenant => ["Historical role evidence can be stale; the selected intended role is authoritative for the new command."],
            TenantCorrectionCommandType.ChangeUserRole => ["SignalR notifications can nudge a refresh but do not prove correction success."],
            _ => ["Global administrator command support is unavailable in this UI surface."],
        };
}
