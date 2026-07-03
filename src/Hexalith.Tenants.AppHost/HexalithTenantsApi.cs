using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenantsApi : IProjectMetadata {

    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants.Api",
        "Hexalith.Tenants.Api.csproj");

    public bool SuppressBuild => true;
}
