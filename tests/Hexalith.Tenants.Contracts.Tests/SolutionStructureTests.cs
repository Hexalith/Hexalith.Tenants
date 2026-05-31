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
        "src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj",
        "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
        "src/Hexalith.Tenants.ServiceDefaults/Hexalith.Tenants.ServiceDefaults.csproj",
        "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj",
    ];

    private static readonly string[] RequiredTestProjects =
    [
        "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj",
        "tests/Hexalith.Tenants.Client.Tests/Hexalith.Tenants.Client.Tests.csproj",
        "tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj",
        "tests/Hexalith.Tenants.Testing.Tests/Hexalith.Tenants.Testing.Tests.csproj",
        "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj",
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

        projectPaths.ShouldNotContain(path => path.StartsWith("Hexalith.EventStore/", StringComparison.Ordinal));
        File.Exists(Path.Combine(repoRoot, "Hexalith.Tenants.sln")).ShouldBeFalse();
    }

    [Fact]
    public void Required_solution_projects_exist_on_disk() {
        string repoRoot = FindRepoRoot();

        foreach (string project in RequiredSourceProjects.Concat(RequiredTestProjects)) {
            File.Exists(Path.Combine(repoRoot, project)).ShouldBeTrue($"{project} must exist because Hexalith.Tenants.slnx references it.");
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
        clientReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.AppHost", StringComparison.Ordinal));
        clientReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Aspire", StringComparison.Ordinal));

        string[] serverReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj");
        serverReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        serverReferences.ShouldContain(reference => reference.Contains("Hexalith.EventStore.Server", StringComparison.Ordinal));
        serverReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Client", StringComparison.Ordinal));
        serverReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Testing", StringComparison.Ordinal));

        string[] testingReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj");
        testingReferences.ShouldContain("..\\Hexalith.Tenants.Server\\Hexalith.Tenants.Server.csproj");
        testingReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");

        string[] aspireReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj");
        aspireReferences.ShouldBeEmpty();

        string[] appHostReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj");
        appHostReferences.ShouldContain("..\\Hexalith.Tenants.Aspire\\Hexalith.Tenants.Aspire.csproj");
        appHostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.EventStore", StringComparison.Ordinal));
        appHostReferences.ShouldAllBe(reference => !reference.Contains("Hexalith.Tenants.Sample", StringComparison.Ordinal));

        string[] hostReferences = GetProjectReferences(repoRoot, "src/Hexalith.Tenants/Hexalith.Tenants.csproj");
        hostReferences.ShouldContain("..\\Hexalith.Tenants.Server\\Hexalith.Tenants.Server.csproj");
        hostReferences.ShouldContain("..\\Hexalith.Tenants.Contracts\\Hexalith.Tenants.Contracts.csproj");
        hostReferences.ShouldContain("..\\Hexalith.Tenants.ServiceDefaults\\Hexalith.Tenants.ServiceDefaults.csproj");
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
            text.ShouldNotContain("--recursive");
        }
    }

    [Fact]
    public void AppHost_keeps_Aspire_reference_out_of_project_resource_graph() {
        string repoRoot = FindRepoRoot();
        XElement aspireReference = XDocument.Load(Path.Combine(repoRoot, "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj"))
            .Descendants("ProjectReference")
            .Single(reference => string.Equals(
                reference.Attribute("Include")?.Value,
                "..\\Hexalith.Tenants.Aspire\\Hexalith.Tenants.Aspire.csproj",
                StringComparison.Ordinal));

        aspireReference.Attribute("IsAspireProjectResource")?.Value.ShouldBe("false");

        string solutionText = File.ReadAllText(Path.Combine(repoRoot, "Hexalith.Tenants.slnx"));
        solutionText.ShouldContain("Path=\"src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj\"");
        solutionText.ShouldNotContain("<Build Solution=\"*|*\" Project=\"false\" />");
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
}
