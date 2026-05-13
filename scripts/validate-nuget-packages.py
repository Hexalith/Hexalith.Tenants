#!/usr/bin/env python3
"""Validate Hexalith.Tenants NuGet packages before publishing."""

from __future__ import annotations

import argparse
import sys
import zipfile
from pathlib import Path
from xml.etree import ElementTree


EXPECTED_PACKAGE_IDS = {
    "Hexalith.Tenants.Contracts",
    "Hexalith.Tenants.Client",
    "Hexalith.Tenants.Server",
    "Hexalith.Tenants.Testing",
    "Hexalith.Tenants.Aspire",
}


def get_metadata(package_path: Path) -> tuple[str, str, str | None, bool]:
    """Return package id, version, readme path, and license metadata flag."""
    with zipfile.ZipFile(package_path) as package:
        nuspec_names = [name for name in package.namelist() if name.endswith(".nuspec")]
        if len(nuspec_names) != 1:
            raise ValueError(f"{package_path.name}: expected exactly one .nuspec file")

        root = ElementTree.fromstring(package.read(nuspec_names[0]))
        ns = {"n": root.tag.split("}")[0].strip("{")} if root.tag.startswith("{") else {}

        def find_text(name: str) -> str | None:
            element = root.find(f".//n:metadata/n:{name}", ns) if ns else root.find(f".//metadata/{name}")
            return element.text.strip() if element is not None and element.text else None

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

        return package_id, version, readme, bool(license_value or license_file)


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
        package_id, version, _readme, has_license = get_metadata(package)
        package_ids.add(package_id)
        versions.add(version)
        if not has_license:
            raise ValueError(f"{package.name}: missing license metadata")

    if package_ids != EXPECTED_PACKAGE_IDS:
        missing = sorted(EXPECTED_PACKAGE_IDS - package_ids)
        unexpected = sorted(package_ids - EXPECTED_PACKAGE_IDS)
        raise ValueError(f"Package id mismatch. Missing: {missing}; unexpected: {unexpected}")

    if len(versions) != 1:
        raise ValueError(f"Expected all packages to share one version, found: {sorted(versions)}")

    version = next(iter(versions))
    print(f"Validated {len(packages)} NuGet packages at version {version}:")
    for package_id in sorted(package_ids):
        print(f"- {package_id}")

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exc:  # noqa: BLE001 - command-line validator should print concise failures.
        print(f"Package validation failed: {exc}", file=sys.stderr)
        raise SystemExit(1)
