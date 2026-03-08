using CommunityToolkit.Aspire.Hosting.Dapr;

using Hexalith.Tenants.Aspire;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// Resolve DAPR access control configuration path.
// Both runtime (bin) and source directory are checked for compatibility.
string accessControlConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "DaprComponents", "accesscontrol.yaml");
if (!File.Exists(accessControlConfigPath))
{
    accessControlConfigPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "DaprComponents", "accesscontrol.yaml"));
}

if (!File.Exists(accessControlConfigPath))
{
    throw new FileNotFoundException(
        "DAPR access control configuration not found. "
        + "Ensure accesscontrol.yaml exists in the DaprComponents directory.",
        accessControlConfigPath);
}

// Add CommandApi project and wire DAPR topology via Aspire extension.
IResourceBuilder<ProjectResource> commandApi = builder.AddProject<Projects.Hexalith_Tenants_CommandApi>("commandapi");
HexalithTenantsResources tenantsResources = builder.AddHexalithTenants(commandApi, accessControlConfigPath);

builder.Build().Run();
