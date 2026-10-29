"""Verify the five-record, append-only qualification against its immutable baseline."""

from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import subprocess


ROOT = Path(__file__).resolve().parents[4]
BASELINE = "95e9a6cce041c30311baf6703e06289beafca2c6"
SELECTED = {116, 137, 446, 459, 466}
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
INDEX = "docs/maintenance-backlog.md"
PATTERN = re.compile(r"(?ms)^### DW-(\d+):.*?(?=^### DW-|^## |\Z)")


def require(condition, message):
    """Keep validation active under optimized Python as well."""
    if not condition:
        raise ValueError(message)


def original(path):
    """Read immutable Git bytes without changing the working tree."""
    return subprocess.check_output(["git", "show", f"{BASELINE}:{path}"], cwd=ROOT)


def entries(text):
    """Reject duplicate numbered records before exact body comparisons."""
    matches = list(PATTERN.finditer(text))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches), "Duplicate numbered IDs")
    return result


def field(body, name):
    """Read one unambiguous top-level field."""
    values = re.findall(r"^" + re.escape(name) + r": (.+)$", body, re.M)
    require(len(values) == 1, f"Expected one {name}: {values}")
    return values[0]


def main():
    """Prove selected additions and preserve every unrelated baseline byte."""
    before_text = original(LEDGER).decode()
    after_text = (ROOT / LEDGER).read_bytes().decode()
    before, after = entries(before_text), entries(after_text)
    require(before.keys() == after.keys() and len(after) == 657, "Numbered IDs changed")
    require({key for key in before if before[key] != after[key]} == SELECTED, "Unexpected record changes")
    restored = after_text
    for key in SELECTED:
        require(after[key].startswith(before[key]), f"DW-{key} original bytes were not preserved")
        require(field(after[key], "status") == "open", f"DW-{key} closed without evidence")
        require(field(after[key], "qualification-evidence") == "dw-116-137-446-459-466-validation-evidence.md", f"DW-{key} evidence missing")
        restored = restored.replace(after[key], before[key], 1)
    require(restored == before_text, "Unselected ledger body or preamble changed")
    counts = Counter(field(body, "status").split()[0] for body in after.values())
    require(counts == {"done": 652, "open": 5}, f"Unexpected totals: {counts}")

    before_index = original(INDEX).decode()
    after_index = (ROOT / INDEX).read_bytes().decode()
    old_rows = {int(re.match(r"\| DW-(\d+)", row)[1]): row for row in before_index.splitlines(keepends=True) if row.startswith("| DW-")}
    new_rows = {int(re.match(r"\| DW-(\d+)", row)[1]): row for row in after_index.splitlines(keepends=True) if row.startswith("| DW-")}
    require(old_rows.keys() == new_rows.keys() == SELECTED, "Maintenance rows do not exactly cover five open IDs")
    restored_index = after_index
    for key in SELECTED:
        old_cells = [cell.strip() for cell in old_rows[key].split("|")[1:-1]]
        new_cells = [cell.strip() for cell in new_rows[key].split("|")[1:-1]]
        require(len(old_cells) == len(new_cells) == 6, f"DW-{key} index shape changed")
        require(old_cells[:-1] == new_cells[:-1] and new_cells[-1].startswith(old_cells[-1]), f"DW-{key} original row fields changed")
        restored_index = restored_index.replace(new_rows[key], old_rows[key], 1)
    require(restored_index == before_index, "Unselected index bytes changed")
    for path in ["_bmad-output/implementation-artifacts/deferred-work-archive.md", "_bmad-output/implementation-artifacts/sprint-status.yaml"]:
        require((ROOT / path).read_bytes() == original(path), f"Protected artifact changed: {path}")
    result = {
        "baseline": BASELINE,
        "numberedIds": len(after),
        "changedNumberedRecords": sorted(SELECTED),
        "unchangedUnselectedNumberedBodies": len(after) - len(SELECTED),
        "originalSelectedBodyBytesPreserved": True,
        "unselectedLedgerAndIndexBytesPreserved": True,
        "statusCounts": dict(counts),
        "exactCoveringMaintenanceRows": len(new_rows),
        "ledgerSha256": hashlib.sha256((ROOT / LEDGER).read_bytes()).hexdigest(),
        "indexSha256": hashlib.sha256((ROOT / INDEX).read_bytes()).hexdigest(),
    }
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
