using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.State.TenantAudit;

/// <summary>Typed correction start evidence used before the separate preview.</summary>
/// <param name="Receipt">Complete original safe receipt.</param>
/// <param name="Row">Matching authorized audit row.</param>
/// <param name="IsAuthorized">Current authority gate.</param>
/// <param name="HasCurrentProjectionSnapshot">Current direct projection gate.</param>
/// <param name="CurrentProjectionSnapshotReference">Compatibility marker for the global correction evaluator.</param>
/// <param name="TenantStatus">Compatibility tenant lifecycle.</param>
/// <param name="CurrentRole">Compatibility target role.</param>
/// <param name="IntendedRole">Deliberately selected role.</param>
/// <param name="HasTenantCommandSupport">Current tenant command support.</param>
/// <param name="HasGlobalAdministratorCommandSupport">Separate global flow support.</param>
/// <param name="IsNarrowViewportSafe">Measured safe width gate.</param>
/// <param name="Projection">Redacted current tenant evidence and authority.</param>
public sealed record TenantCorrectionStartContext(
    TenantAuditReceipt Receipt,
    TenantAuditRow Row,
    bool IsAuthorized,
    bool HasCurrentProjectionSnapshot,
    string CurrentProjectionSnapshotReference,
    TenantStatus TenantStatus = TenantStatus.Active,
    TenantRole? CurrentRole = null,
    TenantRole? IntendedRole = null,
    bool HasTenantCommandSupport = true,
    bool HasGlobalAdministratorCommandSupport = false,
    bool IsNarrowViewportSafe = true,
    TenantCorrectionProjection? Projection = null);
