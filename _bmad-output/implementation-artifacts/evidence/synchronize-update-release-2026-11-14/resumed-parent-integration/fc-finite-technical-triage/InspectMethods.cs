using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using JsonDocument input = JsonDocument.Parse(File.ReadAllText(args[0]));
var rows = new List<object>();
foreach ((string kind, string folder) in new[] {
    ("original", input.RootElement.GetProperty("originalOverlay").GetString()!),
    ("current", Path.Combine(input.RootElement.GetProperty("owner").GetString()!, "tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation")),
    ("hash-private", input.RootElement.GetProperty("hashPrivate").GetString()!) })
{
    IEnumerable<string> paths = kind == "original" ? Directory.GetFiles(folder, "*.cs", SearchOption.AllDirectories)
        : kind == "current" ? Directory.GetFiles(folder, "*.cs")
        : Directory.GetFiles(folder, "*.cs").Where(path => Path.GetFileName(path) is "ModelHashFieldParticipationTests.cs" or "CountingModelComparer.cs");
    foreach (string path in paths)
    {
        SyntaxNode root = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot();
        var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>().Select(method => new {
            key = method.Ancestors().OfType<TypeDeclarationSyntax>().First().Identifier.ValueText + "." + method.Identifier.ValueText,
            tokens = Tokens(method),
            assertions = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(call => call.Expression.ToString().Contains("Should", StringComparison.Ordinal))
                .Select(call => new { expression = call.ToString(), tokens = Tokens(call) }).ToArray()
        }).ToArray();
        rows.Add(new { kind, path, sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), methods });
    }
}
File.WriteAllText(input.RootElement.GetProperty("output").GetString()!, JsonSerializer.Serialize(rows));
static string[] Tokens(SyntaxNode node) => node.DescendantTokens().Select(token => token.RawKind + ":" + token.Text).ToArray();
