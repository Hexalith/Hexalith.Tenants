using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

string prior = args[0];
string current = args[1];
List<object> checks = [];
foreach (string name in new[] { "ProjectionParserSurvivorRegressionTests.cs", "ErrorFollowthroughBehaviorTests.cs" })
{
    RequireSame(Parse(Path.Combine(prior, name)), Parse(Path.Combine(current, name)), name + " tokens");
}

SyntaxNode oldBoundary = Parse(Path.Combine(prior, "TemplateParserProviderBoundaryTests.cs"));
SyntaxNode newBoundary = Parse(Path.Combine(current, "TemplateParserProviderBoundaryTests.cs"));
RequireSame(Method(oldBoundary, "OtherAttributeProviderReturnsAnEmptyTemplateResult"),
    Method(newBoundary, "OtherAttributeProviderReturnsAnEmptyTemplateResult"), "public provider test tokens");
SyntaxNode helper = Parse(Path.Combine(current, "OtherAttributeGenerator.cs"));
RequireSame(Method(oldBoundary, "Initialize"), Method(helper, "Initialize"), "helper Initialize tokens");
RequireSame(oldBoundary.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single(),
    helper.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single(), "helper Results tokens");
ClassDeclarationSyntax helperClass = helper.DescendantNodes().OfType<ClassDeclarationSyntax>().Single();
Require(helperClass.Modifiers.Any(SyntaxKind.InternalKeyword), "helper is internal");
Require(helperClass.GetLeadingTrivia().ToFullString().Contains("<summary>", StringComparison.Ordinal), "helper class summary");
Require(helper.DescendantNodes().OfType<PropertyDeclarationSyntax>().Single().GetLeadingTrivia().ToFullString()
    .Contains("<summary>", StringComparison.Ordinal), "Results summary");

foreach (string file in Directory.GetFiles(current, "*.cs"))
{
    SyntaxNode root = Parse(file);
    Require(root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count() == 1, Path.GetFileName(file) + " has one type");
    foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                 .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)))
    {
        string docs = method.GetLeadingTrivia().ToFullString();
        foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
        {
            Require(docs.Contains("<param name=\"" + parameter.Identifier.ValueText + "\">", StringComparison.Ordinal),
                method.Identifier.ValueText + " documents " + parameter.Identifier.ValueText);
        }
    }
}
SyntaxNode projection = Parse(Path.Combine(current, "ProjectionParserSurvivorRegressionTests.cs"));
foreach (AttributeListSyntax attributes in projection.DescendantNodes().OfType<AttributeListSyntax>()
             .Where(attributes => attributes.Parent is MethodDeclarationSyntax))
{
    Require(attributes.GetLocation().GetLineSpan().StartLinePosition.Character == 4, "member attribute indentation");
}
Console.WriteLine(JsonSerializer.Serialize(checks, new JsonSerializerOptions { WriteIndented = true }));

SyntaxNode Parse(string path) => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
MethodDeclarationSyntax Method(SyntaxNode root, string name) => root.DescendantNodes().OfType<MethodDeclarationSyntax>()
    .Single(method => method.Identifier.ValueText == name);
void RequireSame(SyntaxNode before, SyntaxNode after, string label) => Require(
    before.DescendantTokens().Select(token => (token.RawKind, token.Text))
        .SequenceEqual(after.DescendantTokens().Select(token => (token.RawKind, token.Text))), label);
void Require(bool condition, string label)
{
    if (!condition)
    {
        throw new InvalidOperationException(label);
    }
    checks.Add(new { check = label, passed = condition });
}
