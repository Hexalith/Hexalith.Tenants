"""One bounded local-only refresh of this run's owned disposable checkout.

Uses local depth-one fetches, batched tar extraction and native Windows Python
fingerprints. Existing user checkouts and the source workspace are read only.
"""
import datetime
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time


HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location("inputs", HERE / "fingerprint-inputs.py")
inputs = importlib.util.module_from_spec(spec)
spec.loader.exec_module(inputs)


def save():
    output.write_text(json.dumps(state, indent=2) + "\n")


def run(argv, cwd):
    if time.monotonic() > deadline:
        raise TimeoutError("Ten-minute refresh bound exhausted")
    item = {"argv": argv, "cwd": str(cwd), "startedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat()}
    state["commands"].append(item)
    save()
    result = subprocess.run(argv, cwd=cwd, capture_output=True, timeout=min(180, deadline - time.monotonic()))
    item.update(endedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(), exitCode=result.returncode,
                stdout=result.stdout.decode("utf-8", errors="replace"), stderr=result.stderr.decode("utf-8", errors="replace"))
    save()
    if result.returncode:
        raise RuntimeError("Refresh command failed")


def win(path):
    return "C:\\" + str(path.relative_to("/mnt/c")).replace("/", "\\")


def main():
    global source, destination, output, deadline, state
    source, destination, output = map(Path, sys.argv[1:])
    deadline = time.monotonic() + 600
    state = {"startedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(), "localOnly": True,
             "source": str(source), "ownedDestination": str(destination), "commands": [], "repositories": []}
    guard_spec = importlib.util.spec_from_file_location("preparer", HERE / "prepare-native.py")
    preparer = importlib.util.module_from_spec(guard_spec)
    guard_spec.loader.exec_module(preparer)
    ownership = preparer.require_owned_destination(source, destination)
    state["ownershipMarker"] = ownership
    helper = Path("/mnt/c/Temp/tenants-resume-fingerprint-inputs.py")
    shutil.copyfile(HERE / "fingerprint-inputs.py", helper)
    paths = [""] + [line.split(" ", 1)[1] for line in subprocess.check_output(
        ["git", "config", "--file", ".gitmodules", "--get-regexp", "path"], cwd=source, text=True).splitlines()]
    for ordinal, relative in enumerate(paths):
        original, target = source / relative, destination / relative
        include_tests = not relative
        expected = inputs.fingerprint(original, include_tests)
        revision = expected["revision"]
        before_path = Path(f"/mnt/c/Temp/tenants-resume-refresh-{ordinal}-before.json")
        after_path = Path(f"/mnt/c/Temp/tenants-resume-refresh-{ordinal}-after.json")
        run(["/mnt/c/Python314/python.exe", win(helper), win(target), win(before_path), str(include_tests).lower()], destination)
        before = json.loads(before_path.read_bytes())
        if before["revision"] != revision:
            run(["git", "fetch", "--depth", "1", original.as_uri(), revision], target)
            run(["git", "checkout", "--detach", revision], target)
        run(["git", "sparse-checkout", "set", "--cone", "src", "tests", "Props", "eng"] if include_tests
            else ["git", "sparse-checkout", "set", "--cone", "src", "Props", "eng"], target)
        run(["/mnt/c/Python314/python.exe", win(helper), win(target), win(after_path), str(include_tests).lower()], destination)
        checked_out = json.loads(after_path.read_bytes())
        actual = {row["path"]: row for row in checked_out["files"]}
        changes = [row["path"] for row in expected["files"] if actual.get(row["path"]) != row]
        if changes:
            names = Path(f"/tmp/tenants-resume-refresh-{ordinal}.paths")
            names.write_bytes(b"\0".join(path.encode() for path in changes) + b"\0")
            archive = Path(f"/mnt/c/Temp/tenants-resume-refresh-{ordinal}.tar")
            run(["tar", "-cf", str(archive), "-C", str(original), "--null", "-T", str(names)], source)
            run(["/mnt/c/Windows/System32/tar.exe", "-xf", win(archive), "-C", win(target)], destination)
            run(["/mnt/c/Python314/python.exe", win(helper), win(target), win(after_path), str(include_tests).lower()], destination)
        refreshed = json.loads(after_path.read_bytes())
        source_after = inputs.fingerprint(original, include_tests)
        if expected["files"] != refreshed["files"] or expected != {**source_after, "capturedUtc": expected["capturedUtc"]}:
            raise ValueError("Source changed or refreshed bytes differ: " + relative)
        state["repositories"].append({"path": relative or ".", "before": before, "sourceBefore": expected,
                                      "after": refreshed, "sourceAfter": source_after, "changedPathsCopied": changes,
                                      "exactBytesEqual": True})
        save()
    state["endedUtc"] = datetime.datetime.now(datetime.timezone.utc).isoformat()
    state["completed"] = True
    save()


if __name__ == "__main__":
    main()
