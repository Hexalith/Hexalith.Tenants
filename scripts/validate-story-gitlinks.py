#!/usr/bin/env python3
"""Verify a story declares or reverts every root-declared submodule pointer change.

Story commits must not silently carry `references/` gitlink bumps. This check
diffs the submodule pointers between the story's recorded `baseline_commit` and
the current tree, then requires every drifted pointer to be declared in the
story's File List (or Completion Notes). Undeclared drift fails the check.

Declaring a path is necessary but not sufficient. Where the story also states a
target SHA for that path, the stated SHA must match the tree: a story whose own
pointer table has gone stale asserts a state that is not the one it ships, and
naming the path alone let that pass. Stated SHAs are matched by prefix, so both
short and full forms are accepted.

The check is fail-closed: a story with no usable baseline cannot prove anything
about its own gitlinks, so a missing baseline is a failure, not a pass.
"""

from __future__ import annotations

import argparse
import re
import subprocess
import sys
import unicodedata
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


def extract_section(story_text: str, heading: str, stop_at_subheading: bool = False) -> str:
    """Return the body of a markdown section, or an empty string when absent.

    By default the body runs until the next heading of the same or higher level,
    so nested subsections stay inside it. `stop_at_subheading` ends the body at the
    very next heading of any level instead.

    That stricter form is used for `## Completion Notes List` only. A long-lived
    story accumulates `### Review Findings` subsections there, and those carry
    retained historical pointer tables the story explicitly labels superseded.
    Swallowing them made the newest text in the file a stale table, so a last-wins
    reading picked the wrong SHA and failed a correct tree.

    `## File List` deliberately does NOT use it. A story is free to group its File
    List under `### Source` / `### Tests` headings, and terminating at the first of
    them dropped every entry below: a correctly declared bump then read as
    UNDECLARED and failed a correct tree, while a misstated SHA in that region
    passed unchecked. The guard must not be sensitive to a cosmetic heading edit.
    """
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
            if stop_at_subheading or level <= depth:
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


def normalize_line(line: str) -> str:
    """Drop invisible format controls and fold every separator to a plain space.

    Applied once, to the whole line, before either the path tokenizer or the arrow
    scan reads it -- so both agree on where a token starts and ends.

    Two classes of character are handled, and they are handled differently:

    * Category `Cf` (zero-width space, joiners, bidi controls) carries no width. It
      is invisible decoration pasted from a browser or editor, never part of a path
      or a SHA, so it is removed. Left in, `references/X<ZWSP>` became a phantom key
      that could never match a real gitlink while the report still printed the line
      as declared.
    * Categories `Zs`/`Zl`/`Zp` and the ASCII tab/vertical-tab/form-feed are
      separators the eye reads as a space but `str.split(" ")` and `\\s` do not.
      NBSP is the common one -- Word and Confluence paste it freely. They are folded
      to U+0020 rather than removed, because they really are token boundaries.

    Removing `Cf` shortens the line, which is why normalization happens before
    tokenizing rather than per-token: the offsets the arrow scan and the tokenizer
    compare are then indices into the same string.
    """
    normalized: list[str] = []
    for character in line:
        category = unicodedata.category(character)
        if category == "Cf":
            continue
        normalized.append(" " if category in ("Zs", "Zl", "Zp") or character in "\t\v\f" else character)
    return "".join(normalized)


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
        extract_section(story_text, "Completion Notes List", stop_at_subheading=True),
    )
    for section in sections:
        for line in section.splitlines():
            entry = normalize_line(line).strip()
            if not entry.startswith(("-", "*", "+")):
                continue
            entry = entry[1:].strip()
            if entry.startswith("[") and "]" in entry:
                entry = entry.split("]", 1)[1].strip()
            entry = entry.strip("`").strip()
            if entry.startswith(SUBMODULE_ROOT) and " " not in entry:
                declared.add(entry.rstrip("/"))
    return declared


TOKEN_TRIM = ",.;:()[]<>\"'|*"

# The right-hand side of the last hop in a chain. `(?:...)+` keeps the final
# repetition, so `a -> b -> c` states c, not b: a story correcting its own record
# in place ships the last SHA it names, and reading the first hop failed a tree
# that was right. `–>` (en dash) is accepted alongside `->`, `—>` and `→`.
ARROW_CHAIN = re.compile(
    r"([0-9a-fA-F]{7,40})(?:\s*(?:-+>|—>|–>|→)\s*([0-9a-fA-F]{7,40}))+"
)


def _path_tokens(normalized: str) -> list[tuple[int, str]]:
    """Return (offset, path) for every `references/` token on an already-normalized line.

    The caller must pass a line through `normalize_line` first. Offsets are indices
    into that same string, which is what lets `stated_targets` match an arrow chain
    to the path token preceding it.
    """
    found: list[tuple[int, str]] = []
    offset = 0
    for token in normalized.split(" "):
        bare = token.strip(TOKEN_TRIM).rstrip("/")
        if bare.startswith(SUBMODULE_ROOT):
            found.append((offset, bare))
        offset += len(token) + 1
    return found


def stated_targets(story_text: str) -> dict[str, str]:
    """Return the target SHA each `X -> Y` statement claims for a `references/` path.

    Only the arrow form is read, and only its right-hand side: that is the shape
    the story's own pointer tables use to state where a submodule ended up, and it
    is the half that goes stale when a later commit moves the pointer again.

    Three rules make this match how stories are actually written:

    * Only the File List and Completion Notes List are read, exactly as
      `declared_paths` does. Prose, fenced command examples and retained
      historical tables elsewhere in a story are not binding claims -- reading the
      whole document failed stories whose tree was correct because a paragraph
      recorded where a pointer used to be, or explicitly disowned a bump.
    * The last statement for a path wins. Stories correct their own record in
      place and keep the superseded table above it as history; a set union matched
      with `any()` meant one correct row exonerated every stale row for that path.
    * A line naming several pointers states one target for each, matched to the
      nearest preceding path. Skipping such lines as ambiguous made the check
      invisible on the multi-pointer bump format that motivated it.
    """
    targets: dict[str, str] = {}
    sections = (
        extract_section(story_text, "File List"),
        extract_section(story_text, "Completion Notes List", stop_at_subheading=True),
    )
    for section in sections:
        for line in section.splitlines():
            # Normalized once, here, so the arrow scan below reads the same string the tokenizer does.
            # Scanning the raw line instead meant a `Cf` character next to the arrow -- `53d53ae<ZWSP> -> x`
            # -- produced no match at all, because `\s` does not match U+200B. `stated_targets` then recorded
            # nothing for that path, `record_if_misstated` was never called, and the run printed `[declared]`
            # and PASSED while the stated SHA was stale: the exact failure this check exists to catch,
            # reachable by an invisible character.
            cleaned = normalize_line(line).replace("`", " ").replace("|", " ")
            paths = _path_tokens(cleaned)
            if not paths:
                continue
            for match in ARROW_CHAIN.finditer(cleaned):
                owner = None
                for offset, path in paths:
                    if offset <= match.start():
                        owner = path
                    else:
                        break
                # An arrow chain before the first path on the line still belongs to
                # that path -- "moved to X, see references/Y" reads that way.
                targets[owner or paths[0][1]] = match.group(2).lower()
    return targets


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


def current_pointer(path: str, ref: str | None) -> str | None:
    """Return the submodule commit currently recorded for `path`, or None.

    Needed for paths a story states a target for but never moved. The changed-
    pointer diff cannot see those, so a story claiming a bump it never made -- or
    made and then reverted to baseline -- passed with "no pointer changes".
    """
    try:
        if ref is None:
            return run_git("-C", path, "rev-parse", "HEAD").strip()[:7]
        raw = run_git("ls-tree", ref, "--", path)
    except CheckError:
        return None
    for line in raw.splitlines():
        fields = line.split()
        if len(fields) >= 3 and fields[1] == "commit":
            return fields[2][:7]
    return None


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

    compare_ref = None if compare_worktree else head
    changes = gitlink_changes(baseline, compare_ref)
    declared = declared_paths(story_text)
    stated = stated_targets(story_text)

    undeclared: list[tuple[str, str, str]] = []
    misstated: list[tuple[str, str, str]] = []

    def record_if_misstated(path: str, actual: str) -> None:
        """Declaring the path is not enough: the stated `to` must be the tree SHA.

        Naming the path while the table records a superseded target is exactly how
        this record went stale. Stated SHAs are matched by prefix, so both short
        and full forms are accepted.
        """
        claim = stated.get(path)
        if claim and not (actual.startswith(claim) or claim.startswith(actual)):
            misstated.append((path, actual, claim))

    if changes:
        print(f"\n{len(changes)} references/ pointer change(s) in range:")
        for path, old_sha, new_sha in changes:
            is_declared = path in declared
            marker = "declared" if is_declared else "UNDECLARED"
            print(f"  [{marker}] {path}  {describe_change(old_sha, new_sha)}")
            if not is_declared:
                undeclared.append((path, old_sha, new_sha))
                continue
            record_if_misstated(path, new_sha)
    else:
        print("\nNo references/ pointer changes in range.")

    # A stated target for a pointer that did not move is still a claim about the
    # tree, and the diff above cannot see it. Without this a story could assert a
    # bump it never made, or one it made and reverted to baseline, and pass.
    changed_paths = {path for path, _, _ in changes}
    for path in sorted(stated):
        if path in changed_paths:
            continue
        actual = current_pointer(path, compare_ref)
        if actual is not None:
            record_if_misstated(path, actual)

    for path, actual, claim in misstated:
        print(
            f"\n  [MISSTATED] {path} is {actual} in the tree, "
            f"but the story states {claim}."
        )

    for warning in warnings:
        print(f"\nWARNING: {warning}")

    if misstated and not undeclared:
        print("\nRESULT: FAIL")
        print(
            "\nEvery stated pointer SHA must be the one this story ships. A declaration that\n"
            "names the right path but the wrong commit asserts a state the tree does not have.\n"
            "Update the story's pointer record to the tree values above, or restore the tree to\n"
            "the values the story states."
        )
        return EXIT_FAIL

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
