#!/usr/bin/env python3
"""Exercise real package validation across separate preparation/publication checkouts."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile
import unittest
from unittest.mock import patch
import zipfile

ROOT = Path(__file__).resolve().parents[2]
CONTEXT = ROOT / 'scripts/release-validation-context.py'
VALIDATOR = ROOT / 'scripts/validate-nuget-packages.py'
VERSION = '5.11.0'


class ReleaseValidationContextTests(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix='tenants handoff ')
        self.addCleanup(temporary.cleanup)
        self.base = Path(temporary.name)
        self.prepare = self.base / 'prepare'
        self.publish = self.base / 'publish'
        self.prepare.mkdir()
        self.manifest = json.loads((ROOT / 'tools/release-packages.json').read_text())
        (self.prepare / 'tools').mkdir()
        (self.prepare / 'tools/release-packages.json').write_text(json.dumps(self.manifest))
        for package in self.manifest['packages']:
            project = self.prepare / package['project']
            project.parent.mkdir(parents=True)
            project.write_text('<Project Sdk="Microsoft.NET.Sdk" />\n')
        (self.prepare / '.gitignore').write_text('obj/\nnupkgs/\n.hexalith/\n')
        self.git(self.prepare, 'init', '--initial-branch=main')
        self.git(self.prepare, 'config', 'user.name', 'Release Handoff Fixture')
        self.git(self.prepare, 'config', 'user.email', 'handoff@example.invalid')
        self.git(self.prepare, 'config', 'commit.gpgsign', 'false')
        self.git(self.prepare, 'add', '.')
        self.git(self.prepare, 'commit', '-m', 'build: seed release handoff')
        self.source = self.git(self.prepare, 'rev-parse', 'HEAD')
        (self.prepare / 'nupkgs').mkdir()
        for package in self.manifest['packages']:
            project = self.prepare / package['project']
            assets = self.assets_path(self.prepare, package)
            assets.parent.mkdir()
            assets.write_text(json.dumps({
                'projectFileDependencyGroups': {'net10.0': ['Fixture.Direct >= 1.2.3']},
                'centralTransitiveDependencyGroups': {'net10.0': {'Fixture.Central': {'version': '[4.5.6, )'}}},
                'project': {'restore': {'projectPath': str(project), 'projectName': package['id'],
                            'projectStyle': 'PackageReference', 'originalTargetFrameworks': ['net10.0']},
                            'frameworks': {'net10.0': {}}},
            }))
            path = self.prepare / 'nupkgs' / (package['id'] + '.' + VERSION + '.nupkg')
            with zipfile.ZipFile(path, 'w') as archive:
                archive.writestr(package['id'] + '.nuspec', f'''<package><metadata>
<id>{package['id']}</id><version>{VERSION}</version><readme>README.md</readme>
<license type="expression">MIT</license><dependencies><group targetFramework="net10.0">
<dependency id="Fixture.Direct" version="1.2.3" />
<dependency id="Fixture.Central" version="4.5.6" />
</group></dependencies></metadata></package>''')
                archive.writestr('README.md', 'Isolated handoff metadata fixture.\n')
        state = self.prepare / '.hexalith/release-candidate'
        state.mkdir(parents=True)
        (state / 'version').write_text(VERSION + '\n')
        (state / 'source-sha').write_text(self.source + '\n')
        self.assert_success(self.validate(self.prepare))
        self.assert_success(self.context(self.prepare, 'capture'))
        subprocess.run(['git', 'clone', '--quiet', '--no-hardlinks', str(self.prepare), str(self.publish)],
                       check=True, capture_output=True, text=True)
        shutil.copytree(self.prepare / 'nupkgs', self.publish / 'nupkgs')
        shutil.copytree(self.prepare / '.hexalith', self.publish / '.hexalith')
        self.bundle = self.publish / '.hexalith/release-candidate/validation-context'

    @staticmethod
    def git(root, *arguments):
        return subprocess.check_output(['git', *arguments], cwd=root, stderr=subprocess.PIPE, text=True).strip()

    @staticmethod
    def assets_path(root, package):
        return (root / package['project']).parent / 'obj/project.assets.json'

    def context(self, root, mode):
        return subprocess.run(['python3', str(CONTEXT), mode, '--manifest',
                               str(root / 'tools/release-packages.json')],
                              cwd=root, capture_output=True, text=True, timeout=20)

    def validate(self, root):
        return subprocess.run(['python3', str(VALIDATOR), str(root / 'nupkgs'), '--manifest',
                               str(root / 'tools/release-packages.json')],
                              cwd=root, capture_output=True, text=True, timeout=20)

    def assert_success(self, result):
        self.assertEqual(result.returncode, 0, result.stdout + result.stderr)

    def assert_no_restore_writes(self):
        for package in self.manifest['packages']:
            self.assertFalse(self.assets_path(self.publish, package).exists())

    def assert_restore_refused(self):
        result = self.context(self.publish, 'restore')
        self.assertNotEqual(result.returncode, 0, result.stdout)
        self.assert_no_restore_writes()

    def copy_wrapper_scripts(self):
        scripts = self.publish / 'scripts'
        scripts.mkdir()
        for name in ('release-validation-context.py', 'validate-nuget-packages.py'):
            shutil.copy2(ROOT / 'scripts' / name, scripts / name)
        (scripts / 'validate-consumer-package-references.py').write_text(
            'from pathlib import Path\nPath("consumer-called").touch()\n')
        clients = self.publish / 'clients'
        clients.mkdir()
        python = clients / 'python3'
        python.write_text('#!' + sys.executable + '\n' + '''import os, sys
from pathlib import Path
with Path('wrapper-calls').open('a') as output:
    output.write(' '.join(sys.argv[1:]) + '\\n')
os.execv(sys.executable, [sys.executable, *sys.argv[1:]])
''')
        python.chmod(0o755)
        return {**os.environ, 'PATH': str(clients) + os.pathsep + os.environ['PATH']}

    def run_wrapper(self, environment):
        return subprocess.run(['bash', str(ROOT / 'scripts/require-prepared-release-packages.sh'), VERSION],
                              cwd=self.publish, env=environment, capture_output=True, text=True, timeout=20)

    def test_two_checkout_handoff_enables_unchanged_real_package_validator(self):
        self.assertNotEqual(self.validate(self.publish).returncode, 0)
        self.assert_success(self.context(self.publish, 'restore'))
        self.assert_success(self.validate(self.publish))
        for package in self.manifest['packages']:
            original = json.loads(self.assets_path(self.prepare, package).read_text())
            restored = json.loads(self.assets_path(self.publish, package).read_text())
            self.assertEqual(original['project']['restore']['projectPath'], str(self.prepare / package['project']))
            original['project']['restore']['projectPath'] = package['project']
            self.assertEqual(restored, original)

    def test_each_missing_asset_fails_before_any_materialization(self):
        for number in range(len(self.manifest['packages'])):
            with self.subTest(number=number):
                path = self.bundle / f'{number}.json'
                content = path.read_bytes()
                path.unlink()
                self.assert_restore_refused()
                path.write_bytes(content)

    def test_changed_asset_bytes_are_rejected(self):
        path = self.bundle / '0.json'
        path.write_text(path.read_text() + '\n')
        self.assert_restore_refused()

    def test_changed_package_bytes_are_rejected(self):
        path = next((self.publish / 'nupkgs').glob('*.nupkg'))
        path.write_bytes(path.read_bytes() + b'changed')
        self.assert_restore_refused()

    def test_missing_package_is_rejected(self):
        next((self.publish / 'nupkgs').glob('*.nupkg')).unlink()
        self.assert_restore_refused()

    def test_changed_manifest_is_rejected(self):
        path = self.publish / 'tools/release-packages.json'
        path.write_text(path.read_text() + '\n')
        self.assert_restore_refused()

    def test_changed_candidate_version_is_rejected(self):
        (self.publish / '.hexalith/release-candidate/version').write_text('5.11.1\n')
        self.assert_restore_refused()

    def test_capture_rejects_actual_package_version_mismatch(self):
        shutil.rmtree(self.prepare / '.hexalith/release-candidate/validation-context')
        (self.prepare / '.hexalith/release-candidate/version').write_text('5.11.1\n')
        result = self.context(self.prepare, 'capture')
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('package IDs or versions', result.stderr)

    def test_tracked_source_edit_without_new_commit_is_rejected(self):
        project = self.publish / self.manifest['packages'][0]['project']
        project.write_text(project.read_text() + '<!-- changed source -->\n')
        self.assert_restore_refused()

    def test_changed_candidate_source_is_rejected(self):
        (self.publish / '.hexalith/release-candidate/source-sha').write_text('0' * 40 + '\n')
        self.assert_restore_refused()

    def test_live_source_drift_is_rejected(self):
        self.git(self.publish, '-c', 'user.name=Fixture', '-c', 'user.email=fixture@example.invalid',
                 '-c', 'commit.gpgsign=false', 'commit', '--allow-empty', '-m', 'build: advance fixture')
        self.assert_restore_refused()

    def test_asset_path_symlink_is_rejected_without_external_writes(self):
        path = self.bundle / '0.json'
        external = self.base / 'external.json'
        external.write_bytes(path.read_bytes())
        path.unlink()
        path.symlink_to(external)
        self.assert_restore_refused()
        self.assertTrue(external.is_file())

    def test_output_directory_symlink_is_rejected(self):
        external = self.base / 'outside'
        external.mkdir()
        target = self.assets_path(self.publish, self.manifest['packages'][0]).parent
        target.symlink_to(external, target_is_directory=True)
        self.assert_restore_refused()
        self.assertEqual(list(external.iterdir()), [])

    def test_non_directory_destination_fails_before_any_materialization(self):
        target = self.assets_path(self.publish, self.manifest['packages'][-1]).parent
        target.write_text('preserve existing file')
        self.assert_restore_refused()
        self.assertEqual(target.read_text(), 'preserve existing file')

    def test_invalid_schema_with_updated_digest_fails_before_materialization(self):
        path = self.bundle / '0.json'
        assets = json.loads(path.read_text())
        assets['projectFileDependencyGroups']['net10.0'] = ['malformed-dependency']
        path.write_text(json.dumps(assets))
        index_path = self.bundle / 'index.json'
        index = json.loads(index_path.read_text())
        index['assets'][0]['sha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
        index_path.write_text(json.dumps(index))
        self.assert_restore_refused()

    def test_late_write_failure_rolls_back_only_new_evidence(self):
        spec = importlib.util.spec_from_file_location('context_writer_test', CONTEXT)
        module = importlib.util.module_from_spec(spec)
        sys.modules[spec.name] = module
        spec.loader.exec_module(module)
        original = Path.open
        writes = 0

        def fail_late(path, mode='r', *args, **kwargs):
            nonlocal writes
            if mode == 'xb':
                writes += 1
                if writes == len(self.manifest['packages']):
                    raise OSError('injected late filesystem failure')
            return original(path, mode, *args, **kwargs)

        with patch.object(Path, 'open', fail_late):
            with self.assertRaisesRegex(OSError, 'injected late'):
                module.restore(self.publish / 'tools/release-packages.json')
        self.assert_no_restore_writes()

    def test_existing_restore_evidence_is_preserved(self):
        target = self.assets_path(self.publish, self.manifest['packages'][0])
        target.parent.mkdir()
        target.write_bytes(b'preserve existing restore data')
        result = self.context(self.publish, 'restore')
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual(target.read_bytes(), b'preserve existing restore data')
        for package in self.manifest['packages'][1:]:
            self.assertFalse(self.assets_path(self.publish, package).exists())

    def test_capture_refuses_stale_context(self):
        result = self.context(self.prepare, 'capture')
        self.assertNotEqual(result.returncode, 0)
        self.assert_success(self.context(self.publish, 'restore'))

    def test_dependency_drift_still_fails_real_metadata_validation(self):
        self.assert_success(self.context(self.publish, 'restore'))
        path = self.assets_path(self.publish, self.manifest['packages'][0])
        content = json.loads(path.read_text())
        content['projectFileDependencyGroups']['net10.0'] = ['Fixture.Direct >= 9.0.0']
        path.write_text(json.dumps(content))
        result = self.validate(self.publish)
        self.assertNotEqual(result.returncode, 0)
        self.assertIn('version range mismatch', result.stderr)

    def test_real_publisher_wrapper_restores_before_both_validators(self):
        environment = self.copy_wrapper_scripts()
        result = self.run_wrapper(environment)
        self.assert_success(result)
        self.assertEqual((self.publish / 'wrapper-calls').read_text().splitlines(), [
            'scripts/release-validation-context.py restore',
            'scripts/validate-nuget-packages.py ./nupkgs',
            'scripts/validate-consumer-package-references.py ./nupkgs'])
        self.assertTrue((self.publish / 'consumer-called').exists())

    def test_real_publisher_wrapper_stops_before_validators_on_context_failure(self):
        environment = self.copy_wrapper_scripts()
        path = self.bundle / '0.json'
        path.write_text(path.read_text() + '\n')
        result = self.run_wrapper(environment)
        self.assertNotEqual(result.returncode, 0)
        self.assertEqual((self.publish / 'wrapper-calls').read_text().splitlines(),
                         ['scripts/release-validation-context.py restore'])
        self.assertFalse((self.publish / 'consumer-called').exists())
        self.assert_no_restore_writes()


if __name__ == '__main__':
    unittest.main()
