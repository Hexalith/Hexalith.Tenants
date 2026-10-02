using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Deliberate role selection scoped to one original audit reference.</summary>
/// <param name="AuditReference">Original approved audit reference.</param>
/// <param name="Role">Selected role, or Unknown to clear selection.</param>
public sealed record TenantCorrectionRoleSelection(string AuditReference, TenantRole Role);
