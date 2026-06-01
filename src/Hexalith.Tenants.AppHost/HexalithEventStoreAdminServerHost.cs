using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithEventStoreAdminServerHost : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore.Admin.Server.Host",
        "Hexalith.EventStore.Admin.Server.Host.csproj");

    public bool SuppressBuild => true;
}
