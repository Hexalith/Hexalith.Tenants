"""Reproduce the five-record historical documentation audit from repository root."""
from collections import Counter
import importlib.util
import json
from pathlib import Path
import re
import subprocess
import sys
import tempfile

BASE = 'ffe9522b10f37f56cd9339c3387cbb4b7d16d897'
ROOT = Path('_bmad-output/implementation-artifacts')
PREFIX = ROOT / 'dw-198-246-343-360-384'
SELECTED = {198, 246, 343, 360, 384}
MUTABLE = {'status', 'disposition', 'verification-date', 'verification', 'verification-files', 'evidence-kind', 'next-action'}


def git(*args):
    return subprocess.check_output(['git', *args], text=True).strip()


def original(path):
    return subprocess.check_output(['git', 'show', f'{BASE}:{path}'], text=True)


def load(name, path):
    spec = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    ids = [int(re.match(r'### DW-(\d+)', block)[1]) for block in blocks]
    assert len(ids) == len(set(ids)), 'Duplicate numeric deferred ID'
    return dict(zip(ids, [block.rstrip() for block in blocks], strict=True))


def row_ids(row):
    return [int(n) for n in re.findall(r'DW-(\d+)', row.split('|')[1])]


def changes(before, after):
    result = []
    for line in git('diff', '--raw', '--no-abbrev', '--no-renames', before, after, '--', 'references/').splitlines():
        meta, path = line.split('\t')
        old_mode, new_mode, old, new, _ = meta.split()
        if '160000' in (old_mode.lstrip(':'), new_mode):
            result.append({'path': path, 'before': old, 'after': new})
    return result


def pointers(commit):
    return {line.split('\t')[1]: line.split()[2] for line in git('ls-tree', commit, 'references/').splitlines() if line.startswith('160000 ')}


guard = load('historical_guard', 'scripts/validate-story-gitlinks.py')
runner = load('historical_runner', 'scripts/validate-changed-story-gitlinks.py')
manifest = json.loads(PREFIX.with_name(PREFIX.name + '-history.json').read_text())
assert manifest['entry_commit'] == BASE
artifacts = manifest['artifacts']
assert {a['path'] for a in artifacts} == {str(ROOT / name) for name in (
    'spec-2-4-remove-tenant-member-with-complete-preview-and-proof.md',
    'spec-2-4b-wp-2a-removal-proof-and-audit-available.md',
    'spec-4-2-grant-global-administrator-with-projection-confirmation.md',
    'spec-refresh-dependencies-2.md',
)} and len(artifacts) == 4
controls = Counter()
history_counts = []
for artifact in artifacts:
    path = Path(artifact['path'])
    text = path.read_text()
    baseline, endpoint = artifact['baseline'], artifact['endpoint']
    for commit in (baseline, endpoint):
        assert git('rev-parse', '--verify', f'{commit}^{{commit}}') == commit
    assert guard.read_frontmatter(text)['baseline_commit'] == baseline
    assert guard.read_frontmatter(text)['gitlink_audit_ref'] == endpoint
    assert pointers(baseline) == artifact['start_pointers']
    assert pointers(endpoint) == artifact['end_pointers']
    assert changes(baseline, endpoint) == artifact['changes']
    history_start = artifact.get('implementation_start', baseline)
    history = [commit for commit in git('rev-list', '--first-parent', '--reverse', f'{history_start}..{endpoint}').splitlines() if changes(commit + '^', commit)]
    assert history == [entry['commit'] for entry in artifact['commits']]
    assert artifact['pointer_commit_count'] == len(history)
    chain = pointers(history_start)
    count = 0
    for entry in artifact['commits']:
        commit = entry['commit']
        assert git('rev-parse', commit + '^') == entry['parent']
        if 'parents' in entry:
            parents = git('show', '-s', '--format=%P', commit).split()
            assert parents == entry['parents']
            merged = git('rev-list', '--reverse', parents[0] + '..' + parents[1]).splitlines() if len(parents) == 2 else []
            assert merged == entry['merged_commits']
        assert git('show', '-s', '--format=%s', commit) == entry['subject']
        assert git('diff-tree', '--no-commit-id', '--name-only', '-r', entry['parent'], commit).splitlines() == entry['files']
        assert changes(entry['parent'], commit) == entry['changes']
        attribution = entry['attribution']
        assert attribution['reason'] and attribution['category'] in manifest['classification_definitions']
        family = str(ROOT / 'spec-2-4') if 'spec-2-4' in str(path) else str(path)
        supported = [name for name in entry['files'] if name.startswith(family)]
        assert set(attribution['supporting_paths']) <= set(entry['files'])
        if attribution['category'] != 'independent-range-work':
            assert attribution['supporting_paths'], 'Story attribution needs supporting artifacts or specific implementation paths'
            assert supported or all(name.endswith('GlobalAdministratorGrantPreview.cs') for name in attribution['supporting_paths'])
        else:
            assert not supported, 'Independent classification contains a story artifact'
        if attribution['category'] == 'story-metadata-carried':
            assert not any(name.startswith(('src/', 'tests/')) for name in entry['files'])
        for move in entry['changes']:
            assert chain[move['path']] == move['before']
            chain[move['path']] = move['after']
            count += 1
    assert count == artifact['pointer_change_count']
    assert chain == pointers(endpoint)
    frozen = r'<frozen-after-approval\b.*?</frozen-after-approval>'
    old_text = original(path)
    assert re.findall(frozen, text, re.S) == re.findall(frozen, old_text, re.S)
    current_lines = iter(text.splitlines())
    assert all(any(current == old for current in current_lines) for old in old_text.splitlines()), 'Historical text removed or reordered'
    assert runner.audit_ref(text, {str(path)}, str(path), BASE, BASE, False) == endpoint
    earlier_text = re.sub(r"(?m)^gitlink_audit_ref:.*$", "gitlink_audit_ref: '" + baseline + "'", text)
    try:
        runner.audit_ref(earlier_text, {str(path)}, str(path), BASE, BASE, False)
    except runner.guard.CheckError:
        controls['premature_endpoint_rejected'] += 1
    else:
        raise AssertionError('Premature endpoint accepted: ' + str(path))
    superseded = ('fa5ca5596f4a627713dce9ea712a0de81cf670bf' if 'spec-2-4' in path.name else
                  'd0c534ffb7cfff34ae6bacf8499dc10bf1aceed8' if 'spec-4-2-' in path.name else None)
    if superseded:
        assert git('rev-parse', '--verify', superseded + '^{commit}') == superseded
        early_text = re.sub(r"(?m)^gitlink_audit_ref:.*$", "gitlink_audit_ref: '" + superseded + "'", text)
        try:
            runner.audit_ref(early_text, {str(path)}, str(path), BASE, BASE, False)
        except runner.guard.CheckError as error:
            assert 'historical endpoint omits later story-associated gitlink commit' in str(error)
            controls['superseded_endpoint_rejected'] += 1
        else:
            raise AssertionError('Superseded endpoint accepted: ' + str(path))
    stated = guard.stated_targets(text)
    declared = guard.declared_paths(text)
    assert stated == {move['path']: move['after'] for move in artifact['changes']}
    binding_table = guard.extract_section(text, 'File List')
    rows = re.findall(r'^\| `?(references/[^`| ]+)`? \| `?([0-9a-f]{40})`? \| `?([0-9a-f]{40})`? \|$', binding_table, re.M)
    assert {name: (old, new) for name, old, new in rows} == {move['path']: (move['before'], move['after']) for move in artifact['changes']}

    for name, target in stated.items():
        assert artifact['end_pointers'][name].startswith(target)
        assert name in declared
    with tempfile.TemporaryDirectory(prefix='dw-history-control-') as directory:
        probe = Path(directory) / path.name

        def run(content, ref, expected, diagnostic=None):
            probe.write_text(content)
            outcome = subprocess.run(['python3', 'scripts/validate-story-gitlinks.py', str(probe), '--ref', ref], text=True, capture_output=True)
            assert outcome.returncode == expected, (path, ref, outcome.stdout, outcome.stderr)
            if diagnostic:
                assert diagnostic in outcome.stdout + outcome.stderr

        run(text, endpoint, 0, 'RESULT: PASS')
        controls['historical_pass'] += 1
        run(text, BASE, 1, 'RESULT: FAIL')
        controls['moving_head_rejected'] += 1
        for name, target in stated.items():
            run(text.replace(target, '0' * len(target)), endpoint, 1, 'MISSTATED')
            controls['wrong_target_rejected'] += 1
            stripped = '\n'.join(line for line in text.splitlines() if not (re.match(r'^\s*[-*+]\s', line) and name in line)) + '\n'
            run(stripped, endpoint, 1, 'UNDECLARED')
            controls['missing_declaration_rejected'] += 1
        original_text = original(path)
        probe.write_text(original_text)
        original_result = subprocess.run(['python3', 'scripts/validate-story-gitlinks.py', str(probe), '--ref', endpoint], text=True, capture_output=True)
        expected = 0 if 'spec-4-2-' in path.name else 1
        assert original_result.returncode == expected, (path, original_result.stdout, original_result.stderr)
        if 'spec-2-4' in path.name:
            for name in artifact['start_pointers'].keys() - {'references/Hexalith.EventStore'}:
                assert '[UNDECLARED] ' + name in original_result.stdout
            assert original_result.stdout.count('[UNDECLARED]') == 6
        elif 'refresh-' in path.name:
            assert 'baseline_commit ' + baseline[:7] + ' is a mid-story baseline' in original_result.stdout
            assert 'Earlier pointer changes are outside the legacy baseline-to-tree audit.' in original_result.stdout
        else:
            assert 'RESULT: PASS' in original_result.stdout
        controls['entry_document_replays'] += 1
    if artifact.get('implementation_start'):
        assert changes(history_start, endpoint) == artifact['implementation_changes']
        additions = git('log', '--diff-filter=A', '--format=%H', '--', str(path)).splitlines()
        assert len(additions) == 1 and git('rev-parse', additions[0] + '^') == history_start
        complete_table = guard.extract_section(text, 'Historical range qualification (2026-10-21)')
        rows = re.findall(r'^\| `?(references/[^`| ]+)`? \| `?([0-9a-f]{40})`? \| `?([0-9a-f]{40})`? \|$', complete_table, re.M)
        assert {name: (old, new) for name, old, new in rows} == {move['path']: (move['before'], move['after']) for move in artifact['implementation_changes']}
        alias = 'using CommandStatus = Hexalith.EventStore.Contracts.Commands.CommandStatus;'
        alias_commit = 'a3321266b61fc98f98f700901bd2cc5166eff4b0'
        alias_path = 'tests/Hexalith.Tenants.IntegrationTests/AspireTopologyTests.cs'
        assert alias not in git('show', alias_commit + '^:' + alias_path)
        assert alias in git('show', alias_commit + ':' + alias_path)
        assert alias_path in guard.extract_section(text, 'File List')
        warning = subprocess.check_output(['python3', 'scripts/validate-story-gitlinks.py', str(path), '--ref', endpoint], text=True)
        assert 'WARNING: baseline_commit ' + baseline[:7] + " already touches this story's own files" in warning
        assert 'mid-story baseline. Pointer changes made earlier in the story are outside this diff.' in warning
    history_counts.append({'path': str(path), 'pointer_commits': len(history), 'pointer_changes': count})

ledger = ROOT / 'deferred-work.md'
before, after = records(original(ledger)), records(ledger.read_text())
assert len(before) == len(after) == 657 and before.keys() == after.keys()
assert {n for n in before if before[n] != after[n]} == SELECTED
for n in SELECTED:
    for line in before[n].splitlines():
        expected = 'pre-historical-provenance-' + line if line.partition(':')[0] in MUTABLE else line
        assert expected in after[n].splitlines(), (n, expected)
    assert re.search(r'^status: done 2026-10-21$', after[n], re.M)
open_ids = {n for n, block in after.items() if re.search(r'^status: open$', block, re.M)}
index = Path('docs/maintenance-backlog.md')
rows = [line for line in index.read_text().splitlines() if line.startswith('| DW-')]
expected_rows = []
for line in original(index).splitlines():
    if not line.startswith('| DW-'):
        continue
    ids = row_ids(line)
    kept = [n for n in ids if n not in SELECTED]
    if not kept:
        continue
    if kept != ids:
        cells = line.split('|')
        cells[1] = ' ' + ', '.join('DW-' + str(n) for n in kept) + ' '
        line = '|'.join(cells)
        if kept == [549]:
            line = '| DW-549 | P2 | Gap / awaiting-decision | unassigned | Historical Story 5.6 endpoint/provenance decision | Set a durable story-end/provenance convention for the historical Story 5.6 artifact without rewriting its original findings. |'
    expected_rows.append(line)
assert rows == expected_rows
indexed = [n for line in rows for n in row_ids(line)]
assert set(indexed) == open_ids and all(n == 1 for n in Counter(indexed).values())
counts = Counter('done' if re.search(r'^status: done', block, re.M) else re.search(r'^disposition: (.*)$', block, re.M)[1] for block in after.values())
assert len(open_ids) == 202 and len(rows) == 186 and counts['done'] == 455
for name, count in counts.items():
    assert f'| {name} | {count} |' in index.read_text()
for path in [ROOT / 'deferred-work-archive.md', ROOT / 'sprint-status.yaml', Path('.gitmodules')]:
    assert path.read_text() == original(path)
assert not git('diff', '--raw', BASE, '--', 'references/')
result = {'baseline': BASE, 'selected': sorted(SELECTED), 'ledger_records': len(after), 'done': counts['done'], 'open': len(open_ids), 'maintenance_rows': len(rows), 'controls': dict(controls), 'history': history_counts, 'result': 'pass'}
result_path = PREFIX.with_name(PREFIX.name + '-validation.json')
if '--write-result' in sys.argv:
    result_path.write_text(json.dumps(result, indent=2) + '\n')
else:
    assert json.loads(result_path.read_text()) == result, 'Saved result differs from reproduced evidence'
print(json.dumps(result, indent=2))
