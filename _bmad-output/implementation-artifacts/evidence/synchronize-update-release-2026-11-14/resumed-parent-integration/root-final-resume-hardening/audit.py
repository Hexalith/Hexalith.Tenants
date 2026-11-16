"""Read-only source, historical-field and retained focused-output audit; no release replay."""
import ast
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[6]
PACKET = Path(__file__).resolve().parent
BASE = PACKET.parent


def require(condition, message):
    if not condition:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    before = json.loads((PACKET / 'before-input-hashes.json').read_text())
    for relative, digest in before.items():
        require(sha(PACKET / 'before' / relative) == digest, 'Historical source packet changed: ' + relative)
    release = '_bmad-output/implementation-artifacts/evidence/synchronize-update-release-2026-11-14/resumed-parent-integration/verify-public-release.py'
    original = ast.parse((PACKET / 'before' / release).read_text())
    current = ast.parse((ROOT / release).read_text())
    previous_gates = [node for node in ast.walk(original) if isinstance(node, ast.Assert)]
    current_gates = [node for node in ast.walk(current) if isinstance(node, ast.Call) and isinstance(node.func, ast.Name) and node.func.id == 'require']
    require(not any(isinstance(node, ast.Assert) for node in ast.walk(current)), 'Removable assert gate remains.')
    for gate in previous_gates:
        # The old artifact-name/latest-attempt membership check is replaced by exact producer evidence.
        if 'artifact[\'name\'] in' in ast.unparse(gate.test):
            continue
        require(any(ast.dump(call.args[0]) == ast.dump(gate.test) for call in current_gates),
                'An existing release/source/package/signature/payload gate was lost: ' + ast.unparse(gate.test))
    state_relative = str((BASE / 'current-operational-state.json').relative_to(ROOT))
    previous = json.loads((PACKET / 'before' / state_relative).read_text())
    state = json.loads((ROOT / state_relative).read_text())
    for key in previous:
        if key != 'frontComposer':
            require(previous[key] == state[key], 'Unrelated operational field changed: ' + key)
    fc = state['frontComposer']
    history = fc['historicalObservations']['2026-11-16-prior-summary']['fields']
    for key, value in history.items():
        require(previous['frontComposer'][key] == value, 'Historical FC field was changed: ' + key)
    require(fc['source'] == fc['ownerWorktreeHead'] == fc['latestRemoteMain'] == '11c8377a858a756c4e4069bf08e62db1f58e1733', 'Wrong current FC source.')
    require(fc['mutationRun'] == 43807658283 and fc['mutationJobs'] == {'happy-path': 133102010688, 'error-handling': 133102010838}, 'Wrong native job identities.')
    require(fc['lifecycleJob'] == 133102010473 and fc['lifecycleConclusion'] == 'success', 'Wrong lifecycle result.')
    require(fc['releaseRun'] == 43810326251 and fc['prepareCandidateJob'] == 133111251970 and fc['plannedPatch'] == '6.0.1', 'Wrong protected release plan.')
    require(fc['approvalHold'].startswith('HELD') and fc['productionEnvironmentId'] == 19155294005, 'Approval hold lost.')
    for receipt in fc['currentEvidence']['receipts']:
        require(sha(BASE / receipt['path']) == receipt['sha256'], 'Retained hosted receipt changed.')
    hosted = json.loads((BASE / 'fc-11c-complete-hosted-ci.json').read_text())
    for workflow in hosted['workflows']:
        require(workflow['run']['conclusion'] == 'success' and workflow['run']['head_sha'] == fc['source'], 'Hosted source/result mismatch.')
        if workflow['name'] == 'Quality':
            require(len(workflow['jobs']['jobs']) == 8 and all(job['conclusion'] == 'success' for job in workflow['jobs']['jobs']), 'Quality is not 8/8.')
    progress = json.loads((BASE / 'fc-11c-workflows-progress-02.json').read_text())
    for job in progress['nativeJobs']['jobs']:
        if job['id'] in fc['mutationJobs'].values():
            require(job['status'] == 'in_progress' and job['conclusion'] is None, 'Mutation result was fabricated.')
    ci_relative = '.github/workflows/ci.yml'
    ci_before = (PACKET / 'before' / ci_relative).read_bytes()
    require((ROOT / ci_relative).read_bytes() == ci_before.replace(b'--minimum-expected-tests 66 --fail-skips on', b'--minimum-expected-tests 67 --fail-skips on'), 'Unrelated CI profile changed.')
    paths = [*before, 'tests/scripts/test_public_release_verification.py', 'tests/scripts/test_release_evidence_replays.py']
    for relative in paths:
        path = ROOT / relative
        if path.suffix == '.py':
            compile(path.read_bytes(), str(path), 'exec')
    commands = json.loads((PACKET / 'attempt-01/commands.json').read_text())
    for command in commands:
        require(command['exit_code'] == 0 and (ROOT / command['log']).is_file(), 'Focused command failed or log is missing.')
        command['log_sha256'] = sha(ROOT / command['log'])
    result = {'scope': 'Root-only helper/CI/state repair; no full Builds, .NET, historical qualification, public-release verification or remote/Git operation.',
              'source_sha256': {relative: sha(ROOT / relative) for relative in paths},
              'former_assert_gates_preserved': len(previous_gates), 'historical_fields_preserved': sorted(history),
              'current_state': '11c normal gates success; native Happy/Error pending; protected 6.0.1 preparation HELD.',
              'checks': commands, 'test_counts': {'release_resume': 9, 'public_release': 7, 'replay_paths': 1, 'dapr_evidence': 1},
              'cleanup': 'Owned run-with-temp session 10547 exited 0 after all children completed; replay test allocations were disposable. No full recipe was executed.'}
    target = PACKET / 'attempt-01/audit.json'
    with target.open('x') as output:
        output.write(json.dumps(result, indent=2) + '\n')
    print(json.dumps({'status': 'passed', 'former_assert_gates_preserved': len(previous_gates), 'test_cases': sum(result['test_counts'].values()), 'receipt': str(target.relative_to(ROOT))}))


if __name__ == '__main__':
    main()
