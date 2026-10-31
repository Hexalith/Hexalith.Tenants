#!/usr/bin/env python3
"""Exercise the real recovery pipeline with isolated, fail-closed destinations."""

from __future__ import annotations

import copy
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest

from recovery_execution_fixture import create as create_execution_fixture


ROOT = Path(__file__).resolve().parents[2]
SOURCE_SHA = "a" * 40

# Installed only in each temporary test directory. No live destination is reachable.
CLIENT = r'''
import base64, json, os, pathlib, sys
kind = pathlib.Path(sys.argv[0]).name
args = sys.argv[1:]
root = pathlib.Path(os.environ["RECOVERY_FIXTURE"])
state_path = root / "destinations.json"
state = json.loads(state_path.read_text())
manifest = json.loads(pathlib.Path(os.environ["HEXALITH_RELEASE_PACKAGE_MANIFEST"]).read_text())
with (root / "calls.jsonl").open("a") as log:
    log.write(json.dumps([kind, *args]) + "\n")
def save(): state_path.write_text(json.dumps(state))
def content(package_id): return "reviewed fixture package " + package_id + "\n"
def binary_content(package_id):
    if package_id in state.get("nuget_content", {}): return state["nuget_content"][package_id].encode()
    encoded = state.get("package_content_base64", {}).get(package_id)
    return base64.b64decode(encoded) if encoded else content(package_id).encode()
def fail(): sys.exit("Unexpected fixture request: " + kind + " " + repr(args))
if kind == "curl":
    url = next((a for a in args if a.startswith("https://")), "")
    if "v3-flatcontainer/" in url:
        package_id = next((p["id"] for p in manifest["packages"] if "/" + p["id"].lower() + "/" in url), None)
        if package_id is None: fail()
        statuses = state.get("nuget_statuses", {}).get(package_id, [])
        status = statuses.pop(0) if statuses else (200 if package_id in state["packages"] else 404)
        if statuses or package_id in state.get("nuget_statuses", {}): save()
        if "--output" in args and args[args.index("--output") + 1] != "/dev/null":
            output = pathlib.Path(args[args.index("--output") + 1])
            output.write_bytes(binary_content(package_id) if status == 200 else b"not ready")
            if state.get("mutate_package_on_nuget"):
                pathlib.Path("nupkgs/" + package_id + ".1.2.3.nupkg").write_bytes(binary_content(package_id))
            if state.get("mutate_manifest_on_nuget"):
                path = pathlib.Path(os.environ["HEXALITH_RELEASE_PACKAGE_MANIFEST"])
                path.write_text(path.read_text() + "\n")
            if "--write-out" in args: print(status)
            elif status != 200: sys.exit(22)
        else: print(status)
    elif "/v2/tenants/manifests/" in url: print(200 if state["container"] else 404)
    else: fail()
elif kind == "gh":
    if args[0] == "api":
        endpoint = next((argument for argument in args if argument.startswith("repos/")), "")
        if endpoint.endswith("git/ref/heads/main"): print(state.get("live_main_sha", os.environ["GITHUB_SHA"]))
        elif "/actions/workflows/" in endpoint:
            workflow = endpoint.split("/actions/workflows/", 1)[1].split("/", 1)[0]
            default = {"id": {"ci.yml": 1, "story-guards.yml": 2, "source-reference.yml": 3}[workflow],
                       "run_number": 1, "run_attempt": 1, "head_sha": os.environ["GITHUB_SHA"], "head_branch": "main",
                       "event": "push", "path": ".github/workflows/" + workflow, "status": "completed", "conclusion": "success"}
            print(json.dumps([{"workflow_runs": state.get("gate_runs", {}).get(workflow, [default])}]))
        elif "/actions/runs/" in endpoint:
            run_id = int(endpoint.rsplit("/", 1)[1])
            workflow = {1: "ci.yml", 2: "story-guards.yml", 3: "source-reference.yml"}[run_id]
            default = {"id": run_id, "run_number": 1, "run_attempt": 1, "head_sha": os.environ["GITHUB_SHA"], "head_branch": "main",
                       "event": "push", "path": ".github/workflows/" + workflow, "status": "completed", "conclusion": "success"}
            print(json.dumps(state.get("gate_detail", {}).get(workflow, default)))
        elif "/git/ref/tags/" in endpoint:
            if "--include" in args: print("HTTP/2.0 200 OK" if state["tag"] else "HTTP/2.0 404 Not Found")
            elif "--jq" in args: print("commit" if args[-1] == ".object.type" else state.get("tag_sha", os.environ["GITHUB_SHA"]))
            else: print(json.dumps({"object": {"type": "commit", "sha": state.get("tag_sha", os.environ["GITHUB_SHA"])}}))
        elif "/releases/tags/" in endpoint:
            if "--include" in args: print("HTTP/2.0 200 OK" if state["release"] else "HTTP/2.0 404 Not Found")
            else:
                if not state["release"]: fail()
                print(json.dumps(state["release"]))
        elif "/releases/assets/" in endpoint:
            if args[2:] != ["-H", "Accept: application/octet-stream"]: fail()
            asset = next((a for a in state["release"]["assets"] if str(a["id"]) == endpoint.rsplit("/", 1)[1]), None)
            if asset is None or asset.get("unavailable"): sys.exit(22)
            if state.get("mutate_manifest_on_download"):
                path = pathlib.Path(os.environ["HEXALITH_RELEASE_PACKAGE_MANIFEST"])
                path.write_text(path.read_text() + "\n")
            if state.get("mutate_package_on_download"):
                pathlib.Path("nupkgs/" + manifest["packages"][-1]["id"] + ".1.2.3.nupkg").write_text("changed after first proof")
            if state.get("mutate_state_on_download"):
                (root / "state/container-state").write_text("present\n")
            if "base64" in asset: sys.stdout.buffer.write(base64.b64decode(asset["base64"]))
            else: sys.stdout.write(asset["content"])
        else: fail()
    elif args[:2] in (["release", "upload"], ["release", "create"]):
        if "--clobber" in args: fail()
        if args[1] == "create":
            state["tag"] = True
            state["release"] = {"tag_name": args[2], "draft": False, "prerelease": "--prerelease" in args, "assets": []}
        for file in args[3:]:
            if not file.endswith(".nupkg"): continue
            path = pathlib.Path(file)
            if any(a["name"] == path.name for a in state["release"]["assets"]): fail()
            state["release"]["assets"].append({"id": len(state["release"]["assets"]) + 1, "name": path.name, "content": path.read_text()})
        save()
    else: fail()
elif kind == "dotnet":
    if state.get("failure_stage") == args[0]: sys.exit(42)
    if args[0] == "build":
        if os.environ.get("NUGET_API_KEY"): fail()
        if state.get("mutate_missing_on_build"):
            (root / "state/missing-packages").write_text("")
    elif args[0] == "pack":
        if os.environ.get("NUGET_API_KEY"): fail()
        project = args[1]
        package = next((p for p in manifest["packages"] if p["project"] == project), None)
        if package is None: fail()
        version = next(a.split("=", 1)[1] for a in args if a.startswith("-p:Version="))
        directory = pathlib.Path(args[args.index("--output") + 1])
        encoded = state.get("prepared_package_base64", {}).get(package["id"])
        raw = base64.b64decode(encoded) if encoded else content(package["id"]).encode()
        (directory / (package["id"] + "." + version + ".nupkg")).write_bytes(raw)
    elif args[:3] == ["nuget", "verify", "--all"]:
        if len(args) != 4 or not pathlib.Path(args[3]).is_file(): fail()
        print("native signature verifier fixture")
        sys.exit(state.get("signature_verifier_exit", 0))
    elif args[:2] == ["nuget", "push"]:
        if args[-1] != "fixture-key": fail()
        state["packages"].append(pathlib.Path(args[2]).name.removesuffix(".1.2.3.nupkg")); save()
    elif args[0] == "publish":
        for prefix in ("-p:ContainerProvenanceSourceSha=", "-p:ContainerProvenanceReleaseVersion=", "-p:ContainerProvenanceCreated="):
            if not any(a.startswith(prefix) and a != prefix for a in args): fail()
        if state.get("mutate_helper_on_publish"):
            helper = pathlib.Path(".hexalith/builds-execution/Github/publish-containers/oci_registry_validator.py")
            helper.write_text(helper.read_text() + "\n# publisher mutation\n")
        if state.get("hold_publish"):
            (root / "publisher-held").touch()
            import time
            while not (root / "publisher-resume").exists(): time.sleep(0.02)
        state["created"] = next(a.split("=", 1)[1] for a in args if a.startswith("-p:ContainerProvenanceCreated="))
        state["container"] = True; save()
    else: fail()
elif kind == "docker":
    if args != ["login", "fixture.invalid", "--username", "fixture-user", "--password-stdin"]: fail()
    sys.stdin.read()
else: fail()
'''


class PartialReleaseRecoveryTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix="tenants-recovery-")
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / "bin").mkdir()
        (self.root / "tools").mkdir()
        (self.root / "tools/release-packages.json").write_text((ROOT / "tools/release-packages.json").read_text())
        (self.root / "scripts").symlink_to(ROOT / "scripts", target_is_directory=True)
        (self.root / "references").mkdir()
        (self.root / "references/Hexalith.Builds").symlink_to(ROOT / "references/Hexalith.Builds", target_is_directory=True)
        self.manifest = json.loads((ROOT / "tools/release-packages.json").read_text())
        self.manifest["packages"].reverse()
        for index, package in enumerate(self.manifest["packages"]):
            package["id"] = f"Fixture.Package{index}"
        self.manifest_path = self.root / "override.json"
        self.manifest_path.write_text(json.dumps(self.manifest))
        self.ids = [p["id"] for p in self.manifest["packages"]]
        self.destination_path = self.root / "destinations.json"
        self.destinations = {"packages": [], "container": False, "tag": True, "release": None}
        self.save()
        for client in ("gh", "curl", "dotnet", "docker"):
            path = self.root / "bin" / client
            path.write_text(f"#!{sys.executable}\n" + CLIENT)
            path.chmod(0o755)
        execution = create_execution_fixture(self.root, actual_workflows=getattr(self, "actual_workflows", False))
        self.source_sha = execution["source_sha"]
        self.environment = {
            **os.environ,
            "PATH": str(self.root / "bin") + os.pathsep + os.environ["PATH"],
            "RECOVERY_FIXTURE": str(self.root),
            "HEXALITH_RELEASE_PACKAGE_MANIFEST": str(self.manifest_path),
            "HEXALITH_RECOVERY_STATE_DIRECTORY": str(self.root / "state"),
            "GITHUB_REPOSITORY": "fixture/recovery", "GITHUB_SHA": self.source_sha,
            "HEXALITH_BUILDS_EXECUTION_SHA": execution["builds_sha"], "HEXALITH_RELEASE_ENVIRONMENT": "production",
            "HEXALITH_RELEASE_SOURCE_BRANCH": "main", "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
            "HEXALITH_ZOT_REGISTRY": "fixture.invalid", "HEXALITH_ZOT_USERNAME": "fixture-user",
            "HEXALITH_ZOT_API_KEY": "fixture-key", "GITHUB_OUTPUT": str(self.root / "outputs"),
        }
        self.environment.pop("NUGET_API_KEY", None)
        self.environment["HEXALITH_RELEASE_NUGET_WAIT_SECONDS"] = "0"
        self.environment["HEXALITH_RELEASE_NUGET_WAIT_INTERVAL"] = "0"
        (self.root / "sitecustomize.py").write_text("from release_registry_fixture import install\ninstall()\n")
        self.environment["PYTHONPATH"] = str(self.root) + os.pathsep + str(ROOT / "tests/scripts")

    def save(self):
        self.destination_path.write_text(json.dumps(self.destinations))

    def calls(self):
        path = self.root / "calls.jsonl"
        return [json.loads(line) for line in path.read_text().splitlines()] if path.exists() else []

    def run_script(self, name, accepted=True):
        # Package/consumer validation has its own extensive test suites. Here assert
        # exact routing to that validator and execute the real manifest-driven packer.
        shell = r'''
          set -euo pipefail
          python3() {
            case "$1" in
              scripts/validate-nuget-packages.py)
                [ "${RECOVERY_FAILURE_STAGE:-}" != packages ] || return 42
                [ "$#" = 4 ] && [ "$2" = ./nupkgs ] && [ "$3" = --manifest ] && [ "$4" = "$HEXALITH_RELEASE_PACKAGE_MANIFEST" ] ;;
              scripts/verify-catalog-package-consumer.py)
                [ "${RECOVERY_FAILURE_STAGE:-}" != catalog ] || return 42
                [ "$#" = 6 ] && [ "$2" = ./nupkgs ] && [ "$3" = --expected-source-sha ] && [ "$4" = "$GITHUB_SHA" ] && [ "$5" = --output ] ;;
              scripts/validate-consumer-package-references.py)
                [ "${RECOVERY_FAILURE_STAGE:-}" != consumers ] || return 42
                [ "$#" = 2 ] && [ "$2" = ./nupkgs ] ;;
              *) command python3 "$@" ;;
            esac
          }
          source "$1" 1.2.3 "$GITHUB_SHA"
        '''
        environment = self.environment.copy()
        if name == "prepare-partial-release.sh":
            environment.pop("NUGET_API_KEY", None)
        result = subprocess.run(["bash", "-c", shell, "fixture", str(ROOT / "scripts" / name)],
                                cwd=self.root, env=environment, capture_output=True, text=True, timeout=30)
        if accepted:
            self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        else:
            self.assertNotEqual(result.returncode, 0, result.stdout + result.stderr)
        return result

    def release(self, count=5):
        self.destinations["release"] = {
            "tag_name": "v1.2.3", "draft": False, "prerelease": False,
            "assets": [{"id": i + 1, "name": package_id + ".1.2.3.nupkg",
                        "content": f"reviewed fixture package {package_id}\n"}
                       for i, package_id in enumerate(self.ids[:count])],
        }
        self.save()

    def prepare(self):
        self.run_script("validate-partial-release-recovery.sh")
        before = self.destination_path.read_bytes()
        self.run_script("prepare-partial-release.sh")
        self.assertEqual(self.destination_path.read_bytes(), before)

    def publish(self):
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.run_script("publish-partial-release.sh")

    def assert_no_publication(self):
        self.assertFalse(any(c[:3] in (["gh", "release", "upload"], ["gh", "release", "create"],
                                      ["dotnet", "nuget", "push"])
                             or c[:2] == ["dotnet", "publish"] for c in self.calls()))

    def test_required_source_gate_refuses_before_build_or_publication(self):
        for workflow in ("ci.yml", "story-guards.yml", "source-reference.yml"):
            for status, conclusion in (("completed", "failure"), ("completed", "skipped"), ("in_progress", None)):
                with self.subTest(workflow=workflow, status=status, conclusion=conclusion):
                    run = {"id": {"ci.yml": 1, "story-guards.yml": 2, "source-reference.yml": 3}[workflow],
                           "run_number": 1, "run_attempt": 1, "head_sha": self.source_sha, "head_branch": "main",
                           "event": "push", "path": ".github/workflows/" + workflow, "status": status, "conclusion": conclusion}
                    self.destinations["gate_detail"] = {workflow: run}
                    self.save()
                    result = self.run_script("validate-partial-release-recovery.sh", False)
                    self.assertIn(workflow, result.stderr)
                    self.assertFalse(any(call[0] == "dotnet" for call in self.calls()))
                    self.assert_no_publication()

    def test_gate_failure_after_preparation_refuses_publication(self):
        self.prepare()
        before = (self.root / "state/prepared-state-sha256").read_bytes()
        self.destinations["gate_detail"] = {"ci.yml": {"id": 1, "run_number": 1, "run_attempt": 2,
            "head_sha": self.source_sha, "head_branch": "main", "event": "push",
            "path": ".github/workflows/ci.yml", "status": "completed", "conclusion": "failure"}}
        self.save()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.run_script("publish-partial-release.sh", False)
        self.assertEqual((self.root / "state/prepared-state-sha256").read_bytes(), before)
        self.assert_no_publication()

    def test_recovery_tooling_must_match_source_approval(self):
        self.environment["HEXALITH_BUILDS_EXECUTION_SHA"] = "b" * 40
        result = self.run_script("validate-partial-release-recovery.sh", False)
        self.assertIn("approved execution revision", result.stderr)
        self.assert_no_publication()

    def test_tooling_and_provenance_drift_refuse_prepared_state(self):
        for filename in ("execution-identity.json", "workflow-provenance.json"):
            with self.subTest(filename=filename):
                self.prepare()
                path = self.root / "state" / filename
                original = path.read_text()
                path.write_text(original + " ")
                if filename == "execution-identity.json":
                    document = json.loads(original)
                    document["builds_execution_sha"] = "b" * 40
                    path.write_text(json.dumps(document))
                self.environment["NUGET_API_KEY"] = "fixture-key"
                self.run_script("publish-partial-release.sh", False)
                self.assert_no_publication()
                path.write_text(original)

    def test_unapproved_cached_helper_refuses_before_preparation(self):
        self.run_script("validate-partial-release-recovery.sh")
        cache = self.root / ".hexalith/builds-execution/Github/governed-provenance/__pycache__/unapproved.pyc"
        cache.parent.mkdir()
        cache.write_bytes(b"unapproved cached code")
        result = self.run_script("prepare-partial-release.sh", False)
        self.assertIn("bytecode files appeared", result.stderr)
        self.assertFalse(any(call[0] == "dotnet" for call in self.calls()))
        self.assert_no_publication()

    def test_changed_shared_helper_refuses_before_preparation(self):
        self.run_script("validate-partial-release-recovery.sh")
        helper = self.root / ".hexalith/builds-execution/Github/publish-containers/oci_registry_validator.py"
        helper.write_text(helper.read_text() + "\n# changed helper\n")
        result = self.run_script("prepare-partial-release.sh", False)
        self.assertIn("shared helper bytes changed", result.stderr)
        self.assertFalse(any(call[0] == "dotnet" for call in self.calls()))
        self.assert_no_publication()

    def test_held_publisher_mutation_refuses_before_oci_or_release_writes(self):
        import time
        self.prepare()
        self.destinations.update(hold_publish=True, mutate_helper_on_publish=True)
        self.save()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        (self.root / "calls.jsonl").write_text("")
        process = subprocess.Popen(["bash", str(ROOT / "scripts/publish-partial-release.sh"), "1.2.3"],
                                   cwd=self.root, env=self.environment, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
        try:
            deadline = time.monotonic() + 15
            while not (self.root / "publisher-held").exists() and process.poll() is None and time.monotonic() < deadline:
                time.sleep(0.02)
            self.assertTrue((self.root / "publisher-held").exists())
            (self.root / "publisher-resume").touch()
            stdout, stderr = process.communicate(timeout=15)
            self.assertNotEqual(process.returncode, 0, stdout + stderr)
            self.assertIn("shared helper bytes changed", stderr)
            calls = self.calls()
            publish = next(index for index, call in enumerate(calls) if call[:2] == ["dotnet", "publish"])
            self.assertFalse(any(call[0] == "oci" or call[:2] == ["gh", "release"] for call in calls[publish + 1:]), calls)
        finally:
            if process.poll() is None:
                process.kill()
                process.communicate()

    def test_publication_never_rebuilds_prepared_packages(self):
        self.prepare()
        (self.root / "calls.jsonl").write_text("")
        self.publish()
        self.assertFalse(any(c[:2] in (["dotnet", "build"], ["dotnet", "pack"]) for c in self.calls()))

    def test_changed_or_missing_prepared_bytes_refuse(self):
        for mutation in ("changed", "missing", "proof", "state-proof"):
            with self.subTest(mutation=mutation):
                self.prepare()
                package = self.root / "nupkgs" / (self.ids[-1] + ".1.2.3.nupkg")
                if mutation == "changed": package.write_text("changed bytes")
                if mutation == "missing": package.unlink()
                if mutation == "proof": (self.root / "state/prepared-packages-sha256").unlink()
                if mutation == "state-proof": (self.root / "state/prepared-state-sha256").unlink()
                self.environment["NUGET_API_KEY"] = "fixture-key"
                self.run_script("publish-partial-release.sh", accepted=False)
                self.assert_no_publication()

    def test_changed_validated_state_refuses(self):
        for name, content in (("missing-packages", ""), ("existing-packages", ""),
                              ("existing-package-sha256", "altered\n"), ("container-state", "present\n")):
            with self.subTest(name=name):
                self.destinations.update(packages=[self.ids[0]], container=False)
                self.save()
                self.prepare()
                (self.root / "calls.jsonl").write_text("")
                (self.root / "state" / name).write_text(content)
                self.environment["NUGET_API_KEY"] = "fixture-key"
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("state changed after package preparation", result.stderr)
                self.assert_no_publication()

    def test_missing_or_invalid_container_state_refuses_preparation(self):
        for value in (None, "", "unknown"):
            with self.subTest(value=value):
                self.run_script("validate-partial-release-recovery.sh")
                path = self.root / "state/container-state"
                if value is None: path.unlink()
                else: path.write_text(value)
                (self.root / "calls.jsonl").write_text("")
                self.run_script("prepare-partial-release.sh", accepted=False)
                self.assertFalse(any(c[0] == "dotnet" for c in self.calls()))
                self.assert_no_publication()

    def test_inventory_mutation_during_build_cannot_become_prepared_evidence(self):
        self.destinations["mutate_missing_on_build"] = True
        self.save()
        self.run_script("validate-partial-release-recovery.sh")
        result = self.run_script("prepare-partial-release.sh", accepted=False)
        self.assertIn("state changed during package preparation", result.stderr)
        self.assertFalse((self.root / "state/prepared-state-sha256").exists())
        self.assert_no_publication()

    def test_failed_preparation_revokes_old_evidence_and_stops_publication(self):
        for stage in ("build", "pack", "packages", "consumers"):
            with self.subTest(stage=stage):
                self.destinations["failure_stage"] = stage
                self.environment["RECOVERY_FAILURE_STAGE"] = stage
                self.save()
                self.run_script("validate-partial-release-recovery.sh")
                for name in ("prepared-state-sha256", "prepared-packages-sha256"):
                    (self.root / "state" / name).write_text("stale accepted evidence")
                before = self.destination_path.read_bytes()
                self.run_script("prepare-partial-release.sh", accepted=False)
                self.assertEqual(self.destination_path.read_bytes(), before)
                self.assertFalse((self.root / "state/prepared-state-sha256").exists())
                self.assertFalse((self.root / "state/prepared-packages-sha256").exists())
                self.environment["NUGET_API_KEY"] = "fixture-key"
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("Prepared package evidence is required", result.stderr)
                self.assert_no_publication()

    def test_destination_context_changes_refuse_before_clients(self):
        self.prepare()
        for name, value in (("GITHUB_REPOSITORY", "other/repository"),
                            ("HEXALITH_ZOT_REGISTRY", "other.invalid"),
                            ("HEXALITH_RECOVERY_CONTAINER_REPOSITORY", "other-image")):
            with self.subTest(name=name):
                old_value = self.environment.get(name)
                self.environment[name] = value
                self.environment["NUGET_API_KEY"] = "fixture-key"
                (self.root / "calls.jsonl").write_text("")
                result = self.run_script("publish-partial-release.sh", accepted=False)
                expected = "workflow identity is unverifiable" if name == "GITHUB_REPOSITORY" else "state changed after package preparation"
                self.assertIn(expected, result.stderr)
                self.assertEqual(self.calls(), [])
                if old_value is None: self.environment.pop(name)
                else: self.environment[name] = old_value

    def test_missing_empty_or_whitespace_key_refuses_before_reads(self):
        self.prepare()
        for key in (None, "", " \t\r\n"):
            with self.subTest(key=key):
                if key is None: self.environment.pop("NUGET_API_KEY", None)
                else: self.environment["NUGET_API_KEY"] = key
                (self.root / "calls.jsonl").write_text("")
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("NUGET_API_KEY is required for missing packages", result.stderr)
                self.assertEqual(self.calls(), [])

    def test_entirely_absent_release_refuses_and_preserves_previous_state(self):
        self.destinations["tag"] = False
        self.save()
        state = self.root / "state"
        state.mkdir()
        (state / "previous").write_text("preserved")
        result = self.run_script("validate-partial-release-recovery.sh", accepted=False)
        self.assertIn("requires evidence of an existing partial release", result.stderr)
        self.assertEqual((state / "previous").read_text(), "preserved")
        self.assertFalse((state / "source-sha").exists())
        self.assertEqual(len(list((state / "failures").iterdir())), 1)
        self.assertFalse((self.root / "outputs").exists())

    def test_package_without_tag_is_existing_evidence(self):
        self.destinations.update(tag=False, packages=[self.ids[0]])
        self.save()
        self.prepare()
        self.assertIn("needs-nuget=true", (self.root / "outputs").read_text())
        self.publish()
        self.run_script("verify-partial-release.sh")

    def test_container_without_tag_is_existing_evidence(self):
        self.destinations.update(tag=False, container=True)
        self.save()
        self.prepare()
        self.publish()
        self.run_script("verify-partial-release.sh")

    def test_release_only_repair_needs_no_nuget_credential(self):
        self.destinations.update(packages=self.ids, container=True)
        self.save()
        self.prepare()
        self.assertEqual((self.root / "outputs").read_text(), "needs-nuget=false\n")
        self.run_script("publish-partial-release.sh")
        self.run_script("verify-partial-release.sh")
        self.assertFalse(any(c[:3] == ["dotnet", "nuget", "push"] for c in self.calls()))

    def test_override_inventory_drives_full_pipeline(self):
        self.prepare()
        self.publish()
        previous_reads = len(self.calls())
        self.run_script("verify-partial-release.sh")
        self.assertEqual([c[2] for c in self.calls() if c[:2] == ["dotnet", "pack"]],
                         [p["project"] for p in self.manifest["packages"]])
        self.assertEqual(json.loads(self.destination_path.read_text())["packages"], self.ids)
        self.assertEqual([asset["name"] for asset in json.loads(self.destination_path.read_text())["release"]["assets"]],
                         sorted(f"{package_id}.1.2.3.nupkg" for package_id in self.ids))
        verification_urls = [next(a for a in call if a.startswith("https://"))
                             for call in self.calls()[previous_reads:] if call[0] == "curl"]
        self.assertEqual(verification_urls,
                         [f"https://api.nuget.org/v3-flatcontainer/{p.lower()}/1.2.3/{p.lower()}.1.2.3.nupkg"
                          for p in self.ids])
        self.assertTrue(any(call[0] == "oci" for call in self.calls()[previous_reads:]))

    def test_changed_inventory_refuses_before_build_or_publication(self):
        self.prepare()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.manifest_path.write_text(self.manifest_path.read_text() + "\n")
        (self.root / "calls.jsonl").write_text("")
        result = self.run_script("publish-partial-release.sh", accepted=False)
        self.assertIn("manifest changed", result.stderr)
        self.assertFalse(any(c[0] in ("dotnet", "docker") for c in self.calls()))

    def test_missing_manifest_proof_refuses(self):
        self.prepare()
        (self.root / "state/package-manifest-sha256").unlink()
        (self.root / "calls.jsonl").write_text("")
        self.run_script("publish-partial-release.sh", accepted=False)
        self.assertFalse(any(c[0] == "dotnet" for c in self.calls()))

    def test_invalid_or_duplicate_manifest_refuses_before_api_reads(self):
        for invalid in (None, {}, {"packages": []}, {"packages": [self.manifest["packages"][0]] * 5}):
            with self.subTest(invalid=invalid):
                self.manifest_path.write_text(json.dumps(invalid))
                self.run_script("validate-partial-release-recovery.sh", accepted=False)
                self.assertEqual(self.calls(), [])

    def test_invalid_project_paths_refuse_before_api_reads_or_build(self):
        for project in ("../outside.csproj", "--option.csproj", "src/missing.csproj", str(ROOT / "README.md")):
            with self.subTest(project=project):
                self.manifest["packages"][0]["project"] = project
                self.manifest_path.write_text(json.dumps(self.manifest))
                self.run_script("validate-partial-release-recovery.sh", accepted=False)
                self.assertEqual(self.calls(), [])

    def test_inventory_change_during_asset_download_stops_every_publication(self):
        self.release(2)
        self.destinations["mutate_manifest_on_download"] = True
        self.save()
        self.prepare()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        result = self.run_script("publish-partial-release.sh", accepted=False)
        self.assertIn("manifest changed", result.stderr)
        self.assertFalse(any(c[:3] in (["gh", "release", "upload"], ["dotnet", "nuget", "push"])
                             or c[:2] == ["dotnet", "publish"] for c in self.calls()))

    def test_preparation_evidence_is_rechecked_after_asset_reads(self):
        for mutation in ("mutate_package_on_download", "mutate_state_on_download"):
            with self.subTest(mutation=mutation):
                self.release(2)
                self.destinations[mutation] = True
                self.save()
                self.prepare()
                self.environment["NUGET_API_KEY"] = "fixture-key"
                (self.root / "calls.jsonl").write_text("")
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("changed", result.stderr)
                self.assert_no_publication()
                self.destinations.pop(mutation)

    def test_existing_release_repair_needs_no_nuget_key(self):
        self.release(2)
        self.destinations.update(packages=self.ids, container=True)
        self.save()
        self.prepare()
        self.assertEqual((self.root / "outputs").read_text(), "needs-nuget=false\n")
        self.run_script("publish-partial-release.sh")
        self.run_script("verify-partial-release.sh")
        self.assertEqual(len([c for c in self.calls() if c[:3] == ["gh", "release", "upload"]]), 1)
        self.assertFalse(any(c[:3] == ["dotnet", "nuget", "push"] for c in self.calls()))

    def test_container_only_repair_needs_no_nuget_key(self):
        self.release()
        self.destinations.update(packages=self.ids, container=False)
        self.save()
        self.prepare()
        self.assertEqual((self.root / "outputs").read_text(), "needs-nuget=false\n")
        self.run_script("publish-partial-release.sh")
        self.run_script("verify-partial-release.sh")
        self.assertTrue(any(c[:2] == ["dotnet", "publish"] for c in self.calls()))
        self.assertFalse(any(c[:3] in (["dotnet", "nuget", "push"], ["gh", "release", "upload"])
                             for c in self.calls()))

    def test_existing_release_gets_only_missing_assets_without_replacement(self):
        self.release(2)
        original = copy.deepcopy(self.destinations["release"]["assets"])
        self.prepare()
        self.publish()
        final = json.loads(self.destination_path.read_text())
        self.assertEqual(final["release"]["assets"][:2], original)
        uploads = [c for c in self.calls() if c[:3] == ["gh", "release", "upload"]]
        self.assertEqual(uploads, [["gh", "release", "upload", "v1.2.3",
                                   *[f"nupkgs/{p}.1.2.3.nupkg" for p in self.ids[2:]]]])
        self.run_script("verify-partial-release.sh")

    def test_complete_matching_release_uploads_nothing(self):
        self.release()
        self.prepare()
        self.publish()
        self.run_script("verify-partial-release.sh")
        self.assertFalse(any(c[:3] == ["gh", "release", "upload"] for c in self.calls()))

    def test_conflicting_asset_refuses_before_every_publication(self):
        self.release(2)
        self.destinations["release"]["assets"][1]["content"] = "unreviewed binary"
        self.save()
        self.prepare()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        result = self.run_script("publish-partial-release.sh", accepted=False)
        self.assertIn("does not match the reviewed package", result.stderr)
        self.assertFalse(any(c[:3] in (["gh", "release", "upload"], ["dotnet", "nuget", "push"])
                             or c[:2] == ["dotnet", "publish"] for c in self.calls()))

    def test_unverifiable_asset_metadata_or_download_refuses(self):
        self.environment["NUGET_API_KEY"] = "fixture-key"
        for mutation in ("duplicate-name", "duplicate-id", "unknown-name", "invalid-id", "missing-download"):
            with self.subTest(mutation=mutation):
                self.release(2)
                assets = self.destinations["release"]["assets"]
                if mutation == "duplicate-name": assets[1]["name"] = assets[0]["name"]
                if mutation == "duplicate-id": assets[1]["id"] = assets[0]["id"]
                if mutation == "unknown-name": assets[1]["name"] = "../unreviewed.nupkg"
                if mutation == "invalid-id": assets[1]["id"] = "https://untrusted.invalid"
                if mutation == "missing-download": assets[1]["unavailable"] = True
                self.save()
                self.prepare()
                self.run_script("publish-partial-release.sh", accepted=False)
                self.assertFalse(any(c[:3] == ["gh", "release", "upload"] for c in self.calls()))

    def test_final_verification_rejects_tampered_asset(self):
        self.prepare()
        self.publish()
        self.destinations = json.loads(self.destination_path.read_text())
        self.destinations["release"]["assets"][-1]["content"] = "wrong bytes"
        self.save()
        result = self.run_script("verify-partial-release.sh", accepted=False)
        self.assertIn("does not match the reviewed package", result.stderr)

    def test_invalid_existing_oci_content_is_rejected_without_publication(self):
        for mutation in ("label", "platform", "digest", "size", "unresolved", "negotiation", "missing-created", "malformed-created", "mismatched-created"):
            with self.subTest(mutation=mutation):
                self.destinations.update(container=True, oci_mutation=mutation)
                self.save()
                self.run_script("validate-partial-release-recovery.sh", accepted=False)
                self.assert_no_publication()

    def test_oci_network_read_cannot_hide_source_or_preparation_changes(self):
        for mutation in ("mutate_main_on_oci", "mutate_state_on_oci", "mutate_manifest_on_oci"):
            with self.subTest(mutation=mutation):
                self.destinations = {"packages": [], "container": True, "tag": True, "release": None}
                self.save()
                self.prepare()
                self.destinations[mutation] = True
                self.save()
                self.environment["NUGET_API_KEY"] = "fixture-key"
                (self.root / "calls.jsonl").write_text("")
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("changed", result.stderr)
                self.assert_no_publication()

    def test_signed_existing_nuget_package_can_prepare_matching_unsigned_payload(self):
        import base64
        import io
        import zipfile
        unsigned, signed = io.BytesIO(), io.BytesIO()
        with zipfile.ZipFile(unsigned, "w") as archive:
            archive.writestr("payload.dll", b"reviewed payload")
        with zipfile.ZipFile(signed, "w") as archive:
            archive.writestr("payload.dll", b"reviewed payload")
            archive.writestr(".signature.p7s", b"repository signing fixture")
        package_id = self.ids[0]
        self.destinations.update(packages=[package_id],
            package_content_base64={package_id: base64.b64encode(signed.getvalue()).decode()},
            prepared_package_base64={package_id: base64.b64encode(unsigned.getvalue()).decode()})
        self.save()
        self.prepare()
        observed = self.root / "state" / (package_id + ".1.2.3.nupkg")
        self.assertEqual(observed.read_bytes(), signed.getvalue())
        self.assertIn("existing-package-sha256", (self.root / "state/prepared-state-sha256").read_text())
        observed.write_bytes(b"changed observed archive")
        result = self.run_script("prepare-partial-release.sh", accepted=False)
        self.assertIn("destination archive changed", result.stderr)

    def test_rejected_oci_observations_are_retained_without_replacing_accepted_state(self):
        self.destinations.update(packages=[self.ids[0]], container=True)
        self.save()
        self.run_script("validate-partial-release-recovery.sh")
        state = self.root / "state"
        accepted = {path: path.read_bytes() for path in state.rglob("*") if path.is_file()}
        self.destinations["oci_mutation"] = "label"
        self.save()
        self.run_script("validate-partial-release-recovery.sh", accepted=False)
        for path, raw in accepted.items(): self.assertEqual(path.read_bytes(), raw)
        failures = list((state / "failures").iterdir())
        self.assertEqual(len(failures), 1)
        self.assertTrue((failures[0] / (self.ids[0] + ".1.2.3.nupkg")).is_file())
        self.assertIn("provenance-label-mismatch", (failures[0] / "oci-validation.stderr.log").read_text())
        self.assertIn("rejected", (failures[0] / "validation-result").read_text())

    def test_changed_existing_oci_digest_refuses_before_publication(self):
        self.destinations["container"] = True
        self.save()
        self.prepare()
        self.destinations["oci_generation"] = 1
        self.save()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        result = self.run_script("publish-partial-release.sh", accepted=False)
        self.assertIn("digest changed", result.stderr)
        self.assert_no_publication()

    def test_new_oci_destination_refuses_before_any_publication(self):
        self.prepare()
        self.destinations["container"] = True
        self.save()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.run_script("publish-partial-release.sh", accepted=False)
        self.assert_no_publication()

    def test_changed_oci_receipt_refuses_before_clients(self):
        self.destinations["container"] = True
        self.save()
        self.prepare()
        receipt = self.root / "state/oci-validated/oci-validation.json"
        receipt.write_text(receipt.read_text() + "\n")
        (self.root / "calls.jsonl").write_text("")
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.run_script("publish-partial-release.sh", accepted=False)
        self.assertEqual(self.calls(), [])

    def test_final_verification_rejects_changed_nuget_bytes(self):
        self.prepare()
        self.publish()
        self.destinations = json.loads(self.destination_path.read_text())
        self.destinations["nuget_content"] = {self.ids[-1]: "wrong bytes"}
        self.save()
        result = self.run_script("verify-partial-release.sh", accepted=False)
        self.assertIn("does not match the reviewed package", result.stderr)

    def test_evidence_paths_cannot_escape_workspace(self):
        with tempfile.TemporaryDirectory() as outside:
            for value in (outside, str(self.root / "escape/state")):
                if value != outside: (self.root / "escape").symlink_to(outside, target_is_directory=True)
                self.environment["HEXALITH_RECOVERY_STATE_DIRECTORY"] = value
                self.run_script("validate-partial-release-recovery.sh", accepted=False)
                self.assert_no_publication()
                self.assertEqual(list(Path(outside).iterdir()), [])

    def test_release_metadata_is_rechecked_before_publication(self):
        self.environment["NUGET_API_KEY"] = "fixture-key"
        for field, value in (("draft", True), ("tag_name", "v9.9.9"), ("prerelease", True)):
            with self.subTest(field=field):
                self.release(2)
                self.prepare()
                self.destinations["release"][field] = value
                self.save()
                result = self.run_script("publish-partial-release.sh", accepted=False)
                self.assertIn("Release metadata does not match the reviewed version", result.stderr)
                self.assertFalse(any(c[:3] in (["gh", "release", "upload"], ["dotnet", "nuget", "push"])
                                     or c[:2] == ["dotnet", "publish"] for c in self.calls()))


if __name__ == "__main__":
    unittest.main()
