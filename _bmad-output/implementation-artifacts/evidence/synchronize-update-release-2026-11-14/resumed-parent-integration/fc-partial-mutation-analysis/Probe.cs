using System.Reflection;
using System.Text.Json;
using Hexalith.FrontComposer.SourceTools.Parsing;
using Hexalith.FrontComposer.Contracts.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
    .Append(typeof(ProjectionAttribute).Assembly.Location).Distinct().Select(path => MetadataReference.CreateFromFile(path)).ToArray();
CSharpCompilation Compilation(params (string Code, string File)[] source) => CSharpCompilation.Create("ProbeInput",
    source.Select(item => CSharpSyntaxTree.ParseText(item.Code, path: item.File)), references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithNullableContextOptions(NullableContextOptions.Enable));
void Observe(string name, CSharpCompilation compilation, string type = "P") {
    var symbol = compilation.GetTypeByMetadataName(type)!;
    var attributes = symbol.GetAttributes().Concat(symbol.GetMembers().SelectMany(member => member.GetAttributes()))
        .Select(attribute => new { type = attribute.AttributeClass?.ToDisplayString(), arguments = attribute.ConstructorArguments.Length,
            values = attribute.ConstructorArguments.Select(arg => new { kind = arg.Kind.ToString(), value = arg.IsNull ? null : arg.Value?.ToString() }) });
    try {
        var result = AttributeParser.Parse(symbol, symbol.DeclaringSyntaxReferences.First().GetSyntax(), compilation, default);
        Console.WriteLine(JsonSerializer.Serialize(new { name, errors = compilation.GetDiagnostics().Count(item => item.Severity == DiagnosticSeverity.Error), attributes,
            diagnostics = result.Diagnostics.Select(item => new { item.Id, item.Message, item.Severity, item.FilePath, item.Line, item.Column }),
            properties = result.Model?.Properties.Select(item => new { item.Name, item.SupportsStaticAssignment }) }));
    } catch (Exception exception) { Console.WriteLine(JsonSerializer.Serialize(new { name, attributes, exception = exception.GetType().Name })); }
}
const string prefix = "using System; using System.ComponentModel; using Hexalith.FrontComposer.Contracts.Attributes; ";
var enumSource = prefix + "\npublic enum Status {\n [ProjectionBadge((BadgeSlot)999)]\n Bad\n}";
var badge = Compilation((prefix + "\n[Projection] public partial class P { public Status State { get; set; } }", "Projection.cs"), (enumSource,"Enums.cs"));
Observe("invalid-badge-other-file", badge);
Console.WriteLine(JsonSerializer.Serialize(new { name="source-enum-locations", fields=badge.GetTypeByMetadataName("Status")!.GetMembers().OfType<IFieldSymbol>().Where(item=>item.HasConstantValue).Select(item=>new {item.Name, count=item.Locations.Length, kinds=item.Locations.Select(location=>location.Kind.ToString())}) }));
var metadata = Compilation((enumSource,"ExternalEnum.cs")).WithAssemblyName("ExternalEnum");
using var stream = new MemoryStream();
if (!metadata.Emit(stream).Success) throw new Exception("metadata emit failed");
var consumer = Compilation((prefix+"[Projection] public partial class P { public Status State { get; set; } }", "Projection.cs")).AddReferences(MetadataReference.CreateFromImage(stream.ToArray()));
Observe("invalid-badge-metadata", consumer);
Console.WriteLine(JsonSerializer.Serialize(new { name="metadata-enum-locations", fields=consumer.GetTypeByMetadataName("Status")!.GetMembers().OfType<IFieldSymbol>().Where(item=>item.HasConstantValue).Select(item=>new {item.Name, count=item.Locations.Length, kinds=item.Locations.Select(location=>location.Kind.ToString())}) }));
Observe("first-enum-status", Compilation((prefix+"[Projection,ProjectionRole(ProjectionRole.ActionQueue,WhenState=\"Pending\")] public partial class P { public First A {get;set;} public Second Z {get;set;} } public enum First { Pending } public enum Second { Other }", "Projection.cs")));
Observe("public-partial-outer", Compilation((prefix+"public partial class Outer { [Projection] public partial class P { public int Value {get;set;} } }", "Projection.cs")), "Outer+P");
Observe("parameterless-description", Compilation((prefix+"[Projection] public partial class P { [Description] public int Value {get;set;} }", "Projection.cs")));
Observe("getter-only-property", Compilation((prefix+"[Projection] public partial class P { public int Value => 7; }", "Projection.cs")));
Observe("relative-time-enum", Compilation((prefix+"[Projection] public partial class P { [RelativeTime] public E Value {get;set;} } public enum E { A }", "Projection.cs")));
foreach(var annotation in new[]{"BoundedContext()", "ProjectionEmptyStateCta()"})
    Observe("invalid-type-"+annotation, Compilation((prefix+"[Projection,"+annotation+"] public partial class P { public int Value {get;set;} }", "Projection.cs")));
foreach(var annotation in new[]{"RelativeTime(null)", "RelativeTime(Unknown)", "RelativeTime(1/0)", "ColumnPriority()", "ProjectionFieldGroup()"})
    Observe("invalid-property-"+annotation, Compilation((prefix+"[Projection] public partial class P { ["+annotation+"] public int Value {get;set;} }", "Projection.cs")));
