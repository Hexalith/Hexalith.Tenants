namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// The canonical audit recovery verbs. <see cref="Wait"/> is conveyed by the state explanation only and never
/// renders as a control.
/// </summary>
public enum TenantAuditRecoveryVerb
{
    /// <summary>Wait for the audit record to become readable; conveyed by the explanation, never a control.</summary>
    Wait,

    /// <summary>Retry the status lookup or audit read that would advance the state.</summary>
    Refresh,

    /// <summary>Open the scoped audit trail through the existing audit entry point.</summary>
    InspectAudit,

    /// <summary>Leave the attempt and return to the read-only projection surface.</summary>
    ContinueReadOnly,

    /// <summary>Follow the configured, support-safe escalation destination.</summary>
    Escalate,
}
