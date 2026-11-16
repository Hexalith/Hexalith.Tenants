"""Private finite hash-quality diagnostics on b34 tracked source plus preserved reviewed tests; never mutation qualification."""
from pathlib import Path
import hashlib, io, json, os, resource, shutil, subprocess, tarfile, time

BASE = Path(__file__).resolve().parent
ROOT = BASE.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
SOURCE = 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
BUILDS = '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
OUT = BASE / 'attempt-03'
OUT.mkdir(exist_ok=False)
WORK = Path(os.environ['TMPDIR']) / 'FrontComposer'
old = BASE.parent / 'fc-error-followthrough-analysis'
audit = json.loads((old / 'hash-consumer-audit.json').read_text())
index = json.loads((old / 'source-index.json').read_text())
ledger = {row['id']: row for row in json.loads((old / 'per-id-ledger.json').read_text())['mutants']}
bindings = json.loads((BASE / 'source-bindings.json').read_text())
record = {'scope': __doc__, 'source_base_commit': SOURCE, 'builds_commit': BUILDS, 'workspace': str(WORK),
          'private_source_sha256': {}, 'commands': [], 'phases': {}, 'instrumentation_slots': [],
          'resource_start': {'cpu_count': os.cpu_count(), 'meminfo': [line for line in Path('/proc/meminfo').read_text().splitlines() if line.startswith(('MemAvailable:', 'MemTotal:'))]}}

def save():
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')

def archive(repo, revision, target):
    raw = subprocess.check_output(['git', 'archive', revision], cwd=repo)
    record.setdefault('archives', []).append({'revision': revision, 'sha256': hashlib.sha256(raw).hexdigest()})
    target.mkdir(parents=True, exist_ok=True)
    with tarfile.open(fileobj=io.BytesIO(raw)) as source:
        source.extractall(target, members=[member for member in source.getmembers() if not member.name.startswith(('references/', '.agents/skills/', '.codex/skills/'))], filter='data')

assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER).decode().strip() == SOURCE
archive(OWNER, SOURCE, WORK)
assert subprocess.check_output(['git', 'rev-parse', SOURCE + ':references/Hexalith.Builds'], cwd=OWNER).decode().strip() == BUILDS
archive(ROOT / 'references/Hexalith.Builds', BUILDS, WORK / 'references/Hexalith.Builds')
for path, sha in bindings['working_input_overlay'].items():
    assert hashlib.sha256((OWNER / path).read_bytes()).hexdigest() == sha
    shutil.copy2(OWNER / path, WORK / path)
for path in BASE.glob('*.cs'):
    record['private_source_sha256'][path.name] = hashlib.sha256(path.read_bytes()).hexdigest()
    destination = WORK / ('src/Hexalith.FrontComposer.SourceTools/Parsing' if path.name == 'PrivateHashActivity.cs' else 'tests/Hexalith.FrontComposer.SourceTools.Tests/PrivateHashQuality') / path.name
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(path, destination)
originals = {row['path']: (WORK / row['path']).read_bytes() for row in audit['hash_members']}
for slot, row in enumerate(audit['hash_members']):
    record['instrumentation_slots'].append({'slot': slot, 'path': row['path'], 'member': row['member']})
record['instrumentation_slots'].append({'slot': 19, 'path': 'src/Hexalith.FrontComposer.SourceTools/Parsing/EquatableArray.cs', 'member': 'EquatableArray.Equals(EquatableArray<T>)'})
os.environ.update(DOTNET_CLI_HOME=str(Path(os.environ['TMPDIR']) / 'dotnet-home'), NUGET_PACKAGES=str(Path(os.environ['TMPDIR']) / 'packages'), NUGET_HTTP_CACHE_PATH=str(Path(os.environ['TMPDIR']) / 'http-cache'),
                  MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_USE_MSBUILD_SERVER='0', UseSharedCompilation='false', UseMicrosoftTestingPlatformRunner='true',
                  DOTNET_PROCESSOR_COUNT='2', DOTNET_NOLOGO='1', DOTNET_CLI_TELEMETRY_OPTOUT='1')

def run(name, args, expected=0):
    log = OUT / (name + '.log')
    started = time.monotonic()
    with log.open('x') as stream:
        result = subprocess.run(args, cwd=WORK, stdout=stream, stderr=subprocess.STDOUT)
    record['commands'].append({'name': name, 'command': args, 'exit_code': result.returncode, 'expected_exit': expected,
                               'elapsed_seconds': time.monotonic() - started, 'log': log.name, 'sha256': hashlib.sha256(log.read_bytes()).hexdigest(),
                               'children_maxrss_kib': resource.getrusage(resource.RUSAGE_CHILDREN).ru_maxrss})
    save()
    print(name, result.returncode, flush=True)
    if result.returncode != expected:
        raise SystemExit(result.returncode or 1)

project = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
all_classes = ['RelativeTimeHashParticipationTests', 'ArrayHashCollectionObservationTests', 'GeneratorHashCacheObservationTests', 'GeneratorIncrementalBudgetReplayTests']
phases = [('instrumented-baseline', None, True, all_classes[2:3]), ('instrumented13541', '13541', True, all_classes[2:3])]
for phase, ident, instrumented, classes in phases:
    for path, source in originals.items():
        (WORK / path).write_bytes(source)
    if ident:
        row = ledger[ident]
        source = (WORK / row['path']).read_text()
        lines = source.splitlines(keepends=True)
        start, end = row['location']['start'], row['location']['end']
        a = sum(map(len, lines[:start['line'] - 1])) + start['column'] - 1
        b = sum(map(len, lines[:end['line'] - 1])) + end['column'] - 1
        assert source[a:b] == row['original'].replace('\r\n', '\n')
        (WORK / row['path']).write_text(source[:a] + row['replacement'] + source[b:])
    if instrumented:
        edits = {}
        for row in record['instrumentation_slots']:
            source = (WORK / row['path']).read_text()
            if row['slot'] == 19:
                offset = source.index('{', source.index('public bool Equals(EquatableArray<T> other)')) + 1
            else:
                member = next(member for member in index['members'] if member['path'] == row['path'] and member['name'] == row['member'])
                offset = sum(map(len, source.splitlines(keepends=True)[:member['startLine'] - 1]))
                offset = source.index('{', offset) + 1
            edits.setdefault(row['path'], []).append((offset, '\n        global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(' + str(row['slot']) + ');'))
        for path, rows in edits.items():
            source = (WORK / path).read_text()
            for offset, insertion in sorted(rows, reverse=True):
                source = source[:offset] + insertion + source[offset:]
            (WORK / path).write_text(source)
    directory = OUT / phase
    directory.mkdir()
    for path in originals:
        destination = directory / 'instrumented-source' / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(WORK / path, destination)
    os.environ['FC_PRIVATE_HASH_OBSERVATIONS'] = str(directory)
    record['phases'][phase] = {'literal_id': ident, 'instrumented': instrumented, 'classes': classes,
                              'source_sha256': {path: hashlib.sha256((WORK / path).read_bytes()).hexdigest() for path in originals}}
    save()
    run(phase + '-build', ['dotnet', 'build', project, '-c', 'Release', '-p:UseHexalithProjectReferences=false', '-p:UseMicrosoftTestingPlatformRunner=true',
        '-p:GeneratePackageOnBuild=false', '-p:RunAnalyzers=false', '-m:1', '-nodeReuse:false'])
    filters = [arg for name in classes for arg in ('--filter-class', 'Hexalith.FrontComposer.SourceTools.Tests.PrivateHashQuality.' + name)]
    run(phase + '-tests', ['dotnet', 'test', '--project', project, '--no-build', '--configuration', 'Release', *filters, '--minimum-expected-tests', str(len(classes)),
        '--fail-skips', 'on', '--results-directory', str(directory / 'results'), '--report-xunit-trx', '--report-xunit-trx-filename', 'tests.trx',
        '-p:UseHexalithProjectReferences=false', '-p:UseMicrosoftTestingPlatformRunner=true'], expected=2 if ident == '13419' else 0)
