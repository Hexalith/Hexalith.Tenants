"""Reproduce this batch's read-only ledger and maintenance-index preservation checks."""
from collections import Counter
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[4]
BASELINE = "db83309146816602033320270f10b1cea28d3100"
SELECTED = {122, 334, 358, 579, 595}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
PATTERN = re.compile(r"(?ms)^### DW-(\d+):.*?(?=^### DW-|\Z)")


def entries(text):
    """Return every exact numbered body, refusing duplicate identifiers."""
    matches = list(PATTERN.finditer(text))
    result = {int(match[1]): match[0] for match in matches}
    assert len(matches) == len(result), "Duplicate DW identifiers"
    return result


def field(body, name):
    """Read a required single-line ledger field."""
    match = re.search(r"^" + re.escape(name) + r": (.+)$", body, re.M)
    assert match, ("Missing field", name)
    return match[1]


original = entries(subprocess.check_output(
    ["git", "show", BASELINE + ":" + LEDGER], cwd=ROOT, text=True))
current = entries((ROOT / LEDGER).read_text())
assert len(original) == len(current) == 657
assert original.keys() == current.keys()
assert all(original[key] == current[key] for key in original.keys() - SELECTED)
for key in SELECTED:
    old, new = original[key], current[key]
    assert field(old, "status") == "open"
    assert field(new, "status") == "done 2026-10-28"
    assert field(new, "disposition") == "done"
    for name in ("status", "disposition", "next-action"):
        assert field(new, "pre-closure-" + name) == field(old, name)
    for line in old.splitlines():
        if not line.startswith(("status:", "disposition:", "next-action:")):
            assert line in new.splitlines(), (key, "Original field/body line changed", line)

opened = {key for key, body in current.items() if field(body, "status") == "open"}
index = (ROOT / "docs/maintenance-backlog.md").read_text()
rows = [line for line in index.splitlines() if line.startswith("| DW-")]
indexed = [int(value) for row in rows for value in re.findall(r"DW-(\d+)", row.split("|")[1])]
assert len(indexed) == len(set(indexed)) and set(indexed) == opened
counts = Counter(field(current[key], "disposition") for key in opened)
for disposition, count in counts.items():
    assert f"| {disposition} | {count} |" in index
assert f"| done | {657 - len(opened)} |" in index
assert f"**Remaining: {len(opened)} open records across {len(rows)} index rows.**" in index
print(json.dumps({
    "baseline": BASELINE,
    "total_ids": len(current),
    "done": 657 - len(opened),
    "open": len(opened),
    "maintenance_rows": len(rows),
    "closed_ids": sorted(SELECTED),
    "unselected_bodies_preserved": 652,
    "selected_original_fields_preserved": True,
    "exact_index_coverage": True,
    "remaining_dispositions": dict(counts),
}, indent=2))
