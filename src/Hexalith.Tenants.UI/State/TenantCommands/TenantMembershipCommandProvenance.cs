namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Support-safe helpers that qualify membership projection confirmation with pre-submit provenance.
/// </summary>
internal static class TenantMembershipCommandProvenance
{
    /// <summary>
    /// Returns whether the current projection version is a usable advancement past the captured baseline.
    /// Projection versions are opaque; advancement is unequal, non-empty ordinal comparison only.
    /// </summary>
    /// <param name="baselineProjectionVersion">Projection version captured before submit.</param>
    /// <param name="currentProjectionVersion">Projection version observed after re-query.</param>
    /// <returns><see langword="true"/> when both versions are present and differ.</returns>
    public static bool HasProjectionVersionAdvancement(
        string? baselineProjectionVersion,
        string? currentProjectionVersion)
        => !string.IsNullOrWhiteSpace(baselineProjectionVersion)
        && !string.IsNullOrWhiteSpace(currentProjectionVersion)
        && !string.Equals(baselineProjectionVersion, currentProjectionVersion, StringComparison.Ordinal);
}
