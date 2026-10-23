"""Verify exact-five release closures while preserving the dirty entry snapshots."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASELINE = "25e3bf827a7963ec79b57b4918290b3fed728706"
SELECTED = {527, 544, 582, 583, 589}
ENTRY_LEDGER_SHA256 = "5b7fb9dbfe0ece314cf208fdd9b9a9dfa042f077e6a83aa6c3bd37e0f3b6c5f9"
ENTRY_ARCHIVE_SHA256 = "7ff7509177a32e7f4ff51a830ac13ddfa016b6485811e520b1a0036e8326509c"
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"


def require(condition, message):
    if not condition:
        raise RuntimeError(message)


def records(text):
    matches = list(re.finditer(r"^### DW-(\d+):", text, re.M))
    require(len(matches) == len({match[1] for match in matches}), "Duplicate ledger ID")
    return {int(match[1]): text[match.start():matches[index + 1].start() if index + 1 < len(matches) else len(text)]
            for index, match in enumerate(matches)}


ledger = (ROOT / LEDGER).read_text()
after = records(ledger)
require(set(after) == set(range(1, 658)), "Ledger ID coverage changed")
restored = re.sub(r"^\*\*DW-527/DW-544/DW-582/DW-583/DW-589 closure[^\n]*\n\n", "", ledger, count=1, flags=re.M)
for number in SELECTED:
    block = after[number]
    require("\npre-release-verification-status: open\nstatus: done 2026-10-23\n" in block,
            f"DW-{number} did not transition from open to done")
    original = block.replace("pre-release-verification-status: open\nstatus: done 2026-10-23\n", "status: open\n")
    original = re.sub(r"^release-verification-resolution(?:-evidence)?: [^\n]*\n", "", original, flags=re.M)
    restored = restored.replace(block, original, 1)
require(hashlib.sha256(restored.encode()).hexdigest() == ENTRY_LEDGER_SHA256,
        "Entry ledger bytes or an unrelated/original field changed")
archive = ROOT / "_bmad-output/implementation-artifacts/deferred-work-archive.md"
require(hashlib.sha256(archive.read_bytes()).hexdigest() == ENTRY_ARCHIVE_SHA256, "Entry archive bytes changed")
open_ids = {number for number, block in after.items() if "\nstatus: open\n" in block}
index = (ROOT / INDEX).read_text()
rows = [line for line in index.splitlines() if line.startswith("| DW-")]
indexed = [int(number) for line in rows for number in re.findall(r"DW-(\d+)(?=[, |])", line.split("|")[1])]
require(set(indexed) == open_ids and len(indexed) == len(open_ids), "Index coverage mismatch")
original_index = subprocess.run(["git", "show", f"{BASELINE}:{INDEX}"], cwd=ROOT,
                                capture_output=True, text=True, check=True).stdout
old_rows = [line for line in original_index.splitlines() if line.startswith("| DW-")]
require(rows == [line for line in old_rows if not SELECTED.intersection(map(int, re.findall(r"DW-(\d+)", line.split("|")[1])))],
        "Unrelated index rows changed")
counts = Counter("done" if "\nstatus: done" in block else re.search(r"^disposition: (.*)$", block, re.M)[1]
                 for block in after.values())
for key, value in counts.items():
    require(f"| {key} | {value} |" in index, f"Disposition count mismatch: {key}")
require(f"**Remaining: {len(open_ids)} open records across {len(rows)} index rows.**" in index,
        "Remaining-work summary does not match exact index coverage")
for number in (560, 563, 593):
    require(number in open_ids, f"Unrelated DW-{number} hold was closed")
print(json.dumps({"baseline": BASELINE, "selected": sorted(SELECTED), "records": len(after),
                  "done": len(after) - len(open_ids), "open": len(open_ids), "maintenance_rows": len(rows),
                  "entry_ledger_sha256": ENTRY_LEDGER_SHA256, "entry_archive_sha256": ENTRY_ARCHIVE_SHA256,
                  "original_fields_and_unrelated_records_preserved": True,
                  "dispositions": dict(sorted(counts.items()))}, indent=2))
