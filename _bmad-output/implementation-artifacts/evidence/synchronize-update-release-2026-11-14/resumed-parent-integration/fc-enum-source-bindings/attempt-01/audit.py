"""Read-only exact scope and original-assertion/authority preservation audit."""
import ast
import copy
import difflib
import hashlib
import json
from pathlib import Path
import subprocess

ROOT=Path(__file__).resolve().parents[6]
OWNER=ROOT/'references/Hexalith.FrontComposer'
PACKET=Path(__file__).resolve().parent
VALIDATOR='eng/validate-stryker-reports.ps1'
MANIFEST='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json'
TEST='tests/eng/test_mutation_triage.py'
EDITED=[VALIDATOR,MANIFEST,TEST]


def require(condition,message):
    if not condition:raise ValueError(message)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    inputs=json.loads((PACKET/'before-input-hashes.json').read_text())
    for path,digest in inputs.items():require(sha(PACKET/'before'/path)==digest,'Original packet changed: '+path)
    for path in inputs:
        if path not in EDITED:require(sha(OWNER/path)==inputs[path],'Protected reader/fixture/authority file changed: '+path)
    old=json.loads((PACKET/'before'/MANIFEST).read_text());current=json.loads((OWNER/MANIFEST).read_text())
    expected=copy.deepcopy(old);bindings=json.loads((PACKET/'canonical-enum-hashes.json').read_text())
    for row in expected['explicitExclusions']:
        if row['path'] in bindings:row['canonicalSourceSha256']=bindings[row['path']]
    require(current==expected,'Manifest changed beyond three canonical source bindings.')
    source_hashes={}
    for path,digest in bindings.items():
        raw=(OWNER/path).read_bytes();canonical=raw.decode('utf-8-sig').replace('\r\n','\n').replace('\r','\n').encode()
        require(hashlib.sha256(canonical).hexdigest()==digest,'Canonical source changed: '+path)
        source_hashes[path]={'raw_sha256':hashlib.sha256(raw).hexdigest(),'canonical_sha256':digest}
    old_validator=(PACKET/'before'/VALIDATOR).read_text();new_validator=(OWNER/VALIDATOR).read_text()
    name='function Test-ExplicitlyExcluded';following='function Get-MutatedFiles'
    require(old_validator.split(name,1)[0]==new_validator.split(name,1)[0],'Unrelated validator prefix changed.')
    require(old_validator.split(following,1)[1]==new_validator.split(following,1)[1],'Live/audit/BOTH/provenance/threshold or other validator behavior changed.')
    old_body=old_validator.split(name,1)[1].split(following,1)[0]
    new_body=new_validator.split(name,1)[1].split(following,1)[0]
    start=new_body.index('      $sourceBoundEnums = @(');end=new_body.index('      return $true',start)
    require(new_body[:start]+new_body[end:]==old_body,'Existing exclusion owner/rationale/wildcard behavior changed.')
    old_tree=ast.parse((PACKET/'before'/TEST).read_text());new_tree=ast.parse((OWNER/TEST).read_text())
    old_classes={node.name:node for node in old_tree.body if isinstance(node,ast.ClassDef)}
    new_classes={node.name:node for node in new_tree.body if isinstance(node,ast.ClassDef)}
    require(set(old_classes)==set(new_classes),'Unexpected test class change.')
    for name,node in old_classes.items():
        if name!='MutationInventoryTests':require(ast.dump(node)==ast.dump(new_classes[name]),'Existing triage assertions changed.')
    new_methods={node.name:node for node in new_classes['MutationInventoryTests'].body if isinstance(node,ast.FunctionDef)}
    for method in old_classes['MutationInventoryTests'].body:
        if not isinstance(method,ast.FunctionDef):continue
        candidate=copy.deepcopy(new_methods[method.name])
        if method.name=='testOnlyTheThreeImplicitEnumFilesReceiveFiniteInventoryAccounting':
            for node in ast.walk(candidate):
                if isinstance(node,ast.If):
                    node.body=[statement for statement in node.body if not
                        (isinstance(statement,ast.Assign) and any(isinstance(target,ast.Name) and target.id=='canonical' for target in statement.targets)) and not
                        (isinstance(statement,ast.Expr) and isinstance(statement.value,ast.Call) and isinstance(statement.value.func,ast.Attribute) and statement.value.func.attr=='assertEqual' and any(isinstance(child,ast.Constant) and child.value=='canonicalSourceSha256' for child in ast.walk(statement)))]
        require(ast.dump(method)==ast.dump(candidate),'Original inventory assertion/helper changed: '+method.name)
    incremental=b''
    for path in EDITED:
        incremental+=('diff --git a/'+path+' b/'+path+'\n').encode()
        incremental+=''.join(difflib.unified_diff((PACKET/'before'/path).read_text().splitlines(True),(OWNER/path).read_text().splitlines(True),fromfile='a/'+path,tofile='b/'+path)).encode()
    (PACKET/'enum-bindings.patch').write_bytes(incremental)
    tracked=[path for path in inputs if path!='tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml']
    complete=subprocess.check_output(['git','diff','--',*tracked],cwd=OWNER)
    fixture='tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml'
    complete+=('diff --git a/'+fixture+' b/'+fixture+'\nnew file mode 100644\n').encode()
    complete+=''.join(difflib.unified_diff([],(OWNER/fixture).read_text().splitlines(True),fromfile='/dev/null',tofile='b/'+fixture)).encode()
    (PACKET/'complete-owner-repairs.patch').write_bytes(complete)
    whitespace=subprocess.run(['git','diff','--check','--',*tracked],cwd=OWNER,capture_output=True,text=True)
    (PACKET/'source-whitespace.log').write_text(whitespace.stdout+whitespace.stderr)
    require(whitespace.returncode==0,'Whitespace check failed.')
    for path in inputs:require((OWNER/path).stat().st_size<1048576,'Owner file exceeds1MiB: '+path)
    summary=json.loads((PACKET/'attempt-01/summary.json').read_text())
    require(summary['counts']=={'tests_run':8,'subcases':13,'failures':0,'errors':0,'skipped':0,'success':True},'Focused result mismatch.')
    result={'status':'passed','edited_files':{path:{'sha256':sha(OWNER/path),'bytes':(OWNER/path).stat().st_size} for path in EDITED},
            'protected_files_unchanged':{path:inputs[path] for path in inputs if path not in EDITED},'canonical_sources':source_hashes,
            'original_assertions_and_other_exclusions_preserved':True,'live_counts_audits_both_provenance_threshold_logic_verbatim':True,
            'tests':summary['counts'],'actual_pwsh_invocations':18,'counterexample_before_after':[0,1],
            'incremental_patch':{'bytes':len(incremental),'sha256':hashlib.sha256(incremental).hexdigest()},
            'complete_seven_repair_file_patch':{'bytes':len(complete),'sha256':hashlib.sha256(complete).hexdigest()},
            'cleanup':'Owned run-with-temp session56108 exited0 after all actual PowerShell children completed; its private archive and all synthetic fixture workspaces were removed. This source audit is read-only.',
            'qualification':'Finite real entry-point controls on synthetic report data only; no raw score, new private provenance, current-source native qualification or transfer of historical11c results.'}
    with (PACKET/'handoff.json').open('x') as output:output.write(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'status':'passed','incremental_patch':result['incremental_patch'],'complete_patch':result['complete_seven_repair_file_patch'],'handoff_sha256':sha(PACKET/'handoff.json')}))


if __name__=='__main__':main()
