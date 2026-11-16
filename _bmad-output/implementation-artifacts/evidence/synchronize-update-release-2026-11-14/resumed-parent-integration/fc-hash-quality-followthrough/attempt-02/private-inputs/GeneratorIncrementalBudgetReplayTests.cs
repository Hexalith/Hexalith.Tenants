namespace Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality;

/// <summary>Replays the unchanged existing finite NFR8 assertion without inventing a new budget.</summary>
public sealed class GeneratorIncrementalBudgetReplayTests
{
    /// <summary>Runs the existing warmup/five-sample median delta benchmark and its output checks.</summary>
    [Fact]
    public void ExistingGeneratorIncrementalBudgetIsReplayed()
        => new Benchmarks.IncrementalRebuildBenchmarkTests().IncrementalDeltaRebuild_AddOneProperty_CompletesUnder500ms();
}
