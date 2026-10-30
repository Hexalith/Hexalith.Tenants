"""Replay retained bytes, source bindings, runtime observations and matrix shape.

This checks evidence integrity, never independent speech acceptance or audio.
"""
import hashlib
import json
from pathlib import Path
import zipfile


HERE = Path(__file__).resolve().parent


def require(condition, message):
    if not condition:
        raise ValueError(message)


def digest(data):
    return hashlib.sha256(data).hexdigest()


def read(name):
    return json.loads((HERE / name).read_bytes())


def main():
    manifest = read("artifact-manifest.json")
    paths = [item["path"] for item in manifest["files"]]
    require(len(set(paths)) == len(paths), "Duplicate artifact paths")
    actual = {path.relative_to(HERE).as_posix() for path in HERE.rglob("*")
              if path.is_file() and path.name != "artifact-manifest.json"}
    require(actual == set(paths), "Manifest coverage differs")
    for item in manifest["files"]:
        path = HERE / item["path"]
        require(path.resolve().is_relative_to(HERE.resolve()), "Artifact path escapes")
        data = path.read_bytes()
        require(len(data) == item["bytes"] and digest(data) == item["sha256"], "Artifact bytes differ: " + item["path"])
        if path.suffix == ".json":
            json.loads(data)
        if path.suffix == ".zip":
            with zipfile.ZipFile(path) as archive:
                require(archive.testzip() is None, "ZIP CRC failure")
    source = read("executed-source-manifest.json")
    hashes = {}
    for item in source["source"]:
        path = (HERE / item["immutableSnapshot"]).resolve()
        require(path.is_relative_to(HERE.parent.resolve()), "Snapshot escapes evidence")
        require(digest(path.read_bytes()) == item["sha256"], "Executed source snapshot differs")
        hashes[Path(item["path"]).name] = item["sha256"]
    fields = {"validatorSha256": "validate-authenticated-administrator-dialogs.mjs",
              "handoffFixtureSha256": "deferred-handoff-fixture.mjs",
              "handoffChecksSha256": "validate-deferred-handoff.mjs",
              "nativeSpeechChecksSha256": "validate-deferred-native-speech.mjs",
              "oidcFixtureSha256": "local-oidc-fixture.mjs"}
    for invocation in read("browser-invocations.json")["invocations"]:
        packet_path = HERE / invocation["packet"]
        require(digest(packet_path.read_bytes()) == invocation["packetSha256"], "Browser invocation binding differs")
        packet = json.loads(packet_path.read_bytes())
        for field, name in fields.items():
            require(packet["sourceBeforeBuild"][field] == hashes[name], "Browser helper binding differs")
        require(packet["sourceBeforeBuild"]["sourceBaseRevision"] == source["rootRevision"], "Browser root differs")
        require(packet["sourceStableThroughCleanup"], "Browser source unstable")
        require(not packet["build"]["reusedBuild"] and not packet["build"]["skippedForDevelopment"], "Browser reused build")
    grant = read("grant-current-controls.json")
    control = next(case for case in grant["cases"] if case["scenario"] == "grant-escape-held-subscription")
    require(grant["passed"] and control["completionRejectedWhileHeld"]
            and control["scopedCallbackAndProbeRenderObserved"] and control["launcherEnabled"], "Current grant controls differ")
    require(control["beforeRelease"]["documentHasFocus"] and control["afterRelease"]["documentHasFocus"], "Grant foreground differs")
    grid = read("grid-external-failure.json")
    require(not grid["passed"] and grid["circuitFaults"], "External grid failure was waived")
    require(grid["gridLifetime"]["beforeNavigation"][0]["connected"] is True, "Grid initialization disconnected")
    require(grid["gridLifetime"]["afterRelease"][-1]["connected"] is False, "Grid release connected")
    require(grid["failure"]["label"] == "Held pinned grid continuation must preserve zero circuit errors", "Grid gate changed")
    matrix = read("native-matrix.json")
    rows = matrix["windows"]
    keys = {(row["pairing"], row["culture"], row["trial"]) for row in rows}
    trials = ["S0", "S1", "S2", "S3", "C1-lifecycle", "C1-availability", "C2-lifecycle", "C2-availability",
              "C3-events-lifecycle", "C3-events-availability", "C3-unavailable-lifecycle", "C3-unavailable-availability"]
    expected = {(pairing, culture, trial + "-" + culture)
                for pairing in ("NVDA/Chrome", "NVDA/Firefox") for culture in ("en", "fr") for trial in trials}
    require(keys == expected and len(rows) == 48, "Native matrix must cover the exact 48 windows")
    require(all(not row["acceptedForClosure"] for row in rows), "Independent acceptance claimed")
    for attempt in read("native-attempt-index.json")["attempts"]:
        with zipfile.ZipFile(HERE / attempt["packet"]) as archive:
            for item in attempt["retainedMembers"]:
                require(digest(archive.read(item["path"])) == item["sha256"], "Native member binding differs")
            capture = json.loads(archive.read("capture.json"))
            require(capture["collectorSha256"] == hashes["capture-deferred-nvda-speech.ps1"], "Collector binding differs")
            require(capture["observerSha256"] == hashes["deferred-nvda-observer.py"], "Observer binding differs")
            if "fixture.json" in archive.namelist():
                fixture = json.loads(archive.read("fixture.json"))
                for field, name in fields.items():
                    require(fixture["sourceBeforeBuild"][field] == hashes[name], "Native fixture source binding differs")
                require(fixture["sourceBeforeBuild"]["sourceBaseRevision"] == source["rootRevision"], "Native root differs")
    print(json.dumps({"artifactFiles": len(paths), "executedHelperBindings": len(hashes),
                      "grantCurrentControls": True, "gridExternalFailurePreserved": True,
                      "nativeMatrixWindows": 48, "independentAcceptanceClaimed": False}, indent=2))


if __name__ == "__main__":
    main()
