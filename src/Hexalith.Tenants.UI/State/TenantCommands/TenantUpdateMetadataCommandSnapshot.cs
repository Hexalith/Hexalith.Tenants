using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.UI.State.TenantAudit;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

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
    internal string? LastConfirmedCallerScope { get; init; }

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
            AuditState: TenantCommandAuditStates.NotStarted,
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
            AuditState = TenantCommandAuditStates.NotStarted,
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
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantUpdateMetadataCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        if (State is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.AlreadyApplied or TenantCommandLifecycleState.Failed)
        {
            return this;
        }


        if ((status.IsPending && status.IsRetryableFailure)
            || (status.Status is not null && (status.IsPending || status.IsRetryableFailure))
            || (status.Status is not null && !status.HasVerifiedCommandIdentity))
        {
            return this with
            {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                RejectionCode = null,
                AuditState = TenantCommandAuditStates.Unverifiable,
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        if (status.IsPending || status.IsRetryableFailure)
        {
            return this with
            {
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                AuditState = TenantCommandAuditStates.FromStatusLookup(status, AuditState),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        if (status.Status is null) {
            return this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = status.SafeMessage,
                SafeMessageKey = status.SafeMessageKey,
                // A 404 before the first status is a wait that keeps the audit state; any other missing status is unverifiable.
                AuditState = TenantCommandAuditStates.FromStatusLookup(status, AuditState),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with {
                    State = HasCommandEventEvidence ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = true,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.Completed
                => this with {
                    State = TenantCommandLifecycleState.ProjectionPending,
                    HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0,
                    SafeMessage = null,
                    SafeMessageKey = null,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
                },
            CommandStatus.Rejected
                => this with {
                    State = TenantCommandLifecycleState.Rejected,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    RejectionCode = status.RejectionCode,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.PublishFailed
                => this with {
                    State = TenantCommandLifecycleState.Degraded,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            CommandStatus.TimedOut
                => this with {
                    State = TenantCommandLifecycleState.UnableToVerify,
                    SafeMessage = status.SafeMessage,
                    SafeMessageKey = status.SafeMessageKey,
                    AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                },
            _ => this with {
                State = TenantCommandLifecycleState.UnableToVerify,
                SafeMessage = "Command status could not be verified.",
                SafeMessageKey = status.SafeMessageKey ?? "Tenants.Commands.Status.Unverified",
                AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence),
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            },
        };
    }

    public TenantUpdateMetadataCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantUpdateMetadataCommandSnapshot ConfirmProjection(
        TenantDetailSnapshot? proof,
        TenantAuditRow? auditEvidence = null) {
        if (Intent is null) {
            return this;
        }

        // Accepted alone never confirms. ProjectionPending is also reachable via SignalRNudge, which carries no
        // status evidence, so causal confirmation additionally requires HasCommandEventEvidence below.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        if (proof is null || !TenantMembershipCommandProvenance.IsAuthoritative(proof))
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantDetailProjection? detailEvidence = proof!.Detail;
        string? currentProjectionVersion = proof.ProjectionVersion;
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
                // The projection was read but did not prove this attempt, and no audit read happened: AuditState is kept.
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
                // The projection was read but did not prove this attempt, and no audit read happened: AuditState is kept.
                FocusTarget = TenantCommandFocusTarget.Refresh,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
            };
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedName = detailEvidence!.Name,
            LastConfirmedDescription = detailEvidence.Description,
            LastConfirmedDetailProjection = detailEvidence,
            LastConfirmedCallerScope = proof.CallerScope,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            // Audit availability comes only from a complete, redacted Ready receipt built from the row that
            // matched this attempt. Provenance alone confirms the projection, never the audit dimension.
            AuditState = TenantCommandAuditStates.FromConfirmationEvidence(
                hasQualifyingAuditProvenance ? TenantAuditReceipt.FromRow(auditEvidence!) : null),
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
