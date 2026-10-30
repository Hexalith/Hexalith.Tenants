"""Create a fresh, shallow, nonrecursive native fixture and journal exact commands.

Run from WSL with explicit source, destination and output arguments. No existing
checkout is reused. Root-declared dependency HEADs are consumed without updating
their source checkouts. Fingerprints cover tracked root/build/source/test files,
plus untracked files in those paths; references and generated outputs are excluded.
"""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def run(args, cwd, journal):
    item = {"argv": args, "cwd": str(cwd), "startedUtc": now()}
    result = subprocess.run(args, cwd=cwd, capture_output=True)
    item.update(endedUtc=now(), exitCode=result.returncode,
                stdout=result.stdout.decode("utf-8", errors="replace"),
                stderr=result.stderr.decode("utf-8", errors="replace"))
    journal.append(item)
    output.write_text(json.dumps(state, indent=2) + "\n")
    if result.returncode:
        raise RuntimeError("Preparation command failed: " + repr(args))
    return result.stdout.decode().strip()


def fingerprint(repository):
    paths = subprocess.check_output(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=repository).split(b"\0")
    rows = []
    for raw in sorted(set(paths)):
        if not raw:
            continue
        relative = raw.decode()
        parts = Path(relative).parts
        if len(parts) > 1 and parts[0] not in {"src", "tests", "Props", "eng"}:
            continue
        if any(part in {"bin", "obj", "node_modules"} for part in parts):
            continue
        file = repository / relative
        if file.is_file():
            rows.append({"path": relative, "bytes": file.stat().st_size,
                         "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
    digest = hashlib.sha256(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode()).hexdigest()
    return {"capturedUtc": now(), "files": rows, "manifestSha256": digest}


source, destination, output = map(Path, sys.argv[1:])
if destination.exists():
    raise ValueError("Fresh destination required")
state = {"startedUtc": now(), "source": str(source), "destination": str(destination),
         "commands": [], "repositories": [], "nestedDependenciesInitialized": False}
journal = state["commands"]
dependencies = subprocess.check_output(["git", "config", "--file", ".gitmodules", "--get-regexp", "path"], cwd=source, text=True)
paths = [""] + [line.split(" ", 1)[1] for line in dependencies.splitlines()]
for relative in paths:
    original = source / relative
    target = destination / relative
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=original, text=True).strip()
    before = fingerprint(original)
    run(["git", "clone", "--no-checkout", "--depth", "1", "--no-local", original.as_uri(), str(target)], source, journal)
    run(["git", "sparse-checkout", "set", "--cone", "src", "tests", "Props", "eng"], target, journal)
    run(["git", "checkout", "--detach", revision], target, journal)
    # Git checkout applies attributes and platform newline conversion. Preserve
    # the exact consumed worktree bytes, including CRLF, rather than claiming
    # object equality establishes byte equality.
    copied = []
    for item in before["files"]:
        local = target / item["path"]
        local.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(original / item["path"], local)
        copied.append(item["path"])
    journal.append({"operation": "shutil.copyfile exact consumed worktree bytes",
                    "source": str(original), "destination": str(target),
                    "completedUtc": now(), "paths": copied})
    actual = fingerprint(target)
    after = fingerprint(original)
    if before["files"] != actual["files"] or before["files"] != after["files"]:
        raise ValueError("Source bytes differ or changed: " + relative)
    state["repositories"].append({"path": relative or ".", "revision": revision,
                                  "sourceBefore": before, "native": actual,
                                  "sourceAfter": after, "exactBytesEqual": True})
    output.write_text(json.dumps(state, indent=2) + "\n")
state["endedUtc"] = now()
state["completed"] = True
output.write_text(json.dumps(state, indent=2) + "\n")
