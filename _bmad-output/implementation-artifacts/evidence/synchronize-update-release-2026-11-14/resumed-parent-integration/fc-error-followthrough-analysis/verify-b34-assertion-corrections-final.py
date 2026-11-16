"""Focused current b34 working-input verification of the two corrected test classes; not mutation qualification."""
from pathlib import Path
import difflib, hashlib, io, json, os, shutil, subprocess, tarfile

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
SOURCE = 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
BUILDS = '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
OUT = BASE / 'b34-assertion-corrections-09'
OUT.mkdir(exist_ok=False)
WORK = Path(os.environ['TMPDIR']) / 'FrontComposer'
TEST_DIRECTORY = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation'
record = {'scope': __doc__, 'source_base_commit': SOURCE, 'builds_commit': BUILDS,
          'workspace': str(WORK), 'commands': [], 'test_overlay_sha256': {}, 'archives': {}}

def save():
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')

def archive(repo, revision, target):
    raw = subprocess.check_output(['git', 'archive', revision], cwd=repo)
    record['archives'][str(target.relative_to(WORK))] = {'revision': revision, 'tar_sha256': hashlib.sha256(raw).hexdigest()}
    target.mkdir(parents=True, exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(raw)) as source:
        source.extractall(target, members=[member for member in source.getmembers()
            if not member.name.startswith(('references/', '.agents/skills/', '.codex/skills/'))], filter='data')

assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER).decode().strip() == SOURCE
record['owner_status_before'] = subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode()
archive(OWNER, SOURCE, WORK)
assert subprocess.check_output(['git', 'rev-parse', SOURCE + ':references/Hexalith.Builds'], cwd=OWNER).decode().strip() == BUILDS
archive(ROOT / 'references/Hexalith.Builds', BUILDS, WORK / 'references/Hexalith.Builds')

patch = []
for source in sorted((BASE / 'private-test-proposal').glob('*.cs')):
    target = TEST_DIRECTORY + '/' + source.name
    assert source.read_bytes() == (OWNER / target).read_bytes()
    record['test_overlay_sha256'][target] = hashlib.sha256(source.read_bytes()).hexdigest()
    patch.append('diff --git a/' + target + ' b/' + target + '\nnew file mode 100644\n')
    patch.extend(difflib.unified_diff([], source.read_text().splitlines(keepends=True), fromfile='/dev/null', tofile='b/' + target))
assert len(record['test_overlay_sha256']) == 4
destination = BASE / 'private-test-proposal.patch'
destination.write_text(''.join(patch))
record['patch_sha256'] = hashlib.sha256(destination.read_bytes()).hexdigest()
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR']) / 'dotnet-home'), NUGET_PACKAGES=str(Path(os.environ['TMPDIR']) / 'packages'),
                  NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR']) / 'http-cache'), MSBUILDDISABLENODEREUSE='1',
                  DOTNET_CLI_USE_MSBUILD_SERVER='0', UseSharedCompilation='false', UseMicrosoftTestingPlatformRunner='true',
                  DOTNET_PROCESSOR_COUNT='2', DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')

def run(name, args):
    log = OUT / (name + '.log')
    with log.open('x') as stream:
        result = subprocess.run(args, cwd=WORK, stdout=stream, stderr=subprocess.STDOUT)
    record['commands'].append({'name': name, 'command': args, 'exit_code': result.returncode,
                               'log': log.name, 'sha256': hashlib.sha256(log.read_bytes()).hexdigest()})
    save()
    print(name, result.returncode, flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)

run('prospective-patch-check', ['git', 'apply', '--check', str(destination)])
for source in (BASE / 'private-test-proposal').glob('*.cs'):
    shutil.copy2(source, WORK / TEST_DIRECTORY / source.name)
project = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
run('focused-build', ['dotnet', 'build', project, '-c', 'Release', '-p:UseHexalithProjectReferences=false',
    '-p:UseMicrosoftTestingPlatformRunner=true', '-p:GeneratePackageOnBuild=false', '-p:RunAnalyzers=false', '-m:1', '-nodeReuse:false'])
run('focused-tests', ['dotnet', 'test', '--project', project, '--no-build', '--configuration', 'Release',
    '--filter-class', 'Hexalith.FrontComposer.SourceTools.Tests.Mutation.ErrorFollowthroughBehaviorTests',
    '--filter-class', 'Hexalith.FrontComposer.SourceTools.Tests.Mutation.ProjectionParserSurvivorRegressionTests',
    '--minimum-expected-tests', '38', '--fail-skips', 'on', '--results-directory', str(OUT / 'results'),
    '--report-xunit-trx', '--report-xunit-trx-filename', 'tests.trx', '-p:UseHexalithProjectReferences=false', '-p:UseMicrosoftTestingPlatformRunner=true'])
