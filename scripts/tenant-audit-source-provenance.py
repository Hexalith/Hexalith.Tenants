#!/usr/bin/env python3
"""Validate the clean root and its root-declared dependency checkouts before a run."""

import json
from pathlib import Path
import subprocess
import sys


def git(root: Path, *arguments: str) -> str:
    return subprocess.run(
        ["git", "-C", str(root), *arguments], check=True, text=True,
        stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    ).stdout.strip()


def require_visible_index(root: Path) -> None:
    # Git status intentionally omits changes hidden by either index flag.
    entries = git(root, "ls-files", "-v", "-z").split("\0")
    if any(entry and (entry[0].islower() or entry[0] == "S") for entry in entries):
        raise ValueError("Tracked source uses assume-unchanged or skip-worktree index flags.")


def provenance(root: Path) -> dict:
    root = root.resolve(strict=True)
    if Path(git(root, "rev-parse", "--show-toplevel")).resolve() != root:
        raise ValueError("Source root must be the owning repository root.")
    revision = git(root, "rev-parse", "--verify", "HEAD^{commit}")
    require_visible_index(root)
    # Ignore dependency status here because each declared checkout is checked below.
    if git(root, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=all"):
        raise ValueError("Root source is dirty; commit or isolate the intended source before measuring.")
    declarations = subprocess.run(
        ["git", "-C", str(root), "config", "--file", ".gitmodules", "--get-regexp", r"^submodule\..*\.path$"],
        text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
    )
    if declarations.returncode not in (0, 1):
        raise ValueError("Cannot inspect root dependency declarations.")
    dependencies = {}
    for declaration in declarations.stdout.splitlines():
        _, path = declaration.split(maxsplit=1)
        checkout = (root / path).resolve()
        if not checkout.is_relative_to(root) or checkout == root:
            raise ValueError(f"Dependency path is outside the source root: {path}")
        tracked = git(root, "ls-tree", revision, "--", path).split()
        if len(tracked) < 3 or tracked[0] != "160000":
            raise ValueError(f"Dependency has no committed root gitlink: {path}")
        if not checkout.is_dir() or Path(git(checkout, "rev-parse", "--show-toplevel")).resolve() != checkout:
            raise ValueError(f"Dependency checkout is missing: {path}")
        actual = git(checkout, "rev-parse", "--verify", "HEAD^{commit}")
        if actual != tracked[2]:
            raise ValueError(f"Dependency checkout differs from the committed gitlink: {path}")
        require_visible_index(checkout)
        if git(checkout, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=none"):
            raise ValueError(f"Dependency source is dirty: {path}")
        dependencies[path] = actual
    # Recheck every captured checkout after inspecting the complete dependency set.
    for path, captured in dependencies.items():
        checkout = (root / path).resolve()
        if not checkout.is_relative_to(root) or checkout == root or not checkout.is_dir():
            raise ValueError(f"Dependency checkout changed during source preflight: {path}")
        if Path(git(checkout, "rev-parse", "--show-toplevel")).resolve() != checkout:
            raise ValueError(f"Dependency checkout changed during source preflight: {path}")
        if git(checkout, "rev-parse", "--verify", "HEAD^{commit}") != captured:
            raise ValueError(f"Dependency revision changed during source preflight: {path}")
        require_visible_index(checkout)
        if git(checkout, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=none"):
            raise ValueError(f"Dependency source changed during source preflight: {path}")
    if git(root, "rev-parse", "--verify", "HEAD^{commit}") != revision:
        raise ValueError("Root revision changed during source preflight.")
    require_visible_index(root)
    if git(root, "status", "--porcelain=v1", "--untracked-files=all", "--ignore-submodules=all"):
        raise ValueError("Root source changed during source preflight.")
    return {"rootRevision": revision, "dependencies": dependencies}


def main() -> int:
    try:
        print(json.dumps(provenance(Path(sys.argv[1])), sort_keys=True))
        return 0
    except (IndexError, OSError, ValueError, subprocess.CalledProcessError) as error:
        # Never print Git output that might contain source paths or private source contents.
        message = str(error) if isinstance(error, ValueError) else "Source provenance could not be established."
        print(f"Performance source preflight refused: {message}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
