using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects observable projection diagnostics and valid/malformed annotation handling.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class ProjectionParserSurvivorRegressionTests {
    [Fact]
    public void InvalidBadgeDiagnosticUsesEnumDeclarationFileAndCoordinates() {
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

    [Fact]
    public void InvalidMetadataBadgeKeepsReasonAndProjectionFileFallback() {
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

    [Fact]
    public void PublicPartialOuterTypeDoesNotTriggerUnsupportedTypeDiagnostic() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "public partial class Outer { [Projection] public partial class P { public int Value { get; set; } } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "Outer+P");
        result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1004");
    }

    [Fact]
    public void WhenStateFallsBackToFirstEnumWhenNeitherEnumHasBadges() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"Pending\")] public partial class P { public First A { get; set; } public Second Z { get; set; } } public enum First { Pending } public enum Second { Other }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        result.Model.ShouldNotBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1022");
    }

    [Fact]
    public void UnknownWhenStateListsReadableSeparatedValidMembers() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"Missing\")] public partial class P { public State Value { get; set; } } public enum State { Ready, Pending }");
        AssertValid(compilation);
        DiagnosticInfo diagnostic = CompilationHelper.ParseProjection(compilation, "P").Diagnostics.Single(item => item.Id == "HFC1022");
        diagnostic.Message.ShouldContain("Valid members: Pending, Ready.");
    }

    [Fact]
    public void ParameterlessDescriptionRemainsValidAndHasNoDescriptionText() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [Description] public int Value { get; set; } }");
        AssertValid(compilation);
        ParseResult result = CompilationHelper.ParseProjection(compilation, "P");
        result.Diagnostics.ShouldBeEmpty();
        result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value").Description.ShouldBeNull();
    }

    [Theory]
    [InlineData("RelativeTime")]
    [InlineData("Currency")]
    public void InvalidEnumFormatDiagnosticExplainsUnderlyingEnumType(string attribute) {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [" + attribute + "] public State Value { get; set; } } public enum State { Ready }");
        AssertValid(compilation);
        DiagnosticInfo diagnostic = CompilationHelper.ParseProjection(compilation, "P").Diagnostics.Single(item => item.Id == "HFC1032");
        diagnostic.Message.ShouldContain("enum underlying type int");
        diagnostic.Message.ShouldContain("Apply [" + attribute + "] only");
    }

    [Theory]
    [InlineData("ProjectionEmptyStateCta()", true)]
    [InlineData("ProjectionFieldGroup()", false)]
    [InlineData("ColumnPriority()", false)]
    [InlineData("RelativeTime(null)", false)]
    public void MissingOrUnboundArgumentsKeepProjectionParsingAvailable(string annotation, bool onType) {
        string source = onType
            ? Prefix + "[Projection, " + annotation + "] public partial class P { public int Value { get; set; } }"
            : Prefix + "[Projection] public partial class P { [" + annotation + "] public int Value { get; set; } }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        ParseResult result = Should.NotThrow(() => CompilationHelper.ParseProjection(compilation, "P"));
        result.Model.ShouldNotBeNull().Properties.ShouldContain(item => item.Name == "Value");
    }

    [Fact]
    public void InvalidRelativeTimeConstantWarnsAndKeepsDefaultFormatter() {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(
            Prefix + "[Projection] public partial class P { [RelativeTime(1 / 0)] public DateTime Value { get; set; } }");
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        ParseResult result = Should.NotThrow(() => CompilationHelper.ParseProjection(compilation, "P"));
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1032");
        diagnostic.Message.ShouldContain("Got: null");
        diagnostic.Message.ShouldContain("Fallback:");
        result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value").DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private const string Prefix = "using System; using System.ComponentModel; using Hexalith.FrontComposer.Contracts.Attributes; ";
}
