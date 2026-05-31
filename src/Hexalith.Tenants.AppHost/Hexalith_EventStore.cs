using Aspire.Hosting;

namespace Projects;

public class Hexalith_EventStore : IProjectMetadata {
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore",
        "Hexalith.EventStore.csproj");

    public bool SuppressBuild => true;
}
