using System.Globalization;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Exercises invalid relative-time windows, exact supported boundaries and incremental recovery.</summary>
public sealed class ProjectionFormatBoundaryRecoveryTests
{
    /// <summary>Verifies invalid windows cannot hide an allowed endpoint or reuse repaired formatting.</summary>
    [Theory]
    [InlineData("DateTime", 0, 1)]
    [InlineData("DateTimeOffset", 366, 365)]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidRelativeTimeWindowCanRecoverAtTheExactSupportedBoundary(string propertyType, int invalidWindow, int repairedWindow)
    {
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([Source(propertyType, invalidWindow), HealthySource]);
        AssertCompiles(damaged);
        ParseResult warned = CompilationHelper.ParseProjection(damaged, "Recovery.AffectedProjection");
        DomainModel damagedModel = warned.Model.ShouldNotBeNull();
        AssertWindowDiagnostic(damaged, warned, invalidWindow);
        AssertFormat(damagedModel, propertyType, FieldDisplayFormat.Default, null);
        DomainModel healthy = CompilationHelper.ParseProjection(damaged, "Recovery.HealthyProjection").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1032").Severity.ShouldBe(DiagnosticSeverity.Warning);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyProjection");
        string[] fallbackOutput = Output(driver.GetRunResult(), "AffectedProjection");
        healthyOutput.ShouldNotBeEmpty();
        fallbackOutput.ShouldNotBeEmpty();

        CSharpCompilation repaired = ReplaceFirstTree(damaged, Source(propertyType, repairedWindow));
        AssertCompiles(repaired);
        IPropertySymbol repairedProperty = TimestampProperty(repaired);
        AttributeData metadata = repairedProperty.GetAttributes().Single(attribute => attribute.AttributeClass?.Name == "RelativeTimeAttribute");
        metadata.ConstructorArguments.Single().Kind.ShouldBe(TypedConstantKind.Primitive);
        metadata.ConstructorArguments.Single().Type!.SpecialType.ShouldBe(SpecialType.System_Int32);
        metadata.ConstructorArguments.Single().Value.ShouldBe(repairedWindow);
        ParseResult recovered = CompilationHelper.ParseProjection(repaired, "Recovery.AffectedProjection");
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.ShouldNotBe(warned);
        DomainModel model = recovered.Model.ShouldNotBeNull();
        AssertFormat(model, propertyType, FieldDisplayFormat.RelativeTime, repairedWindow);
        model.ShouldNotBe(damagedModel);
        CompilationHelper.ParseProjection(repaired, "Recovery.HealthyProjection").Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1032");
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        Output(driver.GetRunResult(), "AffectedProjection").SequenceEqual(fallbackOutput, StringComparer.Ordinal).ShouldBeFalse();
        AssertCompiles(repaired.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees));

        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        ParseResult warnedAgain = CompilationHelper.ParseProjection(damaged, "Recovery.AffectedProjection");
        AssertWindowDiagnostic(damaged, warnedAgain, invalidWindow);
        DomainModel fallback = warnedAgain.Model.ShouldNotBeNull();
        AssertFormat(fallback, propertyType, FieldDisplayFormat.Default, null);
        warnedAgain.ShouldBe(warned);
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1032").Severity.ShouldBe(DiagnosticSeverity.Warning);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        Output(driver.GetRunResult(), "AffectedProjection").ShouldBe(fallbackOutput);
    }

    private static void AssertFormat(DomainModel model, string propertyType, FieldDisplayFormat format, int? window)
    {
        PropertyModel property = model.Properties.Single(item => item.Name == "UpdatedAt");
        property.TypeName.ShouldBe(propertyType);
        property.SourceTypeName.ShouldBe("global::System." + propertyType);
        property.IsNullable.ShouldBeFalse();
        property.IsUnsupported.ShouldBeFalse();
        property.DisplayFormat.ShouldBe(format);
        property.RelativeTimeWindowDays.ShouldBe(window);
        ColumnModel column = RazorModelTransform.Transform(model).Columns.Single(item => item.PropertyName == "UpdatedAt");
        column.TypeCategory.ShouldBe(TypeCategory.DateTime);
        column.DisplayFormat.ShouldBe(format);
        column.RelativeTimeWindowDays.ShouldBe(window);
    }

    private static void AssertWindowDiagnostic(CSharpCompilation compilation, ParseResult result, int window)
    {
        result.Model.ShouldNotBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single();
        diagnostic.Id.ShouldBe("HFC1032");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("AffectedProjection");
        diagnostic.Message.ShouldContain("UpdatedAt");
        diagnostic.Message.ShouldContain("invalid Level 1 format annotation metadata");
        diagnostic.Message.ShouldContain("Expected: an int between 1 and 365");
        diagnostic.Message.ShouldContain("Got: " + window.ToString(CultureInfo.InvariantCulture));
        diagnostic.Message.ShouldContain("Fix: Use [RelativeTime] (default 7) or pass an int between 1 and 365.");
        diagnostic.Message.ShouldContain("Fallback: Generated code keeps the column emitted and uses the existing default formatter.");
        FileLinePositionSpan expected = TimestampProperty(compilation).Locations.Single().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
    }

    private static IPropertySymbol TimestampProperty(CSharpCompilation compilation) => compilation.GetTypeByMetadataName("Recovery.AffectedProjection")!
        .GetMembers("UpdatedAt").OfType<IPropertySymbol>().Single();

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

    private static string Source(string propertyType, int window) => $$"""
        using System;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Orders")]
        public partial class AffectedProjection
        {
            [RelativeTime({{window.ToString(CultureInfo.InvariantCulture)}})]
            public {{propertyType}} UpdatedAt { get; set; }
            public string Name { get; set; } = string.Empty;
        }
        """;

    private const string HealthySource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Orders")]
        public partial class HealthyProjection
        {
            public string Name { get; set; } = string.Empty;
        }
        """;
}
