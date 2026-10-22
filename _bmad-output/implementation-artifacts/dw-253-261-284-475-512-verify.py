"""Check scoped deferred-record closure and exact maintenance coverage."""
from collections import Counter
import argparse
from pathlib import Path
import hashlib
import json
import re
import subprocess

BASELINE = '772fd4d8fb995c95efea5288edcc9d06b6cfe5f0'
SELECTED = {253, 261, 284, 475, 512}
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
PREFIX = 'pre-command-recovery-verification-'
MUTABLE = {'status', 'disposition', 'verification-date', 'verification',
           'verification-files', 'evidence-kind', 'dependencies', 'next-action'}


def require(condition, message):
    if not condition:
        raise ValueError(message)


def original(path):
    return subprocess.check_output(['git', 'show', f'{BASELINE}:{path}'], text=True)


def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    result = {}
    for block in blocks:
        number = int(re.match(r'### DW-(\d+)', block)[1])
        require(number not in result, f'Duplicate record {number}')
        result[number] = block.rstrip()
    return result


def field(block, key):
    values = re.findall(r'^' + re.escape(key) + r': (.*)$', block, re.M)
    require(len(values) == 1, f'Expected exactly one current {key} field')
    return values[0]


def row_ids(row):
    return {int(n) for n in re.findall(r'DW-(\d+)', row.split('|')[1])}


before = records(original(LEDGER))
after = records(Path(LEDGER).read_text())
require(before.keys() == after.keys() and len(after) == 657, 'Record inventory changed')
require({n for n in before if before[n] != after[n]} == SELECTED,
        'Changed records differ from requested five')
for n in SELECTED:
    require(field(before[n], 'status') == 'open', f'{n} was not open')
    require(field(after[n], 'status') == 'done 2026-10-22', f'{n} not closed')
    require(field(after[n], 'disposition') == 'done', f'{n} not resolved')
    require(field(after[n], 'next-action').startswith('none;'), f'{n} retains work')
    for line in before[n].splitlines():
        expected = PREFIX + line if line.partition(':')[0] in MUTABLE else line
        require(expected in after[n].splitlines(), f'{n} lost original field: {line}')

old_rows = [r for r in original(INDEX).splitlines() if r.startswith('| DW-')]
index = Path(INDEX).read_text()
rows = [r for r in index.splitlines() if r.startswith('| DW-')]
removed = [r for r in old_rows if row_ids(r) & SELECTED]
require(len(removed) == 5 and all(row_ids(r) <= SELECTED for r in removed),
        'Selected records share an unrelated maintenance row')
require(rows == [r for r in old_rows if r not in removed], 'Unrelated maintenance rows changed')
open_ids = {n for n, b in after.items() if field(b, 'status') == 'open'}
coverage = Counter(n for r in rows for n in row_ids(r))
require(coverage == Counter({n: 1 for n in open_ids}), 'Open records not covered exactly once')
counts = Counter('done' if n not in open_ids else field(b, 'disposition') for n, b in after.items())
require(counts == {'done': 480, 'actionable': 103, 'blocked': 15,
                   'awaiting-decision': 23, 'accepted-limitation': 9, 'uncertain': 27},
        'Disposition totals changed unexpectedly')
for disposition, count in counts.items():
    require(f'| {disposition} | {count} |' in index, f'Missing {disposition} count')
require('**Remaining: 177 open records across 167 index rows.**' in index, 'Wrong remaining count')
require(not subprocess.check_output(['git', 'diff', BASELINE, '--raw', '--', 'references/'], text=True),
        'Dependency pointers changed')
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--verify-local-edit', action='store_true',
                    help='Also verify the uncommitted MCP edit retained in the original workspace.')
args = parser.parse_args()
if args.verify_local_edit:
    require(hashlib.sha256(Path('.mcp.json').read_bytes()).hexdigest()
            == '86a1149a70d7f29f97b596bd4b3ed2d76f844abe14eb2b0dd779fc57e5cf13c1',
            'Pre-existing local MCP configuration changed')

def without_current_summary(text):
    lines = text.splitlines(keepends=True)
    positions = [i for i, line in enumerate(lines)
                 if line.startswith('**DW-253/DW-261/DW-284/DW-475/DW-512 closure (2026-10-22):**')]
    require(len(positions) == 1, 'Missing or duplicated current closure summary')
    position = positions[0]
    require(position + 1 < len(lines) and not lines[position + 1].strip(),
            'Current closure summary lacks its paragraph boundary')
    return ''.join(lines[:position] + lines[position + 2:])


def verify_narrative(ledger_text, index_text):
    require(without_current_summary(ledger_text).partition('### DW-')[0]
            == original(LEDGER).partition('### DW-')[0], 'Ledger preamble changed')
    expected_index = ''.join(line for line in original(INDEX).splitlines(keepends=True)
                             if line.rstrip('\n') not in removed)
    expected_index = expected_index.replace('| done | 475 |', '| done | 480 |')
    expected_index = expected_index.replace('| actionable | 108 |', '| actionable | 103 |')
    expected_index = expected_index.replace('**Remaining: 182 open records across 172 index rows.**',
                                            '**Remaining: 177 open records across 167 index rows.**')
    require(without_current_summary(index_text) == expected_index, 'Maintenance narrative changed')


current_ledger = Path(LEDGER).read_text()
verify_narrative(current_ledger, index)
for path, text in [(LEDGER, current_ledger), (INDEX, index)]:
    historical_summary = next(line for line in original(path).splitlines(keepends=True)
                              if line.startswith('**DW-345/DW-362/DW-378/DW-383/DW-412 closure'))
    changed = text.replace(historical_summary, '', 1)
    try:
        verify_narrative(changed if path == LEDGER else current_ledger,
                         changed if path == INDEX else index)
    except ValueError:
        pass
    else:
        raise ValueError('Historical-narrative deletion control was accepted')


for duplicate in ['### DW-1: Duplicate', '### DW-01: Numeric alias', '### DW-1 Duplicate']:
    try:
        records(duplicate + '\nstatus: open\n### DW-1: Original\nstatus: open\n')
    except ValueError:
        pass
    else:
        raise ValueError('Duplicate-heading control was accepted')
for malformed in ['status: open\nstatus: done', 'pre-status: done']:
    try:
        field(malformed, 'status')
    except ValueError:
        pass
    else:
        raise ValueError('Current-field control was accepted')

print(json.dumps(dict(baseline=BASELINE, selected=sorted(SELECTED), records=len(after),
                      done=counts['done'], open=len(open_ids), index_rows=len(rows),
                      dispositions=dict(counts), duplicate_controls_rejected=3,
                      current_field_controls_rejected=2, narrative_controls_rejected=2, result='pass'), indent=2))
