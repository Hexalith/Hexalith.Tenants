#!/usr/bin/env python3
"""Preflight existing CI historical-identity checks on the uncommitted spec.

This read-only probe is not a simulated push or a full CI event run. It invokes
unchanged audit_ref logic against the repaired Markdown and current Git history,
with HEAD as both existing-state boundaries. No commits or refs are created.
"""
import importlib.util
from pathlib import Path
import subprocess
import sys

root = Path(__file__).resolve().parents[4]
path = "_bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md"
module_spec = importlib.util.spec_from_file_location(
    "changed_story_guard", root / "scripts/validate-changed-story-gitlinks.py")
module = importlib.util.module_from_spec(module_spec)
module_spec.loader.exec_module(module)
head = subprocess.check_output(
    ["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
moved = subprocess.run(
    ["git", "diff", "--exit-code", "HEAD", "--", "references/"],
    cwd=root, capture_output=True, check=False).returncode != 0
print("Read-only CI historical-provenance preflight; existing HEAD:", head)
try:
    endpoint = module.audit_ref(
        (root / path).read_text(), {path}, path, head, head, moved)
except module.guard.CheckError as error:
    print("RESULT: FAIL")
    print(error)
    sys.exit(1)
print("RESULT: PASS", endpoint)
