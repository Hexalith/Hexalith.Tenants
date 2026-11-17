using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class PackageGovernanceTests {
    private const string PrivateSourceProofCondition = "'$(Configuration)' == 'Debug' and '$(HexalithEventStoreFromSource)' == 'true' and '$(EnablePrivateOwnerSourceReadProof)' == 'true'";
    private const string PrivateSourceProofPackageCondition = "'$(Configuration)' != 'Debug' or '$(HexalithEventStoreFromSource)' != 'true' or '$(EnablePrivateOwnerSourceReadProof)' != 'true'";
    private const string PrivateSourceProofProject = "$(HexalithEventStoreRoot)\\src\\Hexalith.EventStore.Testing\\Hexalith.EventStore.Testing.csproj";

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
        "src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj",
    ];

    private static readonly string[] ExplicitlyNonPackableProjects =
    [
        "src/Hexalith.Tenants/Hexalith.Tenants.csproj",
        "src/Hexalith.Tenants.Api/Hexalith.Tenants.Api.csproj",
        "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj",
        "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj",
        "samples/Hexalith.Tenants.Sample/Hexalith.Tenants.Sample.csproj",
        "samples/Hexalith.Tenants.Sample.Tests/Hexalith.Tenants.Sample.Tests.csproj",
        "tests/Hexalith.Tenants.AppHost.Tests/Hexalith.Tenants.AppHost.Tests.csproj",
    ];

    private static readonly string[] ExpectedPackageIds =
    [
        "Hexalith.Tenants.Contracts",
        "Hexalith.Tenants.Client",
        "Hexalith.Tenants.Server",
        "Hexalith.Tenants.Testing",
        "Hexalith.Tenants.Aspire",
    ];

    [Fact]
    public void PackageReference_versions_are_centralized_for_Tenants_owned_projects() {
        string repoRoot = FindRepoRoot();
        XDocument centralPackages = XDocument.Load(Path.Combine(repoRoot, "Directory.Packages.props"));

        centralPackages.Descendants("ManagePackageVersionsCentrally").Single().Value.ShouldBe("true");
        HashSet<string> centralPackageIds = GetCentralPackageIds(repoRoot);

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
    public void Source_edges_do_not_export_a_foreign_assembly_version() {
        string root = FindRepoRoot();
        foreach (string project in Directory.EnumerateFiles(Path.Combine(root, "src"), "*.csproj", SearchOption.AllDirectories)) {
            foreach (XElement edge in XDocument.Load(project).Descendants("ProjectReference")) {
                string metadata = ItemMetadataValue(edge, "AdditionalProperties");
                metadata.Split(';').Any(value => value.TrimStart().StartsWith("Version=", StringComparison.OrdinalIgnoreCase))
                    .ShouldBeFalse($"{project}: source references must retain their dependency's assembly version.");
            }
        }
    }

    [Fact]
    public void EventStore_host_dependencies_follow_one_complementary_source_package_policy() {
        const string SourceCondition = "'$(HexalithEventStoreFromSource)' == 'true'";
        const string PackageCondition = "'$(HexalithEventStoreFromSource)' != 'true'";
        const string VersionMetadata = "HexalithEventStoreVersion=$(HexalithEventStoreVersion)";
        const string HostProjectPath = "src/Hexalith.Tenants/Hexalith.Tenants.csproj";

        string repoRoot = FindRepoRoot();
        XDocument hostProject = XDocument.Load(Path.Combine(repoRoot, HostProjectPath));

        // Enumerated rather than looked up by literal package id: a rule naming individual packages
        // cannot see a newly added EventStore reference, which is exactly how a mixed
        // source/package graph would reappear after this story closed.
        List<(string PackageId, XElement Element)> projectEdges =
            EventStoreReferences(hostProject, "ProjectReference");
        List<(string PackageId, XElement Element)> packageEdges =
            EventStoreReferences(hostProject, "PackageReference");
        XElement privateHelper = ValidatePrivateSourceProofEdge(hostProject);
        projectEdges.RemoveAll(edge => ReferenceEquals(edge.Element, privateHelper));

        List<string> violations = [];

        foreach (IGrouping<string, (string PackageId, XElement Element)> duplicate in projectEdges
            .GroupBy(edge => edge.PackageId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)) {
            violations.Add($"{HostProjectPath}: {duplicate.Key} is referenced as a project {duplicate.Count()} times.");
        }

        HashSet<string> projectIds = [.. projectEdges.Select(edge => edge.PackageId)];
        HashSet<string> packageIds = [.. packageEdges.Select(edge => edge.PackageId)];

        foreach (string packageId in projectIds.Except(packageIds, StringComparer.Ordinal).Order(StringComparer.Ordinal)) {
            violations.Add(
                $"{HostProjectPath}: {packageId} has a ProjectReference with no complementary PackageReference, "
                + "so package mode would drop it or resolve a mixed graph.");
        }

        foreach (string packageId in packageIds.Except(projectIds, StringComparer.Ordinal).Order(StringComparer.Ordinal)) {
            violations.Add(
                $"{HostProjectPath}: {packageId} has a PackageReference with no complementary ProjectReference, "
                + "so source mode would resolve a package for a host dependency.");
        }

        foreach ((string packageId, XElement projectReference) in projectEdges.OrderBy(edge => edge.PackageId, StringComparer.Ordinal)) {
            string condition = EffectiveCondition(projectReference);
            if (!string.Equals(condition, SourceCondition, StringComparison.Ordinal)) {
                violations.Add($"{HostProjectPath}: {packageId} ProjectReference condition is '{condition}', expected '{SourceCondition}'.");
            }

            if (!string.Equals(ItemMetadataValue(projectReference, "AdditionalProperties"), VersionMetadata, StringComparison.Ordinal)) {
                violations.Add($"{HostProjectPath}: {packageId} ProjectReference must carry AdditionalProperties=\"{VersionMetadata}\".");
            }
        }

        foreach ((string packageId, XElement packageReference) in packageEdges.OrderBy(edge => edge.PackageId, StringComparer.Ordinal)) {
            string condition = EffectiveCondition(packageReference);
            if (!string.Equals(condition, PackageCondition, StringComparison.Ordinal)) {
                violations.Add($"{HostProjectPath}: {packageId} PackageReference condition is '{condition}', expected '{PackageCondition}'.");
            }

            if (HasLocalVersionAuthority(packageReference)) {
                violations.Add($"{HostProjectPath}: {packageId} PackageReference declares a local version; the Builds catalog is the only authority.");
            }
        }

        projectEdges.ShouldNotBeEmpty("The domain host must declare its EventStore dependencies; an empty result would pass vacuously.");
        violations.ShouldBeEmpty(
            "EventStore host dependencies must arrive as complementary source/package pairs.");
    }

    [Theory]
    [InlineData("none")]
    [InlineData("missing")]
    [InlineData("debug")]
    [InlineData("source")]
    [InlineData("opt-in")]
    [InlineData("path")]
    [InlineData("duplicate")]
    [InlineData("package")]
    [InlineData("project-update")]
    [InlineData("project-remove")]
    [InlineData("package-update")]
    [InlineData("define-condition")]
    [InlineData("define-token")]
    public void Private_owner_source_helper_admission_refuses_widened_or_ambiguous_edges(string mutation) {
        XDocument project = XDocument.Load(Path.Combine(FindRepoRoot(), "src/Hexalith.Tenants/Hexalith.Tenants.csproj"));
        XElement helper = EventStoreReferences(project, "ProjectReference").Single(edge => edge.PackageId == "Hexalith.EventStore.Testing").Element;
        XElement symbol = project.Descendants("DefineConstants").Single();
        switch (mutation) {
            case "none": break;
            case "missing": helper.Remove(); break;
            case "debug": helper.Parent!.SetAttributeValue("Condition", PrivateSourceProofCondition.Replace("'$(Configuration)' == 'Debug' and ", "", StringComparison.Ordinal)); break;
            case "source": helper.Parent!.SetAttributeValue("Condition", PrivateSourceProofCondition.Replace("'$(HexalithEventStoreFromSource)' == 'true' and ", "", StringComparison.Ordinal)); break;
            case "opt-in": helper.Parent!.SetAttributeValue("Condition", PrivateSourceProofCondition.Replace(" and '$(EnablePrivateOwnerSourceReadProof)' == 'true'", "", StringComparison.Ordinal)); break;
            case "path": helper.SetAttributeValue("Include", "foreign/Hexalith.EventStore.Testing.csproj"); break;
            case "duplicate": helper.Parent!.Add(new XElement(helper)); break;
            case "package": project.Root!.Add(new XElement("ItemGroup", new XElement("PackageReference", new XAttribute("Include", "Hexalith.EventStore.Testing")))); break;
            case "project-update": project.Root!.Add(new XElement("ItemGroup", new XElement("ProjectReference", new XAttribute("Update", "@(ProjectReference)")))); break;
            case "project-remove": project.Root!.Add(new XElement("ItemGroup", new XElement("ProjectReference", new XAttribute("Remove", "@(ProjectReference)")))); break;
            case "package-update": project.Root!.Add(new XElement("ItemGroup", new XElement("PackageReference", new XAttribute("Update", "Hexalith.EventStore.Testing")))); break;
            case "define-condition": symbol.Parent!.Attribute("Condition")!.Remove(); break;
            case "define-token": symbol.Value = "$(DefineConstants);PRIVATE_OWNER_SOURCE_READ_PROOF;UNREVIEWED"; break;
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }

        if (mutation == "none") {
            ValidatePrivateSourceProofEdge(project).ShouldBeSameAs(helper);
            XDocument tests = XDocument.Load(Path.Combine(FindRepoRoot(), "tests/Hexalith.Tenants.Server.Tests/Hexalith.Tenants.Server.Tests.csproj"));
            ValidatePrivateSourceProofTestEdges(tests);
            foreach (string fault in new[] { "missing-source", "missing-package", "source-condition", "package-condition", "duplicate-source", "duplicate-package", "local-version", "define-condition" }) {
                XDocument changed = new(tests);
                XElement source = EventStoreReferences(changed, "ProjectReference").Single(edge => edge.PackageId == "Hexalith.EventStore.Testing").Element;
                XElement package = EventStoreReferences(changed, "PackageReference").Single(edge => edge.PackageId == "Hexalith.EventStore.Testing").Element;
                switch (fault) {
                    case "missing-source": source.Remove(); break;
                    case "missing-package": package.Remove(); break;
                    case "source-condition": source.SetAttributeValue("Condition", "'$(HexalithEventStoreFromSource)' == 'true'"); break;
                    case "package-condition": package.SetAttributeValue("Condition", "'$(HexalithEventStoreFromSource)' != 'true'"); break;
                    case "duplicate-source": source.Parent!.Add(new XElement(source)); break;
                    case "duplicate-package": package.Parent!.Add(new XElement(package)); break;
                    case "local-version": package.SetAttributeValue("Version", "4.1.0"); break;
                    case "define-condition": changed.Descendants("DefineConstants").Single().Parent!.Attribute("Condition")!.Remove(); break;
                }
                Should.Throw<ShouldAssertException>(() => ValidatePrivateSourceProofTestEdges(changed), fault);
            }
        }
        else Should.Throw<ShouldAssertException>(() => ValidatePrivateSourceProofEdge(project));
    }

    [Fact]
    public void AppHost_EventStore_source_edges_preserve_dependency_owned_versions() {
        const string AppHostProjectPath = "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj";

        string repoRoot = FindRepoRoot();
        XDocument appHostProject = XDocument.Load(Path.Combine(repoRoot, AppHostProjectPath));
        List<(string PackageId, XElement Element)> projectEdges =
            EventStoreReferences(appHostProject, "ProjectReference");

        List<string> violations = [];
        HashSet<string> projectIds = [.. projectEdges.Select(edge => edge.PackageId)];
        string[] requiredProjectIds =
        [
            "Hexalith.EventStore.Aspire",
            "Hexalith.EventStore",
            "Hexalith.EventStore.Admin.Server.Host",
            "Hexalith.EventStore.Admin.UI",
        ];

        foreach (string missingProjectId in requiredProjectIds.Except(projectIds, StringComparer.Ordinal)) {
            violations.Add($"{AppHostProjectPath}: required EventStore source edge {missingProjectId} is missing.");
        }

        foreach (IGrouping<string, (string PackageId, XElement Element)> duplicate in projectEdges
            .GroupBy(edge => edge.PackageId, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)) {
            violations.Add($"{AppHostProjectPath}: {duplicate.Key} is referenced as a project {duplicate.Count()} times.");
        }

        foreach ((string packageId, XElement projectReference) in projectEdges) {
            string[] removedGlobalProperties = ItemMetadataValue(projectReference, "GlobalPropertiesToRemove")
                .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (!removedGlobalProperties.Contains("Version", StringComparer.OrdinalIgnoreCase)) {
                violations.Add(
                    $"{AppHostProjectPath}: {packageId} ProjectReference must remove the parent Version global property.");
            }

            if (AdditionalPropertiesExportVersion(ItemMetadataValue(projectReference, "AdditionalProperties"))) {
                violations.Add(
                    $"{AppHostProjectPath}: {packageId} ProjectReference must not export a Version global property.");
            }
        }

        projectEdges.ShouldNotBeEmpty("The AppHost must declare its EventStore source dependencies.");
        violations.ShouldBeEmpty(
            "Cross-repository source builds must let EventStore select its own centrally pinned version without exporting it back into Tenants projects.");
    }

    [Fact]
    public void AppHost_build_tooling_preserves_its_authoritative_catalog_selection() {
        XElement tooling = LoadAppHostProject().Descendants("ProjectReference")
            .Single(item => ((string?)item.Attribute("Include"))?.EndsWith("Hexalith.Builds.Tooling.csproj", StringComparison.Ordinal) == true);
        ItemMetadataValue(tooling, "GlobalPropertiesToRemove")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ShouldContain("HexalithEventStoreVersion");
    }

    [Fact]
    public void Transitive_tooling_keeps_catalog_while_runtime_keeps_explicit_sdk() {
        string fixture = Path.Combine(Path.GetTempPath(), $"tenants-tooling-boundary-{Guid.NewGuid():N}");
        Directory.CreateDirectory(fixture);
        try {
            foreach (string name in new[] { "Hexalith.Builds.Tooling", "Runtime" }) {
                new XDocument(new XElement("Project",
                    new XElement("PropertyGroup", new XElement("HexalithEventStoreVersion",
                        new XAttribute("Condition", "'$(HexalithEventStoreVersion)' == ''"), "3.128.4")),
                    new XElement("Target", new XAttribute("Name", "Record"),
                        new XElement("WriteLinesToFile", new XAttribute("File", Path.Combine(fixture, name + ".txt")),
                            new XAttribute("Lines", "$(HexalithEventStoreVersion)"), new XAttribute("Overwrite", "true")))))
                    .Save(Path.Combine(fixture, name + ".proj"));
            }
            string project = Path.Combine(fixture, "Parent.proj");
            new XDocument(new XElement("Project",
                new XElement("Import", new XAttribute("Project", Path.Combine(FindRepoRoot(), "Directory.Build.targets"))),
                new XElement("Target", new XAttribute("Name", "IncludeTransitiveProjectReferences"),
                    new XElement("ItemGroup", new[] { "Hexalith.Builds.Tooling", "Runtime" }.Select(name =>
                        new XElement("ProjectReference", new XAttribute("Include", Path.Combine(fixture, name + ".proj")))))),
                new XElement("Target", new XAttribute("Name", "Probe"), new XAttribute("DependsOnTargets", "IncludeTransitiveProjectReferences"),
                    new XElement("MSBuild", new XAttribute("Projects", "@(ProjectReference)"), new XAttribute("Targets", "Record"),
                        new XAttribute("RemoveProperties", "%(ProjectReference.GlobalPropertiesToRemove)")))))
                .Save(project);
            var start = new ProcessStartInfo("dotnet") {
                RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
            };
            foreach (string argument in new[] { "msbuild", project, "-nologo", "-t:Probe", "-p:HexalithEventStoreVersion=3.129.0" }) {
                start.ArgumentList.Add(argument);
            }
            using Process process = Process.Start(start)!;
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.ExitCode.ShouldBe(0, output + error);
            File.ReadAllText(Path.Combine(fixture, "Hexalith.Builds.Tooling.txt")).Trim().ShouldBe("3.128.4");
            File.ReadAllText(Path.Combine(fixture, "Runtime.txt")).Trim().ShouldBe("3.129.0");
        }
        finally {
            Directory.Delete(fixture, recursive: true);
        }
    }

    /// <summary>
    /// Verifies the three canonical EventStore child references remain pure composition build edges in every configuration.
    /// </summary>
    [Fact]
    public void AppHost_EventStore_child_projects_are_build_only_for_the_pinned_composition()
    {
        XDocument project = LoadAppHostProject();

        AppHostBuildOnlyEventStoreViolations(project).ShouldBeEmpty();
    }

    /// <summary>
    /// Qualifies rejection of missing, conditional, duplicated or enabled build-edge metadata.
    /// </summary>
    [Theory]
    [InlineData("ReferenceOutputAssembly", "missing")]
    [InlineData("ReferenceOutputAssembly", "true")]
    [InlineData("ReferenceOutputAssembly", "conditional")]
    [InlineData("ReferenceOutputAssembly", "override")]
    [InlineData("ReferenceOutputAssembly", "blank-override")]
    [InlineData("Private", "missing")]
    [InlineData("Private", "true")]
    [InlineData("Private", "conditional")]
    [InlineData("Private", "override")]
    [InlineData("Private", "blank-override")]
    [InlineData("IsAspireProjectResource", "missing")]
    [InlineData("IsAspireProjectResource", "true")]
    [InlineData("IsAspireProjectResource", "conditional")]
    [InlineData("IsAspireProjectResource", "override")]
    [InlineData("IsAspireProjectResource", "blank-override")]
    public void Build_only_governance_rejects_each_child_metadata_regression(string metadataName, string mutation)
    {
        foreach (string packageId in BuildOnlyEventStoreProjectIds())
        {
            XDocument project = LoadAppHostProject();
            AppHostBuildOnlyEventStoreViolations(project).ShouldBeEmpty();
            XElement edge = EventStoreReferences(project, "ProjectReference").Single(edge => edge.PackageId == packageId).Element;
            edge.Attribute(metadataName)?.Remove();
            edge.Elements(metadataName).Remove();

            if (mutation == "true")
            {
                edge.SetAttributeValue(metadataName, "true");
            }
            else if (mutation == "conditional")
            {
                edge.Add(new XElement(metadataName, new XAttribute("Condition", "'$(EnableBuildOnlyMetadata)' == 'true'"), "false"));
            }
            else if (mutation is "override" or "blank-override")
            {
                edge.SetAttributeValue(metadataName, "false");
                edge.Add(new XElement(metadataName, mutation == "override" ? "true" : string.Empty));
            }

            AppHostBuildOnlyEventStoreViolations(project).ShouldContain(
                violation => violation.Contains($"{packageId}: {metadataName}", StringComparison.Ordinal),
                $"The actual governance validator must reject {mutation} {metadataName} on {packageId}.");
        }
    }

    /// <summary>
    /// Qualifies rejection of weakened configuration, source-mode and existence guards.
    /// </summary>
    [Theory]
    [InlineData("Debug")]
    [InlineData("source")]
    [InlineData("Exists")]
    [InlineData("wrong-configuration")]
    [InlineData("wrong-path")]
    public void Build_only_governance_rejects_each_child_condition_regression(string mutation)
    {
        foreach (string packageId in BuildOnlyEventStoreProjectIds())
        {
            XDocument project = LoadAppHostProject();
            AppHostBuildOnlyEventStoreViolations(project).ShouldBeEmpty();
            XElement edge = EventStoreReferences(project, "ProjectReference").Single(edge => edge.PackageId == packageId).Element;
            if (mutation == "Debug")
            {
                edge.Parent!.SetAttributeValue("Condition", "'$(UseHexalithProjectReferences)' == 'true'");
            }
            else if (mutation == "source")
            {
                edge.Parent!.SetAttributeValue("Condition", "'$(Configuration)' == 'Debug'");
            }
            else if (mutation == "wrong-configuration")
            {
                edge.Parent!.SetAttributeValue("Condition", "'$(Configuration)' == 'Release' and '$(UseHexalithProjectReferences)' == 'true'");
            }
            else
            {
                edge.SetAttributeValue("Condition", mutation == "Exists" ? null : "Exists('wrong.csproj')");
            }

            AppHostBuildOnlyEventStoreViolations(project).ShouldContain(
                violation => violation.Contains($"{packageId}: Condition", StringComparison.Ordinal),
                $"The actual governance validator must reject the {mutation} condition regression on {packageId}.");
        }
    }

    /// <summary>
    /// Qualifies rejection of absent or duplicated EventStore child declarations.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Build_only_governance_rejects_missing_or_duplicate_child_edges(bool duplicate)
    {
        foreach (string packageId in BuildOnlyEventStoreProjectIds())
        {
            XDocument project = LoadAppHostProject();
            XElement edge = EventStoreReferences(project, "ProjectReference").Single(edge => edge.PackageId == packageId).Element;
            if (duplicate)
            {
                edge.AddAfterSelf(new XElement(edge));
            }
            else
            {
                edge.Remove();
            }

            AppHostBuildOnlyEventStoreViolations(project).ShouldNotBeEmpty();
        }
    }

    /// <summary>
    /// Rejects source paths that agree with their existence guard but bypass the canonical EventStore root.
    /// </summary>
    [Fact]
    public void Build_only_governance_rejects_self_consistent_wrong_source_paths()
    {
        foreach (string packageId in BuildOnlyEventStoreProjectIds())
        {
            XDocument project = LoadAppHostProject();
            XElement edge = EventStoreReferences(project, "ProjectReference").Single(edge => edge.PackageId == packageId).Element;
            string wrongInclude = $"$(UnrelatedSourceRoot)\\src\\{packageId}\\{packageId}.csproj";
            edge.SetAttributeValue("Include", wrongInclude);
            edge.SetAttributeValue("Condition", $"Exists('{wrongInclude}')");

            AppHostBuildOnlyEventStoreViolations(project).ShouldContain(
                violation => violation.Contains($"{packageId}: Include", StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// Rejects subsequent authored item operations that can change or remove the checked build edges.
    /// </summary>
    [Theory]
    [InlineData("Update", "ReferenceOutputAssembly")]
    [InlineData("Update", "Private")]
    [InlineData("Update", "IsAspireProjectResource")]
    [InlineData("Remove", "")]
    public void Build_only_governance_rejects_later_project_reference_operations(string operation, string metadataName)
    {
        foreach (string packageId in BuildOnlyEventStoreProjectIds())
        {
            XDocument project = LoadAppHostProject();
            XElement item = new("ProjectReference", new XAttribute(operation, BuildOnlyEventStoreInclude(packageId)));
            if (operation == "Update")
            {
                item.SetAttributeValue(metadataName, "true");
            }

            project.Root!.Add(new XElement("ItemGroup", item));
            AppHostBuildOnlyEventStoreViolations(project).ShouldContain(
                violation => violation.Contains($"ProjectReference {operation}", StringComparison.Ordinal));
        }
    }

    private static XDocument LoadAppHostProject()
        => XDocument.Load(Path.Combine(FindRepoRoot(), "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj"));

    private static string[] BuildOnlyEventStoreProjectIds()
        => ["Hexalith.EventStore", "Hexalith.EventStore.Admin.Server.Host", "Hexalith.EventStore.Admin.UI"];

    private static string BuildOnlyEventStoreInclude(string packageId)
        => $"$(HexalithEventStoreRoot)\\src\\{packageId}\\{packageId}.csproj";

    private static string[] AppHostBuildOnlyEventStoreViolations(XDocument project)
    {
        List<string> violations = [];
        foreach (XElement operation in project.Descendants("ProjectReference")
            .Where(item => item.Attribute("Update") is not null || item.Attribute("Remove") is not null))
        {
            violations.Add($"ProjectReference {(operation.Attribute("Update") is not null ? "Update" : "Remove")} operations can override the required build-only declarations.");
        }

        List<(string PackageId, XElement Element)> allEdges = EventStoreReferences(project, "ProjectReference");
        List<(string PackageId, XElement Element)> childEdges = allEdges
            .Where(edge => edge.PackageId != "Hexalith.EventStore.Aspire")
            .ToList();
        string[] expectedIds = BuildOnlyEventStoreProjectIds();
        if (childEdges.Count != expectedIds.Length || childEdges.Any(edge => !expectedIds.Contains(edge.PackageId, StringComparer.Ordinal)))
        {
            violations.Add("The AppHost must contain exactly the three expected EventStore child build edges; the Aspire compile helper is separate.");
        }

        foreach (string packageId in expectedIds)
        {
            XElement[] matches = childEdges.Where(edge => edge.PackageId == packageId).Select(edge => edge.Element).ToArray();
            if (matches.Length != 1)
            {
                violations.Add($"{packageId}: expected one build-only edge, found {matches.Length}.");
                continue;
            }

            XElement edge = matches[0];
            string expectedInclude = BuildOnlyEventStoreInclude(packageId);
            if (!string.Equals((string?)edge.Attribute("Include"), expectedInclude, StringComparison.Ordinal))
            {
                violations.Add($"{packageId}: Include must use the canonical EventStore source-root path {expectedInclude}.");
            }

            string expectedCondition = $"Exists('{expectedInclude}')";
            if (!string.Equals(EffectiveCondition(edge), expectedCondition, StringComparison.Ordinal))
            {
                violations.Add($"{packageId}: Condition must require the canonical pinned executable project to exist.");
            }

            foreach (string metadataName in new[] { "ReferenceOutputAssembly", "Private", "IsAspireProjectResource" })
            {
                int declarations = edge.Attributes().Count(attribute => string.Equals(attribute.Name.LocalName, metadataName, StringComparison.OrdinalIgnoreCase))
                    + edge.Elements().Count(element => string.Equals(element.Name.LocalName, metadataName, StringComparison.OrdinalIgnoreCase));
                bool conditionalMetadata = edge.Elements()
                    .Any(element => string.Equals(element.Name.LocalName, metadataName, StringComparison.OrdinalIgnoreCase) && element.HasAttributes);
                if (declarations != 1 || !string.Equals(ItemMetadataValue(edge, metadataName), "false", StringComparison.Ordinal) || conditionalMetadata)
                {
                    violations.Add($"{packageId}: {metadataName} must be unconditionally false exactly once.");
                }
            }
        }

        return [.. violations];
    }

    [Theory]
    [InlineData("Version")]
    [InlineData("Version=")]
    [InlineData("Version=$(HexalithEventStoreVersion)")]
    [InlineData("TargetFramework=net10.0;Version")]
    [InlineData("VERSION=1.0.0")]
    public void Additional_properties_version_tokens_are_treated_as_version_exports(string additionalProperties) {
        ArgumentNullException.ThrowIfNull(additionalProperties);
        AdditionalPropertiesExportVersion(additionalProperties).ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("TargetFramework=net10.0")]
    [InlineData("VersionSuffix=beta")]
    [InlineData("InformationalVersion=1.0")]
    public void Additional_properties_without_a_version_name_are_not_version_exports(string additionalProperties) {
        ArgumentNullException.ThrowIfNull(additionalProperties);
        AdditionalPropertiesExportVersion(additionalProperties).ShouldBeFalse();
    }

    [Fact]
    public void Item_metadata_reader_combines_attribute_and_child_element_values() {
        XElement item = XElement.Parse(
            """
            <ProjectReference Include="Hexalith.EventStore.csproj" AdditionalProperties="TargetFramework=net10.0" GlobalPropertiesToRemove="TargetFramework">
              <AdditionalProperties>Version</AdditionalProperties>
              <GlobalPropertiesToRemove>Version</GlobalPropertiesToRemove>
            </ProjectReference>
            """);

        ItemMetadataValue(item, "AdditionalProperties").ShouldBe("TargetFramework=net10.0;Version");
        ItemMetadataValue(item, "GlobalPropertiesToRemove").ShouldBe("TargetFramework;Version");
        AdditionalPropertiesExportVersion(ItemMetadataValue(item, "AdditionalProperties")).ShouldBeTrue();
    }

    /// <summary>
    /// Rejects any EventStore <c>ProjectReference</c> that could still be live when the graph
    /// resolves from packages. AC3 forbids an EventStore project edge in Release/package mode
    /// anywhere in the consumer, not only in the domain host.
    /// </summary>
    [Fact]
    public void Only_the_named_EventStore_executables_are_reachable_in_package_mode() {
        const string EventStoreSourceIntent = "'$(HexalithEventStoreFromSource)' == 'true'";
        const string ProjectReferenceIntent = "'$(UseHexalithProjectReferences)' == 'true'";

        string repoRoot = FindRepoRoot();
        List<string> violations = [];
        int inspected = 0;

        foreach (string projectPath in GetOwnedProjectFiles(repoRoot)) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));

            foreach ((string packageId, XElement projectReference) in EventStoreReferences(project, "ProjectReference")) {
                inspected++;

                // The effective condition includes every ancestor ItemGroup condition: the AppHost
                // gates its host references at the ItemGroup rather than the item.
                string condition = EffectiveCondition(projectReference);
                bool compositionExecutable = projectPath == "src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj"
                    && BuildOnlyEventStoreProjectIds().Contains(packageId, StringComparer.Ordinal);
                if (!compositionExecutable && !condition.Contains(EventStoreSourceIntent, StringComparison.Ordinal)
                    && !condition.Contains(ProjectReferenceIntent, StringComparison.Ordinal)) {
                    violations.Add(
                        $"{projectPath}: EventStore ProjectReference {packageId} is not gated on source intent "
                        + $"(effective condition: '{condition}'), so it stays live in package mode.");
                }
            }

            foreach ((string packageId, XElement packageReference) in EventStoreReferences(project, "PackageReference")) {
                if (HasLocalVersionAuthority(packageReference)) {
                    violations.Add(
                        $"{projectPath}: EventStore PackageReference {packageId} declares a local version; "
                        + "the Builds catalog is the only version authority.");
                }
            }
        }

        inspected.ShouldBeGreaterThan(0, "No EventStore ProjectReference was inspected; the rule would pass vacuously.");
        violations.ShouldBeEmpty("Only the three pinned executable composition edges may survive into package mode.");
    }

    [Fact]
    public void Standalone_external_dependencies_have_complementary_source_and_package_edges() {
        (string ProjectPath, string PackageId, string SourceProperty)[] expectedEdges =
        [
            ("src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj", "Hexalith.Commons.Aspire", "HexalithCommonsFromSource"),
            ("src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj", "Hexalith.McpCli.Abstractions", "HexalithMcpCliFromSource"),
            ("src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj", "Hexalith.FrontComposer.Contracts", "HexalithFrontComposerFromSource"),
            ("src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj", "Hexalith.FrontComposer.Shell", "HexalithFrontComposerFromSource"),
            ("tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj", "Hexalith.FrontComposer.Contracts", "HexalithFrontComposerFromSource"),
            ("tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj", "Hexalith.FrontComposer.Shell", "HexalithFrontComposerFromSource"),
            ("tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj", "Hexalith.FrontComposer.Testing", "HexalithFrontComposerFromSource"),
        ];

        string repoRoot = FindRepoRoot();
        List<string> violations = [];
        foreach ((string projectPath, string packageId, string sourceProperty) in expectedEdges) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            XElement[] projectReferences = project.Descendants("ProjectReference")
                .Where(reference => string.Equals(
                    ProjectFileToPackageId((string?)reference.Attribute("Include") ?? string.Empty),
                    packageId,
                    StringComparison.Ordinal))
                .ToArray();
            XElement[] packageReferences = project.Descendants("PackageReference")
                .Where(reference => string.Equals((string?)reference.Attribute("Include"), packageId, StringComparison.Ordinal))
                .ToArray();
            string sourceCondition = $"'$({sourceProperty})' == 'true'";
            string packageCondition = $"'$({sourceProperty})' != 'true'";

            if (projectReferences.Length != 1) {
                violations.Add($"{projectPath}: expected one {packageId} ProjectReference, found {projectReferences.Length}.");
            }
            else if (!string.Equals(EffectiveCondition(projectReferences[0]), sourceCondition, StringComparison.Ordinal)) {
                violations.Add($"{projectPath}: {packageId} ProjectReference must use condition {sourceCondition}.");
            }

            if (packageReferences.Length != 1) {
                violations.Add($"{projectPath}: expected one {packageId} PackageReference, found {packageReferences.Length}.");
            }
            else {
                if (!string.Equals(EffectiveCondition(packageReferences[0]), packageCondition, StringComparison.Ordinal)) {
                    violations.Add($"{projectPath}: {packageId} PackageReference must use condition {packageCondition}.");
                }

                if (HasLocalVersionAuthority(packageReferences[0])) {
                    violations.Add($"{projectPath}: {packageId} PackageReference must use the central catalog version.");
                }
            }
        }

        violations.ShouldBeEmpty("Every standalone-capable external edge must have exclusive source/package routing.");
    }

    [Theory]
    [InlineData(true, null, "false")]
    [InlineData(true, "true", "true")]
    [InlineData(true, "false", "false")]
    [InlineData(false, null, "false")]
    public void FrontComposer_package_routing_is_default_and_source_is_explicitly_overridable(
        bool createSourceGraph,
        string? sourceOverride,
        string expectedFrontComposerRouting) {
        string repoRoot = FindRepoRoot();
        string sourceRoot = Path.Combine(Path.GetTempPath(), $"hexalith-frontcomposer-routing-{Guid.NewGuid():N}");

        try {
            if (createSourceGraph) {
                foreach (string relativeProject in new[] {
                    "src/Hexalith.FrontComposer.Contracts/Hexalith.FrontComposer.Contracts.csproj",
                    "src/Hexalith.FrontComposer.Shell/Hexalith.FrontComposer.Shell.csproj",
                    "src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj",
                }) {
                    string projectPath = Path.Combine(sourceRoot, relativeProject);
                    _ = Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
                    File.WriteAllText(projectPath, "<Project />");
                }
            }

            Dictionary<string, string> properties = EvaluateUiDependencyRouting(
                repoRoot,
                sourceRoot,
                sourceOverride);

            properties["HexalithFrontComposerFromSource"].ShouldBe(expectedFrontComposerRouting);
            properties["HexalithCommonsFromSource"].ShouldNotBe("true");
            properties["HexalithEventStoreFromSource"].ShouldNotBe("true");
            properties["HexalithMemoriesFromSource"].ShouldNotBe("true");
            properties["HexalithMcpCliFromSource"].ShouldNotBe("true");
        }
        finally {
            if (Directory.Exists(sourceRoot)) {
                Directory.Delete(sourceRoot, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("true", "", "", "", true)]
    [InlineData("true", "mapping-hash", "", "", true)]
    [InlineData("true", "", "true", "", true)]
    [InlineData("true", "mapping-hash", "true", "", false)]
    [InlineData("false", "", "", "", false)]
    [InlineData("false", "mapping-hash", "true", "", false)]
    [InlineData("", "", "", "", false)]
    [InlineData("true", "", "", "source", false)]
    [InlineData("true", "mapping-hash", "", "source", false)]
    [InlineData("true", "mapping-hash", "true", "package", false)]
    public void EventStore_source_version_forwarding_defers_only_to_an_active_producer_mapping(
        string fromSource,
        string mappingHash,
        string mappingApplies,
        string mappingMode,
        bool forwardsSourceVersion) {
        string repoRoot = FindRepoRoot();
        string fixtureRoot = Path.Combine(Path.GetTempPath(), $"hexalith-tenants-source-version-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(fixtureRoot);

        try {
            string projectPath = Path.Combine(fixtureRoot, "SourceVersion.proj");
            new XDocument(new XElement("Project",
                new XElement("ItemGroup",
                    new XElement("ProjectReference",
                        new XAttribute("Include", "existing.csproj"),
                        new XAttribute("AdditionalProperties", "ExistingProperty=kept"),
                        new XAttribute("Private", "false")),
                    new XElement("ProjectReference", new XAttribute("Include", "empty.csproj"))),
                new XElement("Import", new XAttribute("Project", Path.Combine(repoRoot, "Directory.Build.targets")))))
                .Save(projectPath);

            var startInfo = new ProcessStartInfo("dotnet") {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string property in new[] {
                "CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets",
                "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH",
            }) {
                _ = startInfo.Environment.Remove(property);
            }
            foreach (string argument in new[] {
                "msbuild", projectPath, "-nologo", "-getItem:ProjectReference",
                $"-property:HexalithEventStoreFromSource={fromSource}",
                "-property:HexalithEventStoreVersion=3.129.1",
                $"-property:HexalithSourceMappingHash={mappingHash}",
                $"-property:HexalithSourceMappingApplies={mappingApplies}",
                $"-property:HexalithSourceMappingMode={mappingMode}",
            }) {
                startInfo.ArgumentList.Add(argument);
            }

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start dotnet msbuild for source version forwarding evaluation.");
            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            process.ExitCode.ShouldBe(0, $"Source version forwarding evaluation failed. stdout: {standardOutput}{Environment.NewLine}stderr: {standardError}");

            using JsonDocument evaluation = JsonDocument.Parse(standardOutput);
            Dictionary<string, JsonElement> references = evaluation.RootElement.GetProperty("Items").GetProperty("ProjectReference")
                .EnumerateArray().ToDictionary(item => item.GetProperty("Identity").GetString()!, StringComparer.Ordinal);
            references.Count.ShouldBe(2);
            string forwarded = forwardsSourceVersion ? ";HexalithEventStoreVersion=3.129.1" : string.Empty;
            references["existing.csproj"].GetProperty("AdditionalProperties").GetString().ShouldBe("ExistingProperty=kept" + forwarded);
            references["existing.csproj"].GetProperty("Private").GetString().ShouldBe("false");
            string emptyProperties = references["empty.csproj"].TryGetProperty("AdditionalProperties", out JsonElement additional)
                ? additional.GetString() ?? string.Empty : string.Empty;
            emptyProperties.ShouldBe(forwarded);
        }
        finally {
            Directory.Delete(fixtureRoot, recursive: true);
        }
    }

    [Theory]
    [InlineData("", false, "5.1.1")]
    [InlineData("4.0.0", false, "4.0.0")]
    [InlineData("4.0.1", false, "4.0.1")]
    [InlineData("4.0.2", false, "4.0.2")]
    [InlineData("4.0.3", false, "4.0.3")]
    [InlineData("4.0.4", false, "4.0.4")]
    [InlineData("4.1.0", false, "4.1.0")]
    [InlineData("4.2.0", false, "4.2.0")]
    [InlineData("5.0.1", false, "5.0.1")]
    [InlineData("5.1.0", false, "5.1.0")]
    [InlineData("5.1.1", false, "5.1.1")]
    [InlineData("3.129.0", false, "3.129.0")]
    [InlineData("3.131.0", false, "3.131.0")]
    [InlineData("3.132.0", false, "3.132.0")]
    [InlineData("3.132.1", false, "3.132.1")]
    [InlineData("3.132.2", false, "3.132.2")]
    [InlineData("3.133.0", false, "3.133.0")]
    [InlineData("3.128.2", false, "3.128.4")]
    [InlineData("3.128.3", false, "3.128.4")]
    [InlineData("3.128.4-preview.1", false, "3.128.4")]
    [InlineData("3.128.4", false, "3.128.4")]
    [InlineData("3.128.4+metadata-with-dash", false, "3.128.4+metadata-with-dash")]
    [InlineData("3.128.4-preview.1+metadata-with-dash", false, "3.128.4")]
    [InlineData("3.128.10", false, "3.128.10")]
    [InlineData("3.128.5", false, "3.128.5")]
    [InlineData("3.129.0-preview.1", false, "3.129.0-preview.1")]
    [InlineData("3.128.2", true, "3.128.2")]
    [InlineData("3.128.5", true, "3.128.5")]
    [InlineData("3.128.4-preview.1", true, "3.128.4-preview.1")]
    [InlineData("test-source-version", true, "test-source-version")]
    public Task EventStore_package_floor_preserves_newer_catalog_and_explicit_source_selectionsAsync(
        string selectedVersion,
        bool useSource,
        string expectedVersion)
        => EvaluateEventStoreVersionSelectionAsync(selectedVersion, useSource, expectedVersion, globalOverride: false, accepted: true);

    [Theory]
    [InlineData("3.128.2", false, false)]
    [InlineData("3.128.3", false, false)]
    [InlineData("3.128.4-preview.1", false, false)]
    [InlineData("3.128.4", false, true)]
    [InlineData("3.128.4+metadata-with-dash", false, true)]
    [InlineData("3.128.4-preview.1+metadata-with-dash", false, false)]
    [InlineData("3.128.10", false, true)]
    [InlineData("3.128.5", false, true)]
    [InlineData("3.129.0-preview.1", false, true)]
    [InlineData("3.128.2", true, true)]
    [InlineData("test-source-version", true, true)]
    public Task EventStore_package_floor_rejects_older_global_package_overridesAsync(
        string selectedVersion,
        bool useSource,
        bool accepted)
        => EvaluateEventStoreVersionSelectionAsync(selectedVersion, useSource, selectedVersion, globalOverride: true, accepted);

    /// <summary>Requires the floor to run through ordinary restore and build entry targets.</summary>
    /// <param name="entryTarget">The normal MSBuild entry target.</param>
    /// <param name="selectedVersion">The global package or exact source selection.</param>
    /// <param name="useSource">Whether the fixture selects the source graph.</param>
    /// <param name="accepted">Whether the selection should pass the floor.</param>
    [Theory]
    [InlineData("CollectPackageReferences", "3.128.3", false, false)]
    [InlineData("PrepareForBuild", "3.128.3", false, false)]
    [InlineData("CollectPackageReferences", "3.128.4-preview.1", false, false)]
    [InlineData("PrepareForBuild", "3.128.4-preview.1", false, false)]
    [InlineData("CollectPackageReferences", "3.128.4+metadata-with-dash", false, true)]
    [InlineData("PrepareForBuild", "3.128.4+metadata-with-dash", false, true)]
    [InlineData("CollectPackageReferences", "4.1.0", false, true)]
    [InlineData("PrepareForBuild", "4.1.0", false, true)]
    [InlineData("CollectPackageReferences", "test-source-version", true, true)]
    [InlineData("PrepareForBuild", "test-source-version", true, true)]
    public Task EventStorePackageFloorRunsThroughRestoreAndBuildAsync(
        string entryTarget,
        string selectedVersion,
        bool useSource,
        bool accepted)
        => EvaluateEventStoreVersionSelectionAsync(selectedVersion, useSource, selectedVersion,
            globalOverride: true, accepted, entryTarget: entryTarget);

    [Fact]
    public Task EventStore_package_floor_does_not_override_nested_owner_catalogsAsync()
        => EvaluateEventStoreVersionSelectionAsync("3.128.4", false, "3.128.4", globalOverride: true, accepted: true, nestedReference: true);
    [Theory]
    [InlineData("3.128.4", false, false)]
    [InlineData("3.129.0-preview.1", false, false)]
    [InlineData("3.129.0", false, true)]
    [InlineData("3.129.0+metadata-with-dash", false, true)]
    [InlineData("3.129.0.0+metadata", false, true)]
    [InlineData("3.129.1", false, true)]
    [InlineData("3.129.1+metadata-with-dash", false, true)]
    [InlineData("3.129.1-preview.1", false, false)]
    [InlineData("3.129.2", false, false)]
    [InlineData("3.130.0", false, true)]
    [InlineData("3.130.0+metadata-with-dash", false, true)]
    [InlineData("3.130.0.0+metadata", false, true)]
    [InlineData("3.130.0-preview.1", false, false)]
    [InlineData("3.130.0.1", false, false)]
    [InlineData("3.130.1", false, true)]
    [InlineData("3.130.1+metadata-with-dash", false, true)]
    [InlineData("3.130.1.0+metadata", false, true)]
    [InlineData("3.130.1-preview.1", false, false)]
    [InlineData("3.130.1-preview.1+metadata", false, false)]
    [InlineData("3.130.1.1", false, false)]
    [InlineData("3.130.2", false, true)]
    [InlineData("3.130.2+metadata-with-dash", false, true)]
    [InlineData("3.130.2.0+metadata", false, true)]
    [InlineData("3.130.2-preview.1", false, false)]
    [InlineData("3.130.2-preview.1+metadata", false, false)]
    [InlineData("3.130.2.1", false, false)]
    [InlineData("3.130.3", false, false)]
    [InlineData("3.131.0", false, true)]
    [InlineData("3.131.0.0", false, true)]
    [InlineData("3.131.0+metadata-with-dash", false, true)]
    [InlineData("3.131.0.0+metadata.1", false, true)]
    [InlineData("3.131.0-preview.1", false, false)]
    [InlineData("3.131.0-preview.1+metadata", false, false)]
    [InlineData("3.131.0.1", false, false)]
    [InlineData("3.131.1", false, false)]
    [InlineData("3.132.0", false, true)]
    [InlineData("3.132.0.0", false, true)]
    [InlineData("3.132.0+metadata-with-dash", false, true)]
    [InlineData("3.132.0.0+metadata.1", false, true)]
    [InlineData("3.132.0-preview.1", false, false)]
    [InlineData("3.132.0-preview.1+metadata", false, false)]
    [InlineData("3.132.0.1", false, false)]
    [InlineData("3.132.0+", false, false)]
    [InlineData("3.132.0+invalid..metadata", false, false)]
    [InlineData("3.132.1", false, true)]
    [InlineData("3.132.1.0", false, true)]
    [InlineData("3.132.1+metadata-with-dash", false, true)]
    [InlineData("3.132.1.0+metadata.1", false, true)]
    [InlineData("3.132.1-preview.1", false, false)]
    [InlineData("3.132.1-preview.1+metadata", false, false)]
    [InlineData("3.132.1.1", false, false)]
    [InlineData("3.132.1+", false, false)]
    [InlineData("3.132.1+invalid..metadata", false, false)]
    [InlineData("3.132.2", false, true)]
    [InlineData("3.132.2.0", false, true)]
    [InlineData("3.132.2+metadata-with-dash", false, true)]
    [InlineData("3.132.2.0+metadata.1", false, true)]
    [InlineData("3.132.2-preview.1", false, false)]
    [InlineData("3.132.2.1", false, false)]
    [InlineData("3.132.3", false, false)]
    [InlineData("3.133.0", false, true)]
    [InlineData("3.133.0.0", false, true)]
    [InlineData("3.133.0+metadata-with-dash", false, true)]
    [InlineData("3.133.0.0+metadata.1", false, true)]
    [InlineData("3.133.0-preview.1", false, false)]
    [InlineData("3.133.0.1", false, false)]
    [InlineData("3.133.1", false, false)]
    [InlineData("4.0.0", false, true)]
    [InlineData("4.0.0+metadata-with-dash", false, true)]
    [InlineData("4.0.0-preview.1", false, false)]
    [InlineData("4.0.1", false, true)]
    [InlineData("4.0.2", false, true)]
    [InlineData("4.0.3", false, true)]
    [InlineData("4.0.4", false, true)]
    [InlineData("4.0.4.0", false, true)]
    [InlineData("4.0.4+metadata-with-dash", false, true)]
    [InlineData("4.0.4-preview.1", false, false)]
    [InlineData("4.0.4.1", false, false)]
    [InlineData("4.0.5", false, false)]
    [InlineData("4.1.0", false, true)]
    [InlineData("4.1.0.0", false, true)]
    [InlineData("4.1.0+metadata-with-dash", false, true)]
    [InlineData("4.1.0.0+metadata.1", false, true)]
    [InlineData("4.1.0-preview.1", false, false)]
    [InlineData("4.1.0.1", false, false)]
    [InlineData("4.1.0+", false, false)]
    [InlineData("4.1.0+invalid..metadata", false, false)]
    [InlineData("4.1.1", false, false)]
    [InlineData("4.2.0", false, true)]
    [InlineData("4.2.0.0", false, true)]
    [InlineData("4.2.0+metadata-with-dash", false, true)]
    [InlineData("4.2.0.0+metadata.1", false, true)]
    [InlineData("4.2.0-preview.1", false, false)]
    [InlineData("4.2.0.1", false, false)]
    [InlineData("4.2.0+", false, false)]
    [InlineData("4.2.0+invalid..metadata", false, false)]
    [InlineData("4.2.1", false, false)]
    [InlineData("4.3.0", false, false)]
    [InlineData("5.0.0", false, false)]
    [InlineData("5.0.1", false, true)]
    [InlineData("5.1.0", false, true)]
    [InlineData("5.0.1.0", false, true)]
    [InlineData("5.1.0.0", false, true)]
    [InlineData("5.0.1+metadata-with-dash", false, true)]
    [InlineData("5.1.0+metadata-with-dash", false, true)]
    [InlineData("5.0.1.0+metadata.1", false, true)]
    [InlineData("5.1.0.0+metadata.1", false, true)]
    [InlineData("5.0.1-preview.1", false, false)]
    [InlineData("5.1.0-preview.1", false, false)]
    [InlineData("5.0.1.1", false, false)]
    [InlineData("5.1.0.1", false, false)]
    [InlineData("5.0.1+", false, false)]
    [InlineData("5.1.0+", false, false)]
    [InlineData("5.0.1+invalid..metadata", false, false)]
    [InlineData("5.1.0+invalid..metadata", false, false)]
    [InlineData("5.1.1", false, true)]
    [InlineData("5.1.1.0", false, true)]
    [InlineData("5.1.1+metadata-with-dash", false, true)]
    [InlineData("5.1.1.0+metadata.1", false, true)]
    [InlineData("5.1.1-preview.1", false, false)]
    [InlineData("5.1.1.1", false, false)]
    [InlineData("5.1.1+", false, false)]
    [InlineData("5.1.1+invalid..metadata", false, false)]
    [InlineData("5.1.2", false, false)]
    [InlineData("5.2.0", false, false)]
    [InlineData("5.0.2", false, false)]
    [InlineData("test-source-version", true, true)]
    public Task Published_packages_require_qualified_catalog_artifactsAsync(
        string selectedVersion,
        bool useSource,
        bool accepted)
        => EvaluateEventStoreVersionSelectionAsync(selectedVersion, useSource, selectedVersion,
            globalOverride: true, accepted, requireCatalogPackage: true);

    [Theory]
    [InlineData("3.128.4", false, true)]
    [InlineData("3.129.0", false, true)]
    [InlineData("3.129.0+metadata-with-dash", false, true)]
    [InlineData("3.129.1", false, true)]
    [InlineData("3.129.1+metadata", false, true)]
    [InlineData("3.129.0-preview.1", false, false)]
    [InlineData("3.129.2", false, false)]
    [InlineData("3.130.0", false, true)]
    [InlineData("3.130.0+metadata-with-dash", false, true)]
    [InlineData("3.130.0.0+metadata", false, true)]
    [InlineData("3.130.0-preview.1", false, false)]
    [InlineData("3.130.0-preview.1+metadata", false, false)]
    [InlineData("3.130.0.1", false, false)]
    [InlineData("3.130.1", false, true)]
    [InlineData("3.130.1+metadata-with-dash", false, true)]
    [InlineData("3.130.1.0+metadata", false, true)]
    [InlineData("3.130.1-preview.1", false, false)]
    [InlineData("3.130.1-preview.1+metadata", false, false)]
    [InlineData("3.130.1.1", false, false)]
    [InlineData("3.130.2", false, true)]
    [InlineData("3.130.2+metadata-with-dash", false, true)]
    [InlineData("3.130.2.0+metadata", false, true)]
    [InlineData("3.130.2-preview.1", false, false)]
    [InlineData("3.130.2-preview.1+metadata", false, false)]
    [InlineData("3.130.2.1", false, false)]
    [InlineData("3.130.3", false, false)]
    [InlineData("3.131.0", false, true)]
    [InlineData("3.131.0.0", false, true)]
    [InlineData("3.131.0+metadata-with-dash", false, true)]
    [InlineData("3.131.0.0+metadata.1", false, true)]
    [InlineData("3.131.0-preview.1", false, false)]
    [InlineData("3.131.0-preview.1+metadata", false, false)]
    [InlineData("3.131.0.1", false, false)]
    [InlineData("3.131.1", false, false)]
    [InlineData("3.132.0", false, true)]
    [InlineData("3.132.0.0", false, true)]
    [InlineData("3.132.0+metadata-with-dash", false, true)]
    [InlineData("3.132.0.0+metadata.1", false, true)]
    [InlineData("3.132.0-preview.1", false, false)]
    [InlineData("3.132.0-preview.1+metadata", false, false)]
    [InlineData("3.132.0.1", false, false)]
    [InlineData("3.132.0+", false, false)]
    [InlineData("3.132.0+invalid..metadata", false, false)]
    [InlineData("3.132.1", false, true)]
    [InlineData("3.132.1.0", false, true)]
    [InlineData("3.132.1+metadata-with-dash", false, true)]
    [InlineData("3.132.1.0+metadata.1", false, true)]
    [InlineData("3.132.1-preview.1", false, false)]
    [InlineData("3.132.1-preview.1+metadata", false, false)]
    [InlineData("3.132.1.1", false, false)]
    [InlineData("3.132.1+", false, false)]
    [InlineData("3.132.1+invalid..metadata", false, false)]
    [InlineData("3.132.2", false, true)]
    [InlineData("3.132.2.0", false, true)]
    [InlineData("3.132.2+metadata-with-dash", false, true)]
    [InlineData("3.132.2.0+metadata.1", false, true)]
    [InlineData("3.132.2-preview.1", false, false)]
    [InlineData("3.132.2.1", false, false)]
    [InlineData("3.132.3", false, false)]
    [InlineData("3.133.0", false, true)]
    [InlineData("3.133.0.0", false, true)]
    [InlineData("3.133.0+metadata-with-dash", false, true)]
    [InlineData("3.133.0.0+metadata.1", false, true)]
    [InlineData("3.133.0-preview.1", false, false)]
    [InlineData("3.133.0.1", false, false)]
    [InlineData("3.133.1", false, false)]
    [InlineData("4.0.0", false, true)]
    [InlineData("4.0.0+metadata-with-dash", false, true)]
    [InlineData("4.0.0-preview.1", false, false)]
    [InlineData("4.0.1", false, true)]
    [InlineData("4.0.2", false, true)]
    [InlineData("4.0.3", false, true)]
    [InlineData("4.0.4", false, true)]
    [InlineData("4.0.4.0", false, true)]
    [InlineData("4.0.4+metadata-with-dash", false, true)]
    [InlineData("4.0.4-preview.1", false, false)]
    [InlineData("4.0.4.1", false, false)]
    [InlineData("4.0.5", false, false)]
    [InlineData("4.1.0", false, true)]
    [InlineData("4.1.0.0", false, true)]
    [InlineData("4.1.0+metadata-with-dash", false, true)]
    [InlineData("4.1.0.0+metadata.1", false, true)]
    [InlineData("4.1.0-preview.1", false, false)]
    [InlineData("4.1.0.1", false, false)]
    [InlineData("4.1.0+", false, false)]
    [InlineData("4.1.0+invalid..metadata", false, false)]
    [InlineData("4.1.1", false, false)]
    [InlineData("4.2.0", false, true)]
    [InlineData("4.2.0.0", false, true)]
    [InlineData("4.2.0+metadata-with-dash", false, true)]
    [InlineData("4.2.0.0+metadata.1", false, true)]
    [InlineData("4.2.0-preview.1", false, false)]
    [InlineData("4.2.0.1", false, false)]
    [InlineData("4.2.0+", false, false)]
    [InlineData("4.2.0+invalid..metadata", false, false)]
    [InlineData("4.2.1", false, false)]
    [InlineData("4.3.0", false, false)]
    [InlineData("5.0.0", false, false)]
    [InlineData("5.0.1", false, true)]
    [InlineData("5.1.0", false, true)]
    [InlineData("5.0.1.0", false, true)]
    [InlineData("5.1.0.0", false, true)]
    [InlineData("5.0.1+metadata-with-dash", false, true)]
    [InlineData("5.1.0+metadata-with-dash", false, true)]
    [InlineData("5.0.1.0+metadata.1", false, true)]
    [InlineData("5.1.0.0+metadata.1", false, true)]
    [InlineData("5.0.1-preview.1", false, false)]
    [InlineData("5.1.0-preview.1", false, false)]
    [InlineData("5.0.1.1", false, false)]
    [InlineData("5.1.0.1", false, false)]
    [InlineData("5.0.1+", false, false)]
    [InlineData("5.1.0+", false, false)]
    [InlineData("5.0.1+invalid..metadata", false, false)]
    [InlineData("5.1.0+invalid..metadata", false, false)]
    [InlineData("5.1.1", false, true)]
    [InlineData("5.1.1.0", false, true)]
    [InlineData("5.1.1+metadata-with-dash", false, true)]
    [InlineData("5.1.1.0+metadata.1", false, true)]
    [InlineData("5.1.1-preview.1", false, false)]
    [InlineData("5.1.1.1", false, false)]
    [InlineData("5.1.1+", false, false)]
    [InlineData("5.1.1+invalid..metadata", false, false)]
    [InlineData("5.1.2", false, false)]
    [InlineData("5.2.0", false, false)]
    [InlineData("5.0.2", false, false)]
    [InlineData("test-source-version", true, true)]
    public Task Native_hosts_refuse_unqualified_catalog_sdk_selectionsAsync(string selectedVersion, bool useSource, bool accepted)
        => EvaluateEventStoreVersionSelectionAsync(selectedVersion, useSource, selectedVersion,
            globalOverride: true, accepted, requireSdkSelection: true);

    private static async Task EvaluateEventStoreVersionSelectionAsync(
        string selectedVersion,
        bool useSource,
        string expectedVersion,
        bool globalOverride,
        bool accepted,
        bool nestedReference = false,
        bool requireCatalogPackage = false,
        bool requireSdkSelection = false,
        string? entryTarget = null) {
        ArgumentNullException.ThrowIfNull(selectedVersion);
        ArgumentNullException.ThrowIfNull(expectedVersion);
        string repoRoot = FindRepoRoot();
        string fixtureRoot = Path.Combine(Path.GetTempPath(), $"hexalith-tenants-eventstore-floor-{Guid.NewGuid():N}");
        string nestedDirectory = Path.Combine(repoRoot, "references", "story33-package-floor-fixture-" + Guid.NewGuid().ToString("N"));
        string contractsDirectory = Path.Combine(fixtureRoot, "src", "Hexalith.EventStore.Contracts");
        _ = Directory.CreateDirectory(contractsDirectory);

        try {
            File.WriteAllText(Path.Combine(contractsDirectory, "Hexalith.EventStore.Contracts.csproj"), "<Project />");
            string projectPath = nestedReference
                ? Path.Combine(nestedDirectory, "External.proj")
                : Path.Combine(fixtureRoot, "PackageFloor.proj");
            if (nestedReference) _ = Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
            new XDocument(new XElement("Project",
                new XElement("PropertyGroup",
                    new XElement("HexalithVersionsLoaded", "true"),
                    new XElement("HexalithEventStoreVersion", selectedVersion),
                    new XElement("HexalithEventStoreRoot", fixtureRoot),
                    new XElement("UseHexalithProjectReferences", useSource ? "true" : "false")),
                new XElement("ItemGroup",
                    new[] { "Contracts", "DomainService", "Gateway" }.Select(package => new XElement("PackageVersion",
                        new XAttribute("Include", $"Hexalith.EventStore.{package}"),
                        new XAttribute("Version", selectedVersion)))),
                new XElement("Import", new XAttribute("Project", Path.Combine(repoRoot, "Directory.Build.props"))),
                new XElement("Import", new XAttribute("Project", Path.Combine(repoRoot, "Directory.Build.targets"))),
                new XElement("Target", new XAttribute("Name", "CollectPackageReferences")),
                new XElement("Target", new XAttribute("Name", "PrepareForBuild"))))
                .Save(projectPath);

            var startInfo = new ProcessStartInfo("dotnet") {
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (string property in new[] {
                "CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps", "CustomBeforeDirectoryBuildTargets",
                "CustomAfterDirectoryBuildTargets", "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH",
            }) {
                _ = startInfo.Environment.Remove(property);
            }
            foreach (string argument in new[] {
                "msbuild", projectPath, "-nologo",
                entryTarget is not null ? $"-target:{entryTarget}" : requireSdkSelection ? "-target:ValidateCatalogSdkSelection"
                    : requireCatalogPackage ? "-target:ValidatePublishedCatalogSdk" : "-target:ValidateEventStorePackageFloor",
                "-getProperty:HexalithEventStoreVersion,HexalithEventStoreFromSource,HexalithTenantsCatalogArtifactsAvailable,HexalithTenantsCatalogSdkVersion,DefineConstants", "-getItem:PackageVersion",
            }) {
                startInfo.ArgumentList.Add(argument);
            }
            if (globalOverride) {
                startInfo.ArgumentList.Add($"-property:HexalithEventStoreVersion={selectedVersion}");
            }

            using Process process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Could not start dotnet msbuild for EventStore version evaluation.");
            Task<string> output = process.StandardOutput.ReadToEndAsync();
            Task<string> error = process.StandardError.ReadToEndAsync();
            try {
                await Task.WhenAll(process.WaitForExitAsync(), output, error).WaitAsync(TimeSpan.FromSeconds(45));
            }
            catch (TimeoutException) {
                try {
                    if (!process.HasExited) {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch (InvalidOperationException) when (process.HasExited) {
                    // The process exited between the deadline and tree cleanup.
                }
                _ = process.WaitForExit(5_000);
                throw new TimeoutException("EventStore version evaluation exceeded its 45-second deadline.");
            }
            string standardOutput = await output;
            string standardError = await error;
            if (!accepted) {
                process.ExitCode.ShouldNotBe(0, "An older global package override must fail before restore or build.");
                (standardOutput + standardError).ShouldContain(requireSdkSelection ? "is not qualified" : requireCatalogPackage
                    ? "Published Tenants packages require the qualified EventStore"
                    : "must be at least 3.128.4 for completion recovery");
                return;
            }
            process.ExitCode.ShouldBe(0, $"EventStore version evaluation failed. stdout: {standardOutput}{Environment.NewLine}stderr: {standardError}");

            using JsonDocument evaluation = JsonDocument.Parse(standardOutput);
            JsonElement properties = evaluation.RootElement.GetProperty("Properties");
            properties.GetProperty("HexalithEventStoreVersion").GetString().ShouldBe(expectedVersion);
            properties.GetProperty("HexalithEventStoreFromSource").GetString().ShouldBe(useSource ? "true" : string.Empty);
            if (requireCatalogPackage || requireSdkSelection || string.IsNullOrEmpty(selectedVersion)) {
                bool catalogAvailable = useSource || !expectedVersion.StartsWith("3.128.", StringComparison.Ordinal);
                properties.GetProperty("HexalithTenantsCatalogArtifactsAvailable").GetString().ShouldBe(catalogAvailable ? "true" : "false");
                string[] constants = properties.GetProperty("DefineConstants").GetString()!.Split(';', StringSplitOptions.RemoveEmptyEntries);
                constants.Contains("HEXALITH_DEVELOPMENT_CATALOG_SOURCE").ShouldBe(catalogAvailable);
                constants.Contains("HEXALITH_DEVELOPMENT_CATALOG_COHORT").ShouldBe(useSource
                    || expectedVersion.StartsWith("4.0.", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.2.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.0.1", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.1", StringComparison.Ordinal));
                constants.Contains("HEXALITH_DEVELOPMENT_CATALOG_SOURCE_REFERENCE").ShouldBe(useSource);
                constants.Contains("HEXALITH_CHECKPOINTED_TENANT_PROJECTION").ShouldBe(useSource
                    || expectedVersion.StartsWith("3.133.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.0.", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.2.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.0.1", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.1", StringComparison.Ordinal));
                constants.Contains("HEXALITH_TECHNICAL_HTTP_CATALOG_BINDING").ShouldBe(useSource
                    || expectedVersion.StartsWith("3.131.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("3.132.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("3.132.1", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("3.132.2", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("3.133.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.0.", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("4.2.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.0.1", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.0", StringComparison.Ordinal)
                    || expectedVersion.StartsWith("5.1.1", StringComparison.Ordinal));
                if (catalogAvailable && !useSource) {
                    properties.GetProperty("HexalithTenantsCatalogSdkVersion").GetString()
                        .ShouldBe(string.Join(".", expectedVersion.Split('+')[0].Split('.').Take(3)));
                }
            }
            JsonElement[] packages = evaluation.RootElement.GetProperty("Items").GetProperty("PackageVersion").EnumerateArray().ToArray();
            packages.Length.ShouldBe(3);
            foreach (JsonElement package in packages) {
                package.GetProperty("Version").GetString().ShouldBe(expectedVersion, package.GetProperty("Identity").GetString());
            }
        }
        finally {
            Directory.Delete(fixtureRoot, recursive: true);
            if (nestedReference && Directory.Exists(nestedDirectory)) Directory.Delete(nestedDirectory, recursive: true);
        }
    }

    /// <summary>
    /// Collects the <c>Hexalith.EventStore*</c> references of the requested item type, keyed by
    /// package id. A <c>ProjectReference</c> id is derived from its project file name so the rule is
    /// independent of path separator and repository layout.
    /// </summary>
    private static List<(string PackageId, XElement Element)> EventStoreReferences(
        XDocument project,
        string elementName) {
        List<(string PackageId, XElement Element)> references = [];

        foreach (XElement element in project.Descendants(elementName)) {
            string include = (string?)element.Attribute("Include") ?? string.Empty;
            if (include.Length == 0) {
                continue;
            }

            string packageId = string.Equals(elementName, "ProjectReference", StringComparison.Ordinal)
                ? ProjectFileToPackageId(include)
                : include;

            if (!packageId.StartsWith("Hexalith.EventStore", StringComparison.Ordinal)) {
                continue;
            }

            references.Add((packageId, element));
        }

        return references;
    }

    private static XElement ValidatePrivateSourceProofEdge(XDocument project) {
        List<XElement> helpers = EventStoreReferences(project, "ProjectReference")
            .Where(edge => edge.PackageId == "Hexalith.EventStore.Testing").Select(edge => edge.Element).ToList();
        helpers.Count.ShouldBe(1, "Exactly one explicit private helper may be excluded from ordinary production SDK edges.");
        XElement helper = helpers.Single();
        ((string?)helper.Attribute("Include")).ShouldBe(PrivateSourceProofProject);
        EffectiveCondition(helper).ShouldBe(PrivateSourceProofCondition, "The helper must be absent in Release, package mode and unselected source builds.");
        helper.Elements().ShouldBeEmpty("The private helper cannot rewrite dependency properties or metadata.");
        EventStoreReferences(project, "PackageReference").ShouldNotContain(edge => edge.PackageId == "Hexalith.EventStore.Testing",
            "Private source proof must not introduce a production Testing package.");
        project.Descendants().Where(element => element.Name.LocalName is "ProjectReference" or "PackageReference")
            .ShouldAllBe(element => element.Attribute("Update") == null && element.Attribute("Remove") == null,
                "Authored reference rewrites cannot widen or replace the finite private helper admission.");
        List<XElement> symbols = project.Descendants("DefineConstants").ToList();
        symbols.Count.ShouldBe(1);
        EffectiveCondition(symbols.Single()).ShouldBe(PrivateSourceProofCondition);
        symbols.Single().Value.ShouldBe("$(DefineConstants);PRIVATE_OWNER_SOURCE_READ_PROOF");
        return helper;
    }

    private static void ValidatePrivateSourceProofTestEdges(XDocument project) {
        List<XElement> sources = EventStoreReferences(project, "ProjectReference")
            .Where(edge => edge.PackageId == "Hexalith.EventStore.Testing").Select(edge => edge.Element).ToList();
        List<XElement> packages = EventStoreReferences(project, "PackageReference")
            .Where(edge => edge.PackageId == "Hexalith.EventStore.Testing").Select(edge => edge.Element).ToList();
        sources.Count.ShouldBe(1);
        packages.Count.ShouldBe(1);
        ((string?)sources.Single().Attribute("Include")).ShouldBe(PrivateSourceProofProject);
        EffectiveCondition(sources.Single()).ShouldBe(PrivateSourceProofCondition);
        EffectiveCondition(packages.Single()).ShouldBe(PrivateSourceProofPackageCondition);
        HasLocalVersionAuthority(packages.Single()).ShouldBeFalse();
        XElement symbol = project.Descendants("DefineConstants").Single();
        EffectiveCondition(symbol).ShouldBe(PrivateSourceProofCondition);
        symbol.Value.ShouldBe("$(DefineConstants);PRIVATE_OWNER_SOURCE_READ_PROOF");
    }

    private static bool AdditionalPropertiesExportVersion(string additionalProperties) {
        ArgumentNullException.ThrowIfNull(additionalProperties);

        foreach (string property in additionalProperties.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            int separator = property.IndexOf('=');
            string name = separator < 0 ? property : property[..separator].Trim();
            if (string.Equals(name, "Version", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }

    private static string ItemMetadataValue(XElement item, string metadataName) {
        IEnumerable<string> attributeValues = item.Attributes()
            .Where(attribute => string.Equals(attribute.Name.LocalName, metadataName, StringComparison.OrdinalIgnoreCase))
            .Select(attribute => attribute.Value);
        IEnumerable<string> elementValues = item.Elements()
            .Where(element => string.Equals(element.Name.LocalName, metadataName, StringComparison.OrdinalIgnoreCase))
            .Select(element => element.Value);

        return string.Join(';', attributeValues.Concat(elementValues).Where(value => !string.IsNullOrWhiteSpace(value)));
    }

    private static string ProjectFileToPackageId(string include) {
        string normalized = include.Replace('\\', '/');
        string fileName = normalized[(normalized.LastIndexOf('/') + 1)..];
        return fileName.EndsWith(".csproj", StringComparison.Ordinal)
            ? fileName[..^".csproj".Length]
            : fileName;
    }

    /// <summary>
    /// Combines the item condition with every ancestor condition, outermost first. Inspecting only
    /// the item attribute would miss a reference moved into a conditional <c>ItemGroup</c>.
    /// </summary>
    private static string EffectiveCondition(XElement element) {
        IEnumerable<string> conditions = element
            .AncestorsAndSelf()
            .Reverse()
            .Select(node => (string?)node.Attribute("Condition"))
            .Where(condition => !string.IsNullOrWhiteSpace(condition))
            .Select(condition => condition!.Trim());

        return string.Join(" and ", conditions);
    }

    /// <summary>
    /// Detects consumer-local version authority supplied either as an attribute or as a child
    /// element; AC3 forbids both.
    /// </summary>
    private static bool HasLocalVersionAuthority(XElement packageReference)
        => packageReference.Attribute("Version") is not null
        || packageReference.Attribute("VersionOverride") is not null
        || packageReference.Elements().Any(child => child.Name.LocalName is "Version" or "VersionOverride");

    private static Dictionary<string, string> EvaluateUiDependencyRouting(
        string repoRoot,
        string frontComposerRoot,
        string? sourceOverride) {
        var startInfo = new ProcessStartInfo("dotnet") {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        startInfo.ArgumentList.Add("msbuild");
        startInfo.ArgumentList.Add("src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj");
        startInfo.ArgumentList.Add("-nologo");
        startInfo.ArgumentList.Add("-getProperty:HexalithFrontComposerFromSource,HexalithCommonsFromSource,HexalithEventStoreFromSource,HexalithMemoriesFromSource,HexalithMcpCliFromSource");
        startInfo.ArgumentList.Add("-property:UseHexalithProjectReferences=false");
        startInfo.ArgumentList.Add($"-property:HexalithFrontComposerRoot={frontComposerRoot}");
        // Isolate this FrontComposer fixture from available Memories source checkouts.
        startInfo.ArgumentList.Add($"-property:HexalithMemoriesRoot={Path.Combine(frontComposerRoot, "unavailable-memories")}");
        if (sourceOverride is not null) {
            startInfo.ArgumentList.Add($"-property:HexalithFrontComposerFromSource={sourceOverride}");
        }

        using Process process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start dotnet msbuild for FrontComposer routing evaluation.");
        string standardOutput = process.StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        process.WaitForExit();
        process.ExitCode.ShouldBe(
            0,
            $"FrontComposer routing evaluation failed. stdout: {standardOutput}{Environment.NewLine}stderr: {standardError}");

        using JsonDocument evaluation = JsonDocument.Parse(standardOutput);
        return evaluation.RootElement.GetProperty("Properties")
            .EnumerateObject()
            .ToDictionary(property => property.Name, property => property.Value.GetString() ?? string.Empty, StringComparer.Ordinal);
    }

    [Fact]
    public void Shared_build_defaults_keep_language_warning_metadata_and_EventStore_governance() {
        string repoRoot = FindRepoRoot();
        XDocument buildProps = XDocument.Load(Path.Combine(repoRoot, "Directory.Build.props"));
        string buildPropsText = File.ReadAllText(Path.Combine(repoRoot, "Directory.Build.props"));

        RequiredValueFor(buildProps, "TargetFramework").ShouldBe("net11.0");
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

        buildProps.Descendants("HexalithEventStoreRoot").Count().ShouldBe(6);
        buildPropsText.ShouldContain("references\\Hexalith.EventStore\\src\\Hexalith.EventStore.Contracts");
        buildPropsText.ShouldNotContain("GenerateDocumentationFile");
        GetForbiddenPackageReferenceViolations(repoRoot, ["StyleCop", "SonarAnalyzer", "Roslynator"])
            .ShouldBeEmpty("Analyzer names may appear in explanatory comments, but Tenants-owned PackageReference nodes must not import analyzer packages.");
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
        testProps.Descendants("PackageReference")
            .Single(reference => reference.Attribute("Include")?.Value == "Microsoft.Testing.Extensions.CodeCoverage")
            .ShouldNotBeNull();
        GetForbiddenPackageReferenceViolations(repoRoot, ["coverlet.collector"])
            .ShouldBeEmpty("MTP coverage must replace the VSTest-only collector in every Tenants-owned test project.");

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
        XDocument uiProject = XDocument.Load(Path.Combine(repoRoot, "src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj"));

        RequiredValueFor(targets, "ContainerBaseImage").ShouldBe("mcr.microsoft.com/dotnet/aspnet:11.0-alpine");
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
        RequiredValueFor(uiProject, "EnableContainer").ShouldBe("true");
        RequiredValueFor(uiProject, "ContainerRepository").ShouldBe("tenants-ui");
        GetProjectContainerDefaultOverrides(repoRoot).ShouldBeEmpty("Container defaults belong in Directory.Build.targets; projects should only opt in with EnableContainer and ContainerRepository.");
        GetAdHocContainerFiles(repoRoot).ShouldBeEmpty("Phase 1 container governance must use .NET SDK publish properties instead of Dockerfiles or compose files.");
    }

    [Fact]
    public async Task Publication_preflight_rejects_release_versions_below_the_published_floor() {
        string repoRoot = FindRepoRoot();
        string bridge = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-publication-preflight.sh"));
        string contributing = File.ReadAllText(Path.Combine(repoRoot, "CONTRIBUTING.md"));

        // The documented static minimum must match the guard, independently of the current
        // reachable release line. A proposal at the minimum still needs the other preflight proofs.
        bridge.ShouldContain("minimum_release_version=4.0.0");
        contributing.ShouldContain("`minimum_release_version=4.0.0`");
        contributing.ShouldContain("**static safety minimum**");

        // Below-floor proposals must fail before any destination probe, with the same authentic
        // producing-commit and reachability policy as the earlier registry/tag-floor guard.
        (int ExitCode, string Error) collided = await RunPublicationPreflightAsync(repoRoot, "3.3.0", "verify");
        collided.ExitCode.ShouldBe(1);
        collided.Error.ShouldContain("below the 4.0.0 release floor");
        string normalizedContributing = Regex.Replace(contributing, @"\s+", " ");
        foreach (string guidance in new string[]
        {
            "Restore a missing release tag only when it is the authentic tag for that published version, "
                + "points to the commit that actually produced it, and is reachable from the dispatched source.",
            "If that provenance or reachability cannot be established, stop and use a "
                + "separately reviewed release-lineage recovery procedure; never create or move a tag merely to clear this guard.",
        })
        {
            collided.Error.ShouldContain(guidance);
            normalizedContributing.ShouldContain(guidance);
        }
        collided.Error.ShouldNotContain("restore the deleted tags");
        collided.Error.ShouldNotContain("BREAKING CHANGE");
        collided.Error.ShouldNotContain("Everything at or below 3.15.1 is published");
        normalizedContributing.ShouldNotContain("restore the deleted tags until semantic-release resumes above the floor");
        normalizedContributing.ShouldNotContain("major bump, so the change needs a `BREAKING CHANGE` footer");

        (int ExitCode, string Error) publishBlocked = await RunPublicationPreflightAsync(repoRoot, "3.3.0", "publish");
        publishBlocked.ExitCode.ShouldBe(1);
        publishBlocked.Error.ShouldBe(collided.Error);

        (int ExitCode, string Error) highestConsumed = await RunPublicationPreflightAsync(repoRoot, "3.15.1", "verify");
        highestConsumed.ExitCode.ShouldBe(1);
        highestConsumed.Error.ShouldContain("below the 4.0.0 release floor");

        // A prerelease below the floor is still below the floor: the core comparison must not become
        // an escape hatch into the consumed range.
        (int ExitCode, string Error) prereleaseBelowFloor = await RunPublicationPreflightAsync(repoRoot, "3.15.1-rc.1", "verify");
        prereleaseBelowFloor.ExitCode.ShouldBe(1);
        prereleaseBelowFloor.Error.ShouldContain("below the 4.0.0 release floor");

        // At and above the floor the bridge falls through to the existing identity checks, proving
        // the floor neither releases anything on its own nor short-circuits the rest of the preflight.
        // Each case asserts the next expected failure, so a version rejected for a new reason cannot
        // pass by merely omitting the floor message.
        foreach (string allowed in new string[] { "4.0.0", "4.1.0", "10.0.0", "4.0.0-rc.1" }) {
            (int ExitCode, string Error) accepted = await RunPublicationPreflightAsync(repoRoot, allowed, "verify");
            accepted.ExitCode.ShouldBe(1, $"{allowed} must reach the identity checks.");
            accepted.Error.ShouldContain("HEXALITH_BUILDS_EXECUTION_SHA must be an exact lowercase commit SHA.", customMessage: allowed);
            accepted.Error.ShouldNotContain("release floor", customMessage: allowed);
        }

        // A malformed version is still rejected first, by the unchanged format check.
        (int ExitCode, string Error) malformed = await RunPublicationPreflightAsync(repoRoot, "x", "verify");
        malformed.ExitCode.ShouldBe(1);
        malformed.Error.ShouldContain("A plain semantic release version is required.");
        malformed.Error.ShouldNotContain("release floor");
    }

    /// <summary>
    /// Gets invalid plain release tags for fallback and invalid-only evidence checks.
    /// </summary>
    public static TheoryData<string> InvalidPlainReleaseTags { get; } = new()
    {
        "v09.0.0",
        "v3.016.0",
        "v3.15.02",
        "v0009.0.0",
        "v3.0016.0",
        "v3.15.0002",
        "v00.0.0",
        "v0.00.0",
        "v0.0.00",
        "v9007199254740992.0.0",
        "v0.9007199254740992.0",
        "v0.0.9007199254740992",
        "v10000000000000000.0.0",
        "v0.10000000000000000.0",
        "v0.0.10000000000000000",
        "v9999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999.0.0",
        "v0.9999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999.0",
        "v0.0.9999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999",
        "v99.0.0+build.1",
        "v99.0.0.0",
    };

    /// <summary>
    /// Gets invalid registry versions that must be rejected before prerelease exclusion.
    /// </summary>
    public static TheoryData<string, bool> InvalidRegistryVersions
    {
        get
        {
            string[] versions =
            [
                "03.15.1", "3.015.1", "3.15.01", "3.15.1.01",
                "00.0.0", "0.00.0", "0.0.00", "0.0.0.00",
                "03.15.1-rc.1", "3.015.1-rc.1", "3.15.01-rc.1", "3.15.1.01-rc.1",
                "2147483648.0.0", "0.2147483648.0", "0.0.2147483648", "0.0.0.2147483648",
                "2147483648.0.0-rc.1", "0.2147483648.0-rc.1", "0.0.2147483648-rc.1", "0.0.0.2147483648-rc.1",
                "9999999999999999999999999999999999999999.0.0",
                "9999999999999999999999999999999999999999.0.0-rc.1",
                "3.15.1-", "3.15.1-.", "3.15.1-.rc", "3.15.1-rc.", "3.15.1-rc..1",
                "3.15.1-01", "3.15.1-00", "3.15.1-rc.01", "3.15.1-01.rc", "3.15.1-rc.00.1",
                "3.15.1-rc_1", "3.15.1-révision", "3.15.1-rc/1",
                "3.15.1+build.1", "3.15.1-rc.1+build.1",
                "3.15.1\n", "3.15.1-rc.1\n", " 3.15.1", "3.15.1 ",
                "3", "3.15.1.0.1", "3.15..1", "v3.15.1", "",
                "4.1.0-" + new string('a', 59), "4.1.0-" + new string('9', 59),
            ];
            TheoryData<string, bool> data = new();
            foreach (string version in versions)
            {
                data.Add(version, false);
                data.Add(version, true);
            }

            return data;
        }
    }

    [Fact]
    public void Release_package_manifest_matches_every_other_copy_of_the_inventory() {
        string repoRoot = FindRepoRoot();
        string manifestPath = Path.Combine(repoRoot, "tools/release-packages.json");
        File.Exists(manifestPath).ShouldBeTrue("tools/release-packages.json is the authoritative release inventory.");

        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
        JsonElement[] packages = [.. manifest.RootElement.GetProperty("packages").EnumerateArray()];

        string[] manifestIds = [.. packages.Select(package => package.GetProperty("id").GetString() ?? string.Empty)];
        string[] manifestProjects = [.. packages.Select(package => package.GetProperty("project").GetString() ?? string.Empty)];

        manifestIds.ShouldBe(ExpectedPackageIds, ignoreOrder: true, "The manifest must enumerate exactly the expected package ids.");
        manifestProjects.ShouldBe(PublishablePackageProjects, ignoreOrder: true, "The manifest must enumerate exactly the publishable package projects.");
        manifestIds.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(manifestIds.Length, "Manifest package ids must be unique.");
        manifestProjects.Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(manifestProjects.Length, "Manifest package projects must be unique.");

        foreach (string project in manifestProjects) {
            File.Exists(Path.Combine(repoRoot, project)).ShouldBeTrue($"Manifest project {project} must exist.");
        }

        // The shipped preflight wrapper retains the same package inventory as the manifest.
        string preflightWrapper = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-publication-preflight.sh"));
        string expectedCount = manifestIds.Length.ToString(CultureInfo.InvariantCulture);
        preflightWrapper.ShouldContain($"expected_package_count={expectedCount}");
    }

    [Fact]
    public void Consumer_package_reference_script_verifies_public_package_surfaces() {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-consumer-package-references.py"));

        script.ShouldContain("Hexalith.Tenants.Contracts");
        script.ShouldContain("Hexalith.Tenants.Client");
        script.ShouldContain("Hexalith.Tenants.Testing");
        GetPythonStringCollectionEntries(script, "PACKAGE_IDS")
            .ToHashSet(StringComparer.Ordinal)
            .SetEquals([
            "Hexalith.Tenants.Contracts",
            "Hexalith.Tenants.Client",
            "Hexalith.Tenants.Server",
            "Hexalith.Tenants.Testing",
        ])
            .ShouldBeTrue("The package-only consumer harness must enumerate exactly the domain consumer package ids.");
        GetPythonStringCollectionEntries(script, "PACKAGE_IDS").ShouldNotContain("Hexalith.Tenants.Aspire");
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
        script.ShouldContain("NUGET_PACKAGES");
        script.ShouldContain("run_xunit_assembly");
        script.ShouldNotContain("[\"test\"");
    }

    [Fact]
    public void NuGet_package_validator_derives_boundaries_from_manifest_and_restore_evidence() {
        string repoRoot = FindRepoRoot();
        string script = File.ReadAllText(Path.Combine(repoRoot, "scripts/validate-nuget-packages.py"));

        script.ShouldContain("DEFAULT_MANIFEST");
        script.ShouldContain("\"release-packages.json\"");
        script.ShouldContain("project.assets.json");
        script.ShouldContain("projectFileDependencyGroups");
        script.ShouldContain("centralTransitiveDependencyGroups");
        script.ShouldContain("FORBIDDEN_DEPENDENCY_IDS");

        // Expected dependency sets are derived, but the forbidden surface stays a hand-declared policy:
        // no published package may ever depend on host, composition, sample, or test projects.
        script.ShouldContain("Hexalith.Tenants.AppHost");
        script.ShouldContain("Hexalith.Tenants.ServiceDefaults");
        script.ShouldContain("Hexalith.Tenants.Sample");
        script.ShouldContain("\".Tests\"");
        script.ShouldContain("\".Samples\"");
        script.Contains("EXPECTED_DEPENDENCIES", StringComparison.Ordinal).ShouldBeFalse();
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
        script.ShouldContain("src/Hexalith.Tenants.Server/Projections/TenantAuditOwnership.cs");
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
            // node_modules is installed tooling (semantic-release et al.), not Tenants-owned container
            // config. It is present during the CI/release npm steps and contains source files whose names
            // start with "Dockerfile" (e.g. highlight.js' lib/languages/dockerfile.js), which would
            // otherwise be misread as ad-hoc container files. Exclude it like bin/ and obj/.
            .Where(path => !path.StartsWith("node_modules/", StringComparison.Ordinal))
            .Where(path => !path.Contains("/node_modules/", StringComparison.Ordinal))
            // Submodules own their own container strategy; this governance is scoped to Tenants files.
            .Where(path => !path.StartsWith("references/Hexalith.EventStore/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.Commons/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.Builds/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.FrontComposer/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.Memories/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.AI.Tools/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.PolymorphicSerializations/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.Platform/", StringComparison.Ordinal))
            .Where(path => !path.StartsWith("references/Hexalith.McpCli/", StringComparison.Ordinal))
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

    private static string[] GetForbiddenPackageReferenceViolations(string repoRoot, string[] forbiddenPackageNameFragments) {
        List<string> violations = [];
        foreach (string projectPath in GetPackageReferenceGovernanceFiles(repoRoot)) {
            XDocument project = XDocument.Load(Path.Combine(repoRoot, projectPath));
            foreach (XElement packageReference in project.Descendants().Where(element => element.Name.LocalName is "PackageReference" or "GlobalPackageReference")) {
                string? packageId = packageReference.Attribute("Include")?.Value ?? packageReference.Attribute("Update")?.Value;
                if (packageId is null) {
                    continue;
                }

                if (forbiddenPackageNameFragments.Any(fragment => packageId.Contains(fragment, StringComparison.OrdinalIgnoreCase))) {
                    violations.Add($"{projectPath}: analyzer package imports are not allowed: {FormatNode(packageReference)}");
                }
            }
        }

        return [.. violations];
    }

    private static HashSet<string> GetCentralPackageIds(string repoRoot) {
        string[] packagePropsFiles =
        [
            Path.Combine(repoRoot, "Directory.Packages.props"),
            Path.Combine(repoRoot, "references", "Hexalith.Builds", "Props", "Directory.Packages.props"),
            Path.Combine(repoRoot, "..", "Hexalith.Builds", "Props", "Directory.Packages.props"),
            Path.Combine(repoRoot, "..", "..", "Hexalith.Builds", "Props", "Directory.Packages.props"),
        ];

        return packagePropsFiles
            .Where(File.Exists)
            .Select(XDocument.Load)
            .SelectMany(document => document.Descendants("PackageVersion"))
            .Select(package => package.Attribute("Include")?.Value)
            .Where(packageId => !string.IsNullOrWhiteSpace(packageId))
            .Select(packageId => packageId!)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string[] GetPythonStringCollectionEntries(string script, string variableName) {
        Match assignment = Regex.Match(script, $@"(?m)^\s*{Regex.Escape(variableName)}\s*=");
        if (!assignment.Success) {
            throw new InvalidOperationException($"Could not find Python assignment {variableName}.");
        }

        int start = IndexOfFirstCollectionOpening(script, assignment.Index + assignment.Length);
        if (start < 0) {
            throw new InvalidOperationException($"Could not find Python collection {variableName}.");
        }

        int end = FindMatchingCollectionEnd(script, start);
        string collectionBody = script[start..(end + 1)];
        return [.. Regex.Matches(collectionBody, "['\"](?<value>(?:\\\\.|[^'\"\\\\])*)['\"]")
            .Select(match => match.Groups["value"].Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))];
    }

    private static async Task<(int ExitCode, string Error)> RunPublicationPreflightAsync(string repoRoot, string version, string phase) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = "bash",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            },
        };
        process.StartInfo.ArgumentList.Add("scripts/validate-publication-preflight.sh");
        process.StartInfo.ArgumentList.Add(version);
        process.StartInfo.ArgumentList.Add(phase);

        // Unset every input the bridge reads so an inherited value cannot change which check reports
        // first, and so the bridge can never reach a real destination probe from a test. Unset rather
        // than blank: the script distinguishes unset from set-but-empty for the package count.
        foreach (string releaseInput in new string[] {
            "HEXALITH_BUILDS_EXECUTION_SHA",
            "GITHUB_SHA",
            "HEXALITH_RELEASE_SOURCE_BRANCH",
            "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW",
            "HEXALITH_RELEASE_PACKAGE_MANIFEST",
            "HEXALITH_RELEASE_ENVIRONMENT",
            "HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT",
            "HEXALITH_RELEASE_CONTRACT_DIRECTORY",
            "HEXALITH_RELEASE_EVIDENCE_DIRECTORY",
            "HEXALITH_PUBLICATION_PREFLIGHT",
            "HEXALITH_ZOT_REGISTRY",
        }) {
            process.StartInfo.Environment.Remove(releaseInput);
        }

        process.Start();
        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        await output;
        return (process.ExitCode, await error);
    }

    private static string ManifestResponse(params string[] packageIds)
        => ManifestResponse("base64", Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new {
            packages = packageIds.Select(packageId => new {
                id = packageId,
                project = $"src/{packageId}/{packageId}.csproj",
            }),
        }))));

    private static string ManifestResponse(string encoding, string content)
        => JsonSerializer.Serialize(new {
            type = "file",
            path = "tools/release-packages.json",
            encoding,
            content,
        });

    private static int IndexOfFirstCollectionOpening(string text, int startIndex) {
        for (int index = startIndex; index < text.Length; index++) {
            if (text[index] is '[' or '{') {
                return index;
            }
        }

        return -1;
    }

    private static int FindMatchingCollectionEnd(string text, int openingIndex) {
        char opening = text[openingIndex];
        char closing = opening == '[' ? ']' : '}';
        int depth = 0;
        char? quotedBy = null;
        bool escaped = false;
        for (int index = openingIndex; index < text.Length; index++) {
            char current = text[index];
            if (quotedBy is not null) {
                if (escaped) {
                    escaped = false;
                }
                else if (current == '\\') {
                    escaped = true;
                }
                else if (current == quotedBy) {
                    quotedBy = null;
                }

                continue;
            }

            if (current is '\'' or '"') {
                quotedBy = current;
                continue;
            }

            if (current == opening) {
                depth++;
            }
            else if (current == closing && --depth == 0) {
                return index;
            }
        }

        throw new InvalidOperationException("Could not find matching Python collection delimiter.");
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
