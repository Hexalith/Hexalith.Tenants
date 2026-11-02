"""Reject unbound catalog archive metadata before any restore or code execution."""

import importlib.util
from pathlib import Path
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
    def write_package(self, root, package_id, version='6.0.0', source='a' * 40, sdk='3.129.0', include_dependency=True):
        package = ET.Element('package')
        metadata = ET.SubElement(package, 'metadata')
        ET.SubElement(metadata, 'id').text = package_id
        ET.SubElement(metadata, 'version').text = version
        ET.SubElement(metadata, 'repository', type='git', commit=source)
        dependencies = ET.SubElement(metadata, 'dependencies')
        if include_dependency:
            dependency = 'Hexalith.EventStore.Contracts' if package_id.endswith('.Contracts') else 'Hexalith.EventStore.Server'
            ET.SubElement(dependencies, 'dependency', id=dependency, version=sdk)
        target = root / (package_id + '.' + version + '.nupkg')
        with zipfile.ZipFile(target, 'w') as archive:
            archive.writestr(package_id + '.nuspec', ET.tostring(package))
            archive.writestr('lib/net10.0/' + package_id + '.dll', b'not executable')

    def test_wrong_source_refuses_before_restore(self):
        self.reject(server={'source': 'b' * 40}, message='reviewed source')

    def test_mixed_package_version_refuses_before_restore(self):
        self.reject(server={'version': '6.0.1'}, message='versions disagree')

    def test_unknown_or_prerelease_sdk_refuses_before_restore(self):
        for sdk in ('3.128.4', '3.129.0-preview.1', '3.129.2', '3.130.3', '3.131.1', '3.132.1', '3.133.0',
                    '3.132.0-preview.1', '3.132.0-preview.1+metadata', '3.132.0.1', '3.132.0+',
                    '3.132.0+invalid..metadata',
                    '3.131.0-preview.1', '3.131.0-preview.1+metadata', '3.131.0.1', '3.131.0+',
                    '3.131.0+invalid..metadata'):
            with self.subTest(sdk=sdk):
                self.reject(server={'sdk': sdk}, message='qualified stable cohort')

    def test_every_qualified_sdk_and_alias_reaches_only_its_normalized_restore(self):
        for sdk in ('3.129.0', '3.129.1', '3.130.0', '3.130.1', '3.130.2', '3.131.0', '3.132.0'):
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

    def test_mixed_sdk_cohort_refuses_before_restore(self):
        for sdk in ('3.129.1', '3.130.2', '3.131.0', '3.132.0'):
            with self.subTest(sdk=sdk):
                self.reject(server={'sdk': sdk}, message='qualified stable cohort')

    def reject(self, server, message):
        with tempfile.TemporaryDirectory(prefix='tenants-catalog-metadata-') as temporary:
            root = Path(temporary)
            self.write_package(root, 'Hexalith.Tenants.Contracts')
            self.write_package(root, 'Hexalith.Tenants.Server', **server)
            with patch.object(CONSUMER.subprocess, 'run', side_effect=AssertionError('Invalid metadata reached execution')) as run:
                with self.assertRaisesRegex(ValueError, message):
                    CONSUMER.verify(root, 'a' * 40)
                run.assert_not_called()


if __name__ == '__main__':
    unittest.main()
