namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>The audit availability states shown by the shared availability control.</summary>
public enum TenantAuditAvailabilityState
{
    /// <summary>Events are stored and their audit record is expected shortly; no proof is claimed yet.</summary>
    Pending,

    /// <summary>The audit record is taking longer than expected to become readable.</summary>
    Delayed,

    /// <summary>The audit status could not be read or verified after the command was sent.</summary>
    Unavailable,

    /// <summary>An attempt-matched, complete, redacted audit receipt proves the outcome.</summary>
    Available,

    /// <summary>The surface has no in-panel audit verification that can match a receipt to the attempt.</summary>
    MissingSupport,
}
