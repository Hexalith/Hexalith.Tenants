using Microsoft.Extensions.Configuration;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Collects base-address settings rejected during composition so they can be reported once at startup.
/// </summary>
/// <remarks>
/// Decision D2 made base-address rejection per-side and fail-closed, which is correct: an unusable read
/// address must not abort command registration, and vice versa. Decision D-G restores the diagnostic that
/// change removed. Without it a typo is indistinguishable from an outage — both render the same
/// "read surface unavailable" state, and this module deliberately strips HTTP client loggers, so nothing
/// else reports it.
/// <para>
/// This type holds configuration key <b>names</b> only. The configured value is never captured: a base
/// address can carry host, path, query or user-info material, and support-safety forbids putting any of it
/// on a log or telemetry channel.
/// </para>
/// </remarks>
internal sealed class TenantsUiConfigurationDiagnostics
{
    private readonly List<string> _rejectedBaseAddressSettings = [];

    /// <summary>
    /// Gets the configuration key names whose base address could not be used.
    /// </summary>
    public IReadOnlyList<string> RejectedBaseAddressSettings => _rejectedBaseAddressSettings;

    /// <summary>
    /// Records a configuration key whose value was present but is not a usable http or https base address.
    /// </summary>
    /// <param name="settingName">The configuration key name. Never the configured value.</param>
    public void RecordRejectedBaseAddress(string settingName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settingName);
        _rejectedBaseAddressSettings.Add(settingName);
    }

    /// <summary>
    /// Records a rejected base address only when the setting was actually configured.
    /// </summary>
    /// <param name="configuration">The host configuration to read the key from.</param>
    /// <param name="settingName">The configuration key name. Never the configured value.</param>
    /// <remarks>
    /// An absent or blank value is not a misconfiguration: it means the dependency is simply not wired, and
    /// failing closed to the unavailable gateway is the intended outcome. Only a value the operator believed
    /// would work — present, but not a usable http or https address — is worth reporting.
    /// </remarks>
    public void RecordRejectedBaseAddressIfConfigured(IConfiguration configuration, string settingName)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingName);
        if (!string.IsNullOrWhiteSpace(configuration[settingName]))
        {
            RecordRejectedBaseAddress(settingName);
        }
    }
}
