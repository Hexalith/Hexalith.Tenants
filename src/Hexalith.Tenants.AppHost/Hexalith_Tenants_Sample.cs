using Aspire.Hosting;

namespace Projects;

public class Hexalith_Tenants_Sample : IProjectMetadata {
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "samples",
        "Hexalith.Tenants.Sample",
        "Hexalith.Tenants.Sample.csproj");

    public bool SuppressBuild => true;
}
