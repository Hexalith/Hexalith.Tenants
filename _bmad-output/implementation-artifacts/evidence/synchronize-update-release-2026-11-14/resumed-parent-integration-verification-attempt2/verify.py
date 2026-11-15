"""Parent verification of the stable post-review root source snapshot."""
import datetime,hashlib,json,os,shutil,subprocess,sys,time,xml.etree.ElementTree as ET
from pathlib import Path
ROOT=Path(__file__).resolve().parents[5]
OUT=ROOT/"_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration-verification-attempt2"
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
for rel in ["_bmad-output/implementation-artifacts/tests/test-summary.md","_bmad-output/implementation-artifacts/story-1-8-support-safe-identifier-copy-and-read-experience-evidence-2026-07-21.md","_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/verify-current.py","_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/verify.sh"]:copy(ROOT,rel,WORKSPACE)
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
for script in []:
 run(script[:-3],["python3","tests/scripts/"+script])
project="Hexalith.Tenants.IntegrationTests"
run(project+"-build",["dotnet","build","tests/"+project+"/"+project+".csproj",*common])
env["ASPIRE_TEST_PROJECT"]="tests/Hexalith.Tenants.IntegrationTests"
try:
 run(project+"-tests",["bash","scripts/run-aspire-integration-tier.sh"])
finally:
 for report in (WORKSPACE/"TestResults/aspire").glob("*.trx"):shutil.copy2(report,OUT/report.name)
ns={"t":"http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
tree=ET.parse(OUT/"aspire-results.trx")
c=tree.find(".//t:Counters",ns);assert c is not None
counts={k:int(c.get(k,"0")) for k in ["total","executed","passed","failed","notExecuted"]}
assert counts["passed"]>=671 and counts["failed"]==counts["notExecuted"]==0,counts
receipt["checks"][-1]["counts"]=counts
(OUT/"verification.json").write_text(json.dumps(receipt,indent=2)+"\n")
print(json.dumps({"project":project,"counts":counts}),flush=True)
receipt["copied_input_hashes_still_match"]=all(hashlib.sha256((WORKSPACE/path).read_bytes()).hexdigest()==value for path,value in receipt["inputs"].items())
assert receipt["copied_input_hashes_still_match"]
(OUT/"verification.json").write_text(json.dumps(receipt,indent=2)+"\n")
