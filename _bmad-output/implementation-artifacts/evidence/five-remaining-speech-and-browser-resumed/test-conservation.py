"""Replay conservation controls on disposable copies, including optimized Python."""
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile
import zipfile


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
ARCHIVE = "_bmad-output/implementation-artifacts/deferred-work-archive.md"
INDEX = "docs/maintenance-backlog.md"
ASSESSMENT = str(HERE.relative_to(ROOT) / "independent-at-assessment.json")
MATRIX = str(HERE.relative_to(ROOT) / "native-matrix.json")


def require(condition, message):
    if not condition:
        raise ValueError(message)


def relocate_unnumbered(data):
    pattern = re.compile(rb"\n  resumed-batch-date: 2026-10-30\n  resumed-batch-result: [^\n]+\n  resumed-batch-evidence: [^\n]+\n")
    additions = pattern.findall(data)
    require(len(additions) == 2, "Expected both unnumbered additions")
    return b"".join(additions) + pattern.sub(b"", data)


def main():
    with zipfile.ZipFile(HERE / "entry-ledgers.zip") as archive:
        archive_prefix = archive.read("deferred-work-archive.md")
    def acceptance_false(raw):
        data = json.loads(raw); data["recordVerdicts"]["DW-116"]["accepted"] = False
        return json.dumps(data).encode()
    def matrix_upgraded(raw):
        data = json.loads(raw); data["independentlyAcceptedWindows"] = 16
        return json.dumps(data).encode()
    def reopen_116(raw):
        return re.sub(rb"(?ms)(^### DW-116:.*?\n)status: done 2026-10-30\n", rb"\1status: open\n", raw, count=1)
    mutations = [
        ("unselected numbered history", LEDGER, lambda b: b.replace(b"### DW-1:", b"### DW-1: changed", 1)),
        ("selected original field", LEDGER, lambda b: b.replace(b"location: TenantListSurfaceTests.cs:1866", b"location: TenantListSurfaceTests.cs:1867", 1)),
        ("numbered ID conservation", LEDGER, lambda b: b.replace(b"### DW-1:", b"### DW-658:", 1)),
        ("numbered status", LEDGER, lambda b: b.replace(b"\nstatus: open\n", b"\nstatus: done\n", 1)),
        ("unselected ledger preamble", LEDGER, lambda b: b"changed\n" + b),
        ("archive byte conservation", ARCHIVE, lambda b: b + b"changed\n"),
        ("unselected index history", INDEX, lambda b: b"changed\n" + b),
        ("ledger LF to CRLF rewrite", LEDGER, lambda b: b.replace(b"\n", b"\r\n")),
        ("index LF to CRLF rewrite", INDEX, lambda b: b.replace(b"\n", b"\r\n")),
        ("both unnumbered additions relocated to preamble", LEDGER, relocate_unnumbered),
        ("approved DW-116 closure removed", LEDGER, reopen_116),
        ("resolved archive copy missing", ARCHIVE, lambda b: archive_prefix),
        ("historical archive prefix changed", ARCHIVE, lambda b: b"X" + b[1:]),
        ("pre-existing defer impact upgraded", LEDGER, lambda b: b.replace(b"production-impact: Unverified;", b"production-impact: Proven;", 1)),
        ("independent acceptance missing", ASSESSMENT, lambda b: None),
        ("independent DW-116 acceptance rejected", ASSESSMENT, acceptance_false),
        ("capture matrix falsely upgraded to acceptance", MATRIX, matrix_upgraded),
    ]
    result = {"isolatedTemporaryCopiesOnly": True, "positiveControls": [], "negativeControls": []}
    with tempfile.TemporaryDirectory(prefix="five-remaining-conservation-controls-") as directory:
        temporary = Path(directory)
        evidence = temporary / HERE.relative_to(ROOT)
        evidence.mkdir(parents=True)
        for name in ("entry-ledgers.zip", "entry-ledger-manifest.json", "validate-conservation.py"):
            shutil.copyfile(HERE / name, evidence / name)
        assessment = json.loads((HERE / "independent-at-assessment.json").read_bytes())
        for packet in assessment["packets"]:
            destination = evidence / packet["path"]
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(HERE / packet["path"], destination)
        originals = {}
        for path in (LEDGER, ARCHIVE, INDEX, ASSESSMENT, MATRIX):
            destination = temporary / path
            destination.parent.mkdir(parents=True, exist_ok=True)
            originals[path] = (ROOT / path).read_bytes()
            destination.write_bytes(originals[path])
        script = str(evidence / "validate-conservation.py")
        for optimized in (False, True):
            command = [sys.executable, "-B"] + (["-O"] if optimized else []) + [script]
            run = subprocess.run(command, capture_output=True, text=True)
            require(run.returncode == 0, run.stderr)
            result["positiveControls"].append({"optimized": optimized, "exitCode": run.returncode})
        for label, path, mutation in mutations:
            changed = mutation(originals[path])
            require(changed != originals[path], "Ineffective mutation: " + label)
            destination = temporary / path
            if changed is None:
                destination.unlink()
            else:
                destination.write_bytes(changed)
            try:
                run = subprocess.run([sys.executable, "-B", "-O", script], capture_output=True, text=True)
                require(run.returncode != 0 and "ValueError:" in run.stderr, "Mutation accepted: " + label)
                result["negativeControls"].append({"mutation": label, "optimized": True, "rejected": True,
                    "diagnostic": run.stderr.splitlines()[-1]})
            finally:
                destination.write_bytes(originals[path])
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
