using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects the documented public IR filter-support mapping independently of generated UI consumers.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ColumnFilterContractTests
{
    /// <summary>Preserves the existing filter-support result for each declared category.</summary>
    /// <param name="category">The public column category.</param>
    /// <param name="expected">The documented filter-support result.</param>
    [Theory]
    [InlineData(TypeCategory.Text, true)]
    [InlineData(TypeCategory.Numeric, true)]
    [InlineData(TypeCategory.Enum, true)]
    [InlineData(TypeCategory.DateTime, true)]
    [InlineData(TypeCategory.Boolean, false)]
    [InlineData(TypeCategory.Collection, false)]
    [InlineData(TypeCategory.Unsupported, false)]
    public void CategoryRetainsItsDocumentedFilterSupport(TypeCategory category, bool expected)
    {
        ColumnModel column = new("Value", "Value", category, null, false, default);
        column.SupportsFilter.ShouldBe(expected);
    }
}
