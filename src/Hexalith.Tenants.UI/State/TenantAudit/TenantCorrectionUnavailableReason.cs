namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Typed correction start evidence used before the separate preview.</summary>
public enum TenantCorrectionUnavailableReason
{
    /// <summary>AuthorizationIndeterminate correction classification.</summary>
    AuthorizationIndeterminate,
    /// <summary>FreshnessIndeterminate correction classification.</summary>
    FreshnessIndeterminate,
    /// <summary>CurrentProjectionUnavailable correction classification.</summary>
    CurrentProjectionUnavailable,
    /// <summary>AuditEvidenceUnavailable correction classification.</summary>
    AuditEvidenceUnavailable,
    /// <summary>CommandSupportUnavailable correction classification.</summary>
    CommandSupportUnavailable,
    /// <summary>ExplicitRoleRequired correction classification.</summary>
    ExplicitRoleRequired,
    /// <summary>UnsupportedOutcome correction classification.</summary>
    UnsupportedOutcome,
    /// <summary>GlobalAdministratorCommandSupportUnavailable correction classification.</summary>
    GlobalAdministratorCommandSupportUnavailable,
    /// <summary>AlreadyApplied correction classification.</summary>
    AlreadyApplied,
    /// <summary>TenantDisabled correction classification.</summary>
    TenantDisabled,
    /// <summary>TenantLifecycleUnknown correction classification.</summary>
    TenantLifecycleUnknown,
    /// <summary>CurrentStateIndeterminate correction classification.</summary>
    CurrentStateIndeterminate,
    /// <summary>CurrentRoleConflict correction classification.</summary>
    CurrentRoleConflict,
    /// <summary>NarrowViewportUnavailable correction classification.</summary>
    NarrowViewportUnavailable,
    /// <summary>ScopeConflict correction classification.</summary>
    ScopeConflict,
    /// <summary>EmptyMembershipRequiresOwner correction classification.</summary>
    EmptyMembershipRequiresOwner,
}
