using Aspire.Hosting;

namespace Projects;

public class Hexalith_EventStore_Admin_UI : IProjectMetadata {
    public string ProjectPath => ProjectMetadataPaths.GetProjectPath(
        "Hexalith.EventStore",
        "src",
        "Hexalith.EventStore.Admin.UI",
        "Hexalith.EventStore.Admin.UI.csproj");

    public bool SuppressBuild => true;
}
