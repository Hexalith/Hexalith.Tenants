"""Hermetic source-provenance checks; no network, build, seed or measurement work."""

import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import patch


_REPO = Path(__file__).resolve().parents[2]
_HELPER = _REPO / "scripts/tenant-audit-source-provenance.py"
_RUNNER = _REPO / "scripts/run-tenant-audit-performance.sh"
_module_spec = importlib.util.spec_from_file_location("tenant_audit_provenance", _HELPER)
_guard = importlib.util.module_from_spec(_module_spec)
_module_spec.loader.exec_module(_guard)


def git(root, *args):
    return subprocess.run(["git", "-C", str(root), *args], check=True, text=True,
                          stdout=subprocess.PIPE, stderr=subprocess.PIPE).stdout.strip()


class SourceProvenanceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        base = Path(self.temp.name)
        self.root = base / "source"
        dependency = base / "dependency"
        for repo in (self.root, dependency):
            repo.mkdir()
            git(repo, "init", "-q")
            git(repo, "config", "user.name", "Fixture")
            git(repo, "config", "user.email", "fixture@example.invalid")
            (repo / "source.txt").write_text("original\n")
            git(repo, "add", ".")
            git(repo, "commit", "-qm", "test: create provenance fixture")
        git(self.root, "-c", "protocol.file.allow=always", "submodule", "add", "-q",
            str(dependency), "references/Dependency")
        scripts = self.root / "scripts"
        scripts.mkdir()
        shutil.copy2(_HELPER, scripts)
        shutil.copy2(_RUNNER, scripts)
        git(self.root, "add", ".")
        git(self.root, "commit", "-qm", "test: add fixture dependency")
        self.dependency = self.root / "references/Dependency"
        self.output = base / "results"

    def run_helper(self):
        return subprocess.run(["python3", str(_HELPER), str(self.root)], text=True,
                              stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    def assert_refused_before_runner_work(self):
        result = self.run_helper()
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(result.stdout, "", "rejected sources must not emit revision-bound evidence")
        env = dict(os.environ, AUDIT_PERF_RESULT_DIR=str(self.output), AUDIT_PERF_SMOKE="1")
        runner = subprocess.run(["bash", str(self.root / "scripts" / _RUNNER.name)],
                                env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.assertNotEqual(runner.returncode, 0)
        self.assertIn("Performance source preflight refused", runner.stderr)
        self.assertFalse(self.output.exists(), "preflight must precede preparation, results and setup")

    def test_clean_checkouts_record_canonical_root_and_dependency_commits(self):
        result = self.run_helper()
        self.assertEqual(result.returncode, 0, result.stderr)
        proof = json.loads(result.stdout)
        self.assertEqual(proof, {"rootRevision": git(self.root, "rev-parse", "HEAD^{commit}"),
                                 "dependencies": {"references/Dependency": git(self.dependency, "rev-parse", "HEAD^{commit}")}})
        self.assertEqual(len(proof["rootRevision"]), 40)

    def test_clean_detached_dependency_is_supported(self):
        git(self.dependency, "checkout", "--detach", "-q")
        self.assertEqual(self.run_helper().returncode, 0)

    def test_dirty_tracked_root_is_rejected_before_work(self):
        (self.root / "source.txt").write_text("changed\n")
        self.assert_refused_before_runner_work()

    def test_untracked_root_is_rejected_before_work(self):
        (self.root / "untracked.txt").write_text("new\n")
        self.assert_refused_before_runner_work()

    def test_staged_root_is_rejected_before_work(self):
        (self.root / "source.txt").write_text("staged\n")
        git(self.root, "add", "source.txt")
        self.assert_refused_before_runner_work()

    def test_dirty_tracked_dependency_is_rejected_before_work(self):
        (self.dependency / "source.txt").write_text("changed\n")
        self.assert_refused_before_runner_work()

    def test_untracked_dependency_is_rejected_before_work(self):
        (self.dependency / "untracked.txt").write_text("new\n")
        self.assert_refused_before_runner_work()

    def test_staged_dependency_is_rejected_before_work(self):
        (self.dependency / "source.txt").write_text("staged\n")
        git(self.dependency, "add", "source.txt")
        self.assert_refused_before_runner_work()

    def test_mismatched_clean_dependency_is_rejected_before_work(self):
        git(self.dependency, "commit", "--allow-empty", "-qm", "test: advance fixture dependency")
        self.assert_refused_before_runner_work()

    def test_assume_unchanged_root_source_is_rejected_before_work(self):
        git(self.root, "update-index", "--assume-unchanged", "source.txt")
        (self.root / "source.txt").write_text("hidden root edit\n")
        self.assert_refused_before_runner_work()

    def test_skip_worktree_root_source_is_rejected_before_work(self):
        git(self.root, "update-index", "--skip-worktree", "source.txt")
        (self.root / "source.txt").write_text("hidden root edit\n")
        self.assert_refused_before_runner_work()

    def test_assume_unchanged_dependency_source_is_rejected_before_work(self):
        git(self.dependency, "update-index", "--assume-unchanged", "source.txt")
        (self.dependency / "source.txt").write_text("hidden dependency edit\n")
        self.assert_refused_before_runner_work()

    def test_skip_worktree_dependency_source_is_rejected_before_work(self):
        git(self.dependency, "update-index", "--skip-worktree", "source.txt")
        (self.dependency / "source.txt").write_text("hidden dependency edit\n")
        self.assert_refused_before_runner_work()

    def assert_late_dependency_change_rejected(self, mutate):
        original_git = _guard.git
        mutated = False

        def inspect_then_change(root, *arguments):
            nonlocal mutated
            result = original_git(root, *arguments)
            if root == self.dependency and arguments[0] == "status" and not mutated:
                mutated = True
                mutate()
            return result

        with patch.object(_guard, "git", side_effect=inspect_then_change):
            with self.assertRaises(ValueError):
                _guard.provenance(self.root)
        self.assertTrue(mutated, "control must change a dependency after its first inspection")

    def test_dependency_revision_is_rechecked_before_output(self):
        self.assert_late_dependency_change_rejected(lambda: git(self.dependency, "commit", "--allow-empty",
            "-qm", "test: advance fixture dependency"))

    def test_dependency_tracked_dirtiness_is_rechecked_before_output(self):
        self.assert_late_dependency_change_rejected(lambda: (self.dependency / "source.txt").write_text("late edit\n"))

    def test_dependency_untracked_dirtiness_is_rechecked_before_output(self):
        self.assert_late_dependency_change_rejected(lambda: (self.dependency / "late.txt").write_text("late source\n"))

    def test_dependency_index_flags_are_rechecked_before_output(self):
        self.assert_late_dependency_change_rejected(lambda: git(self.dependency, "update-index", "--skip-worktree", "source.txt"))

    def test_clean_runner_records_exact_helper_proof_before_build_or_measurement(self):
        self.assertIsNotNone(shutil.which("jq"), "runner fixture requires the runner's jq prerequisite")
        proof_result = self.run_helper()
        self.assertEqual(proof_result.returncode, 0, proof_result.stderr)
        proof = json.loads(proof_result.stdout)
        tool_dir = Path(self.temp.name) / "controlled-tools"
        tool_dir.mkdir()
        calls = Path(self.temp.name) / "tool-calls.txt"
        stubs = {
            "node": r'''printf 'node %s\n' "$*" >> "$AUDIT_PERF_TEST_CALLS"
case "${1:-}" in
    --version) printf 'v26.0.0\n' ;;
    -p) printf '1.63.0\n' ;;
    *) printf '{"mode":"baseline","uiPageSize":50}\n' ;;
esac
''',
            "aspire": r'''printf 'aspire %s\n' "$*" >> "$AUDIT_PERF_TEST_CALLS"
case "$*" in
    'ps --format Json') printf '[]\n' ;;
    '--version') printf '13.6.1\n' ;;
    'certs trust --non-interactive') exit 73 ;;
    *) exit 92 ;;
esac
''',
            "dotnet": r'''printf 'dotnet %s\n' "$*" >> "$AUDIT_PERF_TEST_CALLS"
if [[ "${1:-}" == --version ]]; then printf '10.0.402\n'; else exit 93; fi
''',
            "dapr": "printf '1.18.0\n'\n",
            "sleep": "exit 0\n",
            "npm": r'''printf 'npm %s\n' "$*" >> "$AUDIT_PERF_TEST_CALLS"
exit 94
''',
        }
        for name, body in stubs.items():
            script = tool_dir / name
            script.write_text("#!/usr/bin/env bash\nset -euo pipefail\n" + body)
            script.chmod(0o755)
        env = dict(os.environ, PATH=str(tool_dir) + os.pathsep + os.environ["PATH"],
                   AUDIT_PERF_RESULT_DIR=str(self.output), AUDIT_PERF_SMOKE="1", AUDIT_PERF_TEST_CALLS=str(calls))

        result = subprocess.run(["bash", str(self.root / "scripts" / _RUNNER.name)], env=env,
                                text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        self.assertEqual(result.returncode, 73, result.stderr)
        environment = json.loads((self.output / "environment.json").read_text())
        self.assertEqual(environment["sourceProvenance"], proof)
        self.assertEqual(environment["gitRevision"], proof["rootRevision"])
        recorded = calls.read_text().splitlines()
        self.assertIn("aspire certs trust --non-interactive", recorded)
        self.assertEqual([call for call in recorded if call.startswith("dotnet ")], ["dotnet --version"])
        self.assertFalse(any(call.startswith("npm ") for call in recorded))

    def test_missing_dependency_is_rejected_before_work(self):
        git(self.root, "submodule", "deinit", "-f", "--", "references/Dependency")
        self.assert_refused_before_runner_work()


if __name__ == "__main__":
    unittest.main()
