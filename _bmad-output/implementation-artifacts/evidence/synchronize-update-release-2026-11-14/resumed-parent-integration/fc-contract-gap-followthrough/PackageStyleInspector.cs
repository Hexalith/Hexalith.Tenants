using System.Text.Json;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

List<object> results = [];
foreach (string file in args)
{
    SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
    if (root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Count() != 1)
    {
        throw new InvalidOperationException("Expected one type per file: " + file);
    }
    foreach (ClassDeclarationSyntax type in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
    {
        if (!type.GetLeadingTrivia().ToFullString().Contains("<summary>", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Missing class summary: " + file);
        }
    }
    int cases = 0;
    foreach (MethodDeclarationSyntax method in root.DescendantNodes().OfType<MethodDeclarationSyntax>()
                 .Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword)))
    {
        string docs = method.GetLeadingTrivia().ToFullString();
        if (!docs.Contains("<summary>", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Missing method summary: " + method.Identifier.ValueText);
        }
        foreach (ParameterSyntax parameter in method.ParameterList.Parameters)
        {
            if (!docs.Contains("<param name=\"" + parameter.Identifier.ValueText + "\">", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Missing parameter documentation: " + parameter.Identifier.ValueText);
            }
        }
        foreach (AttributeListSyntax attributes in method.AttributeLists)
        {
            if (attributes.GetLocation().GetLineSpan().StartLinePosition.Character != 4)
            {
                throw new InvalidOperationException("Attribute indentation: " + method.Identifier.ValueText);
            }
        }
        cases += method.AttributeLists.SelectMany(list => list.Attributes).Count(attribute => attribute.Name.ToString() is "Fact" or "InlineData");
    }
    results.Add(new { file = Path.GetFileName(file), oneType = true, publicDocumentation = true, memberAttributeIndentation = 4, cases });
}
Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
