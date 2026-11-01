using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;

using Shouldly;

namespace Hexalith.Tenants.Contracts.Tests;

public class CiQualityGateScriptTests {
    [Fact]
    public async Task Coverage_gate_script_passes_for_valid_reports_and_writes_summary() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);
        string summaryFile = Path.Combine(temp.Path, "summary.md");

        WriteCoverageReport(
            Path.Combine(coverageRoot, "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "src/Hexalith.Tenants.Contracts/TenantContract.cs",
                    [
                        Line(1, 1),
                        Line(2, 1),
                        Line(3, 1),
                        Line(4, 1),
                        Line(5, 0),
                    ]),
                CoverageClass(
                    "src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs",
                    [
                        Line(10, 1),
                        BranchLine(11, 1, 2, 2),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs --summary-file {Quote(summaryFile)}");

        result.ExitCode.ShouldBe(0, result.Output);
        File.ReadAllText(summaryFile).ShouldContain("Overall line coverage");
        result.Output.ShouldContain("Isolation/auth branch coverage: 100.00%");
    }

    [Fact]
    public async Task Coverage_gate_script_merges_paths_with_and_without_src_prefix() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);

        WriteCoverageReport(
            Path.Combine(coverageRoot, "dependency-closure", "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "src/Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs",
                    [
                        Line(1, 0),
                        Line(2, 0),
                        Line(3, 0),
                        Line(4, 0),
                        Line(5, 0),
                    ]),
            ]);
        WriteCoverageReport(
            Path.Combine(coverageRoot, "project", "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "Hexalith.Tenants.Testing/Fakes/InMemoryTenantService.cs",
                    [
                        Line(1, 1),
                        Line(2, 1),
                        Line(3, 1),
                        Line(4, 1),
                        Line(5, 1),
                    ]),
                CoverageClass(
                    "Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs",
                    [
                        BranchLine(10, 1, 2, 2),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs");

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Overall line coverage (publishable packages): 100.00%");
        result.Output.ShouldContain("Isolation/auth branch coverage: 100.00%");
    }

    [Fact]
    public async Task Coverage_gate_script_fails_when_line_coverage_is_not_above_threshold() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);

        WriteCoverageReport(
            Path.Combine(coverageRoot, "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "src/Hexalith.Tenants.Contracts/TenantContract.cs",
                    [
                        Line(1, 1),
                        Line(2, 1),
                        Line(3, 0),
                        Line(4, 0),
                        Line(5, 0),
                    ]),
                CoverageClass(
                    "samples/Hexalith.Tenants.Sample/Program.cs",
                    [
                        BranchLine(10, 1, 2, 2),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target samples/Hexalith.Tenants.Sample/Program.cs");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Overall line coverage");
        result.Error.ShouldContain("is not greater than 80.00%");
    }

    [Fact]
    public async Task Coverage_gate_script_fails_when_isolation_target_is_missing() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);

        WriteCoverageReport(
            Path.Combine(coverageRoot, "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "src/Hexalith.Tenants.Contracts/TenantContract.cs",
                    [
                        Line(1, 1),
                        Line(2, 1),
                        Line(3, 1),
                        Line(4, 1),
                        Line(5, 1),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Isolation/auth coverage target(s) not found");
    }

    [Fact]
    public async Task Coverage_gate_script_fails_when_no_publishable_package_lines_are_matched() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);

        WriteCoverageReport(
            Path.Combine(coverageRoot, "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/EventStoreContract.cs",
                    [
                        Line(1, 1),
                    ]),
                CoverageClass(
                    "samples/Hexalith.Tenants.Sample/Program.cs",
                    [
                        BranchLine(10, 1, 2, 2),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target samples/Hexalith.Tenants.Sample/Program.cs");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("No publishable-package line coverage data found");
    }

    [Fact]
    public async Task Coverage_gate_script_fails_when_named_target_has_no_branch_data() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        string coverageRoot = Path.Combine(temp.Path, "coverage");
        Directory.CreateDirectory(coverageRoot);

        WriteCoverageReport(
            Path.Combine(coverageRoot, "coverage.cobertura.xml"),
            [
                CoverageClass(
                    "src/Hexalith.Tenants.Contracts/TenantContract.cs",
                    [
                        Line(1, 1),
                        Line(2, 1),
                        Line(3, 1),
                        Line(4, 1),
                        Line(5, 1),
                    ]),
                CoverageClass(
                    "src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs",
                    [
                        Line(10, 1),
                    ]),
            ]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-coverage.py --coverage-root {Quote(coverageRoot)} --minimum-line-coverage 80 --required-branch-coverage 100 --isolation-auth-target src/Hexalith.Tenants.Server/Aggregates/TenantAggregate.cs");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("No isolation/auth branch coverage data found");
    }

    [Fact]
    public async Task PackageValidatorAcceptsIndependentRestoreEvidenceAndIgnoresSymbolsPackages() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(
            temp.Path,
            project,
            "Fixture.Package",
            ["Fixture.Direct"],
            ["Fixture.Transitive"]);

        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["Fixture.Direct", "Fixture.Transitive"]);
        WritePackage(
            packageDirectory,
            "Fixture.Package.symbols",
            "1.2.3",
            includeLicense: true,
            dependencyIds: [],
            fileName: "Fixture.Package.symbols.nupkg");

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Validated 1 NuGet packages at version 1.2.3");
        result.Output.ShouldContain("Fixture.Direct, Fixture.Transitive");
    }

    [Fact]
    public async Task PackageValidatorComparesNuGetIdsCaseInsensitivelyAndReportsCanonicalSpellings() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(
            temp.Path,
            project,
            "Fixture.Package",
            ["Fixture.Direct"],
            ["Fixture.Transitive"]);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "fixture.package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["fixture.direct", "fixture.transitive"]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(0, result.Error);
        result.Output.ShouldContain("- Fixture.Package dependencies: Fixture.Direct, Fixture.Transitive");
        result.Output.Contains("- fixture.package dependencies:", StringComparison.Ordinal).ShouldBeFalse();
    }

    [Fact]
    public async Task PackageValidatorFailsBeforePublishWhenRequiredMetadataIsMissing() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(temp.Path, project, "Fixture.Package", [], []);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: false,
            dependencyIds: []);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Fixture.Package.1.2.3.nupkg: missing license metadata");
    }

    [Fact]
    public async Task PackageValidatorRejectsUnexpectedDependencyAbsentFromRestoreEvidence() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(temp.Path, project, "Fixture.Package", ["Fixture.Direct"], []);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["Fixture.Direct", "Fixture.Unexpected"]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("unexpected: ['Fixture.Unexpected']");
    }

    [Fact]
    public async Task PackageValidatorRejectsDependencyMissingFromPackageMetadata() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(
            temp.Path,
            project,
            "Fixture.Package",
            ["Fixture.Direct"],
            ["Fixture.Transitive"]);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["Fixture.Direct"]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Missing: ['Fixture.Transitive']");
    }

    [Fact]
    public async Task PackageValidatorRejectsMissingRestoreEvidenceBeforeInspectingPackages() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(Path.Combine(temp.Path, "missing-packages"))} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Restore assets are missing for Fixture.Package");
        result.Error.ShouldNotContain("Expected 1 packages");
    }

    [Fact]
    public async Task PackageValidatorRejectsMalformedRestoreEvidence() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        string assetsPath = Path.Combine(temp.Path, Path.GetDirectoryName(project)!, "obj", "project.assets.json");
        Directory.CreateDirectory(Path.GetDirectoryName(assetsPath)!);
        File.WriteAllText(assetsPath, "{");

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(Path.Combine(temp.Path, "missing-packages"))} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Restore assets");
        result.Error.ShouldContain("is unusable");
    }

    [Fact]
    public async Task PackageValidatorRejectsRestoreEvidenceAttributedToAnotherPackage() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(temp.Path, project, "Other.Package", [], []);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: []);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("identify package 'Other.Package', expected 'Fixture.Package'");
    }

    [Fact]
    public async Task PackageValidatorRejectsForbiddenDependencyEvenWhenRestoreEvidenceIncludesIt() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Fixture.Package", project);
        WriteRestoreEvidence(temp.Path, project, "Fixture.Package", ["hexalith.tenants.apphost"], []);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Fixture.Package",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["hexalith.tenants.apphost"]);

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("forbidden projects");
        result.Error.ShouldContain("hexalith.tenants.apphost");
    }

    [Theory]
    [InlineData("Hexalith.EventStore.ServiceDefaults", true)]
    [InlineData("hexalith.eventstore.servicedefaults", true)]
    [InlineData("Hexalith.Commons.ServiceDefaults", true)]
    [InlineData("HEXALITH.COMMONS.SERVICEDEFAULTS", true)]
    [InlineData("Other.Module.ServiceDefaults", false)]
    [InlineData("Hexalith.Tenants.ServiceDefaults", false)]
    [InlineData("Hexalith.EventStore.ServiceDefaults.Extensions", false)]
    [InlineData("Evil.Hexalith.Commons.ServiceDefaults", false)]
    [InlineData("evil.hexalith.eventstore.servicedefaults", false)]
    public async Task PackageValidatorAllowsOnlyExactSharedServiceDefaultsDependencies(
        string dependencyId, bool allowed) {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Fixture.Package/Fixture.Package.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Hexalith.Tenants.Server", project);
        WriteRestoreEvidence(temp.Path, project, "Hexalith.Tenants.Server", [dependencyId], []);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(packageDirectory, "Hexalith.Tenants.Server", "1.2.3", includeLicense: true, dependencyIds: [dependencyId]);

        CommandResult result = await RunAsync(repoRoot, "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(allowed ? 0 : 1, result.Output + result.Error);
        if (allowed) {
            result.Output.ShouldContain($"- Hexalith.Tenants.Server dependencies: {dependencyId}");
        }
        else {
            result.Error.ShouldContain("forbidden projects");
            result.Error.ShouldContain(dependencyId);
        }
    }

    [Fact]
    public async Task PackageValidatorAcceptsTheMcpCliDecorationPackageDependencyOfContracts() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();
        const string project = "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj";
        string manifest = WriteReleaseManifest(temp.Path, "Hexalith.Tenants.Contracts", project);
        WriteRestoreEvidence(
            temp.Path,
            project,
            "Hexalith.Tenants.Contracts",
            ["Hexalith.EventStore.Contracts", "Hexalith.McpCli.Abstractions"],
            ["Hexalith.Commons.UniqueIds"]);
        string packageDirectory = Path.Combine(temp.Path, "packages");
        WritePackage(
            packageDirectory,
            "Hexalith.Tenants.Contracts",
            "1.2.3",
            includeLicense: true,
            dependencyIds: ["Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts", "Hexalith.McpCli.Abstractions"]);

        CommandResult result = await RunAsync(repoRoot, "python3",
            $"scripts/validate-nuget-packages.py {Quote(packageDirectory)} --manifest {Quote(manifest)}");

        result.ExitCode.ShouldBe(0, result.Output + result.Error);
        result.Output.ShouldContain(
            "- Hexalith.Tenants.Contracts dependencies: Hexalith.Commons.UniqueIds, Hexalith.EventStore.Contracts, Hexalith.McpCli.Abstractions");
    }

    /// <summary>
    /// Qualifies restore-backed version ranges and framework groups through the package validator.
    /// </summary>
    [Fact]
    public async Task PackageValidatorQualifiesDependencyRangesAndFrameworkBoundaries()
    {
        CommandResult result = await RunAsync(FindRepoRoot(), "python3", "tests/scripts/test_nuget_dependency_ranges.py");
        result.ExitCode.ShouldBe(0, result.Output + result.Error);
        result.Error.ShouldContain("OK");
    }

    /// <summary>
    /// Frozen topology jobs bind both host source and SDK without changing normal package builds.
    /// </summary>
    [Fact]
    public async Task FrozenBootstrapJobsBindTheCompletePublishedSdkCohort()
    {
        CommandResult result = await RunAsync(FindRepoRoot(), "python3", "tests/scripts/test_bootstrap_compatibility.py");
        result.ExitCode.ShouldBe(0, result.Output + result.Error);
        var count = System.Text.RegularExpressions.Regex.Match(result.Error, @"Ran ([0-9]+) tests");
        int.Parse(count.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture).ShouldBeGreaterThanOrEqualTo(12);
        result.Error.ShouldContain("OK");
    }

    /// <summary>
    /// The generic Aspire lane has explicit prerequisites and cannot hide failures or skips.
    /// </summary>
    [Fact]
    public void AspireLaneRequiresBlockingBrowserAndSkipInputs()
    {
        string root = FindRepoRoot();
        string caller = File.ReadAllText(Path.Combine(root, ".github/workflows/ci.yml"));
        foreach (string declaration in new[]
        {
            "aspire-continue-on-error: false", "aspire-fail-skips: true",
            "aspire-browser-project: tests/performance/tenant-audit",
        })
        {
            caller.ShouldContain(declaration);
        }

        string shared = File.ReadAllText(Path.Combine(root, "references/Hexalith.Builds/.github/workflows/domain-ci.yml"));
        shared.ShouldContain("if: ${{ inputs.aspire-browser-project != '' }}");
        shared.ShouldContain("npx --no-install playwright install --with-deps chromium");
        shared.ShouldContain("dotnet dev-certs https --trust");
        shared.ShouldContain("--minimum-expected-tests 1 --fail-skips on");
        shared.ShouldContain("inputs.aspire-test-project != '' && github.event_name == 'schedule'");
        shared.ShouldContain("--filter-not-trait \"$ASPIRE_MTP_EXCLUDED_TRAIT\"");
    }

    /// <summary>
    /// Executing the actual result gate rejects skipped, failed and empty qualification runs.
    /// </summary>
    /// <param name="executed">The number of executed tests.</param>
    /// <param name="skipped">The number of skipped tests.</param>
    /// <param name="failed">The number of failed tests.</param>
    /// <param name="accepted">Whether this result can qualify CI.</param>
    [Theory]
    [InlineData(1, 0, 0, true)]
    [InlineData(1, 1, 0, false)]
    [InlineData(1, 0, 1, false)]
    [InlineData(0, 0, 0, false)]
    public async Task AspireResultGateRequiresPassingExecutedTests(int executed, int skipped, int failed, bool accepted)
    {
        string root = FindRepoRoot();
        string shared = File.ReadAllText(Path.Combine(root, "references/Hexalith.Builds/.github/workflows/domain-ci.yml"));
        string script = shared.Split("python3 - <<'PYTEST'", StringSplitOptions.None)[1]
            .Split("          PYTEST", StringSplitOptions.None)[0];
        script = string.Join('\n', script.Split('\n').Skip(1).Select(line => line.Length >= 10 ? line[10..] : line));
        using TemporaryDirectory temp = new();
        Directory.CreateDirectory(Path.Combine(temp.Path, "TestResults/aspire"));
        string program = Path.Combine(temp.Path, "contract.py");
        await File.WriteAllTextAsync(program, script);
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "TestResults/aspire/results.trx"),
            $"<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results><UnitTestResult outcome=\"Passed\" /></Results><ResultSummary outcome=\"Completed\"><Counters total=\"{executed}\" executed=\"{executed}\" passed=\"{executed}\" notExecuted=\"{skipped}\" failed=\"{failed}\" /></ResultSummary></TestRun>");
        CommandResult result = await RunAsync(temp.Path, "python3", Quote(program));
        (result.ExitCode == 0).ShouldBe(accepted, result.Output + result.Error);
    }

    /// <summary>
    /// A positive execution count cannot hide aborted, erroneous or malformed result summaries.
    /// </summary>
    /// <param name="mutation">The report defect to refuse.</param>
    [Theory]
    [InlineData("aborted-summary")]
    [InlineData("failed-summary")]
    [InlineData("error")]
    [InlineData("aborted")]
    [InlineData("timeout")]
    [InlineData("passedButRunAborted")]
    [InlineData("missing-results")]
    [InlineData("missing-passed")]
    [InlineData("missing-failed")]
    [InlineData("malformed")]
    [InlineData("contradictory-total")]
    [InlineData("wrong-root")]
    public async Task AspireResultGateRejectsNonpassingSummaries(string mutation)
    {
        string shared = File.ReadAllText(Path.Combine(FindRepoRoot(), "references/Hexalith.Builds/.github/workflows/domain-ci.yml"));
        string script = shared.Split("python3 - <<'PYTEST'", StringSplitOptions.None)[1]
            .Split("          PYTEST", StringSplitOptions.None)[0];
        script = string.Join('\n', script.Split('\n').Skip(1).Select(line => line.Length >= 10 ? line[10..] : line));
        using TemporaryDirectory temp = new();
        Directory.CreateDirectory(Path.Combine(temp.Path, "TestResults/aspire"));
        string program = Path.Combine(temp.Path, "contract.py");
        await File.WriteAllTextAsync(program, script);
        string report = "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results><UnitTestResult outcome=\"Passed\" /></Results><ResultSummary outcome=\"Completed\"><Counters total=\"1\" executed=\"1\" passed=\"1\" failed=\"0\" /></ResultSummary></TestRun>";
        report = mutation switch
        {
            "aborted-summary" => report.Replace("Completed", "Aborted", StringComparison.Ordinal),
            "failed-summary" => report.Replace("Completed", "Failed", StringComparison.Ordinal),
            "missing-results" => report.Replace("<UnitTestResult outcome=\"Passed\" />", string.Empty, StringComparison.Ordinal),
            "missing-failed" => report.Replace(" failed=\"0\"", string.Empty, StringComparison.Ordinal),
            "missing-passed" => report.Replace(" passed=\"1\"", string.Empty, StringComparison.Ordinal),
            "malformed" => "<TestRun>",
            "contradictory-total" => report.Replace("total=\"1\"", "total=\"2\"", StringComparison.Ordinal),
            "wrong-root" => report.Replace("TestRun", "Other", StringComparison.Ordinal),
            _ => report.Replace("failed=\"0\"", $"failed=\"0\" {mutation}=\"1\"", StringComparison.Ordinal),
        };
        await File.WriteAllTextAsync(Path.Combine(temp.Path, "TestResults/aspire/results.trx"), report);
        CommandResult result = await RunAsync(temp.Path, "python3", Quote(program));
        result.ExitCode.ShouldNotBe(0, result.Output + result.Error);
    }

    private static string CoverageClass(string filename, string[] lines)
        => $"""
                    <class name="{Path.GetFileNameWithoutExtension(filename)}" filename="{filename}" line-rate="1" branch-rate="1">
                      <lines>
                {string.Join(Environment.NewLine, lines)}
                      </lines>
                    </class>
            """;

    private static string Line(int number, int hits)
        => $"""        <line number="{number}" hits="{hits}" branch="False" />""";

    private static string BranchLine(int number, int hits, int covered, int total)
        => $"""        <line number="{number}" hits="{hits}" branch="True" condition-coverage="{covered * 100 / total}% ({covered}/{total})" />""";

    private static void WriteCoverageReport(string path, string[] classes) {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(
            path,
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <coverage line-rate="1" branch-rate="1">
              <packages>
                <package name="Hexalith.Tenants">
                  <classes>
            {string.Join(Environment.NewLine, classes)}
                  </classes>
                </package>
              </packages>
            </coverage>
            """);
    }

    private static string WriteReleaseManifest(string projectRoot, string packageId, string projectRelativePath) {
        string projectPath = Path.Combine(projectRoot, projectRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(projectPath)!);
        File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk\" />");

        string manifestPath = Path.Combine(projectRoot, "release-packages.json");
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(
                new {
                    packages = new[] {
                        new {
                            id = packageId,
                            project = projectRelativePath,
                        },
                    },
                }));
        return manifestPath;
    }

    private static void WriteRestoreEvidence(
        string projectRoot,
        string projectRelativePath,
        string packageId,
        string[] directDependencyIds,
        string[] transitiveDependencyIds) {
        string projectPath = Path.GetFullPath(Path.Combine(projectRoot, projectRelativePath));
        string assetsPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "obj", "project.assets.json");
        Directory.CreateDirectory(Path.GetDirectoryName(assetsPath)!);

        Dictionary<string, object> transitiveDependencies = new(StringComparer.Ordinal);
        foreach (string dependencyId in transitiveDependencyIds) {
            transitiveDependencies.Add(dependencyId, new { version = "[1.0.0, )" });
        }

        object evidence = new {
            version = 4,
            projectFileDependencyGroups = new Dictionary<string, string[]>(StringComparer.Ordinal) {
                ["net10.0"] = Array.ConvertAll(directDependencyIds, id => $"{id} >= 1.0.0"),
            },
            centralTransitiveDependencyGroups = new Dictionary<string, Dictionary<string, object>>(StringComparer.Ordinal) {
                ["net10.0"] = transitiveDependencies,
            },
            project = new {
                restore = new {
                    projectPath,
                    projectName = packageId,
                    projectStyle = "PackageReference",
                    originalTargetFrameworks = new[] { "net10.0" },
                },
                frameworks = new Dictionary<string, object>(StringComparer.Ordinal) {
                    ["net10.0"] = new { framework = "net10.0" },
                },
            },
        };
        File.WriteAllText(assetsPath, JsonSerializer.Serialize(evidence));
    }

    private static void WritePackage(
        string packageDirectory,
        string packageId,
        string version,
        bool includeLicense,
        string[] dependencyIds,
        string? fileName = null) {
        Directory.CreateDirectory(packageDirectory);
        string path = Path.Combine(packageDirectory, fileName ?? $"{packageId}.{version}.nupkg");
        using ZipArchive package = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteZipEntry(package, $"{packageId}.nuspec", Nuspec(packageId, version, includeLicense, dependencyIds));
        WriteZipEntry(package, "README.md", $"# {packageId}");
    }

    private static string Nuspec(string packageId, string version, bool includeLicense, string[] dependencyIds) {
        string dependencies = dependencyIds.Length > 0
            ? "<dependencies><group targetFramework=\"net10.0\">"
                + string.Concat(Array.ConvertAll(dependencyIds, id => $"<dependency id=\"{id}\" version=\"1.0.0\" />"))
                + "</group></dependencies>"
            : string.Empty;

        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{packageId}</id>
                <version>{version}</version>
                <authors>Hexalith Contributors</authors>
                <readme>README.md</readme>
                {(includeLicense ? "<license type=\"expression\">MIT</license>" : string.Empty)}
                {dependencies}
              </metadata>
            </package>
            """;
    }

    private static void WriteZipEntry(ZipArchive package, string entryName, string content) {
        ZipArchiveEntry entry = package.CreateEntry(entryName);
        using StreamWriter writer = new(entry.Open());
        writer.Write(content);
    }

    private static async Task<CommandResult> RunAsync(string workingDirectory, string executable, string arguments) {
        using Process process = new() {
            StartInfo = new ProcessStartInfo {
                FileName = executable,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
        };

        process.Start();
        string output = await process.StandardOutput.ReadToEndAsync();
        string error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return new CommandResult(process.ExitCode, output, error);
    }

    private static string Quote(string value)
        => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";

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

    private sealed record CommandResult(int ExitCode, string Output, string Error);

    private sealed class TemporaryDirectory : IDisposable {
        public TemporaryDirectory() {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"tenants-ci-gates-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() {
            if (Directory.Exists(Path)) {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
