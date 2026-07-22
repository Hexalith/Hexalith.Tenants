using Hexalith.Tenants.Contracts.Queries;
using Hexalith.Tenants.UI.State.TenantDetail;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Carries factory-sanitized detail plus separate safe read and management state.
/// </summary>
public sealed class TenantConfigurationComposition
{
    /// <summary>
    /// Initializes a safe composition result.
    /// </summary>
    /// <param name="sanitizedDetail">Tenant detail whose configuration dictionary is empty.</param>
    /// <param name="safeModel">Approved read model.</param>
    /// <param name="managementContext">Approved management scope and targets.</param>
    internal TenantConfigurationComposition(
        TenantDetail sanitizedDetail,
        TenantConfigurationSafeModel safeModel,
        TenantConfigurationManagementContext managementContext)
    {
        ArgumentNullException.ThrowIfNull(sanitizedDetail);
        ArgumentNullException.ThrowIfNull(safeModel);
        ArgumentNullException.ThrowIfNull(managementContext);
        SanitizedDetail = sanitizedDetail;
        SafeModel = safeModel;
        ManagementContext = managementContext;
    }

    /// <summary>Gets tenant detail with no raw configuration entries.</summary>
    public TenantDetail SanitizedDetail { get; }

    /// <summary>Gets approved configuration read state.</summary>
    public TenantConfigurationSafeModel SafeModel { get; }

    /// <summary>Gets approved command scope and remove targets.</summary>
    public TenantConfigurationManagementContext ManagementContext { get; }
}
