using System.Diagnostics;
using System.IO.Compression;

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
    public async Task Package_validator_accepts_exact_packages_and_ignores_symbols_packages() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();

        foreach (string packageId in ExpectedPackageIds) {
            WritePackage(temp.Path, packageId, "1.2.3", includeLicense: true);
            WritePackage(temp.Path, $"{packageId}.symbols", "1.2.3", includeLicense: true, fileName: $"{packageId}.symbols.nupkg");
        }

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(temp.Path)}");

        result.ExitCode.ShouldBe(0, result.Output);
        result.Output.ShouldContain("Validated 5 NuGet packages at version 1.2.3");
    }

    [Fact]
    public async Task Package_validator_fails_before_publish_when_required_metadata_is_missing() {
        string repoRoot = FindRepoRoot();
        using TemporaryDirectory temp = new();

        foreach (string packageId in ExpectedPackageIds) {
            WritePackage(temp.Path, packageId, "1.2.3", includeLicense: packageId != "Hexalith.Tenants.Server");
        }

        CommandResult result = await RunAsync(
            repoRoot,
            "python3",
            $"scripts/validate-nuget-packages.py {Quote(temp.Path)}");

        result.ExitCode.ShouldBe(1, result.Output);
        result.Error.ShouldContain("Hexalith.Tenants.Server.1.2.3.nupkg: missing license metadata");
    }

    private static readonly string[] ExpectedPackageIds =
    [
        "Hexalith.Tenants.Contracts",
        "Hexalith.Tenants.Client",
        "Hexalith.Tenants.Server",
        "Hexalith.Tenants.Testing",
        "Hexalith.Tenants.Aspire",
    ];

    // Mirrors EXPECTED_DEPENDENCIES in scripts/validate-nuget-packages.py so synthetic fixtures satisfy the
    // dependency-boundary validation added in Story 1.4 and keep isolating license/symbol/version behavior.
    private static readonly Dictionary<string, string[]> ExpectedDependencies = new(StringComparer.Ordinal) {
        ["Hexalith.Tenants.Contracts"] = ["ByteAether.Ulid", "Hexalith.Commons.UniqueIds", "Hexalith.EventStore.Contracts"],
        ["Hexalith.Tenants.Client"] =
        [
            "ByteAether.Ulid",
            "Dapr.AspNetCore",
            "Dapr.Client",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Client",
            "Hexalith.EventStore.Contracts",
            "Hexalith.Tenants.Contracts",
        ],
        ["Hexalith.Tenants.Server"] =
        [
            "ByteAether.Ulid",
            "Dapr.Actors",
            "Dapr.Actors.AspNetCore",
            "Dapr.Client",
            "FluentValidation",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Client",
            "Hexalith.EventStore.Contracts",
            "Hexalith.EventStore.Server",
            "Hexalith.Tenants.Contracts",
            "MediatR",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Microsoft.IdentityModel.Abstractions",
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.IdentityModel.Logging",
            "Microsoft.IdentityModel.Tokens",
        ],
        ["Hexalith.Tenants.Testing"] =
        [
            "ByteAether.Ulid",
            "Dapr.Actors",
            "Dapr.Actors.AspNetCore",
            "Dapr.Client",
            "FluentValidation",
            "Hexalith.Commons.UniqueIds",
            "Hexalith.EventStore.Client",
            "Hexalith.EventStore.Contracts",
            "Hexalith.EventStore.Server",
            "Hexalith.Tenants.Contracts",
            "Hexalith.Tenants.Server",
            "MediatR",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Microsoft.IdentityModel.Abstractions",
            "Microsoft.IdentityModel.JsonWebTokens",
            "Microsoft.IdentityModel.Logging",
            "Microsoft.IdentityModel.Tokens",
            "Shouldly",
            "xunit.v3.assert",
        ],
        ["Hexalith.Tenants.Aspire"] =
        [
            "Aspire.Hosting",
            "Aspire.Hosting.Keycloak",
            "Aspire.Hosting.Redis",
            "CommunityToolkit.Aspire.Hosting.Dapr",
            "Grpc.Net.ClientFactory",
            "Hexalith.EventStore.Aspire",
            "MessagePack",
            "Microsoft.Extensions.Configuration",
            "Microsoft.Extensions.Configuration.Abstractions",
            "Microsoft.Extensions.Configuration.Binder",
            "Microsoft.Extensions.Configuration.FileExtensions",
            "Microsoft.Extensions.Configuration.UserSecrets",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.Extensions.DependencyInjection.Abstractions",
            "Microsoft.Extensions.Hosting",
            "Microsoft.Extensions.Hosting.Abstractions",
            "Microsoft.Extensions.Http",
            "Microsoft.Extensions.Logging.Abstractions",
            "Microsoft.Extensions.Options",
            "Microsoft.Extensions.Options.ConfigurationExtensions",
            "Newtonsoft.Json",
            "OpenTelemetry.Exporter.OpenTelemetryProtocol",
            "OpenTelemetry.Extensions.Hosting",
            "StackExchange.Redis",
            "YamlDotNet",
        ],
    };

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

    private static void WritePackage(
        string packageDirectory,
        string packageId,
        string version,
        bool includeLicense,
        string? fileName = null) {
        string path = Path.Combine(packageDirectory, fileName ?? $"{packageId}.{version}.nupkg");
        using ZipArchive package = ZipFile.Open(path, ZipArchiveMode.Create);
        WriteZipEntry(package, $"{packageId}.nuspec", Nuspec(packageId, version, includeLicense));
        WriteZipEntry(package, "README.md", $"# {packageId}");
    }

    private static string Nuspec(string packageId, string version, bool includeLicense) {
        string dependencies = ExpectedDependencies.TryGetValue(packageId, out string[]? dependencyIds) && dependencyIds.Length > 0
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
