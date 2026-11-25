"""Exercise real private-topology cleanup without starting Dapr or Aspire."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("product_integration", ROOT / "scripts/run-product-integration.py")
PRODUCT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PRODUCT)


class ProductPackageSelectionTests(unittest.TestCase):
    def write_manifest(self, root, host, scenario):
        packages = ["Hexalith.EventStore.Contracts/5.6.0", "Hexalith.McpCli.Abstractions/3.0.0"]
        if host == "Hexalith.Tenants.AppHost":
            packages.append("Hexalith.EventStore.Aspire/5.6.0")
        if scenario == "stale-domain" and host == "Hexalith.Tenants":
            packages[0] = "Hexalith.EventStore.Contracts/5.3.1"
        runtime = {key: {"runtime": {key.split("/")[0] + ".dll": {}}} for key in packages}
        libraries = {key: {"type": "package", "sha512": "sha512-fixture-hash"} for key in packages}
        targets = {"current": runtime.copy(), "other": runtime}
        if host == "Hexalith.Tenants.Api" and scenario == "api-project":
            libraries[packages[1]]["type"] = "project"
        if host == "Hexalith.Tenants.UI" and scenario == "ui-inactive":
            del targets["current"][packages[1]]
        if host == "Hexalith.Tenants.AppHost" and scenario == "apphost-hash":
            libraries[packages[2]]["sha512"] = "sha512-another-package"
        (root / (host + ".deps.json")).write_text(json.dumps({
            "runtimeTarget": {"name": "current"}, "targets": targets, "libraries": libraries}))

    def test_only_the_restored_and_built_shipping_packages_can_start_the_product_profiles(self):
        for scenario in ("current", "stale-restore", "stale-domain", "api-project", "ui-inactive", "apphost-hash", "stale-host"):
            with self.subTest(scenario=scenario), tempfile.TemporaryDirectory() as temporary:
                root = Path(temporary)
                restored = "5.3.1" if scenario == "stale-restore" else "5.6.0"
                package = "Hexalith.EventStore.Aspire/" + restored
                package_path = root / "feed" / package.lower()
                package_path.mkdir(parents=True)
                (package_path / "hexalith.eventstore.aspire.nuspec").write_text(
                    f'<package><metadata><id>Hexalith.EventStore.Aspire</id><version>{restored}</version>'
                    f'<repository url="https://github.com/Hexalith/Hexalith.EventStore" commit="{"e" * 40}" />'
                    '</metadata></package>')
                assets = root / "src/Hexalith.Tenants.AppHost/obj/project.assets.json"
                assets.parent.mkdir(parents=True)
                assets.write_text(json.dumps({"packageFolders": {str(root / "feed"): {}},
                    "targets": {"net11.0": {package: {"type": "package"}}},
                    "libraries": {package: {"type": "package", "path": package.lower(), "sha512": "fixture-hash"}}}))
                for host in PRODUCT.PROFILE.RUNTIME_HOSTS:
                    self.write_manifest(root, host, scenario)

                def command(*arguments, env=None):
                    if arguments[:2] == ("dotnet", "msbuild"):
                        self.assertIn("-p:Configuration=Release", arguments)
                        if "-getProperty:ProjectDepsFilePath" in arguments:
                            return str(root / (Path(arguments[2]).stem + ".deps.json"))
                        self.assertNotIn("HexalithEventStoreVersion", env)
                        self.assertNotIn("HexalithMcpCliVersion", env)
                        self.assertFalse(any(arg.startswith("-p:HexalithEventStoreVersion=") for arg in arguments))
                        return json.dumps({"Properties": {"HexalithEventStoreVersion": "5.6.0", "HexalithMcpCliVersion": "3.0.0"}})
                    self.assertEqual(arguments, ("git", "-C", str(root / "references/Hexalith.EventStore"), "rev-parse", "HEAD"))
                    return ("a" if scenario == "stale-host" else "e") * 40

                with mock.patch.object(PRODUCT, "ROOT", root), mock.patch.object(PRODUCT.PROFILE, "command", command), \
                        mock.patch.object(PRODUCT.subprocess, "run") as ordinary, mock.patch.object(PRODUCT, "run_profile") as cold, \
                        mock.patch.dict(os.environ, {"RUNNER_TEMP": temporary, "HexalithEventStoreVersion": "5.3.1",
                                                     "HexalithMcpCliVersion": "2.0.1"}, clear=True):
                    if scenario == "current":
                        PRODUCT.main()
                        ordinary.assert_called_once()
                        self.assertEqual([call.args for call in cold.call_args_list], list(PRODUCT.PROFILES))
                        self.assertTrue(all(call.kwargs["producer"] == restored for call in cold.call_args_list))
                        report = json.loads((root / "tenants-product-profiles/package-provenance.json").read_text())
                        self.assertEqual(set(report["runtimeManifests"]), set(PRODUCT.PROFILE.RUNTIME_HOSTS))
                        self.assertNotIn(temporary, json.dumps(report))
                    else:
                        with self.assertRaises(ValueError):
                            PRODUCT.main()
                        ordinary.assert_not_called()
                        cold.assert_not_called()
                        self.assertFalse((root / "tenants-product-profiles").exists())


class PrivateTopologyCleanupTests(unittest.TestCase):
    def test_cleanup_awaits_private_consumers_and_preserves_an_unrelated_process(self):
        with tempfile.TemporaryDirectory() as temporary:
            private = Path(temporary) / "private"
            foreign = Path(temporary) / "foreign"
            private.mkdir()
            foreign.mkdir()
            processes = []
            try:
                for directory in (private, foreign):
                    processes.append(subprocess.Popen([sys.executable, "-c", "import time; time.sleep(60)"],
                        env={**os.environ, "TMPDIR": str(directory)}, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
                self.assertIn(processes[0].pid, PRODUCT.PROFILE.active_processes(private))
                with mock.patch.object(PRODUCT.PROFILE, "private_containers", return_value=[]):
                    PRODUCT.stop_private_consumers(private)
                self.assertIsNotNone(processes[0].wait(timeout=5))
                self.assertIsNone(processes[1].poll())
                self.assertEqual(PRODUCT.PROFILE.active_processes(private), [])
            finally:
                for process in processes:
                    if process.poll() is None:
                        process.terminate()
                    process.wait(timeout=5)


    def test_failed_fixture_cleanup_stops_and_removes_only_mount_bound_containers(self):
        calls = []
        with mock.patch.object(PRODUCT.PROFILE, "active_processes", return_value=[]), \
                mock.patch.object(PRODUCT.PROFILE, "private_containers", side_effect=[["a" * 64], []]), \
                mock.patch.object(PRODUCT.PROFILE, "command", side_effect=lambda *args: calls.append(args)):
            PRODUCT.stop_private_consumers(Path("/job/private"))
        self.assertEqual(calls, [("docker", "stop", "--time", "15", "a" * 64), ("docker", "rm", "a" * 64)])

    def test_malformed_provisioning_cannot_select_a_foreign_failure_cleanup_directory(self):
        with tempfile.TemporaryDirectory() as directory:
            temporary = Path(directory)
            output = temporary / "results"
            output.mkdir()
            foreign = temporary / "foreign"
            foreign.mkdir()
            marker = foreign / "preserve"
            marker.write_bytes(b"foreign material")
            for candidate in (temporary, temporary.parent, foreign):
                with self.subTest(path=candidate):
                    def provision(*args, **kwargs):
                        (output / "rest.json").write_text(json.dumps({"privateDirectory": str(candidate)}))
                    with mock.patch.object(PRODUCT.subprocess, "run", side_effect=provision), \
                            mock.patch.object(PRODUCT, "stop_private_consumers") as stop, \
                            mock.patch.object(PRODUCT.shutil, "rmtree") as remove:
                        with self.assertRaises(ValueError):
                            PRODUCT.run_profile("rest", "example", 1, 1, False, False,
                                temporary=temporary, output=output, producer="5.3.1", source="a" * 40)
                        stop.assert_not_called()
                        remove.assert_not_called()
                    self.assertEqual(marker.read_bytes(), b"foreign material")


class NaturalExitSynchronizationTests(unittest.TestCase):
    def exercise(self, scenario):
        """Use real children around the hook and the original finish acceptance guard."""
        with tempfile.TemporaryDirectory(prefix="tenants-natural-exit-test-") as directory:
            temporary = Path(directory)
            private = temporary / "tenants-source-catalog-owned"
            private.mkdir(mode=0o700)
            components = private / "components"
            components.mkdir(mode=0o700)
            component = components / "statestore.yaml"
            component.write_bytes(b"original private component")
            expected_hash = PRODUCT.PROFILE.digest(component)
            output = temporary / "results"
            output.mkdir()
            receipt = output / "rest.json"
            active = temporary / "tenants-catalog-active.json"
            ready = temporary / "ready"
            natural = temporary / "natural-exit"
            foreign = temporary / "foreign"
            foreign.mkdir()
            identity = {"repository": "Hexalith/Hexalith.Tenants", "runId": "42", "runAttempt": "1", "job": "integration"}
            original_popen = subprocess.Popen
            original_finish = PRODUCT.PROFILE.finish
            processes = []
            observations = []
            started = time.monotonic()

            def provision(*args, **kwargs):
                PRODUCT.PROFILE.write_exclusive(receipt, {
                    "schema": "tenants.catalog-profile.v1", "identity": identity,
                    "privateDirectory": str(private), "components": {component.name: expected_hash}})
                PRODUCT.PROFILE.write_exclusive(active, {"receipt": str(receipt), "sha256": PRODUCT.PROFILE.digest(receipt)})
                if scenario != "missing-proof":
                    PRODUCT.PROFILE.write_exclusive(private / "shutdown.json", {
                        "provisioningSha256": PRODUCT.PROFILE.digest(receipt), "disposed": True})
                Path(kwargs["env"]["GITHUB_ENV"]).write_text("TENANTS_TEST_PROFILE=true\n")
                # This consumer exits on its own after the test process has completed.
                # Removing custody early prevents it from writing its natural-exit proof.
                code = ("import os,pathlib,sys,time; pathlib.Path(sys.argv[1]).write_text('ready'); "
                        "time.sleep(float(sys.argv[2])); "
                        "sys.exit(41) if not pathlib.Path(os.environ['TMPDIR']).is_dir() else None; "
                        "pathlib.Path(sys.argv[3]).write_text('changed') if sys.argv[3] != '-' else None; "
                        "pathlib.Path(sys.argv[4]).write_text('natural')")
                processes.append(original_popen([sys.executable, "-c", code, str(ready),
                    "60" if scenario == "persistent" else "0.35",
                    str(component) if scenario == "changed-component" else "-", str(natural)],
                    env={**os.environ, "TMPDIR": str(private)}, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
                deadline = time.monotonic() + 5
                while not ready.exists():
                    if time.monotonic() >= deadline:
                        self.fail("The controlled private consumer did not become ready.")
                    time.sleep(0.01)

            def completed_test(arguments, **kwargs):
                self.assertEqual(arguments[:2], ["dotnet", "test"])
                return original_popen([sys.executable, "-c", "pass"], **kwargs)

            def finish(*args):
                observations.append({"active": PRODUCT.PROFILE.active_processes(private), "natural": natural.exists(),
                    "custody": private.is_dir(), "proof": (private / "shutdown.json").exists()})
                return original_finish(*args)

            try:
                processes.append(original_popen([sys.executable, "-c", "import time; time.sleep(60)"],
                    env={**os.environ, "TMPDIR": str(foreign)}, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))
                with mock.patch.object(PRODUCT.subprocess, "run", side_effect=provision), \
                        mock.patch.object(PRODUCT.subprocess, "Popen", side_effect=completed_test), \
                        mock.patch.object(PRODUCT.PROFILE, "job_identity", return_value=identity), \
                        mock.patch.object(PRODUCT.PROFILE, "private_containers", return_value=[]), \
                        mock.patch.object(PRODUCT.PROFILE, "finish", side_effect=finish) as final_guard, \
                        mock.patch.object(PRODUCT, "stop_private_consumers", wraps=PRODUCT.stop_private_consumers) as emergency, \
                        mock.patch.object(PRODUCT, "NATURAL_EXIT_TIMEOUT_SECONDS", 0.2 if scenario == "persistent" else 3, create=True):
                    arguments = ("rest", "example", 1, 1, False, False)
                    options = {"temporary": temporary, "output": output, "producer": "5.3.1", "source": "a" * 40}
                    if scenario == "persistent":
                        with self.assertRaisesRegex(ValueError, "^Private topology consumers did not exit before the shutdown deadline\\.$"):
                            PRODUCT.run_profile(*arguments, **options)
                        self.assertGreaterEqual(time.monotonic() - started, 0.2)
                        self.assertLess(time.monotonic() - started, 5)
                        final_guard.assert_not_called()
                        emergency.assert_called_once_with(private)
                        self.assertFalse(natural.exists())
                        self.assertIsNotNone(processes[1].wait(timeout=5))
                    else:
                        if scenario == "changed-component":
                            with self.assertRaisesRegex(ValueError, "^The private component set changed during qualification\\.$"):
                                PRODUCT.run_profile(*arguments, **options)
                        elif scenario == "missing-proof":
                            with self.assertRaises(FileNotFoundError): PRODUCT.run_profile(*arguments, **options)
                        else:
                            PRODUCT.run_profile(*arguments, **options)
                            emergency.assert_not_called()
                        final_guard.assert_called_once_with(receipt, temporary, identity)
                        self.assertEqual(processes[1].wait(timeout=5), 0)
                        self.assertEqual(natural.read_text(), "natural")
                        self.assertEqual(observations, [{"active": [], "natural": True, "custody": True,
                            "proof": scenario != "missing-proof"}])
                    self.assertIsNone(processes[0].poll())
                    self.assertFalse(private.exists())
                    self.assertEqual(active.exists(), scenario != "delayed")
                    self.assertTrue(receipt.is_file())
            finally:
                for process in processes:
                    if process.poll() is None:
                        process.terminate()
                    process.wait(timeout=5)

    def test_delayed_natural_exit_is_awaited_before_original_finish_releases_custody(self):
        self.exercise("delayed")

    def test_persistent_private_consumers_fail_at_deadline_then_use_only_emergency_cleanup(self):
        self.exercise("persistent")

    def test_original_component_hash_guard_still_runs_after_natural_exit(self):
        self.exercise("changed-component")

    def test_wait_does_not_create_a_missing_shutdown_proof(self):
        self.exercise("missing-proof")


if __name__ == "__main__": unittest.main()
