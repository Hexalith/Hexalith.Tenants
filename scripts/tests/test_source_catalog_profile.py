"""Provisioning boundary tests use captured Docker shapes; they do not start a topology."""
import contextlib
import copy
import importlib.util
import io
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest import mock


spec = importlib.util.spec_from_file_location("source_catalog_profile", Path(__file__).parents[1] / "prepare-source-catalog-profile.py")
profile = importlib.util.module_from_spec(spec)
spec.loader.exec_module(profile)


class SourceCatalogProfileProvisioningTests(unittest.TestCase):
    def exercise(self, scenario):
        with tempfile.TemporaryDirectory(prefix="tenants-source-material-test-") as temporary:
            root = Path(temporary)
            environment = root / "environment"
            environment.write_text("preserved=true\n", encoding="utf-8")
            output = root / "evidence.json"
            before = root / "before.json"
            before.write_text(json.dumps(["retained-volume"]), encoding="utf-8")
            backend = {
                "Id": "owned-container", "Image": "sha256:" + "a" * 64,
                "Created": "2026-10-31T21:00:02.123Z", "State": {"Running": True},
                "Mounts": [{"Type": "volume", "Name": "owned-volume"}],
                "NetworkSettings": {"Ports": {"6379/tcp": [{"HostPort": "6379", "HostIp": "127.0.0.1"}]}},
            }
            volume = {"Name": "owned-volume", "Driver": "local", "CreatedAt": "2026-10-31T21:00:02Z"}
            if scenario == "old-container": backend["Created"] = "2026-10-31T20:00:00Z"
            if scenario == "stopped": backend["State"]["Running"] = False
            if scenario == "retained-volume": backend["Mounts"][0]["Name"] = "retained-volume"
            if scenario == "old-volume": volume["CreatedAt"] = "2026-10-31T20:00:00Z"
            if scenario == "bind-mount": backend["Mounts"][0]["Type"] = "bind"
            if scenario == "wrong-port": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostPort"] = "6380"
            if scenario == "ipv6-only": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostIp"] = "::"
            if scenario == "wrong-loopback": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostIp"] = "127.0.0.2"
            outside = root / "outside"
            outside.write_bytes(b"outside-evidence-unchanged")
            if scenario == "existing-evidence": output.write_bytes(b"previous-evidence")
            if scenario == "evidence-link": output.symlink_to(outside)
            settings = {
                "GITHUB_ACTIONS": "true", "GITHUB_JOB": "development-catalog", "RUNNER_TEMP": str(root),
                "GITHUB_ENV": str(environment), "TENANTS_SOURCE_BACKEND_STARTED_AT": "2026-10-31T21:00:01.900+00:00",
                "TENANTS_SOURCE_BACKEND_BEFORE_PATH": str(before), "TENANTS_SOURCE_SDK_PRODUCER_VERSION": "3.128.4",
            }
            if scenario == "wrong-job": settings["GITHUB_JOB"] = "unowned"
            calls = []

            def command(*arguments):
                calls.append(arguments)
                if arguments == ("git", "rev-parse", "--show-toplevel"): return str(root)
                if arguments[:4] == ("git", "config", "--file", ".gitmodules"):
                    module = arguments[-1].split(".", 1)[1].removesuffix(".path")
                    return "references/" + module
                if arguments[:2] == ("git", "rev-parse"): return "b" * 40
                if arguments[:2] == ("git", "-C"): return "c" * 40 if scenario == "source-drift" else "b" * 40
                if arguments == ("docker", "inspect", "dapr_redis"): return json.dumps([copy.deepcopy(backend)])
                if arguments[:3] == ("docker", "volume", "inspect"): return json.dumps([copy.deepcopy(volume)])
                if arguments == ("docker", "exec", "owned-container", "redis-cli", "DBSIZE"):
                    return "1" if scenario == "nonempty" else "0"
                raise AssertionError("Unexpected boundary command: " + repr(arguments))

            with mock.patch.dict(os.environ, settings), mock.patch.object(profile, "command", command), \
                    mock.patch("sys.argv", ["prepare", "--sdk-source-sha", "b" * 40, "--output", str(output)]), \
                    contextlib.redirect_stdout(io.StringIO()) as diagnostics:
                if scenario == "fresh":
                    profile.main()
                    evidence = json.loads(output.read_text(encoding="utf-8"))
                    self.assertTrue(evidence["materialProvisionedWithFreshBackend"])
                    self.assertEqual(evidence["backend"]["initialDatabaseSize"], 0)
                    self.assertEqual(evidence["sdkProducerVersion"], "3.128.4")
                    self.assertNotIn("DigestKeys", output.read_text(encoding="utf-8"))
                    self.assertIn("Tenants__DevelopmentRouteCatalog__CustodyDirectory=", environment.read_text(encoding="utf-8"))
                    self.assertEqual(diagnostics.getvalue().count("::add-mask::"), 2)
                    log_settings = dict(line.split("=", 1) for line in environment.read_text(encoding="utf-8").splitlines()
                                        if line.startswith("Logging__"))
                    self.assertEqual(log_settings, {
                        "Logging__LogLevel__Hexalith.EventStore.ServiceDefaults.Routing.TechnicalHttpRouteCatalogReceiver": "Debug",
                        "Logging__LogLevel__Hexalith.EventStore.Server.Commands.CommandRouter": "Debug",
                        "Logging__LogLevel__Hexalith.EventStore.Server.Pipeline.SubmitCommandHandler": "Debug",
                    })
                else:
                    with self.assertRaises(ValueError): profile.main()
                    self.assertEqual(environment.read_text(encoding="utf-8"), "preserved=true\n")
                    self.assertEqual(diagnostics.getvalue(), "")
                    self.assertEqual(list(root.glob("tenants-source-catalog-*")), [])
                    if scenario == "existing-evidence": self.assertEqual(output.read_bytes(), b"previous-evidence")
                    elif scenario != "evidence-link": self.assertFalse(output.exists())
                self.assertEqual(outside.read_bytes(), b"outside-evidence-unchanged")

    def test_fresh_owned_backend_provisions_private_material_and_public_receipt(self):
        self.exercise("fresh")

    def test_refusals_preserve_existing_evidence_and_do_not_mint_owner_material(self):
        for scenario in ("old-container", "stopped", "retained-volume", "old-volume", "bind-mount", "wrong-port",
                         "wrong-loopback", "ipv6-only", "nonempty", "source-drift", "existing-evidence", "evidence-link", "wrong-job"):
            with self.subTest(scenario=scenario): self.exercise(scenario)


if __name__ == "__main__": unittest.main()
