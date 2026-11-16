"""Package a private test proposal and verify trivia-only adaptation; no owner source edits."""
from pathlib import Path
import difflib, hashlib, json, os, shutil, subprocess

BASE = Path(__file__).resolve().parent
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'
OUT = BASE / 'package-check-06'
OUT.mkdir(exist_ok=False)
WORK = Path(os.environ['TMPDIR']) / 'token-check'
WORK.mkdir()
shutil.copy2(BASE / 'ProposalStyleCheck.cs', WORK / 'Program.cs')
(WORK / 'TokenComparison.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>''')
(WORK / 'global.json').write_text('{"sdk":{"version":"11.0.100","rollForward":"latestPatch"}}')
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR']) / 'dotnet-home'), NUGET_PACKAGES=str(Path(os.environ['TMPDIR']) / 'packages'), NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR']) / 'http-cache'), MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_USE_MSBUILD_SERVER='0', UseSharedCompilation='false', DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_PROCESSOR_COUNT='2')
record = {'scope': __doc__, 'workspace': str(WORK), 'commands': []}

def run(name, args, cwd=WORK):
    output = OUT / name
    with output.open('x') as stream:
        result = subprocess.run(args, cwd=cwd, stdout=stream, stderr=subprocess.STDOUT)
    record['commands'].append({'command': args, 'exit_code': result.returncode, 'output': name, 'sha256': hashlib.sha256(output.read_bytes()).hexdigest()})
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')
    print(name, result.returncode, flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)

run('build.log', ['dotnet', 'build', str(WORK / 'TokenComparison.csproj'), '-c', 'Release', '-m:1', '-nodeReuse:false'])
run('style-token-check.json', ['dotnet', str(WORK / 'bin/Release/net11.0/TokenComparison.dll'), str(BASE / 'pre-fix-packet-2026-11-16-01/private-test-proposal'), str(BASE / 'private-test-proposal')])
patch = []
for source in sorted((BASE / 'private-test-proposal').glob('*.cs')):
    target = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation/' + source.name
    patch.append('diff --git a/' + target + ' b/' + target + '\n')
    patch.append('new file mode 100644\n')
    patch.extend(difflib.unified_diff([], source.read_text().splitlines(keepends=True), fromfile='/dev/null', tofile='b/' + target))
destination = BASE / 'private-test-proposal.patch'
with destination.open('w') as stream:
    stream.write(''.join(patch))
record['patch_sha256'] = hashlib.sha256(destination.read_bytes()).hexdigest()
run('apply-check.log', ['git', 'apply', '--check', str(destination)], cwd=OWNER)
