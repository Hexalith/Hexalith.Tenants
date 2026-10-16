using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;
using Hexalith.Tenants.UI.State.TenantList;

using TenantDetailProjection = Hexalith.Tenants.Contracts.Queries.TenantDetail;
using TenantDetailSnapshot = Hexalith.Tenants.UI.State.TenantDetail.TenantDetailSnapshot;

namespace Hexalith.Tenants.UI.State.TenantCommands;

public sealed record TenantCreateCommandSnapshot(
    TenantCommandLifecycleState State,
    CreateTenant? Intent = null,
    TenantSummary? LastConfirmedListEvidence = null,
    TenantDetailSnapshot? LastConfirmedDetailEvidence = null,
    string? MessageId = null,
    string? CorrelationId = null,
    string? BaselineProjectionVersion = null,
    string? BaselineDetailProjectionVersion = null,
    bool BaselineTenantAbsent = false,
    bool HasCommandEventEvidence = false,
    DateTimeOffset? AttemptStartedAtUtc = null,
    string? SafeMessage = null,
    string? SafeMessageKey = null,
    string? RejectionCode = null,
    TenantCommandAuditState AuditState = TenantCommandAuditState.NotStarted,
    TenantCommandFocusTarget FocusTarget = TenantCommandFocusTarget.Submit,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite) {
    public static TenantCreateCommandSnapshot Idle()
        => new(TenantCommandLifecycleState.Idle);

    public static TenantCreateCommandSnapshot Blocked(string safeMessage, TenantCommandFocusTarget focusTarget)
        => new(
            TenantCommandLifecycleState.UnableToVerify,
            SafeMessage: safeMessage,
            AuditState: TenantCommandAuditStates.NotStarted,
            FocusTarget: focusTarget,
            LiveRegionPoliteness: TenantCommandLiveRegionPoliteness.Assertive);

    /// <summary>
    /// Whether the attempt reached a terminal outcome, so a differing submit is a deliberate new
    /// attempt rather than a retry of this one.
    /// </summary>
    public bool IsTerminal
        => State is TenantCommandLifecycleState.Confirmed
            or TenantCommandLifecycleState.Rejected
            or TenantCommandLifecycleState.Failed;

    /// <summary>
    /// Surfaces a blocking reason without discarding the tracked attempt and without collapsing its
    /// lifecycle state. The static <see cref="Blocked"/> factory builds a fresh record, so using it
    /// while an attempt is tracked would drop MessageId/CorrelationId and disable the refresh
    /// recovery the blocking copy names.
    /// </summary>
    /// <param name="safeMessage">Support-safe reason to render.</param>
    /// <returns>The same attempt carrying a blocking reason.</returns>
    public TenantCreateCommandSnapshot BlockedWithTracking(string safeMessage)
        => this with {
            SafeMessage = safeMessage,
            SafeMessageKey = null,
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };

    public TenantCreateCommandSnapshot RequestSent(
        CreateTenant intent,
        string? baselineProjectionVersion,
        bool baselineTenantAbsent,
        string? baselineDetailProjectionVersion = null,
        DateTimeOffset? attemptStartedAtUtc = null)
        => this with {
            State = TenantCommandLifecycleState.RequestSent,
            Intent = intent,
            BaselineProjectionVersion = baselineProjectionVersion,
            BaselineDetailProjectionVersion = baselineDetailProjectionVersion,
            BaselineTenantAbsent = baselineTenantAbsent,
            HasCommandEventEvidence = false,
            AttemptStartedAtUtc = attemptStartedAtUtc ?? DateTimeOffset.UtcNow,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            AuditState = TenantCommandAuditStates.NotStarted,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };

    public TenantCreateCommandSnapshot Accepted(TenantCommandSubmissionResult result) {
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

    public TenantCreateCommandSnapshot ApplyStatus(TenantCommandStatusResult status) {
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

        // Terminal failures use only the fresh status key; processing and success clear previous reasons.
        return status.Status.Value switch {
            CommandStatus.Received or CommandStatus.Processing
                => this with { State = HasCommandEventEvidence ? TenantCommandLifecycleState.ProjectionPending : TenantCommandLifecycleState.Accepted, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.EventsStored or CommandStatus.EventsPublished
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = true, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
            CommandStatus.Completed
                => this with { State = TenantCommandLifecycleState.ProjectionPending, HasCommandEventEvidence = HasCommandEventEvidence || status.EventCount is > 0, SafeMessage = null, SafeMessageKey = null, RejectionCode = null, AuditState = TenantCommandAuditStates.FromCommandStatus(status.Status, status.EventCount, HasCommandEventEvidence), LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite },
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

    // A SignalR notification is a freshness nudge only. It must never advance the lifecycle into
    // ProjectionPending, which is the single state ConfirmProjection trusts: doing so would let a
    // notification stand in for the command-status evidence that proves events were stored.
    public TenantCreateCommandSnapshot SignalRNudge()
        => this with {
            FocusTarget = TenantCommandFocusTarget.Refresh,
        };

    public TenantCreateCommandSnapshot ConfirmProjection(
        TenantListSnapshot? listEvidence,
        TenantDetailSnapshot? detailEvidence) {
        if (Intent is null) {
            return this;
        }

        // Only a command status that reached event storage/completion may be reconciled into confirmation.
        // Acceptance means receipt or processing only, not proof that the requested create occurred.
        if (State is not TenantCommandLifecycleState.ProjectionPending) {
            return this;
        }

        TenantDetailSnapshot? authoritativeDetail = TenantMembershipCommandProvenance.IsAuthoritative(detailEvidence)
            ? detailEvidence : null;
        TenantListSnapshot? authoritativeList = TenantMembershipCommandProvenance.IsAuthoritative(listEvidence)
            ? listEvidence : null;
        TenantListRow? listRow = authoritativeList?.Rows.FirstOrDefault(row =>
            string.Equals(row.TenantId, Intent.TenantId, StringComparison.Ordinal)
            && row.Freshness is Hexalith.EventStore.Client.Projections.ReadModelFreshnessState.Current
            && row.Lifecycle is ProjectionLifecycleState.Current);
        TenantSummary? list = listRow is null ? null : new(listRow.TenantId, listRow.Name, listRow.Status);
        TenantDetailProjection? detail = authoritativeDetail?.Detail;
        // TenantSummary intentionally omits Description, so list evidence can prove the complete submitted
        // metadata only when the submitted description is absent. A non-null description requires detail.
        // Empty and whitespace descriptions normalise to absent on both sides so a textarea that was typed
        // into and cleared still reconciles against a projection that stores no description.
        string? intentDescription = NormalizeDescription(Intent.Description);
        bool listMatches = intentDescription is null
            && string.Equals(list?.TenantId, Intent.TenantId, StringComparison.Ordinal)
            && string.Equals(list?.Name, Intent.Name, StringComparison.Ordinal);
        bool detailMatches = detail is not null
            && string.Equals(detail.TenantId, Intent.TenantId, StringComparison.Ordinal)
            && string.Equals(detail.Name, Intent.Name, StringComparison.Ordinal)
            && string.Equals(NormalizeDescription(detail.Description), intentDescription, StringComparison.Ordinal);
        if (!listMatches && !detailMatches) {
            // No authoritative projection evidence yet: keep the lifecycle state exactly as the command
            // status set it and only nudge focus to the refresh recovery action.
            return this with { FocusTarget = TenantCommandFocusTarget.Refresh };
        }

        if (!BaselineTenantAbsent) {
            return UnableToVerify();
        }

        // Compare like for like. A tenant-detail version token is not a comparable successor to a
        // tenant-list baseline, so each projection is measured against its own captured baseline.
        (string? baselineVersion, string? currentVersion) = detailMatches && authoritativeDetail is not null
            ? (BaselineDetailProjectionVersion, authoritativeDetail.ProjectionVersion)
            : (BaselineProjectionVersion, authoritativeList?.ProjectionVersion);

        // The causal overload additionally requires event evidence for this exact tracked command and an
        // ordered advancement, so opaque-token churn and version regressions fail closed.
        bool versionAdvanced = TenantMembershipCommandProvenance.HasProjectionVersionAdvancement(
            baselineVersion,
            currentVersion,
            HasCommandEventEvidence);
        bool firstAppearanceWithVersion = HasCommandEventEvidence
            && string.IsNullOrWhiteSpace(baselineVersion)
            && !string.IsNullOrWhiteSpace(currentVersion);
        if (!versionAdvanced && !firstAppearanceWithVersion) {
            return UnableToVerify();
        }

        return this with {
            State = TenantCommandLifecycleState.Confirmed,
            LastConfirmedListEvidence = list,
            LastConfirmedDetailEvidence = authoritativeDetail,
            SafeMessage = null,
            SafeMessageKey = null,
            RejectionCode = null,
            // A projection confirmation carries no attempt-specific audit proof, and this flow has no in-panel
            // audit verification: report proof as unsupported rather than pending forever.
            AuditState = TenantCommandAuditStates.ConfirmedWithoutProof,
            FocusTarget = TenantCommandFocusTarget.Lifecycle,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Polite,
        };
    }

    private static string? NormalizeDescription(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value;

    // Leaves the LastConfirmed* slots untouched: evidence that failed the provenance gate was never
    // confirmed, so recording it would blur in-flight intent into confirmed truth.
    private TenantCreateCommandSnapshot UnableToVerify()
        => this with {
            State = TenantCommandLifecycleState.UnableToVerify,
            SafeMessage = null,
            SafeMessageKey = "Tenants.Create.Confirm.UnableToVerify.MissingProvenance",
            // The projection was read but did not prove this attempt, and no audit read happened: AuditState is kept.
            FocusTarget = TenantCommandFocusTarget.Refresh,
            LiveRegionPoliteness = TenantCommandLiveRegionPoliteness.Assertive,
        };
}
