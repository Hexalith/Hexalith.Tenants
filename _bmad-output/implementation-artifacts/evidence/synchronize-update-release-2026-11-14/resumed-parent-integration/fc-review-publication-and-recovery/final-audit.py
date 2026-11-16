"""Read-only final source, focused-case and scratch-cleanup audit for review corrections."""
from pathlib import Path
import difflib,hashlib,json,os,subprocess,time,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
before=json.loads((BASE/'before-input-hashes.json').read_text())
changed=['.github/workflows/mutation-property-nightly.yml','eng/validate-stryker-reports.ps1']+[
 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'+name+'.cs' for name in [
 'ErrorFollowthroughBehaviorTests','RazorRoleRecoveryContractTests','TemplateContractBoundaryTests','AliasSignatureContractTests','CommandContractBoundaryTests']]
new=['eng/mutation-artifact-policy.ps1','eng/prepare-mutation-artifacts.ps1','tests/eng/test_mutation_artifact_publication.py']
changes=[name for name,value in before['files'].items() if not (OWNER/name).is_file() or sha(OWNER/name)!=value]
assert set(changes)==set(changed),changes
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==before['head']
# Original function bodies and every remaining validator statement were already proven equal.
assert json.loads((BASE/'policy-preservation.json').read_text())['allOtherValidatorBytesCanonicalUnchanged']
receipts={name:json.loads((BASE/name/'verification.json').read_text()) for name in [
 'attempt-01','attempt-02','attempt-03-shape-control','attempt-04-publication','attempt-05-publication']}
for name,r in receipts.items():assert not Path(r['scratch']).exists(),name
owned=[]
for proc in Path('/proc').iterdir():
 if not proc.name.isdigit() or int(proc.name)==os.getpid():continue
 try:
  env=(proc/'environ').read_bytes().split(b'\0')
  if any(('TMPDIR='+r['scratch']).encode() in env for r in receipts.values()):owned.append(int(proc.name))
 except OSError:pass
assert not owned,owned
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
first=ET.parse(BASE/'attempt-01/test-results/tests.trx').getroot();second=ET.parse(BASE/'attempt-02/test-results/tests.trx').getroot()
rows1=first.findall('.//t:UnitTestResult',ns);rows2=second.findall('.//t:UnitTestResult',ns)
renderer=[r for r in rows1 if 'ErrorFollowthroughBehaviorTests' in r.attrib['testName']]
assert len(renderer)==25 and all(r.attrib['outcome']=='Passed' for r in renderer)
assert len(rows2)==28 and all(r.attrib['outcome']=='Passed' for r in rows2)
renderer_path='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/ErrorFollowthroughBehaviorTests.cs'
assert sha(OWNER/renderer_path)==receipts['attempt-01']['overlays'][renderer_path]
for name in changed[3:]:assert sha(OWNER/name)==receipts['attempt-02']['overlays'][name]
assert all(sha(OWNER/name)==value for name,value in receipts['attempt-05-publication']['inputs'].items())
assert receipts['attempt-05-publication']['exitCode']==0
commands=[]
for path in sorted((BASE/'attempt-05-publication/actual-processes').rglob('commands.json')):commands.extend(json.loads(path.read_text()))
assert len(commands)==18 and sum(r['exitCode']==0 for r in commands)==2
assert len([r for r in commands if r['exitCode']!=0])==16
assert receipts['attempt-01']['commands'][1]['exitCode']==0
assert sha(OWNER/changed[0])==receipts['attempt-01']['overlays'][changed[0]]
for attempt in ['attempt-01','attempt-02']:
 log=(BASE/attempt/'edited-tests-build.log').read_text();assert '0 Warning(s)' in log and '0 Error(s)' in log
# Preserve exact successful second C# input overlay, including its earlier policy guard.
for name,value in receipts['attempt-02']['overlays'].items():
 snapshot=BASE/'attempt-02/edited-inputs'/name
 assert snapshot.is_file() and sha(snapshot)==value,name
prior=BASE.parent/'fc-segment-job-lifetimes'
manifest=json.loads((prior/'evidence-manifest.json').read_text())['files']
assert all(sha(prior/name)==value for name,value in manifest.items())
files={name:sha(OWNER/name) for name in changed+new}
patch=[]
for name in changed+new:
 before_path=BASE/'pre-fix'/name
 original=before_path.read_text().splitlines(keepends=True) if before_path.exists() else []
 after=(OWNER/name).read_text().splitlines(keepends=True)
 patch.extend(difflib.unified_diff(original,after,fromfile='a/'+name if before_path.exists() else '/dev/null',tofile='b/'+name))
 snapshot=BASE/'final-owner-inputs'/name;snapshot.parent.mkdir(parents=True,exist_ok=True);snapshot.write_bytes((OWNER/name).read_bytes())
(BASE/'review-corrections.patch').write_text(''.join(patch))
summary={'scope':__doc__,'ownerHead':before['head'],'changedOrNewOwnerFiles':files,
 'preservation':{'otherOwnerInputFilesByteIdentical':len(before['files'])-len(changed),'priorLifetimeEvidenceFilesUnchanged':len(manifest),'nativeToolConfigPredicatesThresholdsManifestUnchanged':True},
 'verification':{'uniqueEditedClassCasesPassing':53,'rendererCases':{'attempt':'attempt-01','passed':25,'currentClassBytesIdentical':True},
 'fourRecoveryClasses':{'attempt':'attempt-02','passed':28,'failed':0,'skipped':0},'normalAnalyzerBuilds':{'attempt-01':{'warnings':0,'errors':0},'attempt-02':{'warnings':0,'errors':0}},
 'publication':{'attempt':'attempt-05-publication','testMethodsPassed':9,'actualPowerShellInvocations':18,'safeFailedReportPublicationsEligible':2,'actualRefusals':16},
 'actionlint':{'attempt':'attempt-01','exitCode':0,'currentWorkflowBytesIdentical':True},'exactPolicyPreservation':'policy-preservation.json'},
 'retainedAttempts':{'attempt-01':'53 C# cases:50pass/3assumption failures; publication expected refusals occurred but3 assertions failed on PowerShell line wrapping; raw inputs/logs/TRX retained',
 'attempt-02':'28 corrected recovery cases and7 publication tests pass; source inputs retained',
 'attempt-03-shape-control':'Two actual pre-fix guard admissions (linked directory and allowed filename directory) demonstrate missing shape refusals; actual child tests exit1 retained',
 'attempt-04-publication':'Fail-closed StrictMode Directory property error after expanding enumeration; raw refusal retained',
 'attempt-05-publication':'Corrected enumeration getter; all9 publication tests pass'},
 'cleanup':{'allFiveScratchRootsGone':[r['scratch'] for r in receipts.values()],'processesRetainingOwnedTMPDIR':owned,'allChildCommandsWaited':True},
 'limits':['Local publication controls invoke actual PowerShell guards; no GitHub artifact upload/download service was executed.',
 'Focused case evidence is25 unchanged renderer cases plus28 current recovery cases across retained attempts, not one combined53-case current-source suite.',
 'No full mutation score, hosted/current-source qualification, accepted triage, Git/remote or authority change is claimed.'],
 'finishedUtc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())}
(BASE/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'changedOrNewOwnerFiles':len(files),'protectedFilesPreserved':summary['preservation']['otherOwnerInputFilesByteIdentical'],'uniqueCasesPassing':53,'publicationTests':9,'actualGuardRefusals':16,'allScratchGone':True}))
