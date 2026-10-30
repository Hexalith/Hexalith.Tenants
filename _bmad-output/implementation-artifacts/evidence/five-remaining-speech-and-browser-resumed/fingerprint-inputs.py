"""Fingerprint actual tracked/untracked source/build inputs on Linux or Windows."""
import datetime
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def fingerprint(repository, include_tests):
    paths = subprocess.check_output(["git", "ls-files", "-z", "--cached", "--others", "--exclude-standard"], cwd=repository).split(b"\0")
    rows = []
    directories = {"src", "Props", "eng"} | ({"tests"} if include_tests else set())
    for raw in sorted(set(paths)):
        if not raw:
            continue
        relative = raw.decode()
        parts = Path(relative).parts
        if len(parts) > 1 and parts[0] not in directories:
            continue
        if any(part in {"bin", "obj", "node_modules", "__pycache__"} for part in parts):
            continue
        file = repository / relative
        if file.is_file():
            rows.append({"path": relative.replace("\\", "/"), "bytes": file.stat().st_size,
                         "sha256": hashlib.sha256(file.read_bytes()).hexdigest()})
    canonical = json.dumps(rows, sort_keys=True, separators=(",", ":")).encode()
    return {"capturedUtc": datetime.datetime.now(datetime.timezone.utc).isoformat(),
            "revision": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=repository, text=True).strip(),
            "includeTests": include_tests, "manifestSha256": hashlib.sha256(canonical).hexdigest(), "files": rows}


if __name__ == "__main__":
    repository, output, include_tests = sys.argv[1:]
    Path(output).write_text(json.dumps(fingerprint(Path(repository), include_tests == "true"), indent=2) + "\n", encoding="utf-8")
