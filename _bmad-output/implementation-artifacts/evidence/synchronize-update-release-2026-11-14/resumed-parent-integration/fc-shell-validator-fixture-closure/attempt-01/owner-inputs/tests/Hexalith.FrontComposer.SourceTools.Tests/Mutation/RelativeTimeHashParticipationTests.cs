using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Exercises the existing finite Level1 window equality/hash participation requirement.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class RelativeTimeHashParticipationTests
{
    /// <summary>Distinguishes the supported seven- and fourteen-day metadata and their model hashes.</summary>
    [Fact]
    public void SupportedWindowChangeParticipatesInTheDocumentedModelHash()
    {
        DomainModel seven = Parse(7);
        DomainModel fourteen = Parse(14);
        PropertyModel first = seven.Properties.Single();
        PropertyModel second = fourteen.Properties.Single();
        first.RelativeTimeWindowDays.ShouldBe(7);
        second.RelativeTimeWindowDays.ShouldBe(14);
        RazorModelTransform.Transform(seven).Columns.Single().RelativeTimeWindowDays.ShouldBe(7);
        RazorModelTransform.Transform(fourteen).Columns.Single().RelativeTimeWindowDays.ShouldBe(14);
        first.Equals(second).ShouldBeFalse();
        first.GetHashCode().ShouldNotBe(second.GetHashCode());
    }

    private static DomainModel Parse(int days)
    {
        string source = "using System; using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public partial class P { [RelativeTime(" + days + ")] public DateTime Changed { get; set; } }";
        return CompilationHelper.ParseProjection(CompilationHelper.CreateCompilation(source), "P").Model.ShouldNotBeNull();
    }
}
