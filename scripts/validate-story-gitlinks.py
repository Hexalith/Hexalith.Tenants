#!/usr/bin/env python3
"""Verify a story declares or reverts every root-declared submodule pointer change.

Story commits must not silently carry `references/` gitlink bumps. This check
diffs the submodule pointers between the story's recorded `baseline_commit` and
the current tree, then requires every drifted pointer to be declared in the
story's File List (or Completion Notes). Undeclared drift fails the check.

The check is fail-closed: a story with no usable baseline cannot prove anything
about its own gitlinks, so a missing baseline is a failure, not a pass.
"""

from __future__ import annotations

import argparse
import subprocess
import sys
from pathlib import Path

SCRIPT_PATH = Path(__file__).resolve()
REPO_ROOT = SCRIPT_PATH.parents[1]

SUBMODULE_ROOT = "references/"
NO_VCS = "NO_VCS"

EXIT_PASS = 0
EXIT_FAIL = 1
EXIT_USAGE = 2


class CheckError(Exception):
    """A condition that prevents the check from producing a verdict."""


def run_git(*args: str) -> str:
    result = subprocess.run(
        ["git", *args],
        cwd=REPO_ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    if result.returncode != 0:
        raise CheckError(f"git {' '.join(args)} failed: {result.stderr.strip()}")
    return result.stdout


def read_frontmatter(story_text: str) -> dict[str, str]:
    """Parse the leading `---` delimited block as flat `key: value` pairs."""
    lines = story_text.splitlines()
    if not lines or lines[0].strip() != "---":
        return {}
    fields: dict[str, str] = {}
    for line in lines[1:]:
        if line.strip() == "---":
            break
        key, separator, value = line.partition(":")
        if separator and not key.startswith((" ", "\t", "#")):
            fields[key.strip()] = value.strip().strip("'\"")
    return fields


def extract_section(story_text: str, heading: str) -> str:
    """Return the body of a markdown section, or an empty string when absent."""
    lines = story_text.splitlines()
    body: list[str] = []
    depth = 0
    for line in lines:
        stripped = line.strip()
        if depth == 0:
            if stripped.lstrip("#").strip().lower() == heading.lower() and stripped.startswith("#"):
                depth = len(stripped) - len(stripped.lstrip("#"))
            continue
        if stripped.startswith("#"):
            level = len(stripped) - len(stripped.lstrip("#"))
            if level <= depth:
                break
        body.append(line)
    return "\n".join(body)


def is_null_sha(sha: str) -> bool:
    return set(sha) == {"0"}


def describe_change(old_sha: str, new_sha: str) -> str:
    if is_null_sha(old_sha):
        return f"absent at baseline -> {new_sha}"
    if is_null_sha(new_sha):
        return f"{old_sha} -> removed"
    return f"{old_sha} -> {new_sha}"


def declared_paths(story_text: str) -> set[str]:
    """Return paths declared as list entries in File List / Completion Notes.

    A declaration is a list item whose entire content is the path. Prose that
    merely mentions a path does not declare it -- a sentence such as
    "`references/X` was left untouched" is a denial, and matching it as a
    declaration would pass exactly the drift this check exists to catch.
    """
    declared: set[str] = set()
    sections = (
        extract_section(story_text, "File List"),
        extract_section(story_text, "Completion Notes List"),
    )
    for section in sections:
        for line in section.splitlines():
            entry = line.strip()
            if not entry.startswith(("-", "*", "+")):
                continue
            entry = entry[1:].strip()
            if entry.startswith("[") and "]" in entry:
                entry = entry.split("]", 1)[1].strip()
            entry = entry.strip("`").strip()
            if entry.startswith(SUBMODULE_ROOT) and " " not in entry:
                declared.add(entry.rstrip("/"))
    return declared


def gitlink_changes(baseline: str, ref: str | None) -> list[tuple[str, str, str]]:
    """Return (path, old_sha, new_sha) for every changed pointer under references/.

    When `ref` is None the comparison runs against the working tree, so pointers
    that moved but are not yet committed are caught too. Those are the dangerous
    ones at completion time: a later `git add -A` sweeps them into an unrelated
    commit, which is exactly how this drift keeps happening.
    """
    diff_args = ["diff", "--ignore-submodules=dirty", "--raw", baseline]
    if ref is not None:
        diff_args.append(ref)
    raw = run_git(*diff_args, "--", SUBMODULE_ROOT)
    changes: list[tuple[str, str, str]] = []
    for line in raw.splitlines():
        if not line.startswith(":"):
            continue
        meta, _, path = line.partition("\t")
        fields = meta.split()
        if len(fields) < 4:
            continue
        old_mode, new_mode, old_sha, new_sha = fields[0].lstrip(":"), fields[1], fields[2], fields[3]
        if "160000" not in (old_mode, new_mode):
            continue
        path = path.strip()
        # Against the working tree, --raw reports a null new SHA for a submodule
        # whose checked-out commit moved. Resolve the real commit so the report
        # says "moved to X" instead of the misleading "removed".
        if ref is None and is_null_sha(new_sha) and new_mode == "160000":
            try:
                new_sha = run_git("-C", path, "rev-parse", "HEAD").strip()
            except CheckError:
                pass
        changes.append((path, old_sha[:7], new_sha[:7]))
    return sorted(changes)


def baseline_is_mid_story(baseline: str, story_key: str) -> bool:
    """True when the baseline commit itself touched this story's own artifacts.

    A baseline that already contains story work narrows the diff and can hide
    drift that landed earlier in the story.
    """
    try:
        touched = run_git("show", "--pretty=format:", "--name-only", baseline)
    except CheckError:
        return False
    return any(story_key in line for line in touched.splitlines() if line.strip())


def resolve_baseline(story_path: Path, story_text: str) -> str:
    fields = read_frontmatter(story_text)
    baseline = fields.get("baseline_commit", "").strip()
    if not baseline:
        raise CheckError(
            f"{story_path.name} has no `baseline_commit` in its frontmatter.\n"
            "  The gitlink check cannot prove which pointer changes belong to this story.\n"
            "  Record the story's starting commit as `baseline_commit:` in the frontmatter,\n"
            "  or explicitly declare every references/ pointer this story intends to change."
        )
    if baseline == NO_VCS:
        raise CheckError(
            f"{story_path.name} records `baseline_commit: {NO_VCS}`, so pointer drift is unprovable."
        )
    try:
        return run_git("rev-parse", "--verify", f"{baseline}^{{commit}}").strip()
    except CheckError as error:
        raise CheckError(f"baseline_commit `{baseline}` is not a commit in this repository.") from error


def check(story_path: Path, ref: str) -> int:
    story_text = story_path.read_text(encoding="utf-8")
    story_key = story_path.stem

    baseline = resolve_baseline(story_path, story_text)
    head = run_git("rev-parse", "--verify", f"{ref}^{{commit}}").strip()

    ancestor = subprocess.run(
        ["git", "merge-base", "--is-ancestor", baseline, head],
        cwd=REPO_ROOT,
        capture_output=True,
        check=False,
    )
    if ancestor.returncode != 0:
        raise CheckError(
            f"baseline_commit {baseline[:7]} is not an ancestor of {ref} ({head[:7]}).\n"
            "  The recorded baseline does not describe this working tree."
        )

    # Default runs include the working tree; an explicit --ref audits that commit only.
    compare_worktree = ref == "HEAD"
    print(f"Story:    {story_path.name}")
    print(f"Baseline: {baseline[:7]}")
    print(f"Compared: {ref} ({head[:7]}){' + working tree' if compare_worktree else ''}")

    warnings: list[str] = []
    if baseline_is_mid_story(baseline, story_key):
        warnings.append(
            f"baseline_commit {baseline[:7]} already touches this story's own files, so it is a\n"
            "  mid-story baseline. Pointer changes made earlier in the story are outside this diff."
        )

    changes = gitlink_changes(baseline, None if compare_worktree else head)
    if not changes:
        print("\nNo references/ pointer changes in range.")
        for warning in warnings:
            print(f"\nWARNING: {warning}")
        print("\nRESULT: PASS")
        return EXIT_PASS

    declared = declared_paths(story_text)

    undeclared: list[tuple[str, str, str]] = []
    print(f"\n{len(changes)} references/ pointer change(s) in range:")
    for path, old_sha, new_sha in changes:
        is_declared = path in declared
        marker = "declared" if is_declared else "UNDECLARED"
        print(f"  [{marker}] {path}  {describe_change(old_sha, new_sha)}")
        if not is_declared:
            undeclared.append((path, old_sha, new_sha))

    for warning in warnings:
        print(f"\nWARNING: {warning}")

    if undeclared:
        print("\nRESULT: FAIL")
        print(
            "\nEvery submodule pointer this story moves must be a deliberate, stated change.\n"
            "For each UNDECLARED entry above, do one of:\n"
            "  DECLARE — add the path to the story's File List and say in the Completion Notes\n"
            "            why the bump belongs to this story.\n"
            "  REVERT  — restore the baseline pointer, then commit the bump separately:\n"
        )
        for path, old_sha, _ in undeclared:
            if is_null_sha(old_sha):
                print(f"    # {path}: absent at baseline — confirm the baseline is the story's true start")
            else:
                print(f"    git checkout {old_sha} -- {path}")
        return EXIT_FAIL

    print("\nRESULT: PASS")
    return EXIT_PASS


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Verify a story declares or reverts every references/ submodule pointer change.",
    )
    parser.add_argument("story", type=Path, help="Path to the story markdown file")
    parser.add_argument(
        "--ref",
        default="HEAD",
        help="Commit or ref to compare the baseline against (default: HEAD)",
    )
    args = parser.parse_args()

    story_path = args.story if args.story.is_absolute() else (Path.cwd() / args.story)
    story_path = story_path.resolve()
    if not story_path.is_file():
        sys.stderr.write(f"error: story file not found: {args.story}\n")
        return EXIT_USAGE

    try:
        return check(story_path, args.ref)
    except CheckError as error:
        print(f"Story:    {story_path.name}")
        print(f"\nRESULT: FAIL\n\n{error}")
        return EXIT_FAIL


if __name__ == "__main__":
    sys.exit(main())
