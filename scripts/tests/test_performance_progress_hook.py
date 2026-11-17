"""Exercise the product benchmark launch/progress/cleanup boundary without .NET or Dapr."""
import json
import os
from pathlib import Path
import subprocess
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "run-product-nightly.sh"


class PerformanceProgressHookTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory(prefix="performance-hook-control-")
        self.addCleanup(self.scratch.cleanup)
        self.root = Path(self.scratch.name).resolve()
        self.bin = self.root / "bin"
        self.bin.mkdir()
        dotnet = self.bin / "dotnet"
        dotnet.write_text("""#!/usr/bin/env python3
import json, os, pathlib, sys
pathlib.Path(os.environ['CONTROL_CALL']).write_text(json.dumps({
 'arguments': sys.argv[1:],
 'eventstore': os.environ.get('HEXALITH_EVENTSTORE_RUN_PERFORMANCE_TESTS'),
 'tenants': os.environ.get('HEXALITH_TENANTS_RUN_PERFORMANCE_TESTS'),
 'directory': os.environ.get('HEXALITH_TENANTS_PERFORMANCE_PROGRESS_DIRECTORY')}))
sys.exit(int(os.environ.get('CONTROL_EXIT', '0')))
""")
        dotnet.chmod(0o700)
        self.output = self.root / "output"
        self.call = self.root / "call.json"
        self.environment = {**os.environ, "RUNNER_TEMP": str(self.root), "GITHUB_OUTPUT": str(self.output),
                            "CONTROL_CALL": str(self.call), "PATH": str(self.bin) + os.pathsep + os.environ["PATH"]}

    def run_script(self, *arguments):
        return subprocess.run(["bash", str(SCRIPT), *arguments], cwd=self.root, env=self.environment,
                              text=True, capture_output=True, timeout=10, check=False)

    def launch(self, exit_code=0):
        self.environment["CONTROL_EXIT"] = str(exit_code)
        result = self.run_script("daily")
        self.assertEqual(result.returncode, exit_code, result.stderr)
        outputs = dict(line.split("=", 1) for line in self.output.read_text().splitlines())
        return result, outputs, Path(outputs["progress_directory"])

    def test_real_hook_preserves_exact_workload_invocation_and_both_opt_ins(self):
        _, outputs, directory = self.launch()
        call = json.loads(self.call.read_text())
        self.assertEqual(call, {"arguments": ["test", "--project", "tests/Hexalith.Tenants.IntegrationTests/Hexalith.Tenants.IntegrationTests.csproj",
                         "--configuration", "Release", "--no-build", "--no-restore", "--stop-on-fail", "off",
                         "--filter-trait", "Category=Performance", "--minimum-expected-tests", "1", "--fail-skips", "on",
                         "--results-directory", "TestResults/performance", "--report-xunit-trx", "--report-xunit-trx-filename", "perf-results.trx"],
                         "eventstore": "1", "tenants": "1", "directory": str(directory)})
        self.assertEqual(outputs["progress_path"], str(directory / "progress.json"))
        self.assertEqual(directory.stat().st_mode & 0o777, 0o700)
        self.assertEqual((directory / "progress.json").stat().st_mode & 0o777, 0o600)
        self.assertEqual((directory / "owner").stat().st_mode & 0o777, 0o600)

    def test_failed_process_retains_incomplete_launch_observation_and_original_exit(self):
        _, _, directory = self.launch(23)
        self.assertEqual(json.loads((directory / "progress.json").read_text()),
                         {"schemaVersion": 1, "phase": "RunnerStarting", "status": "incomplete"})
        self.assertEqual(self.run_script("cleanup", str(directory)).returncode, 0)
        self.assertFalse(directory.exists())

    def test_cleanup_removes_only_its_private_owned_directory(self):
        foreign = self.root / "foreign"
        foreign.mkdir()
        sentinel = foreign / "credential-sentinel"
        sentinel.write_text("preserve")
        _, _, directory = self.launch()
        self.assertEqual(self.run_script("cleanup", str(directory)).returncode, 0)
        self.assertFalse(directory.exists())
        self.assertEqual(sentinel.read_text(), "preserve")

    def test_cleanup_refuses_root_ancestor_sibling_link_and_foreign_owner(self):
        for mutation in ("root", "ancestor", "sibling", "link", "public", "owner", "missing-owner"):
            with self.subTest(mutation=mutation):
                self.output.unlink(missing_ok=True)
                _, _, directory = self.launch()
                target = directory
                if mutation == "root": target = self.root
                elif mutation == "ancestor": target = self.root.parent
                elif mutation == "sibling": target = self.root / "foreign"
                elif mutation == "link":
                    target = self.root / "tenants-performance-progress-AbCd1234"
                    target.symlink_to(directory, target_is_directory=True)
                elif mutation == "public": directory.chmod(0o755)
                elif mutation == "owner": (directory / "owner").write_text("credential-sentinel")
                elif mutation == "missing-owner": (directory / "owner").unlink()
                result = self.run_script("cleanup", str(target))
                self.assertNotEqual(result.returncode, 0)
                self.assertIn("performance-progress-cleanup-invalid", result.stderr)
                self.assertNotIn("credential-sentinel", result.stdout + result.stderr)
                self.assertTrue(directory.exists())

    def test_invalid_cadence_and_noncanonical_root_do_not_launch_dotnet(self):
        self.assertNotEqual(self.run_script("weekly").returncode, 0)
        self.assertFalse(self.call.exists())
        self.environment["RUNNER_TEMP"] += "/.."
        self.assertNotEqual(self.run_script("daily").returncode, 0)
        self.assertFalse(self.call.exists())


if __name__ == "__main__":
    unittest.main()
