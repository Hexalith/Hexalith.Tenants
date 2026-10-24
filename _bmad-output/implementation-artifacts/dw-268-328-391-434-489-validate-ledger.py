"""Replay the five UI runtime qualification closures without rewriting saved evidence by default."""

import collections
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys


ROOT = Path(__file__).resolve().parents[2]
BASELINE = "3808cd73f6560d035c417109d27d0a2b3365992b"
SELECTED = {268, 328, 391, 434, 489}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"
MUTABLE = {
    "status", "disposition", "verification-date", "verification",
    "verification-files", "evidence-kind", "next-action",
}


def require(condition, message):
    if not condition:
        raise SystemExit(message)


def original(path):
    return subprocess.check_output(
        ["git", "show", f"{BASELINE}:{path}"], cwd=ROOT, text=True
    )


def records(content):
    blocks = re.findall(r"^### DW-\d+:.*?(?=^#{2,3} |\Z)", content, re.M | re.S)
    result = {int(re.match(r"### DW-(\d+):", block)[1]): block for block in blocks}
    require(len(blocks) == len(result), "Duplicate ledger identifiers")
    return result


def is_open(block):
    return re.search(r"^status: open$", block, re.M) is not None


before = records(original(LEDGER))
ledger = (ROOT / LEDGER).read_text()
index = (ROOT / INDEX).read_text()
after = records(ledger)
require(len(after) == len(before) == 657 and before.keys() == after.keys(), "Ledger ID drift")
require({key for key in before if before[key] != after[key]} == SELECTED, "Closure scope drift")
for key in SELECTED:
    require(is_open(before[key]) and not is_open(after[key]), f"DW-{key} status mismatch")
    expected = collections.Counter(
        "pre-ui-qualification-" + line if line.partition(":")[0] in MUTABLE else line
        for line in before[key].splitlines() if line
    )
    actual = collections.Counter(after[key].splitlines())
    require(not expected - actual, f"DW-{key} historical fields lost")
    current = collections.defaultdict(list)
    for line in after[key].splitlines():
        name, separator, value = line.partition(":")
        if separator and name in MUTABLE:
            current[name].append(value.strip())
    require(current["status"] == ["done 2026-10-24"], f"DW-{key} exact current status missing")
    require(current["disposition"] == ["done"], f"DW-{key} current disposition missing")
    require(current["verification-date"] == ["2026-10-24"], f"DW-{key} current verification date missing")
    for name in ("verification", "verification-files", "evidence-kind", "next-action"):
        require(len(current[name]) == 1 and bool(current[name][0]), f"DW-{key} current {name} missing or ambiguous")
    expected_evidence = "_bmad-output/implementation-artifacts/dw-268-328-391-434-489-validation-evidence.md"
    require(expected_evidence in current["verification-files"][0].split("; "), f"DW-{key} closure evidence link missing")
    require((ROOT / expected_evidence).is_file(), f"DW-{key} closure evidence absent")

open_ids = {key for key in after if is_open(after[key])}
rows = [line for line in index.splitlines() if re.match(r"^\| DW-\d+", line)]
coverage = collections.Counter(
    int(key) for row in rows for key in re.findall(r"DW-(\d+)", row.split("|")[1])
)
require(set(coverage) == open_ids and all(count == 1 for count in coverage.values()), "Index coverage drift")
require(len(open_ids) == 67 and len(rows) == 62, "Unexpected closure totals")
for row in original(INDEX).splitlines():
    if re.match(r"^\| DW-\d+", row):
        ids = {int(key) for key in re.findall(r"DW-(\d+)", row.split("|")[1])}
        require((row not in rows) if ids & SELECTED else (row in rows), "Unrelated index row changed")

counts = collections.Counter()
for block in after.values():
    disposition = re.search(r"^disposition: (.*)$", block, re.M)
    counts[disposition[1] if is_open(block) else "done"] += 1
for disposition, count in counts.items():
    require(f"| {disposition} | {count} |" in index, f"Wrong {disposition} count")
require("**Remaining: 67 open records across 62 index rows.**" in index, "Stale remaining summary")
archive = "_bmad-output/implementation-artifacts/deferred-work-archive.md"
require((ROOT / archive).read_text() == original(archive), "Archive changed")
result = {
    "baseline": BASELINE, "closed_records": sorted(SELECTED), "total_records": len(after),
    "done": len(after) - len(open_ids), "open": len(open_ids), "maintenance_rows": len(rows),
    "dispositions": dict(sorted(counts.items())), "unrelated_records_unchanged": True,
    "original_fields_preserved": True, "open_index_exact_coverage": True, "archive_unchanged": True,
    "ledger_sha256": hashlib.sha256(ledger.encode()).hexdigest(),
    "index_sha256": hashlib.sha256(index.encode()).hexdigest(),
}
output = Path(__file__).with_name("dw-268-328-391-434-489-ledger-validation.json")
if sys.argv[1:] == ["--write-result"]:
    output.write_text(json.dumps(result, indent=2) + "\n")
else:
    require(not sys.argv[1:], "Usage: validator [--write-result]")
    require(json.loads(output.read_text()) == result, "Saved evidence differs; inspect before regenerating")
print(json.dumps(result, indent=2))
