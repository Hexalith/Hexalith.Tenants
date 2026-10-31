"""Evaluate the actual native host's opt-in package profile without restoring future artifacts."""

import json
import os
from pathlib import Path
import subprocess
import unittest

ROOT = Path(__file__).resolve().parents[2]
PROJECT = ROOT / 'src/Hexalith.Tenants/Hexalith.Tenants.csproj'
OWN = {'Hexalith.Tenants.Contracts', 'Hexalith.Tenants.Server'}


def evaluate(profile, source, publishing=False):
    environment = os.environ.copy()
    for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
                'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH'):
        environment.pop(key, None)
    command = ['dotnet', 'msbuild', str(PROJECT), '-nologo', '-getProperty:IsPackable',
               '-getItem:ProjectReference,PackageReference,PackageVersion', '-p:Configuration=Release',
               '-p:HexalithTenantsVersion=6.0.0', '-p:HexalithEventStoreVersion=3.129.0',
               '-p:UseHexalithProjectReferences=' + ('true' if source else 'false'),
               '-p:HexalithUsePublishedSdkForHostedTests=' + profile]
    if publishing:
        command.append('-p:_IsPublishing=true')
    result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, timeout=45)
    if result.returncode != 0:
        raise AssertionError(result.stdout + result.stderr)
    data = json.loads(result.stdout)
    projects = {Path(item['Identity'].replace('\\', '/')).stem for item in data['Items']['ProjectReference']}
    packages = {item['Identity']: item for item in data['Items']['PackageReference']}
    versions = {item['Identity']: item['Version'] for item in data['Items']['PackageVersion']}
    return data['Properties'], projects, packages, versions


class HostedPackageProfileTests(unittest.TestCase):
    def test_default_false_and_publishing_retain_native_owner_projects(self):
        for profile in ('', 'false'):
            for source in (False, True):
                for publishing in (False, True):
                    with self.subTest(profile=profile, source=source, publishing=publishing):
                        properties, projects, packages, _ = evaluate(profile, source, publishing)
                        self.assertEqual(properties['IsPackable'], 'false')
                        self.assertEqual(projects & OWN, OWN)
                        self.assertFalse(set(packages) & OWN)

    def test_opt_in_selects_exact_cpm_packages_and_preserves_every_sdk_edge(self):
        for source in (False, True):
            with self.subTest(source=source):
                _, baseline_projects, baseline_packages, _ = evaluate('', source)
                properties, projects, packages, versions = evaluate('true', source)
                self.assertEqual(properties['IsPackable'], 'false')
                self.assertFalse(projects & OWN)
                self.assertEqual(set(packages) & OWN, OWN)
                self.assertEqual(projects, baseline_projects - OWN)
                self.assertEqual(set(packages) - OWN, set(baseline_packages))
                for package in OWN:
                    self.assertEqual(versions[package], '6.0.0')
                    self.assertFalse(packages[package].get('Version'), 'CPM owns the selected public artifact version.')


if __name__ == '__main__':
    unittest.main()
