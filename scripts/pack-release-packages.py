#!/usr/bin/env python3
"""Pack the exact Hexalith.Tenants NuGet packages published by semantic-release."""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path


PACKAGE_PROJECTS = [
    "src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj",
    "src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj",
    "src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj",
    "src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj",
    "src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj",
]


def main() -> int:
    parser = argparse.ArgumentParser(description="Pack Hexalith.Tenants release packages.")
    parser.add_argument("output_directory", type=Path, help="Directory where .nupkg files are written.")
    parser.add_argument("version", help="Package version to apply.")
    args = parser.parse_args()

    output_directory = args.output_directory
    output_directory.mkdir(parents=True, exist_ok=True)
    for package in output_directory.glob("*.nupkg"):
        package.unlink()
    for package in output_directory.glob("*.snupkg"):
        package.unlink()

    for project in PACKAGE_PROJECTS:
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
