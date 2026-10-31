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
        for sdk in ('3.129.0-preview.1', '3.129.2', '3.130.0'):
            with self.subTest(sdk=sdk):
                self.reject(server={'sdk': sdk}, message='qualified stable cohort')

    def test_missing_sdk_dependency_refuses_before_restore(self):
        self.reject(server={'include_dependency': False}, message='required SDK dependency')

    def test_mixed_sdk_cohort_refuses_before_restore(self):
        self.reject(server={'sdk': '3.129.1'}, message='qualified stable cohort')

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
