using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Enums;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantAudit;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

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
    bool HasCommandEventEvidence = false,
    DateTimeOffset? AttemptStartedAtUtc = null,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    /// <summary>Gets whether the retained membership projection wait has expired without terminal evidence.</summary>
    public bool ProjectionWaitExpired { get; init; }

    /// <summary>Gets the exact verified command's committed aggregate sequence, retained only for proof.</summary>
    internal long? CommittedEventSequence { get; init; }

    /// <summary>Gets caller provenance from the authoritative detail read that confirmed this removal.</summary>
    internal string? ProjectionProofCallerScope { get; init; }

    /// <summary>Gets the support-safe proof assembled for this exact retained removal attempt.</summary>
    internal TenantAuditReceipt? RemovalProof { get; init; }

    /// <summary>Gets the bounded count of complete audit walks without an exact removal proof.</summary>
    internal int UnmatchedRemovalProofWalkCount { get; init; }

    public static TenantRemoveMemberCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantRemoveMemberCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditStates.NotStarted,
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
            AuditState = TenantCommandAuditStates.NotStarted,
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
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantRemoveMemberCommandSnapshot DuplicatePrevented(string safeMessage)
        => this with {
            State = TenantCommandLifecycleState.DuplicatePrevented,
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantRemoveMemberCommandSnapshot RequestSent(
        string? baselineProjectionVersion = null,
        bool baselinePostconditionMet = false,
        DateTimeOffset? attemptStartedAtUtc = null)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselinePostconditionMet = baselinePostconditionMet,
            HasCommandEventEvidence = false,
            CommittedEventSequence = null,
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
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
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public TenantRemoveMemberCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
        ArgumentNullException.ThrowIfNull(status);

        // A projection-confirmed or already-applied removal is terminal access evidence. A later status
        // poll -- including the Refresh rendered on the WP-2A receipt -- must never regress it to
        // ProjectionPending or reset AuditAvailable back to AuditPending: confirmed access and audit
        // proof are distinct, non-collapsing states, and the confirmed outcome survives audit flaps.
        if (State is TenantCommandLifecycleState.Confirmed or TenantCommandLifecycleState.AlreadyApplied
            or TenantCommandLifecycleState.Rejected or TenantCommandLifecycleState.Failed) {
            return this;
        }

        if ((status.IsPending && status.IsRetryableFailure)
            || (status.Status is not null && (status.IsPending || status.IsRetryableFailure))
            || (status.Status is not null && !status.HasVerifiedCommandIdentity)
            || (status.Status is CommandStatus.Completed
                && CommittedEventSequence is > 0
                && status.CommittedEventSequence is > 0
                && status.CommittedEventSequence != CommittedEventSequence))
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
            // A temporary status nudge does not revoke previously qualified command-event evidence.
            // Keep the expiry uncertainty eligible for the same projection proof as before expiry.
            bool expiredWait = State is TenantCommandLifecycleState.UnableToVerify && ProjectionWaitExpired
                && SafeMessageKey == TenantMembershipProjectionWaitPolicy.ExpiredMessageKey;
            return this with
            {
                SafeMessage = expiredWait ? null : status.SafeMessage,
                SafeMessageKey = expiredWait ? TenantMembershipProjectionWaitPolicy.ExpiredMessageKey : status.SafeMessageKey,
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
                => this with { State = HasCommandEventEvidence ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, CommittedEventSequence = CommittedEventSequence is > 0 ? CommittedEventSequence : status.CommittedEventSequence, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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

    public TenantRemoveMemberCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantRemoveMemberCommandSnapshot ConfirmProjection(
        TenantDetailSnapshot? proof) {
        if (Intent is null) {
            return this;
        }

        if (proof is null || !TenantMembershipCommandProvenance.IsAuthoritative(proof))
        {
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        TenantDetailProjection? detailEvidence = proof!.Detail;
        string? currentProjectionVersion = proof.ProjectionVersion;
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
        if (State is TenantCommandLifecycleState.ProjectionPending
            || State is TenantCommandLifecycleState.UnableToVerify && ProjectionWaitExpired
                && SafeMessageKey == TenantMembershipProjectionWaitPolicy.ExpiredMessageKey) {
            if (BaselinePostconditionMet) {
                return this with {
                    State = TenantCommandLifecycleState.AlreadyApplied,
                    LastConfirmedMemberProjection = detailEvidence,
                    SafeMessage = null,
                    SafeMessageKey = "Tenants.RemoveMember.Confirm.AlreadyApplied.PreExisting",
                    RejectionCode = null,
                    AuditState = TenantCommandAuditStates.NotStarted,
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
                    // The projection was read but did not prove this attempt, and no audit read happened: AuditState is kept.
                    FocusTarget = TenantCommandFocusTarget.Refresh,
                    LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
                };
            }

            bool versionAdvanced = TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
                BaselineProjectionVersion,
                currentProjectionVersion,
                HasCommandEventEvidence);
            bool committedProjectionReached = TenantLifecycleProjectionVersion.HasReached(
                CommittedEventSequence, currentProjectionVersion)
                && (!TenantLifecycleProjectionVersion.IsOrdered(BaselineProjectionVersion)
                    || TenantLifecycleProjectionVersion.HasReached(CommittedEventSequence, BaselineProjectionVersion, requireBeyond: true));
            if (!versionAdvanced || !committedProjectionReached) {
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
                ProjectionProofCallerScope = proof.CallerScope,
                // RemoveMember keeps its removal-proof walk: confirmed access waits for a matched receipt
                // (ApplyRemovalProofMatch) instead of reporting missing support.
                AuditState = TenantCommandAuditStates.AwaitingMatchedProof,
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
                AuditState = TenantCommandAuditStates.NotStarted,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        return this;
    }

    /// <summary>
    /// Promotes confirmed removal to <see cref="TenantCommandAuditState.AuditAvailable"/> only when a
    /// matching WP-2A removal audit row is supplied. Never invents available from empty or mismatched evidence.
    /// Unmatched rematches keep Available (confirmed outcome survives audit flaps) and keep Delayed. An unmatched
    /// rematch is a complete audit read, so an earlier Unavailable falls back to Pending until the
    /// retained unmatched-walk budget is exhausted, after which the audit is Delayed.
    /// </summary>
    public TenantRemoveMemberCommandSnapshot ApplyRemovalProofMatch(
        bool matched,
        bool hasCurrentLifecycleBackedEvidence = false,
        bool hasReadyReceipt = false) {
        if (State is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (matched && hasCurrentLifecycleBackedEvidence && hasReadyReceipt) {
            return this with {
                AuditState = TenantCommandAuditState.AuditAvailable,
                UnmatchedRemovalProofWalkCount = 0,
                FocusTarget = TenantCommandFocusTarget.Lifecycle,
                LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
            };
        }

        // Keep Available / Delayed across unmatched rematches; only Pending is the default wait state. An unmatched
        // rematch is a complete, successful audit read, so an earlier "could not be read" (Unavailable) is stale and
        // falls back to Pending within the retained walk budget: the trail is readable but has no proof yet.
        if (AuditState is TenantCommandAuditState.AuditAvailable
            or TenantCommandAuditState.AuditDelayed) {
            return this;
        }

        int unmatchedWalks = matched ? UnmatchedRemovalProofWalkCount
            : Math.Min(UnmatchedRemovalProofWalkCount + 1, TenantAuditAvailability.MaximumUnchangedRetries);
        return this with {
            UnmatchedRemovalProofWalkCount = unmatchedWalks,
            AuditState = unmatchedWalks >= TenantAuditAvailability.MaximumUnchangedRetries
                ? TenantCommandAuditState.AuditDelayed : TenantCommandAuditState.AuditPending,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    /// <summary>
    /// Maps audit-query surface failures after confirmation without reversing the confirmed access outcome.
    /// </summary>
    public TenantRemoveMemberCommandSnapshot ApplyRemovalProofQueryFailure(TenantCommandAuditState failureState) {
        if (State is not TenantCommandLifecycleState.Confirmed) {
            return this;
        }

        if (failureState is not (TenantCommandAuditState.AuditDelayed or TenantCommandAuditState.AuditUnavailable)) {
            return this;
        }

        // Keep an already-matched available proof; later flaps must not silently downgrade success.
        if (AuditState is TenantCommandAuditState.AuditAvailable) {
            return this;
        }

        return this with {
            AuditState = failureState,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = failureState is TenantCommandAuditState.AuditUnavailable
                ? TenantCommandLiveRegionPoliteness.Assertive
                : TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    public static bool IsMatchingRemovalProof(
        TenantAuditRow? row,
        string tenantId,
        string targetUserId,
        string? correlationId,
        long? committedEventSequence)
        => row is not null
        && string.Equals(row.EventType, "UserRemovedFromTenant", StringComparison.Ordinal)
        && string.Equals(row.TenantId, tenantId, StringComparison.Ordinal)
        && string.Equals(row.Target, targetUserId, StringComparison.Ordinal)
        && row.Category is AuditEventCategory.Access
        && string.Equals(row.Scope, tenantId, StringComparison.Ordinal)
        && string.Equals(row.Outcome, nameof(UserRemovedFromTenant), StringComparison.Ordinal)
        && !string.IsNullOrWhiteSpace(correlationId)
        && string.Equals(row.CommandCorrelationId, correlationId, StringComparison.Ordinal)
        && committedEventSequence is > 0
        && row.AggregateSequence == committedEventSequence;

    /// <summary>
    /// Enumerates qualifying removal-proof rows newest first. The flow walks this sequence so it can
    /// continue past weak matches to a later current, projection-backed receipt; keeping the ordering
    /// here means the unit tests exercise the exact selection production uses.
    /// </summary>
    /// <param name="rows">Audit rows returned by the authorized audit read.</param>
    /// <param name="tenantId">Tenant the removal targeted.</param>
    /// <param name="targetUserId">Removed member identifier.</param>
    /// <param name="correlationId">Correlation of the exact verified command attempt.</param>
    /// <param name="committedEventSequence">Committed aggregate sequence from that command's verified completion.</param>
    /// <returns>Matching rows ordered newest first.</returns>
    public static IEnumerable<TenantAuditRow> EnumerateMatchingRemovalProofs(
        IReadOnlyList<TenantAuditRow> rows,
        string tenantId,
        string targetUserId,
        string? correlationId,
        long? committedEventSequence)
        => rows
            .Where(row => IsMatchingRemovalProof(row, tenantId, targetUserId, correlationId, committedEventSequence))
            .OrderByDescending(row => row.Timestamp);

    public static TenantAuditRow? FindMatchingRemovalProof(
        IReadOnlyList<TenantAuditRow> rows,
        string tenantId,
        string targetUserId,
        string? correlationId,
        long? committedEventSequence)
        => EnumerateMatchingRemovalProofs(rows, tenantId, targetUserId, correlationId, committedEventSequence)
            .FirstOrDefault();
}
