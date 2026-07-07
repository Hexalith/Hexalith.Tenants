using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

using Hexalith.EventStore.Aspire;

namespace Hexalith.Tenants.Aspire;

/// <summary>
/// Cross-repo project metadata for the Hexalith.Tenants domain server, resolved from the consuming repository's
/// <c>Hexalith.Tenants</c> checkout via <see cref="RepositoryProjectPaths.GetReferencedModuleProjectPath"/>,
/// which tolerates every layout (the dependency as the current repo, under this repo's <c>references/</c>, a
/// sibling under a parent's <c>references/</c>, or a nested checkout inside the Tenants repo). <see cref="SuppressBuild"/>
/// stays <see langword="true"/>: the Tenants server is built independently of the consuming AppHost (Aspire runs
/// children with <c>--no-build</c>), so the AppHost build never compiles it.
/// </summary>
/// <remarks>
/// This mirrors <c>EventStoreProjectMetadata</c> and <c>MemoriesServerProjectMetadata</c>. The earlier hand-rolled
/// two-branch probe only checked the own-repo (<c>&lt;root&gt;/src/…</c>) and root-level-submodule
/// (<c>&lt;root&gt;/Hexalith.Tenants/src/…</c>) layouts, so it failed to resolve the canonical consumer layout where
/// Tenants is checked out under <c>&lt;root&gt;/references/Hexalith.Tenants/…</c> — the shared helper probes that path
/// (and the others) in the same order as the <c>$(Hexalith*Root)</c> auto-detection in <c>Directory.Build.props</c>.
/// </remarks>
internal sealed class TenantsServerProjectMetadata : IProjectMetadata
{
    /// <inheritdoc/>
    public string ProjectPath => RepositoryProjectPaths.GetReferencedModuleProjectPath(
        "Hexalith.Tenants",
        "src",
        "Hexalith.Tenants",
        "Hexalith.Tenants.csproj");

    /// <inheritdoc/>
    public bool SuppressBuild => true;
}
