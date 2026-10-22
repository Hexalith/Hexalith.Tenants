"""Verify the five grant closures without losing historical or unrelated records."""
from collections import Counter
from pathlib import Path
import json
import re
import subprocess

BASELINE = 'f0792c44f98b4dd9492020e1bb6b108e84dc12e7'
SELECTED = {346, 369, 374, 377, 402}
STALE_INDEX = {507, 509, 510, 513, 518}
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
PREFIX = 'pre-grant-surface-'
SUMMARY = '**DW-346/DW-369/DW-374/DW-377/DW-402 closure (2026-10-22):**'
MUTABLE = {'status', 'disposition', 'verification-date', 'verification',
           'verification-files', 'evidence-kind', 'dependencies', 'next-action'}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def original(path):
    return subprocess.check_output(['git', 'show', f'{BASELINE}:{path}'], text=True)


def records(text):
    result = {}
    for block in re.split(r'(?=^### DW-)', text, flags=re.M)[1:]:
        number = int(re.match(r'### DW-(\d+)', block)[1])
        require(number not in result, 'Duplicate record')
        result[number] = block.rstrip()
    return result


def field(block, key):
    values = re.findall(r'^' + re.escape(key) + r': (.*)$', block, re.M)
    require(len(values) == 1, f'Expected one {key}')
    return values[0]


def row_ids(row):
    return {int(n) for n in re.findall(r'DW-(\d+)', row.split('|')[1])}


def without_summary(text):
    lines = text.splitlines(keepends=True)
    positions = [i for i, line in enumerate(lines) if line.startswith(SUMMARY)]
    require(len(positions) == 1, 'Missing/duplicate current summary')
    i = positions[0]
    require(i + 1 < len(lines) and lines[i + 1] == '\n', 'Summary paragraph boundary')
    return ''.join(lines[:i] + lines[i + 2:])


def verify_selected(before_block, after_block):
    for key in MUTABLE:
        field(after_block, key)
    reconstructed = []
    for line in after_block.splitlines():
        key = line.partition(':')[0]
        if key in MUTABLE:
            continue
        if key.startswith(PREFIX) and key[len(PREFIX):] in MUTABLE:
            line = line[len(PREFIX):]
        reconstructed.append(line)
    require('\n'.join(reconstructed) == before_block, 'Selected record changed beyond allowed fields')


before = records(original(LEDGER))
ledger = Path(LEDGER).read_text()
after = records(ledger)
require(before.keys() == after.keys() and len(after) == 657, 'Record inventory changed')
require({n for n in before if before[n] != after[n]} == SELECTED, 'Unexpected changed records')
for n in SELECTED:
    require(field(before[n], 'status') == 'open', f'{n} was not open')
    require(field(after[n], 'status') == 'done 2026-10-22', f'{n} not closed')
    require(field(after[n], 'disposition') == 'done', f'{n} disposition')
    require(field(after[n], 'next-action').startswith('none;'), f'{n} retains work')
    verify_selected(before[n], after[n])
    for line in before[n].splitlines():
        expected = PREFIX + line if line.partition(':')[0] in MUTABLE else line
        require(expected in after[n].splitlines(), f'{n} lost historical content: {line}')
require(without_summary(ledger).partition('### DW-')[0] ==
        original(LEDGER).partition('### DW-')[0], 'Historical ledger preamble changed')
index = Path(INDEX).read_text()
old_rows = [r for r in original(INDEX).splitlines() if r.startswith('| DW-')]
removed = [r for r in old_rows if row_ids(r) & (SELECTED | STALE_INDEX)]
require(len(removed) == 10 and all(row_ids(r) <= (SELECTED | STALE_INDEX) for r in removed), 'Shared row removal')
require(all(field(before[n], 'status').startswith('done') for n in STALE_INDEX), 'Stale rows were not previously done')
rows = [r for r in index.splitlines() if r.startswith('| DW-')]
require(rows == [r for r in old_rows if r not in removed], 'Unrelated index rows changed')
open_ids = {n for n, b in after.items() if field(b, 'status') == 'open'}
require(Counter(n for r in rows for n in row_ids(r)) == Counter({n: 1 for n in open_ids}),
        'Index must cover every open record exactly once')
counts = Counter('done' if n not in open_ids else field(b, 'disposition') for n, b in after.items())
require(counts == dict(done=490, actionable=96, blocked=15,
                       **{'awaiting-decision': 23, 'accepted-limitation': 7, 'uncertain': 26}),
        'Disposition counts')
expected_index = ''.join(line for line in original(INDEX).splitlines(keepends=True)
                         if line.rstrip('\n') not in removed)
for a, b in [('done | 480', 'done | 490'), ('actionable | 103', 'actionable | 96'),
             ('accepted-limitation | 9', 'accepted-limitation | 7'), ('uncertain | 27', 'uncertain | 26'),
             ('**Remaining: 177 open records across 167 index rows.**',
              '**Remaining: 167 open records across 157 index rows.**')]:
    expected_index = expected_index.replace(a, b, 1)
require(without_summary(index) == expected_index, 'Historical index narrative changed')
require(not subprocess.check_output(['git', 'diff', BASELINE, '--raw', '--', 'references/'], text=True),
        'Dependency pointers changed')
for duplicate in ['### DW-1: Duplicate', '### DW-01: Numeric alias', '### DW-1 Duplicate']:
    try:
        records(duplicate + '\nstatus: open\n### DW-1: Original\nstatus: open\n')
    except ValueError:
        pass
    else:
        raise ValueError('Duplicate control accepted')
for mutation in [after[346] + '\nowner: duplicate',
                 after[346].replace('priority: P3', 'priority: P2'),
                 after[346].replace('owner: unassigned\n', ''),
                 after[346].replace('owner: unassigned', 'unexpected-field: value\nowner: unassigned')]:
    try:
        verify_selected(before[346], mutation)
    except ValueError:
        pass
    else:
        raise ValueError('Immutable-field control was accepted')
print(json.dumps(dict(baseline=BASELINE, selected=sorted(SELECTED), records=len(after),
                      done=counts['done'], open=len(open_ids), index_rows=len(rows), stale_index_reconciled=sorted(STALE_INDEX),
                      dispositions=dict(counts), immutable_field_controls_rejected=4, result='pass'), indent=2))
