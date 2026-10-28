#!/usr/bin/env python3
"""Carry validated restore evidence between isolated release runners."""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SPEC = importlib.util.spec_from_file_location("release_package_validator", ROOT / "scripts/validate-nuget-packages.py")
assert SPEC is not None and SPEC.loader is not None
VALIDATOR = importlib.util.module_from_spec(SPEC)
sys.modules[SPEC.name] = VALIDATOR
SPEC.loader.exec_module(VALIDATOR)
SCHEMA = "hexalith.release.restore-context.v1"


def safe_path(path: Path, root: Path) -> Path:
    """Reject aliases and escapes before reading or writing generated evidence."""
    relative = path.relative_to(root)
    current = root
    for part in relative.parts:
        current /= part
        if current.is_symlink():
            raise ValueError(f"Release validation context must not follow symlinks: {current}")
    if root not in path.resolve().parents:
        raise ValueError(f"Release validation context escapes its source checkout: {path}")
    return path


def sha256(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def encode(value: object) -> bytes:
    return (json.dumps(value, indent=2, sort_keys=True) + "\n").encode("utf-8")


def identity(root: Path) -> tuple[str, str]:
    """Bind handoff evidence to the already prepared version and exact live source."""
    state = root / ".hexalith/release-candidate"
    version = safe_path(state / "version", root).read_text().strip()
    source = safe_path(state / "source-sha", root).read_text().strip()
    if not re.fullmatch(r"(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)", version):
        raise ValueError("Prepared release version is invalid")
    if not re.fullmatch(r"[0-9a-f]{40}", source):
        raise ValueError("Prepared release source is invalid")
    live = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    if source != live:
        raise ValueError("Prepared restore evidence does not match the live source")
    if subprocess.run(["git", "diff", "--quiet", "HEAD", "--"], cwd=root, check=False).returncode:
        raise ValueError("Tracked release source differs from the prepared commit")
    return version, source


def packages(root: Path, projects: list[tuple[str, Path]], version: str) -> list[dict[str, str]]:
    directory = safe_path(root / "nupkgs", root)
    files = sorted(directory.glob("*.nupkg"))
    expected_ids = {package_id.casefold() for package_id, _ in projects}
    if len(files) != len(expected_ids):
        raise ValueError("Prepared package inventory differs from the release manifest")
    rows = []
    seen = set()
    for file in files:
        safe_path(file, root)
        metadata = VALIDATOR.get_metadata(file)
        package_id = metadata.package_id.casefold()
        if package_id not in expected_ids or package_id in seen or metadata.version != version:
            raise ValueError("Prepared package IDs or versions differ from the candidate")
        seen.add(package_id)
        rows.append({"name": file.name, "sha256": sha256(file.read_bytes())})
    return rows


def capture(manifest: Path) -> None:
    root, projects = VALIDATOR.load_release_projects(manifest)
    version, source = identity(root)
    VALIDATOR.load_dependency_boundaries(manifest)
    context = safe_path(root / ".hexalith/release-candidate/validation-context", root)
    if context.exists():
        raise ValueError("Release validation context already exists")
    rows = []
    contents = []
    for _, project in projects:
        path = safe_path(project.parent / "obj/project.assets.json", root)
        assets = VALIDATOR.load_json_object(path, "Restore assets")
        relative = project.relative_to(root).as_posix()
        # Other absolute fields are not consumed by the package-boundary validator.
        # Preserve all dependency evidence; only the source-project identity becomes portable.
        assets["project"]["restore"]["projectPath"] = relative
        content = encode(assets)
        rows.append({"project": relative, "sha256": sha256(content)})
        contents.append(content)
    index = {"schema": SCHEMA, "version": version, "source_sha": source,
             "manifest_sha256": sha256(manifest.read_bytes()),
             "packages": packages(root, projects, version), "assets": rows}
    context.mkdir(parents=True)
    for number, content in enumerate(contents):
        (context / f"{number}.json").write_bytes(content)
    (context / "index.json").write_bytes(encode(index))


def restore(manifest: Path) -> None:
    root, projects = VALIDATOR.load_release_projects(manifest)
    version, source = identity(root)
    context = safe_path(root / ".hexalith/release-candidate/validation-context", root)
    index = VALIDATOR.load_json_object(safe_path(context / "index.json", root), "Restore context")
    expected = {"schema": SCHEMA, "version": version, "source_sha": source,
                "manifest_sha256": sha256(manifest.read_bytes()),
                "packages": packages(root, projects, version)}
    if set(index) != set(expected) | {"assets"} or any(index.get(key) != value for key, value in expected.items()):
        raise ValueError("Restore context identity, manifest or package bytes changed")
    rows = index["assets"]
    if not isinstance(rows, list) or len(rows) != len(projects):
        raise ValueError("Restore context asset inventory is invalid")
    writes = []
    for number, ((package_id, project), row) in enumerate(zip(projects, rows, strict=True)):
        relative = project.relative_to(root).as_posix()
        path = safe_path(context / f"{number}.json", root)
        content = path.read_bytes()
        if not isinstance(row, dict) or row != {"project": relative, "sha256": sha256(content)}:
            raise ValueError("Restore context asset identity or bytes changed")
        assets = VALIDATOR.load_json_object(path, "Restore context assets")
        project_data = VALIDATOR.require_object(assets.get("project"), "project", path)
        restore_data = VALIDATOR.require_object(project_data.get("restore"), "project.restore", path)
        if restore_data.get("projectPath") != relative:
            raise ValueError("Restore context must use its manifest-relative source project")
        VALIDATOR.load_restore_dependencies(package_id, project, root, path)
        target = safe_path(project.parent / "obj/project.assets.json", root)
        if target.exists():
            raise ValueError(f"Refusing to replace existing restore evidence: {target}")
        if target.parent.exists() and not target.parent.is_dir():
            raise ValueError(f"Restore evidence parent is not a directory: {target.parent}")
        writes.append((target, content))
    # Validate every bound input before materializing any source-tree evidence.
    created = []
    try:
        for target, content in writes:
            target.parent.mkdir(parents=True, exist_ok=True)
            with target.open("xb") as output:
                created.append(target)
                output.write(content)
    except BaseException:
        for target in reversed(created):
            target.unlink(missing_ok=True)
        raise
    VALIDATOR.load_dependency_boundaries(manifest)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("mode", choices=("capture", "restore"))
    parser.add_argument("--manifest", type=Path, default=ROOT / "tools/release-packages.json")
    arguments = parser.parse_args()
    manifest = arguments.manifest.resolve()
    (capture if arguments.mode == "capture" else restore)(manifest)
    print(f"Release validation context {arguments.mode} passed.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        print(f"Release validation context failed: {error}", file=sys.stderr)
        raise SystemExit(1)
