"""Bounded checks of the workflow edit, never hosted execution or mutation evidence."""
import copy
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import unittest

import yaml

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
WORKFLOW = OWNER / '.github/workflows/mutation-property-nightly.yml'
ORIGINAL_BYTES = (BASE / 'original-mutation-property-nightly.yml').read_bytes()
ORIGINAL = yaml.safe_load(ORIGINAL_BYTES)
CURRENT_BYTES = WORKFLOW.read_bytes()
CURRENT = yaml.safe_load(CURRENT_BYTES)
SEGMENTS = ['happy-path', 'error-handling']
WORKER = 'source-tools-mutation-segment'
AGGREGATE = 'source-tools-mutation'
UPLOAD = 'actions/upload-artifact@043fb46d1a93c77aae656e7c1c64a875d1fc6a0a'
DOWNLOAD = 'actions/download-artifact@37930b1c2abaa49bbe596cd826c3c89aef350131'

def step(job, name):
    rows = [row for row in job['steps'] if row.get('name') == name]
    assert len(rows) == 1, (name, rows)
    return rows[0]

def report_paths(segment):
    return [f'artifacts/mutation/{segment}/**/reports/source-tools-{segment}.json',
            f'artifacts/mutation/{segment}/**/reports/source-tools-{segment}.html',
            f'artifacts/mutation/{segment}/tool-provenance.json']

def assert_structure(test, current):
    jobs = current['jobs']
    test.assertEqual(set(jobs), {WORKER, AGGREGATE, 'lifecycle-properties'})
    worker, aggregate = jobs[WORKER], jobs[AGGREGATE]
    test.assertEqual(worker['strategy'], {'fail-fast': False, 'matrix': {'segment': SEGMENTS}})
    test.assertEqual(aggregate['name'], AGGREGATE)
    test.assertEqual(aggregate['needs'], WORKER)
    test.assertEqual(aggregate['if'], 'always()')
    for job in [worker, aggregate]:
        for key in ['runs-on', 'container', 'defaults', 'env', 'timeout-minutes']:
            test.assertEqual(job[key], ORIGINAL['jobs'][AGGREGATE][key], key)
        test.assertNotIn('continue-on-error', job)
        test.assertTrue(all('continue-on-error' not in row for row in job['steps']))
    original_preparation = ORIGINAL['jobs'][AGGREGATE]['steps'][:8]
    test.assertEqual(worker['steps'][:8], original_preparation)
    test.assertEqual(aggregate['steps'][:8], original_preparation)
    run = step(worker, 'Run complete mutation segment')
    test.assertEqual(run['working-directory'], 'src/Hexalith.FrontComposer.SourceTools')
    test.assertEqual(run['run'].strip(), 'python3 ../../eng/trace-mutation-run.py -- python3 ../../eng/stryker_compat.py run ${{ matrix.segment }}')
    test.assertNotIn('if', run)
    verify = step(worker, 'Verify segment private-tool provenance')
    test.assertEqual(verify['run'], 'python3 eng/stryker_compat.py verify ${{ matrix.segment }}')
    test.assertEqual(verify['if'], 'always()')
    uploads = [row for row in worker['steps'] if row.get('uses') == UPLOAD]
    test.assertEqual(len(uploads), 1)
    test.assertEqual(uploads[0]['if'], 'always()')
    test.assertEqual(uploads[0]['with'], {
        'name': 'mutation-source-tools-${{ matrix.segment }}', 'retention-days': 14,
        'if-no-files-found': 'error', 'path': '\n'.join(report_paths('${{ matrix.segment }}')) + '\n'})
    downloads = [row for row in aggregate['steps'] if row.get('uses') == DOWNLOAD]
    test.assertEqual(len(downloads), 2)
    for row, segment in zip(downloads, SEGMENTS):
        test.assertEqual(row['if'], 'always()')
        # No run-id/repository/token/pattern inputs: fixed named artifacts of this same run.
        test.assertEqual(row['with'], {'name': f'mutation-source-tools-{segment}', 'path': f'artifacts/mutation/{segment}'})
    validate = step(aggregate, 'Validate mutation reports')
    test.assertEqual(validate, step(ORIGINAL['jobs'][AGGREGATE], 'Validate mutation reports'))
    require = step(aggregate, 'Require both complete segment jobs to succeed')
    test.assertEqual(require['if'], 'always()')
    test.assertEqual(require['env'], {'SEGMENT_RESULT': '${{ needs.source-tools-mutation-segment.result }}'})
    test.assertEqual(require['run'], 'test "$SEGMENT_RESULT" = success')
    test.assertEqual(step(aggregate, 'Mutation Summary'), step(ORIGINAL['jobs'][AGGREGATE], 'Mutation Summary'))
    final_upload = step(aggregate, 'Upload mutation artifacts')
    test.assertEqual(final_upload['if'], 'always()')
    test.assertEqual(final_upload['uses'], UPLOAD)
    test.assertEqual(final_upload['with'], {'name': 'mutation-source-tools-artifacts', 'retention-days': 14,
        'if-no-files-found': 'error', 'path': '\n'.join(sum([report_paths(s) for s in SEGMENTS], []) + [
            'artifacts/mutation/job-summary.md', 'artifacts/mutation/mutation-report-audit.json',
            'artifacts/mutation/mutation-validation-errors.txt']) + '\n'})
    test.assertEqual(jobs['lifecycle-properties'], ORIGINAL['jobs']['lifecycle-properties'])
    test.assertEqual({k:v for k,v in current.items() if k != 'jobs'}, {k:v for k,v in ORIGINAL.items() if k != 'jobs'})

class WorkflowTests(unittest.TestCase):
    def test_complete_fixed_cohorts_preparation_failure_paths_and_finite_artifacts(self):
        assert_structure(self, CURRENT)

    def test_lifecycle_header_bytes_and_checkout_line_endings_preserved(self):
        marker = b'  lifecycle-properties:\r\n'
        self.assertEqual(CURRENT_BYTES.split(marker, 1)[1], ORIGINAL_BYTES.split(marker, 1)[1])
        self.assertEqual(CURRENT_BYTES.split(b'jobs:\r\n',1)[0], ORIGINAL_BYTES.split(b'jobs:\r\n',1)[0])
        self.assertNotIn(b'\n', CURRENT_BYTES.replace(b'\r\n', b''))

    def test_worker_effective_invocation_is_each_original_full_segment(self):
        run = step(CURRENT['jobs'][WORKER], 'Run complete mutation segment')['run']
        for segment, old_name in zip(SEGMENTS, ['Happy-path mutation segment','Error-handling mutation segment']):
            self.assertEqual(run.replace('${{ matrix.segment }}', segment).strip(),
                             step(ORIGINAL['jobs'][AGGREGATE], old_name)['run'].strip())

    def test_result_shell_refuses_failure_cancelled_skipped_and_empty(self):
        command = step(CURRENT['jobs'][AGGREGATE], 'Require both complete segment jobs to succeed')['run']
        self.results = []
        for result in ['success','failure','cancelled','skipped','']:
            process = subprocess.run(['bash','-e','-c',command], env={**os.environ,'SEGMENT_RESULT':result}, capture_output=True)
            self.assertEqual(process.returncode == 0, result == 'success')
            self.results.append({'matrixResult': result, 'exitCode': process.returncode})
        (Path(os.environ['WORKFLOW_ATTEMPT'])/'result-shell-controls.json').write_text(json.dumps(self.results,indent=2)+'\n')

    def test_structural_regressions_refuse(self):
        def mutate(path, value):
            item = copy.deepcopy(CURRENT)
            parent = item
            for part in path[:-1]: parent = parent[part]
            parent[path[-1]] = value
            return item
        mutations = [
            ('fail-fast', ['jobs',WORKER,'strategy','fail-fast'], True),
            ('missing-cohort', ['jobs',WORKER,'strategy','matrix','segment'], ['happy-path']),
            ('lower-lifetime', ['jobs',WORKER,'timeout-minutes'], 90),
            ('different-environment', ['jobs',AGGREGATE,'env','DOTNET_PROCESSOR_COUNT'], '4'),
            ('skipped-aggregate-on-worker-failure', ['jobs',AGGREGATE,'if'], 'success()'),
            ('advisory-worker', ['jobs',WORKER,'continue-on-error'], True),
            ('runtime-distribution', ['jobs',WORKER,'steps',10,'with','path'], '**/*.dll'),
            ('missing-native-verify', ['jobs',WORKER,'steps',9,'run'], 'echo accepted'),
            ('no-required-provenance', ['jobs',AGGREGATE,'steps',10,'run'], './eng/validate-stryker-reports.ps1'),
            ('accept-failed-segment', ['jobs',AGGREGATE,'steps',11,'run'], 'true'),
            ('other-run-download', ['jobs',AGGREGATE,'steps',8,'with','run-id'], '123'),
            ('broad-final-artifact', ['jobs',AGGREGATE,'steps',13,'with','path'], 'artifacts/mutation/**')]
        records = []
        for name, path, value in mutations:
            with self.subTest(name=name):
                with self.assertRaises(AssertionError): assert_structure(self, mutate(path,value))
                records.append({'control':name,'refused':True})
        (Path(os.environ['WORKFLOW_ATTEMPT'])/'structure-refusal-controls.json').write_text(json.dumps(records,indent=2)+'\n')

    def test_source_closure_parser_sees_every_pinned_action(self):
        spec=importlib.util.spec_from_file_location('workflow_closure',OWNER/'eng/workflow_source_closure.py')
        module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
        entries=module._uses_entries(module._scan_yaml(CURRENT_BYTES,str(WORKFLOW)))
        uses=[value for value,_ in entries]
        self.assertEqual(uses.count(DOWNLOAD),2)
        self.assertEqual(uses.count(UPLOAD),3)
        original_uses=[value for value,_ in module._uses_entries(module._scan_yaml(ORIGINAL_BYTES,'original'))]
        self.assertEqual(set(uses),set(original_uses)|{DOWNLOAD})
        self.assertIn(DOWNLOAD, (ROOT/'.github/workflows/release.yml').read_text())
        (Path(os.environ['WORKFLOW_ATTEMPT'])/'action-bindings.json').write_text(json.dumps(entries,indent=2)+'\n')

if __name__ == '__main__': unittest.main(verbosity=2)
