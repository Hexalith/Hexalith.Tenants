from pathlib import Path
from collections import Counter
import argparse
import json
import re
import subprocess
import sys
import tempfile

ROOT = Path.cwd()
parser = argparse.ArgumentParser(description='Qualify DW-419 with isolated source variants.')
parser.add_argument('--output-directory', type=Path, required=True)
OUTPUT = parser.parse_args().output_directory.resolve()
OUTPUT.mkdir(parents=True, exist_ok=True)
(OUTPUT / 'qualification.json').unlink(missing_ok=True)
SCRIPT = Path('scripts/validate-story-gitlinks.py')
TESTS = Path('tests/scripts/test_validate_story_gitlinks.py')
BASE = '7f1e449dbe411853e065c938ca75d29e5688e15a'
METHODS = [
    'BaselineStoryInspectionTests',
    *['GuardCommandIntegrationTests.' + name for name in (
        'test_legacy_mid_story_baseline_rejects_hidden_earlier_pointer_movement',
        'test_legacy_mid_story_baseline_rejects_pointer_movement_in_the_baseline_commit',
        'test_correcting_a_legacy_mid_story_baseline_restores_the_complete_pointer_audit',
        'test_unrelated_baseline_artifacts_do_not_block_a_valid_legacy_audit',
        'test_recorded_provenance_keeps_its_mid_story_warning_and_owned_commit_checks',
        'test_baseline_inspection_error_produces_a_failed_command_verdict',
        'test_mid_story_recorded_provenance_retains_target_endpoint_worktree_and_chain_refusals',
    )],
]

def require(condition, message):
    if not condition:
        raise SystemExit(message)


def replace_once(source, old, new):
    require(source.count(old) == 1, 'Mutation anchor must match once')
    return source.replace(old, new, 1)


current = (ROOT / SCRIPT).read_text()
original = subprocess.check_output(['git', 'show', f'{BASE}:{SCRIPT}'], text=True)
test_bytes = (ROOT / TESTS).read_bytes()
refusal = '        if owned_commits is None:\n'
inspection = '    touched = run_git("show", "--pretty=format:", "--name-only", baseline)\n'
variants = [
    ('current', current, {}),
    ('entry', original, {
        'test_inspection_failure_cannot_establish_a_safe_baseline': 1,
        'test_baseline_inspection_error_produces_a_failed_command_verdict': 1,
        'test_legacy_mid_story_baseline_rejects_hidden_earlier_pointer_movement': 12,
        'test_legacy_mid_story_baseline_rejects_pointer_movement_in_the_baseline_commit': 6,
        'test_correcting_a_legacy_mid_story_baseline_restores_the_complete_pointer_audit': 6,
    }),
    ('all-modes-refusal', replace_once(current, refusal, '        if True:\n'), {
        'test_recorded_provenance_keeps_its_mid_story_warning_and_owned_commit_checks': 2,
        'test_mid_story_recorded_provenance_retains_target_endpoint_worktree_and_chain_refusals': 1,
    }),
    ('warning-only', replace_once(current, refusal, '        if False:\n'), {
        'test_legacy_mid_story_baseline_rejects_hidden_earlier_pointer_movement': 12,
        'test_legacy_mid_story_baseline_rejects_pointer_movement_in_the_baseline_commit': 6,
        'test_correcting_a_legacy_mid_story_baseline_restores_the_complete_pointer_audit': 6,
    }),
    ('swallowed-inspection-error', replace_once(current, inspection,
        '    try:\n    ' + inspection + '    except CheckError:\n        return False\n'), {
        'test_inspection_failure_cannot_establish_a_safe_baseline': 1,
        'test_baseline_inspection_error_produces_a_failed_command_verdict': 1,
    }),
]
results = []
for name, source, expected in variants:
    with tempfile.TemporaryDirectory(prefix='dw-419-') as directory:
        root = Path(directory)
        for path, data in ((SCRIPT, source.encode()), (TESTS, test_bytes)):
            (root / path.parent).mkdir(parents=True, exist_ok=True)
            (root / path).write_bytes(data)
        command = [sys.executable, str(root / TESTS), '-v', *METHODS]
        result = subprocess.run(command, capture_output=True, text=True, timeout=60)
        log = result.stdout + result.stderr
        (OUTPUT / f'{name}.log').write_text(log)
        actual = dict(Counter(re.findall(r'^FAIL: (\w+) ', log, re.M)))
        require(actual == expected, f'{name}: wrong failures: {actual}\n{log}')
        require(result.returncode == (1 if expected else 0), f'{name}: wrong exit\n{log}')
        require(re.search(r'Ran 8 tests in', log) and not re.search(r'^ERROR:|skipped=|errors=', log, re.M),
                f'{name}: unexpected count/errors/skips\n{log}')
        require(log.rstrip().endswith(f'FAILED (failures={sum(expected.values())})' if expected else 'OK'),
                f'{name}: wrong summary\n{log}')
        if name == 'entry':
            require(log.count('RESULT: PASS') == 25, 'Entry must reproduce 25 unsafe command PASS diagnostics')
        results.append({'variant': name, 'exit': result.returncode, 'methods': 8, 'failures': actual})
require((ROOT / SCRIPT).read_text() == current and (ROOT / TESTS).read_bytes() == test_bytes,
        'Workspace inputs changed')
(OUTPUT / 'qualification.json').write_text(json.dumps(results, indent=2) + '\n')
print(json.dumps(results, indent=2))
