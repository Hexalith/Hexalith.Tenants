#!/usr/bin/env python3
"""Validate Hexalith.Tenants NuGet packages before publishing."""

from __future__ import annotations

import argparse
import json
import sys
import zipfile
from dataclasses import dataclass
from pathlib import Path
from xml.etree import ElementTree


ROOT = Path(__file__).resolve().parents[1]
DEFAULT_MANIFEST = ROOT / "tools" / "release-packages.json"

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

FORBIDDEN_DEPENDENCY_IDS_NORMALIZED = frozenset(
    dependency_id.casefold() for dependency_id in FORBIDDEN_DEPENDENCY_IDS
)
FORBIDDEN_DEPENDENCY_FRAGMENTS_NORMALIZED = tuple(
    fragment.casefold() for fragment in FORBIDDEN_DEPENDENCY_FRAGMENTS
)


@dataclass(frozen=True)
class PackageMetadata:
    package_id: str
    version: str
    readme: str
    has_license: bool
    dependencies: frozenset[str]


def reject_duplicate_json_keys(pairs: list[tuple[str, object]]) -> dict[str, object]:
    """Build a JSON object while rejecting duplicate properties."""
    result: dict[str, object] = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON property '{key}'")
        result[key] = value
    return result


def load_json_object(path: Path, description: str) -> dict[str, object]:
    """Load a JSON object with fail-closed structural validation."""
    try:
        with path.open("r", encoding="utf-8") as handle:
            data = json.load(handle, object_pairs_hook=reject_duplicate_json_keys)
    except (OSError, UnicodeError, ValueError) as exc:
        raise ValueError(f"{description} '{path}' is unusable: {exc}") from exc

    if not isinstance(data, dict):
        raise ValueError(f"{description} '{path}' must contain a JSON object")
    return data


def require_object(value: object, path: str, assets_path: Path) -> dict[str, object]:
    """Return a restore-assets object or reject the malformed evidence."""
    if not isinstance(value, dict):
        raise ValueError(f"Restore assets '{assets_path}' must define object '{path}'")
    return value


def load_restore_dependencies(package_id: str, project_path: Path, project_root: Path) -> frozenset[str]:
    """Derive a package boundary from one project's NuGet restore evidence."""
    assets_path = project_path.parent / "obj" / "project.assets.json"
    resolved_assets_path = assets_path.resolve()
    if project_root not in resolved_assets_path.parents:
        raise ValueError(f"Restore assets must stay below '{project_root}': {assets_path}")
    if not resolved_assets_path.is_file():
        raise ValueError(f"Restore assets are missing for {package_id}: {assets_path}")

    assets = load_json_object(resolved_assets_path, "Restore assets")
    dependency_groups = require_object(
        assets.get("projectFileDependencyGroups"),
        "projectFileDependencyGroups",
        resolved_assets_path,
    )
    central_groups = require_object(
        assets.get("centralTransitiveDependencyGroups"),
        "centralTransitiveDependencyGroups",
        resolved_assets_path,
    )
    project = require_object(assets.get("project"), "project", resolved_assets_path)
    restore = require_object(project.get("restore"), "project.restore", resolved_assets_path)
    project_frameworks = require_object(project.get("frameworks"), "project.frameworks", resolved_assets_path)

    restored_project = restore.get("projectPath")
    restored_package_id = restore.get("projectName")
    project_style = restore.get("projectStyle")
    original_frameworks = restore.get("originalTargetFrameworks")
    if not isinstance(restored_project, str) or not restored_project.strip():
        raise ValueError(f"Restore assets '{resolved_assets_path}' have no usable project.restore.projectPath")
    restored_project_path = Path(restored_project)
    if not restored_project_path.is_absolute():
        restored_project_path = project_root / restored_project_path
    if restored_project_path.resolve() != project_path:
        raise ValueError(
            f"Restore assets '{resolved_assets_path}' belong to '{restored_project}', not '{project_path}'"
        )
    if not isinstance(restored_package_id, str) or restored_package_id.casefold() != package_id.casefold():
        raise ValueError(
            f"Restore assets '{resolved_assets_path}' identify package '{restored_package_id}', expected '{package_id}'"
        )
    if project_style != "PackageReference":
        raise ValueError(
            f"Restore assets '{resolved_assets_path}' use project style '{project_style}', expected 'PackageReference'"
        )

    framework_names = set(dependency_groups)
    if not framework_names:
        raise ValueError(f"Restore assets '{resolved_assets_path}' contain no target-framework dependency groups")
    if set(central_groups) != framework_names or set(project_frameworks) != framework_names:
        raise ValueError(f"Restore assets '{resolved_assets_path}' have inconsistent target-framework groups")
    if (
        not isinstance(original_frameworks, list)
        or any(not isinstance(framework, str) or not framework for framework in original_frameworks)
        or set(original_frameworks) != framework_names
    ):
        raise ValueError(f"Restore assets '{resolved_assets_path}' have inconsistent original target frameworks")

    canonical_ids: dict[str, str] = {}

    def add_dependency(dependency_id: str) -> None:
        normalized = dependency_id.casefold()
        previous = canonical_ids.get(normalized)
        if previous is not None and previous != dependency_id:
            raise ValueError(
                f"Restore assets '{resolved_assets_path}' contain ambiguous dependency IDs "
                f"'{previous}' and '{dependency_id}'"
            )
        canonical_ids[normalized] = dependency_id

    for framework in sorted(framework_names):
        direct_dependencies = dependency_groups[framework]
        if not isinstance(direct_dependencies, list):
            raise ValueError(
                f"Restore assets '{resolved_assets_path}' must define an array for "
                f"projectFileDependencyGroups.{framework}"
            )
        direct_ids: set[str] = set()
        for dependency in direct_dependencies:
            if not isinstance(dependency, str):
                raise ValueError(
                    f"Restore assets '{resolved_assets_path}' contain a non-string direct dependency for {framework}"
                )
            parts = dependency.split(maxsplit=1)
            if len(parts) != 2 or not parts[0] or not parts[1].strip():
                raise ValueError(
                    f"Restore assets '{resolved_assets_path}' contain unusable direct dependency '{dependency}'"
                )
            dependency_id = parts[0]
            normalized = dependency_id.casefold()
            if normalized in direct_ids:
                raise ValueError(
                    f"Restore assets '{resolved_assets_path}' contain duplicate direct dependency '{dependency_id}' "
                    f"for {framework}"
                )
            direct_ids.add(normalized)
            add_dependency(dependency_id)

        transitive_dependencies = central_groups[framework]
        if not isinstance(transitive_dependencies, dict):
            raise ValueError(
                f"Restore assets '{resolved_assets_path}' must define an object for "
                f"centralTransitiveDependencyGroups.{framework}"
            )
        transitive_ids: set[str] = set()
        for dependency_id, details in transitive_dependencies.items():
            if not dependency_id.strip() or not isinstance(details, dict):
                raise ValueError(
                    f"Restore assets '{resolved_assets_path}' contain an unusable centrally transitive dependency "
                    f"for {framework}"
                )
            normalized = dependency_id.casefold()
            if normalized in transitive_ids:
                raise ValueError(
                    f"Restore assets '{resolved_assets_path}' contain duplicate centrally transitive dependency "
                    f"'{dependency_id}' for {framework}"
                )
            transitive_ids.add(normalized)
            add_dependency(dependency_id)

    return frozenset(canonical_ids.values())


def load_dependency_boundaries(manifest_path: Path) -> dict[str, frozenset[str]]:
    """Load the release inventory and its restore-backed dependency boundaries."""
    resolved_manifest = manifest_path.resolve()
    if not resolved_manifest.is_file():
        raise ValueError(f"Release package manifest is missing: {manifest_path}")

    # The production manifest lives in tools/. A custom test manifest may live directly at
    # an isolated project root, while retaining the same project-relative path semantics.
    project_root = (
        resolved_manifest.parent.parent
        if resolved_manifest.parent.name == "tools"
        else resolved_manifest.parent
    )
    manifest = load_json_object(resolved_manifest, "Release package manifest")
    packages = manifest.get("packages")
    if not isinstance(packages, list) or not packages:
        raise ValueError(f"Release package manifest '{resolved_manifest}' must contain a non-empty 'packages' array")

    seen_ids: set[str] = set()
    seen_projects: set[str] = set()
    projects: list[tuple[str, Path]] = []
    for index, package in enumerate(packages, start=1):
        if not isinstance(package, dict):
            raise ValueError(f"Release package manifest entry #{index} must be an object")

        package_id = package.get("id")
        project = package.get("project")
        if not isinstance(package_id, str) or not isinstance(project, str):
            raise ValueError(
                f"Release package manifest entry #{index} must define string 'id' and 'project'"
            )
        package_id = package_id.strip()
        project = project.strip()
        if not package_id or not project:
            raise ValueError(
                f"Release package manifest entry #{index} must define non-empty 'id' and 'project'"
            )

        normalized_id = package_id.casefold()
        if normalized_id in seen_ids:
            raise ValueError(f"Duplicate package id in '{resolved_manifest}': {package_id}")
        if project.startswith("-"):
            raise ValueError(f"Package project must not look like an option: {project}")

        resolved_project = (project_root / project).resolve()
        if project_root not in resolved_project.parents:
            raise ValueError(f"Package project must stay below '{project_root}': {project}")
        normalized_project = str(resolved_project).casefold()
        if normalized_project in seen_projects:
            raise ValueError(f"Duplicate package project in '{resolved_manifest}': {project}")
        if resolved_project.suffix != ".csproj" or not resolved_project.is_file():
            raise ValueError(f"Package project must be an existing .csproj file: {project}")

        seen_ids.add(normalized_id)
        seen_projects.add(normalized_project)
        projects.append((package_id, resolved_project))

    return {
        package_id: load_restore_dependencies(package_id, project_path, project_root)
        for package_id, project_path in projects
    }


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


def validate_dependency_boundaries(
    package_path: Path,
    metadata: PackageMetadata,
    expected_dependencies: frozenset[str],
) -> None:
    """Validate package dependency metadata against restore evidence and forbidden boundaries."""
    forbidden_dependencies = sorted(
        dependency
        for dependency in metadata.dependencies
        if dependency.casefold() in FORBIDDEN_DEPENDENCY_IDS_NORMALIZED
        or any(fragment in dependency.casefold() for fragment in FORBIDDEN_DEPENDENCY_FRAGMENTS_NORMALIZED)
    )
    if forbidden_dependencies:
        raise ValueError(
            f"{package_path.name}: dependency boundary includes host, samples, tests, or other forbidden projects: "
            f"{forbidden_dependencies}"
        )

    expected_by_normalized_id = {
        dependency.casefold(): dependency
        for dependency in expected_dependencies
    }
    actual_by_normalized_id = {
        dependency.casefold(): dependency
        for dependency in metadata.dependencies
    }
    if actual_by_normalized_id.keys() != expected_by_normalized_id.keys():
        missing = sorted(
            expected_by_normalized_id[dependency_id]
            for dependency_id in expected_by_normalized_id.keys() - actual_by_normalized_id.keys()
        )
        unexpected = sorted(
            actual_by_normalized_id[dependency_id]
            for dependency_id in actual_by_normalized_id.keys() - expected_by_normalized_id.keys()
        )
        raise ValueError(
            f"{package_path.name}: dependency boundary mismatch. Missing: {missing}; unexpected: {unexpected}"
        )


def normalize_dependency_boundaries(
    dependency_boundaries: dict[str, frozenset[str]],
) -> dict[str, tuple[str, frozenset[str]]]:
    """Index manifest boundaries case-insensitively while retaining canonical spellings."""
    return {
        package_id.casefold(): (package_id, dependencies)
        for package_id, dependencies in dependency_boundaries.items()
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate Hexalith.Tenants NuGet package output.")
    parser.add_argument("package_directory", type=Path, help="Directory containing .nupkg files.")
    parser.add_argument(
        "--manifest",
        type=Path,
        default=DEFAULT_MANIFEST,
        help="Release package manifest (defaults to tools/release-packages.json).",
    )
    args = parser.parse_args()

    dependency_boundaries = normalize_dependency_boundaries(load_dependency_boundaries(args.manifest))
    expected_package_ids = frozenset(dependency_boundaries)
    package_directory = args.package_directory
    packages = sorted(
        path
        for path in package_directory.glob("*.nupkg")
        if ".symbols." not in path.name and not path.name.endswith(".snupkg")
    )

    if len(packages) != len(expected_package_ids):
        package_list = ", ".join(path.name for path in packages) or "<none>"
        raise ValueError(
            f"Expected {len(expected_package_ids)} packages, found {len(packages)}: {package_list}"
        )

    package_ids: dict[str, str] = {}
    versions: set[str] = set()
    for package in packages:
        metadata = get_metadata(package)
        normalized_package_id = metadata.package_id.casefold()
        if normalized_package_id in package_ids:
            raise ValueError(f"Duplicate package id in package output: {metadata.package_id}")
        package_ids[normalized_package_id] = metadata.package_id
        versions.add(metadata.version)
        if not metadata.has_license:
            raise ValueError(f"{package.name}: missing license metadata")
        expected_boundary = dependency_boundaries.get(normalized_package_id)
        if expected_boundary is None:
            raise ValueError(f"{package.name}: package id is not declared in the release manifest")
        _, expected_dependencies = expected_boundary
        validate_dependency_boundaries(package, metadata, expected_dependencies)

    if len(versions) != 1:
        raise ValueError(f"Expected all packages to share one version, found: {sorted(versions)}")

    version = next(iter(versions))
    print(f"Validated {len(packages)} NuGet packages at version {version}:")
    for package_id, expected_dependencies in sorted(dependency_boundaries.values()):
        print(f"- {package_id} dependencies: {', '.join(sorted(expected_dependencies))}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Package validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
