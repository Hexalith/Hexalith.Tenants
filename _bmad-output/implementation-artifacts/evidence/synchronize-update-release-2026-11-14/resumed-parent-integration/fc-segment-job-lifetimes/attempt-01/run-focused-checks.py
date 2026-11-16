"""One disposable workflow-only verification job; no full mutation or hosted qualification."""
from pathlib import Path
import hashlib
import json
import os
import shutil
import subprocess
import time

BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
ATTEMPT=BASE/'attempt-01'
ATTEMPT.mkdir(exist_ok=False)
SCRATCH=Path(os.environ['TMPDIR'])
env={**os.environ,'PYTHONDONTWRITEBYTECODE':'1','PYTHONPYCACHEPREFIX':str(SCRATCH/'pycache'),'WORKFLOW_ATTEMPT':str(ATTEMPT)}
record={'scope':__doc__,'scratch':str(SCRATCH),'commands':[],'runnerSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}

def run(name,args,cwd=OWNER):
    start=time.monotonic()
    with (ATTEMPT/(name+'.log')).open('wb') as log:
        process=subprocess.run(args,cwd=cwd,env=env,stdout=log,stderr=subprocess.STDOUT)
    row={'name':name,'command':args,'cwd':str(cwd),'exitCode':process.returncode,'seconds':round(time.monotonic()-start,3),
         'logSha256':hashlib.sha256((ATTEMPT/(name+'.log')).read_bytes()).hexdigest()}
    record['commands'].append(row)
    (ATTEMPT/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
    print(json.dumps(row),flush=True)
    return process.returncode

run('actionlint',['actionlint','-color=never','.github/workflows/mutation-property-nightly.yml'])
run('workflow-contract',['python3','-B',str(BASE/'test_workflow_contract.py')])
# Existing finite native contract/source/runtime refusal controls. Synthetic runtime
# fixtures are explicitly tagged by the original tests; no compiled-tool claim.
run('existing-native-provenance-contracts',['python3','-B','-c', '''import importlib.util,unittest
from pathlib import Path
spec=importlib.util.spec_from_file_location("existing_contracts",Path("tests/eng/test_stryker_compat.py"))
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
names=[name for name in unittest.defaultTestLoader.getTestCaseNames(module.ContractTests) if name!="testPreparationOwnsAndReapsChildrenAcrossExitTimeoutAndCancellation"]
suite=unittest.TestSuite(module.ContractTests(name) for name in names)
result=unittest.TextTestRunner(verbosity=2).run(suite)
raise SystemExit(not result.wasSuccessful())'''])
# Test finite artifact selection and byte-preserving local transport. These files
# are a layout fixture, not mutation reports or a GitHub upload implementation.
layout=SCRATCH/'artifact-layout'; transported=SCRATCH/'aggregate-layout'
layout.mkdir();transported.mkdir()
from glob import glob
selected={}
for segment in ['happy-path','error-handling']:
    segment_root=layout/'artifacts/mutation'/segment
    files=['reports/source-tools-'+segment+'.json','reports/source-tools-'+segment+'.html','tool-provenance.json',
           'runtime/Stryker.CLI.dll','full-console.log','generated/source.cs','reports/unbounded.json']
    for name in files:
        path=segment_root/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(('layout fixture '+segment+'/'+name).encode())
    patterns=['**/reports/source-tools-'+segment+'.json','**/reports/source-tools-'+segment+'.html','tool-provenance.json']
    matched=set()
    for pattern in patterns: matched.update(Path(value) for value in glob(str(segment_root/pattern),recursive=True))
    assert {str(path.relative_to(segment_root)) for path in matched}==set(files[:3])
    for path in matched:
        target=transported/'artifacts/mutation'/segment/path.relative_to(segment_root)
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
        assert target.read_bytes()==path.read_bytes()
    selected[segment]=sorted(str(path.relative_to(segment_root)) for path in matched)
record['artifactLayout']={'kind':'local finite layout/byte transport control; not actual actions upload','selected':selected,'runtimeAndLogsExcluded':True,'bytesPreserved':True}
# Preserve and cite, never rewrite, the prior actual two-build evidence.
proof=BASE.parent/'fc-private-tool-rebuild-proof'
first=(proof/'first-build-provenance.json').read_bytes();second=(proof/'second-build-provenance.json').read_bytes()
assert first==second
record['independentPreparationPrecedent']={'scope':'Existing actual same-path clean rebuild proof only; separate hosted-job equality remains pending',
    'firstSha256':hashlib.sha256(first).hexdigest(),'secondSha256':hashlib.sha256(second).hexdigest(),
    'priorVerificationSha256':hashlib.sha256((proof/'verification.json').read_bytes()).hexdigest()}
record['finishedUtc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())
record['allChecksPassed']=all(row['exitCode']==0 for row in record['commands'])
(ATTEMPT/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
raise SystemExit(not record['allChecksPassed'])
