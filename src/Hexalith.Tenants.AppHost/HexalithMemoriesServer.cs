using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.AppHost;

/// <summary>
/// Cross-repo project metadata for the Hexalith.Memories server, started inline by the Tenants AppHost so
/// the Tenants list can search the curated <c>tenants-index</c>. SuppressBuild is true: the Memories server
/// is built independently (Aspire runs children with --no-build), so the AppHost build never compiles it and
/// the two repos' package graphs stay isolated.
/// </summary>
public class HexalithMemoriesServer : IProjectMetadata {

    public string ProjectPath => RepositoryProjectPaths.GetProjectPath(
        "Hexalith.Memories",
        "src",
        "Hexalith.Memories.Server",
        "Hexalith.Memories.Server.csproj");

    public bool SuppressBuild => true;
}
