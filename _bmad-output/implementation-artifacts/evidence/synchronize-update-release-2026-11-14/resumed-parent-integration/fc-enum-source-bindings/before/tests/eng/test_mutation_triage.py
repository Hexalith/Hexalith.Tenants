"""Finite ledger/count/admission controls only, never a full native mutation qualification."""
import copy
import hashlib
import importlib.util
import json
import re
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

    def report(self,row,status,segment=None):
        segment=segment or next(iter(row['allowedRemaining']))
        files={}
        # Complete realistic inventory: removing a reviewed mutant still leaves every
        # approved source file and many live mutants, satisfying the old shallow checks.
        for root in self.manifest['approvedTargetRoots']:
            for path in (OWNER/root).rglob('*.cs'):
                relative=path.relative_to(OWNER).as_posix()
                files[relative]={'language':'cs','source':path.read_text(),'mutants':[]}
        for expected in self.rows.values():
            if segment in expected['allowedRemaining']:
                files[expected['path']]['mutants'].append({'id':expected['id'],'status':'Killed','mutatorName':expected['mutator'],
                    'location':check.location(expected['span']),'replacement':expected['replacement']})
        for index,file in enumerate(files.values()):
            if not file['mutants']:
                file['mutants'].append({'id':str(90000000+index),'status':'Killed','mutatorName':'synthetic filler','location':{},'replacement':''})
        target=next((mutant for mutant in files[row['path']]['mutants'] if mutant['id']==row['id']),None)
        if target is None:
            target={'id':row['id'],'mutatorName':row['mutator'],'location':check.location(row['span']),'replacement':row['replacement']}
            files[row['path']]['mutants'].append(target)
        target['status']=status
        return {'files':files}

    def selected_mutant(self,report,row):
        return next(mutant for mutant in report['files'][row['path']]['mutants'] if mutant['id']==row['id'])

    def result(self,row,status,segment=None):
        return check.inspect_report(self.report(row,status,segment),segment or next(iter(row['allowedRemaining'])),self.rows,self.sources)

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
            report=self.report(row,'Survived');file=report['files'][row['path']];mutant=self.selected_mutant(report,row)
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
            for path in [*self.ledger['sourceFiles'],check.REL+'mutation-problem-followups.json',check.REL+'mutation-target-manifest.json',self.ledger['evidenceCatalog']['path'],*[item['ownerFile'] for case in json.loads((OWNER/self.ledger['evidenceCatalog']['path']).read_text())['cases'].values() for item in case['currentPreservation']]]:
                target=root/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/path,target)
            changed=root/next(iter(self.ledger['sourceFiles']));original=changed.read_bytes();changed.write_bytes(original+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Changed current production source'):check.load_metadata(root)
            changed.write_bytes(original)
            ledger=root/(check.REL+'mutation-problem-followups.json');ledger.write_bytes(ledger.read_bytes()+b' ')
            with self.assertRaisesRegex(check.AdmissionError,'Stale/changed ledger-manifest binding'):check.load_metadata(root)

    def testNativeInvalidCompilerShapeIsFinite(self):
        row=next(row for row in self.rows.values() if row['action']=='deferred-with-owner')
        report=self.report(row,'CompileError');mutant=self.selected_mutant(report,row)
        mutant.update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
        self.assertFalse(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])
        mutant['coveredBy']=['executed-case']
        self.assertFalse(check.inspect_report(report,next(iter(row['observed'])),self.rows,self.sources)['acceptedByThisFiniteCheck'])



    def cli_fixture(self):
        temporary=tempfile.TemporaryDirectory()
        self.addCleanup(temporary.cleanup)
        owner=Path(temporary.name)
        for path in [*self.ledger['sourceFiles'],check.REL+'mutation-problem-followups.json',check.REL+'mutation-target-manifest.json','eng/check-mutation-triage.py',self.ledger['evidenceCatalog']['path'],*[item['ownerFile'] for case in json.loads((OWNER/self.ledger['evidenceCatalog']['path']).read_text())['cases'].values() for item in case['currentPreservation']]]:
            target=owner/path;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/path,target)
        row=next(row for row in self.rows.values() if row['action']=='equivalent-accepted'
                 and set(row['allowedRemaining'])=={'happy-path','error-handling'}
                 and all(row['allowedRemaining'].values()))
        for segment in ['happy-path','error-handling']:
            path=owner/f'artifacts/mutation/{segment}/reports/source-tools-{segment}.json'
            path.parent.mkdir(parents=True)
            path.write_text(json.dumps(self.report(row,row['allowedRemaining'][segment][0],segment)))
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
            report=json.loads(path.read_text());mutant=self.selected_mutant(report,row)
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

    def testReviewedLiveIdentityCannotBeIgnoredOrHiddenByCompilerCandidate(self):
        row=self.rows['11924']
        for status in ['Ignored','CompileError']:
            report=self.report(row,status,'happy-path')
            if status=='CompileError':self.selected_mutant(report,row).update({'statusReason':'Mutant caused compile errors','coveredBy':[],'killedBy':[]})
            result=check.inspect_report(report,'happy-path',self.rows,self.sources)
            self.assertFalse(result['acceptedByThisFiniteCheck'])
            self.assertTrue(any(failure['id']=='11924' for failure in result['failures']))

    def testTruncatedOtherwiseCompleteReportCannotHideReviewedIdentity(self):
        row=self.rows['11924'];report=self.report(row,'Killed','happy-path')
        before_files=set(report['files'])
        report['files'][row['path']]['mutants']=[mutant for mutant in report['files'][row['path']]['mutants'] if mutant['id']!='11924']
        self.assertEqual(set(report['files']),before_files)
        self.assertGreater(sum(len(file['mutants']) for file in report['files'].values()),self.manifest['segments'][0]['minimumMutantCount'])
        self.assertTrue(all(file['mutants'] for file in report['files'].values()))
        result=check.inspect_report(report,'happy-path',self.rows,self.sources)
        self.assertFalse(result['acceptedByThisFiniteCheck'])
        self.assertIn({'id':'11924','reason':'Expected reviewed identity missing from report'},result['failures'])

    def testEveryClaimedKillHasAnExactResolvableExecutedEvidenceCatalog(self):
        catalog=json.loads((OWNER/self.ledger['evidenceCatalog']['path']).read_text())
        self.assertEqual(len(catalog['cases']),95)
        self.assertIn('pending',catalog['rootEvidencePublication']['state'])
        for ident,case in catalog['cases'].items():
            self.assertEqual(case['identitySha256'],self.rows[ident]['identitySha256'])
            self.assertTrue(case['executed']['commands'])
            self.assertTrue(case['executed']['testSources'])
            self.assertTrue(case['executed']['resultLocator'])
            self.assertEqual(len(case['executed']['resultSha256']),64)
            self.assertTrue(all(item['method'] and item['testCase'] for item in case['currentPreservation']))

    def testQualityRequiresBothExistingSuitesAndStopsOnFailure(self):
        workflow=(OWNER/'.github/workflows/quality.yml').read_text()
        block=workflow.split("- name: 'Gate 2b: Dependency graph semantic policy tests'",1)[1].split("- name:",1)[0]
        self.assertIn('set -euo pipefail',block)
        for suite in ['test_mutation_triage.py','test_mutation_artifact_publication.py']:
            self.assertIn('python3 -m unittest tests/eng/'+suite,block)

    def testDocumentedReplayPropagatesNativePreparationValidationAndMatcherFailures(self):
        text=(OWNER/'docs/how-to/mutation-and-property-quality-gates.md').read_text()
        script=re.search(r"run-with-temp bash -c '\n(.*?)\n'",text,re.S).group(1)
        self.assertIn('prepare-mutation-artifacts.ps1 -Segment happy-path',script)
        self.assertIn('prepare-mutation-artifacts.ps1 -VerifyPublishedInputs',script)
        self.assertIn('python3 eng/check-mutation-triage.py',script)
        stub="""python3() {
          case "$*" in
            'eng/stryker_compat.py prepare') return 0 ;;
            'eng/stryker_compat.py run happy-path') label=happy ;;
            'eng/stryker_compat.py run error-handling') label=error ;;
            'eng/check-mutation-triage.py') label=matcher ;;
            *) return 99 ;;
          esac
          [ "$FAIL_STAGE" != "$label" ] || return 7
        }
        pwsh() {
          case "$*" in
            *'-Segment happy-path') label=publish-happy ;;
            *'-Segment error-handling') label=publish-error ;;
            *'-VerifyPublishedInputs') label=lineage ;;
            *'-RequireCompatibilityProvenance') label=validation ;;
            *) return 99 ;;
          esac
          [ "$FAIL_STAGE" != "$label" ] || return 7
        }
        """
        for stage in ['none','happy','error','publish-happy','publish-error','lineage','validation','matcher']:
            process=subprocess.run(['bash','-c',stub+script],env={**os.environ,'FAIL_STAGE':stage},capture_output=True,text=True)
            self.assertEqual(process.returncode,0 if stage=='none' else 7,stage+process.stderr)

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

class MutationInventoryTests(unittest.TestCase):
    """Exact enum inventory accounting without excluding executable mutation targets."""

    ENUM_ROOT='src/Hexalith.FrontComposer.SourceTools/Transforms/'
    ENUM_NAMES=('FormFieldTypeCategory','ProjectionRenderStrategy','TypeCategory')

    @staticmethod
    def implicit_enum(source):
        source=re.sub(r'//[^\r\n]*|/\*.*?\*/','',source,flags=re.S)
        return re.fullmatch(r'\s*namespace\s+[\w.]+;\s*public\s+enum\s+\w+\s*\{\s*(?:\w+\s*,\s*)+\}\s*',source) is not None

    def testOnlyTheThreeImplicitEnumFilesReceiveFiniteInventoryAccounting(self):
        manifest=json.loads((OWNER/(check.REL+'mutation-target-manifest.json')).read_text())
        expected={self.ENUM_ROOT+name+'.cs' for name in self.ENUM_NAMES}
        actual={entry['path'] for entry in manifest['explicitExclusions'] if entry['path'].startswith(self.ENUM_ROOT)}
        self.assertEqual(actual,expected)
        for entry in manifest['explicitExclusions']:
            if entry['path'] in expected:
                self.assertEqual(entry['owner'],'Story 10-4 SourceTools quality/release-automation')
                self.assertIn('implicit ordinals only',entry['reason'])
                self.assertTrue(self.implicit_enum((OWNER/entry['path']).read_text()),entry['path'])
        self.assertFalse(self.implicit_enum('namespace Fixture; public enum E { One = 1, Two, }'))
        self.assertFalse(self.implicit_enum('namespace Fixture; public enum E { One, } class C { int M() => 1; }'))

    def fixture(self,root,remove_enums=False,extra_executable=False):
        for relative in ('eng/validate-stryker-reports.ps1','eng/mutation-artifact-policy.ps1','.config/dotnet-tools.json'):
            target=root/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/relative,target)
        manifest=json.loads((OWNER/(check.REL+'mutation-target-manifest.json')).read_text())
        if remove_enums:
            manifest['explicitExclusions']=[entry for entry in manifest['explicitExclusions'] if entry['path'] not in {self.ENUM_ROOT+name+'.cs' for name in self.ENUM_NAMES}]
        for name in self.ENUM_NAMES:
            relative=self.ENUM_ROOT+name+'.cs';target=root/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/relative,target)
        files={}
        for index,target_root in enumerate(manifest['approvedTargetRoots']):
            relative=target_root+'/LiveFixture.cs';target=root/relative;target.parent.mkdir(parents=True,exist_ok=True)
            source='namespace Fixture; class Live'+str(index)+' { int Value() => 1; }\n'
            target.write_text(source)
            files[relative]={'language':'cs','source':source,'mutants':[{'id':str(index),'status':'Killed','location':{}}]}
        for name in self.ENUM_NAMES:
            relative=self.ENUM_ROOT+name+'.cs'
            files[relative]={'language':'cs','source':(root/relative).read_text(),'mutants':[]}
        if extra_executable:
            (root/self.ENUM_ROOT/'FutureExecutable.cs').write_text('namespace Fixture; class FutureExecutable { int Value() => 2; }\n')
        reports=root/'reports';reports.mkdir()
        for segment in manifest['segments']:
            target=root/segment['config'];target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(OWNER/segment['config'],target)
            (reports/(segment['artifactPrefix']+'.json')).write_text(json.dumps({'files':files}))
        (root/'manifest.json').write_text(json.dumps(manifest))

    def validate(self,root):
        result=subprocess.run(['pwsh','-NoProfile','-File',str(root/'eng/validate-stryker-reports.ps1'),
            '-ManifestPath','manifest.json','-ReportRoot','reports','-OutputPath','reports/job-summary.md'],cwd=root,capture_output=True,text=True)
        refusal=root/'reports/mutation-validation-errors.txt'
        return result,refusal.read_text() if refusal.exists() else ''

    def testNativeValidatorRetainsTheOriginalThreeTargetDriftRefusals(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root,remove_enums=True)
            result,refusal=self.validate(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertEqual(refusal.count('Target drift:'),3,refusal)
            for name in self.ENUM_NAMES:self.assertIn(self.ENUM_ROOT+name+'.cs',refusal)

    def testNativeValidatorAcceptsTheseExactNonMutatableInventoryOmissions(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root)
            result,refusal=self.validate(root)
            self.assertEqual(result.returncode,0,result.stdout+result.stderr+refusal)

    def testNativeValidatorStillRefusesAnUnreportedExecutableTarget(self):
        with tempfile.TemporaryDirectory() as temporary:
            root=Path(temporary);self.fixture(root,extra_executable=True)
            result,refusal=self.validate(root)
            self.assertNotEqual(result.returncode,0,result.stdout+result.stderr)
            self.assertEqual(refusal.count('Target drift:'),1,refusal)
            self.assertIn(self.ENUM_ROOT+'FutureExecutable.cs',refusal)


if __name__=='__main__':unittest.main(verbosity=2)
