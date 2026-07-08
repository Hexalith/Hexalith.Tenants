#!/usr/bin/env python3
"""Validate Hexalith.Tenants NuGet packages before publishing."""

from __future__ import annotations

import argparse
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


EXPECTED_PACKAGE_IDS = frozenset({
    "Hexalith.Tenants.Contracts",
    "Hexalith.Tenants.Client",
    "Hexalith.Tenants.Server",
    "Hexalith.Tenants.Testing",
    "Hexalith.Tenants.Aspire",
})

EXPECTED_DEPENDENCIES = {
    "Hexalith.Tenants.Contracts": frozenset({
        "ByteAether.Ulid",
        "Hexalith.Commons.UniqueIds",
        "Hexalith.EventStore.Contracts",
    }),
    "Hexalith.Tenants.Client": frozenset({
        "ByteAether.Ulid",
        "Dapr.AspNetCore",
        "Dapr.Client",
        "Hexalith.Commons.UniqueIds",
        "Hexalith.EventStore.Client",
        "Hexalith.EventStore.Contracts",
        "Hexalith.Tenants.Contracts",
    }),
    "Hexalith.Tenants.Server": frozenset({
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
    }),
    "Hexalith.Tenants.Testing": frozenset({
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
    }),
    "Hexalith.Tenants.Aspire": frozenset({
        "Aspire.Hosting",
        "Aspire.Hosting.Keycloak",
        "Aspire.Hosting.Redis",
        "CommunityToolkit.Aspire.Hosting.Dapr",
        "Grpc.Net.ClientFactory",
        "Hexalith.EventStore.Aspire",
        "MessagePack",
        "ModelContextProtocol",
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
    }),
}

FORBIDDEN_DEPENDENCY_IDS = frozenset({
    "Hexalith.Tenants",
    "Hexalith.Tenants.AppHost",
    "Hexalith.Tenants.ServiceDefaults",
    "Hexalith.Tenants.Sample",
    "Hexalith.Tenants.Sample.Tests",
})

FORBIDDEN_DEPENDENCY_FRAGMENTS = (
    ".Tests",
    ".Test",
    ".Sample",
    ".Samples",
    ".AppHost",
    ".ServiceDefaults",
)


@dataclass(frozen=True)
class PackageMetadata:
    package_id: str
    version: str
    readme: str
    has_license: bool
    dependencies: frozenset[str]


def get_metadata(package_path: Path) -> PackageMetadata:
    """Return package id, version, metadata flags, and dependency ids."""
    with zipfile.ZipFile(package_path) as package:
        nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise ValueError(f"{package_path.name}: expected exactly one .nuspec file")

        root = ElementTree.fromstring(package.read(nuspec_names[0]))
        ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

        def find_text(name: str) -> str | None:
            element = root.find(f".//n:metadata/n:{name}", ns) if ns else root.find(f".//metadata/{name}")
            return element.text.strip() if element is not None and element.text else None

        def find_elements(path: str) -> list[ElementTree.Element]:
            return root.findall(path, ns) if ns else root.findall(path.replace("n:", ""))

        package_id = find_text("id")
        version = find_text("version")
        readme = find_text("readme")
        license_value = find_text("license")
        license_file = find_text("licenseFile")

        if not package_id:
            raise ValueError(f"{package_path.name}: missing nuspec package id")
        if not version:
            raise ValueError(f"{package_path.name}: missing nuspec version")
        if not readme:
            raise ValueError(f"{package_path.name}: missing nuspec readme metadata")
        if readme not in package.namelist():
            raise ValueError(f"{package_path.name}: readme file '{readme}' is not in the package")

        dependencies = frozenset(
            dependency.attrib["id"].strip()
            for dependency in find_elements(".//n:metadata/n:dependencies//n:dependency")
            if dependency.attrib.get("id", "").strip()
        )

        return PackageMetadata(package_id, version, readme, bool(license_value or license_file), dependencies)


def validate_dependency_boundaries(package_path: Path, metadata: PackageMetadata) -> None:
    """Validate package dependency metadata against the intended package boundaries."""
    expected_dependencies = EXPECTED_DEPENDENCIES.get(metadata.package_id)
    if expected_dependencies is None:
        raise ValueError(f"{package_path.name}: no expected dependency boundary is defined")

    if metadata.dependencies != expected_dependencies:
        missing = sorted(expected_dependencies - metadata.dependencies)
        unexpected = sorted(metadata.dependencies - expected_dependencies)
        raise ValueError(
            f"{package_path.name}: dependency boundary mismatch. Missing: {missing}; unexpected: {unexpected}"
        )

    forbidden_dependencies = sorted(
        dependency
        for dependency in metadata.dependencies
        if dependency in FORBIDDEN_DEPENDENCY_IDS or any(fragment in dependency for fragment in FORBIDDEN_DEPENDENCY_FRAGMENTS)
    )
    if forbidden_dependencies:
        raise ValueError(
            f"{package_path.name}: dependency boundary includes host, samples, tests, or other forbidden projects: "
            f"{forbidden_dependencies}"
        )


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Hexalith.Tenants NuGet package output.")
    parser.add_argument("package_directory", type=Path, help="Directory containing .nupkg files.")
    args = parser.parse_args()

    package_directory = args.package_directory
    packages = sorted(
        path
        for path in package_directory.glob("*.nupkg")
        if ".symbols." not in path.name and not path.name.endswith(".snupkg")
    )

    if len(packages) != len(EXPECTED_PACKAGE_IDS):
        package_list = ", ".join(path.name for path in packages) or "<none>"
        raise ValueError(
            f"Expected {len(EXPECTED_PACKAGE_IDS)} packages, found {len(packages)}: {package_list}"
        )

    package_ids: set[str] = set()
    versions: set[str] = set()
    for package in packages:
        metadata = get_metadata(package)
        package_ids.add(metadata.package_id)
        versions.add(metadata.version)
        if not metadata.has_license:
            raise ValueError(f"{package.name}: missing license metadata")
        validate_dependency_boundaries(package, metadata)

    if package_ids != EXPECTED_PACKAGE_IDS:
        missing = sorted(EXPECTED_PACKAGE_IDS - package_ids)
        unexpected = sorted(package_ids - EXPECTED_PACKAGE_IDS)
        raise ValueError(f"Package id mismatch. Missing: {missing}; unexpected: {unexpected}")

    if len(versions) != 1:
        raise ValueError(f"Expected all packages to share one version, found: {sorted(versions)}")

    version = next(iter(versions))
    print(f"Validated {len(packages)} NuGet packages at version {version}:")
    for package_id in sorted(package_ids):
        print(f"- {package_id} dependencies: {', '.join(sorted(EXPECTED_DEPENDENCIES[package_id]))}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Package validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
