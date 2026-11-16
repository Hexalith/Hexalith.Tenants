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
assert len(paths)==14,paths
for rel in paths:
 record['overlay'][rel]=hashlib.sha256((OWNER/rel).read_bytes()).hexdigest();shutil.copy2(OWNER/rel,WORK/rel)
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',UseMicrosoftTestingPlatformRunner='true',DOTNET_PROCESSOR_COUNT='2',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')

import xml.etree.ElementTree as ET
project='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
report_path=BASE.parent/'fc-happy-local-diagnostic/retained-original-reports/source-tools-happy-path.json'
if not report_path.exists():
 report_path=next((BASE.parent/'fc-happy-local-diagnostic/retained-original-reports').glob('*.json'))
report=json.loads(report_path.read_text())
found=[(name,file,mutant) for name,file in report['files'].items() for mutant in file['mutants'] if str(mutant['id'])=='12812']
assert len(found)==1
name,file,row=found[0]
rel=name if name.startswith('src/') else 'src/Hexalith.FrontComposer.SourceTools/'+name.lstrip('./')
assert (WORK/rel).exists(),rel
record['originalReportSha256']=hashlib.sha256(report_path.read_bytes()).hexdigest()
record['originalLocalStatus']=row['status'];record['hostedBoundary']='Raw success observed in hosted completed console; no hosted original JSON retained; this is a new finite control only'
original=(WORK/rel).read_bytes();text=original.decode().replace('\r\n','\n');lines=text.splitlines(keepends=True);start,end=row['location']['start'],row['location']['end'];a=sum(map(len,lines[:start['line']-1]))+start['column']-1;z=sum(map(len,lines[:end['line']-1]))+end['column']-1
record['literal']={'id':'12812','path':rel,'location':row['location'],'original':text[a:z],'replacement':row['replacement'],'sourceSha256':hashlib.sha256(original).hexdigest()}
classes=[Path(p).stem for p in paths if Path(p).stem not in ['OtherAttributeGenerator','CountingModelComparer']]
filters=[arg for c in classes for arg in ('--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.'+c)]
def run(name,args):
 start=time.monotonic()
 with (BASE/(name+'.log')).open('x') as stream:result=subprocess.run(args,cwd=WORK,stdout=stream,stderr=subprocess.STDOUT)
 record['commands'].append({'name':name,'command':args,'exitCode':result.returncode,'elapsedSeconds':time.monotonic()-start});save();print(name,result.returncode,flush=True);return result.returncode
for label in ['baseline','literal12812']:
 if label!='baseline':(WORK/rel).write_text(text[:a]+row['replacement'].replace('\r\n','\n')+text[z:])
 code=run(label+'-build',['dotnet','build',project,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-m:1','-nodeReuse:false'])
 if code:record['compileRefused']=label;save();raise SystemExit(code)
 code=run(label+'-tests',['dotnet','test','--project',project,'--no-build','--configuration','Release',*filters,'--minimum-expected-tests','94','--fail-skips','on','--results-directory',str(BASE/(label+'-results')),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'])
 trx=BASE/(label+'-results')/'tests.trx';data=trx.read_bytes();trx.with_suffix('.xml').write_bytes(data);doc=ET.fromstring(data);record[label]={'exit':code,'sha256':hashlib.sha256(data).hexdigest(),'counters':[x.attrib for x in doc.iter() if x.tag.rsplit('}',1)[-1]=='Counters'],'failedTests':[x.attrib['testName'] for x in doc.iter() if x.tag.rsplit('}',1)[-1]=='UnitTestResult' and x.attrib.get('outcome')=='Failed']};save()
 if label=='baseline' and code:raise SystemExit(code)
for rel,digest in record['overlay'].items():assert hashlib.sha256((OWNER/rel).read_bytes()).hexdigest()==digest
record['ownerCsharpOverlayUnchanged']=True;save()
