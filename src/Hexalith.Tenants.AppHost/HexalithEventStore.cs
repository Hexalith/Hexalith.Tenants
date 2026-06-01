using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithEventStore : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore",
        "Hexalith.EventStore.csproj");

    public bool SuppressBuild => true;
}
