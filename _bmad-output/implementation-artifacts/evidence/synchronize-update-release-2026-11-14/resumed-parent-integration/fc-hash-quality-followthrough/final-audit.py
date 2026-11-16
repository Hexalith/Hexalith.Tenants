"""Audit bounded hash diagnostics and unchanged owner bytes; never score or accept a mutation gate."""
from pathlib import Path
from collections import Counter
import difflib, hashlib, json, subprocess, xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
OWNER = BASE.parents[5] / 'references/Hexalith.FrontComposer'
bindings = json.loads((BASE / 'source-bindings.json').read_text())
SOURCE = bindings['source_base_commit']

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def git(*args):
    return subprocess.check_output(['git', *args], cwd=OWNER).decode().strip()

assert git('rev-parse', 'HEAD') == SOURCE == 'b34be9245443d21b72a6076cf205bbbc8a8c521f'
assert subprocess.check_output(['git', 'status', '--porcelain=v1'], cwd=OWNER).decode() == bindings['owner_status_before']
assert not git('diff', '--name-only')
for path, row in bindings['read_inputs'].items():
    assert sha(OWNER / path) == row['sha256'] and git('rev-parse', 'HEAD:' + path) == row['git_blob']
for path, expected in bindings['working_input_overlay'].items():
    assert sha(OWNER / path) == expected
additional = {path: {'sha256': sha(OWNER / path), 'git_blob': git('rev-parse', 'HEAD:' + path)}
              for path in ('.github/workflows/quality.yml', '.github/workflows/ci.yml')}
(BASE / 'additional-contract-bindings.json').write_text(json.dumps(additional, indent=2) + '\n')

attempts = []
jobs = []
test_runs = []
for number in ('01', '02', '03', '04'):
    directory = BASE / ('attempt-' + number)
    record = json.loads((directory / 'verification.json').read_text())
    assert record['source_base_commit'] == SOURCE and record['builds_commit'] == '73e60af8a1fcdf3f06726d5d1b6e1199cce778ee'
    jobs.append(str(Path(record['workspace']).parent))
    private_inputs = directory / 'private-inputs' if number != '04' else BASE
    for name, expected in record['private_source_sha256'].items():
        assert sha(private_inputs / name) == expected
    for command in record['commands']:
        assert sha(directory / command['log']) == command['sha256']
    for trx in sorted(directory.rglob('tests.trx')):
        rows = [node.attrib for node in ET.parse(trx).getroot().iter() if node.tag.endswith('UnitTestResult')]
        phase = trx.parents[1].name
        expected_count = len(record['phases'][phase]['classes'])
        assert len(rows) == expected_count
        assert all(row['outcome'] == ('Failed' if phase == 'literal13419' else 'Passed') for row in rows)
        test_runs.append({'attempt': number, 'phase': phase, 'count': len(rows), 'outcomes': dict(Counter(row['outcome'] for row in rows)),
                          'trx': str(trx.relative_to(BASE)), 'sha256': sha(trx),
                          'failed_tests': [row['testName'] for row in rows if row['outcome'] == 'Failed']})
    for phase, inputs in record['phases'].items():
        captured = directory / phase / 'instrumented-source'
        if captured.exists():
            for path, expected in inputs['source_sha256'].items():
                assert sha(captured / path) == expected
            assert sum((captured / path).read_text().count('global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(')
                       for path in inputs['source_sha256']) == 20
    attempts.append({'attempt': number, 'receipt': str((directory / 'verification.json').relative_to(BASE)), 'sha256': sha(directory / 'verification.json'),
                     'command_count': len(record['commands']), 'nonzero_exits': [{'name': command['name'], 'exit_code': command['exit_code']}
                         for command in record['commands'] if command['exit_code']]})
assert sum(row['count'] for row in test_runs) == 18
assert sum(row['outcomes'].get('Failed', 0) for row in test_runs) == 1

arrays = {}
for phase in ('baseline', 'literal13541', 'literal13545', 'literal13546'):
    arrays[phase] = json.loads((BASE / 'attempt-02' / phase / 'array-collection.json').read_text())
    assert arrays[phase]['collectionCorrect'] and not arrays[phase]['collisionBudgetAsserted']
assert [arrays[phase]['distinctHashes'] for phase in arrays] == [128, 1, 128, 5]
assert [arrays[phase]['insertionEqualityProbes'] for phase in arrays] == [0, 8128, 0, 1866]

observations = {}
for phase in ('instrumented-baseline', 'instrumented13541'):
    row = json.loads((BASE / 'attempt-04' / phase / 'generator-cache.json').read_text())
    assert sum(row['positiveControl'][:19]) > 0 and row['positiveControl'][19] > 0
    assert sum(row['beforeEdit'][:19]) == sum(row['afterEdit'][:19]) == 0
    assert row['beforeEdit'][19] == row['afterEdit'][19] == 8
    observations[phase] = row
assert observations['instrumented-baseline']['initialOutputSha256'] == observations['instrumented13541']['initialOutputSha256']
assert observations['instrumented-baseline']['changedOutputSha256'] == observations['instrumented13541']['changedOutputSha256']

# Preserve the failed unqualified instrumentation source with an explicit reconstruction label.
failed = json.loads((BASE / 'attempt-02/verification.json').read_text())['phases']['instrumented-baseline']['source_sha256']
for path, expected in failed.items():
    source = (BASE / 'attempt-03/instrumented-baseline/instrumented-source' / path).read_bytes()
    reconstructed = source.replace(b'global::Hexalith.FrontComposer.SourceTools.Parsing.PrivateHashActivity.Record(', b'PrivateHashActivity.Record(')
    assert hashlib.sha256(reconstructed).hexdigest() == expected
    target = BASE / 'attempt-02/instrumented-baseline/reconstructed-source' / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(reconstructed)

candidate = BASE / 'RelativeTimeHashParticipationTests.cs'
baseline = json.loads((BASE / 'attempt-02/verification.json').read_text())
assert sha(candidate) == baseline['private_source_sha256'][candidate.name]
target = 'tests/Hexalith.FrontComposer.SourceTools.Tests/PrivateHashQuality/' + candidate.name
patch = 'diff --git a/' + target + ' b/' + target + '\nnew file mode 100644\n'
patch += ''.join(difflib.unified_diff([], candidate.read_text().splitlines(keepends=True), fromfile='/dev/null', tofile='b/' + target))
(BASE / 'candidate-relative-time-hash-participation.patch').write_text(patch)

assert all(path.startswith('/dev/shm/hexalith-job.') and not Path(path).exists() for path in jobs)
processes = subprocess.check_output(['ps', '-eo', 'pid,ppid,args']).decode().splitlines()
assert not [line for line in processes if any(path in line for path in jobs)]
summary = {'scope': __doc__, 'source_base_commit': SOURCE, 'working_input_overlay': bindings['working_input_overlay'],
           'owner_bytes_unchanged': True, 'original_error_inventory_source': 'e0eef14ddbbcd03823863a4f417a4b07f6f5bafc',
           'existing_hash_assertion_locations': 28, 'source_tools_assertion_locations': 25, 'unrelated_assertion_locations': 3,
           'assertion_location_kinds': {'selected-unequal-hash': 6, 'equal-value-hash-congruence': 22},
           'attempts': attempts, 'test_runs': test_runs, 'total_test_executions': 18, 'passed': 17, 'expected_literal_failures': 1,
           'array_collection_observations': arrays, 'actual_generator_observations': observations,
           'existing_uninstrumented_generator_budget_replays_passed': 4,
           'private_candidate': {'path': candidate.name, 'sha256': sha(candidate), 'patch_sha256': sha(BASE / 'candidate-relative-time-hash-participation.patch'),
                                 'original_passes': True, 'literal13419_fails': True, 'owner_acceptance': None,
                                 'bound': 'Finite documented window hash-participation expectation; not proof of a stale-output defect or a universal unequal-value hash rule.'},
           'reconstructed_failed_source': 'attempt-02/instrumented-baseline/reconstructed-source; exact recorded SHA verified, explicitly reconstructed from matching qualified control.',
           'remaining_unknowns': ['External clients and collision-performance budgets are not exhaustively known.',
                                  'This finite Roslyn5.12 path does not qualify every provider/configuration or the full generator workload.',
                                  'Most arithmetic variants preserve equal-value/hash congruence; no exact numeric values were pinned and no blanket equivalence is accepted.',
                                  'Complete original Happy reports and parent/owner disposition remain pending outside this task.'],
           'cleanup': {'owned_job_paths_removed': jobs, 'owned_processes_remaining': []},
           'full_mutation_run': False, 'gate_or_threshold_changes': False, 'accepted_triage_authored': False, 'qualification_claim': False}
(BASE / 'summary.json').write_text(json.dumps(summary, indent=2) + '\n')
manifest = [{'path': str(path.relative_to(BASE)), 'bytes': path.stat().st_size, 'sha256': sha(path)}
            for path in sorted(BASE.rglob('*')) if path.is_file() and path != BASE / 'evidence-manifest.json']
(BASE / 'evidence-manifest.json').write_text(json.dumps({'source_base_commit': SOURCE, 'files': manifest}, indent=2) + '\n')
print(json.dumps({'test_executions': 18, 'passed': 17, 'expected_literal_failure': 1, 'jobs_removed': len(jobs), 'owner_unchanged': True, 'manifest_files': len(manifest)}))
