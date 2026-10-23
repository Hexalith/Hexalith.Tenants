#!/usr/bin/env python3
"""Verify this batch's historical exact-five ledger preservation separately from maintained shell tests."""

import os
from pathlib import Path
import re
import subprocess

os.chdir(Path(__file__).resolve().parents[2])

# Verify the exact-five ledger closure independently against the approved entry.
ledger = Path('_bmad-output/implementation-artifacts/deferred-work.md')
index = Path('docs/maintenance-backlog.md')
baseline = '870d677cbac5bb54d8a542a289421da8a9243b53'
selected = {551, 556, 575, 576, 597}

def baseline_text(path):
    return subprocess.check_output(['git', 'show', f'{baseline}:{path}'], text=True)

def ledger_records(text):
    # An unnumbered follow-up section after the last DW entry is not part of it.
    headings = list(re.finditer(r"^#{1,6} [^\n]+(?:\n|$)", text, re.M))
    records = {}
    for i, heading in enumerate(headings):
        match = re.match(r"### DW-(\d+):", heading[0])
        if match:
            end = headings[i + 1].start() if i + 1 < len(headings) else len(text)
            records[int(match[1])] = text[heading.start():end].rstrip("\n")
    return records

before = ledger_records(baseline_text(ledger))
after = ledger_records(ledger.read_text())
assert before.keys() == after.keys()
assert ledger_records(ledger.read_text() + "\n## Unnumbered review follow-up\nRetained outside the numbered ledger.\n") == after
assert {number for number in before if before[number] != after[number]} == selected
for number in selected:
    old_fields = dict(re.findall(r'^([a-z][a-z0-9-]*): (.*)$', before[number], re.M))
    new_fields = dict(re.findall(r'^([a-z][a-z0-9-]*): (.*)$', after[number], re.M))
    assert old_fields['status'] == 'open'
    assert new_fields['status'] == 'done 2026-10-23'
    for key, value in old_fields.items():
        assert new_fields.get(key) == value or new_fields.get('pre-bootstrap-handoff-' + key) == value
old_rows = [line for line in baseline_text(index).splitlines() if line.startswith('| DW-')]
new_rows = [line for line in index.read_text().splitlines() if line.startswith('| DW-')]
removed = [line for line in old_rows if line not in new_rows]
assert len(removed) == 5
assert set(map(int, re.findall(r'DW-(\d+)', '\n'.join(line.split('|')[1] for line in removed)))) == selected
assert [line for line in old_rows if line not in removed] == new_rows
open_ids = {number for number, body in after.items() if re.search(r'^status: open$', body, re.M)}
indexed = list(map(int, re.findall(r'DW-(\d+)', '\n'.join(line.split('|')[1] for line in new_rows))))
assert set(indexed) == open_ids and len(indexed) == len(open_ids)
assert len(after) == 657 and len(open_ids) == 107 and len(new_rows) == 97
print('Ledger preservation passed: exact five closed, all 657 IDs retained, 550 done/107 open; 97 exact-covering index rows. Historical selected fields and all unrelated records/rows preserved.')
