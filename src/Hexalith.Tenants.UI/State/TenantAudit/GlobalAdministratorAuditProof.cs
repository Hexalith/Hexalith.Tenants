namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>A redacted row returned only after the BFF verifies exact command and event provenance.</summary>
public sealed class GlobalAdministratorAuditProof
{
    internal GlobalAdministratorAuditProof(TenantAuditRow row)
    {
        Row = row;
    }

    public TenantAuditRow Row { get; }
}
