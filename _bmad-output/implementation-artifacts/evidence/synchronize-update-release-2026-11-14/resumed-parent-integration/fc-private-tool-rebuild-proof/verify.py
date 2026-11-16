"""Verify exact private-tool receipt/runtime repeatability at one clean path; no mutation run."""
import ctypes,hashlib,importlib.util,json,os,shutil,subprocess,time
from pathlib import Path
OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[5]
WORK=Path(os.environ['TMPDIR'])/'FrontComposer'
SOURCE='b34be9245443d21b72a6076cf205bbbc8a8c521f'
assert ctypes.CDLL(None,use_errno=True).prctl(36,1,0,0,0)==0
record={'scope':__doc__,'source':SOURCE,'workspace':str(WORK),'commands':[],'qualification':False}
cleanup=None

def save(): (OUT/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
def run(name,args,cwd=WORK):
    started=time.time()
    with (OUT/(name+'.log')).open('x') as f:p=subprocess.run(args,cwd=cwd,stdout=f,stderr=subprocess.STDOUT)
    record['commands'].append({'name':name,'command':args,'exitCode':p.returncode,'seconds':round(time.time()-started,3),'logSha256':hashlib.sha256((OUT/(name+'.log')).read_bytes()).hexdigest()})
    if cleanup:record['commands'][-1]['ownedChildrenReaped']=sorted(cleanup())
    save();print(name,p.returncode,flush=True)
    assert p.returncode==0,name
try:
    run('clone',['git','clone','--no-hardlinks','--no-checkout',str(ROOT/'references/Hexalith.FrontComposer'),str(WORK)],Path(os.environ['TMPDIR']))
    run('checkout',['git','checkout','--detach',SOURCE])
    spec=importlib.util.spec_from_file_location('rebuild_trace',WORK/'eng/trace-mutation-run.py');trace=importlib.util.module_from_spec(spec);spec.loader.exec_module(trace);cleanup=trace.finish_children
    os.environ.update(DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    assert os.environ.get('GITHUB_ACTIONS')!='true'
    run('prepare-first',['python3','eng/stryker_compat.py','prepare'])
    dirs=list(Path(os.environ['TMPDIR']).glob('hfcst-*'));assert len(dirs)==1
    tool=dirs[0];first=json.loads((tool/'build-provenance.json').read_text());(OUT/'first-build-provenance.json').write_text(json.dumps(first,indent=2)+'\n')
    assert tool.parent.resolve()==Path(os.environ['TMPDIR']).resolve() and tool.name.startswith('hfcst-')
    record['firstToolPath']=str(tool);save()
    shutil.rmtree(tool)
    run('prepare-second',['python3','eng/stryker_compat.py','prepare'])
    second=json.loads((tool/'build-provenance.json').read_text());(OUT/'second-build-provenance.json').write_text(json.dumps(second,indent=2)+'\n')
    record['receiptsIdentical']=first==second
    record['runtimeFilesIdentical']=first['runtime_files']==second['runtime_files']
    record['buildDigestIdentical']=first['build_digest']==second['build_digest']
    record['differingTopLevelFields']=[k for k in set(first)|set(second) if first.get(k)!=second.get(k)]
    run('clean-source',['git','status','--porcelain'])
    assert not subprocess.check_output(['git','status','--porcelain'],cwd=WORK,text=True)
finally:
    if cleanup:record['finalOwnedChildrenReaped']=sorted(cleanup())
    record['finishedUtc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime());save()
