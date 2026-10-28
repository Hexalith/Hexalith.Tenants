#!/usr/bin/env python3
"""Qualify the real locked release proposal against offline, write-forbidden Git histories."""
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]
SCRIPT = ROOT / 'scripts/calculate-release-version.mjs'
REAL_GIT = shutil.which('git')


class ReadOnlyReleaseVersionTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory(prefix='tenants-release-version-')
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.git('init', '--initial-branch=main')
        self.git('config', 'user.email', 'release-version@example.invalid')
        self.git('config', 'user.name', 'Release Version Fixture')
        self.git('config', 'commit.gpgsign', 'false')
        self.git('remote', 'add', 'origin', 'https://unavailable.example.invalid/release-version.git')
        (self.root / 'node_modules').symlink_to(ROOT / 'node_modules', target_is_directory=True)
        (self.root / 'package.json').write_text(json.dumps({'name': 'release-version-fixture', 'private': True}))
        self.config = {'branches': ['main'], 'tagFormat': 'v${version}',
                       'plugins': ['@semantic-release/commit-analyzer']}
        self.save_config()
        self.commit('build: seed release history')
        self.bin = self.root / 'bin'
        self.bin.mkdir()
        wrapper = self.bin / 'git'
        wrapper.write_text('''#!/usr/bin/env python3
import os, sys
from pathlib import Path
args = sys.argv[1:]
with Path(os.environ['PROPOSAL_GIT_TRACE']).open('a') as trace:
    trace.write(' '.join(args) + '\\n')
allowed = args and (args[0] in ('log', 'rev-parse', 'rev-list')
    or args == ['symbolic-ref', '--quiet', '--short', 'HEAD']
    or args == ['config', '--get', 'remote.origin.url']
    or args == ['tag', '--merged', 'HEAD'])
if not allowed:
    raise SystemExit('Git write/network operation is forbidden in release proposal: ' + ' '.join(args))
os.execv(''' + repr(REAL_GIT) + ''', ['git', *args])
''')
        wrapper.chmod(0o755)
        self.environment = os.environ.copy()
        for key in ('GITHUB_TOKEN', 'GH_TOKEN', 'NUGET_API_KEY', 'HEXALITH_ZOT_API_KEY'):
            self.environment.pop(key, None)
        self.environment.update(PATH=str(self.bin) + os.pathsep + os.environ['PATH'],
                                PROPOSAL_GIT_TRACE=str(self.root / 'git-trace'))

    def git(self, *args):
        return subprocess.run([REAL_GIT, *args], cwd=self.root, text=True, capture_output=True, check=True).stdout.strip()

    def commit(self, message):
        return self.git('commit', '--allow-empty', '-m', message)

    def save_config(self):
        (self.root / '.releaserc.json').write_text(json.dumps(self.config))

    def propose(self, expected=None):
        result = subprocess.run(['node', str(SCRIPT)], cwd=self.root, env=self.environment,
                                capture_output=True, text=True, timeout=20)
        if expected is not None:
            self.assertEqual(result.returncode, 0, result.stderr)
            self.assertEqual(result.stdout.strip(), expected)
        return result

    def test_patch_minor_and_major_use_locked_commit_analysis_without_credentials(self):
        self.git('tag', 'v5.10.1')
        for message, expected in [('fix: repair behavior', '5.10.2'),
                                  ('feat: extend behavior', '5.11.0'),
                                  ('feat: replace behavior\n\nBREAKING CHANGE: replace behavior', '6.0.0')]:
            with self.subTest(message=message):
                self.commit(message)
                self.propose(expected)

    def test_documentation_only_returns_no_release(self):
        self.git('tag', 'v5.10.1')
        self.commit('docs: update usage')
        self.propose('none')

    def test_first_release_matches_semantic_release_default(self):
        self.commit('fix: introduce behavior')
        self.propose('1.0.0')

    def test_highest_reachable_stable_tag_wins_over_unmerged_and_prerelease_tags(self):
        self.git('tag', 'v5.9.0')
        self.commit('fix: establish stable release')
        self.git('tag', 'v5.10.1')
        self.git('checkout', '-b', 'future')
        self.commit('feat: future behavior')
        self.git('tag', 'v99.0.0')
        self.git('checkout', 'main')
        self.commit('fix: repair behavior')
        self.git('tag', 'v6.0.0-rc.1')
        self.propose('5.10.2')

    def test_configured_release_rules_are_respected(self):
        self.config['plugins'] = [['@semantic-release/commit-analyzer',
                                   {'releaseRules': [{'type': 'docs', 'release': 'patch'}]}]]
        self.save_config()
        self.git('tag', 'v5.10.1')
        self.commit('docs: update usage')
        self.propose('5.10.2')

    def test_publication_and_verification_lifecycle_hooks_are_never_called(self):
        marker = self.root / 'publication-hook-reached'
        self.config['plugins'].append(['@semantic-release/exec',
            {name: 'touch publication-hook-reached' for name in
             ('verifyConditionsCmd', 'verifyReleaseCmd', 'prepareCmd', 'publishCmd', 'successCmd', 'failCmd')}])
        self.save_config()
        self.git('tag', 'v5.10.1')
        self.commit('fix: repair behavior')
        before = self.git('show-ref')
        self.propose('5.10.2')
        self.assertFalse(marker.exists())
        self.assertEqual(self.git('show-ref'), before)
        trace = (self.root / 'git-trace').read_text()
        self.assertNotIn('push ', trace)
        self.assertNotIn('fetch ', trace)

    def test_checked_in_release_configuration_works_without_publication_credentials(self):
        self.config = json.loads((ROOT / '.releaserc.json').read_text())
        self.save_config()
        self.git('tag', 'v5.10.1')
        self.commit('feat: extend behavior')
        self.propose('5.11.0')

    def test_configured_exec_analyzer_receives_streams_without_polluting_version_output(self):
        self.config['plugins'] = [['@semantic-release/exec', {'analyzeCommitsCmd': 'printf patch'}]]
        self.save_config()
        self.git('tag', 'v5.10.1')
        self.commit('docs: update usage')
        self.propose('5.10.2')

    def test_shallow_history_is_rejected_without_fetching(self):
        self.git('tag', 'v5.10.1')
        self.commit('fix: repair behavior')
        (self.root / '.git/shallow').write_text(self.git('rev-parse', 'HEAD') + '\n')
        result = self.propose()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('complete Git history', result.stderr)
        self.assertEqual(result.stdout, '')

    def prepare_shell(self):
        self.git('tag', 'v5.10.1')
        self.commit('fix: repair behavior')
        scripts = self.root / 'scripts'
        scripts.mkdir()
        (scripts / 'calculate-release-version.mjs').symlink_to(SCRIPT)
        (scripts / 'validate-publication-preflight.sh').write_text(
            '#!/bin/bash\nprintf "preflight %s %s\\n" "$1" "$2" >> "$CANDIDATE_TRACE"\n'
            'exit 91\n')
        dotnet = self.bin / 'dotnet'
        dotnet.write_text('#!/bin/bash\nprintf "dotnet %s\\n" "$*" >> "$CANDIDATE_TRACE"\n'
                          'exit "${CANDIDATE_BUILD_EXIT:-0}"\n')
        dotnet.chmod(0o755)
        for name in ('pack-release-packages', 'validate-nuget-packages', 'validate-consumer-package-references'):
            (scripts / (name + '.py')).write_text(
                'import os\nfrom pathlib import Path\n'
                'with Path(os.environ["CANDIDATE_TRACE"]).open("a") as output:\n'
                '    output.write(' + repr(name + '\n') + ')\n')
        self.environment['CANDIDATE_TRACE'] = str(self.root / 'candidate-trace')

    def run_shell(self, **updates):
        return subprocess.run(['bash', str(ROOT / 'scripts/build-release-candidate.sh')],
                              cwd=self.root, env={**self.environment, **updates},
                              capture_output=True, text=True, timeout=20)

    def test_shell_build_failure_prevents_pack_and_package_validation(self):
        self.prepare_shell()
        result = self.run_shell(CANDIDATE_BUILD_EXIT='9')
        self.assertEqual(result.returncode, 9, result.stderr)
        trace = (self.root / 'candidate-trace').read_text().splitlines()
        self.assertEqual(len(trace), 1, trace)
        self.assertTrue(trace[0].startswith('dotnet build '), trace)

    def test_shell_helper_failure_prevents_preflight_build_and_pack(self):
        self.prepare_shell()
        self.config['branches'].append('next')
        self.save_config()
        result = self.run_shell()
        self.assertNotEqual(result.returncode, 0)
        self.assertFalse((self.root / 'candidate-trace').exists())
        self.assertFalse((self.root / '.hexalith/release-candidate/disposition').exists())

    def test_shell_rejects_each_publication_credential_before_any_work(self):
        self.prepare_shell()
        for key in ('NUGET_API_KEY', 'HEXALITH_ZOT_API_KEY'):
            with self.subTest(key=key):
                result = self.run_shell(**{key: '__INERT_TEST_PLACEHOLDER__'})
                self.assertNotEqual(result.returncode, 0)
                self.assertIn('must not see publication credentials', result.stderr)
                self.assertFalse((self.root / 'candidate-trace').exists())
                self.assertFalse((self.root / '.hexalith').exists())

    def test_shell_prepares_exact_version_and_source_without_authenticated_preflight(self):
        self.prepare_shell()
        result = self.run_shell()
        self.assertEqual(result.returncode, 0, result.stderr)
        trace = (self.root / 'candidate-trace').read_text().splitlines()
        self.assertTrue(trace[0].startswith('dotnet build '), trace)
        self.assertIn('-p:Version=5.10.2', trace[0])
        self.assertEqual(trace[1:], ['pack-release-packages', 'validate-nuget-packages',
                                     'validate-consumer-package-references'])
        candidate = self.root / '.hexalith/release-candidate'
        self.assertEqual((candidate / 'version').read_text().strip(), '5.10.2')
        self.assertEqual((candidate / 'source-sha').read_text().strip(), self.git('rev-parse', 'HEAD'))
        self.assertEqual((candidate / 'disposition').read_text().strip(), 'release')

    def test_unsupported_branch_profiles_and_wrong_branch_fail_closed(self):
        self.git('tag', 'v5.10.1')
        self.commit('fix: repair behavior')
        self.config['branches'].append('next')
        self.save_config()
        result = self.propose()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('only stable main', result.stderr)
        self.config['branches'] = ['main']
        self.save_config()
        self.git('checkout', '-b', 'other')
        result = self.propose()
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('must run from main', result.stderr)


if __name__ == '__main__':
    unittest.main()
