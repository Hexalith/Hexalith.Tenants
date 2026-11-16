using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Exercises unsafe provider assignments and invalid command-target repair.</summary>
public sealed class CommandTargetRecoveryTests
{
    /// <summary>Verifies unsafe assignments remain refused after a safe provider plan has been cached.</summary>
    [Theory]
    [InlineData("[DerivedFrom(DerivedFromSource.Context)] public int Guarded { get; private set; }", true, "non-public setter")]
    [InlineData("[DerivedFrom(DerivedFromSource.Context)] public int Guarded { get; init; }", true, "init")]
    [InlineData("[Obsolete(\"removed\", true)] [DerivedFrom(DerivedFromSource.Context)] public int Guarded { get; set; }", false, "")]
    [InlineData("[DerivedFrom(DerivedFromSource.Context)] public int Guarded { get; [Experimental(\"RECOVERY001\")] set; }", false, "")]
    [Trait("Category", "MutationErrorRegression")]
    public void UnsafeAssignmentCannotSurviveRepairOrPoisonAnUnaffectedProviderPlan(string unsafeMember, bool refusesCommand, string reason)
    {
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([AssignmentSource(unsafeMember), HealthySource]);
        AssertCompiles(damaged);
        CommandParseResult refused = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        AssertUnsafeAssignment(damaged, refused, refusesCommand, reason);
        CommandModel healthy = CompilationHelper.ParseCommand(damaged, "Recovery.HealthyCommand").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyCommand");
        healthyOutput.ShouldNotBeEmpty();
        AssertUnsafeOutput(driver.GetRunResult(), refusesCommand);

        CSharpCompilation repaired = ReplaceFirstTree(damaged, AssignmentSource("[DerivedFrom(DerivedFromSource.Context)] public int Guarded { get; set; }"));
        AssertCompiles(repaired);
        CommandParseResult recovered = CompilationHelper.ParseCommand(repaired, "Recovery.AffectedCommand");
        recovered.Diagnostics.ShouldBeEmpty();
        CommandModel model = recovered.Model.ShouldNotBeNull();
        PropertyModel property = model.Properties.Single(item => item.Name == "Guarded");
        property.IsWritable.ShouldBeTrue();
        property.SupportsStaticAssignment.ShouldBeTrue();
        property.SourceTypeName.ShouldBe("global::System.Int32");
        model.DerivableProperties.Select(item => item.Name).ShouldContain("Guarded");
        CommandFormModel form = CommandFormTransform.Transform(model);
        form.ProviderCloneFields.Single(item => item.PropertyName == "Guarded").IsProviderCloneAssignable.ShouldBeTrue();
        form.Fields.Select(item => item.PropertyName).ShouldBe(["Name"]);
        CompilationHelper.ParseCommand(repaired, "Recovery.HealthyCommand").Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
        string renderer = Renderer(driver.GetRunResult());
        GuardedCase(renderer).ShouldContain("TryConvertPropertyValue<global::System.Int32>");
        GuardedCase(renderer).ShouldContain("_prefilledModel.@Guarded =");
        AssertGeneratedCompiles(repaired, driver.GetRunResult());

        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        CommandParseResult refusedAgain = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        AssertUnsafeAssignment(damaged, refusedAgain, refusesCommand, reason);
        refusedAgain.ShouldBe(refused);
        AssertUnsafeOutput(driver.GetRunResult(), refusesCommand);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
    }

    /// <summary>Verifies invalid target metadata cannot reuse a previously repaired target descriptor.</summary>
    [Theory]
    [InlineData("CommandTargetResolutionMode.SameAsSource", "CommandTargetChangeKind.Delete", "Orders:Recovery.TargetProjection")]
    [InlineData("(CommandTargetResolutionMode)99", "CommandTargetChangeKind.Update", "Orders:Recovery.TargetProjection")]
    [InlineData("CommandTargetResolutionMode.Provider", "(CommandTargetChangeKind)99", "Orders:Recovery.TargetProjection")]
    [InlineData("CommandTargetResolutionMode.Provider", "CommandTargetChangeKind.Update", "Other:Recovery.TargetProjection")]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidTargetCannotReuseARepairedScopeKeyModeOrChangeKind(string resolution, string changeKind, string viewKey)
    {
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([TargetSource(resolution, changeKind, viewKey), HealthySource]);
        AssertCompiles(damaged);
        CommandParseResult refused = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        AssertTargetRefusal(damaged, refused);
        CommandModel damagedModel = refused.Model.ShouldNotBeNull();
        damagedModel.CommandTarget.ShouldBeNull();
        CommandFormTransform.Transform(damagedModel).CommandTarget.ShouldBeNull();
        CommandModel healthy = CompilationHelper.ParseCommand(damaged, "Recovery.HealthyCommand").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyCommand");
        healthyOutput.ShouldNotBeEmpty();

        CSharpCompilation repaired = ReplaceFirstTree(damaged, TargetSource("CommandTargetResolutionMode.Provider", "CommandTargetChangeKind.StatusMove", " Orders:Recovery.TargetProjection "));
        AssertCompiles(repaired);
        CommandParseResult recovered = CompilationHelper.ParseCommand(repaired, "Recovery.AffectedCommand");
        recovered.Diagnostics.ShouldBeEmpty();
        CommandModel model = recovered.Model.ShouldNotBeNull();
        CommandTargetModel target = model.CommandTarget.ShouldNotBeNull();
        target.ProjectionFullyQualifiedName.ShouldBe("global::Recovery.TargetProjection");
        target.ProjectionViewKey.ShouldBe("Orders:Recovery.TargetProjection");
        target.ViewKey.ShouldBe("Orders:Recovery.TargetProjection");
        target.ExpectedStatus.ShouldBe("Approved");
        target.ResolutionMode.ShouldBe(CommandTargetResolutionMode.Provider);
        target.ChangeKind.ShouldBe(CommandTargetChangeKind.StatusMove);
        model.BoundedContext.ShouldBe("Operations");
        CommandFormTransform.Transform(model).CommandTarget.ShouldBe(target);
        model.ShouldNotBe(damagedModel);
        CompilationHelper.ParseCommand(repaired, "Recovery.HealthyCommand").Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1005");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
        AssertGeneratedCompiles(repaired, driver.GetRunResult());

        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        CommandParseResult refusedAgain = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        AssertTargetRefusal(damaged, refusedAgain);
        refusedAgain.Model.ShouldBe(damagedModel);
        refusedAgain.Model!.CommandTarget.ShouldBeNull();
        driver.GetRunResult().Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "HFC1005");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
    }

    private static void AssertUnsafeAssignment(CSharpCompilation compilation, CommandParseResult result, bool refusesCommand, string reason)
    {
        if (refusesCommand)
        {
            result.Model.ShouldBeNull();
            DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1016");
            IPropertySymbol property = compilation.GetTypeByMetadataName("Recovery.AffectedCommand")!
                .GetMembers("Guarded").OfType<IPropertySymbol>().Single();
            AssertDiagnostic(diagnostic, "HFC1016", "Error", "Guarded", reason, property.Locations.Single());
            diagnostic.Message.ShouldContain("public non-init setter");
            diagnostic.Message.ShouldContain("{ get; set; }");
            return;
        }

        result.Diagnostics.ShouldBeEmpty();
        CommandModel model = result.Model.ShouldNotBeNull();
        PropertyModel guarded = model.Properties.Single(item => item.Name == "Guarded");
        guarded.IsWritable.ShouldBeTrue();
        guarded.SupportsStaticAssignment.ShouldBeFalse();
        guarded.SourceTypeName.ShouldBe("global::System.Int32");
    }

    private static void AssertUnsafeOutput(GeneratorDriverRunResult result, bool refusesCommand)
    {
        if (refusesCommand)
        {
            Output(result, "AffectedCommand").ShouldBeEmpty();
            return;
        }

        string arm = GuardedCase(Renderer(result));
        arm.ShouldContain("return false;");
        arm.ShouldNotContain("_prefilledModel.@Guarded =");
        arm.ShouldNotContain("TryConvertPropertyValue");
    }

    private static void AssertTargetRefusal(CSharpCompilation compilation, CommandParseResult result)
    {
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1005");
        INamedTypeSymbol symbol = compilation.GetTypeByMetadataName("Recovery.AffectedCommand").ShouldNotBeNull();
        AssertDiagnostic(diagnostic, "HFC1005", "Warning", "AffectedCommand", "invalid CommandTarget declaration",
            symbol.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).GetLocation());
        diagnostic.Message.ShouldContain("projection must be [Projection]");
        diagnostic.Message.ShouldContain("enum values must be known");
        diagnostic.Message.ShouldContain("SameAsSource is valid only with Update");
    }

    private static void AssertDiagnostic(DiagnosticInfo diagnostic, string id, string severity, string symbolName, string reason, Location location)
    {
        diagnostic.Id.ShouldBe(id);
        diagnostic.Severity.ShouldBe(severity);
        diagnostic.Message.ShouldContain(symbolName);
        diagnostic.Message.ShouldContain(reason);
        FileLinePositionSpan expected = location.GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
    }

    private static string Renderer(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources.Single(source =>
        source.HintName.EndsWith("AffectedCommand.CommandRenderer.g.razor.cs", StringComparison.Ordinal)).SourceText.ToString();

    private static string GuardedCase(string renderer)
    {
        const string marker = "            case \"Guarded\":";
        int start = renderer.IndexOf(marker, StringComparison.Ordinal);
        start.ShouldBeGreaterThanOrEqualTo(0);
        int end = renderer.IndexOf("            case ", start + marker.Length, StringComparison.Ordinal);
        if (end < 0)
        {
            end = renderer.IndexOf("            default:", start + marker.Length, StringComparison.Ordinal);
        }

        end.ShouldBeGreaterThan(start);
        return renderer[start..end];
    }

    private static CSharpCompilation ReplaceFirstTree(CSharpCompilation compilation, string source)
    {
        SyntaxTree old = compilation.SyntaxTrees.First();
        return compilation.ReplaceSyntaxTree(old, CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)old.Options, old.FilePath,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    private static void AssertCompiles(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();

    private static void AssertGeneratedCompiles(CSharpCompilation compilation, GeneratorDriverRunResult result) =>
        AssertCompiles(compilation.AddSyntaxTrees(result.GeneratedTrees));

    private static string[] Output(GeneratorDriverRunResult result, string typeName) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(typeName, StringComparison.Ordinal)).OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private static string AssignmentSource(string member) => $$"""
        using System;
        using System.Diagnostics.CodeAnalysis;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Command]
        [BoundedContext("Operations")]
        public sealed class AffectedCommand
        {
            public string MessageId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            {{member}}
        }
        """;

    private static string TargetSource(string resolution, string changeKind, string viewKey) => $$"""
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Orders")]
        public partial class TargetProjection
        {
            public string Name { get; set; } = string.Empty;
        }
        [Command]
        [BoundedContext("Operations")]
        [CommandTarget(typeof(TargetProjection), {{resolution}}, {{changeKind}}, ViewKey = "{{viewKey}}", ExpectedStatus = " Approved ")]
        public sealed class AffectedCommand
        {
            public string MessageId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        """;

    private const string HealthySource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Command]
        [BoundedContext("Operations")]
        public sealed class HealthyCommand
        {
            public string MessageId { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
        }
        """;
}
