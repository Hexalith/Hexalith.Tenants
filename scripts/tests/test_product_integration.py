"""Exercise real private-topology cleanup without starting Dapr or Aspire."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
from unittest import mock


ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location("product_integration", ROOT / "scripts/run-product-integration.py")
PRODUCT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PRODUCT)


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
                                temporary=temporary, output=output, producer="5.1.1", source="a" * 40)
                        stop.assert_not_called()
                        remove.assert_not_called()
                    self.assertEqual(marker.read_bytes(), b"foreign material")


if __name__ == "__main__": unittest.main()
