"""Exercise the ordinary pack lifecycle against real stale and qualified DLLs."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import unittest

ROOT = Path(__file__).resolve().parents[2]


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
            for package in ('Contracts', 'Server', 'Client', 'Testing', 'Aspire'):
                source = ROOT / ('src/Hexalith.Tenants.' + package)
                shutil.copytree(source, fixture / source.relative_to(ROOT), ignore=shutil.ignore_patterns('bin', 'obj'))
            shutil.copytree(ROOT / 'tools/CatalogArtifactVerifier', fixture / 'tools/CatalogArtifactVerifier',
                            ignore=shutil.ignore_patterns('bin', 'obj'))
            environment = os.environ.copy()
            for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
                        'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH'):
                environment.pop(key, None)
            common = ['-c', 'Release', '-m:1', '-nr:false', '-p:UseHexalithProjectReferences=false', '-p:UseNuGetDeps=true']
            for package in ('Server', 'Client', 'Testing', 'Aspire'):
                built = subprocess.run(['dotnet', 'build', f'src/Hexalith.Tenants.{package}/Hexalith.Tenants.{package}.csproj',
                                        *common, '-p:HexalithEventStoreVersion=3.128.4', '-warnaserror'],
                                       cwd=fixture, env=environment, capture_output=True, text=True, timeout=120)
                self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
            for package in ('Contracts', 'Server', 'Client', 'Testing', 'Aspire'):
                with self.subTest(legacy_package=package):
                    output = parent / ('legacy-rejected-' + package)
                    packed = subprocess.run(['dotnet', 'pack', f'src/Hexalith.Tenants.{package}/Hexalith.Tenants.{package}.csproj',
                                             '--no-build', '--no-restore', *common, '-p:HexalithEventStoreVersion=3.128.4',
                                             '--output', str(output)], cwd=fixture, env=environment,
                                            capture_output=True, text=True, timeout=120)
                    self.assertNotEqual(packed.returncode, 0, packed.stdout + packed.stderr)
                    self.assertIn('Published Tenants packages require', packed.stdout + packed.stderr)
                    self.assertFalse(list(output.glob('*.nupkg')))
            for package in ('Contracts', 'Server'):
                with self.subTest(package=package):
                    output = parent / ('rejected-' + package)
                    packed = subprocess.run(['dotnet', 'pack', f'src/Hexalith.Tenants.{package}/Hexalith.Tenants.{package}.csproj',
                                             '--no-build', '--no-restore', *common, '-p:HexalithEventStoreVersion=3.129.0',
                                             '--output', str(output)], cwd=fixture, env=environment,
                                            capture_output=True, text=True, timeout=120)
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
