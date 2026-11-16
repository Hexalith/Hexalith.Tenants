using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects author-facing metadata and diagnostic recovery contracts.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class MetadataDiagnosticContractTests
{
    /// <summary>Preserves meaningful bounded-context labels and treats blank labels as absent.</summary>
    /// <param name="label">The declared display label.</param>
    /// <param name="expected">The meaningful label, or null for a blank value.</param>
    [Theory]
    [InlineData("Shipments", "Shipments")]
    [InlineData("", null)]
    [InlineData(" ", null)]
    [InlineData("\t", null)]
    [InlineData(null, null)]
    public void CommandBoundedContextPreservesItsGroupingAndLabel(string? label, string? expected)
    {
        string source = Prefix + "[Command, BoundedContext(\" Sales \", DisplayLabel = " + Literal(label) + ")] public class C { public string MessageId { get; set; } = \"\"; }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        AssertValid(compilation);
        CommandModel model = CompilationHelper.ParseCommand(compilation, "C").Model.ShouldNotBeNull();
        model.BoundedContext.ShouldBe("Sales");
        model.BoundedContextDisplayLabel.ShouldBe(expected);
    }

    /// <summary>Explains an invalid projection context and clears its unusable display label.</summary>
    [Fact]
    public void InvalidProjectionContextExplainsWhyGroupingWasDiscarded()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Projection, BoundedContext(\" \", DisplayLabel = \"Sales\")] public partial class P { public int Value { get; set; } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        DomainModel model = result.Model.ShouldNotBeNull();
        model.BoundedContext.ShouldBeNull();
        model.BoundedContextDisplayLabel.ShouldBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1005");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("BoundedContext");
        diagnostic.Message.ShouldContain("name must be a non-empty string");
    }

    /// <summary>Explains the unsupported numeric role and keeps the documented default strategy.</summary>
    [Fact]
    public void UnknownProjectionRoleExplainsItsDefaultRenderingFallback()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Projection, ProjectionRole((ProjectionRole)999)] public partial class P { public int Value { get; set; } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1024");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("Unknown ProjectionRole value '999' on P");
        diagnostic.Message.ShouldContain("falling back to Default rendering");
        RazorModelTransform.Transform(result.Model.ShouldNotBeNull()).Strategy.ShouldBe(ProjectionRenderStrategy.Default);
    }

    /// <summary>Quotes invalid policy values without embedding invisible control characters in the reason.</summary>
    /// <param name="value">The invalid declared policy value.</param>
    /// <param name="echo">The readable diagnostic representation.</param>
    [Theory]
    [InlineData(null, "<null>")]
    [InlineData("A\tB", "'A\\tB'")]
    [InlineData("A\rB", "'A\\rB'")]
    [InlineData("A\nB", "'A\\nB'")]
    [InlineData("A\\B", "'A\\\\B'")]
    [InlineData("A\u0001B", "'A\\u0001B'")]
    public void InvalidPolicyDiagnosticKeepsItsReadableOffendingValue(string? value, string echo)
    {
        CommandParseResult result = ParsePolicy(value);
        result.Model.ShouldBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1056");
        diagnostic.Severity.ShouldBe("Error");
        diagnostic.Message.ShouldContain("Expected a non-empty policy name");
        diagnostic.Message.ShouldEndWith("Got: " + echo);
    }

    /// <summary>Bounds the offending value echo without truncating an exactly sixty-four-character value.</summary>
    /// <param name="length">The invalid value length around the documented echo boundary.</param>
    [Theory]
    [InlineData(64)]
    [InlineData(65)]
    public void InvalidPolicyEchoRetainsItsTruncationBoundary(int length)
    {
        string value = new string('A', length - 1) + "!";
        DiagnosticInfo diagnostic = ParsePolicy(value).Diagnostics.Single(item => item.Id == "HFC1056");
        string expected = length == 64 ? value : new string('A', 64) + "...";
        diagnostic.Message.ShouldEndWith("Got: '" + expected + "'");
    }

    /// <summary>Keeps flags-enum refusal attached to the property in a separate partial declaration.</summary>
    [Fact]
    public void FlagsCommandPropertyKeepsItsPlaceholderAndActualSourceLocation()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            Prefix + "[Command] public partial class C { public string MessageId { get; set; } = \"\"; }",
            Prefix + "public partial class C { public Access Value { get; set; } } [System.Flags] public enum Access { Read = 1, Write = 2 }",
        ]);
        AssertValid(compilation);
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, "C");
        PropertyModel property = result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value");
        property.IsUnsupported.ShouldBeTrue();
        property.EnumFullyQualifiedName.ShouldBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1008");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("[Flags] enum");
        diagnostic.Message.ShouldContain("FcFieldPlaceholder");
        FileLinePositionSpan expected = compilation.GetTypeByMetadataName("C")!.GetMembers("Value").OfType<IPropertySymbol>().Single().Locations.Single().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
    }

    private static CommandParseResult ParsePolicy(string? value)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Command, RequiresPolicy(" + Literal(value) + ")] public class C { public string MessageId { get; set; } = \"\"; }");
        AssertValid(compilation);
        return CompilationHelper.ParseCommand(compilation, "C");
    }

    private static string Literal(string? value) => value is null ? "null" : SyntaxFactory.Literal(value).ToFullString();

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private const string Prefix = "using Hexalith.FrontComposer.Contracts.Attributes; ";
}
