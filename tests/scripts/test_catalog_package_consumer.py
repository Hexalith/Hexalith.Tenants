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
                      mcp='3.0.0', include_mcp=True, framework='net11.0'):
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

    def test_current_and_retained_consumers_select_distinct_inventories_and_dependencies(self):
        profiles = (
            ('net11.0', '5.6.0', '3.0.0', 'approved-route-ids.txt', 25),
            ('net10.0', '4.0.4', '2.0.0', 'approved-route-ids-net10.txt', 23),
            ('net10.0', '4.1.0', '2.0.0', 'approved-route-ids-net10.txt', 23),
            ('net10.0', '4.2.0', '2.0.1', 'approved-route-ids-net10.txt', 23),
        )
        for framework, sdk, mcp, manifest, route_count in profiles:
            with self.subTest(framework=framework, sdk=sdk), tempfile.TemporaryDirectory(prefix='tenants-catalog-framework-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk=sdk, mcp=mcp, framework=framework)

                def inspect_restore(arguments, **kwargs):
                    project = ET.parse(arguments[2])
                    self.assertEqual(project.findtext('.//TargetFramework'), framework)
                    for package_id, version in (('Hexalith.EventStore.DomainService', sdk), ('Hexalith.McpCli.Abstractions', mcp)):
                        reference = project.find(f'.//PackageReference[@Include="{package_id}"]')
                        self.assertEqual(reference.get('Version'), f'[{version}]')
                    consumer = kwargs['cwd']
                    self.assertEqual([path.name for path in consumer.glob('approved-route-ids*.txt')], [manifest])
                    route_ids = (consumer / manifest).read_text().splitlines()
                    self.assertEqual(len(route_ids), route_count)
                    self.assertEqual(len(set(route_ids)), route_count)
                    for later_route in ('tenants-refresh-tenant-access', 'tenants-create-synthetic-tenant'):
                        self.assertEqual(later_route in route_ids, framework == 'net11.0')
                    raise InterruptedError('Verified catalog profile before restore')

                with patch.object(CONSUMER.subprocess, 'run', side_effect=inspect_restore) as run:
                    with self.assertRaisesRegex(InterruptedError, 'Verified catalog profile before restore'):
                        options = {} if framework == 'net11.0' else {'target_framework': framework}
                        CONSUMER.verify(root, 'a' * 40, **options)
                    run.assert_called_once()
        with self.assertRaisesRegex(ValueError, 'framework is not qualified'):
            CONSUMER.verify(Path('/unused'), target_framework='net12.0')

    def test_retained_consumer_rejects_incompatible_producer_pairs_before_restore(self):
        for sdk, mcp, diagnostic in (
                ('4.2.0', '2.0.0', 'qualified EventStore and McpCli dependency pair'),
                ('4.1.0', '2.0.1', 'qualified EventStore and McpCli dependency pair'),
                ('5.6.0', '2.0.1', 'qualified EventStore and McpCli dependency pair'),
                ('5.6.0', '3.0.0', 'coordinated McpCli 2.0.0 or 2.0.1'),
                ('3.133.0', '1.1.0', 'coordinated McpCli 2.0.0 or 2.0.1')):
            with self.subTest(sdk=sdk, mcp=mcp), tempfile.TemporaryDirectory(prefix='tenants-catalog-framework-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk=sdk, mcp=mcp, framework='net10.0')
                with patch.object(CONSUMER.subprocess, 'run') as run:
                    with self.assertRaisesRegex(ValueError, diagnostic):
                        CONSUMER.verify(root, 'a' * 40, target_framework='net10.0')
                    run.assert_not_called()

    def test_consumer_workspace_pins_sdk_over_an_ancestor_global_json(self):
        with tempfile.TemporaryDirectory(prefix='tenants-catalog-sdk-') as temporary:
            root = Path(temporary)
            for package_id in CONSUMER.IDS:
                self.write_package(root, package_id)
            ancestor_pin = '{"sdk":{"version":"99.0.100","rollForward":"disable"}}'
            (root / 'global.json').write_text(ancestor_pin)

            def inspect_restore(arguments, **kwargs):
                consumer = kwargs['cwd']
                self.assertEqual(consumer.parent, root)
                self.assertEqual((consumer / 'global.json').read_bytes(), (ROOT / 'global.json').read_bytes())
                self.assertEqual((root / 'global.json').read_text(), ancestor_pin)
                raise InterruptedError('Verified SDK pin before restore')

            with patch.object(CONSUMER.tempfile, 'tempdir', str(root)):
                with patch.object(CONSUMER.subprocess, 'run', side_effect=inspect_restore) as run:
                    with self.assertRaisesRegex(InterruptedError, 'Verified SDK pin before restore'):
                        CONSUMER.verify(root, 'a' * 40)
                    run.assert_called_once()

    def test_wrong_source_refuses_before_restore(self):
        self.reject(server={'source': 'b' * 40}, message='reviewed source')

    def test_mixed_package_version_refuses_before_restore(self):
        self.reject(server={'version': '7.0.1'}, message='versions disagree')

    def test_unknown_or_prerelease_sdk_refuses_before_restore(self):
        for sdk in ('3.128.4', '3.129.0-preview.1', '3.129.2', '3.130.3', '3.131.1', '3.132.3', '3.133.1', '4.0.5', '4.1.1', '4.2.1', '4.3.0', '5.0.0', '5.0.2', '5.1.2', '5.1.1-preview.1', '5.1.1.1', '5.1.1+', '5.1.1+invalid..metadata', '5.2.1', '5.3.2', '5.3.1-preview.1', '5.4.1', '5.4.0-preview.1', '5.4.0.1', '5.5.3', '5.5.2-preview.1', '5.5.2.1', '5.5.1-preview.1', '5.5.1.1', '5.6.1', '5.6.0-preview.1', '5.6.0.1', '5.1.0-preview.1', '5.1.0.1', '5.1.0+', '5.1.0+invalid..metadata', '5.0.1-preview.1', '5.0.1.1', '5.0.1+', '5.0.1+invalid..metadata', '4.2.0-preview.1', '4.2.0.1', '4.2.0+', '4.2.0+invalid..metadata', '4.1.0-preview.1', '4.1.0.1', '4.1.0+', '4.1.0+invalid..metadata', '4.0.4-preview.1', '4.0.4.1', '4.0.3-preview.1', '4.0.0-preview.1',
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
        for sdk in ('3.129.0', '3.129.1', '3.130.0', '3.130.1', '3.130.2', '3.131.0', '3.132.0', '3.132.1', '3.132.2', '3.133.0', '4.0.0', '4.0.1', '4.0.2', '4.0.3', '4.0.4', '4.1.0', '4.2.0', '5.0.1', '5.1.0', '5.1.1', '5.2.0', '5.3.0', '5.3.1', '5.4.0', '5.5.1', '5.5.2', '5.6.0'):
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
        self.reject(server={'include_mcp': False}, message='coordinated McpCli 3.0.0')
        for mcp in ('1.1.0', '2.0.0', '2.0.1', '2.0.2', '3.0.1', '3.0.0-preview.1'):
            with self.subTest(mcp=mcp):
                self.reject(server={'mcp': mcp}, message='coordinated McpCli 3.0.0')

    def test_refreshed_mcpcli_cohort_reaches_restore(self):
        for mcp in ('3.0.0',):
            for dependency_range in (mcp, '[' + mcp + ']', '[' + mcp + ', )', '[' + mcp + ', 4.0.0)'):
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
        for dependency_range in ('(3.0.0, )', '(2.0.0, 3.0.0]', '(, 3.0.0]',
                                 '[3.0.0, 2.0.0)', '[3.0.0, 3.0.0)', '[3.0.0, )garbage',
                                 '[3.0.0, invalid)', '[3.0.0, 4.0.0', '[3.0.0, ]',
                                 '[3.0.0, 4.0.0, 5.0.0)', '[3.0.0-preview.1, )', '[3.0.1, )'):
            with self.subTest(dependency_range=dependency_range):
                self.reject(server={'mcp': dependency_range}, message='McpCli')

    def test_exclusive_or_malformed_sdk_ranges_refuse_before_restore(self):
        for dependency_range in ('(5.0.1,5.0.2]', '(5.0.1,)', '(,5.0.1]',
                                 '[5.0.1,5.0.0)', '[5.0.1,5.0.1)', '[5.0.1,invalid)',
                                 '[5.0.1,5.0.2)garbage', '[5.0.1,5.0.2', '[5.0.1,]',
                                 '[5.0.1,5.0.2,5.0.3)', '[5.0.1-preview.1,)'):
            with self.subTest(dependency_range=dependency_range):
                self.reject(contracts={'sdk': '5.0.1'}, server={'sdk': dependency_range},
                            message='qualified stable cohort')

    def test_public_restored_sdk_must_match_the_admitted_package_before_build(self):
        for restored_version, kind in (('5.0.1', 'package'), ('5.0.2', 'package'), ('5.0.1', 'project')):
            with self.subTest(restored_version=restored_version, kind=kind), tempfile.TemporaryDirectory(prefix='tenants-catalog-assets-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='[5.0.1,5.0.2]')

                def restore(arguments, **kwargs):
                    if arguments[:2] == ['dotnet', 'build']:
                        raise InterruptedError('Verified build boundary')
                    self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                    consumer_root = Path(arguments[2]).parent
                    (consumer_root / 'obj').mkdir()
                    libraries = {'Hexalith.McpCli.Abstractions/3.0.0': {'type': 'package'},
                                 'Hexalith.EventStore.Contracts/' + restored_version: {'type': kind},
                                 'Hexalith.EventStore.ServiceDefaults/5.0.1': {'type': 'package'}}
                    for package_id in CONSUMER.IDS:
                        libraries[package_id + '/7.0.0'] = {'type': 'package'}
                        destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                    (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                    if restored_version == '5.0.1' and kind == 'package':
                        with self.assertRaisesRegex(InterruptedError, 'Verified build boundary'):
                            CONSUMER.verify(root, 'a' * 40)
                        self.assertEqual(run.call_count, 2)
                    else:
                        with self.assertRaisesRegex(ValueError, 'Restored EventStore dependency differs'):
                            CONSUMER.verify(root, 'a' * 40)
                        run.assert_called_once()

    def test_restored_mcpcli_must_match_the_admitted_package_before_build(self):
        for mcp in ('3.0.0',):
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
        self.reject(server={'mcp': '2.0.1'}, message='coordinated McpCli 3.0.0')
        self.reject(contracts={'mcp': '3.0.0'}, server={'mcp': '2.0.1'}, message='coordinated McpCli 3.0.0')

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
        self.reject(contracts={'sdk': '5.0.1'}, server={'sdk': '4.2.0'}, message='qualified stable cohort')
        self.reject(contracts={'sdk': '4.2.0'}, server={'sdk': '5.0.1'}, message='qualified stable cohort')

    def write_local_candidate(self, root, package_id, version, filename=None, content=b'local candidate payload'):
        package = ET.Element('package')
        metadata = ET.SubElement(package, 'metadata')
        ET.SubElement(metadata, 'id').text = package_id
        ET.SubElement(metadata, 'version').text = version
        target = root / (filename or (package_id + '.' + version + '.nupkg'))
        with zipfile.ZipFile(target, 'w') as archive:
            archive.writestr(package_id + '.nuspec', ET.tostring(package))
            archive.writestr('lib/net11.0/' + package_id + '.dll', content)
        return target

    def test_lowercased_local_candidates_emit_canonical_source_mapping_and_bind_bytes(self):
        for restored_eventstore_id, restored_mcp_id in (
                ('hexalith.eventstore.contracts', 'hexalith.mcpcli.abstractions'),
                ('Hexalith.EventStore.Contracts', 'Hexalith.McpCli.Abstractions')):
            with self.subTest(eventstore_id=restored_eventstore_id, mcp_id=restored_mcp_id), \
                    tempfile.TemporaryDirectory(prefix='tenants-catalog-candidate-casing-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
                sdk_archive = self.write_local_candidate(root, 'hexalith.eventstore.contracts', '5.6.0', content=b'sdk bytes')
                mcp_archive = self.write_local_candidate(root, 'hexalith.mcpcli.abstractions', '3.0.0', content=b'mcp bytes')

                def restore(arguments, **kwargs):
                    if arguments[:2] == ['dotnet', 'build']:
                        raise InterruptedError('Verified build boundary')
                    self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                    config = ET.parse(arguments[arguments.index('--configfile') + 1])
                    packed_patterns = {
                        item.get('pattern')
                        for item in config.findall('.//packageSourceMapping/packageSource[@key="packed"]/package')
                    }
                    self.assertEqual(
                        packed_patterns,
                        {'Hexalith.Tenants.Contracts', 'Hexalith.Tenants.Server', 'Hexalith.EventStore.*', 'Hexalith.McpCli.*'},
                    )
                    consumer_root = Path(arguments[2]).parent
                    (consumer_root / 'obj').mkdir()
                    libraries = {
                        restored_eventstore_id + '/5.6.0': {'type': 'package'},
                        restored_mcp_id + '/3.0.0': {'type': 'package'},
                    }
                    for package_id in CONSUMER.IDS:
                        libraries[package_id + '/7.0.0'] = {'type': 'package'}
                        destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                    for restored_id, version, source_archive in (
                            (restored_eventstore_id, '5.6.0', sdk_archive),
                            (restored_mcp_id, '3.0.0', mcp_archive)):
                        destination = (consumer_root / 'packages' / restored_id.lower() / version.lower()
                                       / (restored_id.lower() + '.' + version.lower() + '.nupkg'))
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(source_archive, destination)
                    (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                    with self.assertRaisesRegex(InterruptedError, 'Verified build boundary'):
                        CONSUMER.verify(root, 'a' * 40)
                    self.assertEqual(run.call_count, 2)

    def test_lowercased_local_candidate_tampered_bytes_rejected_before_build(self):
        for tampered_target in ('eventstore', 'mcpcli'):
            with self.subTest(tampered_target=tampered_target), \
                    tempfile.TemporaryDirectory(prefix='tenants-catalog-candidate-tamper-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
                sdk_archive = self.write_local_candidate(root, 'hexalith.eventstore.contracts', '5.6.0', content=b'expected sdk')
                mcp_archive = self.write_local_candidate(root, 'hexalith.mcpcli.abstractions', '3.0.0', content=b'expected mcp')

                def restore(arguments, **kwargs):
                    self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                    consumer_root = Path(arguments[2]).parent
                    (consumer_root / 'obj').mkdir()
                    libraries = {
                        'hexalith.eventstore.contracts/5.6.0': {'type': 'package'},
                        'hexalith.mcpcli.abstractions/3.0.0': {'type': 'package'},
                    }
                    for package_id in CONSUMER.IDS:
                        libraries[package_id + '/7.0.0'] = {'type': 'package'}
                        destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                    for key_name, restored_id, version, source_archive in (
                            ('eventstore', 'hexalith.eventstore.contracts', '5.6.0', sdk_archive),
                            ('mcpcli', 'hexalith.mcpcli.abstractions', '3.0.0', mcp_archive)):
                        destination = (consumer_root / 'packages' / restored_id.lower() / version.lower()
                                       / (restored_id.lower() + '.' + version.lower() + '.nupkg'))
                        destination.parent.mkdir(parents=True)
                        if key_name == tampered_target:
                            destination.write_bytes(b'tampered archive bytes')
                        else:
                            shutil.copyfile(source_archive, destination)
                    (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                    with self.assertRaisesRegex(ValueError, 'Restored dependency differs from the local candidate bytes'):
                        CONSUMER.verify(root, 'a' * 40)
                    run.assert_called_once()

    def test_mixed_case_duplicate_local_candidates_rejected_before_restore(self):
        for canonical_id, lowercased_id, version in (
                ('Hexalith.EventStore.Contracts', 'hexalith.eventstore.contracts', '5.6.0'),
                ('Hexalith.McpCli.Abstractions', 'hexalith.mcpcli.abstractions', '3.0.0')):
            with self.subTest(canonical_id=canonical_id), \
                    tempfile.TemporaryDirectory(prefix='tenants-catalog-candidate-dup-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
                self.write_local_candidate(root, canonical_id, version, filename='candidate-canonical.nupkg')
                self.write_local_candidate(root, lowercased_id, version, filename='candidate-lowercased.nupkg')
                with patch.object(CONSUMER.subprocess, 'run', side_effect=AssertionError('Duplicate candidate reached restore')) as run:
                    with self.assertRaisesRegex(ValueError, 'Duplicate local dependency candidate'):
                        CONSUMER.verify(root, 'a' * 40)
                    run.assert_not_called()

    def test_lowercased_wrong_cohort_local_candidates_rejected_before_restore(self):
        for candidate_id, wrong_version, message in (
                ('hexalith.eventstore.contracts', '5.5.2', 'Local SDK candidate differs from the catalog dependency cohort'),
                ('hexalith.mcpcli.abstractions', '2.0.2', 'Local McpCli candidate differs from the catalog dependency cohort')):
            with self.subTest(candidate_id=candidate_id, wrong_version=wrong_version), \
                    tempfile.TemporaryDirectory(prefix='tenants-catalog-candidate-cohort-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
                self.write_local_candidate(root, candidate_id, wrong_version)
                with patch.object(CONSUMER.subprocess, 'run', side_effect=AssertionError('Wrong-cohort candidate reached restore')) as run:
                    with self.assertRaisesRegex(ValueError, message):
                        CONSUMER.verify(root, 'a' * 40)
                    run.assert_not_called()

    def test_missing_local_candidate_for_restored_package_rejected_before_build(self):
        for present_id, present_version, extra_restored_key in (
                ('hexalith.eventstore.contracts', '5.6.0', 'hexalith.eventstore.domainservice/5.6.0'),
                ('hexalith.mcpcli.abstractions', '3.0.0', 'hexalith.mcpcli.extra/3.0.0')):
            with self.subTest(extra_restored_key=extra_restored_key), \
                    tempfile.TemporaryDirectory(prefix='tenants-catalog-candidate-missing-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
                present_archive = self.write_local_candidate(root, present_id, present_version)

                def restore(arguments, **kwargs):
                    self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                    consumer_root = Path(arguments[2]).parent
                    (consumer_root / 'obj').mkdir()
                    libraries = {
                        'Hexalith.McpCli.Abstractions/3.0.0': {'type': 'package'},
                        present_id + '/' + present_version: {'type': 'package'},
                        extra_restored_key: {'type': 'package'},
                    }
                    if present_id == 'hexalith.mcpcli.abstractions':
                        libraries.pop('Hexalith.McpCli.Abstractions/3.0.0')
                    for package_id in CONSUMER.IDS:
                        libraries[package_id + '/7.0.0'] = {'type': 'package'}
                        destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                    destination = (consumer_root / 'packages' / present_id.lower() / present_version.lower()
                                   / (present_id.lower() + '.' + present_version.lower() + '.nupkg'))
                    destination.parent.mkdir(parents=True)
                    shutil.copyfile(present_archive, destination)
                    (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                    with self.assertRaisesRegex(ValueError, 'local dependency candidate was missing'):
                        CONSUMER.verify(root, 'a' * 40)
                    run.assert_called_once()

    def test_restored_mcpcli_without_local_candidates_checks_casing_and_other_mcpcli_packages(self):
        cases = (
            ('lowercased_abstractions_passes', {'hexalith.mcpcli.abstractions/3.0.0': {'type': 'package'}}, True),
            ('wrong_version_extra_rejected', {
                'hexalith.mcpcli.abstractions/3.0.0': {'type': 'package'},
                'hexalith.mcpcli.extra/2.0.2': {'type': 'package'},
            }, False),
            ('project_extra_rejected', {
                'hexalith.mcpcli.abstractions/3.0.0': {'type': 'package'},
                'hexalith.mcpcli.extra/3.0.0': {'type': 'project'},
            }, False),
        )
        for label, mcp_libraries, accepted in cases:
            with self.subTest(case=label), tempfile.TemporaryDirectory(prefix='tenants-catalog-mcp-public-') as temporary:
                root = Path(temporary)
                for package_id in CONSUMER.IDS:
                    self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')

                def restore(arguments, **kwargs):
                    if arguments[:2] == ['dotnet', 'build']:
                        raise InterruptedError('Verified build boundary')
                    self.assertEqual(arguments[:2], ['dotnet', 'restore'])
                    consumer_root = Path(arguments[2]).parent
                    (consumer_root / 'obj').mkdir()
                    libraries = dict(mcp_libraries)
                    for package_id in CONSUMER.IDS:
                        libraries[package_id + '/7.0.0'] = {'type': 'package'}
                        destination = consumer_root / 'packages' / package_id.lower() / '7.0.0' / (package_id.lower() + '.7.0.0.nupkg')
                        destination.parent.mkdir(parents=True)
                        shutil.copyfile(root / (package_id + '.7.0.0.nupkg'), destination)
                    (consumer_root / 'obj/project.assets.json').write_text(json.dumps({'libraries': libraries}))

                with patch.object(CONSUMER.subprocess, 'run', side_effect=restore) as run:
                    if accepted:
                        with self.assertRaisesRegex(InterruptedError, 'Verified build boundary'):
                            CONSUMER.verify(root, 'a' * 40)
                        self.assertEqual(run.call_count, 2)
                    else:
                        with self.assertRaisesRegex(ValueError, 'Restored McpCli dependency differs'):
                            CONSUMER.verify(root, 'a' * 40)
                        run.assert_called_once()

        with tempfile.TemporaryDirectory(prefix='tenants-catalog-malformed-candidate-') as temporary:
            root = Path(temporary)
            for package_id in CONSUMER.IDS:
                self.write_package(root, package_id, sdk='5.6.0', mcp='3.0.0')
            with zipfile.ZipFile(root / 'malformed.nupkg', 'w') as archive:
                archive.writestr('malformed.nuspec', b'<package />')
            with patch.object(CONSUMER.subprocess, 'run', side_effect=AssertionError('Malformed candidate reached restore')) as run:
                with self.assertRaisesRegex(ValueError, 'has no package metadata'):
                    CONSUMER.verify(root, 'a' * 40)
                run.assert_not_called()

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
