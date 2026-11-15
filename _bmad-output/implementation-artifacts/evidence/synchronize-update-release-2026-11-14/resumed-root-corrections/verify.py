"""Public SDK 5.1.1 qualification from a byte-bound disposable root source copy."""
import datetime
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[5]
EVIDENCE = Path(__file__).resolve().parent
RECEIPT = EVIDENCE / ('packed-verification.json' if '--pack-only' in sys.argv else 'integration-verification.json' if '--integration-only' in sys.argv else 'verification.json')
WORKSPACE = Path(os.environ['TMPDIR']) / 'workspace'
WORKSPACE.mkdir()
results = {'captured_at_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
           'root_head': subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip(),
           'scope': 'Modified working-tree snapshot; local package API/behavior qualification only. Final committed-source hosted CI and publication are separate.',
           'workspace': str(WORKSPACE), 'inputs': {}, 'dependencies': {}, 'checks': []}


def copy_file(base, relative, target_base):
    source = base / relative
    if not source.is_file():
        return
    target = target_base / relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)
    digest = hashlib.sha256(target.read_bytes()).hexdigest()
    if digest != hashlib.sha256(source.read_bytes()).hexdigest():
        raise ValueError('Input changed while copying: ' + str(source))
    results['inputs'][str(target.relative_to(WORKSPACE))] = digest


paths = subprocess.check_output(['git', 'ls-files', '--cached', '--others', '--exclude-standard', '-z'], cwd=ROOT).decode().split('\0')
for relative in paths:
    if relative and not relative.startswith(('references/', '_bmad-output/')):
        copy_file(ROOT, relative, WORKSPACE)
for relative in (
    '_bmad-output/implementation-artifacts/tests/test-summary.md',
    '_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/verify-current.py',
    '_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/verify.sh',
):
    copy_file(ROOT, relative, WORKSPACE)
for name in (('Hexalith.Builds',) if '--pack-only' in sys.argv else ('Hexalith.Builds', 'Hexalith.Platform', 'Hexalith.EventStore')):
    base = ROOT / 'references' / name
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=base, text=True).strip()
    dirty = subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=no'], cwd=base, text=True)
    if dirty:
        if name != 'Hexalith.Platform':
            raise ValueError('Dependency source must be clean: ' + name)
        changed = subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], cwd=base, text=True).splitlines()
        if any('/' not in item for item in changed):
            raise ValueError('A selected Platform root input is dirty.')
        results['platform_uncopied_tracked_changes'] = dirty
    results['dependencies'][name] = revision
    for relative in subprocess.check_output(['git', 'ls-files', '-z'], cwd=base).decode().split('\0'):
        if relative and (name == 'Hexalith.EventStore' and relative.startswith(('src/', 'docs/')) or name == 'Hexalith.Builds' and relative.startswith(('Props/', 'Tools/', 'schemas/', 'src/libraries/Hexalith.Builds.Tooling/')) or '/' not in relative):
            copy_file(base, relative, WORKSPACE / 'references' / name)
    if subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=base, text=True).strip() != revision:
        raise ValueError('Dependency revision changed while copying: ' + name)
(WORKSPACE / '.git').mkdir()
os.environ['NUGET_PACKAGES'] = str(Path(os.environ['TMPDIR']) / 'packages')
os.environ['NUGET_HTTP_CACHE_PATH'] = str(Path(os.environ['TMPDIR']) / 'http-cache')
os.environ['DOTNET_CLI_HOME'] = str(Path(os.environ['TMPDIR']) / 'dotnet-home')
os.environ['DOTNET_NOLOGO'] = '1'
os.environ['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
# This platform's executable filesystem hosts the SDK; tmpfs holds copied inputs/caches/outputs.
common = ['-c', 'Release', '-m:1', '-nr:false', '-warnaserror', '-p:UseHexalithProjectReferences=false', '-p:UseNuGetDeps=true',
          '-p:HexalithEventStoreFromSource=false', '-p:HexalithFrontComposerFromSource=false', '-p:HexalithMemoriesFromSource=false',
          '-p:NuGetAudit=false', '-p:MinVerVersionOverride=8.0.0-local.resumed', '-p:RepositoryCommit=' + results['root_head']]


def run(label, args, expected=0):
    print(label, flush=True)
    with (EVIDENCE / (label + '.log')).open('w') as log:
        process = subprocess.run(args, cwd=WORKSPACE, stdout=log, stderr=subprocess.STDOUT)
    results['checks'].append({'name': label, 'command': args, 'exit_code': process.returncode, 'expected_exit_code': expected,
                             'log_sha256': hashlib.sha256((EVIDENCE / (label + '.log')).read_bytes()).hexdigest()})
    RECEIPT.write_text(json.dumps(results, indent=2) + '\n')
    print('\n'.join((EVIDENCE / (label + '.log')).read_text().splitlines()[-10:]), flush=True)
    if process.returncode != expected:
        raise SystemExit(process.returncode or 1)


for script in (() if '--skip-guards' in sys.argv or '--pack-only' in sys.argv or '--integration-only' in sys.argv else ('test_catalog_package_consumer.py', 'test_bootstrap_compatibility.py', 'test_release_resume_verification.py', 'test_catalog_artifact_guards.py')):
    run(script.removesuffix('.py'), ['python3', 'tests/scripts/' + script])
for project in (() if '--pack-only' in sys.argv else ('IntegrationTests',) if '--integration-only' in sys.argv else ('Server.Tests', 'Client.Tests', 'Testing.Tests', 'Contracts.Tests', 'IntegrationTests')):
    name = 'Hexalith.Tenants.' + project
    run(name + '-build', ['dotnet', 'build', 'tests/' + name + '/' + name + '.csproj', *common])
    classes = ['*PackageGovernanceTests', '*PublishedTenantCatalogTests'] if project == 'Contracts.Tests' else [
        '*DaprEndToEndTests'] if project == 'IntegrationTests' else []
    args = ['dotnet', 'tests/' + name + '/bin/Release/net11.0/' + name + '.dll']
    for item in classes:
        args += ['-class', item]
    args += ['-result-xml', str(EVIDENCE / (name + '.xml'))]
    run(name + '-tests', args)
    assembly = ET.parse(EVIDENCE / (name + '.xml')).getroot().find('assembly')
    if assembly is None or int(assembly.get('failed', '0')) or int(assembly.get('skipped', '0')) or not int(assembly.get('passed', '0')):
        raise SystemExit('Qualification requires executed passing cases and no failures/skips: ' + name)
    results['checks'][-1]['counts'] = assembly.attrib
    RECEIPT.write_text(json.dumps(results, indent=2) + '\n')

if '--pack-only' in sys.argv:
    feed = Path(os.environ['TMPDIR']) / 'feed'
    feed.mkdir()
    manifest = json.loads((WORKSPACE / 'tools/release-packages.json').read_text())
    for row in manifest['packages']:
        run(row['id'] + '-pack', ['dotnet', 'pack', row['project'], *common,
            '-p:Version=8.0.0-local.resumed', '-p:PackageVersion=8.0.0-local.resumed', '--output', str(feed)])
    run('actual-package-validation', ['python3', 'scripts/validate-nuget-packages.py', str(feed)])
    run('actual-packed-catalog-consumer', ['python3', 'scripts/verify-catalog-package-consumer.py', str(feed),
        '--expected-source-sha', results['root_head'], '--output', str(EVIDENCE / 'packed-catalog-consumer.json')])
