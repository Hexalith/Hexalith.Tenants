"""Integrity tamper controls run only in isolated disposable evidence copies."""
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import zipfile


HERE = Path(__file__).resolve().parent


def require(condition, message):
    if not condition:
        raise ValueError(message)


def write(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def refresh(directory):
    path = directory / "artifact-manifest.json"
    manifest = json.loads(path.read_bytes())
    manifest["files"] = [{"path": item.relative_to(directory).as_posix(), "bytes": item.stat().st_size,
                          "sha256": hashlib.sha256(item.read_bytes()).hexdigest()}
                         for item in sorted(directory.rglob("*")) if item.is_file() and item != path]
    write(path, manifest)


def edit_json(directory, name, mutate):
    path = directory / name
    value = json.loads(path.read_bytes())
    mutate(value)
    write(path, value)


def bind_browser(directory, name):
    fingerprint = hashlib.sha256((directory / name).read_bytes()).hexdigest()
    edit_json(directory, "browser-invocations.json", lambda data: [row.update(packetSha256=fingerprint)
              for row in data["invocations"] if row["packet"] == name])


def edit_packet(directory, member, mutate):
    """Rebind temporary bytes, isolating semantic rejection from simple hashes."""
    index_path = directory / "native-attempt-index.json"
    index = json.loads(index_path.read_bytes())
    attempt = next(row for row in index["attempts"] if row["label"] == "current-chrome-matrix-1")
    path = directory / attempt["packet"]
    with zipfile.ZipFile(path) as archive:
        members = {name: archive.read(name) for name in archive.namelist()}
    value = json.loads(members[member]); mutate(value)
    members[member] = (json.dumps(value, indent=2) + "\n").encode()
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as archive:
        for name, raw in members.items(): archive.writestr(name, raw)
    attempt["packetSha256"] = hashlib.sha256(path.read_bytes()).hexdigest()
    attempt["retainedMembers"] = [{"path": name, "bytes": len(raw), "sha256": hashlib.sha256(raw).hexdigest()}
                                  for name, raw in members.items()]
    write(index_path, index)


def first_trial(data):
    return data["nativeSpeechTrials"][0]["trial"]


def main():
    results = []
    cases = ["positive", "unbound bytes", "matrix coverage", "native observer binding", "grid gate", "grant negative control", "speech receipt boundary"]
    cases += ["capture failed", "capture incomplete", "runner failed", "invocation failed", "build failed", "build reused",
              "runtime unstable", "runtime failure", "missing raw Search speech", "missing raw cancellation",
              "runtime browser identity", "runtime assembly identity", "runtime count", "runtime cleanup",
              "native packet SHA", "partial native member inventory", "duplicate native member inventory", "duplicate ZIP member"]
    cases += [stage + " repositories " + mutation for stage in ("preparation", "refresh", "final") for mutation in ("omitted", "duplicated")]
    for case in cases:
        with tempfile.TemporaryDirectory(prefix="tenants-resumed-artifact-control-") as temporary:
            base = Path(temporary)
            directory = base / HERE.name
            shutil.copytree(HERE, directory)
            # Copy only the immutable helper links needed by this check.
            source = json.loads((directory / "executed-source-manifest.json").read_bytes())
            for item in source["source"]:
                for version in [item] + item.get("previousVersions", []):
                    relative = version["immutableSnapshot"]
                    destination = directory / relative
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    shutil.copyfile(HERE / relative, destination)
            if case == "unbound bytes":
                path = directory / "grant-current-controls.json"
                path.write_bytes(path.read_bytes() + b" ")
            elif case == "matrix coverage":
                edit_json(directory, "native-matrix.json", lambda data: data["windows"][0].update(trial="S9-en"))
            elif case == "native observer binding":
                edit_json(directory, "native-attempt-index.json", lambda data: data["attempts"][0].update(observerSha256="0" * 64))
            elif case == "grid gate":
                edit_json(directory, "grid-external-failure.json", lambda data: data["failure"].update(label="waived"))
                bind_browser(directory, "grid-external-failure.json")
            elif case == "grant negative control":
                edit_json(directory, "grant-current-controls.json", lambda data: next(row for row in data["cases"]
                    if row["scenario"] == "grant-escape-held-subscription").update(completionRejectedWhileHeld=False))
                bind_browser(directory, "grant-current-controls.json")
            elif case == "speech receipt boundary":
                edit_json(directory, "speech-inspection.json", lambda data: data["windows"][0]["sampleArrivalSegments"].update(
                    matchedStateBeforeCollectorReceipt=999))
            elif case in {"capture failed", "capture incomplete", "runner failed"}:
                key, value = {"capture failed": ("passed", False), "capture incomplete": ("completed", False), "runner failed": ("runnerExitCode", 1)}[case]
                edit_packet(directory, "capture.json", lambda data: data.update({key: value}))
            elif case == "invocation failed":
                edit_packet(directory, "invocation.json", lambda data: data.update(exitCode=1))
            elif case in {"build failed", "build reused"}:
                edit_packet(directory, "fixture.json", lambda data: data["build"].update(
                    {"succeeded": False} if case == "build failed" else {"reusedBuild": True}))
            elif case == "runtime unstable":
                edit_packet(directory, "fixture.json", lambda data: data["sourceAfterBrowser"].update(sourceDiffSha256="0" * 64))
            elif case == "runtime failure":
                edit_packet(directory, "fixture.json", lambda data: data.update(failure={"label": "failed"}))
            elif case == "missing raw Search speech":
                edit_packet(directory, "speech.json", lambda data: data.__setitem__(slice(None),
                    [row for row in data if not row["trial"].endswith("-focus-control")]))
            elif case == "missing raw cancellation":
                edit_packet(directory, "timeline.json", lambda data: data.__setitem__(slice(None),
                    [row for row in data if row.get("kind") != "nvda-speech-canceled"]))
            elif case.startswith("runtime "):
                key, value = {"runtime browser identity": ("browserVersion", "contradiction"),
                    "runtime assembly identity": ("uiAssemblySha256", "0" * 64), "runtime count": ("completeWindows", 25),
                    "runtime cleanup": ("nvdaProcessesAfter", [{"Id": 999, "ProcessName": "nvda"}])}[case]
                edit_json(directory, "native-runtime-summary.json", lambda data: data["pairings"][0].update({key: value}))
            elif case == "native packet SHA":
                edit_json(directory, "native-attempt-index.json", lambda data: data["attempts"][0].update(packetSha256="0" * 64))
            elif case in {"partial native member inventory", "duplicate native member inventory"}:
                def inventory(data):
                    rows = data["attempts"][0]["retainedMembers"]
                    rows.pop() if case.startswith("partial") else rows.append(dict(rows[0]))
                edit_json(directory, "native-attempt-index.json", inventory)
            elif case == "duplicate ZIP member":
                data = json.loads((directory / "native-attempt-index.json").read_bytes()); row = data["attempts"][0]; packet = directory / row["packet"]
                with zipfile.ZipFile(packet, "a") as archive: archive.writestr("speech.json", archive.read("speech.json"))
                row["packetSha256"] = hashlib.sha256(packet.read_bytes()).hexdigest(); write(directory / "native-attempt-index.json", data)
            elif " repositories " in case:
                stage, mutation = case.split(" repositories ")
                filename = {"preparation": "native-preparation.json", "refresh": "native-refresh.json", "final": "native-final-input-verification.json"}[stage]
                def repositories(data):
                    rows = data["repositories"]
                    rows.pop() if mutation == "omitted" else rows.__setitem__(-1, dict(rows[0]))
                edit_json(directory, filename, repositories)
            if case not in {"positive", "unbound bytes"}:
                refresh(directory)
            run = subprocess.run([sys.executable, "-B", "-O", str(directory / "validate-artifacts.py")], capture_output=True, text=True)
            if case == "positive":
                require(run.returncode == 0, run.stderr)
            else:
                require(run.returncode != 0 and "ValueError:" in run.stderr, "Tamper accepted: " + case)
            results.append({"case": case, "exitCode": run.returncode,
                            "diagnostic": run.stderr.splitlines()[-1] if run.stderr else "accepted"})
    print(json.dumps({"isolatedTemporaryCopiesOnly": True, "optimizedPython": True, "controls": results}, indent=2))


if __name__ == "__main__":
    main()
