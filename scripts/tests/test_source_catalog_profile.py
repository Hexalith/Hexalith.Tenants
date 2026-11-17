"""Exercise real receipt/component handling with captured Docker boundary shapes."""
import contextlib
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest import mock


REPOSITORY = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location("source_catalog_profile", REPOSITORY / "scripts/prepare-source-catalog-profile.py")
profile = importlib.util.module_from_spec(spec)
spec.loader.exec_module(profile)


class SourceCatalogProfileProvisioningTests(unittest.TestCase):
    def exercise(self, scenario, *, database="1", worker="false"):
        with tempfile.TemporaryDirectory(prefix="tenants-source-material-test-") as temporary:
            root = Path(temporary)
            shutil.copytree(REPOSITORY / "src/Hexalith.Tenants.AppHost/DaprComponents",
                            root / "src/Hexalith.Tenants.AppHost/DaprComponents")
            package_path = root / "feed/hexalith.eventstore.aspire/5.1.1"
            package_path.mkdir(parents=True)
            (package_path / "hexalith.eventstore.aspire.nuspec").write_text(
                '<package><metadata><repository url="https://github.com/Hexalith/Hexalith.EventStore" commit="' + 'f' * 40 + '" /></metadata></package>')
            assets_path = root / "src/Hexalith.Tenants.AppHost/obj/project.assets.json"
            assets_path.parent.mkdir()
            assets = {"libraries": {"Hexalith.EventStore.Aspire/5.1.1": {"type": "package", "sha512": "fixture-hash",
                      "path": "hexalith.eventstore.aspire/5.1.1"}}, "packageFolders": {str(root / "feed"): {}}}
            if scenario == "producer-source": assets["libraries"]["Hexalith.EventStore.Aspire/5.1.1"]["type"] = "project"
            if scenario == "producer-mismatch": assets["libraries"] = {}
            if scenario == "producer-missing-revision":
                (package_path / "hexalith.eventstore.aspire.nuspec").write_text('<package><metadata><repository /></metadata></package>')
            assets_path.write_text(json.dumps(assets))
            environment = root / "environment"
            environment.write_text("preserved=true\n", encoding="utf-8")
            output = root / "evidence.json"
            receipt_path = root / "hexalith-dapr-initialization.json"
            mount = {"Type": "volume", "Name": "owned-volume", "Source": "/var/lib/docker/volumes/owned-volume/_data",
                     "Destination": "/data", "RW": True}
            backend = {
                "Id": "a" * 64, "Name": "/dapr_redis", "Image": "sha256:" + "b" * 64,
                "Config": {"Image": "redis:7"}, "Created": "2026-10-31T21:00:02.123Z", "State": {"Running": True},
                "Mounts": [copy.deepcopy(mount)],
                "NetworkSettings": {"Ports": {"6379/tcp": [{"HostPort": "6379", "HostIp": "127.0.0.1"}]}},
            }
            volume = {"Name": "owned-volume", "Driver": "local", "Scope": "local", "Mountpoint": mount["Source"],
                      "CreatedAt": "2026-10-31T21:00:02Z"}
            identity = {"repository": "Hexalith/Hexalith.Tenants", "runId": "42", "runAttempt": "2", "job": "integration"}
            receipt = {"schema": "hexalith.dapr-initialization.v1", "status": "complete", "identity": identity,
                       "runtimeVersion": "1.19.1", "startedAt": "2026-10-31T21:00:01.900+00:00",
                       "completedAt": "2026-10-31T21:00:05+00:00", "preexistingVolumes": ["retained-volume"],
                       "preexistingContainerIds": ["e" * 64],
                       "createdContainers": [{"Id": backend["Id"], "Name": backend["Name"], "Image": backend["Config"]["Image"],
                                              "Created": backend["Created"], "Running": True, "Mounts": [mount]}],
                       "mountedVolumes": [copy.deepcopy(volume)]}
            if scenario == "old-container": backend["Created"] = "2026-10-31T20:00:00Z"
            if scenario == "substituted-container": backend["Id"] = "f" * 64
            if scenario == "stopped": backend["State"]["Running"] = False
            if scenario == "retained-volume": receipt["preexistingVolumes"].append("owned-volume")
            if scenario == "old-volume": volume["CreatedAt"] = "2026-10-31T20:00:00Z"
            if scenario == "remote-volume": volume["Driver"] = "remote"
            if scenario == "bind-mount": backend["Mounts"][0]["Type"] = "bind"
            if scenario == "changed-mount": backend["Mounts"][0]["Source"] += "-substitution"
            if scenario == "wrong-port": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostPort"] = "6380"
            if scenario == "ipv6-only": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostIp"] = "::"
            if scenario == "wrong-loopback": backend["NetworkSettings"]["Ports"]["6379/tcp"][0]["HostIp"] = "127.0.0.2"
            if scenario == "incomplete-receipt": receipt["status"] = "initializing"
            if scenario == "mismatched-receipt": receipt["identity"] = {**identity, "runAttempt": "1"}
            if scenario == "missing-created-redis": receipt["createdContainers"] = []
            if scenario == "preexisting-container": receipt["preexistingContainerIds"].append(backend["Id"])
            if scenario == "non-utc": receipt["startedAt"] = "2026-10-31T21:00:01"
            if scenario == "reversed-time": receipt["completedAt"] = "2026-10-31T21:00:00Z"
            profile.write_exclusive(receipt_path, receipt)
            if scenario == "malformed-receipt": receipt_path.write_text('{"status":"complete","status":"complete"}')
            if scenario == "public-receipt": receipt_path.chmod(0o644)
            outside = root / "outside"
            outside.write_bytes(b"outside-evidence-unchanged")
            if scenario == "receipt-link":
                receipt_path.unlink()
                receipt_path.symlink_to(outside)
            if scenario == "existing-evidence": output.write_bytes(b"previous-evidence")
            if scenario == "evidence-link": output.symlink_to(outside)
            if scenario == "unfinished-profile": profile.write_exclusive(root / "tenants-catalog-active.json", {"retained": True})
            if scenario == "used-database": profile.write_exclusive(root / f"tenants-catalog-database-{database}.json", {"retained": True})
            if scenario == "used-profile": profile.write_exclusive(root / "tenants-catalog-profile-rest.json", {"retained": True})
            source = root / "src/Hexalith.Tenants.AppHost/DaprComponents/statestore.yaml"
            if scenario in ("duplicate-db", "noncanonical-db", "commented-db", "quoted-db", "changed-host"):
                text = source.read_text()
                if scenario == "changed-host": text = text.replace("localhost:6379", "localhost:6380")
                elif scenario == "commented-db": text += '\n    - name: redisDB # still a database selector\n      value: "5"\n'
                elif scenario == "quoted-db": text += '\n    - "name": "redisDB"\n      value: "5"\n'
                else: text += '\n    - name: redisDB\n      value: "01"\n'
                source.write_text(text)
            settings = {variable: identity[key] for key, variable in profile.IDENTITY.items()}
            settings.update(GITHUB_ACTIONS="true", RUNNER_TEMP=str(root), GITHUB_ENV=str(environment),
                            HEXALITH_DAPR_INITIALIZATION_RECEIPT=str(receipt_path), TENANTS_SOURCE_SDK_PRODUCER_VERSION="5.1.1")
            if scenario == "wrong-job": settings["GITHUB_JOB"] = "invalid/job"
            if scenario == "different-job": settings["GITHUB_JOB"] = "another_shared_job"
            if scenario == "no-producer": settings.pop("TENANTS_SOURCE_SDK_PRODUCER_VERSION")
            calls = []

            def command(*arguments):
                calls.append(arguments)
                if arguments == ("git", "rev-parse", "--show-toplevel"): return str(root)
                if arguments[:4] == ("git", "config", "--file", ".gitmodules"):
                    return "references/" + arguments[-1].split(".", 1)[1].removesuffix(".path")
                if arguments[:2] == ("git", "rev-parse"): return "c" * 40
                if arguments[:2] == ("git", "-C"): return "d" * 40 if scenario == "source-drift" else "c" * 40
                if arguments == ("docker", "inspect", "dapr_redis"): return json.dumps([copy.deepcopy(backend)])
                if arguments[:3] == ("docker", "volume", "inspect"): return json.dumps([copy.deepcopy(volume)])
                if arguments == ("docker", "exec", backend["Id"], "redis-cli", "-n", database, "DBSIZE"):
                    return "1" if scenario == "nonempty" else "0"
                if arguments == ("docker", "exec", backend["Id"], "redis-cli", "--raw", "INFO", "server"):
                    return "run_id:" + ("substituted" if scenario == "server-identity-invalid" else "e" * 40)
                raise AssertionError("Unexpected boundary command: " + repr(arguments))

            with mock.patch.dict(os.environ, settings, clear=True), mock.patch.object(profile, "command", command), \
                    mock.patch("sys.argv", ["prepare", "--sdk-source-sha", "c" * 40, "--profile", "rest",
                                            "--redis-db", database, "--worker-backed-responses", worker, "--output", str(output)]), \
                    contextlib.redirect_stdout(io.StringIO()) as diagnostics:
                if scenario in ("fresh", "finish", "shutdown-missing", "shutdown-running", "shutdown-container", "shutdown-changed-component"):
                    profile.main()
                    evidence = json.loads(output.read_text(encoding="utf-8"))
                    self.assertTrue(evidence["materialProvisionedWithFreshBackend"])
                    self.assertEqual(evidence["backend"]["initialDatabaseSize"], 0)
                    self.assertEqual(evidence["sdkProducerVersion"], "5.1.1")
                    self.assertEqual(evidence["sdkPackageProducer"]["revision"], "f" * 40)
                    self.assertEqual(evidence["database"], int(database))
                    self.assertEqual(evidence["initializationSha256"], profile.digest(receipt_path))
                    self.assertNotIn("DigestKeys", output.read_text(encoding="utf-8"))
                    values = dict(line.split("=", 1) for line in environment.read_text().splitlines())
                    self.assertEqual(values["TENANTS_SOURCE_WORKER_BACKED_RESPONSES_ENABLED"], worker)
                    self.assertEqual(values["TENANTS_SOURCE_CATALOG_REDIS_DATABASE"], database)
                    self.assertEqual(diagnostics.getvalue().count("::add-mask::"), 2)
                    self.assertEqual(set(evidence["components"]), {path.name for path in source.parent.glob("*.yaml")} | {"secrets.json"})
                    for name in ("statestore.yaml", "pubsub.yaml", "configstore.yaml"):
                        component = Path(evidence["privateDirectory"]) / "components" / name
                        self.assertEqual(profile.redis_metadata(component.read_text(), "redisDB"), database)
                        self.assertEqual(profile.digest(component), evidence["components"][name])
                    self.assertIsNone(profile.redis_metadata(source.read_text(), "redisDB"))
                    private_components = Path(evidence["privateDirectory"]) / "components"
                    self.assertEqual((private_components / "secrets.json").read_bytes(), (source.parent / "secrets.json").read_bytes())
                    self.assertIn(str(private_components / "secrets.json"), (private_components / "secretstore.memories.yaml").read_text())
                    if scenario != "fresh":
                        private = Path(evidence["privateDirectory"])
                        if scenario != "shutdown-missing":
                            profile.write_exclusive(private / "shutdown.json", {"provisioningSha256": profile.digest(output), "disposed": True})
                        if scenario == "shutdown-changed-component":
                            (private / "components/statestore.yaml").write_text("substituted")
                        with mock.patch.object(profile, "active_processes", return_value=[42] if scenario == "shutdown-running" else []), \
                                mock.patch.object(profile, "private_containers", return_value=["d" * 64] if scenario == "shutdown-container" else []):
                            if scenario == "finish":
                                profile.finish(output, root, identity)
                                self.assertFalse(private.exists())
                                self.assertFalse((root / "tenants-catalog-active.json").exists())
                                self.assertTrue((root / f"tenants-catalog-database-{database}.json").exists())
                            else:
                                with self.assertRaises((ValueError, FileNotFoundError)):
                                    profile.finish(output, root, identity)
                                self.assertTrue(private.exists())
                                self.assertTrue((root / "tenants-catalog-active.json").exists())
                else:
                    with self.assertRaises(ValueError): profile.main()
                    self.assertEqual(environment.read_text(encoding="utf-8"), "preserved=true\n")
                    self.assertEqual(diagnostics.getvalue(), "")
                    self.assertEqual(list(root.glob("tenants-source-catalog-*")), [])
                    if scenario == "existing-evidence": self.assertEqual(output.read_bytes(), b"previous-evidence")
                    elif scenario != "evidence-link": self.assertFalse(output.exists())
                self.assertEqual(outside.read_bytes(), b"outside-evidence-unchanged")
                self.assertFalse(any("FLUSH" in argument for call in calls for argument in call))

    def test_shared_receipt_provisions_a_private_complete_component_set_for_each_worker_profile(self):
        for database, worker in (("1", "false"), ("5", "true"), ("15", "false")):
            with self.subTest(database=database, worker=worker): self.exercise("fresh", database=database, worker=worker)

    def test_reserved_noncanonical_and_out_of_range_databases_refuse_before_minting(self):
        for database in ("0", "01", "-1", "16", "1.0", "1,2", " 1", "1\n"):
            with self.subTest(database=database): self.exercise("invalid-db", database=database)

    def test_refusals_preserve_existing_evidence_and_do_not_mint_owner_material(self):
        for scenario in ("old-container", "substituted-container", "stopped", "retained-volume", "old-volume", "remote-volume",
                         "bind-mount", "changed-mount", "wrong-port", "wrong-loopback", "ipv6-only", "nonempty", "source-drift",
                         "existing-evidence", "evidence-link", "wrong-job", "different-job", "no-producer", "incomplete-receipt",
                         "mismatched-receipt", "missing-created-redis", "preexisting-container", "non-utc", "reversed-time",
                         "malformed-receipt", "public-receipt", "receipt-link", "unfinished-profile", "used-database",
                         "used-profile", "duplicate-db", "noncanonical-db", "commented-db", "quoted-db", "changed-host", "producer-source", "producer-mismatch", "producer-missing-revision", "server-identity-invalid"):
            with self.subTest(scenario=scenario): self.exercise(scenario)

    def test_private_container_inventory_binds_mounts_and_preserves_foreign_resources(self):
        records = [{"Id": "a" * 64, "Mounts": [{"Source": "/job/private/certificates"}]},
                   {"Id": "b" * 64, "Mounts": [{"Source": "/job/private-foreign/certificates"}]}]
        with mock.patch.object(profile, "command", side_effect=["a" * 64 + "\n" + "b" * 64, json.dumps(records)]):
            self.assertEqual(profile.private_containers(Path("/job/private")), ["a" * 64])

    def test_awaited_shutdown_releases_only_active_profile_and_private_material(self):
        self.exercise("finish")

    def test_cleanup_refuses_root_ancestors_siblings_nested_and_linked_directories(self):
        with tempfile.TemporaryDirectory() as directory:
            temporary = Path(directory)
            own = temporary / "tenants-source-catalog-owned"
            own.mkdir(mode=0o700)
            sibling = temporary / "foreign"
            sibling.mkdir()
            nested = own / "tenants-source-catalog-nested"
            nested.mkdir(mode=0o700)
            link = temporary / "tenants-source-catalog-link"
            link.symlink_to(own, target_is_directory=True)
            self.assertEqual(profile.private_directory(own, temporary), own)
            receipt = temporary / "receipt.json"
            marker = sibling / "preserve"
            marker.write_bytes(b"foreign material")
            for candidate in (temporary, temporary.parent, sibling, nested, link,
                              temporary / "tenants-source-catalog-owned/../foreign", Path("relative")):
                with self.subTest(path=candidate):
                    receipt.unlink(missing_ok=True)
                    profile.write_exclusive(receipt, {"privateDirectory": str(candidate)})
                    with mock.patch.object(profile.shutil, "rmtree") as remove:
                        with self.assertRaises(ValueError): profile.finish(receipt, temporary, {})
                        remove.assert_not_called()
                    self.assertEqual(marker.read_bytes(), b"foreign material")
                    self.assertTrue(own.is_dir())

    def test_incomplete_shutdown_and_changed_components_block_the_next_profile(self):
        for scenario in ("shutdown-missing", "shutdown-running", "shutdown-container", "shutdown-changed-component"):
            with self.subTest(scenario=scenario): self.exercise(scenario)


if __name__ == "__main__": unittest.main()
