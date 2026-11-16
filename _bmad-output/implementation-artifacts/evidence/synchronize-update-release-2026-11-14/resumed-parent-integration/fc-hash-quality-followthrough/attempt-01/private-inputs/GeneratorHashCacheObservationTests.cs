using System.Security.Cryptography;
using System.Text;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality;

/// <summary>Observes real generator cache consumers while checking emitted behavior.</summary>
public sealed class GeneratorHashCacheObservationTests
{
    /// <summary>Preserves cached output and observes a real WhenState edit without hash assumptions.</summary>
    [Fact]
    public void RealGeneratorRetainsCacheHitsAndEmitsChangedMetadata()
    {
        PrivateHashActivity.Reset();
        CSharpCompilation first = CompilationHelper.CreateCompilation(Source("Pending"));
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new FrontComposerGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(first, TestContext.Current.CancellationToken);
        string initial = Output(driver.GetRunResult());
        driver = driver.RunGenerators(first, TestContext.Current.CancellationToken);
        Output(driver.GetRunResult()).ShouldBe(initial);
        driver.GetRunResult().Results.Single().TrackedSteps["Parse"].SelectMany(step => step.Outputs)
            .ShouldAllBe(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged);
        long[] beforeEdit = PrivateHashActivity.Snapshot();
        CSharpCompilation changed = first.ReplaceSyntaxTree(first.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(Source("Pending,Submitted"), cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(changed, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.Results.Single().Exception.ShouldBeNull();
        string updated = Output(result);
        updated.ShouldNotBe(initial);
        updated.ShouldContain("Submitted");
        result.Results.Single().TrackedSteps["Parse"].SelectMany(step => step.Outputs)
            .ShouldContain(output => output.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New);
        PrivateHashObservationOutput.Write("generator-cache", new
        {
            roslynAssembly = typeof(IIncrementalGenerator).Assembly.FullName,
            beforeEdit,
            afterEdit = PrivateHashActivity.Snapshot(),
            initialOutputSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(initial))),
            changedOutputSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(updated))),
            hashCountAsserted = false,
        });
    }

    private static string Output(GeneratorDriverRunResult result)
        => string.Join("\n", result.Results.Single().GeneratedSources.OrderBy(source => source.HintName, StringComparer.Ordinal)
            .Select(source => source.HintName + "\n" + source.SourceText.ToString()));

    private static string Source(string whenState) => "using Hexalith.FrontComposer.Contracts.Attributes; namespace HashFixture; [Projection, ProjectionRole(ProjectionRole.ActionQueue, WhenState = \"" + whenState + "\")] public partial class P { public string Id { get; set; } = \"\"; public State Status { get; set; } } public enum State { Pending, Submitted }";
}
