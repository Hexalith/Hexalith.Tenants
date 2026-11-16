#!/usr/bin/env python3
"""Historical current-input qualification driver under run-with-temp.

Copies current sources, not the original historical snapshot. It intentionally
requires 5.1.0 still to be latest stable and refuses now 5.1.1 is public; retained
historical results are not replay results and remain unchanged.
"""

import datetime
import hashlib
import json
import os
from pathlib import Path
import shlex
import shutil
import subprocess
import sys
import tempfile
import traceback
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[4]
ORIGINAL_EVIDENCE = Path(__file__).resolve().parent
EVIDENCE = None
environment = {}
results = []
SCRATCH = Path(os.environ["TMPDIR"])
WORK = SCRATCH / "Hexalith.Tenants"
PUBLIC = "https://api.nuget.org/v3/index.json"
ARGS = ["-p:UseHexalithProjectReferences=false", "-p:UseNuGetDeps=true",
        "-p:HexalithEventStoreFromSource=false", "-p:HexalithFrontComposerFromSource=false",
        "-p:HexalithMemoriesFromSource=false"]


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def git(directory, *arguments):
    return subprocess.check_output(["git", "-C", str(directory), *arguments], text=True).strip()


def write(name, value):
    (EVIDENCE / name).write_text(json.dumps(value, indent=2) + "\n")


def inputs():
    modules = []
    for line in git(ROOT, "ls-files", "--stage").splitlines():
        metadata, name = line.split("\t", 1)
        if metadata.startswith("160000 "):
            directory = ROOT / name
            try:
                initialized = directory.is_dir() and Path(git(directory, "rev-parse", "--show-toplevel")).resolve() == directory.resolve()
            except subprocess.CalledProcessError:
                initialized = False
            modules.append({"path": name, "index": metadata.split()[1], "initialized": initialized,
                            "head": git(directory, "rev-parse", "HEAD") if initialized else None,
                            "status": git(directory, "status", "--porcelain=v1") if initialized else None})
    return {"head": git(ROOT, "rev-parse", "HEAD"),
            "branch": git(ROOT, "branch", "--show-current"),
            "status": git(ROOT, "status", "--porcelain=v1"), "submodules": modules}


def copy_tracked(source, target):
    target.mkdir(parents=True, exist_ok=True)
    tracked = subprocess.check_output(["git", "-C", str(source), "ls-files", "-z"])
    manifest = {}
    for raw in tracked.split(b"\0"):
        if not raw:
            continue
        name = os.fsdecode(raw)
        path = source / name
        if path.is_file():
            destination = target / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, destination)
            manifest[name] = sha(destination)
    return manifest


def run(name, arguments):
    print("START " + name + ": " + shlex.join(arguments), flush=True)
    record = {"name": name, "command": shlex.join(arguments), "cwd": str(WORK), "started": now()}
    with (EVIDENCE / (name + ".log")).open("w") as log:
        log.write("$ " + shlex.join(arguments) + "\n")
        log.flush()
        result = subprocess.run(arguments, cwd=WORK, env=environment, stdout=log, stderr=subprocess.STDOUT)
    record.update({"finished": now(), "exitCode": result.returncode, "log": name + ".log"})
    results.append(record)
    write("commands.json", results)
    print("END " + name + ": " + str(result.returncode), flush=True)
    return result.returncode == 0


def public_json(url, name):
    with urllib.request.urlopen(url, timeout=60) as response:
        payload = response.read()
    (EVIDENCE / name).write_bytes(payload)
    return json.loads(payload)


def capture_toolchain():
    """Measure this invocation's SDK selection in both project ancestry paths."""
    selected = {}
    for name, directory in (("repository", WORK), ("consumerParent", SCRATCH)):
        selected[name] = subprocess.check_output(
            ["dotnet", "--version"], cwd=directory, env=environment, text=True).strip()
    info = subprocess.check_output(["dotnet", "--info"], cwd=WORK, env=environment, text=True)
    (EVIDENCE / "dotnet-info.log").write_text(info)
    shutil.copy2(ROOT / "global.json", EVIDENCE / "global.json")
    write("toolchain.json", {"measured": now(), "selectedSdk": selected,
          "policySha256": sha(SCRATCH / "global.json"), "policy": json.loads((SCRATCH / "global.json").read_text()),
          "dotnetInfo": "dotnet-info.log", "pythonVersion": sys.version,
          "pythonExecutable": sys.executable})
    if selected["repository"] != selected["consumerParent"]:
        raise ValueError("Repository and package-consumer parent selected different SDKs.")


def qualify():
    global environment
    before = inputs()
    write("source-before.json", before)
    manifest = {"superproject": copy_tracked(ROOT, WORK)}
    for module in ("Hexalith.Builds", "Hexalith.Platform"):
        manifest[module] = copy_tracked(ROOT / "references" / module, WORK / "references" / module)
    write("source-input-sha256.json", {
        repository: {"trackedFileCount": len(files),
                     "manifestSha256": hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest(),
                     "buildInputSha256": {name: digest for name, digest in files.items()
                        if (repository == "superproject" and not name.startswith(("_bmad-output/", ".agents/", "_bmad/")))
                        or (repository == "Hexalith.Builds" and name.startswith(("Props/", "src/libraries/Hexalith.Builds.Tooling/")))
                        or (repository == "Hexalith.Platform" and (name.startswith("DaprSelfHosted") or name in ("Hexalith.Platform.slnx", "global.json")))}}
        for repository, files in manifest.items()})
    (WORK / "NuGet.Config").write_text(
        '<configuration><packageSources><clear/><add key="nuget.org" value="' + PUBLIC +
        '"/></packageSources></configuration>\n')
    environment = os.environ.copy()
    for key in ("CustomBeforeDirectoryBuildProps", "CustomAfterMicrosoftCommonProps",
                "CustomBeforeDirectoryBuildTargets", "CustomAfterDirectoryBuildTargets",
                "AfterMicrosoftNETSdkTargets", "HEXALITH_SOURCE_MAPPING_HASH"):
        environment.pop(key, None)
    environment.update({"NUGET_PACKAGES": str(SCRATCH / "packages"),
                        "NUGET_HTTP_CACHE_PATH": str(SCRATCH / "http-cache"),
                        "NUGET_PLUGINS_CACHE_PATH": str(SCRATCH / "plugin-cache"),
                        "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1",
                        "RepositoryCommit": before["head"], "PYTHONDONTWRITEBYTECODE": "1"})
    capture_toolchain()

    index_url = "https://api.nuget.org/v3-flatcontainer/hexalith.eventstore.client/index.json"
    index = public_json(index_url, "nuget-client-index.json")
    stable = [value for value in index["versions"] if "-" not in value]
    latest = max(stable, key=lambda value: tuple(map(int, value.split("."))))
    if latest != "5.1.0":
        raise ValueError("The spec's selected 5.1.0 is no longer the latest public stable SDK: " + latest)
    write("public-sdk-selection.json", {"checked": now(), "index": index_url, "latestStable": latest,
          "gallery": "https://www.nuget.org/packages/Hexalith.EventStore.Client",
          "packageSources": [PUBLIC], "sdkCandidateFeed": False,
          "environment": {key: environment[key] for key in ("RepositoryCommit", "NUGET_PACKAGES", "NUGET_HTTP_CACHE_PATH")}})

    for name in ("test_hosted_package_profile", "test_catalog_package_consumer", "test_catalog_artifact_guards"):
        run(name, ["python3", "tests/scripts/" + name + ".py"])

    built = {}
    for suite in ("IntegrationTests", "Contracts.Tests"):
        project = "tests/Hexalith.Tenants." + suite + "/Hexalith.Tenants." + suite + ".csproj"
        built[suite] = run("build-" + suite, ["dotnet", "build", project, "--configuration", "Release",
                                            "-warnaserror", "-m:1", "-nr:false", *ARGS, "--source", PUBLIC])

    if built["Contracts.Tests"]:
        for suite, minimum in (("PackageGovernanceTests", 1), ("PublishedTenantCatalogTests", 1), ("TenantCatalogTests", 2)):
            run(suite, ["dotnet", "test", "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj",
                        "--configuration", "Release", "--no-build", *ARGS,
                        "--filter-class", "Hexalith.Tenants.Contracts.Tests." + suite,
                        "--minimum-expected-tests", str(minimum), "--fail-skips", "on"])
    if built["IntegrationTests"]:
        for suite, minimum in (("TenantsApiGeneratedControllerTests", 66), ("TenantOwnerSourceGatewayTests", 25)):
            run(suite, ["dotnet", "test", "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj",
                        "--configuration", "Release", "--no-build", *ARGS,
                        "--filter-class", "Hexalith.Tenants.IntegrationTests." + suite,
                        "--minimum-expected-tests", str(minimum), "--fail-skips", "on"])
        for method, minimum in (("BoundBootstrap*", 13), ("AuthenticatedBootstrapCannotAssignDifferentTarget", 2),
                                ("Commands_endpoint_returns_problem_details_for_domain_rejection", 1)):
            run("bootstrap-" + method.replace("*", "all"),
                ["dotnet", "test", "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj",
                 "--configuration", "Release", "--no-build", *ARGS,
                 "--filter-method", "Hexalith.Tenants.IntegrationTests.CommandApiRuntimeIntegrationTests." + method,
                 "--minimum-expected-tests", str(minimum), "--fail-skips", "on"])

    # Bind each runtime graph's actual restored SDK archive to public NuGet bytes and producer metadata.
    graphs = {}
    cohort = {}
    for assets_path in sorted(WORK.glob("**/obj/project.assets.json")):
        assets = json.loads(assets_path.read_text())
        selected = []
        for identity, library in assets["libraries"].items():
            package_id, _, version = identity.partition("/")
            if not package_id.startswith("Hexalith.EventStore."):
                continue
            if library["type"] != "package":
                raise ValueError("Source SDK dependency in package qualification: " + identity)
            expected = "3.128.4" if package_id == "Hexalith.EventStore.Testing.Integration" else "5.1.0"
            if version != expected:
                raise ValueError("Mixed runtime SDK cohort: " + identity)
            selected.append(identity)
            cohort[identity] = (package_id, version)
        graphs[str(assets_path.relative_to(WORK))] = {"sdkPackages": selected,
            "packageSources": list(assets["project"]["restore"]["sources"])}
    write("restored-graphs.json", graphs)
    archives = []
    for identity, (package_id, version) in sorted(cohort.items()):
        path = Path(environment["NUGET_PACKAGES"]) / package_id.lower() / version / (package_id.lower() + "." + version + ".nupkg")
        url = "https://api.nuget.org/v3-flatcontainer/" + package_id.lower() + "/" + version + "/" + path.name
        with urllib.request.urlopen(url, timeout=60) as response:
            public_bytes = response.read()
        if hashlib.sha256(public_bytes).hexdigest() != sha(path):
            raise ValueError("Restored archive differs from public NuGet bytes: " + identity)
        with zipfile.ZipFile(path) as archive:
            nuspec = ET.fromstring(archive.read(next(name for name in archive.namelist() if name.endswith(".nuspec"))))
            metadata = nuspec.find("{*}metadata")
            repository = metadata.find("{*}repository")
            if metadata.findtext("{*}id") != package_id or metadata.findtext("{*}version") != version:
                raise ValueError("Archive identity mismatch: " + identity)
            archives.append({"id": package_id, "version": version, "sha256": sha(path), "publicArchive": url,
                             "publicBytesMatch": True, "producerRepository": dict(repository.attrib),
                             "legacyHarness": package_id.endswith(".Testing.Integration")})
    write("public-archive-provenance.json", {"checked": now(), "packages": archives})

    if all(built.values()):
        directory = SCRATCH / "catalog-packages"
        directory.mkdir()
        packed = []
        for package in ("Contracts", "Server"):
            packed.append(run("pack-" + package,
                ["dotnet", "pack", "src/Hexalith.Tenants." + package + "/Hexalith.Tenants." + package + ".csproj",
                 "--configuration", "Release", "--no-build", "--output", str(directory),
                 "-p:Version=0.0.0-catalog-qualification.20261115", *ARGS]))
        if all(packed):
            run("catalog-package-consumer", ["python3", "scripts/verify-catalog-package-consumer.py", str(directory),
                "--expected-source-sha", before["head"], "--output", str(EVIDENCE / "catalog-package-consumer.json")])

    after = inputs()
    write("source-after.json", after)
    if before["submodules"] != after["submodules"]:
        raise ValueError("Original submodule inputs changed during qualification.")
    return {"finished": now(), "sourceBase": before["head"],
          "workingTreeQualification": True, "sdk": latest, "failedCommands": [row["name"] for row in results if row["exitCode"]],
          "allCommandsPassed": all(row["exitCode"] == 0 for row in results), "submodulesUnchanged": True,
          "limits": ["Local qualification only; no exact-source remote CI, publication or live Production qualification.",
                     "The five pre-existing moved gitlinks are inputs and are excluded from implementation changes."]}


def main():
    global EVIDENCE, results
    replay_root = ORIGINAL_EVIDENCE / "replays"
    replay_root.mkdir(exist_ok=True)
    EVIDENCE = Path(tempfile.mkdtemp(prefix=datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%dT%H%M%SZ-"),
                                     dir=replay_root))
    results = []
    print("Replay evidence: " + str(EVIDENCE), flush=True)
    write("invocation.json", {"started": now(), "kind": "replay", "originalSnapshot": str(ORIGINAL_EVIDENCE),
          "result": "qualification-result.json"})
    write("qualification-result.json", {"status": "incomplete", "allCommandsPassed": False})
    try:
        # The separate package consumer is a sibling of WORK; inherit the same SDK/runner policy.
        shutil.copy2(ROOT / "global.json", SCRATCH / "global.json")
        result = qualify()
        result["status"] = "passed" if result["allCommandsPassed"] else "failed"
        write("qualification-result.json", result)
        return 0 if result["allCommandsPassed"] else 1
    except Exception as error:
        write("qualification-result.json", {"finished": now(), "status": "failed", "allCommandsPassed": False,
              "errorType": type(error).__name__, "error": str(error),
              "failedCommands": [row["name"] for row in results if row["exitCode"]]})
        traceback.print_exc()
        return 1


if __name__ == "__main__":
    sys.exit(main())
