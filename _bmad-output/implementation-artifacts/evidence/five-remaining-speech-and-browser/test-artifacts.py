"""Replay artifact-checker tamper controls on isolated evidence copies."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


HERE = Path(__file__).resolve().parent


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def refresh_manifest(directory):
    write_json(directory / "artifact-manifest.json", {"files": [
        {"path": p.relative_to(directory).as_posix(), "bytes": p.stat().st_size,
         "sha256": hashlib.sha256(p.read_bytes()).hexdigest()}
        for p in sorted(directory.rglob("*")) if p.is_file() and p != directory / "artifact-manifest.json"]})


def main():
    results = []
    for mutation in ("none", "unbound packet bytes", "matrix coverage", "browser scenario flag", "native source link"):
        with tempfile.TemporaryDirectory(prefix="five-remaining-artifact-controls-") as temporary:
            directory = Path(temporary) / "evidence"
            shutil.copytree(HERE, directory)
            if mutation == "unbound packet bytes":
                with (directory / "grant-browser-build-blocked.json").open("ab") as stream:
                    stream.write(b"\n")
            elif mutation == "matrix coverage":
                path = directory / "native-matrix.json"
                data = json.loads(path.read_bytes())
                data["cells"].pop()
                write_json(path, data)
            elif mutation == "browser scenario flag":
                path = directory / "browser-invocations.json"
                data = json.loads(path.read_bytes())
                data["invocations"][0]["explicitEnvironment"].pop("TENANTS_BROWSER_RUNTIME_FOLLOWUPS")
                write_json(path, data)
            elif mutation == "native source link":
                relative = "native-attempts/chrome-attempt-3/fixture.json"
                path = directory / relative
                data = json.loads(path.read_bytes())
                for field in ("sourceBeforeBuild", "sourceAfterCleanup"):
                    data[field]["nativeSpeechChecksSha256"] = "0" * 64
                write_json(path, data)
                path = directory / "native-attempt-index.json"
                data = json.loads(path.read_bytes())
                for attempt in data["attempts"]:
                    if attempt["attempt"] == "chrome-attempt-3":
                        for item in attempt["files"]:
                            if item["name"] == "fixture.json":
                                item["retainedSha256"] = hashlib.sha256((directory / relative).read_bytes()).hexdigest()
                write_json(path, data)
            if mutation not in ("none", "unbound packet bytes"):
                # Semantic controls must fail even after their byte manifests are refreshed.
                refresh_manifest(directory)
            run = subprocess.run([sys.executable, "-B", "-O", str(directory / "validate-artifacts.py")],
                                 capture_output=True, text=True)
            expected = 0 if mutation == "none" else 1
            if (run.returncode != 0) != bool(expected) or (expected and "ValueError:" not in run.stderr):
                raise ValueError(f"Unexpected artifact control result: {mutation}: {run.stderr}")
            results.append({"mutation": mutation, "optimized": True, "exitCode": run.returncode,
                            "diagnostic": run.stderr.splitlines()[-1] if run.stderr else "accepted"})
    print(json.dumps({"isolatedTemporaryCopiesOnly": True, "controls": results}, indent=2))


if __name__ == "__main__":
    main()
