#!/usr/bin/env python3
"""Execute the shipped publication admission and recovery bootstrap with immutable graphs."""
import json
import re
from pathlib import Path
import shutil
import subprocess
import sys
import unittest

from recovery_execution_fixture import ROOT, workflow_step
import test_partial_release_recovery as recovery

sys.path.insert(0, str(ROOT / 'references/Hexalith.Builds/Github/governed-provenance'))
import execution_identity

WORKFLOWS = ('ci.yml', 'story-guards.yml', 'source-reference.yml')
PUBLISHER_FILES = ('publish-containers.sh', 'oci_registry_validator.py', 'publication_preflight.py',
                   'smoke-container-platforms.sh', 'smoke_container_platforms.py')


class ReleaseWorkflowGovernanceTests(unittest.TestCase):
    def setUp(self):
        self.fixture = recovery.PartialReleaseRecoveryTests()
        self.fixture.actual_workflows = True
        self.fixture.setUp()
        self.addCleanup(self.fixture.doCleanups)
        self.root = self.fixture.root
        self.environment = self.fixture.environment.copy()
        self.environment.update(REPOSITORY='fixture/recovery', DISPATCH_SHA=self.fixture.source_sha)
        self.execution = self.root / '.hexalith/builds-execution'
        self.environment['HEXALITH_BUILDS_EXECUTION_ROOT'] = str(self.execution)
        self.installed = self.root / '.hexalith/release'
        self.installed.mkdir()
        for name in PUBLISHER_FILES:
            shutil.copyfile(self.execution / 'Github/publish-containers' / name, self.installed / name)
        provenance = (self.root / '.hexalith/release-workflow-provenance.json').read_bytes()
        (self.installed / 'governed').mkdir()
        self.prepared = self.installed / 'governed/workflow-provenance.json'
        self.prepared.write_bytes(provenance)
        self.current = self.root / '.hexalith/release-evidence/workflow-provenance.json'
        self.current.parent.mkdir()
        self.current.write_bytes(provenance)
        self.marker = self.root / 'publication-reached'
        self.admission = workflow_step(ROOT / '.github/workflows/release.yml',
                                       'Require the prepared workflow closure and latest source gates')

    def execute(self, script, accepted=True, environment=None):
        self.marker.unlink(missing_ok=True)
        result = subprocess.run(['bash', '-c', script + '\nprintf publication > publication-reached\n'],
                                cwd=self.root, env=environment or self.environment, capture_output=True, text=True, timeout=20)
        self.assertEqual(result.returncode == 0, accepted, result.stdout + result.stderr)
        self.assertEqual(self.marker.exists(), accepted, result.stdout + result.stderr)
        return result

    def gate(self, workflow, status, conclusion):
        row = dict(id=WORKFLOWS.index(workflow) + 1, run_number=2, run_attempt=2,
                   head_sha=self.fixture.source_sha, head_branch='main', event='push',
                   path='.github/workflows/' + workflow, status=status, conclusion=conclusion)
        old = {**row, 'id': row['id'] + 100, 'run_number': 1, 'run_attempt': 1, 'status': 'completed', 'conclusion': 'success'}
        self.fixture.destinations['gate_runs'] = {workflow: [old, row]}
        self.fixture.destinations['gate_detail'] = {workflow: row}
        self.fixture.save()

    def test_actual_workflow_graphs_and_approved_execution_are_immutable(self):
        source = (self.root / '.github/workflows/release.yml').read_text()
        shipped_release = (ROOT / '.github/workflows/release.yml').read_text()
        execution_identity.approved_builds_sha(shipped_release)
        pin = r'(?m)^([ \t]*HEXALITH_BUILDS_EXECUTION_SHA:[ \t]*)[0-9a-f]{40}'
        normalize = lambda text: re.sub(pin, lambda match: match[1] + '<execution>', text)
        self.assertEqual(normalize(source), normalize(shipped_release))
        self.assertEqual(execution_identity.approved_builds_sha(source), self.environment['HEXALITH_BUILDS_EXECUTION_SHA'])
        for name, minimum in (('release-workflow-provenance.json', 3), ('recovery-workflow-provenance.json', 1)):
            document = json.loads((self.root / '.hexalith' / name).read_text())
            self.assertEqual(document['candidate'], self.fixture.source_sha)
            self.assertGreaterEqual(len(document['closure']['actions']), minimum)
            self.assertTrue(document['closure']['helpers'])
            self.assertTrue(all(action['commit'] == self.environment['HEXALITH_BUILDS_EXECUTION_SHA']
                                for action in document['closure']['actions']))
        shipped = (ROOT / '.github/workflows/recover-partial-release.yml').read_bytes()
        self.assertEqual((self.root / '.github/workflows/recover-partial-release.yml').read_bytes(), shipped)
        self.execute(self.admission)

    def test_actual_admission_refuses_each_changed_provenance_field(self):
        original = self.current.read_bytes()
        for field in ('candidate', 'workflow_mode', 'builds_execution_sha', 'closure'):
            with self.subTest(field=field):
                document = json.loads(original)
                document[field] = {} if field == 'closure' else 'changed'
                self.current.write_text(json.dumps(document))
                self.assertIn(field, self.execute(self.admission, False).stderr)
        self.current.write_bytes(original)

    def test_actual_admission_refuses_every_copied_publisher_file_drift(self):
        for name in PUBLISHER_FILES:
            with self.subTest(name=name):
                path = self.installed / name
                original = path.read_bytes()
                path.write_bytes(original + b'\n# installed-copy drift\n')
                self.assertIn(name, self.execute(self.admission, False).stderr)
                path.write_bytes(original)

    def test_actual_admission_requires_each_latest_gate_attempt(self):
        for workflow in WORKFLOWS:
            for status, conclusion, accepted in (('completed', 'success', True), ('completed', 'failure', False),
                                                 ('completed', 'skipped', False), ('in_progress', None, False)):
                with self.subTest(workflow=workflow, status=status, conclusion=conclusion):
                    self.gate(workflow, status, conclusion)
                    self.execute(self.admission, accepted)

    def test_caller_owned_final_preflight_rechecks_each_late_attempt(self):
        legacy = self.root / 'legacy-preflight'
        legacy.write_text('#!/bin/bash\nprintf admitted > legacy-admitted\n')
        legacy.chmod(0o755)
        environment = {**self.environment, 'HEXALITH_PUBLICATION_PREFLIGHT': str(legacy),
                       'HEXALITH_RELEASE_PACKAGE_MANIFEST': 'tools/release-packages.json',
                       'HEXALITH_RELEASE_EXPECTED_PACKAGE_COUNT': '5', 'HEXALITH_ZOT_REGISTRY': 'registry.hexalith.com',
                       'HEXALITH_RELEASE_EVIDENCE_DIRECTORY': str(self.root / 'boundary-evidence')}
        command = 'set -euo pipefail\nbash ' + str(ROOT / 'scripts/validate-publication-preflight.sh') + ' 4.0.0 publish\n'
        for workflow in WORKFLOWS:
            for status, conclusion, accepted in (('completed', 'success', True), ('completed', 'failure', False),
                                                 ('completed', 'skipped', False), ('queued', None, False)):
                with self.subTest(workflow=workflow, status=status, conclusion=conclusion):
                    (self.root / 'legacy-admitted').unlink(missing_ok=True)
                    self.gate(workflow, status, conclusion)
                    self.execute(command, accepted, environment)
                    self.assertEqual((self.root / 'legacy-admitted').exists(), accepted)

    def test_actual_recovery_bootstrap_excludes_body_text_and_rejects_ambiguous_shapes(self):
        bootstrap = workflow_step(ROOT / '.github/workflows/recover-partial-release.yml',
                                  'Resolve approved Builds execution from the source publication workflow')
        approved = self.environment['HEXALITH_BUILDS_EXECUTION_SHA']
        spoof = 'b' * 40
        cases = [
            (f'env:\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', True),
            (f'jobs:\n  release:\n    env: {{HEXALITH_BUILDS_EXECUTION_SHA: {spoof}}}\n    steps:\n      - run: |\n          HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'jobs:\n  release:\n    steps:\n      - run: |\n          HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'env: |\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'jobs:\n  release:\n    steps:\n      env:\n        HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'env: *approval\njobs:\n  release:\n    steps:\n      - run: |\n          HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'"env": {{HEXALITH_BUILDS_EXECUTION_SHA: {spoof}}}\njobs:\n  release:\n    steps:\n      - run: |\n          HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'env:\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\njobs:\n  release:\n    env: {{HEXALITH_BUILDS_EXECUTION_SHA: {spoof}}}\n', False),
            (f'env:\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\n', False),
            (f'env:\n  HEXALITH_BUILDS_EXECUTION_SHA: {approved}\njobs:\n  release:\n    steps:\n      - run: |\n          HEXALITH_BUILDS_EXECUTION_SHA: {spoof}\n', True),
        ]
        path = self.root / '.github/workflows/release.yml'
        for index, (source, accepted) in enumerate(cases):
            with self.subTest(case=index):
                path.write_text(source)
                result_path = self.root / 'bootstrap-env'
                result_path.unlink(missing_ok=True)
                self.execute(bootstrap, accepted, {**self.environment, 'GITHUB_ENV': str(result_path)})
                if accepted:
                    self.assertEqual(execution_identity.approved_builds_sha(source), approved)
                    self.assertEqual(result_path.read_text(), 'HEXALITH_BUILDS_EXECUTION_SHA=' + approved + '\n')
                else:
                    with self.assertRaises(ValueError):
                        execution_identity.approved_builds_sha(source)
                    self.assertFalse(result_path.exists())


if __name__ == '__main__':
    unittest.main()
