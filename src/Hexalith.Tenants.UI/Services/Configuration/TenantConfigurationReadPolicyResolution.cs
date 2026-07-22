using System.Collections.ObjectModel;

namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Contains validated non-sensitive authorization and display-policy inputs.
/// </summary>
internal sealed class TenantConfigurationReadPolicyResolution
{
    private TenantConfigurationReadPolicyResolution(
        bool isAvailable,
        bool isGlobalAdministrator,
        IEnumerable<string> authorizedPrefixes,
        IEnumerable<string> displaySafeKeys)
    {
        IsAvailable = isAvailable;
        IsGlobalAdministrator = isGlobalAdministrator;
        AuthorizedPrefixes = new ReadOnlyCollection<string>(authorizedPrefixes.ToArray());
        DisplaySafeKeys = new ReadOnlySet<string>(displaySafeKeys.ToHashSet(StringComparer.Ordinal));
    }

    /// <summary>Gets whether policy and principal evidence are usable.</summary>
    public bool IsAvailable { get; }

    /// <summary>Gets whether the only namespace wildcard is proven.</summary>
    public bool IsGlobalAdministrator { get; }

    /// <summary>Gets ordinary literal prefixes, or the sole wildcard for a proven administrator.</summary>
    public IReadOnlyList<string> AuthorizedPrefixes { get; }

    /// <summary>Gets exact full keys positively approved for value display.</summary>
    public IReadOnlySet<string> DisplaySafeKeys { get; }

    /// <summary>Creates an unavailable resolution.</summary>
    /// <returns>Unavailable policy.</returns>
    public static TenantConfigurationReadPolicyResolution Unavailable()
        => new(false, false, [], []);

    /// <summary>Creates a validated resolution.</summary>
    /// <param name="isGlobalAdministrator">Whether administrator scope is proven.</param>
    /// <param name="authorizedPrefixes">Literal authorized prefixes.</param>
    /// <param name="displaySafeKeys">Exact display-safe keys.</param>
    /// <returns>Validated policy.</returns>
    public static TenantConfigurationReadPolicyResolution Available(
        bool isGlobalAdministrator,
        IEnumerable<string> authorizedPrefixes,
        IEnumerable<string> displaySafeKeys)
    {
        ArgumentNullException.ThrowIfNull(authorizedPrefixes);
        ArgumentNullException.ThrowIfNull(displaySafeKeys);
        return new(true, isGlobalAdministrator, authorizedPrefixes, displaySafeKeys);
    }
}
