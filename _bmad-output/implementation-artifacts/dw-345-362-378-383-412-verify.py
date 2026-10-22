"""Verify exact conservation for the five rendered Cancel focus risk records."""
from collections import Counter
import json
import hashlib
from pathlib import Path
import re
import subprocess

BASELINE = 'ab6e99f656eaae14a01c021cf0c6eadf9f38cefe'
SELECTED = {345, 362, 378, 383, 412}
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'


def original(path):
    return subprocess.check_output(['git', 'show', BASELINE + ':' + path], text=True)


def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    numbers = [int(re.match(r'### DW-(\d+)', block)[1]) for block in blocks]
    assert len(numbers) == len(set(numbers)), 'Duplicate deferred IDs'
    return {number: block.rstrip() for number, block in zip(numbers, blocks, strict=True)}


for duplicate in ['### DW-1: Duplicate', '### DW-1 Duplicate', '### DW-01: Alias']:
    try:
        records(duplicate + '\nstatus: open\n### DW-1: Original\nstatus: open\n')
    except AssertionError:
        pass
    else:
        raise AssertionError('Duplicate control unexpectedly passed')
before, after = records(original(LEDGER)), records(Path(LEDGER).read_text())
assert len(before) == len(after) == 657 and before.keys() == after.keys()
assert {n for n in before if before[n] != after[n]} == SELECTED
mutable = {'status', 'disposition', 'verification-date', 'verification', 'verification-files', 'evidence-kind', 'next-action'}
def validate_current_fields(data):
    for number in SELECTED:
        current = Counter(line.partition(':')[0] for line in data[number].splitlines()
                          if line.partition(':')[0] in mutable)
        assert all(current[key] == 1 for key in mutable), (number, current)
        assert '\nstatus: done 2026-10-22\n' in data[number] + '\n'
        assert '\ndisposition: done\n' in data[number] + '\n'


validate_current_fields(after)
for bad_value in ['disposition: uncertain', 'disposition: done\ndisposition: done']:
    control = dict(after)
    control[345] = control[345].replace('\ndisposition: done\n', '\n' + bad_value + '\n')
    try:
        validate_current_fields(control)
    except AssertionError:
        pass
    else:
        raise AssertionError('Wrong or duplicate current disposition control unexpectedly passed')
for number in SELECTED:
    assert '\nstatus: open\n' in before[number] + '\n'
    assert '\nstatus: done 2026-10-22\n' in after[number] + '\n'
    for line in before[number].splitlines():
        expected = 'pre-cancel-focus-' + line if line.partition(':')[0] in mutable else line
        assert expected in after[number].splitlines(), (number, expected)


def row_ids(row):
    return {int(n) for n in re.findall(r'DW-(\d+)', row.split('|')[1])}


old_rows = [line for line in original(INDEX).splitlines() if line.startswith('| DW-')]
index = Path(INDEX).read_text()
rows = [line for line in index.splitlines() if line.startswith('| DW-')]
removed = [row for row in old_rows if row_ids(row) & SELECTED]
assert len(removed) == 1 and row_ids(removed[0]) == SELECTED
assert rows == [row for row in old_rows if row not in removed]
open_ids = {n for n, body in after.items() if re.search(r'^status: open$', body, re.M)}
coverage = [n for row in rows for n in row_ids(row)]
assert len(rows) == 172 and len(coverage) == len(open_ids) == 182
assert set(coverage) == open_ids
counts = Counter('done' if n not in open_ids else re.search(r'^disposition: (.+)$', body, re.M)[1]
                 for n, body in after.items())
assert counts == dict(done=475, actionable=108, blocked=15, **{'awaiting-decision': 23, 'accepted-limitation': 9, 'uncertain': 27})
for disposition, count in counts.items():
    assert f'| {disposition} | {count} |' in index
assert '**Remaining: 182 open records across 172 index rows.**' in index
# Exact allowed paths also conserve production code, archive, sprint and release files.
artifact = '_bmad-output/implementation-artifacts/'
prefix = artifact + 'dw-345-362-378-383-412-'
allowed = {
    LEDGER, INDEX,
    'tests/Hexalith.Tenants.UI.Tests/Components/GlobalAdministratorsPageTests.FocusBrowserFixtures.cs',
    'tests/Hexalith.Tenants.UI.Tests/Browser/global-administrator-remove-focus-browser-validation.html',
    'tests/Hexalith.Tenants.UI.Tests/Browser/validate-tenants-focus-browser.sh',
    'tests/Hexalith.Tenants.UI.Tests/Browser/dump-validation-dom.mjs',
    artifact + 'spec-dw-345-362-378-383-412-rendered-cancel-focus.md',
    *(prefix + suffix for suffix in ['verify.py', 'ledger-validation.json', 'run-manifest.json',
                                    'validation-evidence.md', 'validation-transcript.txt']),
}
tracked = subprocess.check_output(['git', 'diff', '--name-only', BASELINE], text=True).splitlines()
untracked = subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard'], text=True).splitlines()
# An unrelated concurrent configuration edit appeared after the clean entry snapshot.
# Pin its observed bytes and prohibit staging it; do not reset or include it.
external = {'.mcp.json': '86a1149a70d7f29f97b596bd4b3ed2d76f844abe14eb2b0dd779fc57e5cf13c1'}
unexpected = set(tracked + untracked) - allowed
assert unexpected <= external.keys(), sorted(unexpected - external.keys())
staged = subprocess.check_output(['git', 'diff', '--cached', '--name-only'], text=True).splitlines()
for path in unexpected:
    assert hashlib.sha256(Path(path).read_bytes()).hexdigest() == external[path], path
    assert path not in staged, 'External configuration must stay outside this commit'

# A refutation must not silently include a production helper or dependency change.
assert Path('src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js').read_text() == original('src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js')
changed = subprocess.check_output(['git', 'diff', BASELINE, '--raw', '--', 'references/'], text=True)
assert not changed, 'Unexpected dependency pointer/content diff'
result = dict(baseline=BASELINE, selected=sorted(SELECTED), records=657, done=475, open=182,
              index_rows=172, dispositions=dict(counts), duplicate_controls_rejected=3, current_field_controls_rejected=2, allowed_change_paths=sorted(allowed), preserved_external_changes={p: external[p] for p in sorted(unexpected)}, result='pass')
print(json.dumps(result, indent=2))
