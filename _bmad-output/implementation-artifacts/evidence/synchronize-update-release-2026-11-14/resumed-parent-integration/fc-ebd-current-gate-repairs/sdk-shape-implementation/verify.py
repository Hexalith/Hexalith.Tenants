"""Focused static reader controls and exact-object real selected-catalog before/after proof."""
import ast
import hashlib
import io
import json
import os
from pathlib import Path
import runpy
import shutil
import subprocess
import sys
import tarfile

PACKET=Path(__file__).resolve().parent
ROOT=Path(__file__).resolve().parents[7]
OWNER=ROOT/'references/Hexalith.FrontComposer'
CANDIDATE='ebd3f76dc0b320d455917c593ff86526b58d0c77'
HISTORICAL='11c8377a858a756c4e4069bf08e62db1f58e1733'
READER='eng/dependency_graph.py'
TESTS='tests/eng/test_dependency_graph.py'
FIXTURE='tests/ci-governance/fixtures/builds-sdk42-package-511-qualification.xml'
PROTECTED=['eng/dependency-graph-policy.json','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json','tests/eng/test_mutation_triage.py']


def require(condition,message):
    if not condition:raise ValueError(message)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    attempt=PACKET/(sys.argv[1] if len(sys.argv)==2 else 'attempt-01');attempt.mkdir(exist_ok=False)
    previous=runpy.run_path(str(PACKET.parent/'verify.py'))
    read_git=previous['read_git'];private_store=previous['private_store']
    scratch=Path(os.environ['TMPDIR'])/'owner';scratch.mkdir()
    archive=read_git(OWNER,'archive',CANDIDATE)
    with tarfile.open(fileobj=io.BytesIO(archive)) as bundle:bundle.extractall(scratch,filter='data')
    for relative in [*PROTECTED,READER,TESTS,FIXTURE]:
        (scratch/relative).write_bytes((OWNER/relative).read_bytes())
    policy=json.loads((scratch/PROTECTED[0]).read_text())
    stores=[private_store(scratch,OWNER,CANDIDATE,'github.com/hexalith/hexalith.frontcomposer')]
    for row in policy['trusted_identities']:
        if row['local_path']=='.':continue
        name=row['local_path'].split('/')[-1]
        source=ROOT if name=='Hexalith.Tenants' else Path('/home/administrator/projects/hexalith/parties') if name=='Hexalith.Parties' else ROOT/'references'/name
        stores.append(private_store(scratch/row['local_path'],source,read_git(source,'rev-parse','HEAD').decode().strip(),row['identity']))
    builds=scratch/'references/Hexalith.Builds';selected=policy['consumer_source_projections']['github.com/hexalith/hexalith.platform']['builds_commit']
    (builds/'.git/HEAD').write_text(selected+'\n')
    (builds/'.gitattributes').write_bytes(read_git(ROOT/'references/Hexalith.Builds','show',selected+':.gitattributes'))
    catalog=builds/'Props/Directory.Packages.props';catalog.parent.mkdir()
    catalog.write_bytes(read_git(builds,'cat-file','--filters',selected+':Props/Directory.Packages.props'))
    (attempt/'input-bindings.json').write_text(json.dumps({'source':CANDIDATE,'archive_sha256':hashlib.sha256(archive).hexdigest(),'selected_builds':selected,'source_files':{relative:sha(OWNER/relative) for relative in [*PROTECTED,READER,TESTS,FIXTURE]},'stores':stores},indent=2)+'\n')
    env={**os.environ,'PYTHONDONTWRITEBYTECODE':'1','GIT_OPTIONAL_LOCKS':'0','GIT_TERMINAL_PROMPT':'0'}
    commands=[]
    def command(name,arguments,expected=0,contains=None):
        log=attempt/(name+'.log')
        with log.open('x') as output:result=subprocess.run(arguments,cwd=scratch,env=env,stdout=output,stderr=subprocess.STDOUT)
        commands.append({'name':name,'command':arguments,'exit_code':result.returncode,'expected_exit':expected,'log':log.name,'log_sha256':sha(log)})
        (attempt/'commands.json').write_text(json.dumps(commands,indent=2)+'\n')
        require(result.returncode==expected,name+' unexpected exit; see '+str(log))
        if contains:require(contains in log.read_text(),name+' missing expected refusal.')
        print(name+': '+str(result.returncode),flush=True)
    # Same committed65fd catalog and same prospective source seal, changing only
    # the reader bytes between these actual CLI calls. Retain the old refusal.
    current_reader=(scratch/READER).read_bytes()
    (scratch/READER).write_bytes((PACKET/'before'/READER).read_bytes())
    command('selected65fd-before-refusal',['python3',READER,'--policy',str(scratch/PROTECTED[0]),'validate','--commit',CANDIDATE],1,"found 2 values ['4.2.0', '5.1.1']")
    (scratch/READER).write_bytes(current_reader)
    command('selected65fd-after-success',['python3',READER,'--policy',str(scratch/PROTECTED[0]),'validate','--commit',CANDIDATE])
    old_policy=scratch/'retained-11c-policy.json';old_policy.write_bytes((PACKET.parent/'before'/PROTECTED[0]).read_bytes())
    command('retained11c-old-shape-success',['python3',READER,'--policy',str(old_policy),'validate','--commit',HISTORICAL])
    tree=ast.parse((scratch/TESTS).read_text());cls=next(node for node in tree.body if isinstance(node,ast.ClassDef) and node.name=='PolicyShapeTests')
    methods=[node.name for node in cls.body if isinstance(node,ast.FunctionDef) and node.name.startswith('test_') and any(word in node.name for word in ('sdk42','sdk511','eventstore_qualification','tenants8'))]
    runner=Path(os.environ['TMPDIR'])/'focused.py'
    runner.write_text('''import importlib.util,json,sys,unittest
from pathlib import Path
owner=Path(sys.argv[1]);sys.path.insert(0,str(owner/'tests/eng'))
spec=importlib.util.spec_from_file_location('focused_graph_tests',owner/'tests/eng/test_dependency_graph.py')
module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
names=json.loads(sys.argv[3]);suite=unittest.TestSuite(module.PolicyShapeTests(name) for name in names)
class Result(unittest.TextTestResult):
 def __init__(self,*args,**kwargs):super().__init__(*args,**kwargs);self.subcases=0
 def addSubTest(self,test,subtest,error):self.subcases+=1;super().addSubTest(test,subtest,error)
result=unittest.TextTestRunner(verbosity=2,resultclass=Result).run(suite)
Path(sys.argv[2]).write_text(json.dumps({'methods':names,'tests_run':result.testsRun,'subcases':result.subcases,'failures':len(result.failures),'errors':len(result.errors),'skipped':len(result.skipped),'success':result.wasSuccessful()},indent=2)+'\\n')
raise SystemExit(not result.wasSuccessful())
''')
    command('focused-sdk42-tenants-controls',['python3',str(runner),str(scratch),str(attempt/'focused-counts.json'),json.dumps(methods)])
    for relative in PROTECTED:
        require(sha(OWNER/relative)==sha(PACKET/'before'/relative),'Protected prior patch changed: '+relative)
    (attempt/'summary.json').write_text(json.dumps({'status':'passed','source':CANDIDATE,'commands':commands,'focused':json.loads((attempt/'focused-counts.json').read_text()),'scope':'Static FrontComposer reader proof only. Historical/current package selection does not confer runtime, producer tuple or future native qualification.','authority':'Protected policy, enum accounting, prior inventory controls and historical fixtures unchanged.'},indent=2)+'\n')
    print(json.dumps({'status':'passed','focused':json.loads((attempt/'focused-counts.json').read_text())}),flush=True)


if __name__=='__main__':main()
