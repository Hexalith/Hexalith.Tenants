using System.Text.Json;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Exercises rejected default metadata and independent compiler-approved numeric repair.</summary>
public sealed class DefaultValueRecoveryTests
{
    /// <summary>Verifies invalid defaults cannot remain cached after a permitted conversion is restored.</summary>
    [Theory]
    [InlineData("sbyte", "long", "7.5d", false, SpecialType.System_SByte, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("sbyte", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_SByte, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("byte", "long", "7.5d", false, SpecialType.System_Byte, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("byte", "decimal", "7.5d", false, SpecialType.System_Byte, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("short", "int", "7.5d", false, SpecialType.System_Int16, SpecialType.System_Int32, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("short", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_Int16, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("ushort", "long", "7.5d", false, SpecialType.System_UInt16, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("ushort", "decimal", "7.5d", false, SpecialType.System_UInt16, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("int", "long", "7.5d", false, SpecialType.System_Int32, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("int", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_Int32, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("uint", "long", "7.5d", false, SpecialType.System_UInt32, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("uint", "decimal", "7.5d", false, SpecialType.System_UInt32, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("long", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_Int64, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("long", "decimal", "7.5d", false, SpecialType.System_Int64, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("ulong", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_UInt64, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("ulong", "decimal", "7.5d", false, SpecialType.System_UInt64, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("char", "long", "7.5d", false, SpecialType.System_Char, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("char", "decimal", "7.5d", false, SpecialType.System_Char, SpecialType.System_Decimal, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("float", "double", "typeof(decimal), \"7.5\"", false, SpecialType.System_Single, SpecialType.System_Double, FormFieldTypeCategory.DecimalInput, false)]
    [InlineData("int", "int", "long.MaxValue", true, SpecialType.System_Int32, SpecialType.System_Int32, FormFieldTypeCategory.NumberInput, false)]
    [InlineData("int", "long?", "\"not-a-number\"", true, SpecialType.System_Int32, SpecialType.System_Int64, FormFieldTypeCategory.NumberInput, true)]
    [Trait("Category", "MutationErrorRegression")]
    public void InvalidDefaultCannotPoisonARepairedNumericField(string sourceType, string propertyType, string damagedArguments,
        bool literalRepair, SpecialType expectedSource, SpecialType expectedTarget, FormFieldTypeCategory expectedCategory, bool nullable)
    {
        string metadataText = sourceType == "char" ? "A" : "7";
        string repairedArguments = literalRepair ? "7" : "typeof(" + sourceType + "), " + JsonSerializer.Serialize(metadataText);
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([Source(propertyType, damagedArguments), HealthySource]);
        AssertCompiles(damaged);
        AssertConversion(damaged, expectedTarget, nullable, implicitConversion: false);
        CommandParseResult damagedResult = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        AssertDefaultDiagnostic(damaged, damagedResult);
        CommandModel damagedModel = damagedResult.Model.ShouldNotBeNull();
        CommandModel healthy = CompilationHelper.ParseCommand(damaged, "Recovery.HealthyCommand").Model.ShouldNotBeNull();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator());
        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1012").Severity.ShouldBe(DiagnosticSeverity.Error);
        string[] healthyOutput = Output(driver.GetRunResult(), "HealthyCommand");
        healthyOutput.ShouldNotBeEmpty();

        CSharpCompilation repaired = ReplaceFirstTree(damaged, Source(propertyType, repairedArguments));
        AssertCompiles(repaired);
        AssertConversion(repaired, expectedTarget, nullable, implicitConversion: true);
        AttributeData attribute = DefaultAttribute(repaired);
        if (literalRepair)
        {
            attribute.ConstructorArguments.Length.ShouldBe(1);
            attribute.ConstructorArguments[0].Kind.ShouldBe(TypedConstantKind.Primitive);
            attribute.ConstructorArguments[0].Value.ShouldBe(7);
        }
        else
        {
            attribute.ConstructorArguments.Length.ShouldBe(2);
            attribute.ConstructorArguments[0].Kind.ShouldBe(TypedConstantKind.Type);
            attribute.ConstructorArguments[1].Value.ShouldBe(metadataText);
        }

        ITypeSymbol declaredDefaultType = DefaultType(attribute);
        declaredDefaultType.SpecialType.ShouldBe(expectedSource);
        CommandParseResult recovered = CompilationHelper.ParseCommand(repaired, "Recovery.AffectedCommand");
        recovered.Diagnostics.ShouldBeEmpty();
        recovered.ShouldNotBe(damagedResult);
        CommandModel model = recovered.Model.ShouldNotBeNull();
        // DefaultValue currently validates source metadata; it does not initialize or alter the field IR.
        model.ShouldBe(damagedModel);
        PropertyModel value = model.Properties.Single(property => property.Name == "Value");
        string typeName = repaired.GetSpecialType(expectedTarget).Name;
        value.TypeName.ShouldBe(typeName);
        value.SourceTypeName.ShouldBe("global::System." + typeName);
        value.IsNullable.ShouldBe(nullable);
        value.IsUnsupported.ShouldBeFalse();
        value.IsWritable.ShouldBeTrue();
        value.SupportsStaticAssignment.ShouldBeTrue();
        CommandFormModel form = CommandFormTransform.Transform(model);
        form.Fields.Select(field => field.PropertyName).ShouldBe(["Value", "Note"]);
        FormFieldModel field = form.Fields.Single(item => item.PropertyName == "Value");
        field.TypeName.ShouldBe(typeName);
        field.TypeCategory.ShouldBe(expectedCategory);
        field.IsNullable.ShouldBe(nullable);
        field.IsRequired.ShouldBe(!nullable);
        CompilationHelper.ParseCommand(repaired, "Recovery.HealthyCommand").Model.ShouldBe(healthy);
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        driver.GetRunResult().Diagnostics.ShouldNotContain(diagnostic => diagnostic.Id == "HFC1012");
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
        AssertCompiles(repaired.AddSyntaxTrees(driver.GetRunResult().GeneratedTrees));

        driver = driver.RunGenerators(damaged, TestContext.Current.CancellationToken);
        CommandParseResult damagedAgain = CompilationHelper.ParseCommand(damaged, "Recovery.AffectedCommand");
        damagedAgain.Model.ShouldNotBeNull();
        AssertDefaultDiagnostic(damaged, damagedAgain);
        damagedAgain.ShouldBe(damagedResult);
        driver.GetRunResult().Diagnostics.Single(diagnostic => diagnostic.Id == "HFC1012").Severity.ShouldBe(DiagnosticSeverity.Error);
        Output(driver.GetRunResult(), "HealthyCommand").ShouldBe(healthyOutput);
    }

    private static void AssertConversion(CSharpCompilation compilation, SpecialType expectedTarget, bool nullable, bool implicitConversion)
    {
        IPropertySymbol property = ValueProperty(compilation);
        ITypeSymbol target = property.Type;
        if (nullable)
        {
            INamedTypeSymbol wrapper = target.ShouldBeAssignableTo<INamedTypeSymbol>();
            wrapper.OriginalDefinition.SpecialType.ShouldBe(SpecialType.System_Nullable_T);
            target = wrapper.TypeArguments.Single();
        }

        target.SpecialType.ShouldBe(expectedTarget);
        Microsoft.CodeAnalysis.CSharp.Conversion conversion = compilation.ClassifyConversion(DefaultType(DefaultAttribute(compilation)), target);
        conversion.IsImplicit.ShouldBe(implicitConversion);
    }

    private static void AssertDefaultDiagnostic(CSharpCompilation compilation, CommandParseResult result)
    {
        result.Model.ShouldNotBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single();
        diagnostic.Id.ShouldBe("HFC1012");
        diagnostic.Severity.ShouldBe("Error");
        diagnostic.Message.ShouldContain("AffectedCommand.Value");
        diagnostic.Message.ShouldContain("has [DefaultValue] of type");
        diagnostic.Message.ShouldContain("not assignable");
        diagnostic.Message.ShouldContain(DefaultType(DefaultAttribute(compilation)).ToDisplayString());
        diagnostic.Message.ShouldContain(ValueProperty(compilation).Type.ToDisplayString());
        FileLinePositionSpan expected = DefaultAttribute(compilation).ApplicationSyntaxReference!
            .GetSyntax(TestContext.Current.CancellationToken).GetLocation().GetLineSpan();
        diagnostic.FilePath.ShouldBe(expected.Path);
        diagnostic.Line.ShouldBe(expected.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(expected.StartLinePosition.Character);
    }

    private static IPropertySymbol ValueProperty(CSharpCompilation compilation) => compilation.GetTypeByMetadataName("Recovery.AffectedCommand")!
        .GetMembers("Value").OfType<IPropertySymbol>().Single();

    private static AttributeData DefaultAttribute(CSharpCompilation compilation) => ValueProperty(compilation).GetAttributes()
        .Single(attribute => attribute.AttributeClass?.ToDisplayString() == "System.ComponentModel.DefaultValueAttribute");

    private static ITypeSymbol DefaultType(AttributeData attribute) => attribute.ConstructorArguments[0].Kind == TypedConstantKind.Type
        ? attribute.ConstructorArguments[0].Value.ShouldBeAssignableTo<ITypeSymbol>().ShouldNotBeNull()
        : attribute.ConstructorArguments[0].Type.ShouldNotBeNull();

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

    private static string Source(string propertyType, string arguments) => $$"""
        using System.ComponentModel;
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Command]
        [BoundedContext("Orders")]
        public sealed class AffectedCommand
        {
            public string MessageId { get; set; } = string.Empty;
            [DefaultValue({{arguments}})]
            public {{propertyType}} Value { get; set; }
            public string Note { get; set; } = string.Empty;
        }
        """;

    private const string HealthySource = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace Recovery;
        [Command]
        [BoundedContext("Orders")]
        public sealed class HealthyCommand
        {
            public string MessageId { get; set; } = string.Empty;
            public string Note { get; set; } = string.Empty;
        }
        """;
}
