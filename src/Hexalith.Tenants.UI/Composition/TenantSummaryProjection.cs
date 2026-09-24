using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.Tenants.Contracts.Enums;

namespace Hexalith.Tenants.UI.Composition;

/// <summary>
/// Tenant summary shape used to exercise FrontComposer's generated projection view.
/// </summary>
[Projection]
[BoundedContext("tenants")]
public partial class TenantSummaryProjection
{
    /// <summary>Gets or sets the tenant identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant status.</summary>
    public TenantStatus Status { get; set; }
}
