using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

string root = args[0];
string reportPath = args[1];
string inventoryPath = args[2];
using var reportDocument=JsonDocument.Parse(File.ReadAllText(reportPath));
var reportSources=reportDocument.RootElement.GetProperty("files").EnumerateObject().ToDictionary(file=>file.Name[(file.Name.IndexOf("/src/",StringComparison.Ordinal)+1)..],file=>file.Value.GetProperty("source").GetString()!);
var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
var files = Directory.GetFiles(Path.Combine(root,"src/Hexalith.FrontComposer.SourceTools"),"*.cs",SearchOption.AllDirectories)
    .Concat(Directory.GetFiles(Path.Combine(root,"src/Hexalith.FrontComposer.Contracts"),"*.cs",SearchOption.AllDirectories)).ToArray();
var trees = files.Select(path => CSharpSyntaxTree.ParseText(reportSources.TryGetValue(Path.GetRelativePath(root,path).Replace('\\','/'),out var exactSource)?exactSource:File.ReadAllText(path), options, path)).ToArray();
var usings = CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;",options);
var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator).Distinct().Select(path => MetadataReference.CreateFromFile(path));
var compilation = CSharpCompilation.Create("SourceAnalysis",trees.Append(usings),references,new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
var methods = new List<object>();
var calls = new List<object>();
string Name(SyntaxNode node) => string.Join(".",node.Ancestors().OfType<BaseTypeDeclarationSyntax>().Reverse().Select(type=>type.Identifier.ValueText)) + "." + (node switch { MethodDeclarationSyntax method=>method.Identifier.ValueText,ConstructorDeclarationSyntax constructor=>".ctor",PropertyDeclarationSyntax property=>property.Identifier.ValueText,FieldDeclarationSyntax field=>string.Join(",",field.Declaration.Variables.Select(variable=>variable.Identifier.ValueText)),_=>node.Kind().ToString() });
SyntaxNode? Member(SyntaxNode node) => node.AncestorsAndSelf().FirstOrDefault(parent=>parent is BaseMethodDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax);
var nodes = new Dictionary<string,List<SyntaxNode>>();
foreach(var tree in trees) {
 var relative=Path.GetRelativePath(root,tree.FilePath).Replace('\\','/');
 var model=compilation.GetSemanticModel(tree);
 var members=tree.GetRoot().DescendantNodes().Where(node=>node is MethodDeclarationSyntax or ConstructorDeclarationSyntax or PropertyDeclarationSyntax or FieldDeclarationSyntax).ToList();
 nodes[relative]=members;
 foreach(var member in members) {
  var declared=model.GetDeclaredSymbol(member);
  methods.Add(new {path=relative,name=Name(member),symbol=declared?.ToDisplayString(),kind=member.Kind().ToString(),startLine=member.GetLocation().GetLineSpan().StartLinePosition.Line+1,endLine=member.GetLocation().GetLineSpan().EndLinePosition.Line+1,text=member.ToString()});
 }
 foreach(var call in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()) {
  var info=model.GetSymbolInfo(call);var symbol=info.Symbol as IMethodSymbol;
  calls.Add(new {path=relative,line=call.GetLocation().GetLineSpan().StartLinePosition.Line+1,caller=Member(call) is {} parent ? Name(parent) : "<initializer>",expression=call.Expression.ToString(),target=symbol?.OriginalDefinition.ToDisplayString(),targetType=symbol?.ContainingType.ToDisplayString(),targetMethod=symbol?.Name,resolved=symbol is not null});
 }
}
using var inventory=JsonDocument.Parse(File.ReadAllText(inventoryPath));
using var report=JsonDocument.Parse(File.ReadAllText(reportPath));
var reportFiles=report.RootElement.GetProperty("files");
var mapped=new List<object>();
foreach(var mutant in inventory.RootElement.GetProperty("mutants").EnumerateArray()) {
 string path=mutant.GetProperty("path").GetString()!;
 var sourceFile=reportFiles.EnumerateObject().Single(file=>file.Name.EndsWith('/'+path,StringComparison.Ordinal));
 string source=sourceFile.Value.GetProperty("source").GetString()!;
 var text=SourceText.From(source);
 var start=mutant.GetProperty("location").GetProperty("start");var end=mutant.GetProperty("location").GetProperty("end");
 int from=text.Lines[start.GetProperty("line").GetInt32()-1].Start+start.GetProperty("column").GetInt32()-1;
 int to=text.Lines[end.GetProperty("line").GetInt32()-1].Start+end.GetProperty("column").GetInt32()-1;
 var candidates=nodes[path].Where(node=>node.FullSpan.Start<=from && node.FullSpan.End>=to).OrderBy(node=>node.FullSpan.Length).ToArray();
 var member=candidates.FirstOrDefault();
 mapped.Add(new {id=mutant.GetProperty("id").GetString(),path,member=member is null?"<unmapped>":Name(member),memberStartLine=member?.GetLocation().GetLineSpan().StartLinePosition.Line+1,memberEndLine=member?.GetLocation().GetLineSpan().EndLinePosition.Line+1,original=text.ToString(TextSpan.FromBounds(from,to)),replacement=mutant.GetProperty("replacement").GetString(),status=mutant.GetProperty("status").GetString(),mutator=mutant.GetProperty("mutatorName").GetString()});
}
Console.WriteLine(JsonSerializer.Serialize(new {roslynVersion=typeof(CSharpCompilation).Assembly.GetName().Version?.ToString(),semanticErrors=compilation.GetDiagnostics().Where(diagnostic=>diagnostic.Severity==DiagnosticSeverity.Error).Select(diagnostic=>diagnostic.ToString()).Distinct().ToArray(),members=methods,calls,mutants=mapped},new JsonSerializerOptions {WriteIndented=true}));
