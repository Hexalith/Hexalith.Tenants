"""Replay retained manifest, source/packet and matrix integrity; no runtime claims."""
import hashlib
import json
from pathlib import Path
import zipfile


HERE = Path(__file__).resolve().parent


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_json(path):
    return json.loads(path.read_bytes())


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def child(root, relative):
    path = root / relative
    require(not Path(relative).is_absolute() and path.resolve().is_relative_to(root.resolve()),
            "Artifact path escapes its directory")
    return path


def main():
    manifest = read_json(HERE / "artifact-manifest.json")
    paths = [item["path"] for item in manifest["files"]]
    require(len(paths) == len(set(paths)), "Duplicate manifest paths")
    actual = {path.relative_to(HERE).as_posix() for path in HERE.rglob("*") if path.is_file()
              and path != HERE / "artifact-manifest.json"}
    require(set(paths) == actual, "Manifest must cover every retained file except itself")
    for item in manifest["files"]:
        path = child(HERE, item["path"])
        require(path.stat().st_size == item["bytes"] and sha256(path) == item["sha256"],
                "Manifest hash/size mismatch: " + item["path"])
        if path.suffix == ".json":
            read_json(path)
        if path.suffix == ".zip":
            with zipfile.ZipFile(path) as archive:
                require(archive.testzip() is None, "ZIP CRC failure")

    entry = read_json(HERE / "entry-ledger-manifest.json")
    require(sha256(HERE / "entry-ledgers.zip") == entry["archiveSha256"], "Entry archive hash mismatch")
    with zipfile.ZipFile(HERE / "entry-ledgers.zip") as archive:
        require(set(archive.namelist()) == {item["name"] for item in entry["files"]}, "Entry members differ")
        for item in entry["files"]:
            require(hashlib.sha256(archive.read(item["name"])).hexdigest() == item["sha256"], "Entry member hash mismatch")

    source = read_json(HERE / "executed-source-manifest.json")
    names = [Path(item["path"]).name for item in source["source"]]
    require(len(names) == len(set(names)), "Ambiguous source snapshot names")
    hashes = {}
    for item in source["source"]:
        name = Path(item["path"]).name
        require(sha256(child(HERE / "executed-source", name)) == item["sha256"], "Helper snapshot hash mismatch")
        hashes[name] = item["sha256"]
    identity = read_json(HERE / "native-checkout-identity.json")
    require(identity["rootRevision"] == source["baselineCommit"], "Root revision disagreement")
    for item in identity["sourceSnapshots"]:
        require(hashes[Path(item["path"]).name] == item["nativeSha256"], "Native helper fingerprint disagreement")
    fields = {
        "validatorSha256": "validate-authenticated-administrator-dialogs.mjs",
        "handoffFixtureSha256": "deferred-handoff-fixture.mjs",
        "handoffChecksSha256": "validate-deferred-handoff.mjs",
        "nativeSpeechChecksSha256": "validate-deferred-native-speech.mjs",
        "oidcFixtureSha256": "local-oidc-fixture.mjs",
    }

    def fixture_checks(fixture):
        require(fixture["failure"]["phase"] == "build-current-ui" and fixture["cases"] == [],
                "Unexpected product execution in build-blocked fixture")
        require(fixture["acceptedForClosure"] is False and fixture["build"]["succeeded"] is False,
                "Build-blocked fixture claims acceptance")
        require(not fixture["build"]["skippedForDevelopment"] and not fixture["build"]["reusedBuild"],
                "Fresh-build selection changed")
        before = fixture["sourceBeforeBuild"]
        require(before == fixture["sourceAfterCleanup"] and fixture["sourceStableThroughCleanup"] is True,
                "Captured source fingerprints changed")
        require(before["sourceBaseRevision"] == source["baselineCommit"], "Fixture root revision disagreement")
        for field, name in fields.items():
            require(before[field] == hashes[name], "Fixture/source link mismatch: " + field)

    index = read_json(HERE / "native-attempt-index.json")
    attempts = {item["attempt"]: item for item in index["attempts"]}
    require(len(attempts) == len(index["attempts"]) == 5, "Missing or duplicated native attempts")
    packet_files = 0
    for name, attempt in attempts.items():
        directory = child(HERE / "native-attempts", name)
        files = [item["name"] for item in attempt["files"]]
        require(len(files) == len(set(files)) and set(files) == {p.name for p in directory.iterdir() if p.is_file()},
                "Native packet file coverage differs")
        for item in attempt["files"]:
            require(sha256(child(directory, item["name"])) == item["retainedSha256"], "Native retained hash mismatch")
            packet_files += 1
        capture = read_json(directory / "capture.json")
        require(capture["collectorSha256"] == hashes["capture-deferred-nvda-speech.ps1"]
                and capture["observerSha256"] == hashes["deferred-nvda-observer.py"], "Collector/source link mismatch")
        require(capture["engine"] == attempt["engine"] and capture["startedUtc"] == attempt["startedUtc"]
                and capture["finishedUtc"] == attempt["finishedUtc"], "Attempt/capture identity mismatch")
        require(capture["passed"] is False and capture["completed"] is False
                and capture["sourceSettingsUnchanged"] is True and attempt["originalSettingsUnchanged"] is True,
                "Failed collector flags/settings report changed")
        require(attempt["exitCode"] == 1 and attempt["completedProductWindows"] == attempt["speechSamples"] == 0,
                "Failed attempt counts changed")
        require(read_json(directory / "speech.json") == [] and read_json(directory / "timeline.json") == [],
                "Product capture unexpectedly present")
        if attempt["status"] == "collector-build-failed":
            fixture_checks(read_json(directory / "fixture.json"))
        else:
            require(attempt["status"] == "collector-setup-failed" and "fixture.json" not in files,
                    "Setup/build boundary changed")

    invocations = read_json(HERE / "browser-invocations.json")["invocations"]
    require({item["scenario"] for item in invocations} == {"grant", "grid"} and len(invocations) == 2,
            "Browser invocation coverage differs")
    for invocation in invocations:
        packet = child(HERE, invocation["packet"])
        require(sha256(packet) == invocation["packetSha256"], "Browser invocation/packet hash mismatch")
        fixture_checks(read_json(packet))
        flags = invocation["explicitEnvironment"]
        require(flags["TENANTS_BROWSER_STANDALONE_OIDC"] == flags["TENANTS_BROWSER_UI_SAFETY_ONLY"] == "1",
                "Browser fixture/safety flags differ")
        selected = "TENANTS_BROWSER_RUNTIME_FOLLOWUPS" if invocation["scenario"] == "grant" else "TENANTS_BROWSER_HOLD_GRID"
        require(flags.get(selected) == "1", "Browser scenario flag differs")

    required = set()
    for culture in ("en", "fr"):
        required.update(f"S{case}-{culture}" for case in range(4))
        for status, first in (("events", "C1"), ("unavailable", "C2")):
            for refresh in ("lifecycle", "availability"):
                required.add(f"{first}-{refresh}-{culture}")
                required.add(f"C3-{status}-{refresh}-{culture}")
    matrix = read_json(HERE / "native-matrix.json")
    pairs = {(cell["engine"], cell["trial"]) for cell in matrix["cells"]}
    require(len(matrix["cells"]) == len(pairs) == 48
            and pairs == {(engine, trial) for engine in ("chromium", "firefox") for trial in required},
            "Native matrix does not exactly cover both full plans")
    for cell in matrix["cells"]:
        attempt = attempts[cell["finalAttempt"]]
        require(cell["engine"] == attempt["engine"] and cell["culture"] == cell["trial"][-2:]
                and attempt["status"] == "collector-build-failed", "Matrix/attempt identity mismatch")
        require(cell["status"] == "blocked-before-product-capture" and all(cell[field] is False for field in
                ("observationComplete", "positiveControlVerified", "cancellationResponseVerified", "independentlyAccepted")),
                "Build-blocked matrix cell claims qualification")
    require(matrix["requiredWindowsTotal"] == 48 and matrix["requiredWindowsPerPairing"] == 24
            and matrix["completedWindows"] == matrix["independentlyAcceptedWindows"] == matrix["speechViewerSamples"] == 0,
            "Native matrix totals disagree")
    require(index["expectedWindowsPerPairing"] == 24 and index["completedWindows"] == index["independentAcceptedWindows"] == 0,
            "Attempt index totals disagree")
    print(json.dumps({"manifestFilesVerified": len(paths), "retainedSourceHashesVerified": len(hashes),
        "retainedNativePacketHashesVerified": packet_files, "nativeFailedAttempts": 5,
        "browserInvocationPacketLinksVerified": 2, "nativeWindowCells": 48, "completedWindows": 0,
        "collectorReportsOriginalSettingsUnchanged": True, "failedSpeechAndTimelinesEmpty": True,
        "sourceAttributionLimit": "Recorded root/helper and FrontComposer fingerprints only; no contemporaneous full worktree fingerprints for every consumed dependency.",
        "cleanupLimit": "Collector cleanup path requests owned-process termination; no timestamped post-termination process observation is retained. No zero-process assertion.",
        "rawLogLimit": "Retained hashes/metadata cannot replay private raw logs or original NVDA settings; those claims are limited to collector reports."}, indent=2))


if __name__ == "__main__":
    main()
