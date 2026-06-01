using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenantsSample : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "samples",
        "Hexalith.Tenants.Sample",
        "Hexalith.Tenants.Sample.csproj");

    public bool SuppressBuild => true;
}
