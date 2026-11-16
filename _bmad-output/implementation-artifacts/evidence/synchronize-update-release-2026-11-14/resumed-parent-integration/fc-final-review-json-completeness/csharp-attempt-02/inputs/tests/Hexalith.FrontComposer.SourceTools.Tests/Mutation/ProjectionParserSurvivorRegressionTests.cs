using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects observable projection diagnostics and valid/malformed annotation handling.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ProjectionParserSurvivorRegressionTests
{
    /// <summary>Keeps invalid enum badge diagnostics attached to the declaring source location.</summary>
    [Fact]
    public void InvalidBadgeDiagnosticUsesEnumDeclarationFileAndCoordinates()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            Prefix + "[Projection] public partial class P { public State Value { get; set; } }",
            Prefix + "\npublic enum State {\n [ProjectionBadge((BadgeSlot)999)]\n Bad\n}",
        ]);
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1005");
        IFieldSymbol field = compilation.GetTypeByMetadataName("State")!.GetMembers("Bad").OfType<IFieldSymbol>().Single();
        FileLinePositionSpan expected = field.Locations.Single().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
        diagnostic.Message.ShouldContain("enum member 'Bad'");
        diagnostic.Message.ShouldContain("slot must be a defined BadgeSlot value");
        diagnostic.Severity.ShouldBe("Warning");
    }

    /// <summary>Keeps metadata badge reasons and the projection source fallback location.</summary>
    [Fact]
    public void InvalidMetadataBadgeKeepsReasonAndProjectionFileFallback()
    {
        CSharpCompilation external = CompilationHelper.CreateCompilation(
            Prefix + "public enum ExternalState { [ProjectionBadge((BadgeSlot)999)] Bad }")
            .WithAssemblyName("ExternalStates");
        AssertValid(external);
        using MemoryStream stream = new();
        external.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { public ExternalState Value { get; set; } }")
            .AddReferences(MetadataReference.CreateFromImage(stream.ToArray()));
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1005");
        diagnostic.FilePath.ShouldBe(compilation.SyntaxTrees.Single().FilePath);
        diagnostic.Message.ShouldContain("enum member 'Bad'");
        diagnostic.Message.ShouldContain("slot must be a defined BadgeSlot value");
        diagnostic.Severity.ShouldBe("Warning");
    }

    /// <summary>Accepts a supported public partial containing type.</summary>
    [Fact]
    public void PublicPartialOuterTypeDoesNotTriggerUnsupportedTypeDiagnostic()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "public partial class Outer { [Projection] public partial class P { public int Value { get; set; } } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "Outer+P");
        result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1004");
    }

    /// <summary>Uses the documented first-enum fallback when no enum has badge metadata.</summary>
    [Fact]
    public void WhenStateFallsBackToFirstEnumWhenNeitherEnumHasBadges()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"Pending\")] public partial class P { public First Z { get; set; } public Second A { get; set; } } public enum First { Pending } public enum Second { Other }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1022");

        CSharpCompilation unknownState = CompilationHelper.CreateCompilation(
            Prefix + "[Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"Other\")] public partial class P { public First Z { get; set; } public Second A { get; set; } } public enum First { Pending } public enum Second { Other }");
        AssertValid(unknownState);
        DiagnosticInfo diagnostic = CompilationHelper.ParseProjection(unknownState, "P").Diagnostics.Single(item => item.Id == "HFC1022");
        diagnostic.Message.ShouldContain("unknown enum member 'Other'");
        diagnostic.Message.ShouldContain("Valid members: Pending.");
    }

    /// <summary>Lists valid state members with readable separators in the diagnostic.</summary>
    [Fact]
    public void UnknownWhenStateListsReadableSeparatedValidMembers()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"Missing\")] public partial class P { public State Value { get; set; } } public enum State { Ready, Pending }");
        AssertValid(compilation);
        DiagnosticInfo diagnostic = CompilationHelper.ParseProjection(compilation, "P").Diagnostics.Single(item => item.Id == "HFC1022");
        diagnostic.Message.ShouldContain("Valid members: Pending, Ready.");
    }

    /// <summary>Accepts the valid parameterless description attribute without inventing description text.</summary>
    [Fact]
    public void ParameterlessDescriptionRemainsValidAndHasNoDescriptionText()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [Description] public int Value { get; set; } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        result.Diagnostics.ShouldBeEmpty();
        result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value").Description.ShouldBeNull();
    }

    /// <summary>Explains why the selected display format is unsupported for the enum underlying type.</summary>
    /// <param name="attribute">The display format attribute applied to the enum property.</param>
    [Theory]
    [InlineData("RelativeTime")]
    [InlineData("Currency")]
    public void InvalidEnumFormatDiagnosticExplainsUnderlyingEnumType(string attribute)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [" + attribute + "] public State Value { get; set; } } public enum State { Ready }");
        AssertValid(compilation);
        DiagnosticInfo diagnostic = CompilationHelper.ParseProjection(compilation, "P").Diagnostics.Single(item => item.Id == "HFC1032");
        diagnostic.Message.ShouldContain("enum underlying type int");
        diagnostic.Message.ShouldContain("Apply [" + attribute + "] only");
    }

    /// <summary>Keeps projection parsing available while source attribute arguments are incomplete.</summary>
    /// <param name="annotation">The incomplete source annotation.</param>
    /// <param name="onType">Whether the annotation is attached to the projection type.</param>
    [Theory]
    [InlineData("ProjectionEmptyStateCta()", true)]
    [InlineData("ProjectionFieldGroup()", false)]
    [InlineData("ColumnPriority()", false)]
    [InlineData("RelativeTime(null)", false)]
    public void MissingOrUnboundArgumentsKeepProjectionParsingAvailable(string annotation, bool onType)
    {
        string propertyType = annotation == "RelativeTime(null)" ? "DateTime" : "int";
        string source = onType
            ? Prefix + "[Projection, " + annotation + "] public partial class P { public " + propertyType + " Value { get; set; } }"
            : Prefix + "[Projection] public partial class P { [" + annotation + "] public " + propertyType + " Value { get; set; } }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        ParseResult result = Should.NotThrow(() => CompilationHelper.ParseProjection(compilation, "P"));
        DomainModel model = result.Model.ShouldNotBeNull();
        PropertyModel property = model.Properties.Single(item => item.Name == "Value");
        model.EmptyStateCtaCommandTypeName.ShouldBeNull();
        property.FieldGroup.ShouldBeNull();
        property.FieldGroupDescription.ShouldBeNull();
        property.ColumnPriority.ShouldBeNull();
        if (annotation == "RelativeTime(null)")
        {
            IPropertySymbol propertySymbol = compilation.GetTypeByMetadataName("P")!.GetMembers("Value").OfType<IPropertySymbol>().Single();
            propertySymbol.GetAttributes().Single().ConstructorArguments.ShouldBeEmpty();
            property.DisplayFormat.ShouldBe(FieldDisplayFormat.RelativeTime);
            property.RelativeTimeWindowDays.ShouldBe(7);
            result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1032");
        }
        else
        {
            property.DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
            property.RelativeTimeWindowDays.ShouldBeNull();
        }
    }

    /// <summary>Warns about an invalid relative-time constant and retains the default formatter.</summary>
    [Fact]
    public void InvalidRelativeTimeConstantWarnsAndKeepsDefaultFormatter()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [RelativeTime(1 / 0)] public DateTime Value { get; set; } }");
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        ParseResult result = Should.NotThrow(() => CompilationHelper.ParseProjection(compilation, "P"));
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1032");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("Expected: an int between 1 and 365");
        diagnostic.Message.ShouldContain("Got: null");
        diagnostic.Message.ShouldContain("Use [RelativeTime] (default 7)");
        diagnostic.Message.ShouldContain("Fallback:");
        PropertyModel property = result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value");
        property.DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
        property.RelativeTimeWindowDays.ShouldBeNull();
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private const string Prefix = "using System; using System.ComponentModel; using Hexalith.FrontComposer.Contracts.Attributes; ";
}
