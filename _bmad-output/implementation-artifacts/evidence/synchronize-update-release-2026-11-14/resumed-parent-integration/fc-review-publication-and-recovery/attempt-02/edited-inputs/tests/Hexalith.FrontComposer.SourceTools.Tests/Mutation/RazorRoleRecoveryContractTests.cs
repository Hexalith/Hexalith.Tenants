using System.Globalization;

using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Mutation;

/// <summary>Protects real role, badge and field-format recovery across incremental generation.</summary>
[Trait("Category", "MutationErrorRegression")]
public sealed class RazorRoleRecoveryContractTests
{
    /// <summary>Explains missing timeline ordering and recovers to actual date-based generated ordering.</summary>
    [Fact]
    public void MissingTimelineDateRecoversWithoutChangingTheHealthyProjection()
    {
        string damagedSource = Source("public string Title { get; set; } = \"\";", "Timeline");
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, damagedSource]);
        AssertValid(damaged);
        GeneratorDriverRunResult before = Run(damaged);
        Diagnostic diagnostic = before.Diagnostics.Single(item => item.Id == "HFC1022");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Warning);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("DateTime/DateTimeOffset/DateOnly/TimeOnly");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("timeline ordering falls back to declaration order");
        AssertLocation(diagnostic, damaged);
        Affected(before).ShouldNotContain("state.Items.OrderByDescending(x => x.Timestamp)");
        Affected(before).ShouldContain("var orderedItems = state.Items.ToList();");
        Affected(before).ShouldContain("foreach (var item in orderedItems)");
        AssertValid(damaged.AddSyntaxTrees(before.GeneratedTrees));
        string repairedSource = damagedSource.Replace("public string Title", "public DateTime Timestamp { get; set; } public string Title", StringComparison.Ordinal);
        (CSharpCompilation repaired, GeneratorDriverRunResult after) = Recover(damaged, repairedSource);
        after.Diagnostics.ShouldNotContain(item => item.Id == "HFC1022");
        DomainModel model = CompilationHelper.ParseProjection(repaired, "RazorGap.P").Model.ShouldNotBeNull();
        RazorModelTransform.Transform(model).Columns.Single(item => item.PropertyName == "Timestamp").TypeCategory.ShouldBe(TypeCategory.DateTime);
        Affected(after).ShouldContain("state.Items.OrderByDescending(x => x.Timestamp)");
    }

    /// <summary>Explains exactly which enum members use plain text and recovers complete badge coverage.</summary>
    [Fact]
    public void PartialBadgesRecoverFromPreciseFallbackToCompleteMetadata()
    {
        const string values = "public enum State { [ProjectionBadge(BadgeSlot.Success)] Ready, Pending, Rejected }";
        string damagedSource = Source("public State Status { get; set; }", extra: values);
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, damagedSource]);
        AssertValid(damaged);
        GeneratorDriverRunResult before = Run(damaged);
        Diagnostic diagnostic = before.Diagnostics.Single(item => item.Id == "HFC1025");
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Info);
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Enum property 'Status' on projection P has 1 of 3 members annotated");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("unannotated members (Pending, Rejected) render as plain text");
        diagnostic.GetMessage(CultureInfo.InvariantCulture).ShouldContain("Annotate every member or none for visual consistency");
        AssertLocation(diagnostic, damaged);
        Affected(before).ShouldContain("case \"Pending\":");
        Affected(before).ShouldContain("case \"Rejected\":");
        foreach (string member in new[] { "Pending", "Rejected" })
        {
            string[] cases = EnumCases(before, member);
            cases.ShouldNotBeEmpty();
            foreach (string fallback in cases)
            {
                fallback.ShouldContain(".AddContent(");
                fallback.ShouldContain(", _label);");
                fallback.ShouldNotContain("FcStatusIcon");
            }
        }
        string repairedSource = damagedSource.Replace("Ready, Pending, Rejected", "Ready, [ProjectionBadge(BadgeSlot.Info)] Pending, [ProjectionBadge(BadgeSlot.Danger)] Rejected", StringComparison.Ordinal);
        (CSharpCompilation repaired, GeneratorDriverRunResult after) = Recover(damaged, repairedSource);
        after.Diagnostics.ShouldNotContain(item => item.Id == "HFC1025");
        ColumnModel status = RazorModelTransform.Transform(CompilationHelper.ParseProjection(repaired, "RazorGap.P").Model.ShouldNotBeNull()).Columns.Single(item => item.PropertyName == "Status");
        status.BadgeMappings.Select(item => item.EnumMemberName).OrderBy(item => item, StringComparer.Ordinal).ShouldBe(["Pending", "Ready", "Rejected"]);
        status.BadgeMappings.Select(item => (item.EnumMemberName, item.Slot)).OrderBy(item => item.EnumMemberName, StringComparer.Ordinal)
            .ShouldBe([("Pending", "Info"), ("Ready", "Success"), ("Rejected", "Danger")]);
        foreach ((string member, string slot) in new[] { ("Pending", "Info"), ("Ready", "Success"), ("Rejected", "Danger") })
        {
            string[] cases = EnumCases(after, member);
            cases.ShouldNotBeEmpty();
            foreach (string badge in cases)
            {
                badge.ShouldContain("FcStatusIcon");
                badge.ShouldContain("BadgeSlot." + slot);
            }
        }
        Affected(after).ShouldContain("BadgeSlot.Info");
        Affected(after).ShouldContain("BadgeSlot.Danger");
    }

    /// <summary>Recovers supported field types and carries their existing format into actual slot context code.</summary>
    /// <param name="type">The repaired supported property type.</param>
    /// <param name="category">The existing transform category.</param>
    /// <param name="format">The existing field-format contract.</param>
    [Theory]
    [InlineData("bool", TypeCategory.Boolean, "Yes/No")]
    [InlineData("TimeOnly", TypeCategory.DateTime, "t")]
    [InlineData("List<string>", TypeCategory.Collection, "Count")]
    public void RepairedFieldTypeRetainsItsObservableFormat(string type, TypeCategory category, string format)
    {
        string damagedSource = Source("public string Value { get; set; } = default!;", "DetailRecord");
        CSharpCompilation damaged = CompilationHelper.CreateCompilation([HealthySource, damagedSource]);
        AssertValid(damaged);
        DomainModel original = CompilationHelper.ParseProjection(damaged, "RazorGap.P").Model.ShouldNotBeNull();
        RazorModelTransform.Transform(original).Columns.Single(item => item.PropertyName == "Value").FormatHint.ShouldBeNull();
        (CSharpCompilation repaired, GeneratorDriverRunResult generated) = Recover(damaged, damagedSource.Replace("public string Value", "public " + type + " Value", StringComparison.Ordinal));
        ColumnModel column = RazorModelTransform.Transform(CompilationHelper.ParseProjection(repaired, "RazorGap.P").Model.ShouldNotBeNull()).Columns.Single(item => item.PropertyName == "Value");
        column.TypeCategory.ShouldBe(category);
        column.FormatHint.ShouldBe(format);
        Affected(generated).ShouldContain("format: \"" + format + "\"");
        if (type == "TimeOnly")
        {
            Affected(generated).ShouldContain(".ToString(\"t\", CultureInfo.CurrentCulture)");
        }
    }

    private static (CSharpCompilation Compilation, GeneratorDriverRunResult Generated) Recover(CSharpCompilation damaged, string source)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(damaged, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult before = driver.GetRunResult();
        string[] healthy = Healthy(before);
        healthy.ShouldNotBeEmpty();
        SyntaxTree tree = damaged.SyntaxTrees.Last();
        CSharpCompilation repaired = damaged.ReplaceSyntaxTree(tree, CSharpSyntaxTree.ParseText(source, (CSharpParseOptions)tree.Options, tree.FilePath, cancellationToken: TestContext.Current.CancellationToken));
        driver = driver.RunGenerators(repaired, TestContext.Current.CancellationToken);
        GeneratorDriverRunResult result = driver.GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        result.Diagnostics.Where(item => item.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        Healthy(result).ShouldBe(healthy);
        Affected(result).ShouldNotBeEmpty();
        AssertValid(repaired.AddSyntaxTrees(result.GeneratedTrees));
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

    private static GeneratorDriverRunResult Run(CSharpCompilation compilation)
    {
        GeneratorDriverRunResult result = CSharpGeneratorDriver.Create(new FrontComposerGenerator()).RunGenerators(compilation, TestContext.Current.CancellationToken).GetRunResult();
        result.Results.Single().Exception.ShouldBeNull();
        return result;
    }

    private static string[] EnumCases(GeneratorDriverRunResult result, string member) => CSharpSyntaxTree.ParseText(Affected(result), cancellationToken: TestContext.Current.CancellationToken)
        .GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<SwitchSectionSyntax>()
        .Where(section => section.Parent is SwitchStatementSyntax statement && statement.Expression.ToString() == "_memberName"
            && section.Labels.OfType<CaseSwitchLabelSyntax>().Any(label => label.Value is LiteralExpressionSyntax literal && literal.Token.ValueText == member))
        .Select(section => section.ToString()).ToArray();

    private static string Affected(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources.Single(item => item.HintName == "RazorGap.P.g.razor.cs").SourceText.ToString();

    private static string[] Healthy(GeneratorDriverRunResult result) => result.Results.Single().GeneratedSources.Where(item => item.HintName.StartsWith("RazorGap.Healthy", StringComparison.Ordinal))
        .OrderBy(item => item.HintName, StringComparer.Ordinal).Select(item => item.HintName + "\n" + item.SourceText.ToString()).ToArray();

    private static void AssertLocation(Diagnostic diagnostic, CSharpCompilation compilation)
    {
        FileLinePositionSpan expected = compilation.SyntaxTrees.Last().GetRoot(TestContext.Current.CancellationToken).DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(item => item.Identifier.ValueText == "P").GetLocation().GetLineSpan();
        FileLinePositionSpan actual = diagnostic.Location.GetLineSpan();
        actual.Path.ShouldBe(expected.Path);
        actual.StartLinePosition.ShouldBe(expected.StartLinePosition);
    }

    private static void AssertValid(CSharpCompilation compilation) => compilation.GetDiagnostics(TestContext.Current.CancellationToken)
        .Where(item => item.Severity == DiagnosticSeverity.Error).Select(item => item.ToString()).ShouldBeEmpty();

    private static string Source(string body, string? role = null, string extra = "") => Prefix + "[Projection" + (role is null ? "" : ", ProjectionRole(ProjectionRole." + role + ")") + "] public partial class P { public string Id { get; set; } = \"\"; " + body + " } " + extra;

    private const string Prefix = "using System; using System.Collections.Generic; using Hexalith.FrontComposer.Contracts.Attributes; namespace RazorGap; ";
    private const string HealthySource = Prefix + "[Projection] public partial class Healthy { public string Id { get; set; } = \"\"; public string Name { get; set; } = \"\"; }";
}
