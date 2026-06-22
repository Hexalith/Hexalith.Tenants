using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Cross-repo project metadata for the Hexalith.Tenants domain server. <see cref="SuppressBuild"/> is
/// <see langword="true"/>: the Tenants server is built independently of the consuming AppHost (Aspire runs
/// children with <c>--no-build</c>), so the AppHost build never compiles it.
/// </summary>
/// <remarks>
/// The project path is resolved against the consuming repository root via the shared
/// <see cref="RepositoryProjectPaths"/> helper, tolerating both layouts: the Tenants repository's own AppHost
/// (<c>&lt;root&gt;/src/Hexalith.Tenants/…</c>) and an external consumer where Tenants is a Git submodule
/// (<c>&lt;root&gt;/Hexalith.Tenants/src/Hexalith.Tenants/…</c>).
/// </remarks>
internal sealed class TenantsServerProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath
    {
        get
        {
            // Own-repo layout (the Tenants AppHost itself).
            string local = RepositoryProjectPaths.GetProjectPath(
                "src",
                "Hexalith.Tenants",
                "Hexalith.Tenants.csproj");
            if (File.Exists(local))
            {
                return local;
            }

            // Submodule layout (an external consumer hosting Tenants as a submodule).
            return RepositoryProjectPaths.GetProjectPath(
                "Hexalith.Tenants",
                "src",
                "Hexalith.Tenants",
                "Hexalith.Tenants.csproj");
        }
    }

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
