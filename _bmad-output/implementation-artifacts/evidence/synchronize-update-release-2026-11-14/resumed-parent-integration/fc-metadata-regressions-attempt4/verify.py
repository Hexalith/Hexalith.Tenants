"""Build and execute all ten current regression classes from an exact owner source plus recorded overlay."""
from pathlib import Path
import hashlib,io,json,os,shutil,subprocess,tarfile,time
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
WORK=Path(os.environ['TMPDIR'])/'FrontComposer'
record={'scope':__doc__,'commands':[],'workspace':str(WORK),'sourceBase':subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip(),'overlay':{}}
def save(): (BASE/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
def archive(repo,rev,dest):
 raw=subprocess.check_output(['git','archive',rev],cwd=repo)
 dest.mkdir(parents=True,exist_ok=True)
 with tarfile.open(fileobj=io.BytesIO(raw)) as t:
  t.extractall(dest,members=[x for x in t.getmembers() if not x.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
 record.setdefault('archives',[]).append({'revision':rev,'sha256':hashlib.sha256(raw).hexdigest()})
archive(OWNER,record['sourceBase'],WORK)
rev=subprocess.check_output(['git','rev-parse',record['sourceBase']+':references/Hexalith.Builds'],cwd=OWNER,text=True).strip()
archive(ROOT/'references/Hexalith.Builds',rev,WORK/'references/Hexalith.Builds')
paths=subprocess.check_output(['git','ls-files','--others','--exclude-standard','tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'],cwd=OWNER,text=True).splitlines()
assert len(paths)==12,paths
for rel in paths:
 record['overlay'][rel]=hashlib.sha256((OWNER/rel).read_bytes()).hexdigest();shutil.copy2(OWNER/rel,WORK/rel)
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',UseMicrosoftTestingPlatformRunner='true',DOTNET_PROCESSOR_COUNT='2',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
def run(name,args):
 start=time.monotonic()
 with (BASE/(name+'.log')).open('x') as f:r=subprocess.run(args,cwd=WORK,stdout=f,stderr=subprocess.STDOUT)
 record['commands'].append({'name':name,'command':args,'exitCode':r.returncode,'elapsedSeconds':time.monotonic()-start});save();print(name,r.returncode,flush=True)
 if r.returncode:raise SystemExit(r.returncode)
p='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
run('build',['dotnet','build',p,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-m:1','-nodeReuse:false'])
classes=['MetadataDiagnosticContractTests']
filters=[a for c in classes for a in ('--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.'+c)]
run('tests',['dotnet','test','--project',p,'--no-build','--configuration','Release',*filters,'--minimum-expected-tests','16','--fail-skips','on','--results-directory',str(BASE/'results'),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'])
for rel,digest in record['overlay'].items():assert hashlib.sha256((OWNER/rel).read_bytes()).hexdigest()==digest
record['ownerOverlayUnchanged']=True;save()
