"""Replay retained bytes, source bindings, runtime observations and matrix shape.

This checks evidence integrity, never independent speech acceptance or audio.
"""
import hashlib
import json
from pathlib import Path
import zipfile


HERE = Path(__file__).resolve().parent
EXPECTED_REPOSITORIES = {"."} | {"references/Hexalith." + name for name in
    ("EventStore", "Commons", "AI.Tools", "FrontComposer", "Builds", "PolymorphicSerializations", "Memories", "McpCli", "Platform")}


def repository_set(rows, stage):
    paths = [row["path"] for row in rows]
    require(len(paths) == len(set(paths)) and set(paths) == EXPECTED_REPOSITORIES,
            stage + " must contain the exact unique root-plus-nine declared repositories")


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
    versions = {}
    for item in source["source"]:
        path = (HERE / item["immutableSnapshot"]).resolve()
        require(path.is_relative_to(HERE.parent.resolve()), "Snapshot escapes evidence")
        require(digest(path.read_bytes()) == item["sha256"], "Executed source snapshot differs")
        name = Path(item["path"]).name
        hashes[name] = item["sha256"]
        versions[name] = {item["sha256"]}
        for prior in item.get("previousVersions", []):
            previous_path = (HERE / prior["immutableSnapshot"]).resolve()
            require(previous_path.is_relative_to(HERE.parent.resolve()), "Prior snapshot escapes evidence")
            require(digest(previous_path.read_bytes()) == prior["sha256"], "Prior snapshot differs")
            versions[name].add(prior["sha256"])
    preparation = read("native-preparation.json")
    repository_set(preparation["repositories"], "Preparation")
    with zipfile.ZipFile(HERE / "native-path-inventories.zip") as archive:
        for repository in preparation["repositories"]:
            inventory = repository["inventory"]
            raw = archive.read(inventory["member"])
            rows = json.loads(raw)
            require(digest(raw) == inventory["sha256"] and len(rows) == inventory["fileCount"], "Initial inventory binding differs")
            canonical = digest(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode())
            require(all(repository[key]["manifestSha256"] == canonical for key in ("sourceBefore", "native", "sourceAfter")),
                    "Initial exact-byte equality differs")
    refreshed = read("native-refresh.json")
    repository_set(refreshed["repositories"], "Refresh")
    require(refreshed["completed"] and len(refreshed["repositories"]) == 10, "Refresh is incomplete")
    with zipfile.ZipFile(HERE / "native-refresh-inventories.zip") as archive:
        for repository in refreshed["repositories"]:
            for key in ("before", "after"):
                inventory = repository[key]["inventory"]
                raw = archive.read(inventory["member"])
                rows = json.loads(raw)
                canonical = digest(json.dumps(rows, sort_keys=True, separators=(",", ":")).encode())
                require(digest(raw) == inventory["sha256"] and len(rows) == inventory["fileCount"], "Refreshed inventory binding differs")
                require(repository[key]["manifestSha256"] == canonical, "Refreshed inventory aggregate differs")
                if key == "after":
                    require(all(repository[side]["manifestSha256"] == canonical
                                and repository[side]["revision"] == repository["after"]["revision"]
                                for side in ("sourceBefore", "sourceAfter")), "Refreshed exact-byte equality differs")
    final_inputs = read("native-final-input-verification.json")
    repository_set(final_inputs["repositories"], "Final verification")
    expected_inputs = {row["path"]: row["after"] for row in refreshed["repositories"]}
    actual_inputs = {row["path"]: row for row in final_inputs["repositories"]}
    require(actual_inputs.keys() == expected_inputs.keys() and final_inputs["allFrozenInputsUnchanged"], "Final input cover differs")
    for path, expected_input in expected_inputs.items():
        actual_input = actual_inputs[path]
        require(actual_input["revision"] == expected_input["revision"]
                and actual_input["manifestSha256"] == expected_input["manifestSha256"]
                and actual_input["exitCode"] == 0 and actual_input["matchesRecordedFrozenInputs"], "Frozen input changed: " + path)
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
    attempts = read("native-attempt-index.json")["attempts"]
    require(len({row["label"] for row in attempts}) == len(attempts)
            and len({row["packet"] for row in attempts}) == len(attempts), "Duplicate native attempts")
    for attempt in attempts:
        require(digest((HERE / attempt["packet"]).read_bytes()) == attempt["packetSha256"], "Native archive hash differs")
        with zipfile.ZipFile(HERE / attempt["packet"]) as archive:
            names = archive.namelist()
            declared = [item["path"] for item in attempt["retainedMembers"]]
            require(len(names) == len(set(names)) and len(declared) == len(set(declared))
                    and set(names) == set(declared), "Native ZIP needs complete unique member coverage")
            for item in attempt["retainedMembers"]:
                data = archive.read(item["path"])
                require(digest(data) == item["sha256"] and len(data) == item["bytes"], "Native member binding differs")
            capture = json.loads(archive.read("capture.json"))
            require(capture["collectorSha256"] == hashes["capture-deferred-nvda-speech.ps1"], "Collector binding differs")
            require(capture["observerSha256"] in versions["deferred-nvda-observer.py"]
                    and capture["observerSha256"] == attempt["observerSha256"], "Observer binding differs")
            if "fixture.json" in archive.namelist():
                fixture = json.loads(archive.read("fixture.json"))
                for field, name in fields.items():
                    require(fixture["sourceBeforeBuild"][field] == hashes[name], "Native fixture source binding differs")
                require(fixture["sourceBeforeBuild"]["sourceBaseRevision"] == attempt["sourceRootRevision"], "Native root differs")
    builder_path = HERE / "build-native-matrix.py"
    namespace = {"__name__": "artifact_replay_builder", "__file__": str(builder_path)}
    exec(compile(builder_path.read_bytes(), str(builder_path), "exec"), namespace)
    expected_matrix, expected_inspection = namespace["build"]()
    require(matrix == expected_matrix, "Matrix does not replay from packet windows/controls")
    require(read("speech-inspection.json") == expected_inspection, "Speech timing/occurrence inventory does not replay")
    require(read("native-runtime-summary.json") == namespace["runtime_summary"](), "Native runtime/count/cleanup summary differs")
    print(json.dumps({"artifactFiles": len(paths), "executedHelperBindings": len(hashes),
                      "grantCurrentControls": True, "gridExternalFailurePreserved": True,
                      "nativeMatrixWindows": 48, "completedCurrentProfileWindows": matrix["completedWindowControls"],
                      "unchangedFrozenRepositories": len(actual_inputs),
                      "independentAcceptanceClaimed": False}, indent=2))


if __name__ == "__main__":
    main()
