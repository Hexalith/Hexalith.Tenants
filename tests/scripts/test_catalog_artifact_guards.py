"""Exercise the ordinary pack lifecycle against real stale and qualified DLLs."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import unittest
import zipfile

ROOT = Path(__file__).resolve().parents[2]
PACKAGES = ('Contracts', 'Server', 'Client', 'Testing', 'Aspire')


class CatalogArtifactPackTests(unittest.TestCase):
    def test_normal_pack_lifecycle_preserves_owner_legacy_and_stale_byte_refusals(self):
        builds = next(path for path in (ROOT / 'references/Hexalith.Builds', ROOT.parent / 'Hexalith.Builds')
                      if (path / 'Props/Directory.Packages.props').is_file())
        with tempfile.TemporaryDirectory(prefix='tenants-real-pack-guard-') as temporary:
            parent = Path(temporary)
            fixture = parent / 'Hexalith.Tenants'
            fixture.mkdir()
            shutil.copytree(builds / 'Props', parent / 'Hexalith.Builds/Props')
            for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'README.md', 'global.json'):
                shutil.copyfile(ROOT / name, fixture / name)
            shutil.copytree(ROOT / 'tools/CatalogArtifactVerifier', fixture / 'tools/CatalogArtifactVerifier',
                            ignore=shutil.ignore_patterns('bin', 'obj'))

            # These compiled test fixtures exercise the pack guards without compiling
            # today's canonical Contracts sources against an obsolete producer API.
            # The synthetic external attribute assembly supplies only metadata shapes;
            # it is neither a producer package nor evidence of SDK qualification.
            attributes = fixture / 'references/CatalogAttributeFixture'
            attributes.mkdir(parents=True)
            (attributes / 'CatalogAttributeFixture.csproj').write_text(
                '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><IsPackable>false</IsPackable>'
                '</PropertyGroup></Project>')
            for namespace, name in (('Hexalith.EventStore.Contracts.Routing', 'RouteCatalogDeclarationSourceAttribute'),
                                    ('Hexalith.EventStore.DomainService', 'RouteCatalogAdapterSourceAttribute')):
                (attributes / (name + '.cs')).write_text(
                    f'namespace {namespace};\n[AttributeUsage(AttributeTargets.Assembly)]\n'
                    f'public sealed class {name} : Attribute\n{{\n    public {name}(Type source) {{ }}\n}}\n')
            solution = ET.Element('Solution')
            for package in PACKAGES:
                directory = fixture / ('src/Hexalith.Tenants.' + package)
                directory.mkdir(parents=True)
                project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
                references = ET.SubElement(project, 'ItemGroup')
                ET.SubElement(references, 'ProjectReference', Include='../../references/CatalogAttributeFixture/CatalogAttributeFixture.csproj')
                dependencies = ('Server',) if package == 'Testing' else ('Contracts',) if package in ('Server', 'Client') else ()
                for dependency in dependencies:
                    ET.SubElement(references, 'ProjectReference',
                                  Include=f'../Hexalith.Tenants.{dependency}/Hexalith.Tenants.{dependency}.csproj')
                project_path = directory / ('Hexalith.Tenants.' + package + '.csproj')
                ET.ElementTree(project).write(project_path, encoding='unicode')
                (directory / 'FixtureMarker.cs').write_text(f'namespace Hexalith.Tenants.{package};\npublic sealed class FixtureMarker;\n')
                ET.SubElement(solution, 'Project', Path=str(project_path.relative_to(fixture)))
            ET.ElementTree(solution).write(fixture / 'PackFixtures.slnx', encoding='unicode')

            environment = os.environ.copy()
            for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
                        'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH'):
                environment.pop(key, None)
            common = ['-c', 'Release', '-m:1', '-nr:false', '-p:UseHexalithProjectReferences=false', '-p:UseNuGetDeps=true']

            def run(*arguments):
                return subprocess.run(['dotnet', *arguments], cwd=fixture, env=environment,
                                      capture_output=True, text=True, timeout=120)

            def project(package):
                return f'src/Hexalith.Tenants.{package}/Hexalith.Tenants.{package}.csproj'

            def binary(package, assembly=None):
                return fixture / f'src/Hexalith.Tenants.{package}/bin/Release/net10.0/Hexalith.Tenants.{assembly or package}.dll'

            built = run('build', 'PackFixtures.slnx', *common, '-warnaserror')
            self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
            stale = {package: binary(package).read_bytes() for package in ('Contracts', 'Server')}
            for property_name, version, diagnostic in (
                    ('HexalithEventStoreVersion', '3.128.4', 'Canonical Tenants packages require EventStore 4.0.3'),
                    ('HexalithMcpCliVersion', '1.1.0', 'Canonical Tenants packages require McpCli.Abstractions 2.0.0')):
                with self.subTest(canonical_guard=property_name):
                    rejected = run('build', project('Contracts'), '--no-restore', *common,
                                   f'-p:{property_name}={version}')
                    self.assertNotEqual(rejected.returncode, 0, rejected.stdout + rejected.stderr)
                    self.assertIn(diagnostic, rejected.stdout + rejected.stderr)
            for package in PACKAGES:
                with self.subTest(legacy_package=package):
                    output = parent / ('legacy-rejected-' + package)
                    packed = run('pack', project(package), '--no-build', '--no-restore', *common,
                                 '-p:HexalithEventStoreVersion=3.128.4', '--output', str(output))
                    self.assertNotEqual(packed.returncode, 0, packed.stdout + packed.stderr)
                    self.assertIn('Canonical Tenants packages require EventStore 4.0.3', packed.stdout + packed.stderr)
                    self.assertFalse(list(output.glob('*.nupkg')))

            for package, namespace, attribute, declaration, members in (
                    ('Contracts', 'Hexalith.EventStore.Contracts.Routing', 'RouteCatalogDeclarationSource',
                     'TenantCatalogDeclarations', 'public static object[] Operations { get; } = [];'),
                    ('Server', 'Hexalith.EventStore.DomainService', 'RouteCatalogAdapterSource', 'TenantIntentAdapters', '')):
                (fixture / f'src/Hexalith.Tenants.{package}/{declaration}.cs').write_text(
                    f'[assembly: {namespace}.{attribute}(typeof(Hexalith.Tenants.{package}.Routing.{declaration}))]\n'
                    f'namespace Hexalith.Tenants.{package}.Routing;\npublic static class {declaration} {{ {members} }}\n')
            built = run('build', 'PackFixtures.slnx', *common, '-warnaserror')
            self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
            for package in PACKAGES:
                with self.subTest(qualified_package=package):
                    output = parent / ('qualified-' + package)
                    packed = run('pack', project(package), '--no-build', '--no-restore', *common, '--output', str(output))
                    self.assertEqual(packed.returncode, 0, packed.stdout + packed.stderr)
                    self.assertIn('Actual pack-target catalog enrollment verified: Hexalith.Tenants.' + package,
                                  packed.stdout + packed.stderr)
                    archives = list(output.glob('*.nupkg'))
                    self.assertEqual(len(archives), 1)
                    with zipfile.ZipFile(archives[0]) as archive:
                        self.assertEqual(archive.read(f'lib/net10.0/Hexalith.Tenants.{package}.dll'), binary(package).read_bytes())
            for package, assembly in (('Contracts', 'Contracts'), ('Server', 'Server'), ('Server', 'Contracts'),
                                      ('Client', 'Contracts'), ('Testing', 'Contracts'), ('Testing', 'Server')):
                with self.subTest(package=package, stale_assembly=assembly):
                    target = binary(package, assembly)
                    qualified = target.read_bytes()
                    target.write_bytes(stale[assembly])
                    output = parent / ('rejected-' + package + '-' + assembly)
                    try:
                        packed = run('pack', project(package), '--no-build', '--no-restore', *common, '--output', str(output))
                    finally:
                        target.write_bytes(qualified)
                    self.assertNotEqual(packed.returncode, 0, packed.stdout + packed.stderr)
                    self.assertIn('Catalog artifact publication refused', packed.stdout + packed.stderr)
                    self.assertFalse(list(output.glob('*.nupkg')), 'A rejected stale DLL must not produce any archive.')


    def test_solution_build_allows_foreign_generated_packages_with_their_own_sdk(self):
        builds = next(path for path in (ROOT / 'references/Hexalith.Builds', ROOT.parent / 'Hexalith.Builds')
                      if (path / 'Props/Directory.Packages.props').is_file())
        with tempfile.TemporaryDirectory(prefix='tenants-foreign-pack-boundary-') as temporary:
            parent = Path(temporary)
            fixture = parent / 'Hexalith.Tenants'
            fixture.mkdir()
            shutil.copytree(builds / 'Props', parent / 'Hexalith.Builds/Props')
            for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props',
                         'Directory.Solution.targets', 'README.md', 'global.json'):
                shutil.copyfile(ROOT / name, fixture / name)
            solution = ET.Element('Solution')
            for package in ('Aspire', 'ServiceDefaults', 'UniqueIds'):
                directory = fixture / ('references/Foreign/Hexalith.Commons.' + package)
                directory.mkdir(parents=True)
                project = directory / ('Hexalith.Commons.' + package + '.csproj')
                project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>'
                                   '<HexalithEventStoreVersion>3.128.4</HexalithEventStoreVersion>'
                                   '<GeneratePackageOnBuild>true</GeneratePackageOnBuild>'
                                   '<Version>1.0.0</Version></PropertyGroup></Project>')
                ET.SubElement(solution, 'Project', Path=str(project.relative_to(fixture)))
            ET.ElementTree(solution).write(fixture / 'Foreign.slnx', encoding='unicode')
            built = subprocess.run(['dotnet', 'build', 'Foreign.slnx', '-c', 'Release', '-m:1', '-nr:false', '-warnaserror'],
                                   cwd=fixture, capture_output=True, text=True, timeout=120)
            self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
            for package in ('Aspire', 'ServiceDefaults', 'UniqueIds'):
                directory = fixture / ('references/Foreign/Hexalith.Commons.' + package)
                self.assertEqual(len(list((directory / 'bin/Release').glob('*.nupkg'))), 1)
                selected = subprocess.run(['dotnet', 'msbuild', str(directory / ('Hexalith.Commons.' + package + '.csproj')),
                                           '-nologo', '-getProperty:HexalithEventStoreVersion'], cwd=fixture,
                                          capture_output=True, text=True, timeout=30)
                self.assertEqual(selected.returncode, 0, selected.stdout + selected.stderr)
                self.assertEqual(selected.stdout.strip(), '3.128.4')


if __name__ == '__main__':
    unittest.main()
