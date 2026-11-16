using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Exercises member admission and diagnostic recovery using real source symbols.</summary>
public sealed class ProjectionMemberSafetyRecoveryTests
{
    /// <summary>Verifies excluded member shapes and supported payload repair without losing sibling output.</summary>
    [Theory]
    [InlineData("private string Sensitive { get; set; } = string.Empty;")]
    [InlineData("protected string Sensitive { get; set; } = string.Empty;")]
    [InlineData("internal string Sensitive { get; set; } = string.Empty;")]
    [InlineData("public static string Sensitive { get; set; } = string.Empty;")]
    [InlineData("public string this[int index] { get => string.Empty; set { } }")]
    [Trait("Category", "MutationErrorRegression")]
    public void ExcludedMemberAndUnsupportedPayloadCannotPoisonTheRepairedPartialProjection(string excludedMember)
    {
        string damagedSource = MembersSource(excludedMember, "object");
        string repairedSource = MembersSource("public string Sensitive { get; set; } = string.Empty;", "string");
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([
            damagedSource,
            """
            namespace Recovery;
            public partial class AffectedProjection
            {
                public string Alpha { get; set; } = string.Empty;
            }
            """,
            HealthySource]);
        AssertCompiles(compilation);
        ParseResult damaged = CompilationHelper.ParseProjection(compilation, "Recovery.AffectedProjection");
        DomainModel damagedModel = damaged.Model.ShouldNotBeNull();
        damagedModel.Properties.Select(property => property.Name).ShouldBe(["Alpha", "Raw", "Zulu"]);
        damagedModel.Properties.Single(property => property.Name == "Raw").IsUnsupported.ShouldBeTrue();
        AssertUnsupportedDiagnostic(compilation, damaged);
        DomainModel healthy = CompilationHelper.ParseProjection(compilation, "Recovery.HealthyProjection").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyProjection");
        healthyOutput.ShouldNotBeEmpty();
        string.Join('\n', Output(driver.GetRunResult(), "AffectedProjection")).ShouldContain("FcFieldPlaceholder");

        CSharpCompilation repaired = ReplaceFirstTree(compilation, repairedSource);
        AssertCompiles(repaired);
        ParseResult recovered = CompilationHelper.ParseProjection(repaired, "Recovery.AffectedProjection");
        recovered.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        DomainModel recoveredModel = recovered.Model.ShouldNotBeNull();
        recoveredModel.Properties.Select(property => property.Name).ShouldBe(["Alpha", "Raw", "Sensitive", "Zulu"]);
        PropertyModel raw = recoveredModel.Properties.Single(property => property.Name == "Raw");
        raw.IsUnsupported.ShouldBeFalse();
        raw.TypeName.ShouldBe("String");
        raw.SourceTypeName.ShouldBe("global::System.String");
        raw.IsWritable.ShouldBeTrue();
        raw.SupportsStaticAssignment.ShouldBeTrue();
        raw.Description.ShouldBe("Primary detail");
        raw.DisplayName.ShouldBe("Raw label");
        raw.FieldGroup.ShouldBe("Details");
        PropertyModel admitted = recoveredModel.Properties.Single(property => property.Name == "Sensitive");
        admitted.IsWritable.ShouldBeTrue();
        admitted.SupportsStaticAssignment.ShouldBeTrue();
        RazorModel view = RazorModelTransform.Transform(recoveredModel);
        view.Columns.Select(column => column.PropertyName).ShouldBe(["Alpha", "Raw", "Sensitive", "Zulu"]);
        view.Columns.Single(column => column.PropertyName == "Raw").Description.ShouldBe("Primary detail");
        recoveredModel.ShouldNotBe(damagedModel);
        CompilationHelper.ParseProjection(repaired, "Recovery.HealthyProjection").Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        string.Join('\n', Output(driver.GetRunResult(), "AffectedProjection")).ShouldNotContain("FcFieldPlaceholder");

        // Return to the same adverse source after a valid incremental result has been cached.
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        ParseResult rejectedAgain = CompilationHelper.ParseProjection(compilation, "Recovery.AffectedProjection");
        rejectedAgain.Model.ShouldBe(damagedModel);
        AssertUnsupportedDiagnostic(compilation, rejectedAgain);
        rejectedAgain.Model!.Properties.Select(property => property.Name).ShouldBe(["Alpha", "Raw", "Zulu"]);
        Output(driver.GetRunResult(), "HealthyProjection").ShouldBe(healthyOutput);
        string.Join('\n', Output(driver.GetRunResult(), "AffectedProjection")).ShouldContain("FcFieldPlaceholder");
    }

    /// <summary>Verifies declaration warnings retain their documented reason and source location after repair.</summary>
    [Theory]
    [InlineData("public class AffectedProjection", "Recovery.AffectedProjection", "HFC1003", "declared as partial", false)]
    [InlineData("public abstract partial class AffectedProjection", "Recovery.AffectedProjection", "HFC1004", "abstract types are not supported", false)]
    [InlineData("public partial class AffectedProjection<T>", "Recovery.AffectedProjection`1", "HFC1004", "generic types are not supported", false)]
    [InlineData("public partial class AffectedProjection", "Recovery.Container+AffectedProjection", "HFC1004", "nested in non-partial type 'Container'", true)]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidProjectionDeclarationRetainsItsReasonAndLocationAfterRepair(string declaration, string metadataName, string diagnosticId, string reason, bool nested)
    {
        string damagedSource = DeclarationSource(declaration, nested);
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([damagedSource, HealthySource]);
        AssertCompiles(damaged);
        ParseResult warned = CompilationHelper.ParseProjection(damaged, metadataName);
        warned.Model.ShouldNotBeNull();
        INamedTypeSymbol symbol = damaged.GetTypeByMetadataName(metadataName).ShouldNotBeNull();
        AssertDiagnostic(warned.Diagnostics.Single(diagnostic => diagnostic.Id == diagnosticId), diagnosticId, "Warning", "AffectedProjection", reason,
            symbol.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).GetLocation());
        DomainModel healthy = CompilationHelper.ParseProjection(damaged, "Recovery.HealthyProjection").Model.ShouldNotBeNull();

        CSharpCompilation repaired = ReplaceFirstTree(damaged, DeclarationSource("public partial class AffectedProjection", false));
        AssertCompiles(repaired);
        ParseResult recovered = CompilationHelper.ParseProjection(repaired, "Recovery.AffectedProjection");
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.ShouldNotBe(warned);
        DomainModel model = recovered.Model.ShouldNotBeNull();
        model.TypeName.ShouldBe("AffectedProjection");
        model.Namespace.ShouldBe("Recovery");
        model.Properties.Single().Name.ShouldBe("Name");
        model.Properties.Single().SourceTypeName.ShouldBe("global::System.String");
        CompilationHelper.ParseProjection(repaired, "Recovery.HealthyProjection").Model.ShouldBe(healthy);

        ParseResult warnedAgain = CompilationHelper.ParseProjection(damaged, metadataName);
        warnedAgain.Model.ShouldNotBeNull();
        AssertDiagnostic(warnedAgain.Diagnostics.Single(diagnostic => diagnostic.Id == diagnosticId), diagnosticId, "Warning", "AffectedProjection", reason,
            symbol.DeclaringSyntaxReferences.Single().GetSyntax(TestContext.Current.CancellationToken).GetLocation());
        warnedAgain.ShouldBe(warned);
        CompilationHelper.ParseProjection(damaged, "Recovery.HealthyProjection").Model.ShouldBe(healthy);
    }

    private static void AssertUnsupportedDiagnostic(CSharpCompilation compilation, ParseResult result)
    {
        IPropertySymbol property = compilation.GetTypeByMetadataName("Recovery.AffectedProjection")!
            .GetMembers("Raw").OfType<IPropertySymbol>().Single();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1002");
        AssertDiagnostic(diagnostic, "HFC1002", "Warning", "Raw", "not supported for auto-generation", property.Locations.Single());
        diagnostic.Message.ShouldContain("AffectedProjection");
        diagnostic.Message.ShouldContain("Fix: Use a supported type");
        diagnostic.Message.ShouldContain("DocsLink: https://hexalith.github.io/FrontComposer/diagnostics/HFC1002");
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

    private static CSharpCompilation ReplaceFirstTree(CSharpCompilation compilation, string source)
    {
        SyntaxTree old = compilation.SyntaxTrees.First();
        SyntaxTree replacement = CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)old.Options, old.FilePath,
            cancellationToken: TestContext.Current.CancellationToken);
        return compilation.ReplaceSyntaxTree(old, replacement);
    }

    private static void AssertCompiles(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.ToString()).ShouldBeEmpty();

    private static string[] Output(GeneratorDriverRunResult result, string typeName) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(typeName, StringComparison.Ordinal)).OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private static string MembersSource(string member, string rawType) => $$"""
        using System.ComponentModel;
        using System.ComponentModel.DataAnnotations;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Recovery")]
        public partial class AffectedProjection
        {
            public string Zulu { get; set; } = string.Empty;
            [Description("Primary detail")]
            [ProjectionFieldGroup("Details")]
            [Display(Name = "Raw label", Description = "Fallback detail", GroupName = "Details")]
            public {{rawType}} Raw { get; set; } = default!;
            {{member}}
        }
        """;

    private static string DeclarationSource(string declaration, bool nested)
    {
        string projection = $$"""
            [Projection]
            [BoundedContext("Recovery")]
            {{declaration}}
            {
                public string? Name { get; set; }
            }
            """;
        return "using Hexalith.FrontComposer.Contracts.Attributes;\nnamespace Recovery;\n"
            + (nested ? "public class Container {\n" + projection + "\n}" : projection);
    }

    private const string HealthySource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Recovery")]
        public partial class HealthyProjection
        {
            public string Name { get; set; } = string.Empty;
        }
        """;
}
