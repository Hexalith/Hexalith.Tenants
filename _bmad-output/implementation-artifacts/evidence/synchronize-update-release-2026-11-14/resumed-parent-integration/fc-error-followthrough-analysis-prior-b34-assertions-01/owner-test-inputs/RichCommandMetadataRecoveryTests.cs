using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Checks that malformed payload repair preserves scoped, fully typed command contracts.</summary>
public sealed class RichCommandMetadataRecoveryTests
{
    /// <summary>Verifies typed sibling metadata and namespace/scope identities survive payload recovery.</summary>
    [Theory]
    [InlineData("Metadata.Nested.Commands", "Ordering", "Ordering")]
    [InlineData("Metadata.Nested.Commands", null, "Default")]
    [InlineData("", null, "Default")]
    [Trait("Category", "MutationErrorRegression")]
    public void UnsupportedPayloadCannotPoisonTypedSiblingsOrScopedCommandMetadata(string sourceNamespace, string? declaredScope, string expectedScope)
    {
        ArgumentNullException.ThrowIfNull(sourceNamespace);
        ArgumentNullException.ThrowIfNull(expectedScope);
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([
            Source(sourceNamespace, declaredScope, "AffectedCommand", "object"),
            Source(sourceNamespace, declaredScope, "HealthyCommand", "string"),
            EnumSource(sourceNamespace)]);
        AssertCompiles(damaged);
        string affectedName = Qualify(sourceNamespace, "AffectedCommand");
        string healthyName = Qualify(sourceNamespace, "HealthyCommand");
        CommandParseResult warned = CompilationHelper.ParseCommand(damaged, affectedName);
        CommandModel damagedModel = warned.Model.ShouldNotBeNull();
        AssertWarning(damaged, affectedName, warned);
        AssertIdentities(damagedModel, sourceNamespace, declaredScope, expectedScope);
        AssertFields(damagedModel, rawUnsupported: true, sourceNamespace);
        CommandModel healthy = CompilationHelper.ParseCommand(damaged, healthyName).Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyCommand");
        string[] fallbackOutput = Output(driver.GetRunResult(), "AffectedCommand");
        healthyOutput.ShouldNotBeEmpty();
        string.Join('\n', fallbackOutput).ShouldContain("FcFieldPlaceholder");
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1002").Severity.ShouldBe(DiagnosticSeverity.Warning);

        CSharpCompilation repaired = ReplaceFirstTree(damaged, Source(sourceNamespace, declaredScope, "AffectedCommand", "string"));
        AssertCompiles(repaired);
        CommandParseResult recovered = CompilationHelper.ParseCommand(repaired, affectedName);
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.ShouldNotBe(warned);
        CommandModel model = recovered.Model.ShouldNotBeNull();
        model.ShouldNotBe(damagedModel);
        AssertIdentities(model, sourceNamespace, declaredScope, expectedScope);
        AssertFields(model, rawUnsupported: false, sourceNamespace);
        PropertyModel raw = model.Properties.Single(property => property.Name == "Raw");
        raw.SourceTypeName.ShouldBe("global::System.String");
        raw.IsWritable.ShouldBeTrue();
        raw.SupportsStaticAssignment.ShouldBeTrue();
        CompilationHelper.ParseCommand(repaired, healthyName).Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1002");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
        CSharpCompilation generated = repaired.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees);
        AssertCompiles(generated);
        generated.GetTypeByMetadataName(Qualify(sourceNamespace, "AffectedCommandLifecycleState")).ShouldNotBeNull();

        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        CommandParseResult warnedAgain = CompilationHelper.ParseCommand(damaged, affectedName);
        AssertWarning(damaged, affectedName, warnedAgain);
        warnedAgain.ShouldBe(warned);
        AssertFields(warnedAgain.Model.ShouldNotBeNull(), rawUnsupported: true, sourceNamespace);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
        Output(driver.GetRunResult(), "AffectedCommand").ShouldBe(fallbackOutput);
    }

    private static void AssertIdentities(CommandModel model, string sourceNamespace, string? declaredScope, string expectedScope)
    {
        model.TypeName.ShouldBe("AffectedCommand");
        model.Namespace.ShouldBe(sourceNamespace);
        model.BoundedContext.ShouldBe(declaredScope);
        model.DisplayName.ShouldBe("Commit order Command");
        model.IconName.ShouldBe("Save24");
        model.AuthorizationPolicyName.ShouldBe("Orders.Commit");
        model.Density.ShouldBe(CommandDensity.FullPage);
        model.DerivableProperties.Select(property => property.Name).ShouldBe(["MessageId"]);
        CommandFluxorModel fluxor = CommandFluxorTransform.Transform(model);
        fluxor.TypeName.ShouldBe("AffectedCommand");
        fluxor.Namespace.ShouldBe(sourceNamespace);
        fluxor.StateName.ShouldBe("AffectedCommandLifecycleState");
        fluxor.FeatureName.ShouldBe("AffectedCommandLifecycleFeature");
        fluxor.CommandFullyQualifiedName.ShouldBe(Qualify(sourceNamespace, "AffectedCommand"));
        fluxor.FeatureQualifiedName.ShouldBe(Qualify(sourceNamespace, "AffectedCommandLifecycleState"));
        fluxor.ActionsWrapperName.ShouldBe("AffectedCommandActions");
        fluxor.ReducersClassName.ShouldBe("AffectedCommandReducers");
        CommandFormModel form = CommandFormTransform.Transform(model);
        form.TypeName.ShouldBe("AffectedCommand");
        form.Namespace.ShouldBe(sourceNamespace);
        form.CommandFullyQualifiedName.ShouldBe(Qualify(sourceNamespace, "AffectedCommand"));
        form.BoundedContext.ShouldBe(declaredScope);
        form.ButtonLabel.ShouldBe("Commit order");
        form.AuthorizationPolicyName.ShouldBe("Orders.Commit");
        CommandRendererModel renderer = CommandRendererTransform.Transform(model, fluxor);
        renderer.BoundedContext.ShouldBe(expectedScope);
        renderer.CommandFullyQualifiedName.ShouldBe(Qualify(sourceNamespace, "AffectedCommand"));
        renderer.FullPageRoute.ShouldBe("/commands/" + expectedScope + "/AffectedCommand");
        renderer.DisplayLabel.ShouldBe("Commit order");
        renderer.IconName.ShouldBe("Save24");
        renderer.StateName.ShouldBe("AffectedCommandLifecycleState");
        renderer.FormComponentName.ShouldBe("AffectedCommandForm");
        renderer.ActionsWrapperName.ShouldBe("AffectedCommandActions");
        renderer.SubscriberTypeName.ShouldBe("AffectedCommandLastUsedSubscriber");
        renderer.NonDerivablePropertyNames.ShouldBe(new EquatableArray<string>([.. PayloadNames]));
        RegistrationModel registration = RegistrationModelTransform.TransformCommand(model);
        registration.Namespace.ShouldBe(sourceNamespace);
        registration.TypeName.ShouldBe("AffectedCommand");
        registration.BoundedContext.ShouldBe(expectedScope);
        registration.IsCommand.ShouldBeTrue();
        registration.HasFullPageRoute.ShouldBeTrue();
        registration.AuthorizationPolicyName.ShouldBe("Orders.Commit");
        McpCommandDescriptorModel descriptor = McpManifestTransform.TransformCommand(model);
        descriptor.CommandTypeName.ShouldBe(Qualify(sourceNamespace, "AffectedCommand"));
        descriptor.BoundedContext.ShouldBe(expectedScope);
        descriptor.ProtocolName.ShouldBe(expectedScope + ".AffectedCommand.Execute");
        descriptor.Title.ShouldBe("Commit order");
        descriptor.Description.ShouldBeNull();
        descriptor.AuthorizationPolicyName.ShouldBe("Orders.Commit");
        descriptor.DerivablePropertyNames.ShouldBe(["MessageId"]);
        string expectedDisambiguated = string.IsNullOrEmpty(sourceNamespace)
            ? expectedScope + ".AffectedCommand.Execute"
            : expectedScope + "." + sourceNamespace + ".AffectedCommand.Execute";
        McpManifestTransform.TransformCommand(model, disambiguateProtocolName: true).ProtocolName.ShouldBe(expectedDisambiguated);
    }

    private static void AssertFields(CommandModel model, bool rawUnsupported, string sourceNamespace)
    {
        model.NonDerivableProperties.Select(property => property.Name).ShouldBe(PayloadNames);
        CommandFormModel form = CommandFormTransform.Transform(model);
        form.Fields.Select(field => field.PropertyName).ShouldBe(PayloadNames);
        form.Fields.Select(field => field.TypeCategory).ShouldBe([
            FormFieldTypeCategory.NumberInput, FormFieldTypeCategory.DecimalInput, FormFieldTypeCategory.DecimalInput,
            FormFieldTypeCategory.DecimalInput, FormFieldTypeCategory.Switch, FormFieldTypeCategory.DatePicker,
            FormFieldTypeCategory.DatePicker, FormFieldTypeCategory.DatePicker, FormFieldTypeCategory.TimeInput,
            FormFieldTypeCategory.MonospaceText, FormFieldTypeCategory.Select, FormFieldTypeCategory.Placeholder,
            FormFieldTypeCategory.TextInput, rawUnsupported ? FormFieldTypeCategory.Placeholder : FormFieldTypeCategory.TextInput]);
        FormFieldModel quantity = form.Fields.Single(field => field.PropertyName == "Quantity");
        quantity.StaticLabel.ShouldBe("Quantity total");
        quantity.HasExplicitDisplayName.ShouldBeTrue();
        quantity.Description.ShouldBe("Recorded quantity");
        quantity.IsProviderCloneAssignable.ShouldBeTrue();
        form.Fields.Single(field => field.PropertyName == "Status").EnumFullyQualifiedName.ShouldBe(Qualify(sourceNamespace, "Stage"));
        McpCommandDescriptorModel descriptor = McpManifestTransform.TransformCommand(model);
        descriptor.Parameters.Select(field => field.Name).ShouldBe(PayloadNames);
        descriptor.Parameters.Select(field => field.TypeName).ShouldBe([
            "Int64", "Double", "Single", "Decimal", "Boolean", "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly",
            "Guid", "Enum", "Collection", "String", rawUnsupported ? "object" : "String"]);
        descriptor.Parameters.Select(field => field.JsonType).ShouldBe([
            "number", "number", "number", "number", "boolean", "string", "string", "string", "string", "string",
            "string", "array", "string", rawUnsupported ? "object" : "string"]);
        descriptor.Parameters.Select(field => field.IsNullable).ShouldBe([
            false, false, false, false, false, false, false, false, false, false, false, false, true, false]);
        descriptor.Parameters.Select(field => field.IsRequired).ShouldBe([
            true, true, true, true, true, true, true, true, true, true, true, true, false, !rawUnsupported]);
        descriptor.Parameters.Where(field => field.Name != "Raw").ShouldAllBe(field => !field.IsUnsupported);
        McpParameterDescriptorModel raw = descriptor.Parameters.Single(field => field.Name == "Raw");
        raw.IsUnsupported.ShouldBe(rawUnsupported);
        descriptor.Parameters.Single(field => field.Name == "Status").EnumValues.ShouldBe(["Draft", "Confirmed"]);
        descriptor.Parameters.Single(field => field.Name == "Quantity").Title.ShouldBe("Quantity total");
        descriptor.Parameters.Single(field => field.Name == "Quantity").Description.ShouldBe("Recorded quantity");
        form.Fields.Single(field => field.PropertyName == "Note").IsRequired.ShouldBeFalse();
    }

    private static void AssertWarning(CSharpCompilation compilation, string typeName, CommandParseResult result)
    {
        result.Model.ShouldNotBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single();
        diagnostic.Id.ShouldBe("HFC1002");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("Raw");
        diagnostic.Message.ShouldContain("AffectedCommand");
        diagnostic.Message.ShouldContain("not supported for auto-generation");
        IPropertySymbol property = compilation.GetTypeByMetadataName(typeName)!.GetMembers("Raw").OfType<IPropertySymbol>().Single();
        FileLinePositionSpan expected = property.Locations.Single().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
    }

    private static string Qualify(string sourceNamespace, string name) => sourceNamespace.Length == 0 ? name : sourceNamespace + "." + name;

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

    private static string Source(string sourceNamespace, string? declaredScope, string name, string rawType)
    {
        string namespaceDeclaration = sourceNamespace.Length == 0 ? string.Empty : "namespace " + sourceNamespace + ";";
        string scopeDeclaration = declaredScope is null ? string.Empty : "[BoundedContext(" + JsonSerializer.Serialize(declaredScope) + ")]";
        return $$"""
            using System;
            using System.Collections.Generic;
            using System.ComponentModel;
            using System.ComponentModel.DataAnnotations;
            using Hexalith.FrontComposer.Contracts.Attributes;
            {{namespaceDeclaration}}
            [Command]
            {{scopeDeclaration}}
            [RequiresPolicy("Orders.Commit")]
            [Display(Name = "Commit order Command")]
            [Icon("Save24")]
            public sealed class {{name}}
            {
                public string MessageId { get; set; } = string.Empty;
                [Display(Name = "Quantity total", Description = "Display fallback")]
                [Description("Recorded quantity")]
                public long Quantity { get; set; }
                public double Ratio { get; set; }
                public float Sample { get; set; }
                public decimal Amount { get; set; }
                public bool Enabled { get; set; }
                public DateTime ScheduledDate { get; set; }
                public DateTimeOffset UpdatedAt { get; set; }
                public DateOnly OnDate { get; set; }
                public TimeOnly AtTime { get; set; }
                public Guid RequestId { get; set; }
                public Stage Status { get; set; }
                public List<string> Tags { get; set; } = [];
                public string? Note { get; set; }
                public {{rawType}} Raw { get; set; } = default!;
            }
            """;
    }

    private static string EnumSource(string sourceNamespace) =>
        (sourceNamespace.Length == 0 ? string.Empty : "namespace " + sourceNamespace + ";\n") + "public enum Stage { Draft, Confirmed }";

    private static readonly string[] PayloadNames = [
        "Quantity", "Ratio", "Sample", "Amount", "Enabled", "ScheduledDate", "UpdatedAt", "OnDate", "AtTime", "RequestId",
        "Status", "Tags", "Note", "Raw"];
}
