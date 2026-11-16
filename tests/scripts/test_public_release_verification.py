"""Finite controls for optimized release gates and exact artifact producer evidence."""

import ast
import os
from pathlib import Path
import runpy
import subprocess
import sys
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / '_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/verify-public-release.py'


class PublicReleaseVerificationTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.module = runpy.run_path(str(VERIFIER))

    def inputs(self):
        run = {'id': 123, 'run_attempt': 2, 'head_sha': 'a' * 40}
        artifact = {'name': 'release-candidate', 'created_at': '2026-11-16T01:02:00Z',
                    'updated_at': '2026-11-16T01:03:00Z'}
        job = {'id': 456, 'name': 'Prepare release artifacts without publication credentials',
               'run_id': 123, 'head_sha': 'a' * 40, 'status': 'completed', 'conclusion': 'success',
               'started_at': '2026-11-16T01:00:00Z', 'completed_at': '2026-11-16T01:04:00Z'}
        return artifact, run, job

    def test_generic_artifact_binds_successful_earlier_attempt_instead_of_latest(self):
        artifact, run, job = self.inputs()
        requests = []
        def fetch(path):
            requests.append(path)
            return {'jobs': [job] if '/attempts/1/' in path else []}
        result = self.module['producer_attempt'](artifact, run, fetch, job['name'], 'Tenants')
        self.assertEqual(result['attempt'], 1)
        self.assertEqual(result['jobId'], 456)
        self.assertEqual(result['artifactCreatedAt'], artifact['created_at'])
        self.assertEqual(result['jobStartedAt'], job['started_at'])
        self.assertEqual(len(result['jobsEvidenceSha256']), 64)
        self.assertEqual(len(requests), 2)
        self.assertIn('/attempts/1/', requests[0])
        self.assertIn('/attempts/2/', requests[1])

    def test_missing_or_ambiguous_producer_is_refused(self):
        artifact, run, job = self.inputs()
        for jobs in ([], [job]):
            with self.subTest(jobs=len(jobs)), self.assertRaisesRegex(ValueError, 'Missing or ambiguous'):
                self.module['producer_attempt'](artifact, run, lambda path: {'jobs': jobs}, job['name'], 'Tenants')

    def test_producer_requires_success_source_attempt_and_time_evidence(self):
        artifact, run, job = self.inputs()
        for changes in ({'conclusion': 'failure'}, {'head_sha': 'b' * 40}, {'run_id': 124},
                        {'run_attempt': 2}, {'started_at': None},
                        {'started_at': '2026-11-16T01:02:30Z'}, {'completed_at': '2026-11-16T01:02:30Z'}):
            changed = {**job, **changes}
            def fetch(path):
                return {'jobs': [changed] if '/attempts/1/' in path else []}
            with self.subTest(changes=changes), self.assertRaises(ValueError):
                self.module['producer_attempt'](artifact, run, fetch, job['name'], 'Tenants')
        for timestamp in (None, 'not-a-timestamp', '2026-11-16T00:00:00Z'):
            changed = {**artifact, 'updated_at': timestamp}
            with self.subTest(timestamp=timestamp), self.assertRaises(ValueError):
                self.module['producer_attempt'](changed, run, lambda path: {'jobs': [job]}, job['name'], 'Tenants')

    def test_frontcomposer_requires_its_scoped_name_and_exact_attempt(self):
        artifact, run, job = self.inputs()
        with self.assertRaisesRegex(ValueError, 'exact run/attempt name'):
            self.module['producer_attempt'](artifact, run, lambda path: {'jobs': [job]}, job['name'], 'FrontComposer')
        artifact['name'] = 'release-candidate-123-1'
        requested = []
        def fetch(path):
            requested.append(path)
            return {'jobs': [job]}
        self.assertEqual(self.module['producer_attempt'](artifact, run, fetch, job['name'], 'FrontComposer')['attempt'], 1)
        self.assertEqual(requested, ['actions/runs/123/attempts/1/jobs?per_page=100&page=1'])
        for name in ('release-candidate-124-1', 'release-candidate-123-3'):
            artifact['name'] = name
            with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'different run/attempt'):
                self.module['producer_attempt'](artifact, run, fetch, job['name'], 'FrontComposer')

    def test_source_workflow_supplies_the_real_static_prepare_job_name(self):
        self.assertEqual(self.module['prepare_job_name']((ROOT / '.github/workflows/release.yml').read_text()),
                         'Prepare release artifacts without publication credentials')
        self.assertEqual(self.module['prepare_job_name']('jobs:\n  prepare-candidate:\n    runs-on: ubuntu-latest\n', 'prepare-candidate'),
                         'prepare-candidate')
        for workflow in ('jobs:\n  other:\n', 'jobs:\n  prepare-release:\n    name: ${{ matrix.job }}\n'):
            with self.subTest(workflow=workflow), self.assertRaises(ValueError):
                self.module['prepare_job_name'](workflow)

    def test_optimized_interpreter_still_refuses_invalid_release_and_source(self):
        for version, source in (('invalid', 'a' * 40), ('1.2.3', 'invalid')):
            with self.subTest(version=version, source=source), tempfile.TemporaryDirectory() as temporary:
                result = subprocess.run([sys.executable, '-O', str(VERIFIER), '--owner', 'Tenants',
                    '--version', version, '--source', source, '--artifact-id', '1',
                    '--artifact-zip', str(Path(temporary) / 'missing.zip'), '--output', str(Path(temporary) / 'output')],
                    text=True, capture_output=True, env={**os.environ, 'TMPDIR': temporary})
                self.assertNotEqual(result.returncode, 0)
                self.assertIn('ValueError: Release verification failed:', result.stderr)
                self.assertNotIn('CalledProcessError', result.stderr)
                self.assertFalse((Path(temporary) / 'output').exists())

    def test_optimized_interpreter_preserves_existing_receipt_and_explicit_gates(self):
        self.assertFalse(any(isinstance(node, ast.Assert) for node in ast.walk(ast.parse(VERIFIER.read_text()))))
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / 'output'
            output.mkdir()
            receipt = output / 'verification.json'
            receipt.write_bytes(b'original successful receipt\n')
            result = subprocess.run([sys.executable, '-O', str(VERIFIER), '--owner', 'Tenants',
                '--version', '1.2.3', '--source', 'a' * 40, '--artifact-id', '1',
                '--artifact-zip', str(Path(temporary) / 'missing.zip'), '--output', str(output)],
                text=True, capture_output=True, env={**os.environ, 'TMPDIR': temporary})
            self.assertNotEqual(result.returncode, 0)
            self.assertIn('preserve earlier receipts', result.stderr)
            self.assertEqual(receipt.read_bytes(), b'original successful receipt\n')


if __name__ == '__main__':
    unittest.main()
