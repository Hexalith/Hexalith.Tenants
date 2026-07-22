namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Binds one deployment-owned tenant, subject, and configuration-prefix grant.
/// </summary>
internal sealed class TenantConfigurationPrefixGrantOptions
{
    /// <summary>Gets or sets the literal tenant identifier.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the literal authenticated subject.</summary>
    public string? Subject { get; set; }

    /// <summary>Gets or sets the literal authorized prefix.</summary>
    public string? Prefix { get; set; }
}
