using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects the public template parser when another legitimate attribute provider invokes it.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class TemplateParserProviderBoundaryTests
{
    /// <summary>Returns no marker or diagnostics for a nonmatching provider without failing generator execution.</summary>
    [Fact]
    public void OtherAttributeProviderReturnsAnEmptyTemplateResult()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation("[System.Obsolete] public class Unrelated { }");
        compilation.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        OtherAttributeGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator.AsSourceGenerator());
        GeneratorDriverRunResult result = driver.RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();

        result.Results.Single().Exception.ShouldBeNull();
        result.Diagnostics.ShouldBeEmpty();
        ProjectionTemplateMarkerResult parsed = generator.Results.ShouldHaveSingleItem();
        parsed.Marker.ShouldBeNull();
        parsed.Diagnostics.ShouldBeEmpty();
    }
}
