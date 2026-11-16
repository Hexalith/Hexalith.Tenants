"""Finite private non-hash contract controls on byte-bound b34 inputs; never full mutation qualification."""
from pathlib import Path
import hashlib, io, json, os, resource, shutil, subprocess, tarfile, time

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
SOURCE = 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
BUILDS = '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
OUT = BASE / 'attempt-01'
OUT.mkdir(exist_ok=False)
WORK = Path(os.environ['TMPDIR']) / 'FrontComposer'
ledger = {row['id']: row for row in json.loads((BASE.parent / 'fc-error-followthrough-analysis/per-id-ledger.json').read_text())['mutants']}
record = {'scope': __doc__, 'source_base_commit': SOURCE, 'builds_commit': BUILDS, 'workspace': str(WORK),
          'private_test_sha256': {}, 'commands': [], 'variants': {}, 'archives': [], 'runner_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}

def save():
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')

def archive(repo, revision, target):
    raw = subprocess.check_output(['git', 'archive', revision], cwd=repo)
    record['archives'].append({'revision': revision, 'sha256': hashlib.sha256(raw).hexdigest()})
    target.mkdir(parents=True, exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(raw)) as source:
        source.extractall(target, members=[member for member in source.getmembers() if not member.name.startswith(('references/', '.agents/skills/', '.codex/skills/'))], filter='data')

assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER).decode().strip() == SOURCE
archive(OWNER, SOURCE, WORK)
assert subprocess.check_output(['git', 'rev-parse', SOURCE + ':references/Hexalith.Builds'], cwd=OWNER).decode().strip() == BUILDS
archive(ROOT / 'references/Hexalith.Builds', BUILDS, WORK / 'references/Hexalith.Builds')
for path in BASE.glob('*.cs'):
    record['private_test_sha256'][path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
    shutil.copy2(path, WORK / 'tests/Hexalith.FrontComposer.SourceTools.Tests/Mutation' / path.name)
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR']) / 'dotnet-home'), NUGET_PACKAGES=str(Path(os.environ['TMPDIR']) / 'packages'),
                  NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR']) / 'http-cache'), MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_USE_MSBUILD_SERVER='0',
                  UseSharedCompilation='false', UseMicrosoftTestingPlatformRunner='true', DOTNET_PROCESSOR_COUNT='2', DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')

def run(name, args):
    log = OUT / (name + '.log')
    started = time.monotonic()
    with log.open('x') as stream:
        result = subprocess.run(args, cwd=WORK, stdout=stream, stderr=subprocess.STDOUT)
    record['commands'].append({'name': name, 'command': args, 'exit_code': result.returncode, 'elapsed_seconds': time.monotonic() - started,
                               'children_maxrss_kib': resource.getrusage(resource.RUSAGE_CHILDREN).ru_maxrss, 'log': log.name, 'sha256': hashlib.sha256(log.read_bytes()).hexdigest()})
    save()
    print(name, result.returncode, flush=True)
    return result.returncode

classes = {'ModelHashFieldParticipationTests': 1}
plan = [(ident, 'ModelHashFieldParticipationTests') for ident in ['13180','13382','13419','13178','13179']]
originals = {ledger[ident]['path']: (WORK / ledger[ident]['path']).read_bytes() for ident, _ in plan}
project = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
previous = None
for ident, selected in [('baseline', None), *plan]:
    if previous:
        (WORK / previous).write_bytes(originals[previous])
    selected_classes = [selected] if selected else list(classes)
    if ident != 'baseline':
        row = ledger[ident]
        path = row['path']
        source = (WORK / path).read_text()
        lines = source.splitlines(keepends=True)
        start, end = row['location']['start'], row['location']['end']
        a = sum(map(len, lines[:start['line'] - 1])) + start['column'] - 1
        b = sum(map(len, lines[:end['line'] - 1])) + end['column'] - 1
        assert source[a:b] == row['original'].replace('\r\n', '\n')
        changed = source[:a] + row['replacement'].replace('\r\n', '\n') + source[b:]
        (WORK / path).write_text(changed)
        previous = path
        record['variants'][ident] = {'path': path, 'original_status': row['observed_status'], 'original_location': row['location'],
                                   'original': row['original'], 'replacement': row['replacement'], 'class': selected,
                                   'baseline_source_sha256': hashlib.sha256(source.encode()).hexdigest(), 'literal_source_sha256': hashlib.sha256(changed.encode()).hexdigest()}
        copy = OUT / ident / path
        copy.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(WORK / path, copy)
    save()
    code = run(ident + '-build', ['dotnet', 'build', project, '-c', 'Release', '-p:UseHexalithProjectReferences=false', '-p:UseMicrosoftTestingPlatformRunner=true',
        '-p:GeneratePackageOnBuild=false', '-m:1', '-nodeReuse:false'])
    if code:
        if ident == 'baseline':
            raise SystemExit(code)
        record['variants'][ident]['compile_refused'] = True
        save()
        continue
    filters = [arg for name in selected_classes for arg in ('--filter-class', 'Hexalith.FrontComposer.SourceTools.Tests.PrivateFieldParticipation.' + name)]
    code = run(ident + '-tests', ['dotnet', 'test', '--project', project, '--no-build', '--configuration', 'Release', *filters,
        '--minimum-expected-tests', str(sum(classes[name] for name in selected_classes)), '--fail-skips', 'on', '--results-directory', str(OUT / (ident + '-results')),
        '--report-xunit-trx', '--report-xunit-trx-filename', 'tests.trx', '-p:UseHexalithProjectReferences=false', '-p:UseMicrosoftTestingPlatformRunner=true'])
    if ident == 'baseline' and code or code not in (0, 2):
        raise SystemExit(code)
