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

    /// <summary>
    /// Returns whether an audit event timestamp is a usable causal advancement past the attempt start.
    /// Used by remove-member confirmation when projection-version inequality alone is insufficient.
    /// </summary>
    /// <param name="attemptStartedAtUtc">UTC instant captured when the attempt was submitted.</param>
    /// <param name="auditEventTimestamp">Timestamp of the candidate command-specific audit row.</param>
    /// <returns><see langword="true"/> when the audit event is strictly newer than the attempt start.</returns>
    public static bool HasQualifyingAuditProvenance(
        DateTimeOffset? attemptStartedAtUtc,
        DateTimeOffset? auditEventTimestamp)
        => attemptStartedAtUtc is not null
        && auditEventTimestamp is not null
        && auditEventTimestamp.Value >= attemptStartedAtUtc.Value;
}
