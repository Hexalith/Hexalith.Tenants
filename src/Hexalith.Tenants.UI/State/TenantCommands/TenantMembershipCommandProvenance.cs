namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Support-safe helpers that qualify membership projection confirmation with pre-submit provenance.
/// </summary>
internal static class TenantMembershipCommandProvenance
{
    private const string TenantSequencePrefix = "tenant-sequence:";

    /// <summary>
    /// Returns whether two opaque projection versions differ.
    /// This legacy overload remains for non-membership command snapshots whose contracts only expose an
    /// opaque change token. Membership commands use the causal overload below.
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
    /// Returns whether the current projection version is a causally qualified advancement past the captured baseline.
    /// A tracked command must have produced event evidence, and both versions must expose the same ordered
    /// numeric suffix so regressions and opaque-token churn fail closed.
    /// </summary>
    /// <param name="baselineProjectionVersion">Projection version captured before submit.</param>
    /// <param name="currentProjectionVersion">Projection version observed after re-query.</param>
    /// <param name="hasCommandEventEvidence">Whether status for this exact tracked command proves events were produced.</param>
    /// <returns><see langword="true"/> when the current ordered version is strictly newer.</returns>
    public static bool HasProjectionVersionAdvancement(
        string? baselineProjectionVersion,
        string? currentProjectionVersion,
        bool hasCommandEventEvidence)
    {
        if (!hasCommandEventEvidence
            || string.IsNullOrWhiteSpace(baselineProjectionVersion)
            || string.IsNullOrWhiteSpace(currentProjectionVersion))
        {
            return false;
        }

        bool baselineIsTenantSequence = TryParseTenantSequence(baselineProjectionVersion, out ulong baselineSequence);
        bool currentIsTenantSequence = TryParseTenantSequence(currentProjectionVersion, out ulong currentSequence);
        if (currentIsTenantSequence)
        {
            if (baselineIsTenantSequence)
            {
                return currentSequence > baselineSequence;
            }

            // A persisted legacy model exposes its state-store ETag until the first post-upgrade event.
            // Exact command-event evidence makes the one-way migration to the aggregate sequence token
            // causal; a token that merely resembles a malformed aggregate sequence still fails closed.
            return !baselineProjectionVersion.StartsWith(TenantSequencePrefix, StringComparison.Ordinal);
        }

        // Once the aggregate sequence contract is observed it cannot move back to an opaque/legacy token.
        if (baselineIsTenantSequence
            || currentProjectionVersion.StartsWith(TenantSequencePrefix, StringComparison.Ordinal))
        {
            return false;
        }

        return HasOrderedProjectionVersionAdvancement(baselineProjectionVersion, currentProjectionVersion);
    }

    private static bool TryParseTenantSequence(string value, out ulong sequence)
    {
        sequence = 0;
        return value.StartsWith(TenantSequencePrefix, StringComparison.Ordinal)
            && ulong.TryParse(
                value.AsSpan(TenantSequencePrefix.Length),
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out sequence);
    }

    private static bool HasOrderedProjectionVersionAdvancement(
        string? baselineProjectionVersion,
        string? currentProjectionVersion)
    {
        if (!TrySplitOrderedVersion(baselineProjectionVersion, out string baselinePrefix, out ulong baselineSequence)
            || !TrySplitOrderedVersion(currentProjectionVersion, out string currentPrefix, out ulong currentSequence))
        {
            return false;
        }

        return string.Equals(baselinePrefix, currentPrefix, StringComparison.Ordinal)
            && currentSequence > baselineSequence;
    }

    /// <summary>
    /// Returns whether an audit event timestamp is a usable causal advancement past the attempt start.
    /// Used by remove-member confirmation when projection-version inequality alone is insufficient.
    /// </summary>
    /// <param name="attemptStartedAtUtc">UTC instant captured when the attempt was submitted.</param>
    /// <param name="auditEventTimestamp">Timestamp of the candidate command-specific audit row.</param>
    /// <returns><see langword="true"/> when the audit event is at or after the attempt start.</returns>
    /// <remarks>
    /// The bound is inclusive because the attempt start and the audit event can share a timestamp when the
    /// source clock resolution is coarser than the round trip. Callers must therefore treat this as a row
    /// filter that admits same-instant evidence, not as proof of causal advancement on its own.
    /// </remarks>
    public static bool HasQualifyingAuditProvenance(
        DateTimeOffset? attemptStartedAtUtc,
        DateTimeOffset? auditEventTimestamp)
        => attemptStartedAtUtc is not null
        && auditEventTimestamp is not null
        && auditEventTimestamp.Value >= attemptStartedAtUtc.Value;

    private static bool TrySplitOrderedVersion(string? value, out string prefix, out ulong sequence)
    {
        prefix = string.Empty;
        sequence = 0;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        ReadOnlySpan<char> span = value.AsSpan();
        int sequenceStart = span.Length;
        while (sequenceStart > 0 && char.IsAsciiDigit(span[sequenceStart - 1]))
        {
            sequenceStart--;
        }

        if (sequenceStart == span.Length
            || !ulong.TryParse(span[sequenceStart..], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out sequence))
        {
            return false;
        }

        prefix = value[..sequenceStart];
        return true;
    }
}
