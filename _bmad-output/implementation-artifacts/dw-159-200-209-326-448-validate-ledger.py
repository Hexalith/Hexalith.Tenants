"""Qualify the exact five-record closure against its immutable entry revision."""

import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[2]
BASELINE = "fc5d301dd07683475b7a1bcecf0ba5fab99ab265"
SELECTED = {159, 200, 209, 326, 448}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"
MUTABLE = {
    "status", "disposition", "verification-date", "verification",
    "verification-files", "evidence-kind", "next-action",
}


def original(path):
    return subprocess.check_output(
        ["git", "show", f"{BASELINE}:{path}"], cwd=ROOT, text=True
    )


def records(content):
    return {
        int(re.match(r"### DW-(\d+):", block)[1]): block
        for block in re.findall(r"^### DW-\d+:.*?(?=^#{2,3} |\Z)", content, re.M | re.S)
    }


def is_open(block):
    return re.search(r"^status: open$", block, re.M) is not None


before = records(original(LEDGER))
ledger = (ROOT / LEDGER).read_text()
index = (ROOT / INDEX).read_text()
after = records(ledger)
assert len(before) == len(after) == 657
assert before.keys() == after.keys()
assert {key for key in before if before[key] != after[key]} == SELECTED
assert all(is_open(before[key]) and not is_open(after[key]) for key in SELECTED)
for key in SELECTED:
    for line in before[key].splitlines():
        field = line.partition(":")[0]
        expected = "pre-ui-maintenance-" + line if field in MUTABLE else line
        assert expected in after[key].splitlines(), (key, expected)
    assert "status: done 2026-10-23" in after[key]

open_ids = {key for key, block in after.items() if is_open(block)}
rows = [line for line in index.splitlines() if re.match(r"^\| DW-\d+", line)]
coverage = collections.Counter(
    int(key) for row in rows for key in re.findall(r"DW-(\d+)", row.split("|")[1])
)
assert set(coverage) == open_ids
assert all(count == 1 for count in coverage.values())
assert len(open_ids) == 102 and len(rows) == 92
for row in original(INDEX).splitlines():
    if re.match(r"^\| DW-\d+", row):
        ids = {int(key) for key in re.findall(r"DW-(\d+)", row.split("|")[1])}
        assert (row not in rows) if ids & SELECTED else (row in rows)

counts = collections.Counter()
for block in after.values():
    disposition = re.search(r"^disposition: (.*)$", block, re.M)
    counts[disposition[1] if is_open(block) else "done"] += 1
for disposition, count in counts.items():
    assert f"| {disposition} | {count} |" in index

result = {
    "baseline": BASELINE,
    "closed_records": sorted(SELECTED),
    "total_records": len(after),
    "done": len(after) - len(open_ids),
    "open": len(open_ids),
    "maintenance_rows": len(rows),
    "dispositions": dict(sorted(counts.items())),
    "unrelated_records_unchanged": True,
    "original_fields_preserved": True,
    "open_index_exact_coverage": True,
    "ledger_sha256": hashlib.sha256(ledger.encode()).hexdigest(),
    "index_sha256": hashlib.sha256(index.encode()).hexdigest(),
}
output = Path(__file__).with_name("dw-159-200-209-326-448-ledger-validation.json")
output.write_text(json.dumps(result, indent=2) + "\n")
print(json.dumps(result, indent=2))
