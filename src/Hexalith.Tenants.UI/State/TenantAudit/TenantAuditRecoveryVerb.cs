namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>
/// The canonical audit recovery verbs. <see cref="Wait"/> is conveyed by the state explanation only and never
/// renders as a control.
/// </summary>
public enum TenantAuditRecoveryVerb {
    Wait,
    Refresh,
    InspectAudit,
    ContinueReadOnly,
    Escalate,
}
