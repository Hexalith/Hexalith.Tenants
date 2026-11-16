"""Normal-analyzer focused ModelHash and actual Shell45 JSON-policy fixtures; finite literal controls only."""
from pathlib import Path
import hashlib,io,json,os,subprocess,tarfile,time
BASE=Path(__file__).resolve().parent;ROOT=BASE.parents[5];OWNER=ROOT/'references/Hexalith.FrontComposer';OUT=BASE/'csharp-attempt-02';OUT.mkdir()
WORK=Path(os.environ['TMPDIR'])/'FrontComposer';r={'scope':__doc__,'scratch':os.environ['TMPDIR'],'commands':[],'archives':[],'overlays':{}}
sha=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
def save():(OUT/'verification.json').write_text(json.dumps(r,indent=2)+'\n')
def archive(repo,revision,target):
 data=subprocess.check_output(['git','archive',revision],cwd=repo);target.mkdir(parents=True,exist_ok=True);r['archives'].append({'revision':revision,'sha256':hashlib.sha256(data).hexdigest()})
 with tarfile.open(fileobj=io.BytesIO(data)) as tar:tar.extractall(target,members=[m for m in tar.getmembers() if not m.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
def run(name,args):
 start=time.monotonic()
 with (OUT/(name+'.log')).open('wb') as log:p=subprocess.run(args,cwd=WORK,env=env,stdout=log,stderr=subprocess.STDOUT)
 row={'name':name,'command':args,'exitCode':p.returncode,'seconds':round(time.monotonic()-start,3),'logSha256':sha(OUT/(name+'.log'))};r['commands'].append(row);save();print(json.dumps(row),flush=True);return p.returncode
source=subprocess.check_output(['git','rev-parse','HEAD'],cwd=OWNER,text=True).strip();r['base']=source;archive(OWNER,source,WORK)
for dependency in ['Hexalith.Builds','Hexalith.Platform']:
 revision=subprocess.check_output(['git','rev-parse',source+':references/'+dependency],cwd=OWNER,text=True).strip();archive(ROOT/'references'/dependency,revision,WORK/'references'/dependency)
paths=subprocess.check_output(['git','diff','--name-only','-z'],cwd=OWNER).decode().split('\0')+subprocess.check_output(['git','ls-files','--others','--exclude-standard','-z'],cwd=OWNER).decode().split('\0')
for name in sorted(set(filter(None,paths))):
 if not (OWNER/name).is_file():continue
 target=WORK/name;target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes((OWNER/name).read_bytes());r['overlays'][name]=sha(target)
 copy=OUT/'inputs'/name;copy.parent.mkdir(parents=True,exist_ok=True);copy.write_bytes(target.read_bytes())
env={**os.environ,'DOTNET_CLI_HOME':str(Path(os.environ['TMPDIR'])/'home'),'NUGET_PACKAGES':str(Path(os.environ['TMPDIR'])/'packages'),'UseMicrosoftTestingPlatformRunner':'true','DOTNET_PROCESSOR_COUNT':'2','MSBUILDDISABLENODEREUSE':'1','DOTNET_CLI_USE_MSBUILD_SERVER':'0','UseSharedCompilation':'false'}
common=['-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-m:1','-nodeReuse:false']
project='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
def execute_model(name):
 code=run(name+'-build',['dotnet','build',project,*common])
 if code:return code
 return run(name+'-tests',['dotnet','test','--project',project,'--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.ModelHashFieldParticipationTests','--minimum-expected-tests','1','--fail-skips','on','--results-directory',str(OUT/(name+'-results')),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'])
code=execute_model('baseline');assert code==0,code
ledger=json.loads((BASE.parent/'fc-error-followthrough-analysis/per-id-ledger.json').read_text());rows={row['id']:row for row in ledger['mutants']}
selected=['13180','13382','13419']
raw=json.loads((BASE.parent/'fc-error-local-diagnostic/retained-original-reports/source-tools-error-handling.json').read_text())
mutation=next(m for file in raw['files'].values() for m in file['mutants'] if str(m['id'])=='13163')
eq={'id':'13163','path':'src/Hexalith.FrontComposer.SourceTools/Parsing/DomainModel.cs','location':mutation['location'],'original':'TypeName == other.TypeName','replacement':mutation['replacement']}
rows['13163']=eq
selected.append(eq['id']);r['equalityControlId']=eq['id'];r['variants']={};originals={}
for ident in selected:
 row=rows[ident];path=WORK/row['path'];originals.setdefault(row['path'],path.read_bytes());source_text=originals[row['path']].decode().replace('\r\n','\n');lines=source_text.splitlines(keepends=True);start,end=row['location']['start'],row['location']['end'];a=sum(map(len,lines[:start['line']-1]))+start['column']-1;b=sum(map(len,lines[:end['line']-1]))+end['column']-1;assert source_text[a:b]==row['original'].replace('\r\n','\n')
 variant=source_text[:a]+row['replacement'].replace('\r\n','\n')+source_text[b:];path.write_text(variant);r['variants'][ident]={'path':row['path'],'location':row['location'],'original':row['original'],'replacement':row['replacement'],'baselineSha256':hashlib.sha256(originals[row['path']]).hexdigest(),'literalSha256':sha(path)}
 code=execute_model(ident);assert code==2,(ident,code)
 path.write_bytes(originals[row['path']]);save()
# The JSON-policy module is also consumed by the actual existing Shell45 fixture.
project_shell='tests/Hexalith.FrontComposer.Shell.Tests/Hexalith.FrontComposer.Shell.Tests.csproj'
code=run('shell-build',['dotnet','build',project_shell,*common]);assert code==0,code
code=run('shell45-tests',['dotnet','test','--project',project_shell,'--no-build','--configuration','Release','--filter-class','Hexalith.FrontComposer.Shell.Tests.Governance.MutationReportValidationTests','--minimum-expected-tests','45','--fail-skips','on','--results-directory',str(OUT/'shell45-results'),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true']);assert code==0,code
r['completed']=True;save()
