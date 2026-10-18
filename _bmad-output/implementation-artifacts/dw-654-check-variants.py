"""Replay DW-654 against isolated current, baseline and harmful publisher copies."""

import argparse
import hashlib
import json
import re
import shlex
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

BASELINE = "ff7014107a8fe0c53315e43d8ccd6838f8a3d010"
CLASS = "Hexalith.Tenants.Contracts.Tests.PartialReleasePublicationTests"


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def replace_once(source, before, after):
    if source.count(before) != 1:
        raise ValueError(f"Expected one mutation anchor: {before}")
    return source.replace(before, after, 1)


def case_key(test):
    name = test.get("name").replace("\\", "")
    inventory = re.search(r'inventory: "([^"]+)"', name)
    index = re.search(r'(?:mismatchIndex|missingHashIndex): (\d+)', name)
    response = re.search(r'response: "([^"]+)"', name)
    return (test.get("method"), inventory[1] if inventory else None,
            int(index[1]) if index else None, response[1] if response else None)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output-directory", type=Path, default=Path(tempfile.gettempdir()) / "dw-654-variants")
    parser.add_argument("--probe-truncated-inventory", action="store_true")
    args = parser.parse_args()
    output = args.output_directory.resolve()
    output.mkdir(parents=True, exist_ok=True)
    (output / "results.json").unlink(missing_ok=True)
    run_directory = Path(tempfile.mkdtemp(prefix="run-", dir=output))
    repo = Path(__file__).resolve().parents[2]
    project = "tests/Hexalith.Tenants.Contracts.Tests/Hexalith.Tenants.Contracts.Tests.csproj"
    build_command = ["dotnet", "build", project, "--configuration", "Debug", "-m:1", "-nr:false",
                     "-p:UseHexalithProjectReferences=true"]
    with (run_directory / "build.log").open("w") as log:
        subprocess.run(build_command, cwd=repo, stdout=log, stderr=subprocess.STDOUT, timeout=180, check=True)
    publisher = (repo / "scripts/publish-partial-release.sh").read_text()
    baseline = subprocess.check_output(["git", "show", f"{BASELINE}:scripts/publish-partial-release.sh"], cwd=repo).decode()
    loop_start = "while IFS= read -r package_id; do\n"
    # Select only the first loop; both the existing and missing inventories use this form.
    unconditional = publisher[:publisher.index(loop_start)] + (
        '[ -f "$state_directory/existing-package-sha256" ] || '
        '{ echo "existing-package-sha256 is absent" >&2; exit 1; }\n'
    ) + publisher[publisher.index(loop_start):]

    counts = {"package": 1, "package-existing": 4, "container": 5, "release": 5, "mixed-existing": 4}
    omitted = {("MissingExpectedHashPreventsEveryPublication", inventory, index, None)
               for inventory, count in counts.items() for index in range(count)}
    mismatch = {("ExistingPackageHashMismatchPreventsEveryPublication", inventory, index, None)
                for inventory, count in counts.items() for index in range(count)}
    matching = {("UnchangedSourceCanPublishMissingArtifacts", inventory, None, None)
                for inventory in (*counts, "mixed", "all-missing")}
    unusable = {("UnusableExpectedHashesPreventEveryPublication", inventory, None, response)
                for inventory in ("package-existing", "container", "release", "mixed-existing")
                for response in ("hash-file-empty", "hash-file-whitespace", "hash-wrong-id", "hash-record-blank", "hash-file-absent")}
    no_file = {("NoExistingPackagesCanPublishWithoutHashFile", None, None, None)}
    expected = omitted | mismatch | matching | unusable | no_file
    assert len(expected) == 66
    baseline_failures = omitted | {case for case in unusable if case[3] != "hash-file-absent"}
    variants = [("production-control", publisher, set()),
                ("entry-baseline", baseline, baseline_failures),
                ("unconditional-hash-file", unconditional, no_file)]
    results = []
    with tempfile.TemporaryDirectory(prefix="dw-654-fixture-") as temporary:
        fixture = Path(temporary)
        (fixture / "Hexalith.Tenants.slnx").touch()
        (fixture / "scripts").mkdir()
        (fixture / "tools").mkdir()
        shutil.copy2(repo / "tools/release-packages.json", fixture / "tools")
        shutil.copytree(repo / "tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0", fixture / "bin")
        for name, source, failed_cases in variants:
            script = fixture / "scripts/publish-partial-release.sh"
            script.write_bytes(source.encode())
            report = run_directory / f"{name}.xml"
            command = ["dotnet", str(fixture / "bin/Hexalith.Tenants.Contracts.Tests.dll")]
            for method in sorted({case[0] for case in expected}):
                command.extend(["-method", f"{CLASS}.{method}"])
            command.extend(["-noColor", "-result-xml", str(report)])
            with (run_directory / f"{name}.log").open("w") as log:
                run = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, timeout=60, check=False)
            assembly = ET.parse(report).getroot().find("assembly")
            totals = {key: int(assembly.get(key)) for key in ("total", "passed", "failed", "skipped", "errors")}
            assert totals == {"total": 66, "passed": 66 - len(failed_cases), "failed": len(failed_cases), "skipped": 0, "errors": 0}, (name, totals)
            assert run.returncode == (1 if failed_cases else 0), name
            cases = {case_key(test): test for test in assembly.findall(".//test")}
            assert set(cases) == expected, name
            assert {key for key, test in cases.items() if test.get("result") == "Fail"} == failed_cases, name
            for key in failed_cases:
                failure = cases[key].find("failure")
                message = failure.findtext("message", "")
                assert failure.get("exception-type") == "Shouldly.ShouldAssertException", (name, key)
                assert message.startswith("Shouldly.ShouldAssertException : calls\n"), (name, key, message)
                if name == "entry-baseline":
                    publication = "container-publish" if key[1] == "container" else "release-publish" if key[1] == "release" else "package-publish"
                    assert publication in message and "unexpected" not in message.lower(), key
            result = {"variant": name, "exit": run.returncode, "publisher_sha256": sha256(script), **totals}
            results.append(result)
            print(json.dumps(result), flush=True)

    probe_result = None
    if args.probe_truncated_inventory:
        historical_probe = (repo / "_bmad-output/implementation-artifacts/dw-608-probe-missing-hash.sh").read_text()
        root_assignment = next(line for line in historical_probe.splitlines() if line.startswith("project_root="))
        probe = replace_once(historical_probe, root_assignment, "project_root=" + shlex.quote(str(repo)))
        probe = replace_once(probe, "printf 'Hexalith.Tenants.Contracts\\n' > state/existing-packages",
                             "printf 'Hexalith.Tenants.Contracts' > state/existing-packages")
        probe_file = run_directory / "truncated-inventory.sh"
        probe_file.write_text(probe)
        run = subprocess.run(["bash", str(probe_file), "missing"], cwd=repo, capture_output=True, text=True, timeout=30, check=False)
        (run_directory / "truncated-inventory.log").write_text(run.stdout + run.stderr)
        assert run.returncode == 0, run.stderr
        assert run.stdout.count("dotnet nuget push ") == 4
        assert all(call in run.stdout for call in ("container-login", "dotnet publish ", "release-publish"))
        probe_result = {"exit": run.returncode, "probe_sha256": sha256(probe_file), "publisher_sha256": sha256(repo / "scripts/publish-partial-release.sh"), "intercepted_publication_reached": True}

    provenance = {
        "baseline_commit": BASELINE, "build_command": build_command,
        "assembly_sha256": sha256(repo / "tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0/Hexalith.Tenants.Contracts.Tests.dll"),
        "test_source_sha256": sha256(repo / "tests/Hexalith.Tenants.Contracts.Tests/PartialReleasePublicationTests.cs"),
    }
    (output / "results.json").write_text(json.dumps({"run_directory": str(run_directory), "provenance": provenance,
                                                   "variants": results, "truncated_inventory": probe_result}, indent=2) + "\n")


if __name__ == "__main__":
    main()
