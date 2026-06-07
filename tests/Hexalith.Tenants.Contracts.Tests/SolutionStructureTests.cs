using System.Xml.Linq;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class SolutionStructureTests {
    private static readonly string[] RequiredSourceProjects =
    [
        "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj",
        "src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj",
        "src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj",
        "src/Hexalith.Tenants/Hexalith.Tenants.csproj",
        "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj",
        "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj",
        // Tenants keeps its own Aspire AppHost (its composition root), but it consumes the platform
        // Aspire boilerplate (AddHexalithEventStore + AddEventStoreDomainModule) rather than a per-domain
        // Aspire library.
        "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
    ];

    // Domain-centric guardrail (Epic B): the module must NOT re-implement reusable infrastructure as its
    // own Aspire wiring library or a ServiceDefaults copy — that boilerplate lives in the EventStore
    // platform (Hexalith.EventStore.Aspire + the domain-service SDK), consumed by the AppHost above.
    private static readonly string[] ForbiddenSourceProjects =
    [
        "src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj",
        "src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj",
    ];

    private static readonly string[] RequiredTestProjects =
    [
        "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj",
        "tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj",
        "tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj",
        "tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj",
        "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj",
        "tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj",
    ];

    private static readonly string[] RootSubmoduleProjectPrefixes =
    [
        "Hexalith.Commons/",
        "Hexalith.EventStore/",
        "Hexalith.FrontComposer/",
    ];

    private static readonly string[] ForbiddenTenantQueryRoutingTerms =
    [
        "Projection" + "ActorType",
        "Tenant" + "ProjectionRouting",
    ];

    [Fact]
    public void Hexalith_Tenants_slnx_contains_required_source_and_test_projects() {
        string repoRoot = FindRepoRoot();
        string solutionPath = Path.Combine(repoRoot, "Hexalith.Tenants.slnx");

        HashSet<string> projectPaths = XDocument.Load(solutionPath)
            .Descendants("Project")
            .Select(project => project.Attribute("Path")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(NormalizePath)
            .ToHashSet(StringComparer.Ordinal);

        foreach (string project in RequiredSourceProjects.Concat(RequiredTestProjects)) {
            projectPaths.ShouldContain(project);
        }

        foreach (string forbidden in ForbiddenSourceProjects) {
            projectPaths.ShouldNotContain(forbidden);
        }

        projectPaths.ShouldAllBe(path => IsOwnedProject(path) || IsAllowedRootSubmoduleProject(path));
        File.Exists(Path.Combine(repoRoot, "Hexalith.Tenants.sln")).ShouldBeFalse();
    }

    [Fact]
    public void Required_solution_projects_exist_on_disk() {
        string repoRoot = FindRepoRoot();

        foreach (string project in RequiredSourceProjects.Concat(RequiredTestProjects)) {
            File.Exists(Path.Combine(repoRoot, project)).ShouldBeTrue($"{project} must exist because Hexalith.Tenants.slnx references it.");
        }

        foreach (string forbidden in ForbiddenSourceProjects) {
            File.Exists(Path.Combine(repoRoot, forbidden))
                .ShouldBeFalse($"{forbidden} must not exist — the platform provides this boilerplate (domain-centric rule).");
        }

        Directory.GetFiles(repoRoot, "*.sln", SearchOption.TopDirectoryOnly).ShouldBeEmpty();
        File.Exists(Path.Combine(repoRoot, "Hexalith.Tenants.slnx")).ShouldBeTrue();
    }

    [Fact]
    public void Project_references_preserve_story_boundaries() {
        string repoRoot = FindRepoRoot();

        string[] contractsReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj");
        contractsReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.", StringComparison.Ordinal));

        string[] clientReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj");
        clientReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        clientReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Server", StringComparison.Ordinal));

        string[] serverReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj");
        serverReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        serverReferences.ShouldContain(reference => reference.Contains("Hexalith.EventStore.Server", StringComparison.Ordinal));
        serverReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Client", StringComparison.Ordinal));
        serverReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Testing", StringComparison.Ordinal));

        string[] testingReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj");
        testingReferences.ShouldContain("..\\Hexalith.Tenants.Server\\Hexalith.Tenants.Server.csproj");
        testingReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");

        string[] hostReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants/Hexalith.Tenants.csproj");
        hostReferences.ShouldContain("..\\Hexalith.Tenants.Server\\Hexalith.Tenants.Server.csproj");
        hostReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        // The host consumes the platform domain-service SDK for hosting/telemetry/health/endpoints
        // instead of a per-domain ServiceDefaults copy (domain-centric rule).
        hostReferences.ShouldContain(reference => reference.Contains("Hexalith.EventStore.DomainService", StringComparison.Ordinal));
        hostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.ServiceDefaults", StringComparison.Ordinal));
        hostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Client", StringComparison.Ordinal));
        hostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Testing", StringComparison.Ordinal));
        hostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.AppHost", StringComparison.Ordinal));
        hostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Aspire", StringComparison.Ordinal));
    }

    [Fact]
    public void EventStore_submodule_setup_remains_root_level_only() {
        string repoRoot = FindRepoRoot();

        string gitmodules = File.ReadAllText(Path.Combine(repoRoot, ".gitmodules"));
        gitmodules.ShouldContain("path = Hexalith.EventStore");
        gitmodules.ShouldContain("path = Hexalith.Commons");
        gitmodules.ShouldContain("path = Hexalith.AI.Tools");
        gitmodules.ShouldContain("path = Hexalith.Builds");
        gitmodules.ShouldContain("path = Hexalith.FrontComposer");

        string directoryBuildProps = File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.props"));
        directoryBuildProps.ShouldContain("HexalithEventStoreRoot");
        directoryBuildProps.ShouldContain("Hexalith.EventStore\\src\\Hexalith.EventStore.Contracts");

        string[] setupDocs =
        [
            "README.md",
            "CONTRIBUTING.md",
            "docs/quickstart.md",
        ];

        foreach (string setupDoc in setupDocs) {
            string text = File.ReadAllText(Path.Combine(repoRoot, setupDoc));
            text.ShouldContain("git submodule update --init");
            if (text.Contains("--recursive", StringComparison.Ordinal)) {
                text.ShouldContain("Do not");
                text.ShouldContain("recursive");
            }
        }
    }

    [Fact]
    public void Root_solution_build_defaults_force_single_node_serial_builds() {
        string repoRoot = FindRepoRoot();

        string responseFile = File.ReadAllText(Path.Combine(repoRoot, "MSBuild.rsp"));
        responseFile.ShouldContain("-m:1");
        responseFile.ShouldContain("-p:BuildInParallel=false");
        responseFile.ShouldContain("-p:RestoreBuildInParallel=false");

        string solutionTargets = File.ReadAllText(Path.Combine(repoRoot, "Directory.Solution.targets"));
        solutionTargets.ShouldContain("BuildInParallel=\"False\"");
        solutionTargets.ShouldContain("BuildInParallel=false;RestoreBuildInParallel=false");
    }

    [Fact]
    public void Tenant_ui_contracts_and_host_do_not_use_projection_actor_query_routing() {
        string repoRoot = FindRepoRoot();
        string[] scannedRoots =
        [
            "src/Hexalith.Tenants.Contracts",
            "src/Hexalith.Tenants.UI",
            "src/Hexalith.Tenants",
        ];

        foreach (string relativeRoot in scannedRoots) {
            foreach (string file in Directory.GetFiles(Path.Combine(repoRoot, relativeRoot), "*.cs", SearchOption.AllDirectories)) {
                string relativePath = Path.GetRelativePath(repoRoot, file);
                string text = File.ReadAllText(file);

                foreach (string term in ForbiddenTenantQueryRoutingTerms) {
                    text.Contains(term, StringComparison.Ordinal)
                        .ShouldBeFalse($"{relativePath} must use the Tenants REST query API instead of the retired actor query path.");
                }
            }
        }
    }

    private static string[] GetProjectReferences(string repoRoot, string projectPath)
        => XDocument.Load(Path.Combine(repoRoot, projectPath))
            .Descendants("ProjectReference")
            .Select(reference => reference.Attribute("Include")?.Value)
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => path!)
            .ToArray();

    private static string FindRepoRoot() {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null) {
            if (File.Exists(Path.Combine(current.FullName, "Hexalith.Tenants.slnx"))) {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate Hexalith.Tenants repository root.");
    }

    private static string NormalizePath(string? path)
        => (path ?? string.Empty).Replace('\\', '/');

    private static bool IsOwnedProject(string path)
        => path.StartsWith("src/", StringComparison.Ordinal)
        || path.StartsWith("tests/", StringComparison.Ordinal)
        || path.StartsWith("samples/", StringComparison.Ordinal);

    private static bool IsAllowedRootSubmoduleProject(string path)
        => RootSubmoduleProjectPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.Ordinal));
}
