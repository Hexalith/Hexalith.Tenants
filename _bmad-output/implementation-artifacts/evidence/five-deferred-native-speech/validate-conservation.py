"""Verify the approved dirty entry and all original IDs, independent of Python -O."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import zipfile

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SELECTED = {116, 137, 446, 459, 466}
CLOSED = {137, 466}
PATTERN = re.compile(r"(?ms)^### DW-(\d+):.*?(?=^### DW-|^## |\Z)")


def require(value, message):
    if not value:
        raise ValueError(message)


def records(text):
    matches = list(PATTERN.finditer(text))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches), "Duplicate IDs")
    return result


INDEX_SUMMARY_REPLACEMENTS = [('| done | 652 |', '| done | 654 |'), ('| actionable | 1 |', '| actionable | 0 |'), ('| uncertain | 3 |', '| uncertain | 2 |'), ('**Remaining: 5 open records across 5 index rows.**', '**Remaining: 3 open records across 3 index rows.**'), ("**Selected decisions settled:** The approved native global-administrator authority, fatal precedence and search deadline are implemented. DW-137 still needs separate route-contract work; assistive-technology evidence gaps remain visible below. DW-367's accepted limitation is retained in the ledger under its explicitly approved closure.", "**Selected decisions settled:** The approved native global-administrator authority, fatal precedence and search deadline are implemented. DW-137 reversible-identifier support is withdrawn as an approved accepted contract limitation; only separately approved shared-identity or user-only contract work can reopen it. DW-466 closes as an accepted unresolved historical incident, retaining its missing method/root-cause evidence and reopening on an identified failure or causal reproduction. Assistive-technology evidence gaps remain visible below. DW-367's accepted limitation remains retained under its explicitly approved closure."), ('`Defect` means the contradictory behavior or missing guard is present in current source; it does not imply a newly reproduced incident. `Gap` means a specific contract or validation proof is missing. `Recommendation` and `Unverified` do not assert a verified product defect. Accepted limitations retain an existing intentional boundary and stay open for visibility. DW-367 is an explicitly approved individual exception; its closure preserves its limitation and conditional reopening in the ledger and changes no other disposition.', '`Defect` means the contradictory behavior or missing guard is present in current source; it does not imply a newly reproduced incident. `Gap` means a specific contract or validation proof is missing. `Recommendation` and `Unverified` do not assert a verified product defect. Accepted limitations retain an existing intentional boundary and normally stay open for visibility. DW-367 and DW-137 are explicitly approved individual exceptions; DW-466 is an explicitly approved unresolved-incident closure. Their limitations, evidence gaps and conditional reopening remain in the ledger; these decisions change no other disposition.'), ('No accepted limitation remains open. DW-367 is closed by explicit user decision, with its historical finding, deliberate role-selection boundary and reopening condition retained in the [deferred ledger](../_bmad-output/implementation-artifacts/deferred-work.md).', 'No accepted limitation remains open. DW-367 and DW-137 are closed by explicit user decisions, retaining their historical findings, deliberate role-selection/current identifier boundaries and reopening conditions. DW-466 is closed as an accepted unresolved historical incident, retaining the missing method/root-cause evidence and reopening on an identified failure or causal reproduction. None of these closures claims a newly implemented feature or causal fix. See the [deferred ledger](../_bmad-output/implementation-artifacts/deferred-work.md).')]


def main():
    manifest = json.loads((HERE / "entry-ledger-manifest.json").read_text())
    archive_bytes = (HERE / "entry-ledgers.zip").read_bytes()
    require(hashlib.sha256(archive_bytes).hexdigest() == manifest["archiveSha256"], "Entry archive changed")
    with zipfile.ZipFile(HERE / "entry-ledgers.zip") as archive:
        entry = {item["name"]: archive.read(item["name"]) for item in manifest["files"]}
    for item in manifest["files"]:
        require(hashlib.sha256(entry[item["name"]]).hexdigest() == item["sha256"], "Entry file hash mismatch")
    path = ROOT / "_bmad-output/implementation-artifacts/deferred-work.md"
    before_text, after_text = entry["deferred-work.md"].decode(), path.read_text()
    before, after = records(before_text), records(after_text)
    require(before.keys() == after.keys() and len(after) == 657, "Numbered IDs changed")
    restored = after_text
    for key in before:
        if key not in SELECTED:
            require(before[key] == after[key], f"Unselected DW-{key} changed")
        else:
            original_body = after[key].split("\nnative-disposition-date:")[0]
            if key in CLOSED:
                original_body = original_body.replace("\nstatus: done 2026-10-30\n", "\nstatus: open\n", 1)
            require(original_body.rstrip() == before[key].rstrip(), f"DW-{key} original fields changed")
            restored = restored.replace(after[key], before[key], 1)
    addition = re.search(r"(?s)\*\*Native-speech run and approved dispositions.*?\n\n", restored)
    require(addition is not None, "Disposition preamble missing")
    restored = restored[:addition.start()] + restored[addition.end():]
    require(restored == before_text, "Unselected ledger bytes changed")
    counts = Counter(re.search(r"^status: (\w+)", body, re.M)[1] for body in after.values())
    require(counts == {"done": 654, "open": 3}, f"Incorrect counts: {counts}")
    archive = (ROOT / "_bmad-output/implementation-artifacts/deferred-work-archive.md").read_bytes()
    require(archive.startswith(entry["deferred-work-archive.md"]), "Approved archive bytes changed")
    appended = records(archive[len(entry["deferred-work-archive.md"]):].decode())
    require(appended.keys() == CLOSED, "Unexpected archive additions")
    for key in CLOSED:
        require(appended[key].startswith(after[key].rstrip()), f"DW-{key} full resolved body missing from archive")
    original_index = entry["maintenance-backlog.md"].decode()
    index = (ROOT / "docs/maintenance-backlog.md").read_text()
    rows = {int(re.match(r"\| DW-(\d+)", line)[1]): line for line in index.splitlines(keepends=True) if line.startswith("| DW-")}
    require(rows.keys() == SELECTED - CLOSED, "Maintenance rows do not exactly cover open records")
    old_rows = {int(re.match(r"\| DW-(\d+)", line)[1]): line for line in original_index.splitlines(keepends=True) if line.startswith("| DW-")}
    for key in rows:
        old_cells = [cell.strip() for cell in old_rows[key].split("|")[1:-1]]
        new_cells = [cell.strip() for cell in rows[key].split("|")[1:-1]]
        require(old_cells[:-1] == new_cells[:-1] and new_cells[-1].startswith(old_cells[-1]), f"DW-{key} original row fields changed")
    restored_index = index
    for key in rows:
        restored_index = restored_index.replace(rows[key], old_rows[key], 1)
    summary = re.search(r"(?s)\*\*Native-speech run and approved dispositions.*?\n\n", restored_index)
    require(summary is not None, "Index disposition summary missing")
    restored_index = restored_index[:summary.start()] + restored_index[summary.end():]
    expected_index = original_index
    for key in CLOSED:
        expected_index = expected_index.replace(old_rows[key], "", 1)
    for old_summary, new_summary in INDEX_SUMMARY_REPLACEMENTS:
        require(expected_index.count(old_summary) == 1, "Expected unique original current-summary text")
        expected_index = expected_index.replace(old_summary, new_summary, 1)
    require(restored_index == expected_index, "Unselected index bytes changed")
    print(json.dumps({"numberedIds": len(after), "unselectedNumberedBodiesPreserved": 652,
        "originalSelectedFieldsPreservedExceptApprovedStatus": True,
        "approvedEntryArchiveBytesPreserved": True, "closedByApprovedDisposition": sorted(CLOSED),
        "statusCounts": dict(counts), "exactCoveringMaintenanceRows": len(rows)}, indent=2))


if __name__ == "__main__":
    main()
