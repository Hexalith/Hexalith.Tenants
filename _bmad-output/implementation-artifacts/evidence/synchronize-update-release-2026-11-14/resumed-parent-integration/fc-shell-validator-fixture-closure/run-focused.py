"""Actual existing45-case Shell validator fixture class; no wider suite or mutation qualification."""
from pathlib import Path
import hashlib,io,json,os,subprocess,tarfile,time
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer'
SOURCE='b34be9245443d21b72a6076cf205bbbc8a8c521f';OUT=BASE/'attempt-01';OUT.mkdir()
SCRATCH=Path(os.environ['TMPDIR']);WORK=SCRATCH/'FrontComposer'
r={'scope':__doc__,'sourceBase':SOURCE,'scratch':str(SCRATCH),'archives':[],'overlays':{},'commands':[]}
def save():(OUT/'verification.json').write_text(json.dumps(r,indent=2)+'\n')
def archive(repo,rev,target):
 data=subprocess.check_output(['git','archive',rev],cwd=repo);target.mkdir(parents=True,exist_ok=True)
 r['archives'].append({'owner':str(repo),'revision':rev,'archiveSha256':hashlib.sha256(data).hexdigest()})
 with tarfile.open(fileobj=io.BytesIO(data)) as tar:tar.extractall(target,members=[m for m in tar.getmembers() if not m.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
def run(name,args):
 start=time.monotonic()
 with (OUT/(name+'.log')).open('wb') as log:process=subprocess.run(args,cwd=WORK,env=env,stdout=log,stderr=subprocess.STDOUT)
 row={'name':name,'command':args,'cwd':str(WORK),'exitCode':process.returncode,'seconds':round(time.monotonic()-start,3),'logSha256':hashlib.sha256((OUT/(name+'.log')).read_bytes()).hexdigest()}
 r['commands'].append(row);save();print(json.dumps(row),flush=True);return process.returncode
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==SOURCE
archive(OWNER,SOURCE,WORK)
for owner in ['Hexalith.Builds','Hexalith.Platform']:
 revision=subprocess.check_output(['git','rev-parse',SOURCE+':references/'+owner],cwd=OWNER,text=True).strip()
 archive(ROOT/'references'/owner,revision,WORK/'references'/owner)
paths=subprocess.check_output(['git','diff','--name-only','-z'],cwd=OWNER).decode().split('\0')+subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=OWNER).decode().split('\0')
for name in sorted(set(filter(None,paths))):
 path=OWNER/name
 if path.is_file():
  data=path.read_bytes();target=WORK/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data);r['overlays'][name]=hashlib.sha256(data).hexdigest()
  copy=OUT/'owner-inputs'/name;copy.parent.mkdir(parents=True,exist_ok=True);copy.write_bytes(data)
env={**os.environ,'DOTNET_CLI_HOME':str(SCRATCH/'dotnet-home'),'NUGET_PACKAGES':str(SCRATCH/'packages'),'XDG_CACHE_HOME':str(SCRATCH/'cache'),
 'UseMicrosoftTestingPlatformRunner':'true','DOTNET_PROCESSOR_COUNT':'2','MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','UseSharedCompilation':'false'}
project='tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj'
code=run('shell-build',['dotnet','build',project,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-m:1','-nodeReuse:false'])
if code==0:
 code=run('native-validator-class',['dotnet','test','--project',project,'--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.Shell.Tests.Governance.MutationReportValidationTests','--minimum-expected-tests','45','--fail-skips','on','--results-directory',str(OUT/'test-results'),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'])
r['finishedUtc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime());r['allChecksPassed']=code==0;save();raise SystemExit(code)
