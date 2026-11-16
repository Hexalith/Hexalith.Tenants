namespace Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality;

/// <summary>Counts equality probes in a finite external collection diagnostic.</summary>
internal sealed class PrivateArrayHashValue(int value) : IEquatable<PrivateArrayHashValue>
{
    private static long _equalsCalls;

    internal static long EqualsCalls => Interlocked.Read(ref _equalsCalls);

    internal static void Reset() => Interlocked.Exchange(ref _equalsCalls, 0);

    /// <inheritdoc />
    public bool Equals(PrivateArrayHashValue? other)
    {
        Interlocked.Increment(ref _equalsCalls);
        return other is not null && value == other.Value;
    }

    private int Value => value;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PrivateArrayHashValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => value.GetHashCode();
}
