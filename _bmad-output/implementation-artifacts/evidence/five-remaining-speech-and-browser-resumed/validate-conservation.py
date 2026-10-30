"""Permit only the reviewed DW-116 closure, selected annotations and exact defer."""
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
UNNUMBERED_ANCHORS = (b"  summary: Unverified medium; isolate one administrator-grant Escape focus timeout",
    "  summary: Medium — route a pinned Fluent UI 5.0.1 grid continuation".encode())
ADDITION = re.compile(rb"\n(?P<indent>  |)resumed-batch-date: 2026-10-30\n(?P=indent)resumed-batch-result: [^\n]+\n(?P=indent)resumed-batch-evidence: five-remaining-speech-and-browser-resumed-validation-evidence\.md\n")
CLOSURE_RESULT = "Independent assessment accepts only DW-116's 16 Search queued-notice windows on frozen c2da6bac in NVDA/Chrome and NVDA/Firefox, EN/FR: no stale notice in S0, complete S1/S2 guidance once and both S3 bodies in order without body loss/repetition. Close this narrow queued-notice contract only; repeated component dismiss labels, English dismiss labels in FR, unknown causes, audible-completion limits and unrelated release/localization holds remain. Original findings and fields are preserved. Acceptance: evidence/five-remaining-speech-and-browser-resumed/independent-at-assessment.json."
CREATE_RESULT = "Independent assessment keeps this record open: first-Unavailable C2 and same-state Unavailable Refresh C3 queue repeated audit/lifecycle output in both pairings/cultures; cause is unassigned. The protocol requires a separately approved behavior fix and repeat qualification. Complete protocol controls and first-Pending conveyance do not waive this blocker. Acceptance: evidence/five-remaining-speech-and-browser-resumed/independent-at-assessment.json."
INDEX_NOTE = " 2026-10-30 resumed assessment: duplicated first-Unavailable and same-state Refresh audit/lifecycle output keeps this record open; cause is unassigned. Require an approved behavior fix and repeat qualification. [Assessment and limits](../_bmad-output/implementation-artifacts/five-remaining-speech-and-browser-resumed-validation-evidence.md)."
INDEX_SUMMARY = "\n**Resumed native assessment (2026-10-30):** Only DW-116 closes for its independently accepted 16 EN/FR Search queued-notice windows on NVDA/Chrome and NVDA/Firefox. Repeated dismiss labels, English dismiss labels in FR and audible-completion/localization limits remain. DW-446/DW-459 stay open for duplicated first-Unavailable and same-state Refresh output, with cause unassigned and an approved fix/requalification required. All 657 IDs remain: 655 done/2 open across two maintenance rows. The grant history and external grid follow-ups remain open. [Independent assessment and evidence](../_bmad-output/implementation-artifacts/five-remaining-speech-and-browser-resumed-validation-evidence.md).\n"
DEFER = b"\n\n## Deferred from: resumed speech review incoming coverage (2026-10-30)\n\n- source_spec: `_bmad-output/implementation-artifacts/spec-five-remaining-speech-and-browser-followups.md`\n  summary: Unverified medium, pre-existing incoming coverage gap: qualify granting and post-removal revocation through the registered bound metadata transport.\n  evidence: Incoming a16d897 src/Hexalith.Tenants/Program.cs:82 registers GatewayNativeSourceMetadataReader through TenantOwnerSourceGateway, while tests/Hexalith.Tenants.IntegrationTests/AuthenticatedTenantsRouteTests.cs:105 still constructs DaprNativeSourceMetadataReader directly. Those existing route tests do not establish equivalent coverage through the registered bound transport.\n  status: open\n  classification: pre-existing-incoming-test-coverage-gap\n  production-impact: Unverified; no production authorization or revocation failure is demonstrated or claimed.\n  next-action: Exercise matching granting and post-removal revocation through the registered GatewayNativeSourceMetadataReader bound transport before claiming that production path qualified; preserve current authorization and exact identity rules.\n  scope: Deferred separately from the five selected speech/browser follow-ups; excluded from numbered totals. No incoming host or unrelated integration test was changed.\n  evidence-review: RBH10; five-remaining-speech-and-browser-resumed-validation-evidence.md\n"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def records(data):
    matches = list(RECORD.finditer(data))
    result = {int(match[1]): match[0] for match in matches}
    require(len(result) == len(matches), "Duplicate numbered IDs")
    return result


def unnumbered_record(data, anchor):
    matches = [match[0] for match in UNNUMBERED_RECORD.finditer(data) if anchor in match[0]]
    require(len(matches) == 1, "Selected unnumbered record is missing or ambiguous")
    return matches[0]


def independent_acceptance():
    path = HERE / "independent-at-assessment.json"
    require(path.is_file(), "Independent acceptance is required for DW-116 closure")
    assessment = json.loads(path.read_bytes())
    verdict = assessment["recordVerdicts"]["DW-116"]
    require(verdict["accepted"] is True and verdict["acceptedRecordWindows"] == 16
            and assessment["independentlyAcceptedRecordWindows"] == 16, "DW-116's exact 16-window acceptance is required")
    require(all(assessment["recordVerdicts"][key]["accepted"] is False for key in ("DW-446", "DW-459")), "Create records may not close")
    matrix = json.loads((HERE / "native-matrix.json").read_bytes())
    require(matrix["sourceRootRevision"] == assessment["profileRoot"] and matrix["independentlyAcceptedWindows"] == 0,
            "Capture matrix must retain its pre-assessment scope")
    selected = [row for row in matrix["windows"] if row["trial"].startswith("S")]
    expected = {(pair, f"S{case}-{culture}") for pair in ("NVDA/Chrome", "NVDA/Firefox")
                for culture in ("en", "fr") for case in range(4)}
    require(len(selected) == 16 and {(r["pairing"], r["trial"]) for r in selected} == expected
            and all(row["protocolComplete"] for row in selected), "Accepted Search windows are incomplete")
    require(len(assessment["packets"]) == 2, "Acceptance must bind both native packets")
    for packet in assessment["packets"]:
        require(hashlib.sha256((HERE / packet["path"]).read_bytes()).hexdigest() == packet["sha256"], "Accepted packet binding changed")


def main():
    manifest = json.loads((HERE / "entry-ledger-manifest.json").read_bytes())
    require(hashlib.sha256((HERE / "entry-ledgers.zip").read_bytes()).hexdigest() == manifest["archiveSha256"], "Entry ZIP changed")
    with zipfile.ZipFile(HERE / "entry-ledgers.zip") as archive:
        require(archive.testzip() is None, "Entry ZIP CRC failure")
        entry = {item["name"]: archive.read(item["name"]) for item in manifest["files"]}
    for item in manifest["files"]:
        require(hashlib.sha256(entry[item["name"]]).hexdigest() == item["sha256"], "Entry member changed")
    independent_acceptance()
    before = entry["deferred-work.md"]
    after = (ROOT / "_bmad-output/implementation-artifacts/deferred-work.md").read_bytes()
    require(after.endswith(DEFER) and after.count(DEFER) == 1, "Exactly the reviewed pre-existing incoming defer is permitted")
    without_defer = after[:-len(DEFER)]
    old_records, new_records = records(before), records(without_defer)
    require(old_records.keys() == new_records.keys() and len(new_records) == 657, "Numbered IDs changed")
    for key, original in old_records.items():
        actual = new_records[key]
        if key in SELECTED:
            expected_result = CLOSURE_RESULT if key == 116 else CREATE_RESULT
            require(("resumed-batch-result: " + expected_result + "\n").encode() in actual, "Selected disposition differs")
            actual, count = ADDITION.subn(b"", actual)
            require(count == 1, "Selected result must be appended exactly once")
        if key == 116:
            require(actual.count(b"\nstatus: done 2026-10-30\n") == 1, "Only DW-116's approved closure is required")
            actual = actual.replace(b"\nstatus: done 2026-10-30\n", b"\nstatus: open\n", 1)
        require(actual == original, f"Original DW-{key} body changed")
    for anchor in UNNUMBERED_ANCHORS:
        original, actual = unnumbered_record(before, anchor), unnumbered_record(without_defer, anchor)
        additions = list(ADDITION.finditer(actual))
        require(len(additions) == 1 and additions[0]["indent"] == b"  ", "Unnumbered addition relocated")
        require(ADDITION.sub(b"", actual) == original, "Original unnumbered body changed")
    restored, count = ADDITION.subn(b"", without_defer)
    restored = RECORD.sub(lambda m: m[0].replace(b"\nstatus: done 2026-10-30\n", b"\nstatus: open\n", 1)
                         if int(m[1]) == 116 else m[0], restored)
    require(count == 5 and restored == before, "Unselected ledger bytes changed")
    counts = Counter(re.search(rb"^status: (\w+)", body, re.M)[1].decode() for body in new_records.values())
    require(counts == {"done": 655, "open": 2}, "Unexpected status changes")
    archive = (ROOT / "_bmad-output/implementation-artifacts/deferred-work-archive.md").read_bytes()
    appended = b"\n" + new_records[116] + b"archived: 2026-10-30\n"
    require(archive == entry["deferred-work-archive.md"] + appended, "Archive must preserve prefix and append only resolved DW-116")
    original_index = entry["maintenance-backlog.md"]
    lines = original_index.splitlines(True)
    expected_index = b"".join(b"" if line.startswith(b"| DW-116 |") else
        line[:-3] + INDEX_NOTE.encode() + b" |\n" if re.match(rb"\| DW-(446|459) \|", line) else line for line in lines)
    expected_index = expected_index.replace(b"# Maintenance backlog\n", b"# Maintenance backlog\n" + INDEX_SUMMARY.encode(), 1)
    require((ROOT / "docs/maintenance-backlog.md").read_bytes() == expected_index, "Only approved index closure/annotations are permitted")
    print(json.dumps({"numberedIds": 657, "unselectedNumberedBodiesPreserved": 654,
        "selectedHistoricalFieldsPreserved": True, "onlyApprovedClosure": "DW-116", "independentlyAcceptedSearchWindows": 16,
        "archivePrefixBytesUnchanged": True, "resolvedArchiveCopiesAppended": 1, "preExistingIncomingDefersAppended": 1,
        "statusCounts": dict(counts), "exactCoveringMaintenanceRows": 2}, indent=2))


if __name__ == "__main__":
    main()
