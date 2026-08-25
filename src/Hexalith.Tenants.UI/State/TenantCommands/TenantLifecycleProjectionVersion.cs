using System.Globalization;

using Hexalith.Tenants.Contracts.Projections;

namespace Hexalith.Tenants.UI.State.TenantCommands;

/// <summary>
/// Parses and compares lifecycle projection versions without accepting incomparable evidence.
/// </summary>
internal static class TenantLifecycleProjectionVersion
{
    private const string TenantSequencePrefix = TenantProjectionVersionFormat.SequencePrefix;

    /// <summary>Classifies ordered advancement from a baseline to a current projection version.</summary>
    /// <param name="baseline">Pre-submit projection version.</param>
    /// <param name="current">Current authoritative projection version.</param>
    /// <returns>The ordered comparison result.</returns>
    internal static TenantLifecycleProjectionVersionComparison Compare(string? baseline, string? current)
    {
        if (!TrySplit(baseline, out string baselinePrefix, out ulong baselineSequence)
            || !TrySplit(current, out string currentPrefix, out ulong currentSequence))
        {
            return TenantLifecycleProjectionVersionComparison.Invalid;
        }

        if (!string.Equals(baselinePrefix, currentPrefix, StringComparison.Ordinal))
        {
            return TenantLifecycleProjectionVersionComparison.PrefixMismatch;
        }

        // A shared prefix is not by itself an ordering contract. Only the aggregate sequence token carries
        // one; a store-specific opaque marker (TenantQueryResult falls back to the state-store ETag) can
        // share a textual prefix and end in increasing digits without those digits meaning anything. Such a
        // pair used to compare as Advanced, which let non-causal churn satisfy the ordered-proof gate.
        if (!string.Equals(baselinePrefix, TenantSequencePrefix, StringComparison.Ordinal))
        {
            return TenantLifecycleProjectionVersionComparison.PrefixMismatch;
        }

        return currentSequence > baselineSequence
            ? TenantLifecycleProjectionVersionComparison.Advanced
            : TenantLifecycleProjectionVersionComparison.NotAdvanced;
    }

    /// <summary>Compares two versions for monotonic retained-evidence merging.</summary>
    /// <param name="incoming">Incoming projection version.</param>
    /// <param name="retained">Retained projection version.</param>
    /// <returns>A sequence comparison, or zero when the versions are not comparable.</returns>
    internal static int CompareSequences(string? incoming, string? retained)
    {
        if (!TrySplit(incoming, out string incomingPrefix, out ulong incomingSequence)
            || !TrySplit(retained, out string retainedPrefix, out ulong retainedSequence)
            || !string.Equals(incomingPrefix, retainedPrefix, StringComparison.Ordinal)
            || !string.Equals(incomingPrefix, TenantSequencePrefix, StringComparison.Ordinal))
        {
            return 0;
        }

        return incomingSequence.CompareTo(retainedSequence);
    }

    private static bool TrySplit(string? value, out string prefix, out ulong sequence)
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
            || !ulong.TryParse(
                span[sequenceStart..],
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out sequence))
        {
            return false;
        }

        prefix = value[..sequenceStart];
        return true;
    }
}
