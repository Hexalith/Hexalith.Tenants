using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenants : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants",
        "Hexalith.Tenants.csproj");

    public bool SuppressBuild => true;
}
