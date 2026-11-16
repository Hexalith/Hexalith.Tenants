using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Emitters;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks projection continuation and incremental repair after malformed input.</summary>
public sealed class ProjectionErrorRecoveryTests {
    [Theory]
    [InlineData("object", "string", TypeCategory.Text)]
    [InlineData("byte[]", "int", TypeCategory.Numeric)]
    [InlineData("Dictionary<string, int>", "long", TypeCategory.Numeric)]
    [InlineData("(int X, int Y)", "decimal", TypeCategory.Numeric)]
    [InlineData("object", "double", TypeCategory.Numeric)]
    [InlineData("object", "float", TypeCategory.Numeric)]
    [InlineData("object", "bool", TypeCategory.Boolean)]
    [InlineData("object", "DateTime", TypeCategory.DateTime)]
    [InlineData("object", "DateTimeOffset", TypeCategory.DateTime)]
    [InlineData("object", "DateOnly", TypeCategory.DateTime)]
    [InlineData("object", "TimeOnly", TypeCategory.DateTime)]
    [InlineData("object", "Guid", TypeCategory.Text)]
    [InlineData("object", "Stage", TypeCategory.Enum)]
    [InlineData("object", "int?", TypeCategory.Numeric)]
    [InlineData("object", "List<string>", TypeCategory.Collection)]
    [Trait("Category", "MutationErrorRegression")]
    public void UnsupportedMemberRepairRestoresTypedOutputWithoutPoisoningItsSibling(string unsupported, string repaired, TypeCategory expectedCategory) {
        ArgumentNullException.ThrowIfNull(repaired);
        string healthy = Source("HealthyProjection", "string");
        string broken = Source("AffectedProjection", unsupported);
        string fixedSource = Source("AffectedProjection", repaired);
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([healthy, broken]);
        ParseResult parsed = CompilationHelper.ParseProjection(compilation, "Recovery.AffectedProjection");
        DomainModel model = parsed.Model.ShouldNotBeNull();
        PropertyModel rejected = model.Properties.Single(property => property.Name == "Raw");
        rejected.IsUnsupported.ShouldBeTrue();
        DiagnosticInfo warning = parsed.Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1002");
        IPropertySymbol property = compilation.GetTypeByMetadataName("Recovery.AffectedProjection")!
            .GetMembers("Raw").OfType<IPropertySymbol>().Single();
        FileLinePositionSpan expected = property.Locations.Single().GetLineSpan();
        warning.FilePath.ShouldBe(expected.Path);
        warning.Line.ShouldBe(expected.StartLinePosition.Line);
        warning.Column.ShouldBe(expected.StartLinePosition.Character);
        warning.Message.ShouldContain("Raw");
        warning.Severity.ShouldBe("Warning");

        RazorModel fallback = RazorModelTransform.Transform(model);
        fallback.Columns.Single(column => column.PropertyName == "Raw").TypeCategory.ShouldBe(TypeCategory.Unsupported);
        McpResourceDescriptorModel resource = McpManifestTransform.TransformProjection(model);
        resource.Fields.Single(field => field.Name == "Raw").IsUnsupported.ShouldBeTrue();
        McpParameterDescriptorModel rejectedField = resource.Fields.Single(field => field.Name == "Raw");
        rejectedField.TypeName.ShouldBe("Unsupported");
        rejectedField.JsonType.ShouldBe("object");
        rejectedField.IsRequired.ShouldBeFalse();
        rejectedField.IsNullable.ShouldBeFalse();
        rejectedField.Title.ShouldBe("Recovered payload");
        rejectedField.Description.ShouldBe("Payload narrative");

        GeneratorDriver driver = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult brokenResult = driver.GetRunResult();
        string[] unaffected = Output(brokenResult, "HealthyProjection");
        unaffected.ShouldNotBeEmpty();
        string.Join('\n', Output(brokenResult, "AffectedProjection")).ShouldContain("FcFieldPlaceholder");
        AssertGeneratedCompilation(compilation, brokenResult);

        SyntaxTree oldTree = compilation.SyntaxTrees.Last();
        SyntaxTree fixedTree = CSharpSyntaxTree.ParseText(fixedSource, (CSharpParseOptions)oldTree.Options, oldTree.FilePath, cancellationToken: TestContext.Current.CancellationToken);
        CSharpCompilation recovered = compilation.ReplaceSyntaxTree(oldTree, fixedTree);
        driver = driver.RunGenerators(recovered, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult recoveredResult = driver.GetRunResult();
        recoveredResult.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        Output(recoveredResult, "HealthyProjection").ShouldBe(unaffected);
        DomainModel recoveredModel = CompilationHelper.ParseProjection(recovered, "Recovery.AffectedProjection").Model.ShouldNotBeNull();
        PropertyModel recoveredRaw = recoveredModel.Properties.Single(item => item.Name == "Raw");
        recoveredRaw.IsUnsupported.ShouldBeFalse();
        recoveredRaw.IsNullable.ShouldBe(repaired.EndsWith('?'));
        recoveredModel.TypeName.ShouldBe("AffectedProjection");
        recoveredModel.Namespace.ShouldBe("Recovery");
        recoveredModel.BoundedContext.ShouldBe("Recovery.Views");
        RazorModel recoveredView = RazorModelTransform.Transform(recoveredModel);
        recoveredView.Columns.Single(column => column.PropertyName == "Raw").TypeCategory.ShouldBe(expectedCategory);
        recoveredView.Columns.Single(column => column.PropertyName == "Amount").Header.ShouldBe("Recovery amount");
        RegistrationModel registration = RegistrationModelTransform.Transform(recoveredModel);
        registration.TypeName.ShouldBe("AffectedProjection");
        registration.Namespace.ShouldBe("Recovery");
        registration.BoundedContext.ShouldBe("Recovery.Views");
        FluxorModel state = FluxorModelTransform.Transform(recoveredModel);
        state.StateName.ShouldBe("AffectedProjectionState");
        state.FeatureName.ShouldBe("AffectedProjectionFeature");
        McpManifestTransform.TransformProjection(recoveredModel).ProjectionTypeName.ShouldBe("Recovery.AffectedProjection");
        McpResourceDescriptorModel recoveredResource = McpManifestTransform.TransformProjection(recoveredModel);
        recoveredResource.BoundedContext.ShouldBe("Recovery.Views");
        recoveredResource.Name.ShouldBe("AffectedProjection");
        recoveredResource.Title.ShouldBe("Affected");
        recoveredResource.ProtocolUri.ShouldBe("frontcomposer://Recovery.Views/projections/AffectedProjection");
        McpManifestTransform.TransformProjection(recoveredModel, disambiguateProtocolUri: true).ProtocolUri
            .ShouldBe("frontcomposer://Recovery.Views/projections/Recovery.AffectedProjection");
        McpParameterDescriptorModel recoveredField = recoveredResource.Fields.Single(field => field.Name == "Raw");
        string expectedJsonType = expectedCategory switch
        {
            TypeCategory.Numeric => "number",
            TypeCategory.Boolean => "boolean",
            TypeCategory.Collection => "array",
            _ => "string",
        };
        recoveredField.TypeName.ShouldBe(expectedCategory.ToString());
        recoveredField.JsonType.ShouldBe(expectedJsonType);
        recoveredField.IsUnsupported.ShouldBeFalse();
        recoveredField.IsNullable.ShouldBe(repaired.EndsWith('?'));
        recoveredField.IsRequired.ShouldBe(!repaired.EndsWith('?'));
        recoveredField.Title.ShouldBe("Recovered payload");
        recoveredField.Description.ShouldBe("Payload narrative");
        recoveredField.EnumValues.ShouldBe(repaired == "Stage" ? ["Open", "Closed"] : Array.Empty<string>());
        recoveredModel.ShouldNotBe(model);
        DomainModel fresh = CompilationHelper.ParseProjection(recovered, "Recovery.AffectedProjection").Model.ShouldNotBeNull();
        recoveredModel.ShouldBe(fresh);
        recoveredModel.GetHashCode().ShouldBe(fresh.GetHashCode());
        RazorModelTransform.Transform(recoveredModel).ShouldBe(RazorModelTransform.Transform(fresh));
        JsonSerializer.Serialize(McpManifestTransform.TransformProjection(recoveredModel))
            .ShouldBe(JsonSerializer.Serialize(McpManifestTransform.TransformProjection(fresh)));
        AssertGeneratedCompilation(recovered, recoveredResult);

        // Rejection must return after a previously valid model was cached; the fixed output is not reusable.
        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult rejectedAgain = driver.GetRunResult();
        rejectedAgain.Diagnostics.Count(diagnostic => diagnostic.Id == "HFC1002").ShouldBe(1);
        Output(rejectedAgain, "AffectedProjection").ShouldBe(Output(brokenResult, "AffectedProjection"));
        Output(rejectedAgain, "HealthyProjection").ShouldBe(unaffected);
    }

    [Fact]
    [Trait("Category", "MutationErrorRegression")]
    public void TemplateCollisionRepairRestoresBothDescriptorsWithoutDroppingAnUnaffectedTemplate() {
        string projection = Source("AffectedProjection", "int") + "\n";
        string healthy = Source("HealthyProjection", "string");
        string initialTemplates = Templates("HealthyProjection");
        string collidingTemplates = Templates("AffectedProjection");
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([projection, healthy, initialTemplates]);
        GeneratorDriver driver = Driver().RunGenerators(compilation, TestContext.Current.CancellationToken);
        string initial = Manifest(driver.GetRunResult());
        initial.ShouldContain("TemplateA");
        initial.ShouldContain("TemplateB");
        initial.ShouldContain("UnaffectedTemplate");

        SyntaxTree before = compilation.SyntaxTrees.Last();
        SyntaxTree collision = CSharpSyntaxTree.ParseText(collidingTemplates, (CSharpParseOptions)before.Options, before.FilePath, cancellationToken: TestContext.Current.CancellationToken);
        CSharpCompilation invalid = compilation.ReplaceSyntaxTree(before, collision);
        driver = driver.RunGenerators(invalid, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult refused = driver.GetRunResult();
        refused.Diagnostics.ShouldContain(diagnostic => diagnostic.Id == "HFC1037");
        refused.Diagnostics.Where(diagnostic => diagnostic.Id == "HFC1037")
            .ShouldAllBe(diagnostic => diagnostic.Location.GetLineSpan().Path == before.FilePath);
        string suppressed = Manifest(refused);
        suppressed.ShouldNotContain("typeof(global::Recovery.TemplateA)");
        suppressed.ShouldNotContain("typeof(global::Recovery.TemplateB)");
        suppressed.ShouldContain("UnaffectedTemplate");

        driver = driver.RunGenerators(compilation, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult restored = driver.GetRunResult();
        restored.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1037");
        Manifest(restored).ShouldBe(initial);
    }

    private static CSharpGeneratorDriver Driver() => CSharpGeneratorDriver.Create(
        [new FrontComposerGenerator().AsSourceGenerator()],
        driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

    private static string[] Output(GeneratorDriverRunResult result, string typeName) => result.Results.Single().GeneratedSources
        .Where(source => source.HintName.Contains(typeName, StringComparison.Ordinal))
        .OrderBy(source => source.HintName, StringComparer.Ordinal)
        .Select(source => source.HintName + "\n" + source.SourceText).ToArray();

    private static string Manifest(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Single(source => source.HintName == ProjectionTemplateManifestEmitter.GeneratedHintName).SourceText.ToString();

    private static void AssertGeneratedCompilation(CSharpCompilation input, GeneratorDriverRunResult result) =>
        input.AddSyntaxTrees(result.GeneratedTrees).GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();

    private static string Source(string name, string rawType) => $$"""
        using System;
        using System.Collections.Generic;
        using System.ComponentModel;
        using System.ComponentModel.DataAnnotations;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Projection]
        [BoundedContext("Recovery.Views")]
        public partial class {{name}} {
            public string Id { get; set; } = "";
            public string? Note { get; set; }
            public int Count { get; set; }
            [Display(Name = "Recovery amount", GroupName = "Amounts")]
            public decimal Amount { get; set; }
            public bool Enabled { get; set; }
            public DateTimeOffset UpdatedAt { get; set; }
            [Display(Name = "Recovered payload", Description = "Display fallback")]
            [Description("Payload narrative")]
            public {{rawType}} Raw { get; set; } = default!;
        }
        """ + (rawType == "Stage" ? "\npublic enum Stage { Open, Closed }" : "");

    private static string Templates(string secondProjection) => $$"""
        using Hexalith.FrontComposer.Contracts.Attributes;
        using Hexalith.FrontComposer.Contracts.Rendering;
        using Microsoft.AspNetCore.Components;
        namespace Recovery;
        [ProjectionTemplate(typeof(AffectedProjection), ProjectionTemplateContractVersion.Current)]
        public partial class TemplateA {
            [Parameter] public ProjectionTemplateContext<AffectedProjection> Context { get; set; } = default!;
        }
        [ProjectionTemplate(typeof({{secondProjection}}), ProjectionTemplateContractVersion.Current)]
        public partial class TemplateB {
            [Parameter] public ProjectionTemplateContext<{{secondProjection}}> Context { get; set; } = default!;
        }
        [ProjectionTemplate(typeof(HealthyProjection), ProjectionTemplateContractVersion.Current, Role = ProjectionRole.DetailRecord)]
        public partial class UnaffectedTemplate {
            [Parameter] public ProjectionTemplateContext<HealthyProjection> Context { get; set; } = default!;
        }
        """;
}
