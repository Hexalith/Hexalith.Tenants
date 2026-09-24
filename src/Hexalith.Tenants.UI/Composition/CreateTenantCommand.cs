using System.ComponentModel.DataAnnotations;

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.Tenants.UI.Composition;

/// <summary>
/// Create-tenant input shape used to exercise FrontComposer's generated command form.
/// </summary>
[Command]
[BoundedContext("tenants")]
public partial class CreateTenantCommand
{
    /// <summary>Gets or sets the framework message identifier.</summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>Gets or sets the requested tenant identifier.</summary>
    [Required]
    [Display(Name = "Tenant ID")]
    public string TenantKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant name.</summary>
    [Required]
    public string Name { get; set; } = string.Empty;
}
