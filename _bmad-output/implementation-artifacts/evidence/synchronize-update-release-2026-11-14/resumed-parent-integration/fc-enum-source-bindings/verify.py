"""Distinguishing real PowerShell entry-point controls on synthetic finite report fixtures."""
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import subprocess
import sys
import tarfile
import unittest

ROOT=Path(__file__).resolve().parents[6]
OWNER=ROOT/'references/Hexalith.FrontComposer'
PACKET=Path(__file__).resolve().parent
VALIDATOR='eng/validate-stryker-reports.ps1'
MANIFEST='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json'
TEST='tests/eng/test_mutation_triage.py'


def require(condition,message):
    if not condition:raise ValueError(message)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    attempt=PACKET/(sys.argv[1] if len(sys.argv)==2 else 'attempt-01');attempt.mkdir(exist_ok=False)
    scratch=Path(os.environ['TMPDIR'])/'owner';scratch.mkdir()
    archive=subprocess.check_output(['git','archive','ebd3f76dc0b320d455917c593ff86526b58d0c77'],cwd=OWNER)
    with tarfile.open(fileobj=io.BytesIO(archive)) as bundle:bundle.extractall(scratch,filter='data')
    for relative in (VALIDATOR,MANIFEST,TEST):(scratch/relative).write_bytes((OWNER/relative).read_bytes())
    os.environ['PYTHONDONTWRITEBYTECODE']='1'
    spec=importlib.util.spec_from_file_location('inventory_controls',scratch/TEST);module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    actual_run=subprocess.run;calls=[]
    def native_run(arguments,*args,**kwargs):
        if arguments[0]!='pwsh':return actual_run(arguments,*args,**kwargs)
        fixture=Path(kwargs['cwd'])
        input_hashes={relative:sha(fixture/relative) for relative in ['manifest.json',*sorted(str(path.relative_to(fixture)) for path in (fixture/'reports').glob('source-tools-*.json'))]}
        sources={str(path.relative_to(fixture)):sha(path) for path in (fixture/module.MutationInventoryTests.ENUM_ROOT).glob('*.cs')}
        result=actual_run(arguments,*args,**kwargs);index=len(calls)+1;label=f'pwsh-{index:02}'
        log=attempt/(label+'.log');log.write_text(result.stdout+'\n'+result.stderr)
        audit=fixture/'reports/mutation-report-audit.json'
        item={'command':arguments,'exit_code':result.returncode,'log':log.name,'log_sha256':sha(log),'validator_sha256':sha(fixture/VALIDATOR),'input_sha256':input_hashes,'source_raw_sha256':sources,
              'scope':'Actual PowerShell validator on synthetic report inputs; not a native Stryker run/provenance or mutation score.'}
        if audit.exists():
            retained=attempt/(label+'-audit.json');retained.write_bytes(audit.read_bytes());item['audit']=retained.name;item['audit_sha256']=sha(retained)
        calls.append(item);(attempt/'native-calls.json').write_text(json.dumps(calls,indent=2)+'\n')
        return result
    module.subprocess.run=native_run
    case=module.MutationInventoryTests()
    baseline=[]
    for version in ('before','after'):
        fixture=Path(os.environ['TMPDIR'])/('same-path-'+version);fixture.mkdir();case.fixture(fixture)
        if version=='before':(fixture/VALIDATOR).write_bytes((PACKET/'before'/VALIDATOR).read_bytes())
        path=fixture/case.ENUM_ROOT/(case.ENUM_NAMES[0]+'.cs')
        path.write_text('namespace Fixture; public enum '+case.ENUM_NAMES[0]+' { One = 1, Two, }\n')
        result,refusal=case.validate(fixture)
        baseline.append({'version':version,'exit_code':result.returncode,'manifest_sha256':sha(fixture/'manifest.json'),'changed_source_sha256':sha(path),'call':len(calls),'refusal':refusal})
        require(result.returncode==(0 if version=='before' else 1),'Same-path source binding control did not distinguish readers.')
        if version=='after':require('Canonical source hash mismatch for finite enum exclusion' in refusal,'Missing source-binding diagnostic.')
    require(baseline[0]['manifest_sha256']==baseline[1]['manifest_sha256'] and baseline[0]['changed_source_sha256']==baseline[1]['changed_source_sha256'],'Counterexample inputs differ.')
    require(calls[0]['input_sha256']==calls[1]['input_sha256'],'Retained report/manifest inputs differ between readers.')
    (attempt/'same-path-before-after.json').write_text(json.dumps(baseline,indent=2)+'\n')
    class Result(unittest.TextTestResult):
        def __init__(self,*args,**kwargs):super().__init__(*args,**kwargs);self.subcases=0
        def addSubTest(self,test,subtest,error):self.subcases+=1;super().addSubTest(test,subtest,error)
    suite=unittest.defaultTestLoader.loadTestsFromTestCase(module.MutationInventoryTests)
    log=attempt/'focused-tests.log'
    with log.open('x') as output:result=unittest.TextTestRunner(stream=output,verbosity=2,resultclass=Result).run(suite)
    counts={'tests_run':result.testsRun,'subcases':result.subcases,'failures':len(result.failures),'errors':len(result.errors),'skipped':len(result.skipped),'success':result.wasSuccessful()}
    (attempt/'focused-counts.json').write_text(json.dumps(counts,indent=2)+'\n')
    require(result.wasSuccessful(),'Focused entry-point controls failed; see '+str(log))
    summary={'status':'passed','counts':counts,'actual_pwsh_invocations':len(calls),'source_files_sha256':{relative:sha(OWNER/relative) for relative in (VALIDATOR,MANIFEST,TEST)},
             'before_after_exits':[item['exit_code'] for item in baseline],
             'limits':'Synthetic finite report fixtures exercise actual PowerShell admission only. Compatibility provenance switch/strict BOTH behavior remain unchanged. No raw mutation score, current-source native qualification or historical report reassignment is claimed.'}
    (attempt/'summary.json').write_text(json.dumps(summary,indent=2)+'\n')
    print(json.dumps(summary),flush=True)


if __name__=='__main__':main()
