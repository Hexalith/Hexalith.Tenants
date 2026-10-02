namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Typed correction start evidence used before the separate preview.</summary>
public enum TenantCorrectionCommandType
{
    /// <summary>AddUserToTenant correction classification.</summary>
    AddUserToTenant,
    /// <summary>ChangeUserRole correction classification.</summary>
    ChangeUserRole,
    /// <summary>SetGlobalAdministrator correction classification.</summary>
    SetGlobalAdministrator,
    /// <summary>RemoveGlobalAdministrator correction classification.</summary>
    RemoveGlobalAdministrator,
}
