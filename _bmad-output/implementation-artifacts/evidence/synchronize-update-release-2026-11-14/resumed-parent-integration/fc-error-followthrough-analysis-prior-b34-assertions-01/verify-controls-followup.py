"""Focused private behavioral controls only; never a full mutation/gate result."""
from pathlib import Path
import hashlib,io,json,os,shutil,subprocess,tarfile
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
SOURCE='e0eef14ddbbcd03823863a4f417a4b07f6f5bafc';BUILDS='73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
OUT=BASE/'behavior-controls-02';OUT.mkdir(exist_ok=False)
WORK=Path(os.environ['TMPDIR'])/'FrontComposer'
def archive(repo,rev,target):
 target.mkdir(parents=True,exist_ok=True)
 with tarfile.open(fileobj=io.BytesIO(subprocess.check_output(['git','archive',rev],cwd=repo))) as source:
  source.extractall(target,members=[m for m in source.getmembers() if not m.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
archive(OWNER,SOURCE,WORK);archive(ROOT/'references/Hexalith.Builds',BUILDS,WORK/'references/Hexalith.Builds')
patch=BASE/'ErrorFollowthroughBehaviorTests.cs';shutil.copy2(patch,WORK/'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'/patch.name)
report=json.loads((BASE.parent/'fc-error-local-diagnostic/attempt-01/original-error-artifacts/reports/source-tools-error-handling.json').read_text())
index=json.loads((BASE/'source-index.json').read_text());mapped={m['id']:m for m in index['mutants']}
raw={name.split('/FrontComposer/',1)[-1]:data['source'] for name,data in report['files'].items()}
selected=['14125','14128','15199','15203','15214','15219','15222','15287','13455','13457','13475','13524','13537']
originals={m['path']:(WORK/m['path']).read_bytes() for m in mapped.values()}
records={'source':SOURCE,'builds':BUILDS,'workspace':str(WORK),'scope':__doc__,'test_source_sha256':hashlib.sha256(patch.read_bytes()).hexdigest(),'commands':[],'variants':{}}
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',UseMicrosoftTestingPlatformRunner='true',DOTNET_PROCESSOR_COUNT='2',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
project='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
def run(name,args):
 log=OUT/(name+'.log')
 with log.open('x') as stream:result=subprocess.run(args,cwd=WORK,stdout=stream,stderr=subprocess.STDOUT)
 records['commands'].append({'name':name,'command':args,'exit_code':result.returncode,'log':log.name,'sha256':hashlib.sha256(log.read_bytes()).hexdigest()})
 (OUT/'verification.json').write_text(json.dumps(records,indent=2)+'\n');print(name,result.returncode,flush=True)
 return result.returncode
previous=None
for ident in ['baseline',*selected]:
 if previous:(WORK/previous).write_bytes(originals[previous])
 if ident!='baseline':
  row=mapped[ident];source=raw[row['path']];needle=row['original'];replacement=row['replacement']
  inventory=next(m for d in report['files'].values() for m in d.get('mutants',[]) if m['id']==ident)
  start=inventory['location']['start'];end=inventory['location']['end'];lines=source.splitlines(keepends=True)
  a=sum(map(len,lines[:start['line']-1]))+start['column']-1;b=sum(map(len,lines[:end['line']-1]))+end['column']-1
  assert source[a:b]==needle
  changed=source[:a]+replacement+source[b:];(WORK/row['path']).write_bytes(changed.encode());previous=row['path']
  records['variants'][ident]={'path':row['path'],'baseline_source_sha256':hashlib.sha256(source.encode()).hexdigest(),'mutated_source_sha256':hashlib.sha256(changed.encode()).hexdigest()}
 if run(ident+'-build',['dotnet','build',project,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-m:1','-nodeReuse:false']):raise SystemExit(1)
 code=run(ident+'-tests',['dotnet','test','--project',project,'--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.ErrorFollowthroughBehaviorTests','--minimum-expected-tests','25','--fail-skips','on','--results-directory',str(OUT/(ident+'-results')),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'])
 if ident=='baseline' and code:raise SystemExit(code)
 if code not in (0,2):raise SystemExit(code)
