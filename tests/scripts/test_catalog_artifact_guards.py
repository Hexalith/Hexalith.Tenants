"""Exercise the ordinary pack lifecycle against real stale and qualified DLLs."""

import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[2]


class CatalogArtifactPackTests(unittest.TestCase):
    def test_normal_pack_no_build_refuses_legacy_contracts_and_server_bytes(self):
        builds = next(path for path in (ROOT / 'references/Hexalith.Builds', ROOT.parent / 'Hexalith.Builds')
                      if (path / 'Props/Directory.Packages.props').is_file())
        with tempfile.TemporaryDirectory(prefix='tenants-real-pack-guard-') as temporary:
            parent = Path(temporary)
            fixture = parent / 'Hexalith.Tenants'
            fixture.mkdir()
            shutil.copytree(builds / 'Props', parent / 'Hexalith.Builds/Props')
            for name in ('Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'README.md', 'global.json'):
                shutil.copyfile(ROOT / name, fixture / name)
            for package in ('Contracts', 'Server'):
                source = ROOT / ('src/Hexalith.Tenants.' + package)
                shutil.copytree(source, fixture / source.relative_to(ROOT), ignore=shutil.ignore_patterns('bin', 'obj'))
            shutil.copytree(ROOT / 'tools/CatalogArtifactVerifier', fixture / 'tools/CatalogArtifactVerifier',
                            ignore=shutil.ignore_patterns('bin', 'obj'))
            environment = os.environ.copy()
            for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
                        'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH'):
                environment.pop(key, None)
            project = 'src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj'
            common = ['-c', 'Release', '-m:1', '-nr:false', '-p:UseHexalithProjectReferences=false', '-p:UseNuGetDeps=true']
            built = subprocess.run(['dotnet', 'build', project, *common, '-p:HexalithEventStoreVersion=3.128.4', '-warnaserror'],
                                   cwd=fixture, env=environment, capture_output=True, text=True, timeout=120)
            self.assertEqual(built.returncode, 0, built.stdout + built.stderr)
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


if __name__ == '__main__':
    unittest.main()
