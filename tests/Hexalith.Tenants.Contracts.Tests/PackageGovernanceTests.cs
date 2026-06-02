using System.Xml.Linq;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class PackageGovernanceTests {
    private static readonly string[] CentrallyGovernedBuildProperties =
    [
        "TargetFramework",
        "Nullable",
        "ImplicitUsings",
        "TreatWarningsAsErrors",
        "LangVersion",
    ];

    private static readonly string[] CentrallyGovernedContainerProperties =
    [
        "ContainerBaseImage",
        "ContainerFamily",
        "ContainerRegistry",
        "ContainerImageTag",
        "ContainerImageTags",
        "ContainerUser",
    ];

    private static readonly string[] OwnedProjectRoots =
    [
        "src",
        "tests",
        "samples",
    ];

    private static readonly string[] PublishablePackageProjects =
    [
        "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj",
        "src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj",
        "src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj",
        "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj",
    ];

    private static readonly string[] ExplicitlyNonPackableProjects =
    [
        "src/Hexalith.Tenants/Hexalith.Tenants.csproj",
        "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
        "samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj",
        "samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj",
    ];

    private static readonly string[] BlockingTestProjects =
    [
        "tests/Hexalith.Tenants.Contracts.Tests/",
        "tests/Hexalith.Tenants.Client.Tests/",
        "tests/Hexalith.Tenants.Testing.Tests/",
        "samples/Hexalith.Tenants.Sample.Tests/",
        "tests/Hexalith.Tenants.Server.Tests/",
    ];

    private static readonly string[] ExpectedPackageIds =
    [
        "Hexalith.Tenants.Contracts",
        "Hexalith.Tenants.Client",
        "Hexalith.Tenants.Server",
        "Hexalith.Tenants.Testing",
    ];

    private static readonly string[] BoundedArtifactGlobs =
    [
        "TestResults/**/*.trx",
        "TestResults/**/coverage.cobertura.xml",
        "nupkgs/*.nupkg",
    ];

    private static readonly string[] ForbiddenWorkflowFragments =
    [
        "submodules: recursive",
        "git submodule update --recursive",
        "Hexalith.EventStore.Tests",
        "Hexalith.EventStore.*.Tests",
        "Hexalith.EventStore.*.nupkg",
        "**/bin/**",
        "**/obj/**",
        "**/TestResults/**",
        "**/*.snupkg",
        "**/.nuget/**",
    ];

    [Fact]
    public void PackageReference_versions_are_centralized_for_Tenants_owned_projects() {
        string repoRoot = FindRepoRoot();
        XDocument centralPackages = XDocument.Load(Path.Combine(repoRoot, "Directory.Packages.props"));

        centralPackages.Descendants("ManagePackageVersionsCentrally").Single().Value.ShouldBe("true");

        HashSet<string> centralPackageIds = centralPackages
            .Descendants("PackageVersion")
            .Select(package => package.Attribute("Include")?.Value)
            .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
            .Select(packageId => packageId!)
            .ToHashSet(StringComparer.Ordinal);

        List<string> violations = [];
        foreach (string projectPath in GetPackageReferenceGovernanceFiles(repoRoot)) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));

            foreach (XElement packageReference in project.Descendants("PackageReference")) {
                string packageId = packageReference.Attribute("Include")?.Value ?? packageReference.Attribute("Update")?.Value ?? "<unknown>";

                if (packageReference.Attribute("Version") is not null || packageReference.Attribute("VersionOverride") is not null) {
                    violations.Add(FormatViolation(projectPath, packageReference));
                }

                if (packageReference.Attribute("Include") is not null && !centralPackageIds.Contains(packageId)) {
                    violations.Add($"{projectPath}: PackageReference Include=\"{packageId}\" has no matching Directory.Packages.props PackageVersion: {FormatNode(packageReference)}");
                }
            }
        }

        violations.ShouldBeEmpty("Package governance violations must identify the project and offending XML node.");
    }

    [Fact]
    public void Shared_build_defaults_keep_language_warning_metadata_and_EventStore_governance() {
        string repoRoot = FindRepoRoot();
        XDocument buildProps = XDocument.Load(Path.Combine(repoRoot, "Directory.Build.props"));
        string buildPropsText = File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.props"));

        RequiredValueFor(buildProps, "TargetFramework").ShouldBe("net10.0");
        RequiredValueFor(buildProps, "Nullable").ShouldBe("enable");
        RequiredValueFor(buildProps, "ImplicitUsings").ShouldBe("enable");
        RequiredValueFor(buildProps, "TreatWarningsAsErrors").ShouldBe("true");
        RequiredValueFor(buildProps, "LangVersion").ShouldBe("latest");

        List<string> buildOverrideViolations = [];
        foreach (string projectPath in GetOwnedProjectFiles(repoRoot)) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            foreach (string propertyName in CentrallyGovernedBuildProperties) {
                buildOverrideViolations.AddRange(project
                    .Descendants(propertyName)
                    .Select(property => $"{projectPath}: central build property belongs in Directory.Build.props: {FormatNode(property)}"));
            }
        }

        RequiredValueFor(buildProps, "Authors").ShouldBe("Hexalith Contributors");
        RequiredValueFor(buildProps, "Company").ShouldBe("Hexalith");
        RequiredValueFor(buildProps, "PackageLicenseExpression").ShouldBe("MIT");
        RequiredValueFor(buildProps, "PackageProjectUrl").ShouldBe("https://github.com/Hexalith/Hexalith.Tenants");
        RequiredValueFor(buildProps, "RepositoryUrl").ShouldBe("https://github.com/Hexalith/Hexalith.Tenants");
        RequiredValueFor(buildProps, "RepositoryType").ShouldBe("git");
        RequiredValueFor(buildProps, "Description").ShouldContain("Multi-tenant management service");
        RequiredValueFor(buildProps, "PackageTags").ShouldContain("multi-tenancy");
        RequiredValueFor(buildProps, "PackageReadmeFile").ShouldBe("README.md");

        buildProps.Descendants("HexalithEventStoreRoot").Count().ShouldBe(4);
        buildPropsText.ShouldContain("Hexalith.EventStore\\src\\Hexalith.EventStore.Contracts");
        buildPropsText.ShouldNotContain("GenerateDocumentationFile");
        buildPropsText.ShouldNotContain("StyleCop");
        buildPropsText.ShouldNotContain("SonarAnalyzer");
        buildPropsText.ShouldNotContain("Roslynator");
        buildOverrideViolations.ShouldBeEmpty("Per-project build property overrides must not duplicate or weaken shared build defaults.");
    }

    [Fact]
    public void Package_and_host_projects_have_expected_packability_boundaries() {
        string repoRoot = FindRepoRoot();
        HashSet<string> expectedPackageProjects = PublishablePackageProjects.ToHashSet(StringComparer.Ordinal);
        HashSet<string> expectedNonPackageProjects = ExplicitlyNonPackableProjects.ToHashSet(StringComparer.Ordinal);
        HashSet<string> classifiedSourceAndSampleProjects = PublishablePackageProjects
            .Concat(ExplicitlyNonPackableProjects)
            .ToHashSet(StringComparer.Ordinal);

        string[] unclassifiedSourceAndSampleProjects = GetOwnedProjectFiles(repoRoot)
            .Where(project => project.StartsWith("src/", StringComparison.Ordinal) || project.StartsWith("samples/", StringComparison.Ordinal))
            .Where(project => !classifiedSourceAndSampleProjects.Contains(project))
            .ToArray();

        unclassifiedSourceAndSampleProjects.ShouldBeEmpty("Every source/sample project must be classified as one of the five packages or an explicitly non-packable host/sample project.");

        foreach (string projectPath in PublishablePackageProjects) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            ValueFor(project, "IsPackable").ShouldNotBe(
                "false",
                $"{projectPath}: expected packable package project, offending node: {FormatPropertyNodes(project, "IsPackable")}");
        }

        foreach (string projectPath in ExplicitlyNonPackableProjects) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            ValueFor(project, "IsPackable").ShouldBe(
                "false",
                $"{projectPath}: must explicitly opt out of NuGet packing, offending node: {FormatPropertyNodes(project, "IsPackable")}");
        }

        XDocument testProps = XDocument.Load(Path.Combine(repoRoot, "tests/Directory.Build.props"));
        ValueFor(testProps, "IsPackable").ShouldBe("false");
        ValueFor(testProps, "IsTestProject").ShouldBe("true");

        List<string> testPackabilityViolations = [];
        foreach (string projectPath in GetOwnedProjectFiles(repoRoot).Where(project => project.StartsWith("tests/", StringComparison.Ordinal))) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            foreach (XElement isPackable in project.Descendants("IsPackable").Where(property => property.Value != "false")) {
                testPackabilityViolations.Add($"{projectPath}: test project must inherit or preserve test non-packability: {FormatNode(isPackable)}");
            }
        }

        testPackabilityViolations.ShouldBeEmpty();
        GetOwnedProjectFiles(repoRoot).Where(expectedPackageProjects.Contains).Count().ShouldBe(PublishablePackageProjects.Length);
        GetOwnedProjectFiles(repoRoot).Where(expectedNonPackageProjects.Contains).Count().ShouldBe(ExplicitlyNonPackableProjects.Length);
    }

    [Fact]
    public void Container_publishing_stays_on_shared_dotnet_sdk_defaults() {
        string repoRoot = FindRepoRoot();
        XDocument targets = XDocument.Load(Path.Combine(repoRoot, "Directory.Build.targets"));
        XDocument hostProject = XDocument.Load(Path.Combine(repoRoot, "src/Hexalith.Tenants/Hexalith.Tenants.csproj"));

        RequiredValueFor(targets, "ContainerBaseImage").ShouldBe("mcr.microsoft.com/dotnet/aspnet:10.0-alpine");
        RequiredValueFor(targets, "ContainerRegistry").ShouldBe("registry.hexalith.com");
        RequiredValueFor(targets, "ContainerUser").ShouldBe("app");
        targets.Descendants("ContainerPort").Single().Attribute("Include")?.Value.ShouldBe("8080");

        string[] labelNames = targets.Descendants("ContainerLabel")
            .Select(label => label.Attribute("Include")?.Value)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label!)
            .ToArray();

        labelNames.ShouldContain("org.opencontainers.image.source");
        labelNames.ShouldContain("org.opencontainers.image.licenses");
        labelNames.ShouldContain("org.opencontainers.image.vendor");

        RequiredValueFor(hostProject, "EnableContainer").ShouldBe("true");
        RequiredValueFor(hostProject, "ContainerRepository").ShouldBe("tenants");
        GetProjectContainerDefaultOverrides(repoRoot).ShouldBeEmpty("Container defaults belong in Directory.Build.targets; projects should only opt in with EnableContainer and ContainerRepository.");
        GetAdHocContainerFiles(repoRoot).ShouldBeEmpty("Phase 1 container governance must use .NET SDK publish properties instead of Dockerfiles or compose files.");
    }

    [Fact]
    public void Ci_workflow_enforces_build_test_coverage_and_artifact_governance() {
        string repoRoot = FindRepoRoot();
        string workflowPath = Path.Combine(repoRoot, ".github/workflows/ci.yml");
        string workflow = File.ReadAllText(workflowPath);

        workflow.ShouldContain("permissions:\n  contents: read");
        workflow.ShouldContain("DAPR_VERSION: '1.17.");
        workflow.ShouldContain("global-json-file: global.json");
        workflow.ShouldContain("dotnet restore Hexalith.Tenants.slnx");
        workflow.ShouldContain("dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror");
        workflow.ShouldContain("fetch-depth: 0");
        workflow.ShouldContain("submodules: true");
        workflow.ShouldNotContain("recursive");

        foreach (string testProject in BlockingTestProjects) {
            workflow.ShouldContain(testProject);
            workflow.ShouldContain($"{testProject} --no-build --configuration Release");
        }

        workflow.ShouldContain("scripts/validate-coverage.py");
        workflow.ShouldContain("--minimum-line-coverage 80");
        workflow.ShouldContain("--required-branch-coverage 100");
        workflow.ShouldContain("src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs");
        workflow.ShouldContain("$GITHUB_STEP_SUMMARY");

        foreach (string artifactGlob in BoundedArtifactGlobs.Take(2)) {
            workflow.ShouldContain(artifactGlob);
        }

        foreach (string forbiddenFragment in ForbiddenWorkflowFragments) {
            workflow.ShouldNotContain(forbiddenFragment);
        }

        GetWorkflowActionReferences(workflow).ShouldAllBe(
            action => IsFullCommitSha(action.Reference),
            "GitHub Actions references must stay pinned to full commit SHAs.");
    }

    [Fact]
    public void Release_workflow_packs_validates_and_publishes_only_expected_packages() {
        string repoRoot = FindRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github/workflows/release.yml"));
        string releaseConfig = File.ReadAllText(Path.Combine(repoRoot, "release.config.cjs"));
        string packageValidator = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-nuget-packages.py"));

        workflow.ShouldContain("DAPR_VERSION: '1.17.");
        workflow.ShouldContain("global-json-file: global.json");
        workflow.ShouldContain("dotnet restore Hexalith.Tenants.slnx");
        workflow.ShouldContain("dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror");
        workflow.ShouldContain("npx semantic-release");
        workflow.ShouldNotContain("dotnet nuget push **");
        workflow.ShouldNotContain("recursive");

        foreach (string testProject in BlockingTestProjects) {
            workflow.ShouldContain(testProject);
        }

        releaseConfig.ShouldContain("@semantic-release/exec");
        releaseConfig.ShouldContain("python3 scripts/pack-release-packages.py ./nupkgs ${nextRelease.version}");
        releaseConfig.ShouldContain("python3 scripts/validate-nuget-packages.py ./nupkgs");
        releaseConfig.ShouldContain("python3 scripts/validate-consumer-package-references.py ./nupkgs");
        releaseConfig.ShouldContain("dotnet nuget push ./nupkgs/*.nupkg");
        releaseConfig.ShouldContain("--skip-duplicate");
        releaseConfig.ShouldContain("NUGET_API_KEY");
        releaseConfig.ShouldContain("assets: ['nupkgs/*.nupkg']");
        releaseConfig.ShouldNotContain(".snupkg");
        releaseConfig.ShouldNotContain("**/*.nupkg");
        packageValidator.ShouldContain("not path.name.endswith(\".snupkg\")");
        packageValidator.ShouldContain("\".symbols.\" not in path.name");
        packageValidator.ShouldContain("EXPECTED_DEPENDENCIES");
        packageValidator.ShouldContain("FORBIDDEN_DEPENDENCY_IDS");

        foreach (string packageId in ExpectedPackageIds) {
            releaseConfig.ShouldContain(packageId);
        }

        foreach (string forbiddenFragment in ForbiddenWorkflowFragments) {
            workflow.ShouldNotContain(forbiddenFragment);
            releaseConfig.ShouldNotContain(forbiddenFragment);
        }

        GetWorkflowActionReferences(workflow).ShouldAllBe(
            action => IsFullCommitSha(action.Reference),
            "GitHub Actions references must stay pinned to full commit SHAs.");
    }

    [Fact]
    public void Ci_workflow_runs_package_consumer_validation_after_release_build_and_pack() {
        string repoRoot = FindRepoRoot();
        string workflow = File.ReadAllText(Path.Combine(repoRoot, ".github/workflows/ci.yml"));

        workflow.ShouldContain("python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test");
        workflow.ShouldContain("python3 scripts/validate-nuget-packages.py ./nupkgs");
        workflow.ShouldContain("python3 scripts/validate-consumer-package-references.py ./nupkgs");
        workflow.IndexOf("dotnet build Hexalith.Tenants.slnx --no-restore --configuration Release -warnaserror", StringComparison.Ordinal)
            .ShouldBeLessThan(workflow.IndexOf("python3 scripts/pack-release-packages.py ./nupkgs 0.0.0-ci-test", StringComparison.Ordinal));
    }

    [Fact]
    public void Consumer_package_reference_script_verifies_public_package_surfaces() {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-consumer-package-references.py"));

        script.ShouldContain("Hexalith.Tenants.Contracts");
        script.ShouldContain("Hexalith.Tenants.Client");
        script.ShouldContain("Hexalith.Tenants.Testing");
        // The per-domain Aspire consumer surface (AddHexalithTenants / HexalithTenantsResources) was
        // removed with the Hexalith.Tenants.Aspire package (domain-centric refactor); orchestration is
        // provided by the EventStore platform's AddEventStoreDomainModule.
        script.ShouldNotContain("Hexalith.Tenants.Aspire");
        script.ShouldContain("CreateTenant");
        script.ShouldContain("TenantCreated");
        script.ShouldContain("ListTenantsQuery");
        script.ShouldContain("InMemoryTenantService");
        script.ShouldContain("TenantTestHelpers");
        script.ShouldContain("InMemoryTenantProjection");
        script.ShouldContain("ProjectReference");
        script.ShouldContain("dotnet");
        script.ShouldContain("NuGet.Config");
        script.ShouldContain("local-tenants-packages");
        script.ShouldContain("inherited NuGet.Config sources are preserved");
        script.ShouldContain("run_xunit_assembly");
        script.ShouldNotContain("[\"test\"");
    }

    [Fact]
    public void NuGet_package_validator_enforces_dependency_boundaries() {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-nuget-packages.py"));

        foreach (string packageId in ExpectedPackageIds) {
            script.ShouldContain(packageId);
        }

        script.ShouldContain("Hexalith.EventStore.Contracts");
        script.ShouldContain("Hexalith.EventStore.Server");
        script.ShouldContain("Dapr.AspNetCore");
        script.ShouldContain("Dapr.Client");
        // AppHost/ServiceDefaults remain in the validator's forbidden-dependency surface even though the
        // per-domain projects were removed — no published package may ever depend on such host/composition ids.
        script.ShouldContain("Hexalith.Tenants.AppHost");
        script.ShouldContain("Hexalith.Tenants.ServiceDefaults");
        script.ShouldContain("samples");
        script.ShouldContain("dependency");
    }

    [Fact]
    public void Coverage_gate_script_enforces_overall_and_named_isolation_thresholds() {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-coverage.py"));

        script.ShouldContain("coverage.cobertura.xml");
        script.ShouldContain("minimum_line_coverage");
        script.ShouldContain("required_branch_coverage");
        script.ShouldContain("condition-coverage");
        script.ShouldContain("src/Hexalith.Tenants.Contracts/");
        script.ShouldContain("src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs");
        script.ShouldContain("src/Hexalith.Tenants.Server/Aggregates/GlobalAdministratorsAggregate.cs");
        script.ShouldContain("src/Hexalith.Tenants.Server/Validators/ChangeUserRoleValidator.cs");
        script.ShouldContain("GITHUB_STEP_SUMMARY");
        script.ShouldContain("No publishable-package line coverage data found");
        script.ShouldContain("No isolation/auth branch coverage data found");
    }

    private static string FormatViolation(string projectPath, XElement packageReference)
        => $"{projectPath}: {FormatNode(packageReference)}";

    private static string FormatNode(XElement node)
        => node.ToString(SaveOptions.DisableFormatting);

    private static string FormatPropertyNodes(XDocument document, string propertyName) {
        string[] nodes = document.Descendants(propertyName)
            .Select(FormatNode)
            .ToArray();

        return nodes.Length == 0 ? "<missing>" : string.Join(", ", nodes);
    }

    private static string[] GetAdHocContainerFiles(string repoRoot)
        => Directory.EnumerateFiles(repoRoot, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Where(path => !path.StartsWith("bin/", StringComparison.Ordinal))
            .Where(path => !path.Contains("/bin/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("obj/", StringComparison.Ordinal))
            .Where(path => !path.Contains("/obj/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("Hexalith.EventStore/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("Hexalith.Commons/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("Hexalith.Builds/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("Hexalith.FrontComposer/", StringComparison.Ordinal))
            .Where(IsAdHocContainerFile)
            .ToArray();

    private static string[] GetOwnedProjectFiles(string repoRoot)
        => OwnedProjectRoots
            .Select(root => Path.Combine(repoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories))
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .Order(StringComparer.Ordinal)
            .ToArray();

    private static string[] GetPackageReferenceGovernanceFiles(string repoRoot) {
        string[] ownedProjectFiles = GetOwnedProjectFiles(repoRoot);
        string[] sharedBuildFiles = OwnedProjectRoots
            .Select(root => Path.Combine(repoRoot, root))
            .Where(Directory.Exists)
            .SelectMany(root => Directory.EnumerateFiles(root, "Directory.Build.*", SearchOption.AllDirectories))
            .Append(Path.Combine(repoRoot, "Directory.Build.props"))
            .Append(Path.Combine(repoRoot, "Directory.Build.targets"))
            .Where(File.Exists)
            .Select(path => Path.GetRelativePath(repoRoot, path).Replace('\\', '/'))
            .ToArray();

        return [.. ownedProjectFiles.Concat(sharedBuildFiles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)];
    }

    private static bool IsAdHocContainerFile(string path) {
        string fileName = Path.GetFileName(path);
        return fileName.StartsWith("Dockerfile", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("docker-compose.yml", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("docker-compose.yaml", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".dcproj", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] GetProjectContainerDefaultOverrides(string repoRoot) {
        List<string> violations = [];
        foreach (string projectPath in GetOwnedProjectFiles(repoRoot)) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            foreach (string propertyName in CentrallyGovernedContainerProperties) {
                violations.AddRange(project.Descendants(propertyName).Select(property => $"{projectPath}: {FormatNode(property)}"));
            }

            violations.AddRange(project.Descendants("ContainerPort").Select(port => $"{projectPath}: {FormatNode(port)}"));
            violations.AddRange(project.Descendants("ContainerLabel").Select(label => $"{projectPath}: {FormatNode(label)}"));
        }

        return [.. violations];
    }

    private static (string Action, string Reference)[] GetWorkflowActionReferences(string workflow)
        => workflow
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith("uses: ", StringComparison.Ordinal))
            .Select(line => line["uses: ".Length..].Split('@'))
            .Where(parts => parts.Length == 2)
            .Select(parts => (Action: parts[0], Reference: parts[1].Split(' ')[0]))
            .ToArray();

    private static bool IsFullCommitSha(string value)
        => value.Length == 40 && value.All(IsLowerHexDigit);

    private static bool IsLowerHexDigit(char value)
        => value is >= '0' and <= '9' or >= 'a' and <= 'f';

    private static string? ValueFor(XDocument document, string elementName)
        => document.Descendants(elementName).FirstOrDefault()?.Value;

    private static string RequiredValueFor(XDocument document, string elementName)
        => ValueFor(document, elementName) ?? throw new InvalidOperationException($"Missing required MSBuild property {elementName}.");

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
}
