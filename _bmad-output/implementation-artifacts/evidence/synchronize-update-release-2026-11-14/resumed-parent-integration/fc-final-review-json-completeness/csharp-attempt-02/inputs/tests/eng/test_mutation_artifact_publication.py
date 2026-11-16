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
        for name in ['job-summary.md', 'mutation-report-audit.json', 'mutation-triage-admission.json', 'mutation-validation-errors.txt']:
            path = self.report_root / name
            text=f'Synthetic failed-report summary under {self.root}'
            path.write_text(json.dumps({'summary':text}) if path.suffix=='.json' else text)
            paths.append(path)
        excluded = self.report_root / 'happy-path/unselected-console.log'
        excluded.write_text('password=synthetic-not-published')
        process = self.run_guard()
        self.assertEqual(process.returncode, 0, process.stderr)
        self.assertIn('10 allowed artifacts', process.stdout)
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
                report.write_text(json.dumps({'source':unsafe}))
                process = self.run_guard('happy-path')
                self.assertNotEqual(process.returncode, 0)
                self.assertIn(reason, process.stderr)
                self.assertEqual(html.read_bytes(), original_html)

    def testActualUnsafeAggregateSummaryAndEitherSegmentRefuse(self):
        happy_report, _, _ = self.fixture()
        error_report, _, _ = self.fixture('error-handling')
        for path in [happy_report, error_report, self.report_root / 'job-summary.md', self.report_root / 'mutation-report-audit.json', self.report_root / 'mutation-triage-admission.json', self.report_root / 'mutation-validation-errors.txt']:
            with self.subTest(path=path.name):
                original = path.read_bytes() if path.exists() else None
                path.write_text(json.dumps({'source':'password=synthetic'}) if path.suffix=='.json' else 'password=synthetic')
                try:
                    process = self.run_guard()
                    self.assertNotEqual(process.returncode, 0)
                    self.assertIn('secret-like', process.stderr)
                    self.assertIn('value.', process.stderr)
                    self.assertIn(path.name, process.stderr)
                finally:
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

    def testLinkedReportDirectoryRefusesWithoutPublishingItsUnsafeTarget(self):
        report, _, _ = self.fixture()
        outside = self.root / 'outside/reports'
        outside.mkdir(parents=True)
        target = outside / report.name
        target.write_text('password=synthetic')
        (report.parent.parent / 'linked').symlink_to(outside.parent, target_is_directory=True)
        process = self.run_guard('happy-path')
        self.assertNotEqual(process.returncode, 0)
        self.assertIn('linked', process.stderr)
        self.assertEqual(target.read_text(), 'password=synthetic')

    def testDirectoryAtAllowedReportNameRefusesItsUnsafeDescendants(self):
        report, _, _ = self.fixture()
        report.unlink()
        report.mkdir()
        (report / 'unsafe.txt').write_text('password=synthetic')
        process = self.run_guard('happy-path')
        self.assertNotEqual(process.returncode, 0)
        self.assertIn('regular files', process.stderr)

    def testActualValidatorAuditHashSurvivesIdempotentPublication(self):
        report, _, _ = self.fixture()
        shutil.copyfile(ROOT / 'eng/validate-stryker-reports.ps1', self.root / 'eng/validate-stryker-reports.ps1')
        roots = ['src/Hexalith.FrontComposer.SourceTools/Parsing', 'src/Hexalith.FrontComposer.SourceTools/Transforms']
        files = {}
        for index, directory in enumerate(roots):
            path = self.root / directory / 'Fixture.cs'
            path.parent.mkdir(parents=True)
            path.write_text('namespace Fixture; class Example {}')
            files[directory + '/Fixture.cs'] = {'language': 'cs', 'source': path.read_text(),
                'mutants': [{'id': str(index + 1), 'status': 'Killed', 'location': {}}]}
        (self.root / '.config').mkdir()
        (self.root / '.config/dotnet-tools.json').write_text(json.dumps({'tools': {'dotnet-stryker': {'version': '5.1.0'}}}))
        (self.root / 'config.json').write_text(json.dumps({'stryker-config': {
            'project': 'src/Hexalith.FrontComposer.SourceTools/Hexalith.FrontComposer.SourceTools.csproj',
            'configuration': 'Release', 'reporters': ['progress', 'html', 'json'], 'coverage-analysis': 'perTest',
            'thresholds': {'break': 80}, 'mutate': ['Parsing/**/*.cs', 'Transforms/**/*.cs']}}))
        (self.root / 'manifest.json').write_text(json.dumps({'approvedTargetRoots': roots,
            'segments': [{'name': 'happy-path', 'config': 'config.json', 'threshold': 80,
                          'artifactPrefix': 'source-tools-happy-path', 'minimumMutantCount': 1}],
            'explicitExclusions': [], 'triageActions': ['blocking'], 'problemMutantTriage': []}))
        # Synthetic fixture reaches the real validator's byte audit, never a private-tool qualification.
        report.write_text(json.dumps({'schemaVersion': '1.0', 'thresholds': {'high': 80, 'low': 80}, 'files': files}))
        args = ['pwsh', '-NoProfile', '-NonInteractive', '-File', str(self.root / 'eng/validate-stryker-reports.ps1'),
                '-ManifestPath', 'manifest.json', '-ReportRoot', 'artifacts/mutation',
                '-OutputPath', 'artifacts/mutation/job-summary.md']
        process = subprocess.run(args, cwd=self.root, capture_output=True, text=True, timeout=30)
        self.commands.append({'command': args, 'exitCode': process.returncode, 'output': process.stdout + process.stderr})
        self.assertEqual(process.returncode, 0, process.stderr)
        audit_path = self.report_root / 'mutation-report-audit.json'
        recorded_hash = json.loads(audit_path.read_text())['Reports'][0]['OutputSha256']
        self.assertEqual(hashlib.sha256(report.read_bytes()).hexdigest(), recorded_hash)
        before = {path: path.read_bytes() for path in self.report_root.rglob('*') if path.is_file()}
        for _ in range(2):
            preparation = self.run_guard()
            self.assertEqual(preparation.returncode, 0, preparation.stderr)
            self.assertEqual(hashlib.sha256(report.read_bytes()).hexdigest(), recorded_hash)
            self.assertEqual({path: path.read_bytes() for path in before}, before)
            self.assertEqual(json.loads(audit_path.read_text())['Reports'][0]['OutputSha256'], recorded_hash)

    def testJsonEscapedCredentialIsDecodedAndRefused(self):
        report,_,_=self.fixture()
        report.write_text('{"source":"password\\u003dsynthetic"}')
        process=self.run_guard('happy-path')
        self.assertNotEqual(process.returncode,0)
        self.assertIn('secret-like',process.stderr)
        self.assertFalse((report.parent.parent/'publication-digests.json').exists())

    def testJsonWindowsPathIsRedactedAndRemainsValid(self):
        report,_,_=self.fixture()
        report.write_text(json.dumps({'source':r'C:\Users\private-person\repository'}))
        process=self.run_guard('happy-path')
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertEqual(json.loads(report.read_text())['source'],r'<local-user>\repository')
        self.assertNotIn('private-person',report.read_text())

    def testJsonTerminalHomePathKeepsDelimitersAndIdempotentBytes(self):
        report,_,_=self.fixture()
        report.write_text('{"source":"/home/private-person"}')
        process=self.run_guard('happy-path')
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertEqual(json.loads(report.read_text()),{'source':'<local-user>'})
        published=report.read_bytes()
        process=self.run_guard('happy-path')
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertEqual(report.read_bytes(),published)

    def testOriginalDigestCaptureAndAggregatePublishedBindingAreRealAndFinite(self):
        originals={}
        for segment in ['happy-path','error-handling']:
            report,_,_=self.fixture(segment)
            report.write_text(json.dumps({'source':'/home/private-person'}))
            originals[segment]=hashlib.sha256(report.read_bytes()).hexdigest()
            process=self.run_guard(segment)
            self.assertEqual(process.returncode,0,process.stderr)
            record=json.loads((report.parent.parent/'publication-digests.json').read_text())
            entry=next(item for item in record['files'] if item['path'].endswith('.json') and item['path'].startswith('reports/'))
            self.assertEqual(entry['originalSha256'],originals[segment])
            self.assertEqual(entry['publishedSha256'],hashlib.sha256(report.read_bytes()).hexdigest())
            self.assertNotEqual(entry['originalSha256'],entry['publishedSha256'])
        args=['pwsh','-NoProfile','-NonInteractive','-File',str(self.root/'eng/prepare-mutation-artifacts.ps1'),'-VerifyPublishedInputs']
        process=subprocess.run(args,cwd=self.root,capture_output=True,text=True,timeout=30)
        self.commands.append({'command':args,'exitCode':process.returncode,'output':process.stdout+process.stderr})
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertTrue((self.report_root/'publication-input-audit.json').is_file())
        report.write_text(json.dumps({'source':'changed safe text'}))
        process=subprocess.run(args,cwd=self.root,capture_output=True,text=True,timeout=30)
        self.commands.append({'command':args,'exitCode':process.returncode,'output':process.stdout+process.stderr})
        self.assertNotEqual(process.returncode,0)
        self.assertIn('binding differs',process.stderr)

    def testAggregateCannotInventMissingOriginalCapture(self):
        self.fixture();self.fixture('error-handling')
        args=['pwsh','-NoProfile','-NonInteractive','-File',str(self.root/'eng/prepare-mutation-artifacts.ps1'),'-VerifyPublishedInputs']
        process=subprocess.run(args,cwd=self.root,capture_output=True,text=True,timeout=30)
        self.commands.append({'command':args,'exitCode':process.returncode,'output':process.stdout+process.stderr})
        self.assertNotEqual(process.returncode,0)
        self.assertIn('Missing original publication capture',process.stderr)
        self.assertFalse(any(self.report_root.rglob('publication-digests.json')))

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
