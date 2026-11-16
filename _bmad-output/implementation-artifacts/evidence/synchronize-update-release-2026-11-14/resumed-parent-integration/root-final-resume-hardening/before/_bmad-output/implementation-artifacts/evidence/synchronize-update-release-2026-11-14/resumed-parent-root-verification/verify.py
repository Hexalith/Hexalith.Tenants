"""Parent verification of the stable post-review root source snapshot."""
import datetime,hashlib,json,os,shutil,subprocess,sys,time,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
OUT=ROOT/"_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-root-verification"
OUT.mkdir(exist_ok=True)
WORKSPACE=Path(os.environ["TMPDIR"])/"workspace";WORKSPACE.mkdir()
receipt={"captured_at_utc":datetime.datetime.now(datetime.timezone.utc).isoformat(),"root_head":subprocess.check_output(["git","rev-parse","HEAD"],text=True).strip(),"scope":"Byte-bound modified working-tree package-mode snapshot; public EventStore5.1.1 and FrontComposer6.0.0. This does not supply final committed-source CI or publication provenance.","inputs":{},"dependencies":{},"checks":[]}
def copy(base,rel,dest):
 p=base/rel
 if not p.is_file():return
 before=hashlib.sha256(p.read_bytes()).hexdigest()
 target=dest/rel;target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
 after=hashlib.sha256(target.read_bytes()).hexdigest()
 assert before==after==hashlib.sha256(p.read_bytes()).hexdigest(),rel
 receipt["inputs"][target.relative_to(WORKSPACE).as_posix()]=after
for rel in subprocess.check_output(["git","ls-files","--cached","--others","--exclude-standard","-z"]).decode().split("\0"):
 if rel and not rel.startswith(("references/","_bmad-output/","_bmad/",".agents/",".codex/",".claude/",".cursor/")):copy(ROOT,rel,WORKSPACE)
for rel in ["_bmad-output/implementation-artifacts/tests/test-summary.md","_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/verify-current.py","_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/verify.sh"]:copy(ROOT,rel,WORKSPACE)
for name in ["Hexalith.Builds","Hexalith.Platform","Hexalith.EventStore"]:
 base=ROOT/"references"/name
 revision=subprocess.check_output(["git","rev-parse","HEAD"],cwd=base,text=True).strip()
 assert not subprocess.check_output(["git","status","--porcelain","--untracked-files=no"],cwd=base),name
 receipt["dependencies"][name]=revision
 for rel in subprocess.check_output(["git","ls-files","-z"],cwd=base).decode().split("\0"):
  if rel and ("/" not in rel or name=="Hexalith.Builds" and rel.startswith(("Props/","Tools/","schemas/","src/libraries/Hexalith.Builds.Tooling/")) or name=="Hexalith.EventStore" and rel.startswith(("src/","docs/"))):copy(base,rel,WORKSPACE/"references"/name)
 assert revision==subprocess.check_output(["git","rev-parse","HEAD"],cwd=base,text=True).strip(),name
(WORKSPACE/".git").mkdir()
env=os.environ.copy();env.update(NUGET_PACKAGES=str(Path(os.environ["TMPDIR"])/"packages"),NUGET_HTTP_CACHE_PATH=str(Path(os.environ["TMPDIR"])/"http-cache"),DOTNET_CLI_HOME=str(Path(os.environ["TMPDIR"])/"dotnet-home"),DOTNET_NOLOGO="1",DOTNET_CLI_TELEMETRY_OPTOUT="1",MSBUILDDISABLENODEREUSE="1",DOTNET_CLI_USE_MSBUILD_SERVER="0",UseSharedCompilation="false",DOTNET_PROCESSOR_COUNT="4")
common=["-c","Release","-m:1","-nr:false","-warnaserror","-p:UseHexalithProjectReferences=false","-p:UseNuGetDeps=true","-p:HexalithEventStoreFromSource=false","-p:HexalithFrontComposerFromSource=false","-p:HexalithMemoriesFromSource=false","-p:RestoreFallbackFolders=/home/administrator/.nuget/packages","-p:MinVerVersionOverride=8.0.0-local.finalreview","-p:RepositoryCommit="+receipt["root_head"]]
def run(label,args):
 log=OUT/(label+".txt");start=time.monotonic()
 with log.open("w") as f:p=subprocess.run(args,cwd=WORKSPACE,env=env,stdout=f,stderr=subprocess.STDOUT,timeout=1200)
 row={"name":label,"command":args,"exit_code":p.returncode,"seconds":round(time.monotonic()-start,2),"log":log.name,"log_sha256":hashlib.sha256(log.read_bytes()).hexdigest()}
 receipt["checks"].append(row);(OUT/"verification.json").write_text(json.dumps(receipt,indent=2)+"\n")
 print(json.dumps(row),flush=True)
 if p.returncode:print(log.read_text()[-15000:],flush=True);raise SystemExit(p.returncode)
for script in ["test_catalog_package_consumer.py","test_bootstrap_compatibility.py","test_release_resume_verification.py","test_catalog_artifact_guards.py"]:
 run(script[:-3],["python3","tests/scripts/"+script])
for suffix in ["Server.Tests","Client.Tests","Testing.Tests","UI.Tests","Contracts.Tests"]:
 project="Hexalith.Tenants."+suffix
 run(project+"-build",["dotnet","build","tests/"+project+"/"+project+".csproj",*common])
 args=["dotnet","tests/"+project+"/bin/Release/net11.0/"+project+".dll","-maxThreads","4"]
 if suffix=="Contracts.Tests":args+=["-class","*PackageGovernanceTests","-class","*PublishedTenantCatalogTests"]
 xml=OUT/(project+".xml");args+=["-result-xml",str(xml)]
 run(project+"-tests",args)
 a=ET.parse(xml).getroot().find("assembly");assert a is not None
 counts={key:int(a.get(key,"0")) for key in ["passed","failed","skipped","total"]}
 assert counts["passed"]>0 and counts["failed"]==counts["skipped"]==0,counts
 receipt["checks"][-1]["counts"]=counts
 (OUT/"verification.json").write_text(json.dumps(receipt,indent=2)+"\n")
 print(json.dumps({"project":project,"counts":counts}),flush=True)
receipt["copied_input_hashes_still_match"]=all(hashlib.sha256((WORKSPACE/path).read_bytes()).hexdigest()==value for path,value in receipt["inputs"].items())
assert receipt["copied_input_hashes_still_match"]
(OUT/"verification.json").write_text(json.dumps(receipt,indent=2)+"\n")
