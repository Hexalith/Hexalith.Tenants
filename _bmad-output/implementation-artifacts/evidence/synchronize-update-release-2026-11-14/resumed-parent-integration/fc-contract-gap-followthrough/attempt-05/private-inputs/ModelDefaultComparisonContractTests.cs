using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects externally observable model-default normalization through generic comparison and lookup.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ModelDefaultComparisonContractTests
{
    /// <summary>Matches omitted and empty paths while preserving each model's existing location identity.</summary>
    /// <param name="kind">The public model constructor family.</param>
    [Theory]
    [InlineData("projection")]
    [InlineData("command")]
    public void OmittedLocationAndExplicitEmptyRemainTheSameGenericKey(string kind)
    {
        if (kind == "projection")
        {
            AssertLookup(new DomainModel("P", "N", null, null, null, default),
                new DomainModel("P", "N", null, null, null, default, sourceFilePath: ""),
                new DomainModel("P", "N", null, null, null, default, sourceFilePath: "Source.cs"), pathParticipates: true);
        }
        else
        {
            AssertLookup(new CommandModel("C", "N", null, null, null, default, default, default),
                new CommandModel("C", "N", null, null, null, default, default, default, sourceFilePath: ""),
                new CommandModel("C", "N", null, null, null, default, default, default, sourceFilePath: "Source.cs"), pathParticipates: false);
        }
    }

    private static void AssertLookup<T>(T omitted, T explicitEmpty, T changed, bool pathParticipates) where T : notnull, IEquatable<T>
    {
        EqualityComparer<T>.Default.Equals(omitted, explicitEmpty).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(explicitEmpty, omitted).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(omitted, changed).ShouldBe(!pathParticipates);
        HashSet<T> keys = [omitted];
        keys.Contains(explicitEmpty).ShouldBeTrue();
        keys.Contains(changed).ShouldBe(!pathParticipates);
    }
}
