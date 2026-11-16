"""Index exact original e0 problem spans and local source call sites, without owner edits."""
from pathlib import Path
import hashlib,io,json,os,shutil,subprocess,tarfile
EVIDENCE=Path(__file__).resolve().parent
ROOT=EVIDENCE.parents[5]
OWNER=ROOT/'references/Hexalith.FrontComposer'
INPUT=EVIDENCE.parent/'fc-error-local-diagnostic/attempt-01'
WORK=Path(os.environ['TMPDIR'])/'FrontComposer'
SOURCE='e0eef14ddbbcd03823863a4f417a4b07f6f5bafc'
if (EVIDENCE/'source-index.json').exists():raise SystemExit('Existing source index is immutable')
WORK.mkdir()
with tarfile.open(fileobj=io.BytesIO(subprocess.check_output(['git','archive',SOURCE],cwd=OWNER))) as archive:
 archive.extractall(WORK,members=[member for member in archive.getmembers() if not member.name.startswith(('references/','.agents/skills/','.codex/skills/'))],filter='data')
project=Path(os.environ['TMPDIR'])/'scan';project.mkdir();shutil.copy2(EVIDENCE/'Scan.cs',project/'Program.cs')
(project/'Scan.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>''')
(project/'global.json').write_text('{"sdk":{"version":"11.0.100","rollForward":"latestPatch"}}')
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR'])/'dotnet-home'),NUGET_PACKAGES=str(Path(os.environ['TMPDIR'])/'packages'),NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR'])/'http-cache'),MSBUILDDISABLENODEREUSE='1',DOTNET_CLI_USE_MSBUILD_SERVER='0',UseSharedCompilation='false',DOTNET_NOLOGO='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
records={'source':SOURCE,'workspace':str(WORK),'input_report_sha256':hashlib.sha256((INPUT/'original-error-artifacts/reports/source-tools-error-handling.json').read_bytes()).hexdigest(),'scanner_sha256':hashlib.sha256((EVIDENCE/'Scan.cs').read_bytes()).hexdigest(),'commands':[]}
for name,args,output in [('build',['dotnet','build',str(project/'Scan.csproj'),'-c','Release','-m:1','-nodeReuse:false'],EVIDENCE/'scan-build.log'),('index',['dotnet',str(project/'bin/Release/net11.0/Scan.dll'),str(WORK),str(INPUT/'original-error-artifacts/reports/source-tools-error-handling.json'),str(INPUT/'problem-mutants.json')],EVIDENCE/'source-index.json')]:
 with output.open('x') as stream:result=subprocess.run(args,cwd=project,stdout=stream,stderr=subprocess.STDOUT)
 records['commands'].append({'command':args,'exit_code':result.returncode,'output':output.name,'sha256':hashlib.sha256(output.read_bytes()).hexdigest()})
 (EVIDENCE/'scan-verification.json').write_text(json.dumps(records,indent=2)+'\n')
 print(name,result.returncode,flush=True)
 if result.returncode:raise SystemExit(result.returncode)
