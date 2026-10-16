namespace Hexalith.Tenants.UI.State.TenantCommands;

public enum TenantCommandAuditState {
    NotStarted,
    AuditPending,
    AuditDelayed,
    AuditUnavailable,
    AuditAvailable,
    MissingSupport,
}
