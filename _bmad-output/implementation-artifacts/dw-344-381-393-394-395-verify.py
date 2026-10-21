from pathlib import Path
from collections import Counter
import json
import hashlib
import zipfile
import xml.etree.ElementTree as ET
import re
import subprocess
import sys

BASE = '2cd3cc270e88d3cdbb47a1a938fd6a0951b6f0d4'
SELECTED = {344, 381, 393, 394, 395}
LEDGER = '_bmad-output/implementation-artifacts/deferred-work.md'
INDEX = 'docs/maintenance-backlog.md'
PREFIX = 'pre-provenance-governance-'
EVIDENCE = 'dw-344-381-393-394-395-validation-evidence.md'
MUTABLE = {'status', 'disposition', 'verification-date', 'verification', 'verification-files', 'evidence-kind', 'next-action'}

def original(path):
    return subprocess.check_output(['git', 'show', BASE + ':' + path], text=True)

def records(text):
    blocks = re.split(r'(?=^### DW-)', text, flags=re.M)[1:]
    ids = [int(re.match(r'### DW-(\d+)', block)[1]) for block in blocks]
    assert len(ids) == len(set(ids)), 'Duplicate deferred IDs'
    return dict(zip(ids, [block.rstrip() for block in blocks], strict=True))

def ids_in_row(line):
    return [int(n) for n in re.findall(r'DW-(\d+)', line.split('|')[1])] if line.startswith('| DW-') else []

before, after = records(original(LEDGER)), records(Path(LEDGER).read_text())
assert len(before) == len(after) == 657 and before.keys() == after.keys()
assert {n for n in before if before[n] != after[n]} == SELECTED
for n in SELECTED:
    assert re.search(r'^status: done 2026-10-21$', after[n], re.M)
    for line in before[n].splitlines():
        expected = PREFIX + line if line.partition(':')[0] in MUTABLE else line
        assert expected in after[n].splitlines(), (n, expected)
open_ids = {n for n, block in after.items() if re.search(r'^status: open$', block, re.M)}
rows = [line for line in Path(INDEX).read_text().splitlines() if ids_in_row(line)]
indexed = sum([ids_in_row(line) for line in rows], [])
assert set(indexed) == open_ids and all(count == 1 for count in Counter(indexed).values())
old_rows = [line for line in original(INDEX).splitlines() if ids_in_row(line)]
assert rows == [line for line in old_rows if not SELECTED.intersection(ids_in_row(line))]
counts = Counter('done' if re.search(r'^status: done', block, re.M) else re.search(r'^disposition: ([^\n]+)$', block, re.M)[1] for block in after.values())
assert len(open_ids) == 207 and len(rows) == 189 and counts['done'] == 450
for name, count in counts.items():
    assert f'| {name} | {count} |' in Path(INDEX).read_text()
for path in ['_bmad-output/implementation-artifacts/deferred-work-archive.md', '_bmad-output/implementation-artifacts/sprint-status.yaml', '.gitmodules']:
    assert Path(path).read_text() == original(path), path
assert not subprocess.check_output(['git', 'diff', '--raw', BASE, '--', 'references/'])
result = {'baseline': BASE, 'selected': sorted(SELECTED), 'total_records': len(after), 'open_records': len(open_ids), 'done_records': counts['done'], 'index_rows': len(rows), 'dispositions': dict(counts), 'result': 'pass'}
result_path = Path('_bmad-output/implementation-artifacts/dw-344-381-393-394-395-ledger-validation.json')
if '--write-result' in sys.argv:
    result_path.write_text(json.dumps(result, indent=2) + '\n')
else:
    assert json.loads(result_path.read_text()) == result, 'Saved evidence differs from current audit'

artifact_root = Path('_bmad-output/implementation-artifacts')
manifest = json.loads((artifact_root / 'dw-344-381-393-394-395-run-manifest.json').read_text())
archive = artifact_root / manifest['archive']
assert hashlib.sha256(archive.read_bytes()).hexdigest() == manifest['archive_sha256']
accepted = {
    'tenants-provenance-review-contracts-full.log': 0,
    'tenants-provenance-review-apphost-package-full.log': 0,
    'tenants-provenance-review-apphost-package-proof.log': 0,
    'tenants-provenance-review-source-provenance.log': 0,
    'tenants-provenance-review-apphost-source-rejected.log': 2,
}
entries = {entry['file']: entry for entry in manifest['entries']}
assert len(entries) == len(manifest['entries'])
assert accepted.keys() <= entries.keys()
with zipfile.ZipFile(archive) as saved:
    for entry in manifest['entries']:
        data = saved.read(entry['file'])
        assert len(data) == entry['bytes']
        assert hashlib.sha256(data).hexdigest() == entry['sha256']
        if 'counters' in entry:
            assert ET.fromstring(data).find('.//{*}Counters').attrib == entry['counters']
            assert entry['produced_by'] in entries
        else:
            assert entry['command'] and entry['cwd'] and isinstance(entry['exit_code'], int)
    checked_sources = set()
    for filename, expected_exit in accepted.items():
        entry = entries[filename]
        assert entry['exit_code'] == expected_exit
        assert entry['command'][:2] == ['dotnet', 'test']
        assert entry['assembly_sha256_before'] == entry['assembly_sha256_after']
        assert entry['assembly_sha256_before']
        for filename, hashes in entry['tested_sources'].items():
            normalized = Path(filename).read_bytes().replace(b'\r\n', b'\n')
            assert hashlib.sha256(normalized).hexdigest() == hashes['normalized_lf_sha256'], filename
            checked_sources.add(filename)
    assert {
        '.github/workflows/source-reference.yml',
        'src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj',
        'tests/Hexalith.Tenants.Contracts.Tests/PackageGovernanceTests.cs',
        'tests/Hexalith.Tenants.Contracts.Tests/SourceReferenceWorkflowGovernanceTests.cs',
        'tests/Hexalith.Tenants.AppHost.Tests/MemoriesSecretStoreResourceGraphTests.cs',
    } <= checked_sources
    contracts = ET.fromstring(saved.read('contracts-review.trx'))
    assert contracts.find('.//{*}Counters').get('passed') == '654'
    tests = contracts.findall('.//{*}UnitTestResult')
    assert sum(test.get('testName', '').startswith('Hexalith.Tenants.Contracts.Tests.SourceReferenceWorkflowGovernanceTests.') for test in tests) == 68
    assert sum(test.get('testName', '').startswith('Hexalith.Tenants.Contracts.Tests.PackageGovernanceTests.') for test in tests) == 242
    apphost = ET.fromstring(saved.read('apphost-package-review.trx'))
    assert apphost.find('.//{*}Counters').get('passed') == '9'
    proof = [test for test in apphost.findall('.//{*}UnitTestResult') if test.get('testName', '').endswith('MemoriesSecretStoreResourceGraphTests.PackageModeAppHostUsesTheConsumerOwnedMemoriesSecretStore')]
    assert len(proof) == 1 and proof[0].get('outcome') == 'Passed'
controller = Path('tests/Hexalith.Tenants.IntegrationTests/TenantsApiGeneratedControllerTests.cs')
assert hashlib.sha256(controller.read_bytes()).hexdigest() == manifest['restored_controller_sha256']
print(json.dumps(result, indent=2))
print('Archive, invocation attribution, current tested-source hashes and acceptance results: PASS')
