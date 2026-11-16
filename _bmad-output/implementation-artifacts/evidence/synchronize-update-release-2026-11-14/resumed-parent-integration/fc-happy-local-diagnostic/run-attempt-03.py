"""Exact e0 complete Happy-only local diagnostic, with unmodified authenticated private tool."""
import ctypes,hashlib,importlib.util,io,json,os,pathlib,shutil,subprocess,sys,tarfile,time
sys.dont_write_bytecode=True
EVIDENCE=pathlib.Path(__file__).resolve().parent
ROOT=EVIDENCE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
SOURCE='e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
BUILDS='73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
WORK=pathlib.Path(os.environ['TMPDIR'])/'FrontComposer'
ATTEMPT=EVIDENCE/'attempt-03'
ATTEMPT.mkdir(exist_ok=False)
if ctypes.CDLL(None,use_errno=True).prctl(36,1,0,0,0)!=0: raise OSError(ctypes.get_errno(),'Cannot own child cleanup')
records={'scope':__doc__,'source':SOURCE,'builds':BUILDS,'workspace':str(WORK),'started_utc':time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime()),'runner_sha256':hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest(),'commands':[],'private_regression_proposal_applied':False,'qualification':False,'github_context_supplied':False}
cleanup=None

def save():
    (ATTEMPT/'execution.json').write_text(json.dumps(records,indent=2)+'\n')

def run(name,args,cwd=WORK,required=True):
    log=ATTEMPT/(name+'.log');started=time.time();print(name,flush=True)
    with log.open('x') as output:
        process=subprocess.run(args,cwd=cwd,stdout=output,stderr=subprocess.STDOUT)
    row={'name':name,'command':args,'cwd':str(cwd),'exit_code':process.returncode,'elapsed_seconds':round(time.time()-started,3),'log':log.name,'log_sha256':hashlib.sha256(log.read_bytes()).hexdigest()}
    if cleanup is not None: row['owned_children_cleaned']=sorted(cleanup())
    records['commands'].append(row);save();print(name,'exit',process.returncode,'log',log.name,flush=True)
    if required and process.returncode: raise RuntimeError(name+' refused: exit '+str(process.returncode))
    return process.returncode

def inventory(base):
    return {str(path.relative_to(base)):hashlib.sha256(path.read_bytes()).hexdigest() for path in base.rglob('*') if path.is_file() and '.git' not in path.parts}

try:
    run('clone-exact-input',['git','clone','--no-checkout','--no-hardlinks',str(OWNER),str(WORK)],cwd=pathlib.Path(os.environ['TMPDIR']))
    run('checkout-e0',['git','checkout','--detach',SOURCE])
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=WORK,text=True).strip()==SOURCE
    # Read-only exact catalog-object availability; this creates no nested Git checkout.
    builds_objects=subprocess.check_output(['git','rev-parse','--path-format=absolute','--git-path','objects'],cwd=ROOT/'references/Hexalith.Builds',text=True).strip()
    alternates=WORK/'.git/objects/info/alternates'
    assert not alternates.exists()
    alternates.parent.mkdir(parents=True,exist_ok=True);alternates.write_text(builds_objects+'\n')
    records['read_only_external_builds_object_store']=builds_objects
    expected_catalog=subprocess.check_output(['git','show',BUILDS+':Props/Directory.Packages.props'],cwd=ROOT/'references/Hexalith.Builds')
    assert subprocess.check_output(['git','show',BUILDS+':Props/Directory.Packages.props'],cwd=WORK)==expected_catalog
    build_target=WORK/'references/Hexalith.Builds';build_target.mkdir(parents=True,exist_ok=True)
    archive=subprocess.check_output(['git','archive',BUILDS],cwd=ROOT/'references/Hexalith.Builds')
    with tarfile.open(fileobj=io.BytesIO(archive)) as source:
        source.extractall(build_target,members=[item for item in source.getmembers() if not item.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
    records['builds_archive_sha256']=hashlib.sha256(archive).hexdigest()
    build_inputs=inventory(build_target);(ATTEMPT/'builds-input-manifest.json').write_text(json.dumps(build_inputs,indent=2,sort_keys=True)+'\n')
    tracked=subprocess.check_output(['git','ls-files','-z'],cwd=WORK).decode().split('\0')
    source_inputs={relative:hashlib.sha256((WORK/relative).read_bytes()).hexdigest() for relative in tracked if relative and (WORK/relative).is_file()}
    (ATTEMPT/'source-input-manifest.json').write_text(json.dumps(source_inputs,indent=2,sort_keys=True)+'\n')
    for relative in ('eng/stryker-compat/definition.json','eng/stryker-compat/static-reset-compatibility.patch','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/stryker-happy-path.json','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/mutation-target-manifest.json'):
        records.setdefault('bound_inputs',{})[relative]={'sha256':source_inputs[relative],'git_blob':subprocess.check_output(['git','rev-parse','HEAD:'+relative],cwd=WORK,text=True).strip()}
    assert not (WORK/'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/ProjectionParserSurvivorRegressionTests.cs').exists()
    run('source-clean-preflight',['git','status','--porcelain'])
    assert not subprocess.check_output(['git','status','--porcelain'],cwd=WORK,text=True)
    # The checked-in resource wrapper owns only its own descendants and samples without command arguments.
    spec=importlib.util.spec_from_file_location('owned_happy_trace',WORK/'eng/trace-mutation-run.py');trace=importlib.util.module_from_spec(spec);spec.loader.exec_module(trace);cleanup=trace.finish_children
    os.environ.update(UseMicrosoftTestingPlatformRunner='true',DOTNET_PROCESSOR_COUNT='2',MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',NUGET_PACKAGES=str(pathlib.Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(pathlib.Path(os.environ['TMPDIR'])/'http-cache'),DOTNET_CLI_HOME=str(pathlib.Path(os.environ['TMPDIR'])/'dotnet-home'),DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
    assert os.environ.get('GITHUB_ACTIONS')!='true'
    # Use the same installed PowerShell DLL directly, avoiding this workstation's extra global-tool process.
    import shlex
    ps_home=pathlib.Path(subprocess.check_output(['pwsh','-NoProfile','-Command','$PSHOME'],text=True).strip())
    ps_dll=ps_home/'pwsh.dll';assert ps_dll.is_file()
    dotnet_executable=pathlib.Path(shutil.which('dotnet')).resolve()
    launcher_dir=pathlib.Path(os.environ['TMPDIR'])/'direct-powershell-bin';launcher_dir.mkdir()
    launcher=launcher_dir/'pwsh'
    launcher.write_text('#!/bin/sh\nexec '+shlex.quote(str(dotnet_executable))+' '+shlex.quote(str(ps_dll))+' "$@"\n');launcher.chmod(0o700)
    records['local_powershell_launcher']={'purpose':'Same runtime DLL with direct PID ownership; no source/test/provenance option changes','script_sha256':hashlib.sha256(launcher.read_bytes()).hexdigest(),'dll':str(ps_dll),'dll_sha256':hashlib.sha256(ps_dll.read_bytes()).hexdigest(),'dotnet':str(dotnet_executable),'dotnet_sha256':hashlib.sha256(dotnet_executable.read_bytes()).hexdigest()}
    os.environ['PATH']=str(launcher_dir)+os.pathsep+os.environ['PATH']
    records['environment']={key:os.environ[key] for key in ('UseMicrosoftTestingPlatformRunner','DOTNET_PROCESSOR_COUNT','MSBUILDDISABLENODEREUSE','DOTNET_CLI_USE_MSBUILD_SERVER','UseSharedCompilation','NUGET_PACKAGES','NUGET_HTTP_CACHE_PATH','DOTNET_CLI_HOME')};save()
    run('installed-dotnet',['dotnet','--info'])
    run('tool-restore',['dotnet','tool','restore'])
    run('solution-restore',['dotnet','restore','Hexalith.FrontComposer.slnx','-p:Configuration=Release','-m:1','-nodeReuse:false'])
    run('authenticated-tool-prepare',['python3','eng/stryker_compat.py','prepare'])
    # Preserve the actual verified private build, identity receipt and byte inventory, not a substitute tool.
    directories=list(pathlib.Path(os.environ['TMPDIR']).glob('hfcst-*'));assert len(directories)==1
    private=directories[0]
    shutil.copy2(private/'build-provenance.json',ATTEMPT/'private-tool-build-provenance.json')
    run('source-tools-test-build',['dotnet','build','tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj','--configuration','Release','--no-restore','-m:1','-nodeReuse:false'])
    run('unmutated-docs-baseline',['dotnet','test','--project','tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj','--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Docs.DocsSiteValidationTests','--minimum-expected-tests','1','--fail-skips','on','--results-directory',str(ATTEMPT.resolve()/'docs-baseline'),'--report-xunit-trx','--report-xunit-trx-filename','tests.xml','-p:UseMicrosoftTestingPlatformRunner=true'])
    records['happy_segment_exit']=run('complete-happy-segment',['python3','../../eng/trace-mutation-run.py','--','python3','../../eng/stryker_compat.py','run','happy-path'],cwd=WORK/'src/Hexalith.FrontComposer.SourceTools',required=False)
    original=WORK/'artifacts/mutation/error-handling'
    if original.exists(): shutil.copytree(original,ATTEMPT/'original-happy-artifacts')
    records['happy_provenance_verify_exit']=run('happy-provenance-verify',['python3','eng/stryker_compat.py','verify','happy-path'],required=False)
    records['both_segment_validator_exit']=run('strict-both-segment-validator',['pwsh','-NoProfile','-File','eng/validate-stryker-reports.ps1','-RequireCompatibilityProvenance'],required=False)
    if (WORK/'artifacts/mutation').exists(): shutil.copytree(WORK/'artifacts/mutation',ATTEMPT/'post-validator-artifacts')
    run('source-clean-final',['git','status','--porcelain'])
    assert not subprocess.check_output(['git','status','--porcelain'],cwd=WORK,text=True)
    records['completed_utc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime())
except BaseException as error:
    records['blocker']={'type':type(error).__name__,'message':str(error)}
    # Partial/error artifacts and exact refusal remain useful even when no complete report exists.
    partial=WORK/'artifacts/mutation'
    if partial.exists() and not (ATTEMPT/'partial-artifacts').exists(): shutil.copytree(partial,ATTEMPT/'partial-artifacts')
    raise
finally:
    if cleanup is not None: records['final_owned_children_cleaned']=sorted(cleanup())
    records['finished_utc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime());save()
    manifest={str(path.relative_to(ATTEMPT)):hashlib.sha256(path.read_bytes()).hexdigest() for path in ATTEMPT.rglob('*') if path.is_file() and path.name!='artifact-manifest.json'}
    (ATTEMPT/'artifact-manifest.json').write_text(json.dumps(manifest,indent=2,sort_keys=True)+'\n')
