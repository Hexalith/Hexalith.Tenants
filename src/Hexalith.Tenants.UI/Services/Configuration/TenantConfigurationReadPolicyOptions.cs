namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Binds deployment-owned tenant configuration read policy.
/// </summary>
internal sealed class TenantConfigurationReadPolicyOptions
{
    /// <summary>Gets or sets positive tenant/subject prefix grants.</summary>
    public List<TenantConfigurationPrefixGrantOptions> PrefixGrants { get; set; } = [];

    /// <summary>Gets or sets exact full keys whose values are approved for display.</summary>
    public List<string> DisplaySafe { get; set; } = [];
}
