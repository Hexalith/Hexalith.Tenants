"""Prospective bounded preparation; pre-review executed version is archived separately."""
import datetime
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import time
import uuid

OWNER_FILE = "tenants-native-fixture-owner.json"
OWNER = "five-remaining-speech-and-browser-resumed"


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def destination_guard(source, destination):
    source, destination = source.resolve(), destination.resolve()
    if source == destination or destination.is_relative_to(source) or source.is_relative_to(destination):
        raise ValueError("Destination overlaps source")
    if not destination.name.startswith("tenants-five-resumed-"):
        raise ValueError("Destination is historical or outside this run's disposable namespace")
    return source, destination


def establish_owner(source, destination, run_id):
    source, destination = destination_guard(source, destination)
    git = destination / ".git"
    if not git.is_dir() or git.is_symlink():
        raise ValueError("Owned fixture must have its own Git directory")
    marker = {"owner": OWNER, "runId": run_id, "source": str(source), "destination": str(destination)}
    with (git / OWNER_FILE).open("x", encoding="utf-8") as stream:
        json.dump(marker, stream, indent=2)
    return marker


def require_owned_destination(source, destination):
    source, destination = destination_guard(source, destination)
    git = destination / ".git"
    if not git.is_dir() or git.is_symlink() or not (git / OWNER_FILE).is_file():
        raise ValueError("Destination has no preparer ownership marker")
    marker = json.loads((git / OWNER_FILE).read_bytes())
    if not (marker.get("owner") == OWNER and marker.get("source") == str(source)
            and marker.get("destination") == str(destination) and uuid.UUID(marker["runId"]).version == 4):
        raise ValueError("Destination ownership marker does not match this source/run")
    return marker


class Preparation:
    def __init__(self, source, destination, output, overall_seconds=900, command_seconds=180,
                 clock=time.monotonic, execute=subprocess.run):
        if not (0 < overall_seconds <= 900 and 0 < command_seconds <= 180):
            raise ValueError("Preparation deadlines may only narrow the approved 15-minute bound")
        self.source, self.destination, self.output = Path(source), Path(destination), Path(output)
        self.clock, self.execute = clock, execute
        self.deadline, self.command_seconds = clock() + overall_seconds, command_seconds
        self.state = {"startedUtc": now(), "source": str(source), "destination": str(destination),
                      "commands": [], "repositories": [], "nestedDependenciesInitialized": False,
                      "overallSeconds": overall_seconds, "commandSeconds": command_seconds,
                      "runId": str(uuid.uuid4()), "completed": False}

    def save(self):
        self.output.write_text(json.dumps(self.state, indent=2) + "\n")

    def check(self, stage):
        remaining = self.deadline - self.clock()
        if remaining <= 0:
            self.state["failure"] = {"kind": "overall-timeout", "stage": stage, "observedUtc": now()}
            self.save()
            raise TimeoutError("Preparation overall deadline exhausted: " + stage)
        return remaining

    def run(self, args, cwd):
        limit = min(self.command_seconds, self.check("before command"))
        item = {"argv": args, "cwd": str(cwd), "startedUtc": now(), "timeoutSeconds": limit}
        self.state["commands"].append(item)
        self.save()
        try:
            result = self.execute(args, cwd=cwd, capture_output=True, timeout=limit)
        except subprocess.TimeoutExpired as error:
            item.update(endedUtc=now(), exitCode=None, timedOut=True,
                        stdout=(error.stdout or b"").decode("utf-8", errors="replace"),
                        stderr=(error.stderr or b"").decode("utf-8", errors="replace"))
            self.state["failure"] = {"kind": "command-timeout", "argv": args, "observedUtc": now()}
            self.save()
            raise TimeoutError("Preparation command deadline exhausted") from error
        item.update(endedUtc=now(), exitCode=result.returncode, timedOut=False,
                    stdout=result.stdout.decode("utf-8", errors="replace"),
                    stderr=result.stderr.decode("utf-8", errors="replace"))
        self.save()
        self.check("after command")
        if result.returncode:
            raise RuntimeError("Preparation command failed: " + repr(args))
        return result.stdout.decode()

    def fingerprint(self, repository):
        paths = self.run(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], repository).split("\0")
        rows = []
        for relative in sorted(set(paths)):
            self.check("fingerprint input")
            if not relative:
                continue
            parts = Path(relative).parts
            if len(parts) > 1 and parts[0] not in {"src", "tests", "Props", "eng"}:
                continue
            if any(part in {"bin", "obj", "node_modules"} for part in parts):
                continue
            file = repository / relative
            if file.is_file():
                data = file.read_bytes()
                self.check("after input read")
                rows.append({"path": relative, "bytes": len(data), "sha256": hashlib.sha256(data).hexdigest()})
        return {"capturedUtc": now(), "files": rows,
                "manifestSha256": hashlib.sha256(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode()).hexdigest()}

    def prepare(self):
        source, destination = destination_guard(self.source, self.destination)
        if destination.exists():
            raise ValueError("Fresh destination required")
        dependencies = self.run(["git", "config", "--file", ".gitmodules", "--get-regexp", "path"], source)
        paths = [""] + [line.split(" ", 1)[1] for line in dependencies.splitlines()]
        for relative in paths:
            self.check("repository preparation")
            original, target = source / relative, destination / relative
            revision = self.run(["git", "rev-parse", "HEAD"], original).strip()
            before = self.fingerprint(original)
            self.run(["git", "clone", "--no-checkout", "--depth", "1", "--no-local", original.as_uri(), str(target)], source)
            if not relative:
                self.state["ownershipMarker"] = establish_owner(source, destination, self.state["runId"])
                self.save()
            self.run(["git", "sparse-checkout", "set", "--cone", "src", "tests", "Props", "eng"], target)
            self.run(["git", "checkout", "--detach", revision], target)
            for item in before["files"]:
                self.check("before exact-byte copy")
                local = target / item["path"]
                local.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(original / item["path"], local)
                self.check("after exact-byte copy")
            actual, after = self.fingerprint(target), self.fingerprint(original)
            if before["files"] != actual["files"] or before["files"] != after["files"]:
                raise ValueError("Source bytes differ or changed: " + relative)
            self.state["repositories"].append({"path": relative or ".", "revision": revision,
                "sourceBefore": before, "native": actual, "sourceAfter": after, "exactBytesEqual": True})
            self.save()
        self.check("completion")
        self.state.update(endedUtc=now(), completed=True)
        self.save()


def main():
    preparation = Preparation(*map(Path, sys.argv[1:]))
    try:
        preparation.prepare()
    except Exception as error:
        preparation.state.setdefault("failure", {"kind": type(error).__name__, "message": str(error)})
        preparation.state["endedUtc"] = now()
        preparation.save()
        raise


if __name__ == "__main__":
    main()
