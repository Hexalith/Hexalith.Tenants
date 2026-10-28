#!/usr/bin/env python3
"""Run the production gitlink guard on committed stories selected by a CI event.

Pushes use before..after; pull requests use merge-base(base, head)..head. Optional
gitlink_audit_ref frontmatter permits a historical documentation repair only in
an event with no gitlink-bearing commits. See docs/story-gitlink-ci.md.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import os
from pathlib import Path
import re
import subprocess
import sys
import tempfile

REPO_ROOT = Path(__file__).resolve().parents[1]
GUARD_PATH = REPO_ROOT / "scripts" / "validate-story-gitlinks.py"
ARTIFACT_ROOT = "_bmad-output/implementation-artifacts/"

_spec = importlib.util.spec_from_file_location("story_gitlink_guard", GUARD_PATH)
assert _spec is not None and _spec.loader is not None
guard = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(guard)


def git(*args: str) -> str:
    """Inspect this repository, refusing incomplete history or failed reads."""
    result = subprocess.run(
        ["git", "--literal-pathspecs", *args], cwd=REPO_ROOT,
        capture_output=True, check=False,
    )
    if result.returncode:
        raise guard.CheckError(f"git {' '.join(args)} failed: {result.stderr.decode('utf-8', errors='replace').strip()}")
    # text=True would translate CR bytes in otherwise valid NUL-delimited paths.
    return result.stdout.decode("utf-8")


def canonical_commit(value: str) -> str:
    """Require the event or historical field to name a full, available commit."""
    if not isinstance(value, str) or not re.fullmatch(r"(?:[0-9a-f]{40}|[0-9a-f]{64})", value):
        raise guard.CheckError(f"Expected a full canonical commit ID, got {value!r}.")
    resolved = git("rev-parse", "--verify", f"{value}^{{commit}}").strip()
    if resolved != value:
        raise guard.CheckError(f"Commit {value} is not canonical.")
    return resolved


def require_ancestor(older: str, newer: str) -> None:
    """Distinguish missing ancestry from Git inspection errors."""
    result = subprocess.run(
        ["git", "merge-base", "--is-ancestor", older, newer],
        cwd=REPO_ROOT, capture_output=True, text=True, check=False,
    )
    if result.returncode != 0:
        raise guard.CheckError(
            f"Cannot establish {older} as an ancestor of {newer}: "
            f"{result.stderr.strip() or 'not an ancestor'}."
        )


def event_range(event: dict, event_name: str) -> tuple[str, str]:
    """Resolve GitHub event boundaries without using a synthetic merge HEAD."""
    if event_name == "push":
        base = canonical_commit(event["before"])
        head = canonical_commit(event["after"])
    elif event_name == "pull_request":
        target = canonical_commit(event["pull_request"]["base"]["sha"])
        head = canonical_commit(event["pull_request"]["head"]["sha"])
        base = canonical_commit(git("merge-base", target, head).strip())
    else:
        raise guard.CheckError(f"Unsupported event {event_name!r}; expected push or pull_request.")
    require_ancestor(base, head)
    if git("rev-parse", "--is-shallow-repository").strip() != "false":
        raise guard.CheckError("Full Git history is required; configure checkout fetch-depth: 0.")
    return base, head


def changed_paths(base: str, head: str, *, artifacts_only: bool = True) -> list[tuple[str | None, str | None]]:
    """Return old/new paths, limiting event selection to artifacts by default."""
    fields = git(
        "diff", "--name-status", "-z", "--find-renames", "--diff-filter=AMRDT",
        base, head, "--", *([ARTIFACT_ROOT] if artifacts_only else []),
    ).split("\0")
    changes = []
    index = 0
    while index < len(fields) - 1:
        status, path = fields[index:index + 2]
        index += 2
        if status.startswith("R"):
            new_path = fields[index]
            index += 1
            changes.append((path, new_path))
        else:
            changes.append((None if status == "A" else path, None if status == "D" else path))
    return changes


def snapshot(commit: str, path: str | None, regular: bool = False) -> str | None:
    """Read a committed blob without following a symlink; classify before enforcing mode."""
    if path is None or not path.endswith(".md"):
        return None
    entry = git("ls-tree", "-z", commit, "--", path).rstrip("\0")
    if not entry:
        return None
    metadata, _, actual_path = entry.partition("\t")
    mode, kind, oid = metadata.split()
    if actual_path != path or (regular and (mode not in ("100644", "100755") or kind != "blob")):
        raise guard.CheckError(f"Story snapshot {path} must be a regular Markdown file.")
    if kind != "blob":
        return None
    return git("cat-file", "blob", oid)


def is_story(path: str | None, text: str | None) -> bool:
    """Require conventional specs even if their baseline metadata was removed."""
    if path is None or not path.endswith(".md"):
        return False
    name = Path(path).name
    fields = guard.read_frontmatter(text or "")
    return name.startswith("spec-") or bool(re.match(r"\d+-\d+-", name)) or "baseline_commit" in fields


def touched_stories(base: str, head: str) -> list[set[str]]:
    """Retain story identities across every event commit, including net-zero edits."""
    identities: dict[str, set[str]] = {}
    marked: set[str] = set()
    for record in git("rev-list", "--reverse", "--parents", f"{base}..{head}").splitlines():
        commit, *parents = record.split()
        if not parents:
            raise guard.CheckError("Event includes an unrelated root commit; no audit parent is available.")
        parent = parents[0]
        for old_path, path in changed_paths(parent, commit):
            names = {name for name in (old_path, path) if name is not None}
            group = names | set().union(*(identities.get(name, set()) for name in names))
            for name in group:
                identities[name] = group
            if is_story(old_path, snapshot(parent, old_path)) or is_story(path, snapshot(commit, path)):
                marked.update(names)
    groups = {frozenset(identities[name]) for name in marked}
    return [set(group) for group in sorted(groups, key=lambda group: sorted(group))]


def event_moves_gitlinks(base: str, head: str) -> bool:
    """Inspect every event commit, including merges and move/revert sequences."""
    raw = git(
        "log", "--format=", "--raw", "--no-abbrev", "--no-renames",
        "--diff-merges=first-parent", f"{base}..{head}", "--", "references/",
    )
    return any(
        line.startswith(":") and "160000" in [mode.lstrip(":") for mode in line.split()[:2]]
        for line in raw.splitlines()
    )


def require_start_identity(text: str, names: set[str], path: str, baseline: str) -> None:
    """Retain the legacy baseline check for literal paths, including control bytes."""
    if guard.story_owned_commits(text) is None:
        touched = git(
            "show", "--format=", "--name-only", "-z", "--diff-merges=first-parent",
            baseline, "--", *sorted(names),
        )
        if touched.strip("\0\n"):
            raise guard.CheckError(f"{path}: mid-story baseline already touches a recorded story identity.")


def verify_baseline_correction(
    text: str, names: set[str], path: str, baseline: str, endpoint: str, head: str,
) -> str:
    """Permit only the exact parent of an independently verified sole creation."""
    fields = guard.read_frontmatter(text)
    original = fields["gitlink_original_baseline"]
    if not re.fullmatch(r"(?:[0-9a-f]{40}|[0-9a-f]{64})", original):
        raise guard.CheckError(f"{path}: original baseline must be a full canonical commit ID.")
    available = subprocess.run(
        ["git", "cat-file", "-e", original], cwd=REPO_ROOT,
        capture_output=True, check=False,
    )
    if available.returncode != 1:
        raise guard.CheckError(f"{path}: baseline correction requires an unavailable original object.")
    creation = canonical_commit(fields["gitlink_creation_ref"])
    canonical_commit(fields["baseline_commit"])
    require_ancestor(creation, endpoint)

    # Inspect all history for each literal identity, not just --follow's latest
    # incarnation. Expand recognized renames and refuse deletion/recreation or
    # overlapping identities rather than choosing a plausible creation.
    changes: dict[str, list[tuple[str | None, str | None]]] = {}
    parents_by_commit: dict[str, list[str]] = {}
    while True:
        commits = git(
            "log", "--full-history", "--format=%H", "--diff-merges=first-parent", "--no-patch",
            head, "--", *sorted(names),
        ).split()
        for commit in commits:
            if commit in changes:
                continue
            parents = git("show", "-s", "--format=%P", commit).split()
            if not parents:
                raise guard.CheckError(f"{path}: original creation has no single audit parent.")
            parents_by_commit[commit] = parents
            changes[commit] = changed_paths(parents[0], commit, artifacts_only=False)
        expanded = names | {
            name for pairs in changes.values() for pair in pairs
            if names.intersection(pair) for name in pair if name
        }
        if expanded == names:
            break
        names.update(expanded)
    creations = set()
    for commit, pairs in changes.items():
        renames = [pair for pair in pairs if None not in pair and pair[0] != pair[1] and names.intersection(pair)]
        if renames and (len(renames) != 1 or any(old != new and (old, new) != renames[0] for old, new in pairs)):
            raise guard.CheckError(f"{path}: baseline correction has ambiguous rename candidates.")
        for old, new in pairs:
            if old in names and new is None:
                raise guard.CheckError(f"{path}: baseline correction refuses deleted/recreated identities.")
            # A merge can introduce a branch-created path relative to its first
            # parent without creating that identity a second time.
            if old is None and new in names and all(
                snapshot(parent, new, regular=True) is None for parent in parents_by_commit[commit]
            ):
                if any(source and source.endswith(".md") and source != target for source, target in pairs):
                    raise guard.CheckError(f"{path}: baseline correction has competing Markdown sources at original creation.")
                creations.add(commit)
        for revision in (*parents_by_commit[commit], commit):
            if sum(snapshot(revision, name, regular=True) is not None for name in names) > 1:
                raise guard.CheckError(f"{path}: baseline correction has ambiguous story identities.")
    if creations != {creation}:
        raise guard.CheckError(f"{path}: gitlink_creation_ref must be the sole original story creation.")
    if parents_by_commit[creation] != [baseline]:
        raise guard.CheckError(f"{path}: corrected baseline must be the exact single parent of story creation.")
    for revision in (creation, endpoint):
        originals = [snapshot(revision, name, regular=True) for name in sorted(names)]
        originals = [value for value in originals if value is not None]
        if len(originals) != 1 or guard.read_frontmatter(originals[0]).get("baseline_commit") != original:
            raise guard.CheckError(f"{path}: correction must preserve the original creation and endpoint baseline.")
    stated = guard.stated_targets(text)
    for dependency, _, _ in guard.gitlink_changes(baseline, endpoint):
        if dependency not in stated:
            raise guard.CheckError(f"{path}: UNSTATED historical correction target for {dependency}.")
    return original


def audit_ref(text: str, names: set[str], path: str, base: str, head: str, moved: bool) -> str:
    """Bind an explicit historical endpoint without hiding current pointer work."""
    fields = guard.read_frontmatter(text)
    correction_fields = {"gitlink_original_baseline", "gitlink_creation_ref"}
    correction = bool(correction_fields.intersection(fields))
    if correction and not (correction_fields | {"gitlink_audit_ref"}).issubset(fields):
        raise guard.CheckError(f"{path}: baseline correction requires original baseline, creation and explicit endpoint metadata.")
    baseline = guard.resolve_baseline(Path(path), text)
    require_ancestor(baseline, base)
    require_start_identity(text, names, path, baseline)
    for name in names:
        old = snapshot(base, name)
        if old is not None and guard.read_frontmatter(old).get("baseline_commit"):
            if correction and guard.read_frontmatter(old)["baseline_commit"] == fields["gitlink_original_baseline"]:
                continue  # Independently verify this unavailable provenance below.
            previous_baseline = guard.resolve_baseline(Path(name), old)
            # Corrections can broaden the audit, never narrow away existing work.
            require_ancestor(baseline, previous_baseline)
    if "gitlink_audit_ref" not in fields:
        return head
    endpoint = canonical_commit(fields["gitlink_audit_ref"])
    require_ancestor(endpoint, base)
    if moved:
        raise guard.CheckError(
            f"{path}: historical gitlink_audit_ref cannot be combined with gitlink-bearing "
            "event commits. Isolate the historical repair from dependency changes."
        )
    # Follow each identity backwards through historical renames, rather than
    # filtering only by its current name (which can omit intermediate work).
    commits: set[str] = set()
    for name in names:
        commits.update(git(
            "log", "--follow", "--format=%H", "--diff-merges=first-parent", "--no-patch",
            f"{endpoint}..{base}", "--", name,
        ).split())
    historical_changes = []
    for commit in sorted(commits):
        parents = git("show", "-s", "--format=%P", commit).split()
        if not parents:
            raise guard.CheckError(f"{path}: historical identity has no audit parent.")
        historical_changes.extend(changed_paths(parents[0], commit))
        if event_moves_gitlinks(parents[0], commit):
            raise guard.CheckError(f"{path}: historical endpoint omits later story-associated gitlink commit {commit}.")
    while True:
        expanded = names | {name for pair in historical_changes if names.intersection(pair) for name in pair if name}
        if expanded == names:
            break
        names.update(expanded)
    if any(old is None and new in names for old, new in historical_changes):
        raise guard.CheckError(f"{path}: historical story identity was recreated after the endpoint.")
    require_start_identity(text, names, path, baseline)
    previous = next((value for name in sorted(names) if (value := snapshot(endpoint, name)) is not None), None)
    if previous is None:
        raise guard.CheckError(f"{path}: story does not exist at historical endpoint {endpoint}.")
    stated_baseline = fields.get("baseline_commit", "")
    previous_baseline = guard.read_frontmatter(previous).get("baseline_commit", "")
    expected_baseline = (
        verify_baseline_correction(text, names, path, baseline, endpoint, head)
        if correction else stated_baseline
    )
    if not stated_baseline or expected_baseline != previous_baseline:
        raise guard.CheckError(f"{path}: historical audit must retain the endpoint story's baseline_commit.")
    require_ancestor(baseline, endpoint)
    return endpoint


def check_range(base: str, head: str) -> int:
    """Audit all selected snapshots and aggregate failures rather than stopping early."""
    print(f"Event range: {base}..{head}", flush=True)
    moved = event_moves_gitlinks(base, head)
    selected = failed = 0
    for names in touched_stories(base, head):
        selected += 1
        label = ", ".join(sorted(names))
        try:
            surviving = [(name, snapshot(head, name, regular=True)) for name in sorted(names)]
            surviving = [(name, text) for name, text in surviving if text is not None]
            if not surviving:
                raise guard.CheckError(f"Removed story {label} cannot be audited; preserve its Markdown identity.")
            refused = False
            for path, current in surviving:
                endpoint = audit_ref(current, names, path, base, head, moved)
                print(f"Audit: {path} at {endpoint}", flush=True)
                # Preserve every known basename, so a rename cannot erase the
                # original production guard's mid-story-baseline heuristic.
                for basename in sorted({Path(name).name for name in names}):
                    with tempfile.TemporaryDirectory(prefix="story-gitlink-ci-") as directory:
                        story = Path(directory) / basename
                        story.write_text(current, encoding="utf-8")
                        result = subprocess.run(
                            [sys.executable, str(GUARD_PATH), str(story), "--ref", endpoint],
                            cwd=REPO_ROOT, check=False,
                        )
                        refused |= result.returncode != 0
            failed += refused
        except (guard.CheckError, UnicodeError) as error:
            failed += 1
            print(f"FAIL: {label}: {error}", flush=True)
    print(f"Changed stories: {selected}; failed: {failed}", flush=True)
    return 1 if failed else 0


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--event-path", type=Path, default=os.environ.get("GITHUB_EVENT_PATH"))
    parser.add_argument("--event-name", default=os.environ.get("GITHUB_EVENT_NAME"))
    args = parser.parse_args()
    try:
        if args.event_path is None:
            raise guard.CheckError("GITHUB_EVENT_PATH or --event-path is required.")
        event = json.loads(args.event_path.read_text(encoding="utf-8"))
        return check_range(*event_range(event, args.event_name))
    except (guard.CheckError, OSError, ValueError, KeyError, TypeError) as error:
        print(f"RESULT: FAIL\n{error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
