"""Reject unbound catalog archive metadata before any restore or code execution."""

import importlib.util
import json
from pathlib import Path
import shutil
import tempfile
import unittest
from unittest.mock import patch
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[2]
SPEC = importlib.util.spec_from_file_location('catalog_consumer', ROOT / 'scripts/verify-catalog-package-consumer.py')
CONSUMER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(CONSUMER)


class CatalogPackageMetadataTests(unittest.TestCase):
    def write_package(self, root, package_id, version='7.0.0', source='a' * 40, sdk='4.0.0', include_dependency=True,
                      mcp='2.0.0', include_mcp=True, framework='net11.0'):
        package = ET.Element('package')
        metadata = ET.SubElement(package, 'metadata')
        ET.SubElement(metadata, 'id').text = package_id
        ET.SubElement(metadata, 'version').text = version
        ET.SubElement(metadata, 'repository', type='git', commit=source)
        dependencies = ET.SubElement(metadata, 'dependencies')
        if include_dependency:
            dependency = 'Hexalith.EventStore.Contracts' if package_id.endswith('.Contracts') else 'Hexalith.EventStore.Server'
            ET.SubElement(dependencies, 'dependency', id=dependency, version=sdk)
        if include_mcp:
            ET.SubElement(dependencies, 'dependency', id='Hexalith.McpCli.Abstractions', version=mcp)
        target = root / (package_id + '.' + version + '.nupkg')
        with zipfile.ZipFile(target, 'w') as archive:
            archive.writestr(package_id + '.nuspec', ET.tostring(package))
            archive.writestr('lib/' + framework + '/' + package_id + '.dll', b'not executable')

    def test_current_net11_and_explicit_retained_net10_consumers_keep_distinct_frameworks(self):
        for framework in ('net11.0', 'net10.0'):
            with self.subTest(framework=framework), tempfile.TemporaryDirectory(prefix='tenants-catalog-framework-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, framework=framework)
                def inspect_restore(arguments, **kwargs):
                    self.assertEqual(ET.parse(arguments[2]).findtext('.//TargetFramework'), framework)
                    raise InterruptedError('Verified framework boundary')
                with patch.object(CONSUMER.subprocess, 'run', side_effect=inspect_restore):
                    with self.assertRaisesRegex(InterruptedError, 'Verified framework boundary'):
                        options = {} if framework == 'net11.0' else {'target_framework': framework}
                        CONSUMER.verify(root, 'a' * 40, **options)
        with self.assertRaisesRegex(ValueError, 'framework is not qualified'):
            CONSUMER.verify(Path('/unused'), target_framework='net12.0')

    def test_wrong_source_refuses_before_restore(self):
        self.reject(server={'source': 'b' * 40}, message='reviewed source')

    def test_mixed_package_version_refuses_before_restore(self):
        self.reject(server={'version': '7.0.1'}, message='versions disagree')

    def test_unknown_or_prerelease_sdk_refuses_before_restore(self):
        for sdk in ('3.128.4', '3.129.0-preview.1', '3.129.2', '3.130.3', '3.131.1', '3.132.3', '3.133.1', '4.0.5', '4.1.1', '4.2.1', '4.3.0', '5.0.0', '4.2.0-preview.1', '4.2.0.1', '4.2.0+', '4.2.0+invalid..metadata', '4.1.0-preview.1', '4.1.0.1', '4.1.0+', '4.1.0+invalid..metadata', '4.0.4-preview.1', '4.0.4.1', '4.0.3-preview.1', '4.0.0-preview.1',
                    '3.133.0-preview.1', '3.133.0.1',
                    '3.132.2-preview.1', '3.132.2.1',
                    '3.132.1-preview.1', '3.132.1-preview.1+metadata', '3.132.1.1', '3.132.1+',
                    '3.132.1+invalid..metadata',
                    '3.132.0-preview.1', '3.132.0-preview.1+metadata', '3.132.0.1', '3.132.0+',
                    '3.132.0+invalid..metadata',
                    '3.131.0-preview.1', '3.131.0-preview.1+metadata', '3.131.0.1', '3.131.0+',
                    '3.131.0+invalid..metadata'):
            with self.subTest(sdk=sdk):
                self.reject(server={'sdk': sdk}, message='qualified stable cohort')

    def test_every_qualified_sdk_and_alias_reaches_only_its_normalized_restore(self):
        for sdk in ('3.129.0', '3.129.1', '3.130.0', '3.130.1', '3.130.2', '3.131.0', '3.132.0', '3.132.1', '3.132.2', '3.133.0', '4.0.0', '4.0.1', '4.0.2', '4.0.3', '4.0.4', '4.1.0', '4.2.0'):
            for alias in (sdk, sdk + '.0', sdk + '+metadata-with-dash', sdk + '.0+metadata.1'):
                with self.subTest(sdk=sdk, alias=alias), tempfile.TemporaryDirectory(prefix='tenants-catalog-metadata-') as temporary:
                    root = Path(temporary)
                    self.write_package(root, 'Hexalith.Tenants.Contracts', sdk=alias)
                    self.write_package(root, 'Hexalith.Tenants.Server', sdk='[' + sdk + ', )')

                    def inspect_restore(arguments, **kwargs):
                        self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                        project = ET.parse(arguments[2])
                        sdk_references = [item for item in project.findall('.//PackageReference')
                                          if item.get('Include', '').startswith('Hexalith.EventStore.')]
                        self.assertEqual(len(sdk_references), 2)
                        self.assertTrue(all(item.get('Version') == '[' + sdk + ']' for item in sdk_references))
                        self.assertEqual(arguments[arguments.index('--packages') + 1], str(Path(arguments[2]).parent / 'packages'))
                        raise InterruptedError('Verified restore boundary')

                    with patch.object(CONSUMER.subprocess, 'run', side_effect=inspect_restore) as run:
                        with self.assertRaisesRegex(InterruptedError, 'Verified restore boundary'):
                            CONSUMER.verify(root, 'a' * 40)
                        run.assert_called_once()

    def test_missing_sdk_dependency_refuses_before_restore(self):
        self.reject(server={'include_dependency': False}, message='required SDK dependency')

    def test_missing_or_old_mcpcli_dependency_refuses_before_restore(self):
        self.reject(server={'include_mcp': False}, message='coordinated McpCli 2.0.0')
        for mcp in ('1.1.0', '2.0.2', '2.0.1-preview.1'):
            with self.subTest(mcp=mcp):
                self.reject(server={'mcp': mcp}, message='coordinated McpCli 2.0.0')

    def test_refreshed_mcpcli_cohort_reaches_restore(self):
        for mcp in ('2.0.0', '2.0.1'):
            for dependency_range in (mcp, '[' + mcp + ']', '[' + mcp + ', )', '[' + mcp + ', 3.0.0)'):
                with self.subTest(mcp=mcp, dependency_range=dependency_range), tempfile.TemporaryDirectory(prefix='tenants-catalog-metadata-') as temporary:
                    root = Path(temporary)
                    self.write_package(root, 'Hexalith.Tenants.Contracts', mcp=dependency_range)
                    self.write_package(root, 'Hexalith.Tenants.Server', mcp=dependency_range)

                    def inspect_restore(arguments, **kwargs):
                        self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                        project = ET.parse(arguments[2])
                        self.assertEqual(project.find('.//PackageReference[@Include="Hexalith.McpCli.Abstractions"]').get('Version'), '[' + mcp + ']')
                        raise InterruptedError('Verified restore boundary')

                    with patch.object(CONSUMER.subprocess, 'run', side_effect=inspect_restore) as run:
                        with self.assertRaisesRegex(InterruptedError, 'Verified restore boundary'):
                            CONSUMER.verify(root, 'a' * 40)
                        run.assert_called_once()

    def test_exclusive_or_malformed_mcpcli_ranges_refuse_before_restore(self):
        for dependency_range in ('(2.0.1, )', '(2.0.0, 3.0.0]', '(, 2.0.1]',
                                 '[2.0.1, 2.0.0)', '[2.0.1, 2.0.1)', '[2.0.1, )garbage',
                                 '[2.0.1, invalid)', '[2.0.1, 3.0.0', '[2.0.1, ]',
                                 '[2.0.1, 3.0.0, 4.0.0)', '[2.0.1-preview.1, )', '[2.0.2, )'):
            with self.subTest(dependency_range=dependency_range):
                self.reject(server={'mcp': dependency_range}, message='McpCli')

    def test_restored_mcpcli_must_match_the_admitted_package_before_build(self):
        for mcp in ('2.0.0', '2.0.1'):
            for restored_version, kind in ((mcp, 'package'), ('2.0.2', 'package'), (mcp, 'project'), (None, None)):
                with self.subTest(mcp=mcp, restored_version=restored_version, kind=kind), tempfile.TemporaryDirectory(prefix='tenants-catalog-assets-') as temporary:
                    root = Path(temporary)
                    for package_id in CONSUMER.IDS:
                        self.write_package(root, package_id, mcp=mcp)

                    def restore(arguments, **kwargs):
                        if arguments[:2] == ['dotnet', 'build']:
                            raise InterruptedError('Verified build boundary')
                        self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                        consumer_root = Path(arguments[2]).parent
                        (consumer_root / 'obj').mkdir()
                        libraries = {}
                        if restored_version is not None:
                            libraries['Hexalith.McpCli.Abstractions/' + restored_version] = {'type': kind}
                        for package_id in CONSUMER.IDS:
                            libraries[package_id + '/7.0.0'] = {'type': 'package'}
                            destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                            destination.parent.mkdir(parents=True)
                            shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                        (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                    with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                        if restored_version == mcp and kind == 'package':
                            with self.assertRaisesRegex(InterruptedError, 'Verified build boundary'):
                                CONSUMER.verify(root, 'a' * 40)
                            self.assertEqual(run.call_count, 2)
                        else:
                            with self.assertRaisesRegex(ValueError, 'Restored McpCli dependency differs'):
                                CONSUMER.verify(root, 'a' * 40)
                            run.assert_called_once()

    def test_mixed_mcpcli_cohort_refuses_before_restore(self):
        self.reject(server={'mcp': '2.0.1'}, message='coordinated McpCli 2.0.0')
        self.reject(contracts={'mcp': '2.0.1'}, server={'mcp': '2.0.0'}, message='coordinated McpCli 2.0.0')

    def test_mixed_sdk_cohort_refuses_before_restore(self):
        for sdk in ('3.129.1', '3.130.2', '3.131.0', '3.132.0', '3.132.1', '3.132.2', '3.133.0'):
            with self.subTest(sdk=sdk):
                self.reject(server={'sdk': sdk}, message='qualified stable cohort')

        self.reject(contracts={'sdk': '3.132.0'}, server={'sdk': '3.132.1'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '3.132.1'}, server={'sdk': '3.132.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '3.132.1'}, server={'sdk': '3.132.2'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '3.132.2'}, server={'sdk': '3.132.1'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '3.132.2'}, server={'sdk': '3.133.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '3.133.0'}, server={'sdk': '3.132.2'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.0.0'}, server={'sdk': '3.133.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.0.4'}, server={'sdk': '4.1.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.1.0'}, server={'sdk': '4.0.4'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.1.0'}, server={'sdk': '4.2.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.2.0'}, server={'sdk': '4.1.0'}, message='qualified stable cohort')

    def reject(self, server, message, contracts=None):
        with tempfile.TemporaryDirectory(prefix='tenants-catalog-metadata-') as temporary:
            root = Path(temporary)
            self.write_package(root, 'Hexalith.Tenants.Contracts', **(contracts or {}))
            self.write_package(root, 'Hexalith.Tenants.Server', **server)
            with patch.object(CONSUMER.subprocess, 'run', side_effect=AssertionError('Invalid metadata reached execution')) as run:
                with self.assertRaisesRegex(ValueError, message):
                    CONSUMER.verify(root, 'a' * 40)
                run.assert_not_called()


if __name__ == '__main__':
    unittest.main()
