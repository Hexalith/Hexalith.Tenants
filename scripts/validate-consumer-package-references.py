#!/usr/bin/env python3
"""Build isolated consumers against local Hexalith.Tenants NuGet packages."""

from __future__ import annotations

import argparse
import os
import shutil
import subprocess
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree


PACKAGE_IDS = [
    "Hexalith.Tenants.Contracts",
    "Hexalith.Tenants.Client",
    "Hexalith.Tenants.Server",
    "Hexalith.Tenants.Testing",
]
SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]


def central_package_versions() -> dict[str, str]:
    package_props_files = [
        REPO_ROOT / "Directory.Packages.props",
        REPO_ROOT / "references" / "Hexalith.Builds" / "Props" / "Directory.Packages.props",
        REPO_ROOT.parent / "Hexalith.Builds" / "Props" / "Directory.Packages.props",
        REPO_ROOT.parent.parent / "Hexalith.Builds" / "Props" / "Directory.Packages.props",
    ]
    versions: dict[str, str] = {}
    for props_file in package_props_files:
        if not props_file.exists():
            continue

        document = ElementTree.parse(props_file)
        versions.update(
            {
                element.attrib["Include"]: element.attrib["Version"]
                for element in document.getroot().iter()
                if element.tag.endswith("PackageVersion") and "Include" in element.attrib and "Version" in element.attrib
            }
        )

    return versions


def package_versions(package_directory: Path) -> dict[str, str]:
    versions: dict[str, str] = {}
    for package_path in package_directory.glob("*.nupkg"):
        if ".symbols." in package_path.name or package_path.name.endswith(".snupkg"):
            continue

        with zipfile.ZipFile(package_path) as package:
            nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
            if len(nuspec_names) != 1:
                raise ValueError(f"{package_path.name}: expected exactly one .nuspec file")

            root = ElementTree.fromstring(package.read(nuspec_names[0]))
            ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}
            id_element = root.find(".//n:metadata/n:id", ns) if ns else root.find(".//metadata/id")
            version_element = root.find(".//n:metadata/n:version", ns) if ns else root.find(".//metadata/version")
            if id_element is None or version_element is None or not id_element.text or not version_element.text:
                raise ValueError(f"{package_path.name}: missing id or version metadata")
            versions[id_element.text.strip()] = version_element.text.strip()

    missing = sorted(set(PACKAGE_IDS) - set(versions))
    if missing:
        raise ValueError(f"Missing local packages required for consumer smoke tests: {missing}")

    distinct_versions = set(versions[package_id] for package_id in PACKAGE_IDS)
    if len(distinct_versions) != 1:
        raise ValueError(f"Expected Tenants packages to share one version, found {sorted(distinct_versions)}")

    return versions


def run_dotnet(args: list[str], working_directory: Path) -> None:
    env = os.environ.copy()
    env.setdefault("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "1")
    env.setdefault("MSBUILDDISABLENODEREUSE", "1")
    env["NUGET_PACKAGES"] = str(working_directory.parent / ".nuget" / "packages")
    full_args = ["dotnet", *args[:1], *args[1:]]
    subprocess.run(full_args, cwd=working_directory, check=True, env=env)


def run_xunit_assembly(test_assembly: Path, working_directory: Path) -> None:
    env = os.environ.copy()
    env.setdefault("DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER", "1")
    env.setdefault("MSBUILDDISABLENODEREUSE", "1")
    env["NUGET_PACKAGES"] = str(working_directory.parent / ".nuget" / "packages")
    subprocess.run(["dotnet", str(test_assembly), "-parallel", "none", "-noLogo"], cwd=working_directory, check=True, env=env)


def assert_package_only(project_file: Path, required_package_ids: list[str]) -> None:
    project_text = project_file.read_text(encoding="utf-8")
    if "ProjectReference" in project_text:
        raise ValueError(f"{project_file}: consumer projects must not use ProjectReference")

    for package_id in required_package_ids:
        if f'PackageReference Include="{package_id}"' not in project_text:
            raise ValueError(f"{project_file}: missing PackageReference for {package_id}")


def write_nuget_config(root: Path, package_directory: Path, additional_sources: list[str]) -> Path:
    """Add the local package directory while preserving inherited NuGet.Config sources."""
    config_file = root / "NuGet.Config"
    configuration = ElementTree.Element("configuration")
    package_sources = ElementTree.SubElement(configuration, "packageSources")
    ElementTree.SubElement(
        package_sources,
        "add",
        {"key": "local-tenants-packages", "value": str(package_directory.resolve())},
    )
    for index, source in enumerate(additional_sources, start=1):
        ElementTree.SubElement(package_sources, "add", {"key": f"additional-source-{index}", "value": source})

    ElementTree.ElementTree(configuration).write(config_file, encoding="utf-8", xml_declaration=True)
    return config_file


def write_contracts_client_consumer(root: Path, version: str) -> Path:
    project_dir = root / "contracts-client-consumer"
    project_dir.mkdir(parents=True)
    project_file = project_dir / "ContractsClientConsumer.csproj"
    project_file.write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Hexalith.Tenants.Contracts" Version="{version}" />
    <PackageReference Include="Hexalith.Tenants.Client" Version="{version}" />
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )
    (project_dir / "Program.cs").write_text(
        """using Hexalith.Tenants.Client.Registration;
using Hexalith.Tenants.Contracts.Commands;
using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Contracts.Queries;

using Microsoft.Extensions.DependencyInjection;

IServiceCollection services = new ServiceCollection();
services.AddHexalithTenants(options => options.PubSubName = "pubsub");

CreateTenant command = new("acme", "Acme Corp", "Consumer smoke");
TenantCreated created = new(command.TenantId, command.Name, command.Description, DateTimeOffset.UtcNow);
string queryType = ListTenantsQuery.QueryType;

if (created.TenantId != command.TenantId || queryType.Length == 0 || services.Count == 0) {
    throw new InvalidOperationException("Hexalith.Tenants Contracts and Client package surface is unavailable.");
}
""",
        encoding="utf-8",
    )
    assert_package_only(project_file, ["Hexalith.Tenants.Contracts", "Hexalith.Tenants.Client"])
    return project_file


def write_testing_consumer(root: Path, version: str, versions: dict[str, str]) -> Path:
    project_dir = root / "testing-consumer"
    project_dir.mkdir(parents=True)
    project_file = project_dir / "TestingConsumer.csproj"
    project_file.write_text(
        f"""<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Hexalith.Tenants.Testing" Version="{version}" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="{versions["Microsoft.NET.Test.Sdk"]}" />
    <PackageReference Include="xunit.v3" Version="{versions["xunit.v3"]}" />
    <PackageReference Include="xunit.runner.visualstudio" Version="{versions["xunit.runner.visualstudio"]}" PrivateAssets="all" />
  </ItemGroup>
</Project>
""",
        encoding="utf-8",
    )
    (project_dir / "TenantPackageSmokeTests.cs").write_text(
        """using Hexalith.Tenants.Contracts.Events;
using Hexalith.Tenants.Testing.Fakes;
using Hexalith.Tenants.Testing.Helpers;
using Hexalith.Tenants.Testing.Projections;

using Shouldly;

using Xunit;

public sealed class TenantPackageSmokeTests {
    [Fact]
    public void InMemoryTenantService_creates_tenant_without_infrastructure() {
        var service = new InMemoryTenantService();

        var result = TenantTestHelpers.CreateTenant(service, "acme", "Acme Corp");

        result.IsSuccess.ShouldBeTrue();
        TenantCreated created = result.Events.ShouldHaveSingleItem().ShouldBeOfType<TenantCreated>();
        var projection = new InMemoryTenantProjection();
        projection.ApplyEvents(service.EventHistory);
        projection.GetTenant(created.TenantId)!.Name.ShouldBe("Acme Corp");
    }
}
""",
        encoding="utf-8",
    )
    assert_package_only(project_file, ["Hexalith.Tenants.Testing"])
    return project_file


def validate_consumer(project_file: Path, test: bool = False) -> None:
    run_dotnet(["restore", str(project_file)], project_file.parent)
    if test:
        run_dotnet(
            [
                "build",
                str(project_file),
                "--no-restore",
                "--configuration",
                "Release",
                "-warnaserror",
                "-p:WarningsNotAsErrors=NU1603",
            ],
            project_file.parent,
        )
        test_assembly = project_file.parent / "bin" / "Release" / "net10.0" / f"{project_file.stem}.dll"
        run_xunit_assembly(test_assembly, project_file.parent)
    else:
        # Keep -warnaserror for genuine compiler warnings against the public package surface, but do not fail on
        # NU1603: the local/CI smoke version (e.g. 0.0.0-ci-test) is stamped onto submodule project references, so
        # transitive dependencies such as Hexalith.EventStore.Contracts legitimately resolve to a higher published
        # version. That version substitution is the expected consumer experience, not a package-reference defect.
        run_dotnet(
            ["build", str(project_file), "--no-restore", "--configuration", "Release", "-warnaserror", "-p:WarningsNotAsErrors=NU1603"],
            project_file.parent,
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate package-only consumer restore/build experience.")
    parser.add_argument("package_directory", type=Path, help="Directory containing local Hexalith.Tenants .nupkg files.")
    parser.add_argument("--work-directory", type=Path, default=Path("/tmp/hexalith-tenants-consumer-package-smoke"))
    parser.add_argument(
        "--nuget-source",
        action="append",
        default=[],
        help="Additional NuGet package source to add. May be supplied more than once; inherited NuGet.Config sources are preserved.",
    )
    args = parser.parse_args()

    package_directory = args.package_directory.resolve()
    versions = package_versions(package_directory)
    package_version = versions["Hexalith.Tenants.Contracts"]
    central_versions = central_package_versions()

    work_directory = args.work_directory.resolve()
    if work_directory.exists():
        shutil.rmtree(work_directory)
    work_directory.mkdir(parents=True)
    write_nuget_config(work_directory, package_directory, args.nuget_source)

    contracts_client_project = write_contracts_client_consumer(work_directory, package_version)
    testing_project = write_testing_consumer(work_directory, package_version, central_versions)

    validate_consumer(contracts_client_project)
    validate_consumer(testing_project, test=True)

    print(f"Validated package-only consumer restore/build experience at {package_version}:")
    print("- Contracts + Client consumer build")
    print("- Testing consumer infrastructure-free unit test")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as exc:
        print(f"Consumer package-reference validation failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Consumer package-reference validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
