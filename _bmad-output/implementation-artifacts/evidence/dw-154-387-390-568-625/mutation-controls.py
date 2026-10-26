"""Run the two DW-154 mutants, validate their exact failures, and rebuild restored output."""
from pathlib import Path
import json
import re
import subprocess
import sys
import uuid
import tempfile

EVIDENCE = Path(__file__).resolve().parent
SOURCE = Path('src/Hexalith.Tenants.UI/Components/Pages/GlobalAdministratorsPage.razor')
PROJECTS = ['src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj',
            'tests/Hexalith.Tenants.UI.Tests/Hexalith.Tenants.UI.Tests.csproj']
ASSEMBLY = 'tests/Hexalith.Tenants.UI.Tests/bin/Debug/net10.0/Hexalith.Tenants.UI.Tests.dll'
PROPS = ['-c', 'Debug', '-m:1', '--no-restore', '-p:UseHexalithProjectReferences=true',
         '-p:HexalithEventStoreVersion=3.128.2', '-p:BuildProjectReferences=false', '-v:q']
CLASS = 'Hexalith.Tenants.UI.Tests.Components.GlobalAdministratorsPageTests'
TESTS = [('reset', 'WorkerResetCannotClearCursorHistoryWhileRendererOwnsIt'),
         ('notification', 'NotificationFinalizationContainsDisposalAfterAliveCheckBeforeDispatch')]
SUMMARY = re.compile(r'Total:\s*(\d+), Errors:\s*(\d+), Failed:\s*(\d+), Skipped:\s*(\d+), Not Run:\s*(\d+)')


def require_intended_failure(code, output, test, mutation):
    """Tooling, discovery, load, filter and unrelated assertion failures are not killed mutants."""
    summaries = SUMMARY.findall(output)
    failures = re.findall(r'^    (\S+) \[FAIL\]$', output, re.MULTILINE)
    if code != 1 or summaries != [('1', '0', '1', '0', '0')] or failures != [CLASS + '.' + test]:
        raise RuntimeError('Expected exactly the intended discovered/executed failing test')
    if mutation == 'reset':
        expected = 'Shouldly.ShouldAssertException : history' in output and re.search(
            r'should have single item but had\s+0\s+items', output)
    else:
        expected = ('System.ObjectDisposedException' in output and "Object name: 'renderer'." in output
                    and 'GlobalAdministratorsPage.FinalizeNotificationRefreshAsync()' in output)
    if not expected:
        raise RuntimeError('The intended test failed for an unexpected reason')
    return {'executed': 1, 'failed': 1, 'expected_failure_verified': True}


def self_test():
    name = TESTS[0][1]
    valid = f'    {CLASS}.{name} [FAIL]\nShouldly.ShouldAssertException : history\nshould have single item but had\n0\nitems\nTotal: 1, Errors: 0, Failed: 1, Skipped: 0, Not Run: 0'
    require_intended_failure(1, valid, name, 'reset')
    # A previous XML report has no authority: validation consumes only this run's fresh stdout.
    with tempfile.TemporaryDirectory() as stale_dir:
        Path(stale_dir, 'previous-results.xml').write_text('<test result="Fail" name="' + name + '"/>')
        try:
            require_intended_failure(1, 'No tests matched the filter', name, 'reset')
        except RuntimeError:
            pass
        else:
            raise AssertionError('Stale report authorized a no-test execution')
    invalid = [valid.replace('Total: 1', 'Total: 0'), valid.replace(name, 'WrongTest'),
               valid.replace('Shouldly.ShouldAssertException : history', 'System.IO.FileNotFoundException'),
               'No tests matched the filter', valid.replace('Failed: 1', 'Failed: 2')]
    for output in invalid:
        try:
            require_intended_failure(1, output, name, 'reset')
        except RuntimeError:
            continue
        raise AssertionError('Infrastructure/filter/unrelated failure was accepted')
    try:
        require_intended_failure(134, valid, name, 'reset')
    except RuntimeError:
        return
    raise AssertionError('Crash exit was accepted')


def build(output):
    for project in PROJECTS:
        if subprocess.run(['dotnet', 'build', project, *PROPS], stdout=output, stderr=subprocess.STDOUT).returncode:
            raise RuntimeError('Build failed: ' + project)


def run():
    self_test()
    run_id = uuid.uuid4().hex
    run_dir = EVIDENCE / 'mutation-runs' / run_id
    run_dir.mkdir(parents=True)
    (EVIDENCE / 'mutation-results.json').write_text(json.dumps({'run_id': run_id, 'status': 'running'}) + '\n')
    original = SOURCE.read_bytes()
    text = original.decode().replace('\r\n', '\n')
    results = {'run_id': run_id, 'mutations': [], 'restored_source': False, 'restored_binaries': False, 'errors': []}
    try:
        for mutation, test in TESTS:
            if mutation == 'reset':
                start = text.index('    internal async Task ResetPagingAsync()')
                end = text.index('    private async Task RefreshFromNotificationAsync()', start)
                old = '            await InvokeAsync(() =>\n            {\n                _cursorHistory.Clear();'
                new = '            _cursorHistory.Clear();\n            await InvokeAsync(() =>\n            {'
            else:
                start = text.index('    private async Task FinalizeNotificationRefreshAsync()')
                end = text.index('    private async Task QueueRemoveStatusNudgeAsync', start)
                old = '            // Disposal can land after the alive check and before the renderer accepts this notification.'
                new = '            throw;'
            section = text[start:end]
            if section.count(old) != 1:
                raise RuntimeError('Mutation anchor is missing or ambiguous: ' + mutation)
            changed = text[:start] + section.replace(old, new, 1) + text[end:]
            SOURCE.write_bytes(changed.replace('\n', '\r\n').encode())
            log = run_dir / ('mutation-' + mutation + '.txt')
            with log.open('w') as output:
                build(output)
                code = subprocess.run(['dotnet', ASSEMBLY, '-class', CLASS, '-method', '*.' + test],
                                      stdout=output, stderr=subprocess.STDOUT).returncode
            verified = require_intended_failure(code, log.read_text(), test, mutation)
            results['mutations'].append({'mutation': mutation, 'test': CLASS + '.' + test,
                                         'exit_code': code, 'log': str(log.relative_to(EVIDENCE)), **verified})
            SOURCE.write_bytes(original)
    except Exception as error:
        results['errors'].append(str(error))
    finally:
        SOURCE.write_bytes(original)
        results['restored_source'] = SOURCE.read_bytes() == original
        restored_log = run_dir / 'mutation-restored-output.txt'
        try:
            with restored_log.open('w') as output:
                build(output)
                command = ['dotnet', ASSEMBLY, '-class', CLASS]
                for _, test in TESTS:
                    command += ['-method', '*.' + test]
                code = subprocess.run(command, stdout=output, stderr=subprocess.STDOUT).returncode
            if code != 0 or SUMMARY.findall(restored_log.read_text()) != [('2', '0', '0', '0', '0')]:
                raise RuntimeError('Restored output did not execute both positive controls successfully')
            results['restored_binaries'] = True
        except Exception as error:
            results['errors'].append('Cleanup: ' + str(error))
        results['restored_log'] = str(restored_log.relative_to(EVIDENCE))
        (EVIDENCE / 'mutation-results.json').write_text(json.dumps(results, indent=2) + '\n')
    print(json.dumps(results, indent=2))
    return int(bool(results['errors']) or len(results['mutations']) != 2
               or not results['restored_source'] or not results['restored_binaries'])


if __name__ == '__main__':
    if '--self-test' in sys.argv:
        self_test()
        print('Exact-failure validator rejects infrastructure, filters, crashes and unrelated failures.')
    else:
        raise SystemExit(run())
