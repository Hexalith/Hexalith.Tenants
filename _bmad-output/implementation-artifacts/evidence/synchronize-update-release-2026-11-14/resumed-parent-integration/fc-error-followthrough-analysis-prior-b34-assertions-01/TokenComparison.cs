using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.CodeAnalysis.CSharp;

var results = new List<object>();
for (int i = 0; i < args.Length; i += 2)
{
    string[] before = Tokens(args[i]);
    string[] after = Tokens(args[i + 1]);
    bool equal = before.SequenceEqual(after, StringComparer.Ordinal);
    results.Add(new { original = args[i], proposal = args[i + 1], tokenCount = before.Length, equal,
        tokenSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", before)))).ToLowerInvariant() });
    if (!equal)
    {
        throw new InvalidOperationException("Documentation/style adaptation changed C# tokens.");
    }
}

Console.WriteLine(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));

static string[] Tokens(string path) => CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot()
    .DescendantTokens().Select(token => token.RawKind + ":" + token.Text).ToArray();
