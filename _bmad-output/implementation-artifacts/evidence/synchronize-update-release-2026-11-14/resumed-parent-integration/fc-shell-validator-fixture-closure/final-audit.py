"""Final read-only source/count/cleanup binding for the actual Shell fixture class."""
from pathlib import Path
import difflib,hashlib,json,os,subprocess,time,xml.etree.ElementTree as ET
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
name='tests/Hexalith.FrontComposer.Shell.Tests/Governance/MutationReportValidationTests.cs'
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
before=json.loads((BASE/'before-input-hashes.json').read_text());receipt=json.loads((BASE/'attempt-01/verification.json').read_text())
changed=[p for p,h in before['files'].items() if not (OWNER/p).is_file() or sha(OWNER/p)!=h]
assert changed==[name],changed
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==before['head']
assert sha(OWNER/name)==receipt['overlays'][name]
assert all(row['exitCode']==0 for row in receipt['commands'])
assert not Path(receipt['scratch']).exists()
owned=[]
for proc in Path('/proc').iterdir():
 if not proc.name.isdigit() or int(proc.name)==os.getpid():continue
 try:
  if ('TMPDIR='+receipt['scratch']).encode() in (proc/'environ').read_bytes().split(b'\0'):owned.append(int(proc.name))
 except OSError:pass
assert not owned,owned
ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
trx=ET.parse(BASE/'attempt-01/test-results/tests.trx').getroot();counters=trx.find('.//t:Counters',ns).attrib
rows=trx.findall('.//t:UnitTestResult',ns);assert len(rows)==45 and all(row.attrib['outcome']=='Passed' for row in rows)
assert counters['notExecuted']=='0'
build=(BASE/'attempt-01/shell-build.log').read_text();assert '0 Warning(s)' in build and '0 Error(s)' in build
original=(BASE/'before-MutationReportValidationTests.cs').read_bytes();updated=(OWNER/name).read_bytes()
line=b'            File.Copy(Path.Combine(repository, "eng/mutation-artifact-policy.ps1"), Path.Combine(fixture, "eng/mutation-artifact-policy.ps1"));\r\n'
assert updated.count(line)==1 and updated.replace(line,b'')==original
(BASE/'final-MutationReportValidationTests.cs').write_bytes(updated)
(BASE/'fixture-closure.patch').write_text(''.join(difflib.unified_diff(original.decode().replace('\r\n','\n').splitlines(keepends=True),updated.decode().replace('\r\n','\n').splitlines(keepends=True),fromfile='a/'+name,tofile='b/'+name)))
parent_additions={p:sha(OWNER/p) for p in ['tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/CountingModelComparer.cs','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/ModelHashFieldParticipationTests.cs']}
summary={'scope':'One-line existing Shell fixture closure repair; actual class45/45; no wider verification',
 'ownerHead':before['head'],'changedFile':name,'changedFileSha256':sha(OWNER/name),'originalBytesAndAllAssertionsPreserved':True,
 'policyModuleSha256':sha(OWNER/'eng/mutation-artifact-policy.ps1'),'otherCopyCallersFound':0,'callerSearch':'script-callers.log',
 'frozenInputs':receipt['archives'],'workingOverlayHashes':'attempt-01/verification.json',
 'verification':{'actualShellClass':'Hexalith.FrontComposer.Shell.Tests.Governance.MutationReportValidationTests','passed':45,'failed':0,'skipped':0,'buildWarnings':0,'buildErrors':0,'normalAnalyzers':True,'configuration':'Release','externalDependencyMode':'NuGet packages','nativePowerShellUsedByExistingFixture':True,'trx':'attempt-01/test-results/tests.trx'},
 'preservation':{'preexistingOwnerInputFilesByteIdentical':len(before['files'])-1,'parentAuthorizedNewFiles':parent_additions,'parentNewFilesBoundary':'Added after private snapshot; preserved and excluded from this Shell45 scope, no broader-source qualification inferred'},
 'cleanup':{'scratchRemoved':receipt['scratch'],'processesRetainingOwnedTMPDIR':owned,'allCommandsWaited':True},
 'limits':['No extra test class, Python publication test, function-equivalence substitute, source-wide mutation or hosted qualification was run/claimed.','No owner Git/remote/submodule/authority mutation occurred.'],
 'finishedUtc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())}
(BASE/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({'actualClassPassed':45,'failed':0,'skipped':0,'warnings':0,'errors':0,'otherInputsPreserved':len(before['files'])-1,'scratchGone':True}))
