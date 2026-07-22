namespace Hexalith.Tenants.UI.Services.Configuration;

/// <summary>
/// Resolves the authenticated principal evidence used by tenant configuration policy.
/// </summary>
internal interface ITenantConfigurationPrincipalResolver
{
    /// <summary>
    /// Resolves current request or circuit principal evidence.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fail-closed principal evidence.</returns>
    ValueTask<TenantConfigurationPrincipalEvidence> ResolveAsync(CancellationToken cancellationToken = default);
}
