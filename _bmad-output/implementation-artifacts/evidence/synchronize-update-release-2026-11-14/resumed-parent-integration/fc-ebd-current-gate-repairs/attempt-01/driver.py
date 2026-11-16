"""Private read-only-object controls for finite enum inventory and the prospective source seal."""
import copy
import hashlib
import importlib.util
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import zipfile

ROOT=Path(__file__).resolve().parents[6]
OWNER=ROOT/'references/Hexalith.FrontComposer'
PACKET=Path(__file__).resolve().parent
CANDIDATE='ebd3f76dc0b320d455917c593ff86526b58d0c77'
BASE='33e71036c04f2b99955376e9c90cbb1fc3a81e64'
HISTORICAL='11c8377a858a756c4e4069bf08e62db1f58e1733'
POLICY='eng/dependency-graph-policy.json'
MANIFEST='tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json'
IDENTITY='github.com/hexalith/hexalith.frontcomposer'
PLATFORM='github.com/hexalith/hexalith.platform'


def require(condition,message):
    if not condition:raise ValueError(message)


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def read_git(source,*arguments):return subprocess.check_output(['git',*arguments],cwd=source)


def private_store(destination,source,head,identity):
    objects=Path(read_git(source,'rev-parse','--path-format=absolute','--git-path','objects').decode().strip())
    require(objects.is_dir(),'Missing approved object store: '+str(source))
    metadata=destination/'.git'
    (metadata/'objects/info').mkdir(parents=True)
    (metadata/'refs').mkdir()
    (metadata/'HEAD').write_text(head+'\n')
    (metadata/'config').write_text('[core]\n repositoryformatversion = 0\n bare = false\n[remote "origin"]\n url = https://'+identity+'.git\n')
    (metadata/'objects/info/alternates').write_text(str(objects)+'\n')
    return {'path':str(source),'objects':str(objects),'head':head,'identity':identity}


def main():
    attempt=PACKET/'attempt-01';attempt.mkdir(exist_ok=False)
    scratch=Path(os.environ['TMPDIR'])/'owner'
    scratch.mkdir()
    archive=read_git(OWNER,'archive',CANDIDATE)
    with tarfile.open(fileobj=io.BytesIO(archive)) as bundle:bundle.extractall(scratch,filter='data')
    for relative in (POLICY,MANIFEST,'tests/eng/test_mutation_triage.py'):
        (scratch/relative).write_bytes((OWNER/relative).read_bytes())
    stores=[private_store(scratch,OWNER,CANDIDATE,IDENTITY)]
    policy=json.loads((scratch/POLICY).read_text())
    for row in policy['trusted_identities']:
        if row['local_path']=='.':continue
        name=row['local_path'].split('/')[-1]
        source=ROOT if name=='Hexalith.Tenants' else Path('/home/administrator/projects/hexalith/parties') if name=='Hexalith.Parties' else ROOT/'references'/name
        head=read_git(source,'rev-parse','HEAD').decode().strip()
        stores.append(private_store(scratch/row['local_path'],source,head,row['identity']))
    (attempt/'object-store-bindings.json').write_text(json.dumps({'root_archive_sha256':hashlib.sha256(archive).hexdigest(),'stores':stores},indent=2)+'\n')
    # Every Git child is confined to read-only verbs; an unexpected acquisition fetch refuses.
    real_git=shutil.which('git');bin_path=Path(os.environ['TMPDIR'])/'bin';bin_path.mkdir()
    guard=bin_path/'git'
    guard.write_text('#!'+sys.executable+'\nimport os,sys\na=sys.argv[1:];i=0\nwhile i<len(a) and a[i] in ("-c","--git-dir","--work-tree"): i+=2\nif i==len(a) or a[i] not in ("cat-file","ls-tree","rev-parse","show","merge-base","diff","status","log"):\n print("Read-only diagnostic refuses Git mutation: "+repr(a),file=sys.stderr);sys.exit(97)\nos.execv('+repr(real_git)+',['+repr(real_git)+',*a])\n')
    guard.chmod(0o700)
    environment={**os.environ,'PATH':str(bin_path)+os.pathsep+os.environ['PATH'],'PYTHONDONTWRITEBYTECODE':'1','GIT_OPTIONAL_LOCKS':'0','GIT_TERMINAL_PROMPT':'0'}
    checks=[]
    def command(name,args,expected=0,contains=None):
        log=attempt/(name+'.log')
        with log.open('x') as output:result=subprocess.run(args,cwd=scratch,env=environment,stdout=output,stderr=subprocess.STDOUT)
        text=log.read_text()
        checks.append({'name':name,'command':args,'exit_code':result.returncode,'expected_exit':expected,'log':log.name,'log_sha256':sha(log)})
        (attempt/'commands.json').write_text(json.dumps(checks,indent=2)+'\n')
        require(result.returncode==expected,name+' unexpected exit; see '+str(log))
        if contains:require(contains in text,name+' missing distinguishing refusal.')
        print(name+': '+str(result.returncode),flush=True)
        return text
    command('enum-inventory-controls',['python3','tests/eng/test_mutation_triage.py','MutationInventoryTests'])
    command('actual-old-base-catalog-refusal',['python3','eng/dependency_graph.py','acquire-catalogs','--event-base',BASE,'--candidate',CANDIDATE],1,'consumer source projection owner pin changed')
    command('actual-old-base-diff-refusal',['python3','eng/dependency_graph.py','diff','--event','push','--event-base',BASE,'--candidate',CANDIDATE],1,'consumer source projection owner pin changed')
    command('current-committed-policy-refusal',['python3','eng/dependency_graph.py','validate','--commit',CANDIDATE],1,'consumer source projection owner pin changed')
    command('prospective-seal-graph',['python3','eng/dependency_graph.py','--policy',str(scratch/POLICY),'graph','--commit',CANDIDATE])
    command('prospective-seal-validate',['python3','eng/dependency_graph.py','--policy',str(scratch/POLICY),'validate','--commit',CANDIDATE])
    old_path=scratch/'original-policy.json';old_path.write_bytes((PACKET/'before'/POLICY).read_bytes())
    command('retained-11c-policy-validate',['python3','eng/dependency_graph.py','--policy',str(old_path),'validate','--commit',HISTORICAL])
    # Test the real acquisition body with an explicit in-memory prospective policy; this is
    # a prospective offline control, NOT a claim that a seal/activation commit exists.
    sys.path.insert(0,str(scratch/'eng'))
    import dependency_graph as dg
    original_loader=dg.load_policy_at_commit
    network_calls=[]
    def no_network(*args,**kwargs):
        network_calls.append(args);raise ValueError('Unexpected network operation in offline control.')
    dg._run_git_network=no_network
    loaded=dg.load_policy(scratch/POLICY)
    def prospective_loader(root,commit):
        require(commit==CANDIDATE,'Unexpected prospective event base.')
        return loaded,(scratch/POLICY).read_bytes(),dg.policy_projection(loaded,commit,(scratch/POLICY).read_bytes())
    dg.load_policy_at_commit=prospective_loader
    acquired=dg.acquire_catalog_objects(scratch,CANDIDATE,CANDIDATE)
    dg.load_policy_at_commit=original_loader
    require(not network_calls,'Prospective acquisition attempted networking.')
    (attempt/'prospective-offline-catalog-acquisition.json').write_text(json.dumps({'scope':'Real acquisition function under explicitly injected prospective policy on unchanged committed selector bytes; no committed seal/activation or native qualification inferred.','network_calls':0,'acquisition':acquired},indent=2)+'\n')
    graph=dg.collect_graph(scratch,IDENTITY,CANDIDATE,loaded)
    refusals=[]
    for field,value in [('owner_commit','f6f85032598865b9123f16364448bfd02c0ff5b0'),('gitmodules_sha256','0'*64),('inventory_sha256','0'*64),('root_selectors_sha256','0'*64),('consumer_configuration_sha256','0'*64),('builds_commit','73e60af8a1fcdf3f06726d5d1b6e1199cce778ee')]:
        changed=copy.deepcopy(loaded);changed['consumer_source_projections'][PLATFORM][field]=value
        try:dg.collect_graph(scratch,IDENTITY,CANDIDATE,changed)
        except dg.GraphError as error:refusals.append({'changed_field':field,'refusal':str(error)})
        else:raise ValueError('Changed source seal was accepted: '+field)
    changed=copy.deepcopy(loaded);changed['consumer_source_projections'][PLATFORM]['source_files']['DaprSelfHostedMtls.cs']='0'*64
    try:dg.collect_graph(scratch,IDENTITY,CANDIDATE,changed)
    except dg.GraphError as error:refusals.append({'changed_field':'canonical_source_sha256','refusal':str(error)})
    else:raise ValueError('Changed canonical source was accepted.')
    changed=copy.deepcopy(loaded);changed['module_build_registry'][PLATFORM]['disposition']='build'
    try:dg._consumer_projection(changed,PLATFORM,loaded['consumer_source_projections'][PLATFORM]['owner_commit'])
    except dg.GraphError as error:refusals.append({'changed_field':'whole_owner_execution','refusal':str(error)})
    else:raise ValueError('Whole-owner execution was accepted.')
    try:dg.resolve_local_path(loaded,'github.com/hexalith/hexalith.agents',scratch)
    except dg.GraphError as error:refusals.append({'changed_field':'inventory_only_global_trust','refusal':str(error)})
    else:raise ValueError('Inventory-only global trust was accepted.')
    (attempt/'source-projection-refusal-controls.json').write_text(json.dumps(refusals,indent=2)+'\n')
    original=json.loads(old_path.read_text())
    for key in original:
        if key!='consumer_source_projections':require(original[key]==loaded[key],'Unrelated authority changed: '+key)
    # Retain authenticated original failure bytes and finite report identities, without
    # rewriting or relabeling historical 11c reports as qualification of this patch.
    report_inspection=[]
    enums={entry['path'] for entry in json.loads((scratch/MANIFEST).read_text())['explicitExclusions'] if entry['path'].startswith('src/Hexalith.FrontComposer.SourceTools/Transforms/')}
    for name in ('happy','error','aggregate'):
        source=PACKET.parent/('fc-11c-hosted-'+name)
        verification=json.loads((source/'verification.json').read_text())
        archive_path=source/'artifact.zip'
        require(verification['artifact']['digest']=='sha256:'+sha(archive_path),'Retained native artifact digest mismatch.')
        with zipfile.ZipFile(archive_path) as bundle:
            if name=='aggregate':
                refusal=bundle.read('mutation-validation-errors.txt')
                (attempt/'original-11c-target-drift-errors.txt').write_bytes(refusal)
                require(refusal.decode().count('Target drift:')==3,'Original aggregate has additional failures.')
            else:
                member=next(path for path in bundle.namelist() if path.startswith('reports/') and path.endswith('.json'))
                raw=bundle.read(member);report=json.loads(raw);counts={}
                files={path.removeprefix('./'):value for path,value in report['files'].items()}
                require(not enums.intersection(files),'An accounted enum actually appears in original native report.')
                for file in files.values():
                    for mutant in file.get('mutants',[]):counts[mutant['status']]=counts.get(mutant['status'],0)+1
                require(counts.get('CompileError')==717 and counts.get('Ignored')==562,'Historical invalid/ignored census changed.')
                report_inspection.append({'segment':name,'source':HISTORICAL,'artifact_sha256':sha(archive_path),'report_member':member,'report_sha256':hashlib.sha256(raw).hexdigest(),'raw_status_counts':counts,'enum_files_absent':sorted(enums)})
    (attempt/'historical-report-inspection.json').write_text(json.dumps(report_inspection,indent=2)+'\n')
    summary={'status':'passed','source':CANDIDATE,'checks':checks,'prospective_offline_acquisition':True,'source_refusal_controls':len(refusals),'enum_tests':4,
             'limitations':['Explicit prospective policy controls are not active committed policy or a hosted native run.','Initial source-seal push is still evaluated under the invalid old event-base policy and may remain red.','A separate documentation-only activation push with the landed seal as immediate event base is required.','No raw mutation statuses/cohorts/thresholds/runtime/provenance were changed.']}
    with (attempt/'summary.json').open('x') as output:output.write(json.dumps(summary,indent=2)+'\n')
    print(json.dumps({'status':'passed','enum_tests':4,'source_refusals':len(refusals),'commands':len(checks)}),flush=True)


if __name__=='__main__':main()
