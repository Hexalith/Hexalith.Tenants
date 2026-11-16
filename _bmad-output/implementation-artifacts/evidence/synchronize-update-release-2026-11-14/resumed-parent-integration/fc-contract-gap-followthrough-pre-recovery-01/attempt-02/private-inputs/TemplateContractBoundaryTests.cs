using Hexalith.FrontComposer.SourceTools.Emitters;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects template role, context and packed-version diagnostics through the real generator.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class TemplateContractBoundaryTests
{
    /// <summary>Distinguishes exact/build compatibility from minor warnings and major exclusion.</summary>
    /// <param name="version">The marker's packed version.</param>
    /// <param name="diagnosticId">The expected version warning, if any.</param>
    /// <param name="reason">The existing packed-version reason text.</param>
    /// <param name="included">Whether the descriptor remains available.</param>
    [Theory]
    [InlineData(1000000, null, null, true)]
    [InlineData(1000007, null, null, true)]
    [InlineData(1005007, "HFC1036", "Got: 1.5.7 (raw value 1005007).", true)]
    [InlineData(9005007, "HFC1035", "marker requested major 9 (raw value 9005007).", false)]
    public void VersionDiagnosticsRetainThePackedReasonAndManifestDecision(int version, string? diagnosticId, string? reason, bool included)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Source(version));
        AssertValid(compilation);
        GeneratorDriverRunResult result = Run(compilation);
        if (diagnosticId is null)
        {
            result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1035" || item.Id == "HFC1036");
        }
        else
        {
            Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == diagnosticId);
            diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().ShouldContain(reason!);
            diagnostic.GetMessage().ShouldContain("1.0.0");
            AssertLocation(diagnostic, compilation);
        }
        Manifest(result).Contains("typeof(global::GapFixtures.T)", StringComparison.Ordinal).ShouldBe(included);
    }

    /// <summary>Explains a numeric unknown role and excludes the unsafe descriptor.</summary>
    [Fact]
    public void UnknownRoleRetainsReasonLocationAndDescriptorRefusal()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Source(1000000, ", Role = (ProjectionRole)999"));
        AssertValid(compilation);
        GeneratorDriverRunResult result = Run(compilation);
        Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1024");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().ShouldContain("Got: numeric role value 999.");
        diagnostic.GetMessage().ShouldContain("Fix: Remove the unsafe enum cast");
        diagnostic.GetMessage().ShouldContain("Fallback: This template is excluded");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
    }

    /// <summary>Explains a correctly typed Context that lacks its required Razor parameter marker.</summary>
    [Fact]
    public void MissingParameterExplainsTheContextContractAndExcludesTheDescriptor()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Source(1000000, parameter: false));
        AssertValid(compilation);
        GeneratorDriverRunResult result = Run(compilation);
        Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1034");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage().ShouldContain("Got: Matching property without Microsoft.AspNetCore.Components.ParameterAttribute.");
        diagnostic.GetMessage().ShouldContain("Fix: Add `[Parameter]`");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
    }

    private static GeneratorDriverRunResult Run(CSharpCompilation compilation)
    {
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        return result;
    }

    private static string Manifest(GeneratorDriverRunResult result) => result.GeneratedTrees.Single(tree => Path.GetFileName(tree.FilePath) == ProjectionTemplateManifestEmitter.GeneratedHintName).GetText(TestContext.Current.CancellationToken).ToString();

    private static void AssertLocation(Diagnostic diagnostic, CSharpCompilation compilation)
    {
        ClassDeclarationSyntax type = compilation.SyntaxTrees.Single().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single(item => item.Identifier.ValueText == "T");
        FileLinePositionSpan expected = type.GetLocation().GetLineSpan();
        FileLinePositionSpan actual = diagnostic.Location.GetLineSpan();
        actual.Path.ShouldBe(expected.Path);
        actual.StartLinePosition.ShouldBe(expected.StartLinePosition);
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private static string Source(int version, string role = "", bool parameter = true) => $$"""
        using Hexalith.FrontComposer.Contracts.Attributes;
        using Hexalith.FrontComposer.Contracts.Rendering;
        using Microsoft.AspNetCore.Components;
        namespace GapFixtures;
        [Projection] public partial class P { public int Count { get; set; } }
        [ProjectionTemplate(typeof(P), {{version}}{{role}})]
        public partial class T : ComponentBase
        {
            {{(parameter ? "[Parameter]" : "")}}
            public ProjectionTemplateContext<P> Context { get; set; } = default!;
        }
        """;
}
