using System.Globalization;

using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantCommands;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;

namespace Hexalith.Tenants.UI.State.TenantAudit;

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
    /// <summary>Gets the ordered version read with current authority before dispatch.</summary>
    public string? BaselineProjectionVersion { get; init; }

    /// <summary>Gets the time at which the retained command attempt began.</summary>
    public DateTimeOffset? AttemptStartedAtUtc { get; init; }

    /// <summary>Gets the redacted direct read that confirmed the intended state and ordered advance.</summary>
    public TenantCorrectionProjection? LastConfirmedCorrectionProjection { get; init; }

    /// <summary>Gets the complete original evidence time only when its round-trip value is valid.</summary>
    public DateTimeOffset? OriginalTimestampUtc
        => TryGetOriginalTimestamp(Intent, out DateTimeOffset timestamp)
            ? timestamp.ToUniversalTime()
            : null;

    public bool CanSubmit
        => Intent.IsAvailable
            && LifecycleState is TenantCommandLifecycleState.Previewed
            && Intent.CurrentProjection is { IsCurrent: true, IsAuthorized: true }
            && TenantLifecycleProjectionVersion.IsOrdered(BaselineProjectionVersion)
            && OriginalTimestampUtc is not null
            && IntendedRole is TenantRole.TenantOwner or TenantRole.TenantContributor or TenantRole.TenantReader;

    /// <summary>Omits ordered versions, identities, and retained read details from copied diagnostics.</summary>
    public override string ToString()
        => $"{nameof(TenantCorrectionPreviewSnapshot)} {{ LifecycleState = {LifecycleState}, AuditState = {AuditState}, HasCommandTracking = {HasCommandTracking} }}";

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
        bool hasOriginalTime = TryGetOriginalTimestamp(intent, out _);
        bool canPreview = intent.IsAvailable && hasOriginalTime;

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
            canPreview ? TenantCommandLifecycleState.Previewed : TenantCommandLifecycleState.UnableToVerify,
            canPreview ? TenantCommandAuditState.NotStarted : TenantCommandAuditState.MissingSupport,
            canPreview ? TenantCommandFocusTarget.Submit : TenantCommandFocusTarget.Role,
            canPreview ? TenantCommandLiveRegionPoliteness.Polite : TenantCommandLiveRegionPoliteness.Assertive,
            null,
            SafeMessageKey: hasOriginalTime ? null : "Tenants.Correction.Unavailable.OriginalTimeUnavailable");

        snapshot = snapshot with { BaselineProjectionVersion = intent.CurrentProjection?.ProjectionVersion };
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
            AuditState = TenantCommandAuditState.MissingSupport,
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
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantCorrectionPreviewSnapshot ApplySubmissionFailure(TenantCommandSubmissionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return this with {
            LifecycleState = result.State,
            SafeMessage = result.SafeMessage,
            SafeMessageKey = result.SafeMessageKey,
            RejectionCode = result.RejectionCode,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = result.State is TenantCommandLifecycleState.Failed
                ? TenantCommandFocusTarget.Lifecycle
                : TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
    }

    public TenantCorrectionPreviewSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (status.Status is null) {
            return this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = null,
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            Hexalith.EventStore.Contracts.Commands.CommandStatus.Received
                or Hexalith.EventStore.Contracts.Commands.CommandStatus.Processing
                    => this with { LifecycleState = TenantCommandLifecycleState.Accepted, SafeMessage = null,
                        SafeMessageKey = null, LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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
                    => this with { LifecycleState = TenantCommandLifecycleState.ProjectionPending, SafeMessage = null,
                        SafeMessageKey = null, AuditState = TenantCommandAuditState.MissingSupport,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.Rejected
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.Rejected,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        RejectionCode = status.RejectionCode,
                        AuditState = TenantCommandAuditState.MissingSupport,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.PublishFailed
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.Degraded,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        AuditState = TenantCommandAuditState.MissingSupport,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            Hexalith.EventStore.Contracts.Commands.CommandStatus.TimedOut
                    => this with {
                        LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                        SafeMessage = status.SafeMessage,
                        SafeMessageKey = null,
                        AuditState = TenantCommandAuditState.MissingSupport,
                        FocusTarget = TenantCommandFocusTarget.Refresh,
                        LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                    },
            _ => this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = null,
                SafeMessageKey = "Tenants.Correction.State.UnableToVerify",
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantCorrectionPreviewSnapshot ConfirmProjection(TenantDetailProjection? projection) {
        // A detail DTO alone has no version or attempt provenance. Keep this compatibility entry
        // point fail closed for callers that have not adopted the authoritative capture.
        return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
    }

    /// <summary>Confirms only a fresh matching postcondition with ordered causal advancement.</summary>
    public TenantCorrectionPreviewSnapshot ConfirmProjection(TenantCorrectionProjection? projection)
    {
        if (projection is null)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (LifecycleState is not TenantCommandLifecycleState.Accepted
            and not TenantCommandLifecycleState.ProjectionPending)
        {
            return this;
        }

        if (projection is not { IsCurrent: true, IsAuthorized: true }
            || !string.Equals(projection.TenantId, TenantId, StringComparison.Ordinal)
            || !string.Equals(projection.TargetUserId, TargetUserId, StringComparison.Ordinal))
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (projection.CurrentRole != IntendedRole)
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (TenantLifecycleProjectionVersion.Compare(BaselineProjectionVersion, projection.ProjectionVersion)
            is not TenantLifecycleProjectionVersionComparison.Advanced)
        {
            return this with {
                LifecycleState = TenantCommandLifecycleState.UnableToVerify,
                SafeMessageKey = "Tenants.Correction.State.UnableToVerify",
                AuditState = TenantCommandAuditState.MissingSupport,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return this with {
            LifecycleState = TenantCommandLifecycleState.Confirmed,
            CurrentRole = IntendedRole,
            LastConfirmedCorrectionProjection = projection,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditState.MissingSupport,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantCorrectionPreviewSnapshot WithCorrectiveProof(TenantAuditRow? row) {
        if (LifecycleState is not TenantCommandLifecycleState.Confirmed)
        {
            return this;
        }

        // The authorized audit DTO contains an event id, not the command message id. A matching
        // target, event type and time therefore cannot establish attempt-specific association.
        return this with { AuditState = TenantCommandAuditState.MissingSupport, ProofLink = null };
    }

    private static string RequiredInput(TenantCorrectionStartIntent intent, string key)
        => intent.RequiredPreviewInputs.TryGetValue(key, out string? value) ? value : string.Empty;

    private static bool TryGetOriginalTimestamp(TenantCorrectionStartIntent intent, out DateTimeOffset timestamp)
    {
        timestamp = default;
        return intent.RequiredPreviewInputs.TryGetValue("originalTimestamp", out string? value)
            && DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out timestamp)
            && timestamp != default;
    }

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
