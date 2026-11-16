"""Read-only final binding and cleanup audit; no Git/source mutation."""
from pathlib import Path
import hashlib,json,os,subprocess,time
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
WORKFLOW='.github/workflows/mutation-property-nightly.yml'
BEFORE=json.loads((BASE/'before-input-hashes.json').read_text())
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
current={name:sha(OWNER/name) for name in BEFORE['files'] if (OWNER/name).is_file()}
changed=[name for name,value in BEFORE['files'].items() if current.get(name)!=value]
# The parent concurrently authored its MetadataDiagnosticContractTests file.
# Preserve both observations; do not attribute that change to this workflow patch.
parent_path='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/MetadataDiagnosticContractTests.cs'
assert set(changed)<={WORKFLOW,parent_path},changed
assert (OWNER/WORKFLOW).read_bytes()==(BASE/'proposed-mutation-property-nightly.yml').read_bytes()
head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()
assert head==BEFORE['ownerHead']
tracked_modified=subprocess.check_output(['git','diff','--name-only'],cwd=OWNER,text=True).splitlines()
assert tracked_modified==[WORKFLOW],tracked_modified
attempts={name:json.loads((BASE/name/'verification.json').read_text()) for name in ['attempt-01','attempt-02']}
for receipt in attempts.values(): assert not Path(receipt['scratch']).exists()
# No direct or reparented job process should retain either unique private root.
owned=[]
for entry in Path('/proc').iterdir():
    if not entry.name.isdigit() or int(entry.name)==os.getpid(): continue
    try:
        env=(entry/'environ').read_bytes().split(b'\0')
        if any(('TMPDIR='+receipt['scratch']).encode() in env for receipt in attempts.values()):owned.append(int(entry.name))
    except (OSError,PermissionError):pass
assert not owned,owned
first=attempts['attempt-01'];second=attempts['attempt-02']
assert first['commands'][0]['exitCode']==2
assert all(row['exitCode']==0 for row in first['commands'][1:])
assert second['exitCode']==0
protected={name:value for name,value in current.items() if name not in [WORKFLOW,parent_path]}
parent_tests={name:value for name,value in current.items() if name in BEFORE['untracked']}
summary={
    'scope':'CI-only lifetime correction; no mutation score, hosted qualification or owner acceptance',
    'ownerHead':head,'workflowSha256':current[WORKFLOW],
    'trackedOwnerFilesChangedByTask':[WORKFLOW],
    'workflowChange':'Fixed happy-path/error-handling matrix, fail-fast false and separate360m lifetimes; independent strict BOTH aggregate remains source-tools-mutation',
    'verification':{'actionlintVersion':'1.7.12','actionlintExit':0,'workflowTests':6,'structuralAdverseControls':12,'actualResultShellCases':5,
                    'existingNativeProvenanceContractTests':18,'layoutTransportSegments':2,
                    'failedAttemptRetained':'attempt-01/actionlint.log (invalid -color=never CLI argument; tooling invocation refusal, no workflow finding)',
                    'successfulNarrowRetry':'attempt-02/verification.json'},
    'preservation':{'protectedFilesByteIdentical':len(protected),'parentUntrackedTestFileCount':len(parent_tests),
                    'parentConcurrentChange':{'path':parent_path,'beforeSha256':BEFORE['files'][parent_path],'afterSha256':current[parent_path],
                    'attribution':'Parent-authored concurrent test change; this task never wrote test source'},
                    'lifecycleAndHeaderBytesIdentical':True,'helpersConfigsToolPolicyUnchanged':True,'parentTestHashes':parent_tests},
    'cleanup':{'scratchPathsGone':[receipt['scratch'] for receipt in attempts.values()],'processesRetainingJobTMPDIR':owned,'allCommandsWaited':True},
    'limits':['Actual new hosted jobs must still pass exact independent runtime/receipt equality and BOTH report validation; the prior proof establishes clean same-path rebuild repeatability only.',
              'The existing score failure and untriaged mutants are unresolved by job lifetimes; thresholds80/60, predicates, cohorts and source/tool policy remain unchanged.',
              'Local artifact control checks finite paths and byte transport only; it does not execute GitHub upload/download services.',
              'No full mutation rerun, remote operation, owner Git mutation, authority or accepted triage edit was performed.'],
    'finishedUtc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())}
(BASE/'after-input-hashes.json').write_text(json.dumps({'ownerHead':head,'files':current},indent=2)+'\n')
(BASE/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
diff=subprocess.check_output(['git','diff','--',WORKFLOW],cwd=OWNER)
(BASE/'private-workflow-lifetimes.patch').write_bytes(diff)
print(json.dumps({'workflowSha256':current[WORKFLOW],'protectedFileCount':len(protected),'parentFilesPreserved':len(parent_tests),'scratchPathsGone':True,'trackedEdits':tracked_modified}))
