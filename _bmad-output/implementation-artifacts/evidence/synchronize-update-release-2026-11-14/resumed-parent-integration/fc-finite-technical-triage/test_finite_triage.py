"""Finite ledger/count/admission controls only, never a full native mutation qualification."""
import copy
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import tempfile
import unittest

BASE=Path(__file__).resolve().parent
OWNER=BASE.parents[5]/'references/Hexalith.FrontComposer'
spec=importlib.util.spec_from_file_location('finite_triage',BASE/'check-fresh-report.py')
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
        original=json.loads((BASE/'before-mutation-target-manifest.json').read_text())
        current=copy.deepcopy(self.manifest);current['problemMutantTriage']=[]
        self.assertEqual(current,original)
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

    def testPrivateCompileRefusalsAreNotKillsAndNativeInvalidShapeIsFinite(self):
        proof=json.loads((BASE/'verified-identities-and-controls.json').read_text())
        refused=[ident for ident,c in proof['literalControls'].items() if c['result']=='compile-refused']
        self.assertEqual(len(refused),10)
        self.assertTrue(all(self.rows[ident]['action']!='kill-test-added' for ident in refused))
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner')
        report=self.report(row,'CompileError');mutant=next(iter(report['files'].values()))['mutants'][0]
        mutant.update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
        self.assertTrue(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])
        mutant['coveredBy']=['executed-case']
        self.assertFalse(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])

if __name__=='__main__':unittest.main(verbosity=2)
