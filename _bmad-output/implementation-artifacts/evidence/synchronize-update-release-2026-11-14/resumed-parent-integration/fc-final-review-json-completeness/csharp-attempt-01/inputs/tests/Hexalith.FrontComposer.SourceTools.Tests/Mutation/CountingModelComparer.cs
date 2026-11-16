namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Counts actual object comparisons made by a hash collection.</summary>
internal sealed class CountingModelComparer : IEqualityComparer<object>
{
    /// <summary>Gets the number of equality probes performed by the collection.</summary>
    public long Probes { get; private set; }

    /// <inheritdoc />
    public new bool Equals(object? first, object? second)
    {
        Probes++;
        return object.Equals(first, second);
    }

    /// <inheritdoc />
    public int GetHashCode(object value) => value.GetHashCode();
}
