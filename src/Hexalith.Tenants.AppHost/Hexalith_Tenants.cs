using Aspire.Hosting;

namespace Projects;

public class Hexalith_Tenants : IProjectMetadata {
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "src",
        "Hexalith.Tenants",
        "Hexalith.Tenants.csproj");

    public bool SuppressBuild => true;
}
