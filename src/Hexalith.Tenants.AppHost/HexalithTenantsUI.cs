using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenantsUI : IProjectMetadata {

    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants.UI",
        "Hexalith.Tenants.UI.csproj");

    public bool SuppressBuild => true;
}
