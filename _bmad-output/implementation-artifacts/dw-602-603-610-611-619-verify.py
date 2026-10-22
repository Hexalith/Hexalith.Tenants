"""Verify exact closure and history preservation for the five read/paging records."""

from collections import Counter
from pathlib import Path
import json
import re
import subprocess

BASELINE = "165bd76c908003fa92008632360ff8335cfd997a"
SELECTED = {602, 603, 610, 611, 619}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"
PREFIX = "pre-read-paging-"
SUMMARY = "**DW-602/DW-603/DW-610/DW-611/DW-619 closure (2026-10-22):**"
MUTABLE = {
    "status", "disposition", "verification-date", "verification", "verification-files",
    "evidence-kind", "dependencies", "next-action",
}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def original(path):
    return subprocess.check_output(["git", "show", f"{BASELINE}:{path}"], text=True)


def records(text):
    result = {}
    for block in re.split(r"(?=^### DW-)", text, flags=re.M)[1:]:
        match = re.match(r"### DW-(\d+):", block)
        require(match is not None, "malformed record heading")
        number = int(match[1])
        require(number not in result, "duplicate record ID")
        result[number] = block.rstrip()
    return result


def field(block, key):
    values = re.findall(r"^" + re.escape(key) + r": (.*)$", block, re.M)
    require(len(values) == 1, f"missing or duplicate {key}")
    return values[0]


def row_ids(row):
    return {int(number) for number in re.findall(r"DW-(\d+)", row.split("|")[1])}


def without_summary(text):
    lines = text.splitlines(keepends=True)
    positions = [index for index, line in enumerate(lines) if line.startswith(SUMMARY)]
    require(len(positions) == 1, "closure summary count")
    index = positions[0]
    require(lines[index + 1] == "\n", "closure paragraph boundary")
    return "".join(lines[:index] + lines[index + 2:])


before = records(original(LEDGER))
text = Path(LEDGER).read_text()
after = records(text)
require(before.keys() == after.keys() and len(after) == 657, "record inventory changed")
require({number for number in before if before[number] != after[number]} == SELECTED, "changed record set")
for number in SELECTED:
    require(field(before[number], "status") == "open", "selected record was already closed")
    require(field(after[number], "status") == "done 2026-10-22", "closure status")
    require(field(after[number], "disposition") == "done", "closure disposition")
    for key in MUTABLE:
        field(after[number], key)
    reconstructed = []
    for line in after[number].splitlines():
        key = line.partition(":")[0]
        if key in MUTABLE:
            continue
        if key.startswith(PREFIX) and key[len(PREFIX):] in MUTABLE:
            line = line[len(PREFIX):]
        reconstructed.append(line)
    require("\n".join(reconstructed) == before[number], f"DW-{number} historical fields changed")
require(without_summary(text).partition("### DW-")[0] == original(LEDGER).partition("### DW-")[0], "ledger preamble changed")

old_rows = [line for line in original(INDEX).splitlines() if line.startswith("| DW-")]
removed = [row for row in old_rows if row_ids(row) & SELECTED]
require(len(removed) == 5 and all(row_ids(row) <= SELECTED for row in removed), "shared maintenance row")
index_text = Path(INDEX).read_text()
rows = [line for line in index_text.splitlines() if line.startswith("| DW-")]
require(rows == [row for row in old_rows if row not in removed], "unrelated maintenance rows changed")
open_ids = {number for number, block in after.items() if field(block, "status") == "open"}
require(Counter(number for row in rows for number in row_ids(row)) == Counter({number: 1 for number in open_ids}), "open-record coverage")
require(len(open_ids) == 142 and len(rows) == 132, "closure totals")
expected_index = "".join(line for line in original(INDEX).splitlines(keepends=True) if line.rstrip("\n") not in removed)
for old, new in [
    ("| done | 510 |", "| done | 515 |"),
    ("| actionable | 76 |", "| actionable | 71 |"),
    ("**Remaining: 147 open records across 137 index rows.**", "**Remaining: 142 open records across 132 index rows.**"),
]:
    require(old in expected_index, f"missing baseline total: {old}")
    expected_index = expected_index.replace(old, new, 1)
require(without_summary(index_text) == expected_index, "maintenance narrative changed")
require(not subprocess.check_output(["git", "diff", BASELINE, "--raw", "--", "references/"], text=True), "dependency pointers changed")
for path in ["_bmad-output/implementation-artifacts/deferred-work-archive.md", "_bmad-output/implementation-artifacts/sprint-status.yaml", "_bmad-output/implementation-artifacts/release-readiness.md"]:
    require(Path(path).read_text() == original(path), f"unrelated artifact changed: {path}")
print(json.dumps({"baseline": BASELINE, "selected": sorted(SELECTED), "records": len(after), "done": len(after) - len(open_ids), "open": len(open_ids), "index_rows": len(rows), "result": "pass"}, indent=2))
