"""Verify retained review corrections against current source without mutation qualification."""
from pathlib import Path
import hashlib
import io
import json
import os
import subprocess
import tarfile
import time

OUT = Path(__file__).resolve().parent
ROOT = OUT.parents[5]
OWNER = ROOT / 'references/Hexalith.FrontComposer'
SCRATCH = Path(os.environ['TMPDIR'])
WORK = SCRATCH / 'Hexalith.FrontComposer'
record = {'scratch': str(SCRATCH), 'commands': [], 'archives': []}


def save():
    (OUT / 'verification.json').write_text(json.dumps(record, indent=2) + '\n')


def archive(repo, revision, destination):
    raw = subprocess.check_output(['git', 'archive', revision], cwd=repo)
    record['archives'].append({'repository': str(repo.relative_to(ROOT)), 'revision': revision,
                               'archiveSha256': hashlib.sha256(raw).hexdigest()})
    destination.mkdir(parents=True)
    with tarfile.open(fileobj=io.BytesIO(raw)) as source:
        source.extractall(destination, members=[item for item in source.getmembers()
            if not item.name.startswith(('references/', '.agents/skills/', '.codex/skills/'))], filter='data')


def run(name, arguments):
    start = time.monotonic()
    with (OUT / (name + '.log')).open('wb') as log:
        result = subprocess.run(arguments, cwd=WORK, env=environment, stdout=log, stderr=subprocess.STDOUT)
    entry = {'name': name, 'command': arguments, 'exitCode': result.returncode,
             'seconds': round(time.monotonic() - start, 3),
             'logSha256': hashlib.sha256((OUT / (name + '.log')).read_bytes()).hexdigest()}
    record['commands'].append(entry)
    save()
    print(json.dumps(entry), flush=True)
    return result.returncode


revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=OWNER, text=True).strip()
record['ownerHead'] = revision
archive(OWNER, revision, WORK)
builds_revision = subprocess.check_output(['git', 'rev-parse', revision + ':references/Hexalith.Builds'], cwd=OWNER, text=True).strip()
archive(ROOT / 'references/Hexalith.Builds', builds_revision, WORK / 'references/Hexalith.Builds')
environment = {**os.environ, 'PYTHONDONTWRITEBYTECODE': '1',
    'PUBLICATION_TEST_EVIDENCE': str(OUT / 'publication-controls'),
    'DOTNET_CLI_HOME': str(SCRATCH / 'dotnet-home'), 'NUGET_PACKAGES': str(SCRATCH / 'packages'),
    'XDG_CACHE_HOME': str(SCRATCH / 'cache'), 'DOTNET_PROCESSOR_COUNT': '2',
    'MSBUILDDISABLENODEREUSE': '1', 'DOTNET_CLI_USE_MSBUILD_SERVER': '0',
    'UseSharedCompilation': 'false', 'UseMicrosoftTestingPlatformRunner': 'true'}
methods = [
    'SafeFailedSegmentIsNormalizedWithoutChangingReceiptIdentity',
    'SafeFailedAggregatePreservesBothReportsAndNormalizesSummaries',
    'ActualUnsafeSegmentContentRefusesBeforeAnyNormalizationWrite',
    'ActualUnsafeAggregateSummaryAndEitherSegmentRefuse',
    'LinkedArtifactRefusesWithoutWritingItsTarget',
    'NoAllowedArtifactsRefuses',
    'LinkedReportDirectoryRefusesWithoutPublishingItsUnsafeTarget',
    'DirectoryAtAllowedReportNameRefusesItsUnsafeDescendants',
    'WorkflowPublishesOnlyAfterSuccessfulGuardEvenOnMutationFailure',
]
publication = run('publication-controls', ['python3', '-B', 'tests/eng/test_mutation_artifact_publication.py',
    *['PublicationTests.test' + name for name in methods]])
project = 'tests/Hexalith.FrontComposer.SourceTools.Tests/Hexalith.FrontComposer.SourceTools.Tests.csproj'
build = run('focused-debug-build', ['dotnet', 'build', project, '-c', 'Debug',
    '-p:UseMicrosoftTestingPlatformRunner=true', '-p:GeneratePackageOnBuild=false', '-m:1', '-nodeReuse:false'])
tests = None
if build == 0:
    classes = ['ErrorFollowthroughBehaviorTests', 'RazorRoleRecoveryContractTests',
        'TemplateContractBoundaryTests', 'AliasSignatureContractTests', 'CommandContractBoundaryTests']
    filters = [argument for name in classes for argument in
        ['--filter-class', 'Hexalith.FrontComposer.SourceTools.Tests.Mutation.' + name]]
    tests = run('five-regression-classes', ['dotnet', 'test', '--project', project, '--no-build',
        '--configuration', 'Debug', *filters, '--minimum-expected-tests', '53', '--fail-skips', 'on',
        '--results-directory', str(OUT / 'test-results'), '--report-xunit-trx',
        '--report-xunit-trx-filename', 'tests.trx', '-p:UseMicrosoftTestingPlatformRunner=true'])
record['finishedUtc'] = time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime())
record['allChecksPassed'] = publication == 0 and build == 0 and tests == 0
save()
raise SystemExit(not record['allChecksPassed'])
