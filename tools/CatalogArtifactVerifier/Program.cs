using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

if (args.Length != 2) throw new ArgumentException("Expected the actual pack-target DLL and package ID.");
string directory = Path.GetDirectoryName(Path.GetFullPath(args[0]))!;
var assemblies = new Dictionary<string, string>(StringComparer.Ordinal) { [args[1]] = Path.GetFullPath(args[0]) };
if (args[1] is "Hexalith.Tenants.Server" or "Hexalith.Tenants.Client" or "Hexalith.Tenants.Testing") assemblies["Hexalith.Tenants.Contracts"] = Path.Combine(directory, "Hexalith.Tenants.Contracts.dll");
if (args[1] == "Hexalith.Tenants.Testing") assemblies["Hexalith.Tenants.Server"] = Path.Combine(directory, "Hexalith.Tenants.Server.dll");
try
{
    foreach ((string id, string path) in assemblies)
    {
        using var file = File.OpenRead(path);
        using var pe = new PEReader(file);
        MetadataReader reader = pe.GetMetadataReader();
        if (reader.GetString(reader.GetAssemblyDefinition().Name) != id) throw new InvalidDataException("Pack-target assembly identity differs from its package.");
        if (id == "Hexalith.Tenants.Contracts")
        {
            RequireSource(reader, "Hexalith.EventStore.Contracts.Routing", "RouteCatalogDeclarationSourceAttribute", "Hexalith.Tenants.Contracts.Routing.TenantCatalogDeclarations");
            bool operations = reader.TypeDefinitions.Select(reader.GetTypeDefinition)
                .Where(type => reader.GetString(type.Namespace) == "Hexalith.Tenants.Contracts.Routing" && reader.GetString(type.Name) == "TenantCatalogDeclarations")
                .SelectMany(type => type.GetProperties()).Select(reader.GetPropertyDefinition)
                .Any(property => reader.GetString(property.Name) == "Operations" && property.GetAccessors().Getter is { IsNil: false } getter
                    && (reader.GetMethodDefinition(getter).Attributes & (MethodAttributes.Public | MethodAttributes.Static)) == (MethodAttributes.Public | MethodAttributes.Static));
            if (!operations) throw new InvalidDataException("The actual Contracts DLL lacks public static Operations.");
        }
        if (id == "Hexalith.Tenants.Server") RequireSource(reader, "Hexalith.EventStore.DomainService", "RouteCatalogAdapterSourceAttribute", "Hexalith.Tenants.Server.Routing.TenantIntentAdapters");
    }
    Console.WriteLine("Actual pack-target catalog enrollment verified: " + args[1]);
}
catch (Exception exception) when (exception is IOException or BadImageFormatException or InvalidDataException)
{
    Console.Error.WriteLine("Catalog artifact publication refused: " + exception.Message + " Rebuild the qualified catalog SDK outputs before packing.");
    return 1;
}
return 0;

static void RequireSource(MetadataReader reader, string expectedNamespace, string expectedAttribute, string expectedSource)
{
    var sources = new List<string>();
    foreach (CustomAttributeHandle handle in reader.GetAssemblyDefinition().GetCustomAttributes())
    {
        CustomAttribute attribute = reader.GetCustomAttribute(handle);
        if (attribute.Constructor.Kind != HandleKind.MemberReference) continue;
        EntityHandle parent = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor).Parent;
        if (parent.Kind != HandleKind.TypeReference) continue;
        TypeReference type = reader.GetTypeReference((TypeReferenceHandle)parent);
        if (reader.GetString(type.Namespace) != expectedNamespace || reader.GetString(type.Name) != expectedAttribute) continue;
        BlobReader value = reader.GetBlobReader(attribute.Value);
        if (value.ReadUInt16() != 1) throw new InvalidDataException("Invalid catalog source attribute encoding.");
        sources.Add(value.ReadSerializedString()!.Split(',')[0]);
    }
    if (sources.Count != 1 || sources[0] != expectedSource) throw new InvalidDataException("The actual DLL lacks its exact catalog source enrollment: " + expectedSource);
}
