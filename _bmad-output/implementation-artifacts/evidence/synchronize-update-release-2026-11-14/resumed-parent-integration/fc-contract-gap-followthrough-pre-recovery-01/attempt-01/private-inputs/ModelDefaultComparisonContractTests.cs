using Hexalith.FrontComposer.SourceTools.Parsing;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects externally observable model-default normalization through generic comparison and lookup.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ModelDefaultComparisonContractTests
{
    /// <summary>Matches an omitted source path with its explicit empty value and retains a different path.</summary>
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
                new DomainModel("P", "N", null, null, null, default, sourceFilePath: "Source.cs"));
        }
        else
        {
            AssertLookup(new CommandModel("C", "N", null, null, null, default, default, default),
                new CommandModel("C", "N", null, null, null, default, default, default, sourceFilePath: ""),
                new CommandModel("C", "N", null, null, null, default, default, default, sourceFilePath: "Source.cs"));
        }
    }

    private static void AssertLookup<T>(T omitted, T explicitEmpty, T changed) where T : notnull, IEquatable<T>
    {
        EqualityComparer<T>.Default.Equals(omitted, explicitEmpty).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(explicitEmpty, omitted).ShouldBeTrue();
        EqualityComparer<T>.Default.Equals(omitted, changed).ShouldBeFalse();
        HashSet<T> keys = [omitted];
        keys.Contains(explicitEmpty).ShouldBeTrue();
        keys.Contains(changed).ShouldBeFalse();
    }
}
