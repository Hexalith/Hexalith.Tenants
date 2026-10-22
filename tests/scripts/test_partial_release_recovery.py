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


ROOT = Path(__file__).resolve().parents[2]
SOURCE_SHA = "a" * 40

# Installed only in each temporary test directory. No live destination is reachable.
CLIENT = r'''
import json, os, pathlib, sys
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
def fail(): sys.exit("Unexpected fixture request: " + kind + " " + repr(args))
if kind == "curl":
    url = next((a for a in args if a.startswith("https://")), "")
    if "v3-flatcontainer/" in url:
        package_id = next((p["id"] for p in manifest["packages"] if "/" + p["id"].lower() + "/" in url), None)
        if package_id is None: fail()
        if "--output" in args and args[args.index("--output") + 1] != "/dev/null":
            pathlib.Path(args[args.index("--output") + 1]).write_text(content(package_id))
        else: print(200 if package_id in state["packages"] else 404)
    elif "/v2/tenants/manifests/" in url: print(200 if state["container"] else 404)
    else: fail()
elif kind == "gh":
    if args[0] == "api":
        endpoint = args[2] if args[1] in ("--include", "--method") else args[1]
        if args[1] == "--method": endpoint = args[3]
        if endpoint.endswith("git/ref/heads/main"): print(os.environ["GITHUB_SHA"])
        elif "/actions/workflows/" in endpoint: print(1)
        elif "/git/ref/tags/" in endpoint:
            if "--include" in args: print("HTTP/2.0 200 OK" if state["tag"] else "HTTP/2.0 404 Not Found")
            elif "--jq" in args: print("commit" if args[-1] == ".object.type" else os.environ["GITHUB_SHA"])
            else: print(json.dumps({"object": {"type": "commit", "sha": os.environ["GITHUB_SHA"]}}))
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
            sys.stdout.write(asset["content"])
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
    if args[0] == "build": pass
    elif args[0] == "pack":
        project = args[1]
        package = next((p for p in manifest["packages"] if p["project"] == project), None)
        if package is None: fail()
        version = next(a.split("=", 1)[1] for a in args if a.startswith("-p:Version="))
        directory = pathlib.Path(args[args.index("--output") + 1])
        (directory / (package["id"] + "." + version + ".nupkg")).write_text(content(package["id"]))
    elif args[:2] == ["nuget", "push"]:
        if args[-1] != "fixture-key": fail()
        state["packages"].append(pathlib.Path(args[2]).name.removesuffix(".1.2.3.nupkg")); save()
    elif args[0] == "publish": state["container"] = True; save()
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
        self.environment = {
            **os.environ,
            "PATH": str(self.root / "bin") + os.pathsep + os.environ["PATH"],
            "RECOVERY_FIXTURE": str(self.root),
            "HEXALITH_RELEASE_PACKAGE_MANIFEST": str(self.manifest_path),
            "HEXALITH_RECOVERY_STATE_DIRECTORY": str(self.root / "state"),
            "GITHUB_REPOSITORY": "fixture/recovery", "GITHUB_SHA": SOURCE_SHA,
            "HEXALITH_BUILDS_EXECUTION_SHA": SOURCE_SHA, "HEXALITH_RELEASE_ENVIRONMENT": "production",
            "HEXALITH_RELEASE_SOURCE_BRANCH": "main", "HEXALITH_RELEASE_SOURCE_CI_WORKFLOW": "ci.yml",
            "HEXALITH_ZOT_REGISTRY": "fixture.invalid", "HEXALITH_ZOT_USERNAME": "fixture-user",
            "HEXALITH_ZOT_API_KEY": "fixture-key", "GITHUB_OUTPUT": str(self.root / "outputs"),
        }
        self.environment.pop("NUGET_API_KEY", None)

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
                [ "$#" = 4 ] && [ "$2" = ./nupkgs ] && [ "$3" = --manifest ] && [ "$4" = "$HEXALITH_RELEASE_PACKAGE_MANIFEST" ] ;;
              scripts/validate-consumer-package-references.py) [ "$#" = 2 ] && [ "$2" = ./nupkgs ] ;;
              *) command python3 "$@" ;;
            esac
          }
          source "$1" 1.2.3 "$GITHUB_SHA"
        '''
        result = subprocess.run(["bash", "-c", shell, "fixture", str(ROOT / "scripts" / name)],
                                cwd=self.root, env=self.environment, capture_output=True, text=True, timeout=30)
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

    def publish(self):
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.run_script("publish-partial-release.sh")

    def test_entirely_absent_release_refuses_and_preserves_previous_state(self):
        self.destinations["tag"] = False
        self.save()
        state = self.root / "state"
        state.mkdir()
        (state / "previous").write_text("preserved")
        result = self.run_script("validate-partial-release-recovery.sh", accepted=False)
        self.assertIn("requires evidence of an existing partial release", result.stderr)
        self.assertEqual(list(state.iterdir()), [state / "previous"])
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
                          for p in self.ids] + ["https://fixture.invalid/v2/tenants/manifests/1.2.3"])

    def test_changed_inventory_refuses_before_build_or_publication(self):
        self.prepare()
        self.environment["NUGET_API_KEY"] = "fixture-key"
        self.manifest_path.write_text(self.manifest_path.read_text() + "\n")
        result = self.run_script("publish-partial-release.sh", accepted=False)
        self.assertIn("manifest changed", result.stderr)
        self.assertFalse(any(c[0] in ("dotnet", "docker") for c in self.calls()))

    def test_missing_manifest_proof_refuses(self):
        self.prepare()
        (self.root / "state/package-manifest-sha256").unlink()
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
