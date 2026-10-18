"""Replay DW-608 acceptance against isolated production and harmful script copies."""

import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path


def replace_once(source, before, after):
    """Refuse stale mutation anchors instead of silently testing unchanged code."""
    if source.count(before) != 1:
        raise ValueError(f"Expected one mutation anchor: {before}")
    return source.replace(before, after, 1)


def main():
    """Run actual compiled tests with copied scripts and retain their results."""
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-directory", type=Path, default=Path(tempfile.gettempdir()) / "dw-608-variants")
    args = parser.parse_args()
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=True)
    (output / "results.json").unlink(missing_ok=True)
    run_directory = Path(tempfile.mkdtemp(prefix="run-", dir=output))
    print(json.dumps({"run_directory": str(run_directory)}), flush=True)
    repo = Path(__file__).resolve().parents[2]
    build_command = ["dotnet", "build", "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj",
                     "--configuration", "Debug", "-m:1", "-nr:false", "-p:UseHexalithProjectReferences=true"]
    with (run_directory / "build.log").open("w") as log:
        subprocess.run(build_command, cwd=repo, stdout=log, stderr=subprocess.STDOUT, timeout=180, check=True)
    publisher = (repo / "scripts/publish-partial-release.sh").read_text()
    comparison = '  [ "$actual_hash" = "$expected_hash" ] || { echo "[partial-release-recovery] Existing package $package_id does not match the reviewed source." >&2; exit 1; }\n'
    loop_start = publisher.index("while IFS= read -r package_id; do")
    loop_end = publisher.index('done < "$state_directory/existing-packages"\n', loop_start) + len('done < "$state_directory/existing-packages"\n')
    hash_loop = publisher[loop_start:loop_end]
    late = replace_once(publisher, hash_loop, "")
    late = replace_once(late, 'done < "$state_directory/missing-packages"\n', 'done < "$state_directory/missing-packages"\n\n' + hash_loop)
    ids = [package["id"] for package in json.loads((repo / "tools/release-packages.json").read_text())["packages"]]
    variants = [
        ("production-control", publisher, 0),
        ("bypass-comparison", replace_once(publisher, comparison, ""), 19),
        ("first-package-only", replace_once(publisher, comparison, comparison + "  break\n"), 18),
        ("wrong-package-hash", replace_once(publisher, 'awk -v id="$package_id"', f'awk -v id="{ids[0]}"'), 14),
        ("hashes-after-push", late, 25),
    ]
    existing_counts = {"package": 1, "package-existing": 4, "container": 5, "release": 5, "mixed-existing": 4}
    mismatch_cases = {(inventory, index) for inventory, count in existing_counts.items() for index in range(count)}
    matching_cases = {(inventory, None) for inventory in (*existing_counts, "mixed", "all-missing")}
    expected_cases = mismatch_cases | matching_cases
    multiple_existing = {case for case in matching_cases if existing_counts.get(case[0], 1) > 1}
    expected_failed_cases = {
        "production-control": set(),
        "bypass-comparison": mismatch_cases,
        "first-package-only": multiple_existing | {case for case in mismatch_cases if case[1] > 0},
        "wrong-package-hash": multiple_existing | {case for case in mismatch_cases if case[1] > 1},
        "hashes-after-push": expected_cases - {("all-missing", None)},
    }
    results = []
    with tempfile.TemporaryDirectory(prefix="dw-608-variants-") as temporary:
        fixture = Path(temporary)
        (fixture / "Hexalith.Tenants.slnx").touch()
        (fixture / "scripts").mkdir()
        (fixture / "tools").mkdir()
        shutil.copy2(repo / "tools/release-packages.json", fixture / "tools")
        shutil.copytree(repo / "tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0", fixture / "bin")
        for name, selected, failures in variants:
            (fixture / "scripts/publish-partial-release.sh").write_bytes(selected.encode())
            report = run_directory / f"{name}.xml"
            command = [
                "dotnet", str(fixture / "bin/Hexalith.Tenants.Contracts.Tests.dll"),
                "-method", "Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests.ExistingPackageHashMismatchPreventsEveryPublication",
                "-method", "Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests.UnchangedSourceCanPublishMissingArtifacts",
                "-noColor", "-result-xml", str(report),
            ]
            with (run_directory / f"{name}.log").open("w") as log:
                run = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, timeout=60, check=False)
            assembly = ET.parse(report).getroot().find("assembly")
            result = {"variant": name, "exit": run.returncode,
                      **{key: int(assembly.get(key)) for key in ("total", "passed", "failed", "skipped", "errors")}}
            results.append(result)
            print(json.dumps(result), flush=True)
            if result != {"variant": name, "exit": 0 if failures == 0 else 1,
                          "total": 26, "passed": 26 - failures, "failed": failures, "skipped": 0, "errors": 0}:
                raise RuntimeError(f"Unexpected variant outcome: {result}")
            cases = {}
            for test in assembly.findall(".//test"):
                match = re.search(r'\(inventory: "([^"]+)"(?:, mismatchIndex: (\d+))?\)', test.get("name").replace("\\", ""))
                if match is None:
                    raise RuntimeError(f"Unrecognized test identity: {test.get('name')}")
                inventory, index = match.groups()
                key = (inventory, int(index) if index is not None else None)
                if key in cases:
                    raise RuntimeError(f"Duplicate test identity: {key}")
                cases[key] = test
            if set(cases) != expected_cases or {key for key, test in cases.items() if test.get("result") == "Fail"} != expected_failed_cases[name]:
                raise RuntimeError(f"Unexpected case identities or failures: {name}")
            for key in expected_failed_cases[name]:
                failure = cases[key].find("failure")
                message = failure.findtext("message", "")
                if failure.get("exception-type") != "Shouldly.ShouldAssertException" or not message.startswith("Shouldly.ShouldAssertException : calls\n"):
                    raise RuntimeError(f"Unrelated fixture failure: {name} {key}: {message}")
                if name == "bypass-comparison":
                    publication = "container-publish" if key[0] == "container" else "release-publish" if key[0] == "release" else "package-publish"
                    if publication not in message or "unexpected" in message.lower():
                        raise RuntimeError(f"Bypass did not reach intercepted publication: {key}")
    provenance = {
        "build_command": build_command,
        "assembly_sha256": hashlib.sha256((repo / "tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll").read_bytes()).hexdigest(),
        "test_source_sha256": hashlib.sha256((repo / "tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs").read_bytes()).hexdigest(),
        "publisher_sha256": hashlib.sha256(publisher.encode()).hexdigest(),
    }
    (output / "results.json").write_text(json.dumps({"run_directory": str(run_directory), "provenance": provenance, "variants": results}, indent=2) + "\n")


if __name__ == "__main__":
    main()
