using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Hexalith.Tenants.UI.Services;

/// <summary>
/// Reports base-address settings rejected during composition, once, at host startup.
/// </summary>
/// <param name="diagnostics">Every rejection set collected while services were registered.</param>
/// <param name="logger">The logger used to report them.</param>
/// <remarks>
/// Composition runs before any logger exists, so the rejection is recorded there and reported here. The
/// message names the configuration key and nothing else — see <see cref="TenantsUiConfigurationDiagnostics"/>
/// for why the value is deliberately absent.
/// <para>
/// The dependency is the whole enumerable, not a single instance, so every composition contributes its own
/// rejection set and a host that pre-registers its own — or calls <c>AddHexalithTenantsUiModule</c> twice —
/// still has all of them reported. Keys are de-duplicated, so each is warned about exactly once. Reporting
/// occurs when the host starts; a bare service provider can inspect the retained diagnostics but, by
/// definition, does not run hosted services and therefore emits no startup warning.
/// </para>
/// </remarks>
internal sealed partial class TenantsUiConfigurationDiagnosticsReporter(
    IEnumerable<TenantsUiConfigurationDiagnostics> diagnostics,
    ILogger<TenantsUiConfigurationDiagnosticsReporter> logger) : IHostedService
{
    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (string settingName in diagnostics.SelectMany(entry => entry.RejectedBaseAddressSettings).Distinct(StringComparer.Ordinal))
        {
            LogRejectedBaseAddress(logger, settingName);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Warning,
        Message = "Configuration setting '{SettingName}' is not a usable http or https base address, so the surface that depends on it is registered as unavailable. The configured value is intentionally not logged.")]
    private static partial void LogRejectedBaseAddress(ILogger logger, string settingName);
}
