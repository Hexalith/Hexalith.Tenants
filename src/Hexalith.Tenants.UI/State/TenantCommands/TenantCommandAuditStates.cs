using Hexalith.EventStore.Contracts.Commands;
using Hexalith.Tenants.UI.State.TenantAudit;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// The one canonical derivation of the audit dimension of a tenant command attempt. Every tenant command
/// snapshot and flow routes its audit state through these members, so command lifecycle, projection
/// confirmation, and audit evidence stay separate typed dimensions that never collapse into each other.
/// </summary>
/// <remarks>
/// No command status, event count, projection confirmation, or SignalR nudge ever yields
/// <see cref="TenantCommandAuditState.AuditAvailable"/>. Only a complete, redacted
/// <see cref="TenantAuditReceipt"/> in the <see cref="TenantAuditReceiptState.Ready"/> state that was matched to
/// the attempt does, through <see cref="FromConfirmationEvidence(TenantAuditReceipt?)"/>.
/// </remarks>
public static class TenantCommandAuditStates
{
    /// <summary>
    /// Nothing was stored yet: the command is only accepted or processing, it was rejected, or the outcome
    /// dispatched nothing (blocked, previewed, already applied, duplicate prevented). No audit state is
    /// implied, the shared control stays hidden, and the command dimension alone shows the outcome.
    /// </summary>
    public const TenantCommandAuditState NotStarted = TenantCommandAuditState.NotStarted;

    /// <summary>
    /// Events are stored and their audit record should follow: the attempt is not yet confirmed, or it is
    /// confirmed and the flow's in-panel proof walk is still matching a receipt (see <see cref="AwaitingMatchedProof"/>).
    /// </summary>
    public const TenantCommandAuditState EventsStored = TenantCommandAuditState.AuditPending;

    /// <summary>
    /// The projection confirmed the attempt and the flow's in-panel proof walk (RemoveMember) is still looking
    /// for the attempt-matched receipt. The audit record is pending, exactly as between events-stored and
    /// confirmation.
    /// </summary>
    public const TenantCommandAuditState AwaitingMatchedProof = TenantCommandAuditState.AuditPending;

    /// <summary>The status timed out, publication failed, or the attempt outlived its retention window.</summary>
    public const TenantCommandAuditState Delayed = TenantCommandAuditState.AuditDelayed;

    /// <summary>The status after dispatch is missing, unknown, or cannot be verified.</summary>
    public const TenantCommandAuditState Unverifiable = TenantCommandAuditState.AuditUnavailable;

    /// <summary>
    /// The projection confirmed the attempt but the flow has no in-panel audit verification that can match
    /// an attempt-specific receipt, so proof is honestly reported as unsupported rather than pending forever.
    /// </summary>
    public const TenantCommandAuditState ConfirmedWithoutProof = TenantCommandAuditState.MissingSupport;

    /// <summary>Derives the audit dimension from a command status lookup.</summary>
    /// <param name="status">The looked-up command status, or <see langword="null"/> when the lookup returned none.</param>
    /// <param name="eventCount">
    /// The event count the status reported. A <see cref="CommandStatus.Completed"/> status that stored zero
    /// events has no audit record to wait for; an unreported (<see langword="null"/>) count keeps it pending.
    /// </param>
    /// <returns>The canonical audit state for that status.</returns>
    public static TenantCommandAuditState FromCommandStatus(CommandStatus? status, int? eventCount = null)
        => status switch
        {
            // Receipt or processing proves nothing was stored yet: audit pending starts at events-stored.
            CommandStatus.Received or CommandStatus.Processing => NotStarted,
            CommandStatus.Completed when eventCount == 0 => NotStarted,
            CommandStatus.EventsStored or CommandStatus.EventsPublished or CommandStatus.Completed => EventsStored,
            CommandStatus.TimedOut or CommandStatus.PublishFailed => Delayed,
            CommandStatus.Rejected => NotStarted,
            _ => Unverifiable,
        };

    /// <summary>Derives the audit dimension from a command submission result.</summary>
    /// <param name="result">The gateway submission result.</param>
    /// <returns>The canonical audit state for that submission outcome.</returns>
    public static TenantCommandAuditState FromSubmission(TenantCommandSubmissionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        // An ambiguous failure, or a failure the gateway reported after sending the message (it then carries
        // the message id), may have reached the server. A failure without a message id (for example
        // pre-dispatch validation or an unavailable gateway) dispatched nothing, exactly like a rejection.
        return result.IsAmbiguousFailure
            ? Unverifiable
            : result.State switch
            {
                TenantCommandLifecycleState.Failed when !string.IsNullOrWhiteSpace(result.MessageId) => Unverifiable,
                TenantCommandLifecycleState.Accepted
                    or TenantCommandLifecycleState.Rejected
                    or TenantCommandLifecycleState.Failed
                    or TenantCommandLifecycleState.AlreadyApplied
                    or TenantCommandLifecycleState.DuplicatePrevented => NotStarted,
                _ => Unverifiable,
            };
    }

    /// <summary>
    /// Derives the audit dimension of a projection-confirmed attempt from the attempt-matched audit evidence.
    /// </summary>
    /// <param name="matchedReceipt">
    /// The receipt built from the audit row that matched this attempt, or <see langword="null"/> when no row
    /// matched or the flow has no in-panel audit verification.
    /// </param>
    /// <returns>
    /// <see cref="TenantCommandAuditState.AuditAvailable"/> only for a complete, redacted Ready receipt;
    /// otherwise <see cref="ConfirmedWithoutProof"/>.
    /// </returns>
    public static TenantCommandAuditState FromConfirmationEvidence(TenantAuditReceipt? matchedReceipt)
        => matchedReceipt?.State is TenantAuditReceiptState.Ready
            ? TenantCommandAuditState.AuditAvailable
            : ConfirmedWithoutProof;
}
