"""Replay DW-655 in isolated fixtures without invoking real publication tools."""

import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

BASELINE = "c9feedb6656d70c7271e851c844ee29bdfb28c3e"
CLASS = "Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests"
MATCHING = "UnterminatedInventoryWithMatchingHashesCanPublish"
MISSING = "UnterminatedFinalPackageWithoutReviewedHashPreventsEveryPublication"
MISMATCH = "UnterminatedFinalPackageHashMismatchPreventsEveryPublication"
CANONICAL = "UnchangedSourceCanPublishMissingArtifacts"
EMPTY = "NoExistingPackagesCanPublishWithoutHashFile"
ADVANCED = "MainAdvancementDuringUnterminatedFinalHashRefusesPublication"


def require(condition, detail):
    if not condition:
        raise RuntimeError(str(detail))


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def case_key(test):
    name = test.get("name").replace("\\", "")
    inventory = re.search(r'inventory: "([^"]+)"', name)
    response = re.search(r'response: "([^"]+)"', name)
    return (test.get("method"), inventory[1] if inventory else None,
            response[1] if response else None)


def actual_calls(message):
    _, separator, actual = message.partition("\n    but was (case sensitive comparison)\n")
    require(separator, "Missing actual-call section in Shouldly failure")
    actual, separator, _ = actual.partition("\n    difference\n")
    require(separator, "Missing actual-call section boundary in Shouldly failure")
    calls = json.loads(actual.replace(r'\"', '"'))
    require(isinstance(calls, list) and all(isinstance(call, str) for call in calls), "Invalid actual calls")
    return calls


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-directory", type=Path, default=Path(tempfile.gettempdir()) / "dw-655-variants")
    args = parser.parse_args()
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=True)
    (output / "results.json").unlink(missing_ok=True)
    run_directory = Path(tempfile.mkdtemp(prefix="run-", dir=output))
    repo = Path(__file__).resolve().parents[2]
    test_source = repo / "tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs"
    test_source_hash = sha256(test_source)
    build = ["dotnet", "build", "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj",
             "--configuration", "Debug", "-m:1", "-nr:false", "-p:UseHexalithProjectReferences=true"]
    with (run_directory / "build.log").open("w") as log:
        subprocess.run(build, cwd=repo, stdout=log, stderr=subprocess.STDOUT, timeout=180, check=True)
    require(sha256(test_source) == test_source_hash, "Test source changed during build")
    publisher = (repo / "scripts/publish-partial-release.sh").read_text()
    baseline = subprocess.check_output(["git", "show", f"{BASELINE}:scripts/publish-partial-release.sh"], cwd=repo).decode()
    anchor = 'while IFS= read -r package_id || [ -n "$package_id" ]; do\n'
    require(publisher.count(anchor) == 1, "Expected exactly the existing-inventory EOF fix")
    refusal = ('[ ! -s "$state_directory/existing-packages" ] || '
               '[ "$(tail -c 1 "$state_directory/existing-packages")" = "" ] || '
               '{ echo "Unterminated inventory refused by mutation." >&2; exit 1; }\n')
    reject_unterminated = publisher.replace(anchor, refusal + anchor, 1)

    inventories = ("package", "mixed", "package-existing", "container", "release", "mixed-existing")
    missing = {(MISSING, inventory, "hash-record-absent") for inventory in inventories}
    missing |= {(MISSING, inventory, "hash-file-absent") for inventory in ("package", "mixed")}
    mismatch = {(MISMATCH, inventory, None) for inventory in inventories}
    matching = {(MATCHING, inventory, None) for inventory in inventories}
    advanced = {(ADVANCED, inventory, None) for inventory in inventories}
    canonical = {(CANONICAL, inventory, None) for inventory in (*inventories, "all-missing")}
    empty = {(EMPTY, None, None)}
    all_cases = missing | mismatch | matching | advanced | canonical | empty
    controls = matching | canonical | empty
    variants = [("production", publisher, all_cases, set()),
                ("entry-baseline", baseline, all_cases, missing | mismatch | matching | advanced),
                ("reject-unterminated", reject_unterminated, controls, matching)]
    results = []
    with tempfile.TemporaryDirectory(prefix="dw-655-fixture-") as temporary:
        fixture = Path(temporary)
        (fixture / "Hexalith.Tenants.slnx").touch()
        (fixture / "scripts").mkdir()
        (fixture / "tools").mkdir()
        shutil.copy2(repo / "tools/release-packages.json", fixture / "tools")
        manifest_hash = sha256(fixture / "tools/release-packages.json")
        shutil.copytree(repo / "tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0", fixture / "bin")
        assembly_path = fixture / "bin/Hexalith.Tenants.Contracts.Tests.dll"
        assembly_hash = sha256(assembly_path)
        for name, source, expected, failures in variants:
            script = fixture / "scripts/publish-partial-release.sh"
            script.write_bytes(source.encode())
            publisher_hash = sha256(script)
            report = run_directory / f"{name}.xml"
            command = ["dotnet", str(assembly_path)]
            for method in sorted({case[0] for case in expected}):
                command.extend(["-method", f"{CLASS}.{method}"])
            command.extend(["-noColor", "-result-xml", str(report)])
            with (run_directory / f"{name}.log").open("w") as log:
                run = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, timeout=60, check=False)
            assembly = ET.parse(report).getroot().find("assembly")
            totals = {key: int(assembly.get(key)) for key in ("total", "passed", "failed", "skipped", "errors")}
            require(totals == {"total": len(expected), "passed": len(expected) - len(failures),
                              "failed": len(failures), "skipped": 0, "errors": 0}, (name, totals))
            require(run.returncode == (1 if failures else 0), (name, run.returncode))
            tests = assembly.findall(".//test")
            cases = {case_key(test): test for test in tests}
            require(len(tests) == len(cases) == len(expected) and set(cases) == expected, name)
            require({key for key, test in cases.items() if test.get("result") == "Fail"} == failures, name)
            for key in failures:
                failure = cases[key].find("failure")
                message = failure.findtext("message", "")
                require(failure.get("exception-type") == "Shouldly.ShouldAssertException", (name, key))
                require(message.startswith("Shouldly.ShouldAssertException : calls\n"), (name, key, message))
                calls = actual_calls(message)
                if name == "entry-baseline":
                    publication = "container-publish" if key[1] == "container" else "release-publish" if key[1] == "release" else "package-publish"
                    require(any(call == publication or call.startswith(publication + " ") for call in calls), (name, key, calls))
                    require("unexpected" not in message.lower(), (name, key, message))
                else:
                    require("Unterminated inventory refused by mutation." in message, (name, key))
                    require(not any("publish" in call or call == "container-login" for call in calls), (name, key, calls))
            require(sha256(script) == publisher_hash and sha256(assembly_path) == assembly_hash, "Executed fixture changed")
            require(sha256(fixture / "tools/release-packages.json") == manifest_hash, "Fixture manifest changed")
            result = {"variant": name, "exit": run.returncode, "publisher_sha256": publisher_hash, **totals}
            results.append(result)
            print(json.dumps(result), flush=True)

    require(sha256(test_source) == test_source_hash, "Test source changed during replay")
    provenance = {"baseline_commit": BASELINE, "build_command": build,
                  "assembly_sha256": assembly_hash, "test_source_sha256": test_source_hash,
                  "manifest_sha256": manifest_hash}
    (output / "results.json").write_text(json.dumps({"run_directory": str(run_directory), "provenance": provenance,
                                                   "variants": results}, indent=2) + "\n")


if __name__ == "__main__":
    main()
