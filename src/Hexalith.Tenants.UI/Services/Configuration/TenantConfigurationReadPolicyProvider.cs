using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Binds and semantically validates deployment-owned configuration read policy on each use.
/// </summary>
internal sealed class TenantConfigurationReadPolicyProvider(IConfiguration configuration)
{
    private const string PolicySectionPath = "Tenants:ConfigurationReadPolicy";

    /// <summary>
    /// Resolves current policy for a tenant and authenticated principal.
    /// </summary>
    /// <param name="tenantId">Literal requested tenant identifier.</param>
    /// <param name="principal">Current fail-closed principal evidence.</param>
    /// <returns>Validated policy or an unavailable result.</returns>
    public TenantConfigurationReadPolicyResolution Resolve(
        string tenantId,
        TenantConfigurationPrincipalEvidence principal)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentNullException.ThrowIfNull(principal);

        if (principal.State is TenantConfigurationPrincipalEvidenceState.Indeterminate
            || string.IsNullOrWhiteSpace(principal.Subject))
        {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        IConfigurationSection section = configuration.GetSection(PolicySectionPath);
        if (!section.Exists() || HasScalarCollection(section, nameof(TenantConfigurationReadPolicyOptions.PrefixGrants))
            || HasScalarCollection(section, nameof(TenantConfigurationReadPolicyOptions.DisplaySafe)))
        {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        TenantConfigurationReadPolicyOptions? options;
        try
        {
            options = section.Get<TenantConfigurationReadPolicyOptions>();
        }
        catch (InvalidOperationException)
        {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        if (options is null || !TryValidate(options))
        {
            return TenantConfigurationReadPolicyResolution.Unavailable();
        }

        string[] safeKeys = options.DisplaySafe.ToArray();
        if (principal.State is TenantConfigurationPrincipalEvidenceState.GlobalAdministrator)
        {
            return TenantConfigurationReadPolicyResolution.Available(true, ["*"], safeKeys);
        }

        string[] prefixes = options.PrefixGrants
            .Where(grant => string.Equals(grant.TenantId, tenantId, StringComparison.Ordinal)
                && string.Equals(grant.Subject, principal.Subject, StringComparison.Ordinal))
            .Select(static grant => grant.Prefix!)
            .OrderBy(static prefix => prefix, StringComparer.Ordinal)
            .ToArray();
        return TenantConfigurationReadPolicyResolution.Available(false, prefixes, safeKeys);
    }

    private static bool HasScalarCollection(IConfigurationSection section, string childName)
    {
        IConfigurationSection child = section.GetSection(childName);
        return !string.IsNullOrEmpty(child.Value);
    }

    private static bool TryValidate(TenantConfigurationReadPolicyOptions options)
    {
        if (options.PrefixGrants is null || options.DisplaySafe is null)
        {
            return false;
        }

        HashSet<string> grants = new(StringComparer.Ordinal);
        foreach (TenantConfigurationPrefixGrantOptions grant in options.PrefixGrants)
        {
            if (grant is null
                || string.IsNullOrWhiteSpace(grant.TenantId)
                || string.IsNullOrWhiteSpace(grant.Subject)
                || !IsValidPrefix(grant.Prefix))
            {
                return false;
            }

            string identity = string.Concat(grant.TenantId, "\0", grant.Subject, "\0", grant.Prefix);
            if (!grants.Add(identity))
            {
                return false;
            }
        }

        HashSet<string> safeKeys = new(StringComparer.Ordinal);
        foreach (string safeKey in options.DisplaySafe)
        {
            if (string.IsNullOrWhiteSpace(safeKey) || !safeKeys.Add(safeKey))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidPrefix(string? prefix)
        => !string.IsNullOrWhiteSpace(prefix)
        && !prefix.EndsWith(".", StringComparison.Ordinal)
        && !prefix.Any(char.IsWhiteSpace)
        && !string.Equals(prefix, "*", StringComparison.Ordinal);
}
