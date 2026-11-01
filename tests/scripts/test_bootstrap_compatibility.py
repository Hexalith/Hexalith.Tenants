"""Exercise frozen source/SDK selection without changing a checkout or starting hosts."""

import json
import os
from pathlib import Path
import subprocess
import tempfile
import textwrap
import unittest


ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'scripts/prepare-bootstrap-compatibility.sh'
REVISION = '717a06e16c3afd81b904eaf4de646b74b13262d2'


class BootstrapCompatibilityTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='tenants-bootstrap-selection-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        (self.root / 'references/Hexalith.EventStore').mkdir(parents=True)
        self.bin = self.root / 'bin'
        self.bin.mkdir()
        self.log = self.root / 'git-calls.jsonl'
        self.sha = self.root / 'selected-sha'
        self.sha.write_text('current-source')
        self.environment_file = self.root / 'github-env'
        self.environment_file.write_text('Retained=value\n')
        git = self.bin / 'git'
        git.write_text('''#!/usr/bin/env python3
import json, os, pathlib, sys
arguments = sys.argv[1:]
with open(os.environ['MOCK_GIT_LOG'], 'a') as output:
    output.write(json.dumps(arguments) + '\\n')
if arguments[:1] == ['-C']:
    arguments = arguments[2:]
if arguments == ['rev-parse', '--show-toplevel']:
    print(os.environ['MOCK_GIT_ROOT'])
elif arguments[:1] == ['config']:
    print(os.environ.get('MOCK_DECLARED_PATH', 'references/Hexalith.EventStore'))
elif arguments == ['status', '--porcelain']:
    print(' M tracked.txt' if os.environ.get('MOCK_DIRTY') == 'true' else '', end='')
elif 'checkout' in arguments:
    pathlib.Path(os.environ['MOCK_GIT_SHA']).write_text(arguments[-1])
elif arguments == ['rev-parse', 'HEAD']:
    print(pathlib.Path(os.environ['MOCK_GIT_SHA']).read_text())
''')
        git.chmod(0o755)
        self.environment = os.environ | {
            'PATH': str(self.bin) + os.pathsep + os.environ['PATH'],
            'MOCK_GIT_ROOT': str(self.root), 'MOCK_GIT_LOG': str(self.log),
            'MOCK_GIT_SHA': str(self.sha), 'GITHUB_ACTIONS': 'true',
            'GITHUB_JOB': 'aspire-tests', 'GITHUB_ENV': str(self.environment_file),
        }
        self.environment.pop('MOCK_DIRTY', None)
        self.environment.pop('MOCK_DECLARED_PATH', None)

    def run_script(self, *arguments, **changes):
        environment = self.environment.copy()
        for name, value in changes.items():
            if value is None:
                environment.pop(name, None)
            else:
                environment[name] = value
        return subprocess.run(['bash', str(SCRIPT), *arguments], env=environment,
                              capture_output=True, text=True, timeout=20)

    def check_no_selection(self):
        self.assertEqual(self.sha.read_text(), 'current-source')
        self.assertEqual(self.environment_file.read_text(), 'Retained=value\n')
        calls = [json.loads(line) for line in self.log.read_text().splitlines()]
        self.assertFalse(any('checkout' in call or 'fetch' in call for call in calls))

    def test_every_topology_job_binds_source_and_the_sdk_for_subsequent_processes(self):
        for job in ('authenticated-routes', 'aspire-tests', 'performance-tests'):
            with self.subTest(job=job):
                self.sha.write_text('current-source')
                self.environment_file.write_text('Retained=value\n')
                result = self.run_script(GITHUB_JOB=job)
                self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
                self.assertEqual(self.sha.read_text(), REVISION)
                self.assertEqual(self.environment_file.read_text(),
                                 'Retained=value\nHexalithEventStoreVersion=3.128.4\n')
                self.assertIn('checkout', self.log.read_text())

    def run_live_build(self, version):
        log = self.root / 'dotnet-calls.jsonl'
        dotnet = self.bin / 'dotnet'
        dotnet.write_text("""#!/usr/bin/env python3
import json, os, sys
with open(os.environ['MOCK_DOTNET_LOG'], 'a') as output:
    output.write(json.dumps({'arguments': sys.argv[1:],
                            'versionEnvironment': os.environ.get('HexalithEventStoreVersion')}) + '\\n')
""")
        dotnet.chmod(0o755)
        workflow = (ROOT / '.github/workflows/ci.yml').read_text()
        step = workflow.split('      - name: Build live hosts and focused suites against published packages\n', 1)[1]
        block = step.split('        run: |\n', 1)[1].split('\n      - name:', 1)[0]
        environment = self.environment | {'MOCK_DOTNET_LOG': str(log)}
        if version is None:
            environment.pop('HexalithEventStoreVersion', None)
        else:
            environment['HexalithEventStoreVersion'] = version
        result = subprocess.run(['bash', '-c', textwrap.dedent(block)], env=environment,
                                capture_output=True, text=True, timeout=20)
        calls = [json.loads(line) for line in log.read_text().splitlines()] if log.exists() else []
        return result, calls

    def test_live_build_retains_legacy_selection_without_leaking_environment_to_tooling(self):
        result, calls = self.run_live_build('3.128.4')
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.assertEqual(len(calls), 6)
        for call in calls:
            self.assertIsNone(call['versionEnvironment'])
            self.assertIn('-p:HexalithEventStoreVersion=3.128.4', call['arguments'])
            self.assertIn('-p:UseHexalithProjectReferences=false', call['arguments'])
        self.assertEqual(len({call['arguments'][1] for call in calls}), 6)

    def test_live_build_refuses_a_missing_compatibility_selection(self):
        result, calls = self.run_live_build(None)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(calls, [])

    def test_build_and_test_keeps_current_source_and_normal_sdk_default(self):
        result = self.run_script(GITHUB_JOB='build-and-test', GITHUB_ENV=None)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.check_no_selection()

    def test_local_check_only_does_not_select_compatibility_sdk(self):
        result = self.run_script('--check', GITHUB_ACTIONS='false', GITHUB_ENV=None)
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)
        self.check_no_selection()

    def test_missing_environment_file_refuses_before_source_selection(self):
        result = self.run_script(GITHUB_ENV=None)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('requires the GitHub environment file', result.stderr)
        self.check_no_selection()

    def test_unknown_job_refuses_before_source_selection(self):
        result = self.run_script(GITHUB_JOB='release')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('limited to the declared compatibility topology jobs', result.stderr)
        self.check_no_selection()

    def test_non_ci_selection_refuses_before_source_selection(self):
        result = self.run_script(GITHUB_ACTIONS='false')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('requires a disposable GitHub Actions checkout', result.stderr)
        self.check_no_selection()

    def test_dirty_source_refuses_before_source_selection(self):
        result = self.run_script(MOCK_DIRTY='true')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('contains changes', result.stderr)
        self.check_no_selection()

    def test_undeclared_source_refuses_before_source_selection(self):
        result = self.run_script(MOCK_DECLARED_PATH='different/source')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('root-declared EventStore submodule', result.stderr)
        self.check_no_selection()


if __name__ == '__main__':
    unittest.main()
