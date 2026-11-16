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
assert len(paths)==11,paths
for rel in paths:
 record['overlay'][rel]=hashlib.sha256((OWNER/rel).read_bytes()).hexdigest();shutil.copy2(OWNER/rel,WORK/rel)
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',UseMicrosoftTestingPlatformRunner='true',DOTNET_PROCESSOR_COUNT='2',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')

import collections,signal,xml.etree.ElementTree as ET
counts=collections.Counter()
for x in ET.parse(BASE.parent/'fc-combined-regressions-attempt2/results/tests.trx').getroot().iter():
 if x.tag.rsplit('}',1)[-1]=='TestMethod':counts[x.attrib['className'].split('.')[-1]]+=1
assert sum(counts.values())==77,counts
record['classCounts']=dict(counts);record['controls']=[]
project='tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
rows=json.loads((BASE/'plan.json').read_text())['rows']
originals={r['path']:(WORK/r['path']).read_bytes() for r in rows}
def run(name,args,limit=240):
 start=time.monotonic();timedout=False
 with (BASE/(name+'.log')).open('x') as f:
  proc=subprocess.Popen(args,cwd=WORK,stdout=f,stderr=subprocess.STDOUT,start_new_session=True)
  try:code=proc.wait(timeout=limit)
  except subprocess.TimeoutExpired:
   timedout=True;os.killpg(proc.pid,signal.SIGTERM)
   try:proc.wait(timeout=10)
   except subprocess.TimeoutExpired:os.killpg(proc.pid,signal.SIGKILL);proc.wait()
   code=124
 record['commands'].append({'name':name,'command':args,'exitCode':code,'timedOut':timedout,'elapsedSeconds':time.monotonic()-start});save();return code
def selected(row):
 name=row['member'].split('.')[0]
 if name=='AttributeParser':return ['ProjectionParserSurvivorRegressionTests','ErrorFollowthroughBehaviorTests']
 if name=='CommandParser':return ['CommandContractBoundaryTests','AliasSignatureContractTests']
 if name=='ProjectionTemplateMarkerParser':return ['TemplateContractBoundaryTests','TemplateParserProviderBoundaryTests']
 if name=='SourceTypeNameFormatter':return ['AliasSignatureContractTests']
 if name=='ColumnModel':return ['ColumnFilterContractTests']
 if name in ['RazorModelTransform']:return ['RazorRoleRecoveryContractTests']
 if name in ['FieldTypeMapper','CamelCaseHumanizer','McpManifestTransform','SchemaFingerprintTransform','CommandFormTransform']:return ['ErrorFollowthroughBehaviorTests']
 return list(counts)
build=['dotnet','build',project,'-c','Release','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true','-p:GeneratePackageOnBuild=false','-p:RunAnalyzers=false','-m:1','-nodeReuse:false','--nologo']
if run('baseline-build',build):raise SystemExit(1)
last=None
for i,row in enumerate(rows):
 if last:(WORK/last).write_bytes(originals[last])
 rel=row['path'];source=originals[rel].decode().replace('\r\n','\n');lines=source.splitlines(keepends=True);start,end=row['location']['start'],row['location']['end'];a=sum(map(len,lines[:start['line']-1]))+start['column']-1;b=sum(map(len,lines[:end['line']-1]))+end['column']-1
 assert source[a:b]==row['original'].replace('\r\n','\n'),row['id']
 changed=source[:a]+row['replacement'].replace('\r\n','\n')+source[b:];(WORK/rel).write_text(changed);last=rel
 result={'id':row['id'],'originalStatus':row['localOriginalStatus'],'member':row['member'],'sourceSha256':hashlib.sha256(originals[rel]).hexdigest(),'variantSha256':hashlib.sha256(changed.encode()).hexdigest(),'classFilters':selected(row)}
 code=run(row['id']+'-build',build)
 if code:result['result']='compile-refused';result['buildExit']=code
 else:
  names=result['classFilters'];filters=[v for c in names for v in ('--filter-class','Hexalith.FrontComposer.SourceTools.Tests.Mutation.'+c)];dest=BASE/'results'/row['id']
  code=run(row['id']+'-tests',['dotnet','test','--project',project,'--no-build','--configuration','Release',*filters,'--minimum-expected-tests',str(sum(counts[n] for n in names)),'--fail-skips','on','--results-directory',str(dest),'--report-xunit-trx','--report-xunit-trx-filename','tests.trx','-p:UseHexalithProjectReferences=false','-p:UseMicrosoftTestingPlatformRunner=true'],limit=120)
  trx=dest/'tests.trx';failed=[]
  if trx.exists():
   data=trx.read_bytes();(dest/'tests.xml').write_bytes(data);result['nativeTrxSha256']=hashlib.sha256(data).hexdigest();doc=ET.fromstring(data);failed=[x.attrib.get('testName') for x in doc.iter() if x.tag.rsplit('}',1)[-1]=='UnitTestResult' and x.attrib.get('outcome')=='Failed'];result['nativeCounters']=[x.attrib for x in doc.iter() if x.tag.rsplit('}',1)[-1]=='Counters']
  result['result']='detected' if code==2 and failed else 'survived' if code==0 else 'timeout-or-infrastructure-refusal';result['testExit']=code;result['failedTests']=failed
 record['controls'].append(result);save();print(i+1,len(rows),row['id'],result['result'],flush=True)
for rel,digest in record['overlay'].items():assert hashlib.sha256((OWNER/rel).read_bytes()).hexdigest()==digest
record['ownerOverlayUnchanged']=True;save()
