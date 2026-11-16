using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality;

/// <summary>Observes finite collision costs without inventing a new collision pass/fail budget.</summary>
public sealed class ArrayHashCollectionObservationTests
{
    /// <summary>Preserves collection results and records equality probes for distinct two-item arrays.</summary>
    [Fact]
    public void PublicArrayCollectionSemanticsAndCollisionCostsAreRecorded()
    {
        const int count = 128;
        EquatableArray<PrivateArrayHashValue>[] values = Enumerable.Range(0, count).Select(Value).ToArray();
        PrivateArrayHashValue.Reset();
        HashSet<EquatableArray<PrivateArrayHashValue>> set = [];
        foreach (EquatableArray<PrivateArrayHashValue> value in values)
        {
            set.Add(value).ShouldBeTrue();
        }
        long insertionEqualityProbes = PrivateArrayHashValue.EqualsCalls;
        PrivateArrayHashValue.Reset();
        for (int i = 0; i < count; i++)
        {
            set.Contains(Value(i)).ShouldBeTrue();
            values[i].GetHashCode().ShouldBe(Value(i).GetHashCode());
        }
        long lookupEqualityProbes = PrivateArrayHashValue.EqualsCalls;
        set.Count.ShouldBe(count);
        PrivateHashObservationOutput.Write("array-collection", new
        {
            count,
            insertionEqualityProbes,
            lookupEqualityProbes,
            distinctHashes = values.Select(value => value.GetHashCode()).Distinct().Count(),
            collectionCorrect = true,
            collisionBudgetAsserted = false,
        });
    }

    private static EquatableArray<PrivateArrayHashValue> Value(int value)
        => new(ImmutableArray.Create(new PrivateArrayHashValue(value), new PrivateArrayHashValue(0)));
}
