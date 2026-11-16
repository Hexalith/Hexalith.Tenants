using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects actual alias provenance, keyword syntax and compiled generated assignment signatures.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class AliasSignatureContractTests
{
    /// <summary>Retains a usable reference qualification without prescribing one of several valid alias choices.</summary>
    /// <param name="globallyAvailable">Whether the assembly is available through global as well as named aliases.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GeneratedAssignmentSignatureCompilesWithItsActualAliases(bool globallyAvailable)
    {
        CSharpCompilation library = CompilationHelper.CreateCompilation("namespace AliasLibrary; public sealed class @class { }", assemblyName: "GapAliasLibrary");
        using MemoryStream stream = new();
        library.Emit(stream, cancellationToken: TestContext.Current.CancellationToken).Success.ShouldBeTrue();
        ImmutableArray<string> aliases = globallyAvailable ? ["Zulu", "global", "Alpha"] : ["Zulu", "Alpha"];
        MetadataReference reference = MetadataReference.CreateFromImage(stream.ToArray(), properties: MetadataReferenceProperties.Assembly.WithAliases(aliases));
        const string source = """
            extern alias Zulu;
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace AliasFixtures;
            [Command] public sealed class C
            {
                public string MessageId { get; set; } = "";
                [DerivedFrom(DerivedFromSource.Context)]
                public Zulu::AliasLibrary.@class[,][] Value { get; set; } = null!;
            }
            """;
        string damagedSource = source.Replace("extern alias Zulu;", "extern alias Missing;", StringComparison.Ordinal).Replace("Zulu::AliasLibrary", "Missing::AliasLibrary", StringComparison.Ordinal);
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, damagedSource], additionalReferences: [reference]);
        damaged.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        AssertEditingFallback(damaged);
        (CSharpCompilation compilation, GeneratorDriverRunResult result) = Recover(damaged, source);
        AssertValid(compilation);
        PropertyModel property = CompilationHelper.ParseCommand(compilation, "AliasFixtures.C").Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value");
        string[] required = property.RequiredExternAliases.ToArray();
        string qualifier;
        if (required.Length == 0)
        {
            globallyAvailable.ShouldBeTrue();
            qualifier = "global::";
        }
        else
        {
            required.Length.ShouldBe(1);
            (required.Single() is "Alpha" or "Zulu").ShouldBeTrue();
            qualifier = required.Single() + "::";
        }
        property.SourceTypeName.ShouldBe(qualifier + "AliasLibrary.@class[,][]");
        result.Results.Single().Exception.ShouldBeNull();
        string renderer = result.Results.Single().GeneratedSources.Single(item => item.HintName.EndsWith("C.CommandRenderer.g.razor.cs", StringComparison.Ordinal)).SourceText.ToString();
        renderer.ShouldContain("TryConvertPropertyValue<" + property.SourceTypeName + ">");
        foreach (string alias in required)
        {
            renderer.ShouldContain("extern alias " + alias + ";");
        }
        AssertValid(compilation.AddSyntaxTrees(result.GeneratedTrees));
    }

    /// <summary>Recovers unresolved property syntax into nullable-array and generic signatures that round-trip through Roslyn.</summary>
    /// <param name="signature">The supported repaired property type syntax.</param>
    [Theory]
    [InlineData("string?[]?[]")]
    [InlineData("System.Collections.Generic.Dictionary<string, int>")]
    public void RepairedSignatureRetainsItsActualTypeAndGeneratedAssignment(string signature)
    {
        const string damagedSource = "using Hexalith.FrontComposer.Contracts.Attributes; namespace AliasFixtures; [Command] public class C { public string MessageId { get; set; } = \"\"; [DerivedFrom(DerivedFromSource.Context)] public MissingType Value { get; set; } = null!; }";
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, damagedSource]);
        damaged.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        AssertEditingFallback(damaged);
        (CSharpCompilation repaired, GeneratorDriverRunResult generated) = Recover(damaged, damagedSource.Replace("MissingType", signature, StringComparison.Ordinal));
        PropertyModel property = CompilationHelper.ParseCommand(repaired, "AliasFixtures.C").Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value");
        property.SupportsStaticAssignment.ShouldBeTrue();
        string validationSource = "namespace AliasFixtures; public class SignatureCheck { public " + property.SourceTypeName + " Value { get; set; } = null!; }";
        CSharpCompilation validation = repaired.AddSyntaxTrees(CSharpSyntaxTree.ParseText(validationSource, cancellationToken: TestContext.Current.CancellationToken));
        AssertValid(validation);
        ITypeSymbol expected = validation.GetTypeByMetadataName("AliasFixtures.C")!.GetMembers("Value").OfType<IPropertySymbol>().Single().Type;
        ITypeSymbol actual = validation.GetTypeByMetadataName("AliasFixtures.SignatureCheck")!.GetMembers("Value").OfType<IPropertySymbol>().Single().Type;
        SymbolEqualityComparer.IncludeNullability.Equals(expected, actual).ShouldBeTrue();
        string renderer = Renderer(generated);
        renderer.ShouldContain("TryConvertPropertyValue<" + property.SourceTypeName + ">");
        renderer.ShouldContain("_prefilledModel.@Value =");
        AssertValid(repaired.AddSyntaxTrees(generated.GeneratedTrees));
    }

    private static void AssertEditingFallback(CSharpCompilation compilation)
    {
        PropertyModel property = CompilationHelper.ParseCommand(compilation, "AliasFixtures.C").Model.ShouldNotBeNull().Properties.Single(item => item.Name == "Value");
        property.SourceTypeName.ShouldNotBeNullOrWhiteSpace();
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        HealthyOutput(result).ShouldNotBeEmpty();
    }

    private static (CSharpCompilation Compilation, GeneratorDriverRunResult Generated) Recover(CSharpCompilation damaged, string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(damaged, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult before = driver.GetRunResult();
        string[] healthy = HealthyOutput(before);
        healthy.ShouldNotBeEmpty();
        SyntaxTree tree = damaged.SyntaxTrees.Last();
        CSharpCompilation repaired = damaged.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        HealthyOutput(result).ShouldBe(healthy);
        Renderer(result).ShouldContain("_prefilledModel.@Value =");
        CSharpCompilation restored = repaired.ReplaceSyntaxTree(repaired.SyntaxTrees.Last(), tree);
        driver = driver.RunGenerators(restored, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult reverted = driver.GetRunResult();
        reverted.Results.Single().Exception.ShouldBeNull();
        reverted.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal).ShouldBe(before.Diagnostics.Select(item => item.ToString()).Order(StringComparer.Ordinal));
        restored.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal)
            .ShouldBe(damaged.GetDiagnostics(TestContext.Current.CancellationToken).Select(item => item.ToString()).Order(StringComparer.Ordinal));
        reverted.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString())
            .ShouldBe(before.Results.Single().GeneratedSources.OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString()));
        return (repaired, result);
    }

    private static string Renderer(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources.Single(item => item.HintName.EndsWith("C.CommandRenderer.g.razor.cs", StringComparison.Ordinal)).SourceText.ToString();

    private static string[] HealthyOutput(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources
        .Where(item => item.HintName.StartsWith("AliasFixtures.Healthy", StringComparison.Ordinal)).OrderBy(item => item.HintName, StringComparer.Ordinal)
        .Select(item => item.HintName + "\n" + item.SourceText.ToString()).ToArray();

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private const string HealthySource = "using Hexalith.FrontComposer.Contracts.Attributes; namespace AliasFixtures; [Command] public class Healthy { public string MessageId { get; set; } = \"\"; public string Name { get; set; } = \"\"; }";
}
