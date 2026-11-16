"""Finite publication-policy controls; fixture reports never represent mutation qualification."""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


class PublicationTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        (self.root / 'eng').mkdir()
        for name in ['prepare-mutation-artifacts.ps1', 'mutation-artifact-policy.ps1']:
            shutil.copyfile(ROOT / 'eng' / name, self.root / 'eng' / name)
        self.commands = []
        self.report_root = self.root / 'artifacts/mutation'

    def tearDown(self):
        destination = os.environ.get('PUBLICATION_TEST_EVIDENCE')
        if destination:
            directory = Path(destination) / self._testMethodName
            directory.mkdir(parents=True, exist_ok=True)
            for index, row in enumerate(self.commands):
                output = row.pop('output')
                (directory / f'{index:02d}.log').write_text(output)
                row['logSha256'] = hashlib.sha256(output.encode()).hexdigest()
            (directory / 'commands.json').write_text(json.dumps(self.commands, indent=2) + '\n')

    def fixture(self, segment='happy-path'):
        directory = self.report_root / segment
        (directory / 'reports').mkdir(parents=True, exist_ok=True)
        report = directory / f'reports/source-tools-{segment}.json'
        report.write_text(json.dumps({'scope': 'Synthetic failed report publication fixture', 'status': 'Survived'}))
        html = directory / f'reports/source-tools-{segment}.html'
        html.write_text(f'root={self.root} user=/home/policy-user/source windows=C:\\Users\\policy-user\\source\n'
                        '{token:x} var cancellationToken = pending.Token;')
        receipt = directory / 'tool-provenance.json'
        receipt.write_text(json.dumps({'scope': 'Synthetic policy fixture; not a compatibility receipt', 'identity': 'unchanged'}))
        return report, html, receipt

    def run_guard(self, segment=None):
        args = ['pwsh', '-NoProfile', '-NonInteractive', '-File', str(self.root / 'eng/prepare-mutation-artifacts.ps1')]
        if segment:
            args += ['-Segment', segment]
        process = subprocess.run(args, cwd=self.root, capture_output=True, text=True, timeout=30)
        self.commands.append({'command': args, 'exitCode': process.returncode, 'output': process.stdout + process.stderr})
        return process

    def testSafeFailedSegmentIsNormalizedWithoutChangingReceiptIdentity(self):
        report, html, receipt = self.fixture()
        original_receipt = json.loads(receipt.read_text())
        process = self.run_guard('happy-path')
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertEqual(json.loads(report.read_text())['status'], 'Survived')
        self.assertEqual(json.loads(receipt.read_text()), original_receipt)
        normalized = html.read_text()
        self.assertIn('root=.', normalized)
        self.assertNotIn(str(self.root), normalized)
        self.assertNotIn('/home/policy-user', normalized)
        self.assertNotIn('C:\\Users\\policy-user', normalized)
        self.assertIn('{token:x}', normalized)
        self.assertIn('var cancellationToken = pending.Token;', normalized)

    def testSafeFailedAggregatePreservesBothReportsAndNormalizesSummaries(self):
        paths = [*self.fixture(), *self.fixture('error-handling')]
        for name in ['job-summary.md', 'mutation-report-audit.json', 'mutation-validation-errors.txt']:
            path = self.report_root / name
            path.write_text(f'Synthetic failed-report summary under {self.root}')
            paths.append(path)
        excluded = self.report_root / 'happy-path/unselected-console.log'
        excluded.write_text('password=synthetic-not-published')
        process = self.run_guard()
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertIn('9 allowed artifacts', process.stdout)
        self.assertTrue(all(str(self.root) not in path.read_text() for path in paths))
        self.assertEqual(excluded.read_text(), 'password=synthetic-not-published')

    def testActualUnsafeSegmentContentRefusesBeforeAnyNormalizationWrite(self):
        report, html, _ = self.fixture()
        original_html = html.read_bytes()
        for unsafe, reason in [
            ('Bearer ABCDEFGHIJKLMNOP', 'secret-like value'),
            ('api_key=synthetic', 'secret-like value'),
            ('password=synthetic', 'secret-like value'),
            ('secret=synthetic', 'secret-like value'),
            ('token=synthetic', 'secret-like value'),
            ('tenant_id=01234567-89ab-cdef-0123-456789abcdef', 'tenant identifier'),
            ('C:\\Users\\', 'machine-local path'),
        ]:
            with self.subTest(reason=reason, unsafe=unsafe):
                report.write_text(json.dumps({'source': unsafe})) if not unsafe.startswith('C:') else report.write_text(unsafe)
                process = self.run_guard('happy-path')
                self.assertNotEqual(process.returncode, 0)
                self.assertIn(reason, process.stderr)
                self.assertEqual(html.read_bytes(), original_html)

    def testActualUnsafeAggregateSummaryAndEitherSegmentRefuse(self):
        self.fixture()
        error_report, _, _ = self.fixture('error-handling')
        for path in [error_report, self.report_root / 'job-summary.md', self.report_root / 'mutation-report-audit.json', self.report_root / 'mutation-validation-errors.txt']:
            with self.subTest(path=path.name):
                original = path.read_bytes() if path.exists() else None
                path.write_text('password=synthetic')
                process = self.run_guard()
                self.assertNotEqual(process.returncode, 0)
                self.assertIn('secret-like value', process.stderr)
                if original is None:
                    path.unlink()
                else:
                    path.write_bytes(original)

    def testNoAllowedArtifactsRefuses(self):
        process = self.run_guard('error-handling')
        self.assertNotEqual(process.returncode, 0)
        self.assertIn('No allowed mutation artifacts', process.stderr)

    def testLinkedArtifactRefusesWithoutWritingItsTarget(self):
        report, _, _ = self.fixture()
        outside = self.root / 'outside.json'
        outside.write_text('safe outside bytes')
        report.unlink()
        report.symlink_to(outside)
        process = self.run_guard('happy-path')
        self.assertNotEqual(process.returncode, 0)
        self.assertIn('linked artifacts', process.stderr)
        self.assertEqual(outside.read_text(), 'safe outside bytes')

    def testWorkflowPublishesOnlyAfterSuccessfulGuardEvenOnMutationFailure(self):
        text = (ROOT / '.github/workflows/mutation-property-nightly.yml').read_text()
        worker, aggregate = text.split('  source-tools-mutation-segment:\n', 1)[1].split('\n  source-tools-mutation:\n', 1)
        aggregate = aggregate.split('\n  lifecycle-properties:\n', 1)[0]
        condition = "        if: always() && steps.publication.outcome == 'success'\n"
        for block, preparation, upload in [
            (worker, 'Prepare segment artifacts for publication', 'Upload segment reports and provenance'),
            (aggregate, 'Prepare aggregate artifacts for publication', 'Upload mutation artifacts'),
        ]:
            self.assertLess(block.index('- name: ' + preparation), block.index('- name: ' + upload))
            self.assertIn('- name: ' + preparation + '\n        id: publication\n        if: always()\n', block)
            self.assertIn('- name: ' + upload + '\n' + condition, block)
            self.assertNotIn('continue-on-error', block)
        self.assertIn('./eng/validate-stryker-reports.ps1 -RequireCompatibilityProvenance', aggregate)
        self.assertIn('run: test "$SEGMENT_RESULT" = success', aggregate)
        self.assertIn('fail-fast: false', worker)


if __name__ == '__main__':
    unittest.main(verbosity=2)
