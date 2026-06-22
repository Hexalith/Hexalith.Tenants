using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenants : IProjectMetadata {

    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants",
        "Hexalith.Tenants.csproj");

    public bool SuppressBuild => true;
}
