using System.Globalization;

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
            diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain(reason!);
            diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("1.0.0");
            AssertLocation(diagnostic, compilation);
        }
        Manifest(result).Contains("typeof(global::GapFixtures.T)", StringComparison.Ordinal).ShouldBe(included);
        if (included)
        {
            ArgumentListSyntax descriptor = Descriptors(result).Single(item => TemplateType(item) == "typeof(global::GapFixtures.T)");
            descriptor.Arguments.Single(item => item.NameColon!.Name.Identifier.ValueText == "ContractVersion").Expression.ToString()
                .ShouldBe(version.ToString(CultureInfo.InvariantCulture));
        }
        Recover(compilation, Source(1000000));
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
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Got: numeric role value 999.");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Fix: Remove the unsafe enum cast");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Fallback: This template is excluded");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
        GeneratorDriverRunResult recovered = Recover(compilation, Source(1000000, ", Role = ProjectionRole.DetailRecord"));
        Manifest(recovered).ShouldContain("ProjectionRole.DetailRecord");
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
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Got: Matching property without Microsoft.AspNetCore.Components.ParameterAttribute.");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Fix: Add `[Parameter]`");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
        Recover(compilation, Source(1000000));
    }

    /// <summary>Recovers a marker with missing required arguments while retaining the healthy sibling descriptor.</summary>
    [Fact]
    public void MissingProjectionArgumentExplainsTheRefusalAndCanRecover()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Source(1000000).Replace("ProjectionTemplate(typeof(P), 1000000)", "ProjectionTemplate", StringComparison.Ordinal));
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        GeneratorDriverRunResult result = Run(compilation);
        Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1033");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("missing its projection type argument");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Got: null.");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Fix: Pass typeof(MyProjection)");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
        Recover(compilation, Source(1000000));
    }

    /// <summary>Recovers a source-valid abstract template after explaining its unsupported shape.</summary>
    [Fact]
    public void AbstractTemplateExplainsItsShapeAndCanRecover()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Source(1000000).Replace("public partial class T :", "public abstract partial class T :", StringComparison.Ordinal));
        AssertValid(compilation);
        GeneratorDriverRunResult result = Run(compilation);
        Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1034");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Expected: A non-abstract, non-generic Razor component partial type.");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Got: the symbol is abstract.");
        AssertLocation(diagnostic, compilation);
        Manifest(result).ShouldNotContain("typeof(global::GapFixtures.T)");
        Recover(compilation, Source(1000000));
    }

    private static GeneratorDriverRunResult Recover(CSharpCompilation damaged, string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(damaged, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult before = driver.GetRunResult();
        string[] healthy = HealthyOutput(before);
        healthy.ShouldNotBeEmpty();
        Manifest(before).ShouldContain("typeof(global::GapFixtures.HealthyT)");
        string healthyDescriptor = HealthyDescriptor(before);
        SyntaxTree tree = damaged.SyntaxTrees.Single();
        CSharpCompilation repaired = damaged.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult after = driver.GetRunResult();
        after.Results.Single().Exception.ShouldBeNull();
        after.Diagnostics.Any(item => item.Id is "HFC1024" or "HFC1033" or "HFC1034" or "HFC1035" or "HFC1036").ShouldBeFalse();
        HealthyOutput(after).ShouldBe(healthy);
        Manifest(after).ShouldContain("typeof(global::GapFixtures.HealthyT)");
        HealthyDescriptor(after).ShouldBe(healthyDescriptor);
        Descriptors(after).Length.ShouldBe(2);
        Manifest(after).ShouldContain("typeof(global::GapFixtures.T)");
        AssertValid(repaired.AddSyntaxTrees(after.GeneratedTrees));
        CSharpCompilation restored = repaired.ReplaceSyntaxTree(repaired.SyntaxTrees.Last(), tree);
        driver = driver.RunGenerators(restored, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult reverted = driver.GetRunResult();
        reverted.Results.Single().Exception.ShouldBeNull();
        reverted.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal).ShouldBe(before.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal));
        restored.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal)
            .ShouldBe(damaged.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal));
        reverted.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString())
            .ShouldBe(before.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString()));
        HealthyDescriptor(reverted).ShouldBe(healthyDescriptor);
        Descriptors(reverted).Length.ShouldBe(Descriptors(before).Length);
        return after;
    }

    private static ArgumentListSyntax[] Descriptors(GeneratorDriverRunResult result) => CSharpSyntaxTree.ParseText(Manifest(result), cancellationToken: TestContext.Current.CancellationToken)
        .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ObjectCreationExpressionSyntax>()
        .Where(item => item.Type.ToString() == "global::Hexalith.FrontComposer.Contracts.Rendering.ProjectionTemplateDescriptor")
        .Select(item => item.ArgumentList!).ToArray();

    private static string TemplateType(ArgumentListSyntax descriptor) => descriptor.Arguments.Single(item => item.NameColon!.Name.Identifier.ValueText == "TemplateType").Expression.ToString();

    private static string HealthyDescriptor(GeneratorDriverRunResult result)
    {
        ArgumentListSyntax descriptor = Descriptors(result).Single(item => TemplateType(item) == "typeof(global::GapFixtures.HealthyT)");
        descriptor.Arguments.Select(item => item.NameColon!.Name.Identifier.ValueText + ":" + item.Expression.ToString()).ShouldBe([
            "ProjectionType:typeof(global::GapFixtures.HealthyP)",
            "Role:(global::Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole?)null",
            "TemplateType:typeof(global::GapFixtures.HealthyT)",
            "ContractVersion:1000000",
        ]);
        return descriptor.ToString();
    }

    private static string[] HealthyOutput(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Where(item => item.HintName.StartsWith("GapFixtures.HealthyP", StringComparison.Ordinal)).OrderBy(item => item.HintName, StringComparer.Ordinal)
        .Select(item => item.HintName + "\n" + item.SourceText.ToString()).ToArray();

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
        [Projection] public partial class HealthyP { public int Count { get; set; } }
        [ProjectionTemplate(typeof(HealthyP), 1000000)]
        public partial class HealthyT : ComponentBase
        {
            [Parameter] public ProjectionTemplateContext<HealthyP> Context { get; set; } = default!;
        }
        """;
}
