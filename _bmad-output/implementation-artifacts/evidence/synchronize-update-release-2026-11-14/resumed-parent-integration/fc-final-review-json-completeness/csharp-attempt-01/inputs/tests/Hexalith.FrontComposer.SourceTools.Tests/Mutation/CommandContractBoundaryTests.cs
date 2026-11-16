using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects command diagnostic, editing and conversion boundaries with actual source metadata.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class CommandContractBoundaryTests
{
    /// <summary>Explains unsupported command shapes at the declaring syntax location.</summary>
    /// <param name="declaration">The command declaration.</param>
    /// <param name="metadataName">The Roslyn metadata name.</param>
    /// <param name="reason">The existing unsupported-type reason.</param>
    [Theory]
    [InlineData("public abstract class C", "GapFixtures.C", "abstract types are not supported")]
    [InlineData("public class C<T>", "GapFixtures.C`1", "generic types are not supported")]
    public void UnsupportedCommandExplainsItsShapeAndLocation(string declaration, string metadataName, string reason)
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, Prefix + "[Command] " + declaration + " { public C() { } " + Members + " }"]);
        AssertValid(compilation);
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, metadataName);
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1004");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("[Command] attribute on 'C'");
        diagnostic.Message.ShouldContain(reason);
        AssertLocation(diagnostic, compilation.SyntaxTrees.Last().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single());
        Recover(compilation, Prefix + "[Command] public class C { public C() { } " + Members + " }", "HFC1004");
    }

    /// <summary>Rejects a missing policy argument without throwing during incomplete editing.</summary>
    [Fact]
    public void MissingPolicyArgumentRetainsTheRefusalReasonAndAttributeLocation()
    {
        string damaged = Prefix + "[Command, RequiresPolicy] public class C { " + Members + " }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, damaged]);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        CommandParseResult result = Should.NotThrow(() => CompilationHelper.ParseCommand(compilation, "GapFixtures.C"));
        result.Model.ShouldBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1056");
        diagnostic.Severity.ShouldBe("Error");
        diagnostic.Message.ShouldContain("Expected a non-empty policy name");
        diagnostic.Message.ShouldContain("Got: <null>");
        AssertLocation(diagnostic, compilation.SyntaxTrees.Last().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<AttributeSyntax>().Single(item => item.Name.ToString() == "RequiresPolicy"));
        (CSharpCompilation repaired, GeneratorDriverRunResult generated) = Recover(compilation, damaged.Replace("RequiresPolicy]", "RequiresPolicy(\"Gap.Manage\")]", StringComparison.Ordinal), "HFC1056");
        CompilationHelper.ParseCommand(repaired, "GapFixtures.C").Model.ShouldNotBeNull().AuthorizationPolicyName.ShouldBe("Gap.Manage");
        string.Join('\n', Output(generated, "GapFixtures.C")).ShouldContain("Gap.Manage");
    }

    /// <summary>Preserves absent icon metadata when its required source argument is missing.</summary>
    [Fact]
    public void MissingIconArgumentKeepsTheCommandAvailableWithoutAnIcon()
    {
        string damaged = Prefix + "[Command, Icon] public class C { " + Members + " }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, damaged]);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        CommandParseResult result = Should.NotThrow(() => CompilationHelper.ParseCommand(compilation, "GapFixtures.C"));
        result.Model.ShouldNotBeNull().IconName.ShouldBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1056");
        (CSharpCompilation repaired, _) = Recover(compilation, damaged.Replace("Icon]", "Icon(\"Regular.Size16.Play\")]", StringComparison.Ordinal), null);
        CompilationHelper.ParseCommand(repaired, "GapFixtures.C").Model.ShouldNotBeNull().IconName.ShouldBe("Regular.Size16.Play");
    }

    /// <summary>Admits the existing implicit numeric default conversions using Roslyn as an independent oracle.</summary>
    /// <param name="sourceType">The declared default metadata type.</param>
    /// <param name="text">The type-string default payload.</param>
    /// <param name="expectedSource">The expected Roslyn primitive source type.</param>
    [Theory]
    [InlineData("sbyte", "7", SpecialType.System_SByte)]
    [InlineData("byte", "7", SpecialType.System_Byte)]
    [InlineData("ushort", "7", SpecialType.System_UInt16)]
    [InlineData("char", "A", SpecialType.System_Char)]
    public void ImplicitNumericDefaultRemainsAdmitted(string sourceType, string text, SpecialType expectedSource)
    {
        string repairedSource = Prefix + "[Command] public class C { " + Members + " [DefaultValue(typeof(" + sourceType + "), \"" + text + "\")] public int Value { get; set; } }";
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, repairedSource.Replace("typeof(" + sourceType + "), \"" + text + "\"", "typeof(decimal), \"7.5\"", StringComparison.Ordinal)]);
        AssertValid(damaged);
        CommandParseResult refused = CompilationHelper.ParseCommand(damaged, "GapFixtures.C");
        DiagnosticInfo diagnostic = refused.Diagnostics.Single(item => item.Id == "HFC1012");
        diagnostic.Severity.ShouldBe("Error");
        diagnostic.Message.ShouldContain("of type 'decimal' which is not assignable to the property type 'int'");
        AssertLocation(diagnostic, damaged.SyntaxTrees.Last().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<AttributeSyntax>().Single(item => item.Name.ToString() == "DefaultValue"));
        (CSharpCompilation compilation, _) = Recover(damaged, repairedSource, "HFC1012");
        IPropertySymbol property = compilation.GetTypeByMetadataName("GapFixtures.C")!.GetMembers("Value").OfType<IPropertySymbol>().Single();
        ITypeSymbol declaredDefault = (ITypeSymbol)property.GetAttributes().Single().ConstructorArguments[0].Value!;
        declaredDefault.SpecialType.ShouldBe(expectedSource);
        compilation.ClassifyConversion(declaredDefault, property.Type).IsImplicit.ShouldBeTrue();
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, "GapFixtures.C");
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1012");
        CommandModel model = result.Model.ShouldNotBeNull();
        model.Properties.Single(item => item.Name == "Value").SourceTypeName.ShouldBe("global::System.Int32");
        CommandFormTransform.Transform(model).Fields.Single(item => item.PropertyName == "Value").TypeCategory.ShouldBe(FormFieldTypeCategory.NumberInput);
        model.ShouldBe(refused.Model);
    }

    /// <summary>Recovers a missing DefaultValue argument without throwing or inventing field initialization metadata.</summary>
    [Fact]
    public void MissingDefaultArgumentRecoversWithoutPoisoningAHealthyCommand()
    {
        string damaged = Prefix + "[Command] public class C { " + Members + " [DefaultValue] public int Value { get; set; } }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, damaged]);
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        CommandParseResult result = Should.NotThrow(() => CompilationHelper.ParseCommand(compilation, "GapFixtures.C"));
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1012");
        result.Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value").TypeName.ShouldBe("Int32");
        (CSharpCompilation repaired, _) = Recover(compilation, damaged.Replace("[DefaultValue]", "[DefaultValue(7)]", StringComparison.Ordinal), null);
        CompilationHelper.ParseCommand(repaired, "GapFixtures.C").Diagnostics.ShouldNotContain(item => item.Id == "HFC1012");
    }

    /// <summary>Repairs incompatible metadata to a nullable null default and retains nullable form semantics.</summary>
    [Fact]
    public void NullableNullDefaultRecoversWithoutAFalseTypeError()
    {
        string damaged = Prefix + "[Command] public class C { " + Members + " [DefaultValue(\"bad\")] public int? Value { get; set; } }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, damaged]);
        AssertValid(compilation);
        CompilationHelper.ParseCommand(compilation, "GapFixtures.C").Diagnostics.Single(item => item.Id == "HFC1012").Severity.ShouldBe("Error");
        (CSharpCompilation repaired, _) = Recover(compilation, damaged.Replace("DefaultValue(\"bad\")", "DefaultValue(null)", StringComparison.Ordinal), "HFC1012");
        IPropertySymbol symbol = repaired.GetTypeByMetadataName("GapFixtures.C")!.GetMembers("Value").OfType<IPropertySymbol>().Single();
        symbol.GetAttributes().Single().ConstructorArguments[0].IsNull.ShouldBeTrue();
        CommandParseResult recovered = CompilationHelper.ParseCommand(repaired, "GapFixtures.C");
        recovered.Diagnostics.ShouldNotContain(item => item.Id == "HFC1012");
        CommandModel model = recovered.Model.ShouldNotBeNull();
        model.Properties.Single(item => item.Name == "Value").IsNullable.ShouldBeTrue();
        CommandFormTransform.Transform(model).Fields.Single(item => item.PropertyName == "Value").IsRequired.ShouldBeFalse();
    }

    /// <summary>Retains the existing global-namespace target fallback in parsed and form metadata.</summary>
    [Fact]
    public void GlobalProjectionTargetRetainsItsExistingScopeFallback()
    {
        const string source = "using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public sealed partial class P { } [Command, CommandTarget(typeof(P), CommandTargetResolutionMode.Provider, CommandTargetChangeKind.Update, ViewKey = \"Wrong\")] public class C { public string MessageId { get; set; } = \"\"; }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation([HealthySource, source]);
        AssertValid(compilation);
        CommandParseResult refused = CompilationHelper.ParseCommand(compilation, "C");
        refused.Model.ShouldNotBeNull().CommandTarget.ShouldBeNull();
        DiagnosticInfo diagnostic = refused.Diagnostics.Single(item => item.Id == "HFC1005");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("invalid CommandTarget declaration");
        (CSharpCompilation repaired, _) = Recover(compilation, source.Replace("\"Wrong\"", "\":P\"", StringComparison.Ordinal), "HFC1005", affectedPrefix: "C");
        CommandModel model = CompilationHelper.ParseCommand(repaired, "C").Model.ShouldNotBeNull();
        model.CommandTarget.ShouldNotBeNull().ProjectionViewKey.ShouldBe(":P");
        CommandFormTransform.Transform(model).CommandTarget.ShouldNotBeNull().ProjectionViewKey.ShouldBe(":P");
    }

    private static (CSharpCompilation Compilation, GeneratorDriverRunResult Generated) Recover(CSharpCompilation damaged, string repairedSource, string? diagnosticId, string affectedPrefix = "GapFixtures.C")
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(damaged, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult before = driver.GetRunResult();
        before.Results.Single().Exception.ShouldBeNull();
        if (diagnosticId is not null)
        {
            before.Diagnostics.ShouldContain(item => item.Id == diagnosticId);
        }
        string[] healthy = Output(before, "GapFixtures.Healthy");
        healthy.ShouldNotBeEmpty();
        SyntaxTree tree = damaged.SyntaxTrees.Last();
        CSharpCompilation repaired = damaged.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(repairedSource, (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult after = driver.GetRunResult();
        after.Results.Single().Exception.ShouldBeNull();
        after.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        if (diagnosticId is not null)
        {
            after.Diagnostics.ShouldNotContain(item => item.Id == diagnosticId);
        }
        Output(after, "GapFixtures.Healthy").ShouldBe(healthy);
        Output(after, affectedPrefix).ShouldNotBeEmpty();
        AssertValid(repaired.AddSyntaxTrees(after.GeneratedTrees));
        CSharpCompilation restored = repaired.ReplaceSyntaxTree(repaired.SyntaxTrees.Last(), tree);
        driver = driver.RunGenerators(restored, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult reverted = driver.GetRunResult();
        reverted.Results.Single().Exception.ShouldBeNull();
        reverted.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal).ShouldBe(before.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal));
        restored.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal)
            .ShouldBe(damaged.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal));
        reverted.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString())
            .ShouldBe(before.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString()));
        return (repaired, after);
    }

    private static string[] Output(GeneratorDriverRunResult result, string prefix) => result.Results.Single().GeneratedSources
        .Where(item => item.HintName.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(item => item.HintName, StringComparer.Ordinal)
        .Select(item => item.HintName + "\n" + item.SourceText.ToString()).ToArray();

    private static void AssertLocation(DiagnosticInfo diagnostic, SyntaxNode node)
    {
        FileLinePositionSpan location = node.GetLocation().GetLineSpan();
        diagnostic.FilePath.ShouldBe(location.Path);
        diagnostic.Line.ShouldBe(location.StartLinePosition.Line);
        diagnostic.Column.ShouldBe(location.StartLinePosition.Character);
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private const string Prefix = "using System; using System.ComponentModel; using Hexalith.FrontComposer.Contracts.Attributes; namespace GapFixtures; ";
    private const string Members = "public string MessageId { get; set; } = \"\";";
    private const string HealthySource = Prefix + "[Command] public class Healthy { " + Members + " public string Name { get; set; } = \"\"; }";
}
