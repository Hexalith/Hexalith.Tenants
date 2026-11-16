"""Bounded review corrections: publication policy and five edited classes; no full mutation."""
from pathlib import Path
import hashlib,io,json,os,shutil,subprocess,tarfile,time
BASE=Path(__file__).resolve().parent
ROOT=BASE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
SOURCE='b34be9245443d21b72a6076cf205bbbc8a8c521f'
OUT=BASE/'attempt-01';OUT.mkdir()
SCRATCH=Path(os.environ['TMPDIR']);WORK=SCRATCH/'FrontComposer'
record={'scope':__doc__,'sourceBase':SOURCE,'scratch':str(SCRATCH),'commands':[],'overlays':{},'archives':[]}
def save():(OUT/'verification.json').write_text(json.dumps(record,indent=2)+'\n')
def run(name,args,cwd,env):
 start=time.monotonic()
 with (OUT/(name+'.log')).open('wb') as log:p=subprocess.run(args,cwd=cwd,env=env,stdout=log,stderr=subprocess.STDOUT)
 row={'name':name,'command':args,'cwd':str(cwd),'exitCode':p.returncode,'seconds':round(time.monotonic()-start,3),'logSha256':hashlib.sha256((OUT/(name+'.log')).read_bytes()).hexdigest()}
 record['commands'].append(row);save();print(json.dumps(row),flush=True);return p.returncode
def archive(repo,revision,target):
 raw=subprocess.check_output(['git','archive',revision],cwd=repo)
 record['archives'].append({'revision':revision,'sha256':hashlib.sha256(raw).hexdigest()});target.mkdir(parents=True,exist_ok=True)
 with tarfile.open(fileobj=io.BytesIO(raw)) as tar:tar.extractall(target,members=[m for m in tar.getmembers() if not m.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip()==SOURCE
archive(OWNER,SOURCE,WORK)
builds=subprocess.check_output(['git','rev-parse',SOURCE+':references/Hexalith.Builds'],cwd=OWNER,text=True).strip()
archive(ROOT/'references/Hexalith.Builds',builds,WORK/'references/Hexalith.Builds')
paths=subprocess.check_output(['git','diff','--name-only','-z'],cwd=OWNER).decode().split('\0')+subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=OWNER).decode().split('\0')
for name in sorted(set(filter(None,paths))):
 path=OWNER/name
 if path.is_file():
  data=path.read_bytes();record['overlays'][name]=hashlib.sha256(data).hexdigest();target=WORK/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(data)
env={**os.environ,'PYTHONDONTWRITEBYTECODE':'1','PYTHONPYCACHEPREFIX':str(SCRATCH/'pycache'),'PUBLICATION_TEST_EVIDENCE':str(OUT/'publication-controls'),
 'DOTNET_CLI_HOME':str(SCRATCH/'dotnet-home'),'NUGET_PACKAGES':str(SCRATCH/'packages'),'XDG_CACHE_HOME':str(SCRATCH/'cache'),
 'UseMicrosoftTestingPlatformRunner':'true','DOTNET_PROCESSOR_COUNT':'2','MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','UseSharedCompilation':'false'}
results=[]
results.append(run('publication-controls',['python3','-B','tests/eng/test_mutation_artifact_publication.py'],WORK,env))
results.append(run('actionlint',['actionlint','-no-color','.github/workflows/mutation-property-nightly.yml'],WORK,env))
project='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
build=run('edited-tests-build',['dotnet','build',project,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-m:1','-nodeReuse:false'],WORK,env);results.append(build)
if build==0:
 classes=['ErrorFollowthroughBehaviorTests','RazorRoleRecoveryContractTests','TemplateContractBoundaryTests','AliasSignatureContractTests','CommandContractBoundaryTests']
 filters=[arg for name in classes for arg in ['--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.'+name]]
 results.append(run('edited-tests',['dotnet','test','--project',project,'--no-build','--configuration','Release',*filters,'--minimum-expected-tests','53','--fail-skips','on','--results-directory',str(OUT/'test-results'),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'],WORK,env))
record['allChecksPassed']=all(x==0 for x in results);record['finishedUtc']=time.strftime('%Y-%m-%dT%H:%M:%SZ',time.gmtime());save()
raise SystemExit(not record['allChecksPassed'])
