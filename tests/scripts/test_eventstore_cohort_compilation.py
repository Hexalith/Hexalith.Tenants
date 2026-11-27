"""Compile real production Tenants projects against every advertised EventStore package cohort."""

import json
import os
from pathlib import Path
import re
import subprocess
import tempfile
import unittest
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[2]
TARGETS_FILE = ROOT / 'Directory.Build.targets'

PRODUCTION_PROJECTS = (
    ('Hexalith.Tenants.Contracts', 'src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj'),
    ('Hexalith.Tenants.Server', 'src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj'),
    ('Hexalith.Tenants.Client', 'src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj'),
    ('Hexalith.Tenants.Testing', 'src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj'),
    ('Hexalith.Tenants.Aspire', 'src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj'),
    ('Hexalith.Tenants', 'src/Hexalith.Tenants/Hexalith.Tenants.csproj'),
)

SCRUBBED_ENV_KEYS = (
    'CustomBeforeDirectoryBuildProps',
    'CustomAfterMicrosoftCommonProps',
    'CustomBeforeDirectoryBuildTargets',
    'CustomAfterDirectoryBuildTargets',
    'AfterMicrosoftNETSdkTargets',
    'HEXALITH_SOURCE_MAPPING_HASH',
)


def _clean_environment():
    environment = os.environ.copy()
    for key in SCRUBBED_ENV_KEYS:
        environment.pop(key, None)
    return environment


def _read_canonical_boundary_target():
    tree = ET.parse(TARGETS_FILE)
    for target in tree.findall('.//Target'):
        if target.get('Name') == 'ValidateCanonicalBoundaryPackageCohort':
            return target
    raise AssertionError(f'ValidateCanonicalBoundaryPackageCohort target not found in {TARGETS_FILE}')


def _advertised_projects_and_cohorts():
    target = _read_canonical_boundary_target()
    target_condition = target.get('Condition', '')
    projects = tuple(re.findall(r"'\$\(MSBuildProjectName\)'\s*==\s*'([^']+)'", target_condition))

    errors = target.findall('Error')
    if not errors:
        raise AssertionError('ValidateCanonicalBoundaryPackageCohort has no <Error> guard elements.')

    eventstore_error = errors[0]
    condition = eventstore_error.get('Condition', '')
    text = eventstore_error.get('Text', '')
    cohorts = tuple(re.findall(r"'\$\(HexalithEventStoreVersion\)'\s*!=\s*'([^']+)'", condition))
    diagnostic_versions = tuple(re.findall(r'\b\d+\.\d+\.\d+\b', text))
    return projects, cohorts, diagnostic_versions, text


class RealEventStoreCohortCompilationTests(unittest.TestCase):
    def test_advertised_eventstore_cohorts_compile_all_real_production_projects(self):
        advertised_projects, advertised_cohorts, diagnostic_versions, _ = _advertised_projects_and_cohorts()
        expected_project_names = {name for name, _ in PRODUCTION_PROJECTS}
        self.assertEqual(set(advertised_projects), expected_project_names)
        self.assertTrue(advertised_cohorts, 'Expected at least one advertised EventStore cohort.')
        self.assertEqual(advertised_cohorts, diagnostic_versions)
        for version in advertised_cohorts:
            self.assertRegex(version, r'^\d+\.\d+\.\d+$')

        workspace_assets_before = {
            name: (ROOT / Path(rel).parent / 'obj/project.assets.json').stat().st_mtime_ns
            for name, rel in PRODUCTION_PROJECTS
            if (ROOT / Path(rel).parent / 'obj/project.assets.json').is_file()
        }

        environment = _clean_environment()
        with tempfile.TemporaryDirectory(prefix='tenants-eventstore-cohort-compile-') as temporary:
            temp_root = Path(temporary)
            for cohort in advertised_cohorts:
                with self.subTest(eventstore_cohort=cohort):
                    cohort_dir = temp_root / cohort
                    cohort_dir.mkdir()
                    artifacts_dir = cohort_dir / 'artifacts'
                    solution_path = cohort_dir / 'ProductionProjects.slnx'

                    solution = ET.Element('Solution')
                    for _, relative_project in PRODUCTION_PROJECTS:
                        ET.SubElement(solution, 'Project', Path=str(ROOT / relative_project))
                    ET.ElementTree(solution).write(solution_path, encoding='unicode')

                    result = subprocess.run(
                        [
                            'dotnet', 'build', str(solution_path),
                            '-c', 'Release',
                            '-m:1',
                            '-nr:false',
                            '-warnaserror',
                            '-p:UseHexalithProjectReferences=false',
                            '-p:UseNuGetDeps=true',
                            f'-p:HexalithEventStoreVersion={cohort}',
                            '-p:UseArtifactsOutput=true',
                            f'-p:ArtifactsPath={artifacts_dir}',
                        ],
                        cwd=ROOT,
                        env=environment,
                        capture_output=True,
                        text=True,
                        timeout=300,
                    )
                    output = result.stdout + result.stderr
                    self.assertEqual(result.returncode, 0, f'EventStore {cohort} build failed:\n{output}')
                    self.assertIn('0 Error(s)', output)

                    for project_name, _ in PRODUCTION_PROJECTS:
                        assembly_path = artifacts_dir / 'bin' / project_name / 'release' / f'{project_name}.dll'
                        self.assertTrue(
                            assembly_path.is_file() and assembly_path.stat().st_size > 0,
                            f'Expected compiled assembly {assembly_path} for EventStore {cohort}.',
                        )

                        assets_path = artifacts_dir / 'obj' / project_name / 'project.assets.json'
                        self.assertTrue(
                            assets_path.is_file(),
                            f'Expected isolated assets file {assets_path} for EventStore {cohort}.',
                        )
                        assets = json.loads(assets_path.read_text(encoding='utf-8'))
                        libraries = assets.get('libraries', {})
                        eventstore_libs = {
                            lib_key: lib_meta
                            for lib_key, lib_meta in libraries.items()
                            if lib_key.startswith('Hexalith.EventStore.')
                        }
                        self.assertTrue(
                            eventstore_libs,
                            f'{project_name} must resolve real Hexalith.EventStore.* packages for {cohort}.',
                        )
                        for lib_key, lib_meta in eventstore_libs.items():
                            package_id, _, resolved_version = lib_key.partition('/')
                            self.assertEqual(
                                lib_meta.get('type'),
                                'package',
                                f'{project_name} resolved {package_id} as {lib_meta.get("type")!r} instead of package.',
                            )
                            self.assertEqual(
                                resolved_version,
                                cohort,
                                f'{project_name} resolved {package_id} {resolved_version} instead of {cohort}.',
                            )

                        if project_name == 'Hexalith.Tenants.Contracts':
                            mcp_meta = libraries.get('Hexalith.McpCli.Abstractions/3.0.0')
                            self.assertIsNotNone(
                                mcp_meta,
                                f'{project_name} must resolve Hexalith.McpCli.Abstractions/3.0.0 for {cohort}.',
                            )
                            self.assertEqual(mcp_meta.get('type'), 'package')

        for name, rel in PRODUCTION_PROJECTS:
            assets_file = ROOT / Path(rel).parent / 'obj/project.assets.json'
            if name in workspace_assets_before:
                self.assertEqual(
                    assets_file.stat().st_mtime_ns,
                    workspace_assets_before[name],
                    f'Isolated cohort build must not modify workspace assets file {assets_file}.',
                )

    def test_incompatible_older_and_alias_eventstore_versions_remain_rejected_on_real_projects(self):
        _, advertised_cohorts, _, diagnostic_text = _advertised_projects_and_cohorts()
        diagnostic_prefix = diagnostic_text.split(' as an exact 3-part canonical SemVer')[0]
        rejected_versions = ('4.2.0', '5.1.1', '5.2.0', f'{advertised_cohorts[0]}.0', f'{advertised_cohorts[-1]}+sha')
        representative_projects = (
            ('Hexalith.Tenants.Contracts', 'src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj'),
            ('Hexalith.Tenants', 'src/Hexalith.Tenants/Hexalith.Tenants.csproj'),
        )

        environment = _clean_environment()
        with tempfile.TemporaryDirectory(prefix='tenants-eventstore-cohort-reject-') as temporary:
            temp_root = Path(temporary)
            for project_name, relative_project in representative_projects:
                for version in rejected_versions:
                    with self.subTest(project=project_name, rejected_version=version):
                        safe_version = re.sub(r'[^0-9A-Za-z._-]', '_', version)
                        artifacts_dir = temp_root / f'{project_name}-{safe_version}' / 'artifacts'
                        result = subprocess.run(
                            [
                                'dotnet', 'build', str(ROOT / relative_project),
                                '-c', 'Release',
                                '-m:1',
                                '-nr:false',
                                '-warnaserror',
                                '-p:UseHexalithProjectReferences=false',
                                '-p:UseNuGetDeps=true',
                                f'-p:HexalithEventStoreVersion={version}',
                                '-p:UseArtifactsOutput=true',
                                f'-p:ArtifactsPath={artifacts_dir}',
                            ],
                            cwd=ROOT,
                            env=environment,
                            capture_output=True,
                            text=True,
                            timeout=120,
                        )
                        output = result.stdout + result.stderr
                        self.assertNotEqual(
                            result.returncode,
                            0,
                            f'Expected {project_name} build with EventStore {version} to be rejected:\n{output}',
                        )
                        self.assertIn(diagnostic_prefix, output)
                        assembly_path = artifacts_dir / 'bin' / project_name / 'release' / f'{project_name}.dll'
                        self.assertFalse(
                            assembly_path.exists(),
                            f'Rejected EventStore {version} must not emit {assembly_path}.',
                        )


if __name__ == '__main__':
    unittest.main()
