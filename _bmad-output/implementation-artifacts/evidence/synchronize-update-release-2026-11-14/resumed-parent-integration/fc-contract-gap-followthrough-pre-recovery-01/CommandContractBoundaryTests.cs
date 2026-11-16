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
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Command] " + declaration + " { public C() { } " + Members + " }");
        AssertValid(compilation);
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, metadataName);
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1004");
        diagnostic.Severity.ShouldBe("Warning");
        diagnostic.Message.ShouldContain("[Command] attribute on 'C'");
        diagnostic.Message.ShouldContain(reason);
        AssertLocation(diagnostic, compilation.SyntaxTrees.Single().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>().Single());
    }

    /// <summary>Rejects a missing policy argument without throwing during incomplete editing.</summary>
    [Fact]
    public void MissingPolicyArgumentRetainsTheRefusalReasonAndAttributeLocation()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Command, RequiresPolicy] public class C { " + Members + " }");
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        CommandParseResult result = Should.NotThrow(() => CompilationHelper.ParseCommand(compilation, "GapFixtures.C"));
        result.Model.ShouldBeNull();
        DiagnosticInfo diagnostic = result.Diagnostics.Single(item => item.Id == "HFC1056");
        diagnostic.Severity.ShouldBe("Error");
        diagnostic.Message.ShouldContain("Expected a non-empty policy name");
        diagnostic.Message.ShouldContain("Got: <null>");
        AssertLocation(diagnostic, compilation.SyntaxTrees.Single().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<AttributeSyntax>().Single(item => item.Name.ToString() == "RequiresPolicy"));
    }

    /// <summary>Preserves absent icon metadata when its required source argument is missing.</summary>
    [Fact]
    public void MissingIconArgumentKeepsTheCommandAvailableWithoutAnIcon()
    {
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Command, Icon] public class C { " + Members + " }");
        compilation.GetDiagnostics(TestContext.Current.CancellationToken).ShouldContain(item => item.Severity == DiagnosticSeverity.Error);
        CommandParseResult result = Should.NotThrow(() => CompilationHelper.ParseCommand(compilation, "GapFixtures.C"));
        result.Model.ShouldNotBeNull().IconName.ShouldBeNull();
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1056");
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
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(Prefix + "[Command] public class C { " + Members + " [DefaultValue(typeof(" + sourceType + "), \"" + text + "\")] public int Value { get; set; } }");
        AssertValid(compilation);
        IPropertySymbol property = compilation.GetTypeByMetadataName("GapFixtures.C")!.GetMembers("Value").OfType<IPropertySymbol>().Single();
        ITypeSymbol declaredDefault = (ITypeSymbol)property.GetAttributes().Single().ConstructorArguments[0].Value!;
        declaredDefault.SpecialType.ShouldBe(expectedSource);
        compilation.ClassifyConversion(declaredDefault, property.Type).IsImplicit.ShouldBeTrue();
        CommandParseResult result = CompilationHelper.ParseCommand(compilation, "GapFixtures.C");
        result.Diagnostics.ShouldNotContain(item => item.Id == "HFC1012");
        CommandModel model = result.Model.ShouldNotBeNull();
        model.Properties.Single(item => item.Name == "Value").SourceTypeName.ShouldBe("global::System.Int32");
        CommandFormTransform.Transform(model).Fields.Single(item => item.PropertyName == "Value").TypeCategory.ShouldBe(FormFieldTypeCategory.NumberInput);
    }

    /// <summary>Retains the existing global-namespace target fallback in parsed and form metadata.</summary>
    [Fact]
    public void GlobalProjectionTargetRetainsItsExistingScopeFallback()
    {
        const string source = "using Hexalith.FrontComposer.Contracts.Attributes; [Projection] public sealed class P { } [Command, CommandTarget(typeof(P), CommandTargetResolutionMode.Provider, CommandTargetChangeKind.Update)] public class C { public string MessageId { get; set; } = \"\"; }";
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        AssertValid(compilation);
        CommandModel model = CompilationHelper.ParseCommand(compilation, "C").Model.ShouldNotBeNull();
        model.CommandTarget.ShouldNotBeNull().ProjectionViewKey.ShouldBe(":P");
        CommandFormTransform.Transform(model).CommandTarget.ShouldNotBeNull().ProjectionViewKey.ShouldBe(":P");
    }

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
}
