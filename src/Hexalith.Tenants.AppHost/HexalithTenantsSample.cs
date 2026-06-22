using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenantsSample : IProjectMetadata {

    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "samples",
        "Hexalith.Tenants.Sample",
        "Hexalith.Tenants.Sample.csproj");

    public bool SuppressBuild => true;
}
