import concurrent.futures,datetime,gzip,hashlib,io,json,os,pathlib,re,shutil,subprocess,tarfile,urllib.request
root=pathlib.Path(__file__).resolve().parents[5];scratch=pathlib.Path(os.environ['TMPDIR']);copy=scratch/'root';copy.mkdir();evidence=root/'_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration';evidence.mkdir(exist_ok=True)
output=evidence/'pre-fc-patch-stable-package-audit.json';assert not output.exists(),'Preserve the prior audit; use a new attempt filename.'
copied={}
previous=json.loads((root/'_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/root-postrepair-sdk-package-version-audit.json').read_text())
for relative in subprocess.check_output(['git','ls-files','-z']).decode().split('\0'):
 if not relative or relative.startswith(('references/','.agents/','.claude/','.agent/','_bmad/','_bmad-output/')):continue
 source=root/relative
 if source.is_file():
  target=copy/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target);copied[relative]=hashlib.sha256(target.read_bytes()).hexdigest()
builds=root/'references/Hexalith.Builds';selected=subprocess.check_output(['git','rev-parse','HEAD'],cwd=builds,text=True).strip();dest=copy/'references/Hexalith.Builds';dest.mkdir(parents=True)
with tarfile.open(fileobj=io.BytesIO(subprocess.check_output(['git','archive',selected,'Props'],cwd=builds))) as archive:archive.extractall(dest,filter='data')
env=dict(os.environ,NUGET_PACKAGES=str(scratch/'packages'),DOTNET_CLI_HOME=str(scratch/'dotnet-home'),DOTNET_NOLOGO='1');projects=[];selected_packages={}
for old in previous['projects']:
 project=old['project'];cmd=['dotnet','msbuild',project,'-nologo','-noAutoResponse','-nr:false','-p:Configuration=Release','-p:UseNuGetDeps=true','-p:UseHexalithProjectReferences=false','-p:HexalithFrontComposerFromSource=false','-p:HexalithMemoriesFromSource=false','-getProperty:TargetFramework,HexalithEventStoreVersion,HexalithMcpCliVersion,HexalithMemoriesVersion,HexalithFrontComposerVersion,HexalithTenantsVersion','-getItem:PackageReference,PackageVersion'];r=subprocess.run(cmd,cwd=copy,env=env,text=True,capture_output=True);assert r.returncode==0,(project,r.stdout,r.stderr);data=json.loads(r.stdout);versions={x['Identity']:x.get('Version','') for x in data['Items']['PackageVersion']};direct=[]
 for item in data['Items']['PackageReference']:
  identity=item['Identity'];version=item.get('VersionOverride') or item.get('Version') or versions.get(identity);assert version,(project,identity);direct.append({'id':identity,'version':version});selected_packages.setdefault(identity,{}).setdefault(version,[]).append(project)
 projects.append({'project':project,'command':cmd,'properties':data['Properties'],'directPackages':direct})
def get(url):
 with urllib.request.urlopen(url,timeout=60) as response:data=response.read()
 if data[:2]==b'\x1f\x8b':data=gzip.decompress(data)
 return json.loads(data)
metadata_url='https://builds.dotnet.microsoft.com/dotnet/release-metadata/11.0/releases.json';metadata=get(metadata_url);sdk=json.loads((copy/'global.json').read_text())['sdk'];assert sdk['version']==metadata['latest-sdk']
def latest(item):
 identity,versions=item;lower=identity.lower();url='https://api.nuget.org/v3-flatcontainer/'+lower+'/index.json';available=get(url)['versions'];stable=[v for v in available if re.fullmatch(r'\d+(?:\.\d+){1,3}',v)];stable.sort(key=lambda v:tuple(map(int,v.split('.')))+(0,)*(4-len(v.split('.'))),reverse=True);catalog=None;chosen=None
 for version in stable:
  leaf=get('https://api.nuget.org/v3/registration5-gz-semver2/'+lower+'/'+version+'.json');entry=leaf['catalogEntry'];catalog=get(entry) if isinstance(entry,str) else entry
  if catalog.get('listed') is True:chosen=version;break
 assert chosen,(identity,'No listed stable version was authenticated');rows=[]
 for version,consumers in versions.items():
  match=version==chosen
  if match:rationale='Selected latest listed stable release.'
  elif identity in ('Hexalith.EventStore.Contracts','Hexalith.EventStore.Testing.Integration') and version=='3.128.4':rationale='Explicit frozen bootstrap compatibility harness; never current package production.'
  elif identity=='Hexalith.McpCli.Abstractions' and version in ['2.0.0','2.0.1']:rationale='Retained exact admitted McpCli contract cohort. A new major native enrollment needs separate exact authority and qualification; existing native receipts do not transfer.'
  elif '-' in version:rationale='Intentional preview required by the existing Fluent UI V5 UI contract.'
  else:rationale='REQUIRES_COMPATIBILITY_REVIEW'
  rows.append({'selectedVersion':version,'matchesLatestListedStable':match,'consumers':consumers,'compatibilityRationale':rationale})
 return {'id':identity,'latestListedStableAtVerification':chosen,'selected':rows,'source':url,'listedMetadata':catalog['@id']}
with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:packages=list(pool.map(latest,sorted(selected_packages.items())))
assert all(hashlib.sha256((root/name).read_bytes()).hexdigest()==digest for name,digest in copied.items()),'Root inputs changed during evaluation'
record={'copiedRootInputSha256':copied,'capturedAtUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),'scope':'Fresh official SDK, actual18-project normal Release evaluation, official stable/listed NuGet metadata. This is a source-selection audit, not product tests, native enrollment or publication.','rootHead':subprocess.check_output(['git','rev-parse','HEAD'],text=True).strip(),'buildsSource':selected,'sdk':{'source':metadata_url,'latestSdk':metadata['latest-sdk'],'globalJson':sdk},'catalog':{'sha256Raw':hashlib.sha256((dest/'Props/Directory.Packages.props').read_bytes()).hexdigest()},'projects':projects,'packages':packages,'temporaryWorkspace':str(scratch),'disposable':True}
path=output;path.write_text(json.dumps(record,indent=2)+'\n');exceptions=[{'id':p['id'],'latest':p['latestListedStableAtVerification'],'selected':s['selectedVersion'],'rationale':s['compatibilityRationale']} for p in packages for s in p['selected'] if not s['matchesLatestListedStable']];print(json.dumps({'projects':len(projects),'packages':len(packages),'sdk':sdk['version'],'exceptions':exceptions},indent=2),flush=True)

assert not any(item['rationale']=='REQUIRES_COMPATIBILITY_REVIEW' for item in exceptions),'Unreviewed package selection requires a concrete compatibility decision.'
