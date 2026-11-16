"""Run existing fixture and secret checks in a private exact-source repository copy."""
from pathlib import Path
import datetime
import hashlib
import json
import os
import shutil
import subprocess
import tarfile

ROOT = Path(__file__).resolve().parents[6]
OWNER = ROOT / 'references/Hexalith.EventStore'
OUT = Path(__file__).resolve().parent / 'attempt-01'
OUT.mkdir(exist_ok=False)
SCRATCH = Path(os.environ['TMPDIR'])
WORK = SCRATCH / 'eventstore'
env = os.environ.copy()
env.update(PYTHONDONTWRITEBYTECODE='1', DOTNET_PROCESSOR_COUNT='4',
           NUGET_PACKAGES=str(SCRATCH / 'packages'),
           NUGET_HTTP_CACHE_PATH=str(SCRATCH / 'http-cache'),
           DOTNET_CLI_HOME=str(SCRATCH / 'dotnet-home'),
           DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1',
           MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_USE_MSBUILD_SERVER='0',
           UseSharedCompilation='false')
record = {'startedAtUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'scope': 'Modified97c working-tree fixtures, original guard/test classes, frozen direct build inputs. No committed-source CI, full suite or publication qualification.',
          'inputs': {}, 'checks': []}


def git(base, *args):
    return subprocess.check_output(['git', '-c', 'core.fsmonitor=false', '-c', 'submodule.recurse=false', *args], cwd=base).decode().strip()


def snapshot(base, revision, destination):
    destination.mkdir(parents=True, exist_ok=True)
    process = subprocess.Popen(['git', 'archive', revision], cwd=base, stdout=subprocess.PIPE)
    with tarfile.open(fileobj=process.stdout, mode='r|') as archive:
        archive.extractall(destination, filter='data')
    if process.wait() != 0:
        raise ValueError('Source archive failed')
    subprocess.run(['git', 'init', '-q', str(destination)], check=True)
    common = Path(git(base, 'rev-parse', '--git-common-dir'))
    if not common.is_absolute():
        common = (base / common).resolve()
    (destination / '.git/objects/info/alternates').write_text(str(common / 'objects') + '\n')
    (destination / '.git/HEAD').write_text(revision + '\n')
    subprocess.run(['git', '-C', str(destination), 'read-tree', revision], check=True)
    subprocess.run(['git', '-C', str(destination), 'config', 'remote.origin.url', git(base, 'remote', 'get-url', 'origin')], check=True)


def run(label, command, cwd=WORK, timeout=240):
    log = OUT / (label + '.txt')
    with log.open('x') as stream:
        result = subprocess.run(command, cwd=cwd, env=env, stdout=stream, stderr=subprocess.STDOUT, timeout=timeout)
    row = {'name': label, 'command': command, 'exitCode': result.returncode,
           'log': log.name, 'logSha256': hashlib.sha256(log.read_bytes()).hexdigest()}
    record['checks'].append(row)
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')
    print(json.dumps(row), flush=True)
    if result.returncode:
        print(log.read_text()[-7000:], flush=True)
        raise SystemExit(result.returncode)


head = git(OWNER, 'rev-parse', 'HEAD')
record['baseSource'] = head
snapshot(OWNER, head, WORK)
changes = git(OWNER, 'diff', '--name-only').splitlines()
expected = set(json.loads((OUT.parent / 'source-change.json').read_text())['inputs'])
if set(changes) != expected:
    raise ValueError('Unexpected working source changes')
for relative in changes:
    source = OWNER / relative
    record['inputs'][relative] = hashlib.sha256(source.read_bytes()).hexdigest()
    shutil.copy2(source, WORK / relative)
for name, revision in [('Hexalith.Builds', 'e2bbbd0893f586e4d447ef8358ff747ea3d34435'),
                       ('Hexalith.Platform', '00ed06074f5b67522eff81dc8dae6ebd28161c36')]:
    snapshot(ROOT / 'references' / name, revision, WORK / 'references' / name)
shutil.copy2(WORK / 'global.json', SCRATCH / 'global.json')
for name in ['test_phase_4_transition.py', 'test_phase_4_gate_decision.py']:
    run(name, ['python3', 'tools/tests/' + name])

# Link the unchanged original guard class into a minimal pinned native runner.
# Its output stays beneath the snapshot, so its actual repository-root discovery
# and Git tracked-file scanner exercise the full original index plus the overlay.
project = SCRATCH / 'guard-runner'
project.mkdir()
guard_source = WORK / 'tests/Hexalith.EventStore.Server.Tests/Security/SecretsProtectionTests.cs'
output = WORK / '.verification-bin'
(project / 'guard.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><TargetFramework>net11.0</TargetFramework><OutputType>Exe</OutputType>
    <ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><LangVersion>latest</LangVersion>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors><IsTestProject>true</IsTestProject>
    <BaseOutputPath>{output}/</BaseOutputPath></PropertyGroup>
  <ItemGroup><PackageReference Include="xunit.v3" Version="4.0.3" />
    <PackageReference Include="Shouldly" Version="4.3.0" />
    <Using Include="Xunit" /><Compile Include="{guard_source}" Link="SecretsProtectionTests.cs" /></ItemGroup>
</Project>\n''')
run('guard-build', ['dotnet', 'build', str(project / 'guard.csproj'), '-c', 'Release', '-m:1', '-nr:false', '-warnaserror', '-p:RestoreFallbackFolders=/home/administrator/.nuget/packages'], cwd=project)
assembly = output / 'Release/net11.0/guard.dll'
run('original-secret-guard', ['dotnet', str(assembly), '-class', '*SecretsProtectionTests', '-maxThreads', '2', '-result-xml', str(OUT / 'secret-guard.xml')])
target = 'tests/Hexalith.EventStore.Testing.Integration.Tests/Hexalith.EventStore.Testing.Integration.Tests.csproj'
common = ['-c', 'Release', '-m:1', '-nr:false', '-warnaserror', '-p:UseHexalithProjectReferences=false', '-p:UseNuGetDeps=true', '-p:MinVerVersionOverride=5.1.1-local.fixture', '-p:RestoreFallbackFolders=/home/administrator/.nuget/packages']
run('diagnostics-build', ['dotnet', 'build', target, *common])
assembly = WORK / 'tests/Hexalith.EventStore.Testing.Integration.Tests/bin/Release/net11.0/Hexalith.EventStore.Testing.Integration.Tests.dll'
run('diagnostic-class', ['dotnet', str(assembly), '-class', '*GeneratedApiSmokePreflightDiagnosticsTests', '-maxThreads', '2', '-result-xml', str(OUT / 'diagnostics.xml')])
record['sourceUnchanged'] = git(OWNER, 'rev-parse', 'HEAD') == head and all(hashlib.sha256((OWNER / path).read_bytes()).hexdigest() == digest for path, digest in record['inputs'].items())
if not record['sourceUnchanged']:
    raise ValueError('Owner source changed during verification')
record['completedAtUtc'] = datetime.datetime.now(datetime.timezone.utc).isoformat()
(OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')
