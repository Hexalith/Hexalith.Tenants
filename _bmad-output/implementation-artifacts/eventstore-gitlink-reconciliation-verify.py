"""Reproduce the EventStore historical audit without mutating Git or dependencies.

Run from any directory with Python 3. The fixed historical endpoint makes the
evidence reproducible; the separate default CLI command checks today's worktree.
"""

from contextlib import redirect_stdout
import hashlib
import importlib.util
import io
import json
from pathlib import Path
import subprocess
import tempfile
import zipfile


ROOT = Path(__file__).resolve().parents[2]
ARTIFACTS = Path(__file__).resolve().parent
MANIFEST = ARTIFACTS / "eventstore-gitlink-reconciliation-2026-10-29.json"
STORY = "_bmad-output/implementation-artifacts/spec-eventstore-3-117-1.md"
SCOPED_FILES = (STORY, "_bmad-output/implementation-artifacts/deferred-work.md",
                "_bmad-output/project-context.md")


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def verify():
    evidence = json.loads(MANIFEST.read_text())
    expected_artifacts = set()
    for version, date, kinds in (
        ("3.117.0", "2026-10-08", ("evidence", "final-evidence", "raw-logs")),
        ("3.117.1", "2026-10-08", ("evidence", "raw-logs")),
        ("3.120.0", "2026-10-10", ("evidence", "raw-logs")),
        ("3.128.4", "2026-10-29", ("evidence", "raw-logs")),
    ):
        for kind in kinds:
            suffix = "zip" if kind == "raw-logs" else "json"
            expected_artifacts.add(f"_bmad-output/implementation-artifacts/dependency-refresh-eventstore-{version}-{kind}-{date}.{suffix}")
    artifacts = evidence["packageVerificationEvidence"]
    require(len(artifacts) == len(expected_artifacts) and {row["path"] for row in artifacts} == expected_artifacts,
            "complete nine-artifact preservation inventory")
    require(evidence["story"] == STORY, "literal story identity")
    expected_association = {STORY} | {path for path in expected_artifacts if "3.117.0-" not in path}
    require(len(evidence["associationPaths"]) == len(expected_association)
            and set(evidence["associationPaths"]) == expected_association, "complete association filter")
    validator_files = {"scripts/validate-story-gitlinks.py", "scripts/validate-changed-story-gitlinks.py"}
    require(set(evidence["validatorFiles"]) == validator_files, "both validator identities")
    for path in validator_files:
        committed = subprocess.check_output(["git", "show", evidence["entryCommit"] + ":" + path], cwd=ROOT)
        require((ROOT / path).read_bytes() == committed
                and hashlib.sha256(committed).hexdigest() == evidence["validatorFiles"][path], "entry validator identity: " + path)

    validation = evidence["validation"]
    raw = validation["rawArchive"]
    archive_path = ROOT / raw["path"]
    require(hashlib.sha256(archive_path.read_bytes()).hexdigest() == raw["sha256"], "reconciliation archive hash")
    with zipfile.ZipFile(archive_path) as archive:
        require(archive.testzip() is None, "reconciliation archive CRC")
        names = archive.namelist()
        require(len(names) == len(set(names)), "unique archive entries")
        command_names = {"default-before", "retired-endpoint-before", "default-after", "reproduce", "whitespace",
                         "guard-suite", "ci-suite", "pointer-history", "story-history"}
        commands = validation["commands"]
        require(len(commands) == len(command_names) and {row["name"] for row in commands} == command_names,
                "complete command-log inventory")
        required_entries = {"entry-worktree.json", "preservation.json"}
        required_entries |= {phase + "/" + path for phase in ("before", "after") for path in SCOPED_FILES}
        required_entries |= {"commands/" + name + ".log" for name in command_names}
        require(required_entries <= set(names), "all command and original snapshot entries")
        for command in commands:
            require(command["archiveEntry"] == "commands/" + command["name"] + ".log", "canonical log entry")
            require(hashlib.sha256(archive.read(command["archiveEntry"])).hexdigest() == command["sha256"], "command log digest")
            require(command["exitCode"] == (1 if command["name"] == "default-before" else 0), "recorded exit result")
        entry = json.loads(archive.read("entry-worktree.json"))
        require(entry["entry"] == evidence["entryCommit"] and entry["files"] == evidence["initialWorktreeFileHashes"], "entry snapshot binding")
        require(entry["indexSha256"] == evidence["initialIndexSha256"] and entry["gitlinks"] == evidence["initialGitlinks"], "entry index binding")
        before = {path: archive.read("before/" + path) for path in SCOPED_FILES}
        for path, content in before.items():
            require(hashlib.sha256(content).hexdigest() == entry["files"][path], "original snapshot bytes: " + path)
        report_binding = validation["preservationReport"]
        require(report_binding["archiveEntry"] == "preservation.json", "preservation report entry")
        report_bytes = archive.read("preservation.json")
        require(hashlib.sha256(report_bytes).hexdigest() == report_binding["sha256"], "preservation report digest")
        report = json.loads(report_bytes)
        require(report["head"] == entry["entry"] and report["indexSha256"] == entry["indexSha256"]
                and report["gitlinks"] == entry["gitlinks"] and report["stagedDiff"] == "", "observed Git state preservation")
        after = {path: archive.read("after/" + path) for path in SCOPED_FILES}
        require(set(report["afterHashes"]) == set(SCOPED_FILES), "complete after-snapshot inventory")
        for path, content in after.items():
            require(hashlib.sha256(content).hexdigest() == report["afterHashes"][path], "after snapshot binding")
        ledger = SCOPED_FILES[1]
        lines = after[ledger].splitlines(keepends=True)
        resolution = [line for line in lines if line.startswith("  resolution: done — Restored the creation snapshot".encode())]
        require(len(resolution) == 1 and b"".join(line for line in lines if line not in resolution) == before[ledger], "exact ledger preservation")
        old_context = "The retained historical spec gitlink check still requires reconciliation of intervening changes; no baseline reset or gitlink movement is performed."
        new_context = "The historical spec gitlink audit is reconciled from its restored original October 8 baseline, with all nine current range pointers explicitly inventoried and independent changes unclaimed; the default guard passes. See [reconciliation evidence](implementation-artifacts/eventstore-gitlink-reconciliation-2026-10-29.md); no dependency or gitlink movement is performed."
        context = SCOPED_FILES[2]
        require(before[context].count(old_context.encode()) == 1
                and before[context].replace(old_context.encode(), new_context.encode()) == after[context], "exact project-context preservation")

    guard = load("reconciliation_guard", ROOT / "scripts/validate-story-gitlinks.py")
    ci = load("reconciliation_ci", ROOT / "scripts/validate-changed-story-gitlinks.py")
    story = ROOT / evidence["story"]
    text = story.read_text()
    fields = guard.read_frontmatter(text)
    baseline, head = evidence["originalBaseline"], evidence["entryCommit"]
    for key in ("originalBaseline", "entryCommit", "creationCommit", "retiredNarrowedBaseline", "retiredAuditRef"):
        require(ci.canonical_commit(evidence[key]) == evidence[key], key)
    creation_text = git("show", evidence["creationCommit"] + ":" + evidence["story"])
    require(guard.read_frontmatter(creation_text)["baseline_commit"] == baseline, "original start")
    require(evidence["originalCreationBaseline"] == baseline, "recorded creation baseline")
    retired_text = git("show", evidence["retiredAuditRef"] + ":" + STORY)
    require(guard.read_frontmatter(retired_text)["baseline_commit"] == evidence["retiredNarrowedBaseline"], "retired endpoint baseline")
    entry_fields = guard.read_frontmatter(before[STORY].decode())
    require(entry_fields["gitlink_audit_ref"] == evidence["retiredAuditRef"]
            and entry_fields["baseline_commit"] == evidence["retiredNarrowedBaseline"], "entry provenance fields")
    require(fields["baseline_commit"] == baseline, "full original baseline must remain")
    require("gitlink_audit_ref" not in fields, "current audit must cover the full range")
    require(guard.story_owned_commits(text) is None, "independent commits cannot be claimed as owned")
    frozen = text.split("<frozen-after-approval", 1)[1].split("</frozen-after-approval>", 1)[0]
    require(hashlib.sha256(frozen.encode()).hexdigest() == evidence["frozenIntentSha256"], "frozen intent")
    # The target was renegotiated after creation; preserve the effective entry
    # intent, rather than restoring the superseded 3.117.1 creation intent.
    for anchor in (before[STORY].decode(), git("show", head + ":" + STORY)):
        require(frozen == anchor.split("<frozen-after-approval", 1)[1].split("</frozen-after-approval>", 1)[0], "Git-anchored effective frozen intent")
    # Existing sections remain intact; only the latest-run heading is qualified
    # as historical and new reconciliation material is inserted or appended.
    for heading in ("Implementation Notes", "File List", "Completion Notes List", "Review Triage Log"):
        old_section = guard.extract_section(before[STORY].decode(), heading)
        if heading == "Implementation Notes":
            old_section = old_section.replace("### 2026-10-29 — Latest-package continuation",
                                              "### 2026-10-29 — Latest-package continuation (pre-reconciliation observations)", 1)
        require(old_section in guard.extract_section(text, heading), "preserved previous section: " + heading)
    ci.require_ancestor(baseline, evidence["retiredNarrowedBaseline"])
    ci.require_ancestor(evidence["retiredNarrowedBaseline"], evidence["retiredAuditRef"])
    ci.require_ancestor(evidence["retiredAuditRef"], head)

    def changes(before, after):
        return [{"path": path, "before": old, "after": new}
                for path, old, new in guard.gitlink_changes(before, after)]

    for key, start, end in (
        ("originalToCreation", baseline, evidence["creationCommit"]),
        ("originalToRetiredEndpoint", baseline, evidence["retiredAuditRef"]),
        ("originalToEntry", baseline, head),
        ("narrowedBaselineToEntry", evidence["retiredNarrowedBaseline"], head),
    ):
        require(changes(start, end) == evidence[key], key + " Git-tree equality")

    history = []
    associated = set(evidence["associationPaths"])
    for commit in git("rev-list", "--first-parent", "--reverse", f"{baseline}..{head}").split():
        parent = git("rev-parse", commit + "^").strip()
        paths = git("diff", "--name-only", "--no-renames", parent, commit).splitlines()
        pointers = changes(parent, commit)
        matches = sorted(associated.intersection(paths))
        if pointers or matches:
            history.append((commit, parent, paths, pointers, matches))
    recorded = [(row["commit"], row["parent"], row["changedPaths"], row["gitlinks"], row["storyArtifacts"])
                for row in evidence["pointerHistory"]]
    require(history == recorded, "complete first-parent pointer and story history")
    for row in evidence["pointerHistory"]:
        require(row["subject"] == git("show", "-s", "--format=%s", row["commit"]).strip(), "Git subject")
        expected = ("mixed landing carrying this story and dependency pointers; association does not prove dependency necessity or assistant ownership"
                    if row["storyArtifacts"] and row["gitlinks"] else "story documentation/evidence only"
                    if row["storyArtifacts"] else "independent range contents; no changes to the named story artifacts; no story ownership claimed")
        require(row["attribution"] == expected, "qualified attribution category")
    # Check non-first-parent story edits too, so merged story work cannot vanish.
    all_story_commits = set(git("log", "--full-history", "--format=%H", "--diff-merges=first-parent",
                               f"{baseline}..{head}", "--", *sorted(associated)).split())
    require(all_story_commits == {row[0] for row in history if row[4]}, "all story-associated commits")
    targets = {row["path"]: row["after"] for row in evidence["originalToEntry"]}
    require(guard.stated_targets(text) == targets, "all nine exact target claims")
    require(set(targets) <= guard.declared_paths(text), "all nine declarations")
    require(ci.audit_ref(text, {evidence["story"]}, evidence["story"], head, head, False) == head,
            "CI uses current range and accepts broadened baseline")

    checks = 0
    with tempfile.TemporaryDirectory(prefix="eventstore-gitlink-controls-") as directory:
        candidate = Path(directory) / story.name

        def audit(content, revision, expected, diagnostic=""):
            nonlocal checks
            candidate.write_text(content)
            output = io.StringIO()
            with redirect_stdout(output):
                result = guard.check(candidate, revision)
            require(result == expected and diagnostic in output.getvalue(), output.getvalue())
            checks += 1

        audit(text, head, 0)
        audit(before[STORY].decode(), head, 1, "[UNDECLARED]")
        audit(before[STORY].decode(), evidence["retiredAuditRef"], 0)
        # Remove every declaration of a selected path, leaving its target claim.
        for path in targets:
            omitted = "\n".join(line for line in text.splitlines()
                                if line.strip() != f"- `{path}`") + "\n"
            audit(omitted, head, 1, f"[UNDECLARED] {path}")
            row = next(row for row in evidence["originalToEntry"] if row["path"] == path)
            good = f"| `{path}` | `{row['before']}` | `{row['after']}` |"
            bad = f"| `{path}` | `{row['before']}` | `{'f' * 40}` |"
            require(text.count(good) == 1, "unique target row")
            audit(text.replace(good, bad), head, 1, f"[MISSTATED] {path}")
        # Independently audit earlier windows with their own actual target trees.
        # These generated documents are controls, not rewritten historical files.
        for key, revision in (("originalToCreation", evidence["creationCommit"]),
                              ("originalToRetiredEndpoint", evidence["retiredAuditRef"])):
            rows = evidence[key]
            control = f"---\nbaseline_commit: '{baseline}'\n---\n\n## File List\n"
            control += "".join(f"- `{row['path']}`\n" for row in rows)
            control += "\n| Pointer | Before | After |\n| --- | --- | --- |\n"
            control += "".join(f"| `{row['path']}` | `{row['before']}` | `{row['after']}` |\n" for row in rows)
            audit(control, revision, 0)

    for artifact in artifacts:
        content = (ROOT / artifact["path"]).read_bytes()
        require(hashlib.sha256(content).hexdigest() == artifact["sha256"],
                "preserved package evidence: " + artifact["path"])
        if "3.128.4-" in artifact["path"]:
            require(artifact["sha256"] == entry["files"][artifact["path"]], "new package evidence entry binding")
        else:
            committed = subprocess.check_output(["git", "show", head + ":" + artifact["path"]], cwd=ROOT)
            require(content == committed, "historical package evidence Git anchor")
    package = json.loads((ARTIFACTS / "dependency-refresh-eventstore-3.128.4-evidence-2026-10-29.json").read_text())
    require(package["targetVersion"] == "3.128.4" and package["testTotals"] == {
        "total": 10887, "failed": 0, "succeeded": 10887, "skipped": 0}, "preserved passing package result")
    package_archive = package["rawArchive"]
    require(hashlib.sha256((ROOT / package_archive["path"]).read_bytes()).hexdigest() == package_archive["sha256"], "package log archive hash")
    with zipfile.ZipFile(ROOT / package_archive["path"]) as archive:
        require(archive.testzip() is None, "package archive CRC")
        require(hashlib.sha256(archive.read(package_archive["manifestArchiveEntry"])).hexdigest()
                == package_archive["manifestSha256"], "package archive manifest binding")
    for path in (MANIFEST, Path(__file__), ARTIFACTS / "eventstore-gitlink-reconciliation-2026-10-29.md"):
        content = path.read_bytes()
        require(content.endswith(b"\n"), "final newline: " + path.name)
        require(all(line.rstrip(b" \t") == line for line in content.splitlines()), "new-file whitespace: " + path.name)
    moves = sum(len(row[3]) for row in history)
    landings = sum(bool(row[3]) for row in history)
    print(f"PASS: original baseline; {landings} pointer landings / {moves} moves; all story-associated commits covered")
    print(f"PASS: nine declarations/targets; CI current-range contract; {checks} snapshot/control checks")
    print("PASS: Git-anchored frozen intent, validators, provenance, attribution and all nine package artifacts")
    print("PASS: archived original snapshots, command hashes/CRC and new-file whitespace")


if __name__ == "__main__":
    verify()
