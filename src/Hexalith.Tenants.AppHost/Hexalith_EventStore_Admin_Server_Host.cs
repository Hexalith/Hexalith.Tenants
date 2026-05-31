using Aspire.Hosting;

namespace Projects;

public class Hexalith_EventStore_Admin_Server_Host : IProjectMetadata {
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore.Admin.Server.Host",
        "Hexalith.EventStore.Admin.Server.Host.csproj");

    public bool SuppressBuild => true;
}
