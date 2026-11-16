"""Source/cleanup/evidence audit only; no broad tests, native score or release claim."""
from pathlib import Path
import collections,difflib,hashlib,json,os,subprocess,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer';REL='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest();before=json.loads((BASE/'before-input-hashes.json').read_text())
changed=['eng/prepare-mutation-artifacts.ps1','eng/mutation-artifact-policy.ps1','eng/validate-stryker-reports.ps1','eng/check-mutation-triage.py','.github/workflows/quality.yml','.github/workflows/mutation-property-nightly.yml','docs/how-to/mutation-and-property-quality-gates.md','tests/eng/test_mutation_triage.py','tests/eng/test_mutation_artifact_publication.py',REL+'ModelHashFieldParticipationTests.cs',REL+'mutation-problem-followups.json',REL+'mutation-target-manifest.json']
new=[REL+'mutation-regression-evidence.json']
actual=[p for p,h in before['files'].items() if not (OWNER/p).is_file() or sha(OWNER/p)!=h];assert set(actual)==set(changed),actual
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==before['head']
# Existing plain-text policy remains exact; only JSON-aware entry points were added.
def function(text,name,next_name=None):
 a=text.index('function '+name+'(');b=text.index('\n}',a)+2;return text[a:b].strip()
old=(BASE/'before/eng/mutation-artifact-policy.ps1').read_text();policy=(OWNER/'eng/mutation-artifact-policy.ps1').read_text()
for name in ['Normalize-ArtifactText','Test-UnsafeText']:assert function(old,name)==function(policy,name)
manifest=json.loads((OWNER/(REL+'mutation-target-manifest.json')).read_text());old_manifest=json.loads((BASE/'before'/(REL+'mutation-target-manifest.json')).read_text())
for a,b in zip(manifest['problemMutantTriage'],old_manifest['problemMutantTriage']):
 a=dict(a);b=dict(b);a.pop('identityLedgerSha256');b.pop('identityLedgerSha256');assert a==b
ledger=json.loads((OWNER/(REL+'mutation-problem-followups.json')).read_text());old_ledger=json.loads((BASE/'before'/(REL+'mutation-problem-followups.json')).read_text())
assert len(ledger['mutations'])==829 and sum(len(r['observed']) for r in ledger['mutations'])==1498
for current,original in zip(ledger['mutations'],old_ledger['mutations']):
 current=dict(current);original=dict(original)
 if current['action']=='kill-test-added':current.pop('control');original.pop('control')
 assert current==original
catalog=json.loads((OWNER/ledger['evidenceCatalog']['path']).read_text());assert len(catalog['cases'])==95
assert sha(OWNER/ledger['evidenceCatalog']['path'])==ledger['evidenceCatalog']['sha256']
assert all(record['identityLedgerSha256']==sha(OWNER/(REL+'mutation-problem-followups.json')) for record in manifest['problemMutantTriage'])
assert all((OWNER/p).stat().st_size<1048576 for p in changed+new)
cs=json.loads((BASE/'csharp-attempt-02/verification.json').read_text());assert cs['completed']
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
trx={}
for label in ['baseline','13180','13382','13419','13163','shell45']:
 doc=ET.parse(BASE/'csharp-attempt-02'/(label+'-results/tests.trx'));counts=doc.find('.//t:Counters',ns).attrib;trx[label]=counts
 assert counts['failed']==('1' if label in ['13180','13382','13419','13163'] else '0')
 assert counts['total']==('45' if label=='shell45' else '1') and counts['notExecuted']=='0'
for row in cs['commands']:
 if row['name'].endswith('-build'):
  log=(BASE/'csharp-attempt-02'/(row['name']+'.log')).read_text();assert '0 Warning(s)' in log and '0 Error(s)' in log
assert 'Ran 15 tests' in (BASE/'python-attempt-02/publication.log').read_text()
assert 'Ran 18 tests' in (BASE/'triage-catalog-attempt-03/tests.log').read_text()
for row in json.loads((BASE/'python-attempt-02/verification.json').read_text())['commands']:assert row['exitCode']==0
assert json.loads((BASE/'triage-catalog-attempt-03/verification.json').read_text())['exitCode']==0
assert next(row for row in json.loads((BASE/'python-attempt-01/verification.json').read_text())['commands'] if row['name']=='actionlint')['exitCode']==0
receipts=[json.loads((BASE/path).read_text()) for path in ['csharp-attempt-01/verification.json','csharp-attempt-02/verification.json','python-attempt-01/verification.json','python-attempt-02/verification.json','triage-catalog-attempt-03/verification.json']]
for receipt in receipts:assert not Path(receipt['scratch']).exists()
owned=[]
for proc in Path('/proc').iterdir():
 if not proc.name.isdigit() or int(proc.name)==os.getpid():continue
 try:
  values=(proc/'environ').read_bytes().split(b'\0')
  if any(('TMPDIR='+receipt['scratch']).encode() in values for receipt in receipts):owned.append(int(proc.name))
 except OSError:pass
assert not owned
# The current ModelHash source is bound to its actual successful baseline/literal controls.
assert sha(OWNER/(REL+'ModelHashFieldParticipationTests.cs'))==cs['overlays'][REL+'ModelHashFieldParticipationTests.cs']
files={p:sha(OWNER/p) for p in changed+new}
patch=[]
for p in changed+new:
 old_path=BASE/'before'/p
 patch+=difflib.unified_diff(old_path.read_text().splitlines(keepends=True) if old_path.exists() else [],(OWNER/p).read_text().splitlines(keepends=True),fromfile='a/'+p if old_path.exists() else '/dev/null',tofile='b/'+p)
 target=BASE/'final-owner-inputs'/p;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((OWNER/p).read_bytes())
(BASE/'final-review-corrections.patch').write_text(''.join(patch))
summary={'scope':__doc__,'ownerBase':before['head'],'ownerInputs':files,'protectedPriorOwnerInputs':len(before['files'])-len(changed),
 'changes':['JSON parsed/decoded before exact shared string policy; well-formed redaction and unchanged-byte idempotence preserved.','Reviewed live Ignored/CompileError transitions and missing applicable identities fail; realistic complete/truncated report controls included.','Both Python suites required by explicit fail-fast Quality commands; local replay preserves native/publication/lineage/validator/matcher failures.','Worker captures true input digests before first normalization, aggregate verifies published bindings before validation, both safe records explicitly scanned/uploaded.','Finite95-case evidence catalog exposes methods/commands/executed-source/result hashes and retained locators; Root publication pending honestly.','Independent15-type/112-scalar inventory asserts equality participation, names public location/internal-constructor exclusions and keeps collision-permitting HashSet checks.'],
 'verification':{'publicationPythonTests':15,'triagePythonTests':18,'actionlintEditedWorkflows':2,'actualShellScenariosPassed':45,'modelHashBaselinePassed':1,'detectedFiniteLiterals':['13180','13382','13419','13163'],'normalAnalyzerBuildWarnings':0,'normalAnalyzerBuildErrors':0,'allOriginalObservationsPreserved':1498,'catalogControls':95,'declaredScalarFields':112},
 'retainedAttempts':{'python-attempt-01':'Two path-positive failures caught an over-broad structural raw-value check; corrected to scan normalized decoded strings, retaining unsafe JSON-key checks.','csharp-attempt-01':'Model baseline passed; control selector refused missing equality variant in problem-only ledger; rerun binds13163 to full original report.','python-attempt-02':'All15 publication and17 pre-catalog triage tests pass.','triage-catalog-attempt-03':'Final18 owner triage tests pass.','csharp-attempt-02':'Baseline plus three field-drop and equality-regression controls, then actual45 Shell cases complete.'},
 'cleanup':{'scratchRootsGone':[r['scratch'] for r in receipts],'processesRetainingOwnedTMPDIR':owned,'allOwnedChildrenWaited':True},
 'limits':['No full native mutation run, future native status, passing80/60 score, published Root evidence commit or personal review is claimed.','Original executed control sources are distinct from later preserved/current owner assertions; catalog publication remains pending.','No Git/remote/dependency/submodule/governance-authority mutation.']}
(BASE/'summary.json').write_text(json.dumps(summary,indent=2)+'\n');print(json.dumps({'changedOwnerFiles':len(files),'publicationTests':15,'triageTests':18,'shellScenarios':45,'modelBaseline':1,'finiteDetectedControls':4,'protectedInputs':summary['protectedPriorOwnerInputs'],'allScratchGone':True}))
