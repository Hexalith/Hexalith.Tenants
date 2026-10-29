#!/usr/bin/env python3
"""Replay the repair on actual history, committing only in disposable clones."""

import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[4]
EVIDENCE = Path(__file__).resolve().parent
STORY = "_bmad-output/implementation-artifacts/spec-global-admin-projection-paging.md"
POLICY_SPEC = "_bmad-output/implementation-artifacts/spec-ci-historical-baseline-repair.md"
RUNNER = "scripts/validate-changed-story-gitlinks.py"
CREATION = "7716f0b11423eb54d74935b6cc6e3edf405dc400"
BASELINE = "6bd5fc29cf66e0238a4ea5ceb8c743c358fe3513"
ENDPOINT = "ded413dfd4786983585c7f56a399825cc1afbae3"
REJECTED = "23b2a7691ae667fd160a48a5d7293f3cf38d48fe"
RECORDS = []


def run(cwd, *args, expected=0):
    result = subprocess.run(args, cwd=cwd, capture_output=True, text=True, check=False)
    RECORDS.append({"cwd": str(cwd), "command": list(args), "exit": result.returncode,
                    "stdout": result.stdout, "stderr": result.stderr})
    if result.returncode != expected:
        raise AssertionError(RECORDS[-1])
    return result.stdout.strip()


def event(repo, before, after, kind, label, expected=0, diagnostic=None):
    payload = ({"before": before, "after": after} if kind == "push" else
               {"pull_request": {"base": {"sha": before}, "head": {"sha": after}}})
    path = EVIDENCE / f"{label}-event.json"
    path.write_text(json.dumps(payload, indent=2) + "\n")
    output = run(repo, sys.executable, RUNNER, "--event-name", kind,
                 "--event-path", str(path), expected=expected)
    if diagnostic and diagnostic not in output:
        raise AssertionError(output)
    if expected == 0:
        assert "failed: 0" in output, output
    print(f"{label}: exit {expected}")


def fixture_commit(repo):
    # Exact message was validated by the owning repository's pinned commitlint.
    run(repo, "git", "commit", "-m", "test: Create fixture commit")
    return run(repo, "git", "rev-parse", "HEAD")


def main():
    head = run(ROOT, "git", "rev-parse", "HEAD")
    assert run(ROOT, "git", "show", "-s", "--format=%P", CREATION) == BASELINE
    creation_text = run(ROOT, "git", "show", f"{CREATION}:{STORY}")
    endpoint_text = run(ROOT, "git", "show", f"{ENDPOINT}:{STORY}")
    assert f"baseline_revision: '{BASELINE}'" in creation_text
    assert "baseline_commit:" not in creation_text.split("---", 2)[1]
    assert f"baseline_commit: '{REJECTED}'" in endpoint_text
    assert f"baseline_revision: '{BASELINE}'" in endpoint_text
    assert run(ROOT, "git", "cat-file", "-t", REJECTED) == "commit"
    run(ROOT, "git", "merge-base", "--is-ancestor", REJECTED, ENDPOINT, expected=1)
    run(ROOT, "git", "merge-base", "--is-ancestor", CREATION, ENDPOINT)
    run(ROOT, "git", "merge-base", "--is-ancestor", ENDPOINT, head)
    run(ROOT, sys.executable,
        "_bmad-output/implementation-artifacts/evidence/global-admin-projection-paging-2026-10-29/check-ci-provenance.py")
    run(ROOT, sys.executable, "scripts/validate-story-gitlinks.py", STORY, "--ref", ENDPOINT)

    # The old committed metadata must remain refused; current worktree contents
    # cannot retroactively bless an unrepaired committed event.
    parent = run(ROOT, "git", "rev-parse", f"{head}^")
    event(ROOT, parent, head, "push", "unrepaired-source", expected=1,
          diagnostic="historical audit must retain")

    with tempfile.TemporaryDirectory(prefix="ci-historical-repair-") as temporary:
        for transport in (False, True):
            repo = Path(temporary) / ("transport" if transport else "available")
            mode = "--no-local" if transport else "--shared"
            run(ROOT, "git", "clone", "--quiet", "--no-checkout", mode, str(ROOT), str(repo))
            run(repo, "git", "config", "core.hooksPath", "/dev/null")
            run(repo, "git", "config", "commit.gpgsign", "false")
            run(repo, "git", "config", "user.name", "Fixture")
            run(repo, "git", "config", "user.email", "fixture@example.invalid")
            run(repo, "git", "checkout", "--quiet", "--detach", head)
            available = subprocess.run(["git", "cat-file", "-e", REJECTED], cwd=repo,
                                       capture_output=True, text=True, check=False)
            RECORDS.append({"cwd": str(repo), "command": ["git", "cat-file", "-e", REJECTED],
                            "exit": available.returncode, "stdout": available.stdout,
                            "stderr": available.stderr})
            assert available.returncode == (1 if transport else 0)
            assert run(repo, "git", "rev-parse", "--is-shallow-repository") == "false"
            for path in (RUNNER, "tests/scripts/test_validate_changed_story_gitlinks.py",
                         "docs/story-gitlink-ci.md", STORY, POLICY_SPEC):
                (repo / path).parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(ROOT / path, repo / path)
                run(repo, "git", "add", "--", path)
            repaired = fixture_commit(repo)
            label = "transport" if transport else "available"
            event(repo, head, repaired, "push", f"{label}-repair-push")
            event(repo, head, repaired, "pull_request", f"{label}-repair-pr")
            corrected = (repo / STORY).read_text()
            (repo / STORY).write_text(corrected + "\nDisposable later prose edit.\n")
            run(repo, "git", "add", "--", STORY)
            later = fixture_commit(repo)
            event(repo, repaired, later, "push", f"{label}-later-prose")

            (repo / STORY).write_text(corrected.replace(
                f"gitlink_creation_ref: '{CREATION}'", f"gitlink_creation_ref: '{ENDPOINT}'"))
            run(repo, "git", "add", "--", STORY)
            false_creation = fixture_commit(repo)
            event(repo, later, false_creation, "push", f"{label}-false-creation", expected=1,
                  diagnostic="sole original story creation")

            (repo / STORY).write_text(corrected)
            run(repo, "git", "add", "--", STORY)
            dependency = "references/Hexalith.EventStore"
            target = run(repo, "git", "rev-parse", f"{BASELINE}:{dependency}")
            run(repo, "git", "update-index", "--cacheinfo", f"160000,{target},{dependency}")
            moved = fixture_commit(repo)
            event(repo, false_creation, moved, "push", f"{label}-pointer-event", expected=1,
                  diagnostic="gitlink-bearing event commits")
            target = run(repo, "git", "rev-parse", f"{head}:{dependency}")
            run(repo, "git", "update-index", "--cacheinfo", f"160000,{target},{dependency}")
            reverted = fixture_commit(repo)
            event(repo, false_creation, reverted, "push", f"{label}-pointer-revert", expected=1,
                  diagnostic="gitlink-bearing event commits")
    assert run(ROOT, "git", "rev-parse", "HEAD") == head
    print("Actual-history and disposable committed-event verification: PASS")


if __name__ == "__main__":
    try:
        main()
    finally:
        (EVIDENCE / "history-results.json").write_text(json.dumps(RECORDS, indent=2) + "\n")
