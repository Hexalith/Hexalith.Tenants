"""Reproduce owner CI against exact frozen owner Git objects in a private workspace."""
import argparse, hashlib, io, json, os, pathlib, re, shutil, subprocess, sys, tarfile
EVIDENCE=pathlib.Path(__file__).resolve().parent
ROOT=EVIDENCE.parents[4]
OWNER=ROOT/'references/Hexalith.EventStore'
WORK=pathlib.Path(os.environ['TMPDIR'])/'EventStore'
PLATFORM='00ed06074f5b67522eff81dc8dae6ebd28161c36'
BUILDS='e2bbbd0893f586e4d447ef8358ff747ea3d34435'
SOURCE='2dcf708cbab0d4ad0cf72f947d0b0a1444e251a1'
parser=argparse.ArgumentParser()
parser.add_argument('--baseline',action='store_true')
parser.add_argument('--attempt',required=True)
parser.add_argument('--framework-probe',action='store_true')
options=parser.parse_args()
label=options.attempt
receipt=EVIDENCE/(label+'-verification.json')
if receipt.exists(): raise SystemExit('Refusing to overwrite existing attempt '+label)
results={'source':SOURCE,'platform':PLATFORM,'builds':BUILDS,'scope':'Local disposable source-component qualification; frozen package cohort remains separate from current SDK/native or installed acceptance.','workspace':str(WORK),'commands':[],'input_manifest':label+'-input-manifest.json'}

def archive(repository, revision, target):
    target.mkdir(parents=True,exist_ok=True)
    data=subprocess.check_output(['git','archive',revision],cwd=repository)
    with tarfile.open(fileobj=io.BytesIO(data)) as source:
        members=[member for member in source.getmembers() if not member.name.startswith(('.agents/skills/','.codex/skills/','references/'))]
        source.extractall(target,members=members,filter='data')
    (target/'.git').mkdir(exist_ok=True)

archive(OWNER,SOURCE,WORK)
archive(ROOT/'references/Hexalith.Platform',PLATFORM,WORK/'references/Hexalith.Platform')
archive(ROOT/'references/Hexalith.Builds',BUILDS,WORK/'references/Hexalith.Builds')
if not options.baseline:
    for relative in subprocess.check_output(['git','diff','--name-only'],cwd=OWNER,text=True).splitlines()+subprocess.check_output(['git','ls-files','--others','--exclude-standard'],cwd=OWNER,text=True).splitlines():
        source=OWNER/relative
        if source.is_file() and not relative.startswith('references/'):
            target=WORK/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
input_hashes={}
for base in (WORK,):
    for path in base.rglob('*'):
        if path.is_file() and not '/.git/' in str(path):
            input_hashes[str(path.relative_to(WORK))]=hashlib.sha256(path.read_bytes()).hexdigest()
manifest=EVIDENCE/results['input_manifest']
manifest.write_text(json.dumps(input_hashes,indent=2,sort_keys=True)+'\n')
results['input_manifest_sha256']=hashlib.sha256(manifest.read_bytes()).hexdigest()
results['input_file_count']=len(input_hashes)
results['runner_sha256']=hashlib.sha256(pathlib.Path(__file__).read_bytes()).hexdigest()
os.environ['NUGET_PACKAGES']=str(pathlib.Path(os.environ['TMPDIR'])/'packages')
os.environ['NUGET_HTTP_CACHE_PATH']=str(pathlib.Path(os.environ['TMPDIR'])/'http-cache')
os.environ['DOTNET_CLI_HOME']=str(pathlib.Path(os.environ['TMPDIR'])/'dotnet-home')
os.environ['DOTNET_NOLOGO']='1'
os.environ['DOTNET_CLI_TELEMETRY_OPTOUT']='1'
os.environ['EXT_PROTECTION_INTEROP_CONFIGURATION']='Release'
os.environ['EXT_PROTECTION_INTEROP_PROJECT_REFERENCES']='false'
os.environ['CI']='true'
os.environ['HUSKY']='0'

def run(name, args, cwd=WORK, expected=0, env=None):
    print(name,flush=True)
    log=EVIDENCE/(label+'-'+name+'.log')
    with log.open('x') as output:
        process=subprocess.run(args,cwd=cwd,env=env,stdout=output,stderr=subprocess.STDOUT)
    results['commands'].append({'name':name,'command':args,'cwd':str(cwd),'environment_overrides':({key:value for key,value in env.items() if os.environ.get(key)!=value} if env else {}),'exit_code':process.returncode,'expected_exit_code':expected,'log':log.name,'log_sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
    receipt.write_text(json.dumps(results,indent=2)+'\n')
    print(log.name,'exit',process.returncode,'sha256',results['commands'][-1]['log_sha256'],flush=True)
    print('\n'.join(line[:350] for line in log.read_text().splitlines() if re.match(r'^(Ran |OK|FAILED|.*Tests:|Build succeeded|    [0-9]+ (Warning|Error))',line)),flush=True)
    if process.returncode!=expected: raise SystemExit(process.returncode or 1)

if options.baseline:
    run('original-p13-build',['python3','-m','unittest','discover','-s','tools/tests','-p','test_ext_protection_1_source_p13.py','-v'],expected=1)
    raise SystemExit(0)
run('package-view',['dotnet','msbuild','tests/Hexalith.Platform.Protection.Tests/Hexalith.Platform.Protection.Tests.csproj','-p:Configuration=Release','-p:UseHexalithProjectReferences=false','-p:CustomAfterMicrosoftCommonProps='+str(WORK/'tools/tests/ext_protection_component_runtime.props'),'-p:HexalithEventStoreRoot='+str(WORK),'-getProperty:TargetFramework,HexalithEventStoreVersion,HexalithMemoriesVersion,UseHexalithProjectReferences','-getItem:PackageVersion'],cwd=WORK/'references/Hexalith.Platform')
if options.framework_probe:
    run('framework-probe',['dotnet','build','tests/Hexalith.Platform.Protection.Tests/Hexalith.Platform.Protection.Tests.csproj','-c','Release','-p:UseHexalithProjectReferences=false','-p:CustomAfterMicrosoftCommonProps='+str(WORK/'tools/tests/ext_protection_component_runtime.props'),'-p:HexalithEventStoreRoot='+str(WORK),'-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-m:1','-nodeReuse:false'],cwd=WORK/'references/Hexalith.Platform')
    raise SystemExit(0)
run('npm-ci',['npm','ci','--ignore-scripts','--no-audit'])
run('python-owner-qualification',['python3','-m','unittest','discover','-s','tools/tests','-p','test_ext_protection_1*.py','-v'])

platform=WORK/'references/Hexalith.Platform'
component=platform/'tests/Hexalith.Platform.Protection.Tests/Hexalith.Platform.Protection.Tests.csproj'
run('component-build',['dotnet','build',str(component),'-c','Release','-p:UseHexalithProjectReferences=false','-p:CustomAfterMicrosoftCommonProps='+str(WORK/'tools/tests/ext_protection_component_runtime.props'),'-p:HexalithEventStoreRoot='+str(WORK),'-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-m:1','-nodeReuse:false'],cwd=platform)
assembly=component.parent/'bin/Release/net11.0/Hexalith.Platform.Protection.Tests.dll'
assets=component.parent/'obj/project.assets.json'
runtime=assembly.with_suffix('.runtimeconfig.json')
results['package_component_runtime']={'assembly_sha256':hashlib.sha256(assembly.read_bytes()).hexdigest(),'assets_sha256':hashlib.sha256(assets.read_bytes()).hexdigest(),'runtimeconfig':json.loads(runtime.read_bytes()),'runtimeconfig_sha256':hashlib.sha256(runtime.read_bytes()).hexdigest(),'package_versions':[key for key in json.loads(assets.read_bytes())['libraries'] if key.startswith(('Hexalith.EventStore.','Hexalith.Memories.','Hexalith.Commons.UniqueIds/'))]}
receipt.write_text(json.dumps(results,indent=2)+'\n')
methods=(
('p13','ProtectionQualificationPreparationTests','TwoExpectedContainmentTargetsUseDistinctRealControlRootAndEmitCompleteVerifierEvidence','EXT_PROTECTION_P13_VECTOR'),
('p34','ErasureRootLoop2OwnerTests','CompiledP34ActivatesBeforeReplacementCompromiseAndEmitsActualVerifierObservation','EXT_PROTECTION_P34_VECTOR'),
('p33','ErasureRootLoop2OwnerTests','CompiledP33RacesBothActualOwnerOrderingsAndEmitsExactVerifierObservation','EXT_PROTECTION_P33_VECTOR_DIRECTORY'),
('snapshot','ErasureRootLoop2SnapshotObservationTests','ActualSingleAndMixedObserverEmissionsKeepLegacyEnvelopesAndStrictFragmentOriginals','EXT_PROTECTION_SNAPSHOT_VECTOR'),
('producer','ProtectionQualificationLoop3SealingTests',None,'EXT_PROTECTION_LOOP3_SEAL_VECTORS'),
('recovery','ProtectionQualificationLoop7RecoveryTests',None,'EXT_PROTECTION_LOOP7_VECTOR_DIRECTORY'),
('copies','ErasureRootLoop5ConfiguredSealTests',None,'EXT_PROTECTION_COPY_VECTOR'))
for name,cls,method,variable in methods:
    vector=EVIDENCE/(label+'-'+name+('-vectors' if variable.endswith(('DIRECTORY','VECTORS')) else '-vector.json'))
    if variable.endswith(('DIRECTORY','VECTORS')): vector.mkdir()
    args=['dotnet',str(assembly),'-class','Hexalith.Platform.Protection.Tests.'+cls]
    if method: args+=['-method','*.'+method]
    args+=['-noColor','-xml',str(EVIDENCE/(label+'-'+name+'-xunit.xml'))]
    run('actual-csharp-'+name,args,env=dict(os.environ,**{variable:str(vector)}))
run('producer-pinned-component',['python3','-m','unittest','discover','-s','tools/tests','-p','test_ext_protection_1_producer_originals.py','-v'],env=dict(os.environ,EXT_PROTECTION_SNAPSHOT_COMPONENT_ASSEMBLY=str(assembly),EXT_PROTECTION_SNAPSHOT_COMPONENT_SHA256=results['package_component_runtime']['assembly_sha256']))
core='tests/Hexalith.EventStore.PayloadProtection.Tests/Hexalith.EventStore.PayloadProtection.Tests.csproj'
run('payload-core-build',['dotnet','build',core,'-c','Release','-warnaserror','-m:1','-nodeReuse:false','-p:UseHexalithProjectReferences=false','-p:GeneratePackageOnBuild=false'])
for name,extra,minimum in (('payload-core-vectors',[], '324'),('payload-core-invariant',['--filter-method','Hexalith.EventStore.PayloadProtection.Tests.AadPathTests.V029_InertPlatformNormalizer_FailsClosedThroughCoreWriteSeams'],'1')):
    args=['dotnet','test','--project',core,'--no-build','--configuration','Release','--minimum-expected-tests',minimum,'--fail-skips','on','--results-directory',str(EVIDENCE/(label+'-core-results')),'--report-xunit-trx','--report-xunit-trx-filename',name+'.trx','-p:UseHexalithProjectReferences=false',*extra]
    run(name,args,env=dict(os.environ,DOTNET_SYSTEM_GLOBALIZATION_INVARIANT='1') if extra else None)
artifacts={str(path.relative_to(EVIDENCE)):hashlib.sha256(path.read_bytes()).hexdigest() for path in EVIDENCE.glob(label+'-*') if path.is_file() and path!=receipt}
for directory in EVIDENCE.glob(label+'-*'):
    if directory.is_dir():
        artifacts.update({str(path.relative_to(EVIDENCE)):hashlib.sha256(path.read_bytes()).hexdigest() for path in directory.rglob('*') if path.is_file()})
results['artifacts']=artifacts
receipt.write_text(json.dumps(results,indent=2)+'\n')
