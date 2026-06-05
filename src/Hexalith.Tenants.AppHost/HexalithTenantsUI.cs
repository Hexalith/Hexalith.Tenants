using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithTenantsUI : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants.UI",
        "Hexalith.Tenants.UI.csproj");

    public bool SuppressBuild => true;
}
