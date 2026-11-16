using System;
using System.Threading;

namespace Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>Private diagnostic counters; never installed in the owner or proposed product source.</summary>
public static class PrivateHashActivity
{
    private static readonly long[] _counts = new long[20];

    /// <summary>Records one instrumented method entry.</summary>
    /// <param name="slot">The bound method inventory slot.</param>
    public static void Record(int slot) => Interlocked.Increment(ref _counts[slot]);

    /// <summary>Resets the private diagnostic counters.</summary>
    public static void Reset() => Array.Clear(_counts, 0, _counts.Length);

    /// <summary>Gets an independent snapshot of the private counters.</summary>
    public static long[] Snapshot() => (long[])_counts.Clone();
}
