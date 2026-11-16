using System.Globalization;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks diagnostic identity changes that intentionally leave generated model metadata unchanged.</summary>
public sealed class DiagnosticCacheRecoveryTests
{
    /// <summary>Verifies an unchanged model cannot suppress a warning moved to another source position.</summary>
    [Theory]
    [InlineData("line")]
    [InlineData("column")]
    [Trait("Category", "MutationErrorRegression")]
    public void MovingWarningCannotReuseAnIdenticalModelDiagnostic(string movement)
    {
        CSharpCompilation initial = CompilationHelper.CreateCompilation([PayloadSource("object", "none"), HealthySource]);
        AssertCompiles(initial);
        ParseResult first = CompilationHelper.ParseProjection(initial, "Cache.Metadata.AffectedProjection");
        DiagnosticInfo firstInfo = AssertWarning(initial, first, "Raw", "HFC1002");
        DomainModel healthy = CompilationHelper.ParseProjection(initial, "Cache.Metadata.HealthyProjection").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        AssertReportedWarning(driver.GetRunResult(), firstInfo);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyProjection");
        healthyOutput.ShouldNotBeEmpty();

        CSharpCompilation moved = ReplaceFirstTree(initial, PayloadSource("object", movement));
        AssertCompiles(moved);
        ParseResult movedResult = CompilationHelper.ParseProjection(moved, "Cache.Metadata.AffectedProjection");
        DiagnosticInfo movedInfo = AssertWarning(moved, movedResult, "Raw", "HFC1002");
        movedResult.Model.ShouldBe(first.Model.ShouldNotBeNull());
        movedInfo.Id.ShouldBe(firstInfo.Id);
        movedInfo.Message.ShouldBe(firstInfo.Message);
        movedInfo.Severity.ShouldBe(firstInfo.Severity);
        movedInfo.FilePath.ShouldBe(firstInfo.FilePath);
        movedInfo.Line.ShouldBe(firstInfo.Line + (movement == "line" ? 1 : 0));
        movedInfo.Column.ShouldBe(firstInfo.Column + (movement == "column" ? 4 : 0));
        movedInfo.ShouldNotBe(firstInfo);
        movedResult.ShouldNotBe(first);
        driver = driver.RunGenerators(moved, TestContext.Current.CancellationToken);
        AssertReportedWarning(driver.GetRunResult(), movedInfo);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);

        CSharpCompilation repaired = ReplaceFirstTree(moved, PayloadSource("string", movement));
        AssertCompiles(repaired);
        ParseResult recovered = CompilationHelper.ParseProjection(repaired, "Cache.Metadata.AffectedProjection");
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.Model.ShouldNotBeNull().Properties.Single(property => property.Name == "Raw").IsUnsupported.ShouldBeFalse();
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        AssertCompiles(repaired.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees));
        CompilationHelper.ParseProjection(repaired, "Cache.Metadata.HealthyProjection").Model.ShouldBe(healthy);

        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        ParseResult originalAgain = CompilationHelper.ParseProjection(initial, "Cache.Metadata.AffectedProjection");
        originalAgain.ShouldBe(first);
        AssertReportedWarning(driver.GetRunResult(), AssertWarning(initial, originalAgain, "Raw", "HFC1002"));
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
    }

    /// <summary>Verifies equal fallback models still report the current invalid window and recover cleanly.</summary>
    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void ChangedInvalidWindowCannotReuseThePreviousDiagnosticMessage()
    {
        CSharpCompilation initial = CompilationHelper.CreateCompilation([WindowSource(0), HealthySource]);
        AssertCompiles(initial);
        ParseResult first = CompilationHelper.ParseProjection(initial, "Cache.Metadata.AffectedProjection");
        DiagnosticInfo firstInfo = AssertWarning(initial, first, "UpdatedAt", "HFC1032");
        firstInfo.Message.ShouldContain("Got: 0.");
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        AssertReportedWarning(driver.GetRunResult(), firstInfo);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyProjection");
        string[] fallbackOutput = Output(driver.GetRunResult(), "AffectedProjection");
        healthyOutput.ShouldNotBeEmpty();

        CSharpCompilation changed = ReplaceFirstTree(initial, WindowSource(366));
        AssertCompiles(changed);
        ParseResult changedResult = CompilationHelper.ParseProjection(changed, "Cache.Metadata.AffectedProjection");
        DiagnosticInfo changedInfo = AssertWarning(changed, changedResult, "UpdatedAt", "HFC1032");
        changedResult.Model.ShouldBe(first.Model.ShouldNotBeNull());
        changedInfo.Id.ShouldBe(firstInfo.Id);
        changedInfo.Severity.ShouldBe(firstInfo.Severity);
        changedInfo.FilePath.ShouldBe(firstInfo.FilePath);
        changedInfo.Line.ShouldBe(firstInfo.Line);
        changedInfo.Column.ShouldBe(firstInfo.Column);
        changedInfo.Message.ShouldContain("Got: 366.");
        changedInfo.ShouldNotBe(firstInfo);
        changedResult.ShouldNotBe(first);
        PropertyModel fallback = changedResult.Model!.Properties.Single(property => property.Name == "UpdatedAt");
        fallback.DisplayFormat.ShouldBe(FieldDisplayFormat.Default);
        fallback.RelativeTimeWindowDays.ShouldBeNull();
        driver = driver.RunGenerators(changed, TestContext.Current.CancellationToken);
        AssertReportedWarning(driver.GetRunResult(), changedInfo);
        Output(driver.GetRunResult(), "AffectedProjection").ShouldBe(fallbackOutput);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);

        CSharpCompilation repaired = ReplaceFirstTree(changed, WindowSource(365));
        ParseResult recovered = CompilationHelper.ParseProjection(repaired, "Cache.Metadata.AffectedProjection");
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.ShouldNotBe(changedResult);
        PropertyModel timestamp = recovered.Model.ShouldNotBeNull().Properties.Single(property => property.Name == "UpdatedAt");
        timestamp.DisplayFormat.ShouldBe(FieldDisplayFormat.RelativeTime);
        timestamp.RelativeTimeWindowDays.ShouldBe(365);
        RazorModelTransform.Transform(recovered.Model!).Columns.Single(column => column.PropertyName == "UpdatedAt").RelativeTimeWindowDays.ShouldBe(365);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1032");
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        AssertCompiles(repaired.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees));

        driver = driver.RunGenerators(initial, TestContext.Current.CancellationToken);
        ParseResult originalAgain = CompilationHelper.ParseProjection(initial, "Cache.Metadata.AffectedProjection");
        originalAgain.ShouldBe(first);
        AssertReportedWarning(driver.GetRunResult(), AssertWarning(initial, originalAgain, "UpdatedAt", "HFC1032"));
        Output(driver.GetRunResult(), "AffectedProjection").ShouldBe(fallbackOutput);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
    }

    /// <summary>Verifies missing serialized coordinates recover at the valid zero-coordinate boundary.</summary>
    [Theory]
    [InlineData(" ", 0, 0)]
    [InlineData("/repo/adverse.cs", -1, 0)]
    [InlineData("/repo/adverse.cs", 0, -1)]
    [Trait("Category", "MutationErrorRegression")]
    public void UnavailableDiagnosticLocationCanRecoverAtTheZeroCoordinateBoundary(string filePath, int line, int column)
    {
        DiagnosticInfo unavailable = new("HFC1002", "Raw payload is unavailable", "Warning", filePath, line, column);
        unavailable.ToLocation().ShouldBe(Location.None);
        unavailable.ToLocation().Kind.ShouldBe(LocationKind.None);

        DiagnosticInfo repaired = new(unavailable.Id, unavailable.Message, unavailable.Severity, "/repo/adverse.cs", 0, 0);
        Location location = repaired.ToLocation();
        location.Kind.ShouldBe(LocationKind.ExternalFile);
        location.SourceSpan.Start.ShouldBe(0);
        location.SourceSpan.Length.ShouldBe(0);
        FileLinePositionSpan span = location.GetLineSpan();
        span.Path.ShouldBe("/repo/adverse.cs");
        span.StartLinePosition.ShouldBe(new LinePosition(0, 0));
        span.EndLinePosition.ShouldBe(new LinePosition(0, 0));
        repaired.Id.ShouldBe(unavailable.Id);
        repaired.Message.ShouldBe(unavailable.Message);
        repaired.Severity.ShouldBe(unavailable.Severity);

        unavailable.ToLocation().ShouldBe(Location.None);
        unavailable.Id.ShouldBe("HFC1002");
        unavailable.Message.ShouldBe("Raw payload is unavailable");
        unavailable.Severity.ShouldBe("Warning");
    }

    private static DiagnosticInfo AssertWarning(CSharpCompilation compilation, ParseResult result, string propertyName, string id)
    {
        result.Model.ShouldNotBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single();
        diagnostic.Id.ShouldBe(id);
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain(propertyName);
        diagnostic.Message.ShouldContain("AffectedProjection");
        IPropertySymbol property = compilation.GetTypeByMetadataName("Cache.Metadata.AffectedProjection")!
            .GetMembers(propertyName).OfType<IPropertySymbol>().Single();
        FileLinePositionSpan expected = property.Locations.Single().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
        return diagnostic;
    }

    private static void AssertReportedWarning(GeneratorDriverRunResult result, DiagnosticInfo expected)
    {
        Diagnostic diagnostic = result.Diagnostics.Single(item => item.Id == expected.Id);
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldBe(expected.Message);
        FileLinePositionSpan position = diagnostic.Location.GetLineSpan();
        position.Path.ShouldBe(expected.FilePath);
        position.StartLinePosition.Line.ShouldBe(expected.Line);
        position.StartLinePosition.Character.ShouldBe(expected.Column);
    }

    private static string PayloadSource(string rawType, string movement)
    {
        string rawProperty = "    public " + rawType + " Raw { get; set; } = default!;";
        if (movement == "line")
        {
            rawProperty = "\n" + rawProperty;
        }
        else if (movement == "column")
        {
            rawProperty = "    " + rawProperty;
        }

        return "using Hexalith.FrontComposer.Contracts.Attributes;\nnamespace Cache.Metadata;\n"
            + "[Projection]\n[BoundedContext(\"DiagnosticOps\")]\npublic partial class AffectedProjection\n{\n"
            + "    public string Name { get; set; } = string.Empty;\n" + rawProperty + "\n}\n";
    }

    private static string WindowSource(int window) => $$"""
        using System;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Cache.Metadata;
        [Projection]
        [BoundedContext("DiagnosticOps")]
        public partial class AffectedProjection
        {
            [RelativeTime({{window.ToString(CultureInfo.InvariantCulture)}})]
            public DateTimeOffset UpdatedAt { get; set; }
        }
        """;

    private static CSharpCompilation ReplaceFirstTree(CSharpCompilation compilation, string source)
    {
        SyntaxTree old = compilation.SyntaxTrees.First();
        return compilation.ReplaceSyntaxTree(old, CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)old.Options, old.FilePath,
            cancellationToken: TestContext.Current.CancellationToken));
    }

    private static void AssertCompiles(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();

    private static string[] Output(GeneratorDriverRunResult result, string typeName) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(typeName, StringComparison.Ordinal)).OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private const string HealthySource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Cache.Metadata;
        [Projection]
        [BoundedContext("DiagnosticOps")]
        public partial class HealthyProjection
        {
            public string Name { get; set; } = string.Empty;
        }
        """;
}
