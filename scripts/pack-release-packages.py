#!/usr/bin/env python3
"""Pack the exact Hexalith.Tenants NuGet packages published by semantic-release."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "tools" / "release-packages.json"


def load_package_projects() -> list[str]:
    """Read the authoritative release inventory.

    The manifest is the single source of truth: the publication preflight freezes it
    and proves each ID absent before publishing, so packing from a second hard-coded
    list would let published output drift from the inventory that was proven.
    """
    with MANIFEST.open("r", encoding="utf-8") as handle:
        data = json.load(handle)

    if not isinstance(data, dict):
        raise ValueError(f"{MANIFEST} must contain a JSON object.")

    packages = data.get("packages")
    if not isinstance(packages, list) or not packages:
        raise ValueError(f"{MANIFEST} must contain a non-empty 'packages' array.")

    seen_ids: set[str] = set()
    seen_projects: set[str] = set()
    projects: list[str] = []
    for index, package in enumerate(packages, start=1):
        if not isinstance(package, dict):
            raise ValueError(f"Package entry #{index} must be an object.")

        package_id = package.get("id")
        project = package.get("project")
        # Reject non-strings outright: str() would coerce None to "None" and a list to "['a']",
        # both of which survive a bare non-empty check and reach the dotnet pack command line.
        if not isinstance(package_id, str) or not isinstance(project, str):
            raise ValueError(f"Package entry #{index} must define string 'id' and 'project'.")

        package_id = package_id.strip()
        project = project.strip()
        if not package_id or not project:
            raise ValueError(f"Package entry #{index} must define non-empty 'id' and 'project'.")
        if package_id.lower() in seen_ids:
            raise ValueError(f"Duplicate package id in {MANIFEST}: {package_id}")
        if project.lower() in seen_projects:
            raise ValueError(f"Duplicate package project in {MANIFEST}: {project}")

        # The manifest is data, so confine it before it becomes argv: a value like "--interactive"
        # would otherwise be read by dotnet as a flag, and "../" would escape the repository.
        if project.startswith("-"):
            raise ValueError(f"Package project must not look like an option: {project}")
        resolved = (ROOT / project).resolve()
        if resolved.parent == resolved or ROOT not in resolved.parents:
            raise ValueError(f"Package project must stay below {ROOT}: {project}")
        if resolved.suffix != ".csproj" or not resolved.is_file():
            raise ValueError(f"Package project must be an existing .csproj file: {project}")

        seen_ids.add(package_id.lower())
        seen_projects.add(project.lower())
        projects.append(project)

    return projects


def main() -> int:
    parser = argparse.ArgumentParser(description="Pack Hexalith.Tenants release packages.")
    parser.add_argument("output_directory", type=Path, help="Directory where .nupkg files are written.")
    parser.add_argument("version", help="Package version to apply.")
    args = parser.parse_args()

    # Resolve the inventory before touching the output directory so a manifest problem is
    # reported as such, rather than surfacing later mixed in with packing failures.
    try:
        projects = load_package_projects()
    except (OSError, ValueError) as exc:
        print(f"Release package manifest is unusable: {exc}", file=sys.stderr)
        return 1

    output_directory = args.output_directory
    output_directory.mkdir(parents=True, exist_ok=True)
    for package in output_directory.glob("*.nupkg"):
        package.unlink()
    for package in output_directory.glob("*.snupkg"):
        package.unlink()

    for project in projects:
        subprocess.run(
            [
                "dotnet",
                "pack",
                project,
                "--no-build",
                "--configuration",
                "Release",
                "--output",
                str(output_directory),
                f"-p:Version={args.version}",
                "/m:1",
                "/nr:false",
            ],
            check=True,
        )

    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as exc:
        print(f"Package packing failed with exit code {exc.returncode}.", file=sys.stderr)
        raise SystemExit(exc.returncode)
