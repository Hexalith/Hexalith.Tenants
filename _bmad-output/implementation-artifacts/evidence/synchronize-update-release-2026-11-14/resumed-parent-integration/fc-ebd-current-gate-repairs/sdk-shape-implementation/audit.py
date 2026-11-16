"""Read-only scope, authority and exact preserved-path audit for the bounded SDK reader patch."""
import ast
import difflib
import hashlib
import json
from pathlib import Path
import subprocess

PACKET=Path(__file__).resolve().parent
ROOT=Path(__file__).resolve().parents[7]
OWNER=ROOT/'references/Hexalith.FrontComposer'
READER='eng/dependency_graph.py'
TESTS='tests/eng/test_dependency_graph.py'
FIXTURE='tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml'
NEW=[READER,TESTS,FIXTURE]


def require(condition,message):
    if not condition:raise ValueError(message)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def functions(source):
    return {node.name:node for node in ast.parse(source).body if isinstance(node,(ast.FunctionDef,ast.ClassDef))}


def main():
    before=json.loads((PACKET/'before-input-hashes.json').read_text())
    for path,digest in before.items():require(sha(PACKET/'before'/path)==digest,'Original source packet changed: '+path)
    protected=[path for path in before if path not in (READER,TESTS)]
    for path in protected:require(sha(OWNER/path)==before[path],'Protected prior patch/fixture changed: '+path)
    old_reader=(PACKET/'before'/READER).read_text();new_reader=(OWNER/READER).read_text()
    old_nodes=functions(old_reader);new_nodes=functions(new_reader)
    allowed={'_current_sdk42_qualification','_inactive_eventstore_sdk42_override','assert_selected_catalog_property_shape'}
    require(set(old_nodes)==set(new_nodes),'Unexpected top-level helper change.')
    for name,node in old_nodes.items():
        if name not in allowed:require(ast.dump(node)==ast.dump(new_nodes[name]),'Unrelated reader function changed: '+name)
    old_legacy=ast.get_source_segment(old_reader,old_nodes['_inactive_eventstore_sdk42_override']).split('    condition = (',1)[1]
    new_legacy=ast.get_source_segment(new_reader,new_nodes['_inactive_eventstore_sdk42_override']).split('    condition = (',1)[1]
    require(old_legacy==new_legacy,'Older two-selector path changed.')
    old_proof=ast.get_source_segment(old_reader,old_nodes['_current_sdk42_qualification']).split('        definitions = ',1)[1]
    new_proof=ast.get_source_segment(new_reader,new_nodes['_current_sdk42_qualification']).split('        definitions = ',1)[1]
    require(old_proof==new_proof,'Shared role/order/import/alias/shape proof changed.')
    def assignments(source):
        return {node.targets[0].id:node.value for node in ast.parse(source).body if isinstance(node,ast.Assign) and len(node.targets)==1 and isinstance(node.targets[0],ast.Name)}
    old_assign=assignments(old_reader);new_assign=assignments(new_reader)
    require(set(new_assign)-set(old_assign)=={'_SDK42_PACKAGE_CURRENT_CONDITION'},'Unexpected reader constants added.')
    for name,value in old_assign.items():require(ast.dump(value)==ast.dump(new_assign[name]),'Frozen constant/role set changed: '+name)
    old_tests=functions((PACKET/'before'/TESTS).read_text());new_tests=functions((OWNER/TESTS).read_text())
    require(set(old_tests)==set(new_tests),'Existing test class changed.')
    old_count=0
    for name,node in old_tests.items():
        if name!='PolicyShapeTests':
            require(ast.dump(node)==ast.dump(new_tests[name]),'Unrelated test class changed: '+name)
            continue
        methods={child.name:child for child in new_tests[name].body if isinstance(child,ast.FunctionDef)}
        for method in node.body:
            if isinstance(method,ast.FunctionDef):
                require(ast.dump(method)==ast.dump(methods[method.name]),'Existing test/helper changed: '+method.name)
                old_count+=1
    changed=set(subprocess.check_output(['git','diff','--name-only'],cwd=OWNER,text=True).splitlines())
    expected={READER,TESTS,'eng/dependency-graph-policy.json','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json','tests/eng/test_mutation_triage.py'}
    require(changed==expected,'Unexpected tracked changes: '+repr(changed))
    untracked=subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=OWNER,text=True).splitlines()
    require(untracked==[FIXTURE],'Unexpected untracked owner inputs: '+repr(untracked))
    for path in changed|{FIXTURE}:require((OWNER/path).stat().st_size<1048576,'Owner file exceeds1MiB: '+path)
    fixture=(OWNER/FIXTURE).read_bytes()
    require(FIXTURE.endswith('.xml'),'Fixture must remain non-MSBuild test data.')
    addition=('diff --git a/'+FIXTURE+' b/'+FIXTURE+'\nnew file mode 100644\n').encode()
    addition+=''.join(difflib.unified_diff([],fixture.decode().replace('\r\n','\n').splitlines(True),fromfile='/dev/null',tofile='b/'+FIXTURE)).encode()
    complete=subprocess.check_output(['git','diff','--',*sorted(changed)],cwd=OWNER)+addition
    incremental=subprocess.check_output(['git','diff','--',READER,TESTS],cwd=OWNER)+addition
    (PACKET/'complete-owner.patch').write_bytes(complete);(PACKET/'sdk-reader.patch').write_bytes(incremental)
    whitespace=subprocess.run(['git','diff','--check','--',*sorted(changed)],cwd=OWNER,capture_output=True,text=True)
    (PACKET/'source-whitespace.log').write_text(whitespace.stdout+whitespace.stderr)
    require(whitespace.returncode==0,'Source whitespace check failed.')
    counts=json.loads((PACKET/'attempt-01/focused-counts.json').read_text())
    require(counts['tests_run']==36 and counts['subcases']==374 and counts['success'] and counts['skipped']==0,'Focused counts mismatch.')
    result={'status':'passed','base_source':'ebd3f76dc0b320d455917c593ff86526b58d0c77',
            'incremental_files':NEW,'source_files':{path:{'sha256':sha(OWNER/path),'bytes':(OWNER/path).stat().st_size} for path in sorted(changed|{FIXTURE})},
            'protected_files_unchanged':{path:before[path] for path in protected},'existing_policyshape_methods_unchanged':old_count,
            'older_two_selector_path_verbatim':True,'shared_role_order_import_alias_shape_proof_verbatim':True,'frozen_constants_and_role_sets_unchanged':True,
            'focused':counts,'complete_patch':{'bytes':len(complete),'sha256':hashlib.sha256(complete).hexdigest()},
            'incremental_patch':{'bytes':len(incremental),'sha256':hashlib.sha256(incremental).hexdigest()},
            'selected65fd_before_exit':1,'selected65fd_after_exit':0,'retained11c_exit':0,
            'cleanup':'Owned run-with-temp session38720 exited0; all validation/test children completed before its private workspace and synthetic test fixtures were removed. Existing owner/sibling Git state was not mutated.',
            'remaining':'Parent must land the combined source seal, use a separate documentation-only activation push with that seal as actual immediate base, and obtain fresh committed-source normal/native/publication gates.'}
    with (PACKET/'handoff.json').open('x') as output:output.write(json.dumps(result,indent=2)+'\n')
    print(json.dumps({'status':'passed','complete_patch':result['complete_patch'],'incremental_patch':result['incremental_patch'],'handoff_sha256':sha(PACKET/'handoff.json')}))


if __name__=='__main__':main()
