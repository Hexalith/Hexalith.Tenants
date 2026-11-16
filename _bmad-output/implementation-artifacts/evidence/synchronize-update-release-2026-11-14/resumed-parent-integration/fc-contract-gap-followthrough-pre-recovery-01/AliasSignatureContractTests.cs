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
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source, additionalReferences: [reference]);
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
            new[] { "Alpha", "Zulu" }.ShouldContain(required.Single());
            qualifier = required.Single() + "::";
        }
        property.SourceTypeName.ShouldBe(qualifier + "AliasLibrary.@class[,][]");
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        string renderer = result.Results.Single().GeneratedSources.Single(item => item.HintName.EndsWith("C.CommandRenderer.g.razor.cs", StringComparison.Ordinal)).SourceText.ToString();
        renderer.ShouldContain("TryConvertPropertyValue<" + property.SourceTypeName + ">");
        foreach (string alias in required)
        {
            renderer.ShouldContain("extern alias " + alias + ";");
        }
        AssertValid(compilation.AddSyntaxTrees(result.GeneratedTrees));
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();
}
