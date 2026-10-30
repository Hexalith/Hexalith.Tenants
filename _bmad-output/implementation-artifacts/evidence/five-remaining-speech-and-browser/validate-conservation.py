"""Check this run's entry snapshot, preserving all historical fields and bytes."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import zipfile


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SELECTED = {116, 446, 459}
RECORD = re.compile(rb"(?ms)^### DW-(\d+):.*?(?=^### DW-|^## |\Z)")
UNNUMBERED_RECORD = re.compile(rb"(?ms)^- source_spec:.*?(?=^- source_spec:|^##? |\Z)")
UNNUMBERED_ANCHORS = (
    b"  summary: Unverified medium; isolate one administrator-grant Escape focus timeout",
    "  summary: Medium — route a pinned Fluent UI 5.0.1 grid continuation".encode(),
)
ADDITION = re.compile(rb"\n(?P<indent>  |)remaining-batch-date: 2026-10-30\n(?P=indent)remaining-batch-result: [^\n]+\n(?P=indent)remaining-batch-evidence: five-remaining-speech-and-browser-validation-evidence\.md\n")
INDEX_NOTE = " 2026-10-30 remaining-batch rerun: current-source qualification is blocked by the integrated RouteOperationContract build failure; no new speech acceptance or closure. [Fresh attempts and limits](../_bmad-output/implementation-artifacts/five-remaining-speech-and-browser-validation-evidence.md)."


def require(condition, message):
    if not condition:
        raise ValueError(message)


def records(text):
    matches = list(RECORD.finditer(text))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches), "Duplicate numbered IDs")
    return result


def unnumbered_record(text, anchor):
    matches = [match[0] for match in UNNUMBERED_RECORD.finditer(text) if anchor in match[0]]
    require(len(matches) == 1, "Selected unnumbered record is missing or ambiguous")
    return matches[0]


def main():
    manifest = json.loads((HERE / "entry-ledger-manifest.json").read_text())
    snapshot = (HERE / "entry-ledgers.zip").read_bytes()
    require(hashlib.sha256(snapshot).hexdigest() == manifest["archiveSha256"], "Entry ZIP changed")
    with zipfile.ZipFile(HERE / "entry-ledgers.zip") as archive:
        require(archive.testzip() is None, "Entry ZIP CRC failure")
        entry = {item["name"]: archive.read(item["name"]) for item in manifest["files"]}
    for item in manifest["files"]:
        require(hashlib.sha256(entry[item["name"]]).hexdigest() == item["sha256"], "Entry member changed")
    # Do not use read_text(): universal newline conversion would hide CRLF rewrites.
    before = entry["deferred-work.md"]
    after = (ROOT / "_bmad-output/implementation-artifacts/deferred-work.md").read_bytes()
    old_records, new_records = records(before), records(after)
    require(old_records.keys() == new_records.keys() and len(new_records) == 657, "Numbered IDs changed")
    for key, original in old_records.items():
        actual = new_records[key]
        if key in SELECTED:
            actual, count = ADDITION.subn(b"", actual)
            require(count == 1, f"DW-{key} needs exactly one appended result")
        require(actual == original, f"Original DW-{key} body changed")
    for anchor in UNNUMBERED_ANCHORS:
        original = unnumbered_record(before, anchor)
        actual = unnumbered_record(after, anchor)
        additions = list(ADDITION.finditer(actual))
        require(len(additions) == 1 and additions[0]["indent"] == b"  ",
                "Unnumbered addition must remain inside its original enclosing record")
        require(ADDITION.sub(b"", actual) == original, "Original unnumbered record bytes changed")
    restored, count = ADDITION.subn(b"", after)
    require(count == 5 and restored == before, "Unselected ledger bytes changed")
    counts = Counter(re.search(rb"^status: (\w+)", body, re.M)[1].decode() for body in new_records.values())
    require(counts == {"done": 654, "open": 3}, "Unexpected status changes")
    require((ROOT / "_bmad-output/implementation-artifacts/deferred-work-archive.md").read_bytes()
            == entry["deferred-work-archive.md"], "Historical archive changed")
    original_index = entry["maintenance-backlog.md"]
    index = (ROOT / "docs/maintenance-backlog.md").read_bytes()
    note = INDEX_NOTE.encode()
    old_rows = {int(re.match(rb"\| DW-(\d+)", line)[1]): line for line in original_index.splitlines(True) if line.startswith(b"| DW-")}
    rows = {int(re.match(rb"\| DW-(\d+)", line)[1]): line for line in index.splitlines(True) if line.startswith(b"| DW-")}
    require(rows.keys() == old_rows.keys() == SELECTED, "Index does not exactly cover the three open IDs")
    for key, original in old_rows.items():
        require(original.endswith(b" |\n"), "Unexpected original table format")
        require(rows[key] == original[:-3] + note + b" |\n", f"DW-{key} index history changed")
    require(index.count(note) == 3 and index.replace(note, b"") == original_index, "Unselected index bytes changed")
    print(json.dumps({"numberedIds": 657, "unselectedNumberedBodiesPreserved": 654,
        "selectedHistoricalFieldsPreserved": True, "unnumberedResultsAppended": 2,
        "exactInputBytesPreserved": True, "unnumberedAdditionsBoundToOriginalRecords": True,
        "entryArchiveBytesUnchanged": True, "statusCounts": dict(counts),
        "exactCoveringMaintenanceRows": 3}, indent=2))


if __name__ == "__main__":
    main()
