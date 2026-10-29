"""Reproduce the documentation-only DW-367 closure conservation checks."""

from collections import Counter
import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess

if not __debug__:
    raise SystemExit("Validation requires assertions; rerun without -O or PYTHONOPTIMIZE.")

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "03615cc91d940ca37b47fb13413a90a31151dd71"
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"
PREFIX = "pre-accepted-close-"
MUTABLE = {"status", "disposition", "verification-date", "verification", "next-action"}
ADDED = {"resolution-kind", "resolution-spec"}
PATTERN = re.compile(r"(?ms)^### DW-(\d+):.*?(?=^### DW-|^## |\Z)")


def git(*args):
    """Read Git data without changing repository state."""
    return subprocess.check_output(["git", *args], cwd=ROOT)


def original(path):
    """Load an immutable baseline document."""
    return git("show", BASELINE + ":" + path).decode("utf-8")


def read_utf8(path):
    """Decode UTF-8 without universal-newline conversion, preserving exact bytes."""
    return path.read_bytes().decode("utf-8")


def entries(text):
    """Refuse duplicate ledger IDs before comparing exact numbered bodies."""
    matches = list(PATTERN.finditer(text))
    result = {int(match[1]): match[0] for match in matches}
    assert len(matches) == len(result), "Duplicate numbered IDs"
    return result


def field(body, name):
    """Require exactly one top-level value for each inspected field."""
    values = re.findall(r"^" + re.escape(name) + r": (.+)$", body, re.M)
    assert len(values) == 1, (name, values)
    return values[0]


parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("--entry", type=Path, help="Optional entry worktree hash snapshot")
args = parser.parse_args()
before_text = original(LEDGER)
after_text = read_utf8(ROOT / LEDGER)
before, after = entries(before_text), entries(after_text)
assert before.keys() == after.keys() and len(after) == 657
assert {key for key in before if before[key] != after[key]} == {367}
old, new = before[367], after[367]
assert field(old, "status") == "open"
assert field(new, "status") == "done 2026-10-29"
assert field(new, "disposition") == "done"
assert field(new, "resolution-kind") == "accepted-disposition"
assert field(new, "resolution-spec") == "spec-dw-367-accepted-disposition-closure.md"
assert field(new, "verification-date") == "2026-10-29"
assert field(new, "next-action") == (
    "No current implementation task remains. Keep deliberate role selection; reopen only through a "
    "separately approved contract/product change requiring historical role evidence. Historical roles "
    "remain unavailable from the existing removal event."
)
verification = field(new, "verification")
for retained_fact in (
    "overriding open-for-visibility tracking for DW-367 only",
    "still require deliberate intended-role selection",
    "still has no historical-role field",
    "No runtime change or role recovery is claimed",
    "dw-367-accepted-disposition-validation-evidence.md",
):
    assert retained_fact in verification, retained_fact
for name in MUTABLE:
    assert field(new, PREFIX + name) == field(old, name)
restored = []
for line in new.splitlines(keepends=True):
    name = line.split(":", 1)[0]
    if name.startswith(PREFIX):
        restored.append(line.removeprefix(PREFIX))
    elif name not in MUTABLE | ADDED:
        restored.append(line)
assert "".join(restored) == old, "Original DW-367 body was not preserved losslessly"
assert field(new, "dependencies") == "Explicit contract/product change approval"

# Restore our only record edit and remove our only preface addition, then require
# every baseline byte, including unnumbered sections and historical holds, intact.
intro = after_text.splitlines(keepends=True)[2:4]
assert intro[0].startswith("**DW-367 accepted-disposition closure (2026-10-29):** ")
assert intro[1] == "\n"
restored_ledger = after_text.replace(new, old, 1).replace("".join(intro), "", 1)
assert restored_ledger.startswith(before_text), "Unrelated baseline ledger content changed"
external_appendix = restored_ledger[len(before_text):]
if external_appendix:
    assert external_appendix.startswith(
        "\n\n## Deferred from: latest EventStore package verification (2026-10-29)\n\n"
    ), "Unexpected appended ledger content"
    assert len(re.findall(r"^## ", external_appendix, re.M)) == 1

for key in (117, 118, 133, 365):
    assert after[key] == before[key] and field(after[key], "status").startswith("done ")

index = read_utf8(ROOT / INDEX)
old_rows = [line for line in original(INDEX).splitlines() if line.startswith("| DW-")]
rows = [line for line in index.splitlines() if line.startswith("| DW-")]
assert rows == [line for line in old_rows if not line.startswith("| DW-367 |")]
opened = {key for key, body in after.items() if field(body, "status") == "open"}
indexed = [int(value) for row in rows for value in re.findall(r"DW-(\d+)", row.split("|")[1])]
assert len(indexed) == len(set(indexed)) and set(indexed) == opened
assert opened == {116, 137, 446, 459, 466} and len(rows) == 5
counts = Counter(field(after[key], "disposition") for key in opened)
for name in ("actionable", "blocked", "awaiting-decision", "accepted-limitation", "uncertain"):
    assert f"| {name} | {counts[name]} |" in index
assert "| done | 652 |" in index
assert "**Remaining: 5 open records across 5 index rows.**" in index

protected = [
    "_bmad-output/implementation-artifacts/deferred-work-archive.md",
    "_bmad-output/implementation-artifacts/sprint-status.yaml",
    "_bmad-output/implementation-artifacts/spec-dw-117-118-133-365-367-accepted-dispositions.md",
    "_bmad-output/implementation-artifacts/spec-5-5-start-a-forward-tenant-correction-from-audit-evidence.md",
]
for path in protected:
    assert (ROOT / path).read_bytes() == git("show", BASELINE + ":" + path), path
assert not git("diff", BASELINE, "--name-only", "--", "src", "tests", "references", ".gitmodules")

preserved_hashes = None
if args.entry:
    entry = json.loads(read_utf8(args.entry))
    assert entry["baseline"] == BASELINE
    preserved = {path: digest for path, digest in entry["hashes"].items() if path not in {LEDGER, INDEX}}
    for path, digest in preserved.items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, path
    links = [line for line in git("ls-files", "--stage").decode("utf-8").splitlines() if line.startswith("160000 ")]
    assert links == entry["gitlinks"] and len(links) == 9
    preserved_hashes = len(preserved)

print(json.dumps({
    "baseline": BASELINE,
    "closed_ids": [367],
    "total_ids": len(after),
    "done": 652,
    "open": len(opened),
    "open_ids": sorted(opened),
    "maintenance_rows": len(rows),
    "unselected_bodies_preserved": 656,
    "selected_original_body_reconstructed": True,
    "all_baseline_ledger_bytes_preserved_except_scoped_edits": True,
    "retained_role_selection_and_reopening_policy_verified": True,
    "external_appendix_sha256": hashlib.sha256(external_appendix.encode("utf-8")).hexdigest() if external_appendix else None,
    "remaining_dispositions": dict(counts),
    "original_five_item_draft_preserved": True,
    "application_tests_gitlinks_archive_sprint_preserved": True,
    "entry_file_hashes_preserved": preserved_hashes,
}, indent=2))
