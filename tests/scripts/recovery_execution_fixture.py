#!/usr/bin/env python3
"""Create immutable source/tooling checkouts for offline recovery executable controls."""

import json
import os
import re
import textwrap
from pathlib import Path
import shutil
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]


def commit(root):
    for args in (("init", "-q"), ("add", "."), ("-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                                             "commit", "-qm", "test: create fixture")):
        subprocess.run(["git", "-C", str(root), *args], check=True, capture_output=True)
    return subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()


def create(root, actual_workflows=False):
    root = Path(root)
    builds = root / ".hexalith/builds-execution"
    for directory in ("Github/governed-provenance", "Github/publish-containers", "Github/initialize-build"):
        shutil.copytree(ROOT / "references/Hexalith.Builds" / directory, builds / directory,
                        ignore=shutil.ignore_patterns("__pycache__", "*.pyc"))
    builds_sha = commit(builds)
    workflows = root / ".github/workflows"
    workflows.mkdir(parents=True, exist_ok=True)
    if actual_workflows:
        for name in ("release.yml", "recover-partial-release.yml"):
            source = (ROOT / ".github/workflows" / name).read_text()
            if name == "release.yml":
                # Bind the actual workflow graph to this immutable local Builds snapshot.
                source = re.sub(r"(?m)^([ \t]*HEXALITH_BUILDS_EXECUTION_SHA:[ \t]*)[0-9a-f]{40}([ \t]*)$",
                                lambda match: match[1] + builds_sha + match[2], source)
            (workflows / name).write_text(source)
    else:
        (workflows / "release.yml").write_text("env:\n  HEXALITH_BUILDS_EXECUTION_SHA: " + builds_sha + "\n")
        (workflows / "recover-partial-release.yml").write_text("jobs:\n  recover:\n    steps:\n      - uses: actions/checkout@3d3c42e5aac5ba805825da76410c181273ba90b1\n")
    subprocess.run(["git", "-C", str(root), "init", "-q"], check=True)
    subprocess.run(["git", "-C", str(root), "add", ".github"], check=True)
    subprocess.run(["git", "-C", str(root), "-c", "user.name=Fixture", "-c", "user.email=fixture@example.invalid",
                    "commit", "-qm", "test: create source fixture"], check=True)
    source_sha = subprocess.check_output(["git", "-C", str(root), "rev-parse", "HEAD"], text=True).strip()
    provenance = root / ".hexalith/recovery-workflow-provenance.json"
    for name in (("recover-partial-release.yml", "release.yml") if actual_workflows else ("recover-partial-release.yml",)):
        workflow_path = ".github/workflows/" + name
        workflow_ref = "fixture/recovery/" + workflow_path + "@refs/heads/main"
        output_path = ".hexalith/recovery-workflow-provenance.json" if name.startswith("recover-") else ".hexalith/release-workflow-provenance.json"
        subprocess.run([sys.executable, str(builds / "Github/governed-provenance/governed_provenance.py"),
                        "--workflow-mode", "standalone", "--stage", "release", "--builds-root", str(builds),
                        "--builds-execution-sha", builds_sha, "--workflow-root", str(root),
                        "--workflow-path", workflow_path, "--workflow-repository", "fixture/recovery",
                        "--workflow-sha", source_sha, "--workflow-ref", workflow_ref,
                        "--workflow-file-path", workflow_path, "--caller-repository", "fixture/recovery",
                        "--caller-workflow-ref", workflow_ref,
                        "--candidate", source_sha, "--output", output_path],
                       cwd=root, check=True, capture_output=True)
    execution_state = root / ".hexalith/recovery-execution-identity.json"
    subprocess.run([sys.executable, str(builds / "Github/governed-provenance/execution_identity.py"), "capture",
                    "--source", source_sha, "--declared", builds_sha, "--builds-root", str(builds),
                    "--provenance", str(provenance), "--state", str(execution_state)], cwd=root,
                   env={**os.environ, "GITHUB_REPOSITORY": "fixture/recovery", "PYTHONDONTWRITEBYTECODE": "1"},
                   check=True, capture_output=True)
    return {"source_sha": source_sha, "builds_sha": builds_sha, "builds_root": str(builds),
            "provenance": str(provenance), "identity": str(execution_state),
            "release_provenance": str(root / ".hexalith/release-workflow-provenance.json")}


def workflow_step(path, name):
    """Extract the actual literal run block for a named shipped workflow step."""
    source = Path(path).read_text()
    block = source.split("      - name: " + name + "\n", 1)[1]
    lines = block.splitlines(keepends=True)
    start = next(index for index, line in enumerate(lines) if line == "        run: |\n") + 1
    selected = []
    for line in lines[start:]:
        if line.strip() and not line.startswith("          "):
            break
        selected.append(line)
    return textwrap.dedent("".join(selected))


if __name__ == "__main__":
    print(json.dumps(create(sys.argv[1])))
