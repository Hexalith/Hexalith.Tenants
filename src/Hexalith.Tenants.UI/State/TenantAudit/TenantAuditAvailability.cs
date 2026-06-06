using Hexalith.Tenants.UI.State.TenantCommands;

namespace Hexalith.Tenants.UI.State.TenantAudit;

public enum TenantAuditAvailabilityState
{
    Pending,
    Delayed,
    Unavailable,
    MissingSupport,
}

public enum TenantAuditRecoveryVerb
{
    Wait,
    Refresh,
    InspectAudit,
    ContinueReadOnly,
    Escalate,
}

public sealed record TenantAuditAvailability(
    TenantAuditAvailabilityState? State,
    IReadOnlyList<TenantAuditRecoveryVerb> RecoveryVerbs,
    TenantCommandLiveRegionPoliteness LiveRegionPoliteness)
{
    public bool ShouldRender
        => State is not null;

    public bool IsAuditAvailable
        => false;

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
                    TenantAuditRecoveryVerb.Refresh,
                    TenantAuditRecoveryVerb.InspectAudit,
                ],
                TenantCommandLiveRegionPoliteness.Polite),
            TenantCommandAuditState.AuditUnavailable => new(
                TenantAuditAvailabilityState.Unavailable,
                [
                    TenantAuditRecoveryVerb.ContinueReadOnly,
                    TenantAuditRecoveryVerb.Refresh,
                    TenantAuditRecoveryVerb.Escalate,
                ],
                TenantCommandLiveRegionPoliteness.Assertive),
            TenantCommandAuditState.MissingSupport => new(
                TenantAuditAvailabilityState.MissingSupport,
                [
                    TenantAuditRecoveryVerb.ContinueReadOnly,
                    TenantAuditRecoveryVerb.Escalate,
                ],
                TenantCommandLiveRegionPoliteness.Assertive),
            _ => new(null, [], TenantCommandLiveRegionPoliteness.Polite),
        };
}
