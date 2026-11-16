"""Run focused fixture contracts from a clean, byte-bound copy of tracked Builds files."""

import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import uuid


EVIDENCE = Path(__file__).resolve().parent
OWNER = EVIDENCE.parents[3] / 'references/Hexalith.Builds'

MODULE = 'test/Hexalith.Builds.Module.Tests/Hexalith.Builds.Module.Tests.csproj'
EVIDENCE_PROJECT = 'test/Hexalith.Builds.Evidence.Tests/Hexalith.Builds.Evidence.Tests.csproj'
COMMANDS = {
    'module-build': ['dotnet', 'build', MODULE, '-c', 'Debug', '-m:1', '--nologo'],
    'manifest-contracts': ['dotnet', 'test/Hexalith.Builds.Module.Tests/bin/Debug/net11.0/Hexalith.Builds.Module.Tests.dll', '-class', '*ManifestValidationTests', '-class', '*SupportedPlatformPinsCatalogTests'],
    'evidence-build': ['dotnet', 'build', EVIDENCE_PROJECT, '-c', 'Debug', '-m:1', '--nologo'],
    'evidence-contracts': ['dotnet', 'test/Hexalith.Builds.Evidence.Tests/bin/Debug/net11.0/Hexalith.Builds.Evidence.Tests.dll', '-class', '*ReadinessEvidenceValidatorTests', '-class', '*G4P0AcceptanceValidatorTests', '-class', '*SelectedCleanupAcceptanceTests', '-class', '*EvidenceCommandApplicationTests'],
}

def recipe_identity():
    recipe = {'schema': 'builds-focused-verification-recipe.v1', 'configuration': 'Debug',
              'commands': COMMANDS, 'verifier_sha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest()}
    return hashlib.sha256(json.dumps(recipe, sort_keys=True).encode()).hexdigest()


def retained_log(evidence, check):
    relative = check.get('log')
    if not isinstance(relative, str) or not relative or Path(relative).is_absolute() or '..' in Path(relative).parts:
        raise ValueError('Reuse requires a contained real retained log file.')
    candidate = evidence / relative
    try:
        candidate.resolve(strict=True).relative_to(evidence.resolve(strict=True))
    except (OSError, ValueError):
        raise ValueError('Reuse requires a contained real retained log file.') from None
    contained_paths = [evidence.joinpath(*Path(relative).parts[:length]) for length in range(1, len(Path(relative).parts) + 1)]
    if not candidate.is_file() or any(path.is_symlink() for path in contained_paths):
        raise ValueError('Reuse requires a contained real retained log file without symlinks.')
    if hashlib.sha256(candidate.read_bytes()).hexdigest() != check.get('log_sha256'):
        raise ValueError('Reuse requires matching retained log hashes.')
    return candidate


def selection(owner):
    """Refuse dirty tracked inputs and bind every file that will be copied."""
    revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=owner, text=True).strip()
    if subprocess.check_output(['git', 'status', '--porcelain', '--untracked-files=no'], cwd=owner):
        raise ValueError('Builds tracked source must be clean before attributing verification to HEAD.')
    paths = subprocess.check_output(['git', 'ls-files', '-z'], cwd=owner).decode().split('\0')
    inputs = {}
    for relative in paths:
        if not relative or ('/' in relative and not relative.startswith(('src/', 'test/', 'Props/', 'Tools/', 'schemas/'))):
            continue
        source = owner / relative
        if source.is_file():
            inputs[relative] = hashlib.sha256(source.read_bytes()).hexdigest()
    return revision, inputs


def reuse(previous, revision, manifest_sha256, evidence=None):
    """Refuse incomplete/historical recipes and verify the original logs without changing them."""
    evidence = EVIDENCE if evidence is None else evidence
    if previous.get('revision') != revision:
        raise ValueError('Evidence-only reuse requires the exact previous Builds revision; prior evidence is unchanged.')
    if previous.get('source_manifest_sha256') != manifest_sha256:
        raise ValueError('Evidence-only reuse requires the same byte-bound source manifest; prior evidence is unchanged.')
    if previous.get('configuration') != 'Debug':
        raise ValueError('Evidence-only reuse requires the exact Debug configuration.')
    if previous.get('verification_recipe_sha256') != recipe_identity():
        raise ValueError('Evidence-only reuse requires the same verification recipe identity; old incomplete records are refused.')
    checks = [check for check in previous.get('checks', []) if check.get('name') in ('module-build', 'manifest-contracts')]
    if len(checks) != 2 or {check['name'] for check in checks} != {'module-build', 'manifest-contracts'} or any(check.get('exit_code') != 0 for check in checks):
        raise ValueError('Evidence-only reuse requires both successful module checks.')
    paths = set()
    for check in checks:
        if check.get('command') != COMMANDS[check['name']]:
            raise ValueError('Evidence-only reuse requires the exact expected module commands.')
        path = retained_log(evidence, check)
        if path in paths:
            raise ValueError('Evidence-only reuse requires distinct retained logs for each check.')
        paths.add(path)
    return checks


def main():
    revision, inputs = selection(OWNER)
    evidence_only = '--evidence-only' in sys.argv
    results = {'revision': revision, 'configuration': 'Debug', 'verification_recipe_sha256': recipe_identity(), 'checks': [], 'source_files_sha256': inputs,
               'source_manifest_sha256': hashlib.sha256(json.dumps(inputs, sort_keys=True).encode()).hexdigest()}
    if evidence_only:
        prior = (EVIDENCE / 'current-verification.json').read_bytes()
        results['checks'] = reuse(json.loads(prior), revision, results['source_manifest_sha256'], evidence=EVIDENCE)
        results['reused_from'] = {'revision': revision, 'receipt_sha256': hashlib.sha256(prior).hexdigest()}
    snapshot = Path(os.environ['TMPDIR']) / 'builds'
    snapshot.mkdir()
    # Readiness artifacts resolve repository-relative paths from this marker.
    (snapshot / '.git').mkdir()
    for relative, digest in inputs.items():
        source = OWNER / relative
        target = snapshot / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        if hashlib.sha256(target.read_bytes()).hexdigest() != digest:
            raise ValueError('Builds input changed while copying: ' + relative)

    attempt = EVIDENCE / 'attempts' / uuid.uuid4().hex
    attempt.mkdir(parents=True)
    prior_path = EVIDENCE / 'current-verification.json'
    original_receipt = prior_path.read_bytes() if prior_path.is_file() else None
    archived = False

    def run(label, arguments):
        nonlocal archived
        if arguments != COMMANDS[label]:
            raise ValueError('Verification command differs from the exact recipe.')
        print(f'{label}: {" ".join(arguments)}', flush=True)
        log_path = attempt / f'{label}.log'
        with log_path.open('x') as log:
            process = subprocess.run(arguments, cwd=snapshot, stdout=log, stderr=subprocess.STDOUT)
        # Each attempt retains its own output and check record, including source-drift refusals.
        check = {'name': label, 'command': arguments, 'exit_code': process.returncode,
                 'log': str(log_path.relative_to(EVIDENCE)), 'log_sha256': hashlib.sha256(log_path.read_bytes()).hexdigest()}
        try:
            stable = selection(OWNER) == (revision, inputs)
        except ValueError:
            stable = False
        (attempt / f'{label}.json').write_text(json.dumps({**check, 'revision': revision,
            'source_manifest_sha256': results['source_manifest_sha256'], 'source_stable': stable}, indent=2) + '\n')
        # Never publish successful checks under a different revision or changed source bytes.
        if not stable:
            raise ValueError('Builds source changed during verification; prior receipt and logs are unchanged.')
        if original_receipt is not None and not archived:
            with (attempt / 'prior-verification.json').open('xb') as retained:
                retained.write(original_receipt)
            archived = True
        results['checks'].append(check)
        (EVIDENCE / 'current-verification.json').write_text(json.dumps(results, indent=2) + '\n')
        print(f'{label}: exit {process.returncode}', flush=True)
        print('\n'.join(log_path.read_text().splitlines()[-18:]), flush=True)
        return process.returncode

    if not evidence_only:
        if run('module-build', COMMANDS['module-build']):
            return 1
        if run('manifest-contracts', COMMANDS['manifest-contracts']):
            return 1
    if run('evidence-build', COMMANDS['evidence-build']):
        return 1
    return run('evidence-contracts', COMMANDS['evidence-contracts'])


if __name__ == '__main__':
    sys.exit(main())
