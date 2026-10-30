"""Replay conservation controls on disposable copies, including optimized Python."""
import json
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
LEDGER = "_bmad-output/implementation-artifacts/deferred-work.md"
ARCHIVE = "_bmad-output/implementation-artifacts/deferred-work-archive.md"
INDEX = "docs/maintenance-backlog.md"


def require(condition, message):
    if not condition:
        raise ValueError(message)


def relocate_unnumbered(data):
    pattern = re.compile(rb"\n  remaining-batch-date: 2026-10-30\n  remaining-batch-result: [^\n]+\n  remaining-batch-evidence: [^\n]+\n")
    additions = pattern.findall(data)
    require(len(additions) == 2, "Expected both unnumbered additions")
    return b"".join(additions) + pattern.sub(b"", data)


def main():
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
    ]
    result = {"isolatedTemporaryCopiesOnly": True, "positiveControls": [], "negativeControls": []}
    with tempfile.TemporaryDirectory(prefix="five-remaining-conservation-controls-") as directory:
        temporary = Path(directory)
        evidence = temporary / HERE.relative_to(ROOT)
        evidence.mkdir(parents=True)
        for name in ("entry-ledgers.zip", "entry-ledger-manifest.json", "validate-conservation.py"):
            shutil.copyfile(HERE / name, evidence / name)
        originals = {}
        for path in (LEDGER, ARCHIVE, INDEX):
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
