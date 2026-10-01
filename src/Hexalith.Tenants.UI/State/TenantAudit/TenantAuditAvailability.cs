using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// The typed audit dimension of a command or evidence surface: its state, the canonical recovery verbs
/// that state offers, and how an assistive technology announces it.
/// </summary>
/// <param name="State">The audit availability state, or <see langword="null"/> when no audit dimension applies.</param>
/// <param name="RecoveryVerbs">The canonical recovery verbs, in rendering order.</param>
/// <param name="LiveRegionPoliteness">How the state and its explanation are announced.</param>
public sealed record TenantAuditAvailability(
    TenantAuditAvailabilityState? State,
    IReadOnlyList<TenantAuditRecoveryVerb> RecoveryVerbs,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness)
{
    /// <summary>
    /// Refresh attempts that may leave the state unchanged before Refresh is withdrawn. A state change resets
    /// the count, and every other recovery stays available.
    /// </summary>
    public const int MaximumUnchangedRetries = 3;

    private const string StateLabelPrefix = "Tenants.Audit.Availability.State.";

    private const string ExplanationPrefix = "Tenants.Audit.Availability.Reason.";

    /// <summary>Gets whether an audit dimension applies, so the shared control renders.</summary>
    public bool ShouldRender
        => State is not null;

    /// <summary>Gets whether attempt-matched audit evidence proves the outcome.</summary>
    public bool IsAuditAvailable
        => State is TenantAuditAvailabilityState.Available;

    /// <summary>Gets the shared localized label key of this state, or <see langword="null"/> when nothing renders.</summary>
    public string? StateLabelKey
        => State is { } state ? StateLabelKeyFor(state) : null;

    /// <summary>
    /// Gets the explanation key of an incomplete state. Proven availability needs no explanation, and the
    /// explanation never names a recovery verb: the rendered controls carry the recovery.
    /// </summary>
    public string? ExplanationKey
        => State is { } state and not TenantAuditAvailabilityState.Available
            ? ExplanationPrefix + state
            : null;

    /// <summary>Gets the shared localized label key of an availability state.</summary>
    /// <param name="state">The availability state.</param>
    /// <returns>The culture-independent resource key of the state label.</returns>
    public static string StateLabelKeyFor(TenantAuditAvailabilityState state)
        => StateLabelPrefix + state;

    /// <summary>
    /// Gets the shared localized label key of a command audit state, so flows name the state with the same
    /// whole string the shared control renders instead of a flow-local duplicate.
    /// </summary>
    /// <param name="state">The command audit state.</param>
    /// <returns>The resource key, or <see langword="null"/> for a state that renders no audit dimension.</returns>
    public static string? StateLabelKeyFor(TenantCommandAuditState state)
        => FromCommandAuditState(state).StateLabelKey;

    /// <summary>Maps a command audit state to its availability state, recovery verbs, and announcement politeness.</summary>
    /// <param name="state">The command audit state.</param>
    /// <returns>
    /// The availability of that state; <see cref="TenantCommandAuditState.NotStarted"/> yields one that renders nothing.
    /// </returns>
    public static TenantAuditAvailability FromCommandAuditState(TenantCommandAuditState state)
        => state switch
        {
            TenantCommandAuditState.AuditPending => new(
                TenantAuditAvailabilityState.Pending,
                [
                    TenantAuditRecoveryVerb.Wait,
                    TenantAuditRecoveryVerb.Refresh,
                    TenantAuditRecoveryVerb.InspectAudit,
                ],
                TenantCommandLiveRegionPoliteness.Polite),
            TenantCommandAuditState.AuditDelayed => new(
                TenantAuditAvailabilityState.Delayed,
                [
                    TenantAuditRecoveryVerb.Wait,
                    TenantAuditRecoveryVerb.Refresh,
                    TenantAuditRecoveryVerb.InspectAudit,
                    TenantAuditRecoveryVerb.Escalate,
                ],
                TenantCommandLiveRegionPoliteness.Polite),
            TenantCommandAuditState.AuditUnavailable => new(
                TenantAuditAvailabilityState.Unavailable,
                [
                    TenantAuditRecoveryVerb.Refresh,
                    TenantAuditRecoveryVerb.ContinueReadOnly,
                    TenantAuditRecoveryVerb.InspectAudit,
                    TenantAuditRecoveryVerb.Escalate,
                ],
                TenantCommandLiveRegionPoliteness.Assertive),
            TenantCommandAuditState.AuditAvailable => new(
                TenantAuditAvailabilityState.Available,
                [
                    TenantAuditRecoveryVerb.InspectAudit,
                    TenantAuditRecoveryVerb.ContinueReadOnly,
                ],
                TenantCommandLiveRegionPoliteness.Polite),
            TenantCommandAuditState.MissingSupport => new(
                TenantAuditAvailabilityState.MissingSupport,
                [
                    TenantAuditRecoveryVerb.ContinueReadOnly,
                    TenantAuditRecoveryVerb.InspectAudit,
                    TenantAuditRecoveryVerb.Escalate,
                ],
                TenantCommandLiveRegionPoliteness.Assertive),
            _ => new(null, [], TenantCommandLiveRegionPoliteness.Polite),
        };
}
