"""Verify this batch preserves original records and exactly indexes remaining work."""

from collections import Counter
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[2]
BASELINE = "349379e305d4eda41d36821376073308ef8a8ea3"
SELECTED = {292, 397, 417, 418, 601}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def original(path):
    return subprocess.run(["git", "show", f"{BASELINE}:{path}"], cwd=ROOT,
                          capture_output=True, text=True, check=True).stdout


def records(text):
    matches = list(re.finditer(r"^### DW-(\d+):", text, re.M))
    require(len(matches) == len({m[1] for m in matches}), "Duplicate ledger ID")
    return {int(m[1]): text[m.start():matches[i + 1].start() if i + 1 < len(matches) else len(text)]
            for i, m in enumerate(matches)}


before = records(original(LEDGER))
after = records((ROOT / LEDGER).read_text())
require(before.keys() == after.keys(), "Ledger ID set changed")
require({n for n in before if before[n] != after[n]} == SELECTED, "Unrelated record changed")
for number in SELECTED:
    require("\nstatus: open\n" in before[number], f"DW-{number} was not open")
    require("\nstatus: done 2026-10-23\n" in after[number], f"DW-{number} did not close")
    restored = re.sub(r"^pre-tooling-([\w-]+): ([^\n]*)\n\1: [^\n]*$",
                      r"\1: \2", after[number], flags=re.M)
    require(restored == before[number], f"Original fields changed in DW-{number}")

open_ids = {n for n, block in after.items() if "\nstatus: open\n" in block}
index = (ROOT / INDEX).read_text()
rows = [line for line in index.splitlines() if line.startswith("| DW-")]
indexed = [int(n) for line in rows for n in re.findall(r"DW-(\d+)(?=[, |])", line.split("|")[1])]
require(set(indexed) == open_ids and len(indexed) == len(open_ids), "Index coverage mismatch")
old_rows = [line for line in original(INDEX).splitlines() if line.startswith("| DW-")]
expected_rows = [line for line in old_rows
                 if not SELECTED.intersection(map(int, re.findall(r"DW-(\d+)", line.split("|")[1])))]
require(rows == expected_rows, "Unrelated index rows changed")
counts = Counter("done" if "\nstatus: done" in block else
                 re.search(r"^disposition: (.*)$", block, re.M)[1] for block in after.values())
for key, value in counts.items():
    require(f"| {key} | {value} |" in index, f"Disposition count mismatch: {key}")
for path in ("_bmad-output/implementation-artifacts/deferred-work-archive.md",
             "_bmad-output/implementation-artifacts/sprint-status.yaml"):
    require((ROOT / path).read_text() == original(path), f"Unrelated artifact changed: {path}")

print(json.dumps({"baseline": BASELINE, "selected": sorted(SELECTED), "records": len(after),
                  "done": len(after) - len(open_ids), "open": len(open_ids),
                  "maintenance_rows": len(rows), "unrelated_records_preserved": len(after) - len(SELECTED),
                  "original_selected_fields_preserved": True, "dispositions": dict(sorted(counts.items()))},
                 indent=2))
