"""Finite ledger/count/admission controls only, never a full native mutation qualification."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
import subprocess
import sys
import unittest

OWNER=Path(__file__).resolve().parents[2]
spec=importlib.util.spec_from_file_location('finite_triage',OWNER/'eng/check-mutation-triage.py')
check=importlib.util.module_from_spec(spec);spec.loader.exec_module(check)

class FiniteTriageTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.ledger,cls.manifest,cls.rows,cls.sources=check.load_metadata(OWNER)

    def report(self,row,status):
        return {'files':{row['path']:{'source':self.sources[row['path']], 'mutants':[{
            'id':row['id'],'status':status,'mutatorName':row['mutator'],'location':check.location(row['span']),'replacement':row['replacement']}]}}}

    def result(self,row,status,segment=None):
        return check.inspect_report(self.report(row,status),segment or next(iter(row['observed'])),self.rows,self.sources)

    def testEveryOriginalIdentityAndObservationIsFiniteAndNoTechnicalGatesChanged(self):
        self.assertEqual(len(self.rows),829)
        self.assertEqual(sum(len(row['observed']) for row in self.rows.values()),1498)
        self.assertEqual(len(self.manifest['problemMutantTriage']),14)
        current=self.manifest
        self.assertEqual({segment['name']:segment['threshold'] for segment in current['segments']},{'happy-path':80,'error-handling':60})
        self.assertTrue(all(row['action']!='blocking' for row in self.rows.values()))
        self.assertEqual(sum(row['action']=='kill-test-added' for row in self.rows.values()),95)
        self.assertEqual(sum(row['action']=='deferred-with-owner' for row in self.rows.values()),405)
        self.assertLess((OWNER/(check.REL+'mutation-problem-followups.json')).stat().st_size,1048576)

    def testEachFiniteAllowedStatusIncludingSupplementalIsAdmittedByIdentityOnly(self):
        cases=0
        for row in self.rows.values():
            for segment,statuses in row['allowedRemaining'].items():
                for status in statuses:
                    with self.subTest(id=row['id'],segment=segment,status=status):
                        result=self.result(row,status,segment)
                        self.assertTrue(result['acceptedByThisFiniteCheck'],result['failures'])
                        self.assertTrue(result['nativeScoresBothCohortsPrivateProvenanceStillRequired'])
                        cases+=1
        self.assertGreater(cases,0)
        if os.environ.get('TRIAGE_CHECK_EVIDENCE'):
            (Path(os.environ['TRIAGE_CHECK_EVIDENCE'])/'allowed-status-case-count.json').write_text(json.dumps({'finiteStatusCases':cases,'scope':'Identity/status admission only; not complete/native reports'},indent=2)+'\n')

    def testEveryTestAddedIdentityRefusesAnyFreshRemainingProblem(self):
        for row in self.rows.values():
            if row['action']=='kill-test-added':
                for segment in row['observed']:
                    for status in ['Survived','NoCoverage','Timeout','CompileError']:
                        with self.subTest(id=row['id'],segment=segment,status=status):
                            self.assertFalse(self.result(row,status,segment)['acceptedByThisFiniteCheck'])

    def testUnknownIdentitySpanReplacementMutatorAndSourceChangesRefuse(self):
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner' and 'Survived' in next(iter(row['allowedRemaining'].values())))
        segment=next(iter(row['observed']))
        for change in ['id','location','replacement','mutatorName','source','status']:
            report=self.report(row,'Survived');file=next(iter(report['files'].values()));mutant=file['mutants'][0]
            if change=='id':mutant['id']='99999999'
            elif change=='location':mutant['location']['start']['column']+=1
            elif change=='replacement':mutant['replacement']+=' changed'
            elif change=='mutatorName':mutant['mutatorName']='other mutator'
            elif change=='source':file['source']+='\n// changed'
            else:mutant['status']='RuntimeError'
            with self.subTest(change=change):self.assertFalse(check.inspect_report(report,segment,self.rows,self.sources)['acceptedByThisFiniteCheck'])

    def testUnexpectedExecutionStatusAndCoverageRegressionRefuse(self):
        for row in self.rows.values():
            for segment,status in row['observed'].items():
                if row['action']!='kill-test-added' and status=='Survived':
                    self.assertFalse(self.result(row,'NoCoverage',segment)['acceptedByThisFiniteCheck'])
                    self.assertFalse(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'])
                    return
        self.fail('Expected a finite reviewed survivor')

    def testHost12812KeepsOriginalUncertaintyAndOnlyItsExactSupplementalAdmission(self):
        uncertainty=self.ledger['hosted12812Uncertainty']
        self.assertEqual(uncertainty['localCompleteStatus'],'Killed')
        self.assertIsNone(uncertainty['hostedFinalJsonStatus'])
        self.assertFalse(uncertainty['admittedAsProblem'])
        row=self.rows['12812']
        self.assertEqual(row['observed'],{})
        self.assertFalse(row['supplementalReview']['nativeOutcomeClaimed'])
        for segment in ['happy-path','error-handling']:
            for status in ['Survived','NoCoverage']:
                self.assertTrue(self.result(row,status,segment)['acceptedByThisFiniteCheck'])
            self.assertFalse(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'])
        unknown=copy.deepcopy(row);unknown['id']='12812000'
        self.assertFalse(self.result(unknown,'Survived','happy-path')['acceptedByThisFiniteCheck'])

    def testDestructiveTimeoutIsRetainedAndNotBlanketStatusAdmission(self):
        row=self.rows['14021']
        self.assertEqual(row['kind'],'non-actionable-detected-destructive-nonprogress')
        for segment in row['observed']:
            self.assertTrue(self.result(row,'Timeout',segment)['acceptedByThisFiniteCheck'])
            self.assertFalse(self.result(row,'NoCoverage',segment)['acceptedByThisFiniteCheck'])
            self.assertFalse(self.result(row,'Survived',segment)['acceptedByThisFiniteCheck'])
        self.assertEqual(self.rows['13541']['action'],'deferred-with-owner')
        self.assertIsNotNone(self.rows['13541']['followup'])

    def testCurrentSourceAndLedgerDigestDriftActuallyRefuse(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary)
            for path in [*self.ledger['sourceFiles'],check.REL+'mutation-problem-followups.json',check.REL+'mutation-target-manifest.json']:
                target=root/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/path,target)
            changed=root/next(iter(self.ledger['sourceFiles']));original=changed.read_bytes();changed.write_bytes(original+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Changed current production source'):check.load_metadata(root)
            changed.write_bytes(original)
            ledger=root/(check.REL+'mutation-problem-followups.json');ledger.write_bytes(ledger.read_bytes()+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Stale/changed ledger-manifest binding'):check.load_metadata(root)

    def testNativeInvalidCompilerShapeIsFinite(self):
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner')
        report=self.report(row,'CompileError');mutant=next(iter(report['files'].values()))['mutants'][0]
        mutant.update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
        self.assertTrue(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])
        mutant['coveredBy']=['executed-case']
        self.assertFalse(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])



    def cli_fixture(self):
        temporary=tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        owner=Path(temporary.name)
        for path in [*self.ledger['sourceFiles'],check.REL+'mutation-problem-followups.json',check.REL+'mutation-target-manifest.json','eng/check-mutation-triage.py']:
            target=owner/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/path,target)
        row=next(row for row in self.rows.values() if row['action']=='equivalent-accepted'
                 and set(row['allowedRemaining'])=={'happy-path','error-handling'}
                 and all(row['allowedRemaining'].values()))
        for segment in ['happy-path','error-handling']:
            path=owner/f'artifacts/mutation/{segment}/reports/source-tools-{segment}.json'
            path.parent.mkdir(parents=True)
            path.write_text(json.dumps(self.report(row,row['allowedRemaining'][segment][0])))
        return owner,row

    def run_cli(self,owner,label):
        process=subprocess.run([sys.executable,'-B',str(owner/'eng/check-mutation-triage.py')],cwd=owner,capture_output=True,text=True,timeout=30)
        audit=owner/'artifacts/mutation/mutation-triage-admission.json'
        self.assertTrue(audit.is_file(),process.stderr)
        result=json.loads(audit.read_text())
        if os.environ.get('TRIAGE_CHECK_EVIDENCE'):
            directory=Path(os.environ['TRIAGE_CHECK_EVIDENCE'])/label;directory.mkdir(parents=True,exist_ok=True)
            (directory/'console.log').write_text(process.stdout+process.stderr)
            (directory/'admission.json').write_bytes(audit.read_bytes())
            (directory/'process.json').write_text(json.dumps({'exitCode':process.returncode,'scope':'Actual CLI finite fixture only; no native qualification'},indent=2)+'\n')
        return process,result

    def testActualBothSegmentCommandAcceptsOnlyFiniteIdentityStatus(self):
        owner,row=self.cli_fixture()
        process,result=self.run_cli(owner,'cli-positive')
        self.assertEqual(process.returncode,0,process.stderr)
        self.assertTrue(result['acceptedByThisFiniteCheck'])
        self.assertEqual(len(result['segments']),2)
        self.assertTrue(result['nativeScoresBothCohortsPrivateProvenanceStillRequired'])

    def testActualBothSegmentCommandRefusesMissingAndAmbiguousReports(self):
        for shape in ['missing','ambiguous']:
            owner,row=self.cli_fixture()
            path=owner/'artifacts/mutation/happy-path/reports/source-tools-happy-path.json'
            if shape=='missing':path.unlink()
            else:
                extra=path.parent.parent/'other/reports'/path.name;extra.parent.mkdir(parents=True);shutil.copyfile(path,extra)
            process,result=self.run_cli(owner,'cli-'+shape)
            self.assertNotEqual(process.returncode,0)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertEqual(len(result['segments']),2)
            self.assertIn('Missing or ambiguous',result['segments'][0]['error'])
            self.assertTrue(result['segments'][1]['acceptedByThisFiniteCheck'])

    def testActualBothSegmentCommandRefusesUnknownIdentityAndChangedSpan(self):
        for changed in ['id','location','replacement','status']:
            owner,row=self.cli_fixture()
            path=owner/'artifacts/mutation/error-handling/reports/source-tools-error-handling.json'
            report=json.loads(path.read_text());mutant=next(iter(report['files'].values()))['mutants'][0]
            if changed=='id':mutant['id']='99999999'
            elif changed=='location':mutant['location']['start']['column']+=1
            elif changed=='replacement':mutant['replacement']+=' changed'
            else:mutant['status']='RuntimeError'
            path.write_text(json.dumps(report))
            process,result=self.run_cli(owner,'cli-refuse-'+changed)
            self.assertNotEqual(process.returncode,0)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertTrue(result['segments'][0]['acceptedByThisFiniteCheck'])
            self.assertFalse(result['segments'][1]['acceptedByThisFiniteCheck'])

    def testAggregateRequiresMatcherAfterStrictBothAndGuardScansItsAudit(self):
        workflow=(OWNER/'.github/workflows/mutation-property-nightly.yml').read_text().split('  source-tools-mutation:\n',1)[1].split('  lifecycle-properties:\n',1)[0]
        self.assertLess(workflow.index('Validate mutation reports'),workflow.index('Validate finite mutation identities and statuses'))
        self.assertLess(workflow.index('Validate finite mutation identities and statuses'),workflow.index('Require both complete segment jobs to succeed'))
        self.assertIn('run: ./eng/validate-stryker-reports.ps1 -RequireCompatibilityProvenance',workflow)
        self.assertIn('Validate finite mutation identities and statuses\n        if: always()\n        run: python3 eng/check-mutation-triage.py',workflow)
        self.assertIn('run: test "$SEGMENT_RESULT" = success',workflow)
        self.assertIn('artifacts/mutation/mutation-triage-admission.json',workflow)
        self.assertIn('"mutation-triage-admission.json"',(OWNER/'eng/prepare-mutation-artifacts.ps1').read_text())
        self.assertIn("if: always() && steps.publication.outcome == 'success'",workflow)
        self.assertNotIn('continue-on-error',workflow)

if __name__=='__main__':unittest.main(verbosity=2)
