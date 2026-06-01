using Projects;

namespace Hexalith.Tenants.AppHost;

public class HexalithEventStoreAdminUI : IProjectMetadata {

    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore.Admin.UI",
        "Hexalith.EventStore.Admin.UI.csproj");

    public bool SuppressBuild => true;
}
