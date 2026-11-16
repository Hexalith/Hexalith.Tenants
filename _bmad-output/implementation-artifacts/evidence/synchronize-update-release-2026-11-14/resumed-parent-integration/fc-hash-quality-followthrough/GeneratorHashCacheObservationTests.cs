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
        CSharpCompilation first = CompilationHelper.CreateCompilation(Source("Pending"));
        PrivateHashActivity.Reset();
        DomainModel parsed = CompilationHelper.ParseProjection(first, "HashFixture.P").Model.ShouldNotBeNull();
        DomainModel equal = CompilationHelper.ParseProjection(first, "HashFixture.P").Model.ShouldNotBeNull();
        parsed.GetHashCode().ShouldBe(equal.GetHashCode());
        parsed.Equals(equal).ShouldBeTrue();
        long[] positiveControl = PrivateHashActivity.Snapshot();
        if (Environment.GetEnvironmentVariable("FC_PRIVATE_HASH_INSTRUMENTED") == "true")
        {
            positiveControl.Take(19).Sum().ShouldBeGreaterThan(0);
            positiveControl[19].ShouldBeGreaterThan(0);
        }
        PrivateHashActivity.Reset();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new FrontComposerGenerator().AsSourceGenerator()],
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        driver = driver.RunGenerators(first, TestContext.Current.CancellationToken);
        string initial = Output(driver.GetRunResult());
        driver = driver.RunGenerators(first, TestContext.Current.CancellationToken);
        Output(driver.GetRunResult()).ShouldBe(initial);
        driver.GetRunResult().Results.Single().TrackedSteps["Parse"].SelectMany(step => step.Outputs)
            .All(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).ShouldBeTrue();
        SyntaxTree tree = first.SyntaxTrees.Single();
        CSharpCompilation trivia = first.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(Source("Pending") + "\n// unrelated trivia", (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(trivia, TestContext.Current.CancellationToken);
        Output(driver.GetRunResult()).ShouldBe(initial);
        driver.GetRunResult().Results.Single().TrackedSteps["Parse"].SelectMany(step => step.Outputs)
            .All(output => output.Reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged).ShouldBeTrue();
        long[] beforeEdit = PrivateHashActivity.Snapshot();
        CSharpCompilation changed = trivia.ReplaceSyntaxTree(trivia.SyntaxTrees.Single(), CSharpSyntaxTree.ParseText(Source("Pending,Submitted"), (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(changed, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.Results.Single().Exception.ShouldBeNull();
        string updated = Output(result);
        updated.ShouldNotBe(initial);
        updated.ShouldContain("Submitted");
        result.Results.Single().TrackedSteps["Parse"].SelectMany(step => step.Outputs)
            .Any(output => output.Reason is IncrementalStepRunReason.Modified or IncrementalStepRunReason.New).ShouldBeTrue();
        PrivateHashObservationOutput.Write("generator-cache", new
        {
            roslynAssembly = typeof(IIncrementalGenerator).Assembly.FullName,
            positiveControl,
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
