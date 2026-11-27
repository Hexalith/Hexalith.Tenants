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
EXACT_SEMVER_DELIMITER = ' as an exact 3-part canonical SemVer'

# Mirrors ValidateCanonicalBoundaryPackageCohort in Directory.Build.targets:
# the five packable libraries plus the Hexalith.Tenants domain host.
# Non-packable Api/UI/AppHost projects are outside that canonical boundary target.
PRODUCTION_PROJECTS = (
    ('Hexalith.Tenants.Contracts', 'src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj', ('Hexalith.EventStore.Contracts',)),
    ('Hexalith.Tenants.Server', 'src/Hexalith.Tenants.Server/Hexalith.Tenants.Server.csproj', ('Hexalith.EventStore.Server',)),
    ('Hexalith.Tenants.Client', 'src/Hexalith.Tenants.Client/Hexalith.Tenants.Client.csproj', ('Hexalith.EventStore.Client',)),
    ('Hexalith.Tenants.Testing', 'src/Hexalith.Tenants.Testing/Hexalith.Tenants.Testing.csproj', ('Hexalith.EventStore.ServiceDefaults',)),
    ('Hexalith.Tenants.Aspire', 'src/Hexalith.Tenants.Aspire/Hexalith.Tenants.Aspire.csproj', ('Hexalith.EventStore.Aspire',)),
    ('Hexalith.Tenants', 'src/Hexalith.Tenants/Hexalith.Tenants.csproj', ('Hexalith.EventStore.DomainService', 'Hexalith.EventStore.Gateway')),
)

SCRUBBED_ENV_KEYS = (
    'CustomBeforeDirectoryBuildProps',
    'CustomAfterMicrosoftCommonProps',
    'CustomBeforeDirectoryBuildTargets',
    'CustomAfterDirectoryBuildTargets',
    'AfterMicrosoftNETSdkTargets',
    'HEXALITH_SOURCE_MAPPING_HASH',
    'HexalithEventStoreFromSource',
    'HexalithCatalogEventStoreVersion',
    'HexalithMcpCliFromSource',
    'HexalithMcpCliVersion',
    'HexalithCommonsFromSource',
    'HexalithFrontComposerFromSource',
    'HexalithMemoriesFromSource',
    'HexalithUsePublishedSdkForHostedTests',
    'MSBUILDTERMINALLOGGER',
)


def _clean_environment():
    environment = os.environ.copy()
    for key in SCRUBBED_ENV_KEYS:
        environment.pop(key, None)
    environment['DOTNET_CLI_UI_LANGUAGE'] = 'en-US'
    return environment


def _snapshot_workspace_assets():
    snapshot = {}
    for name, rel, _ in PRODUCTION_PROJECTS:
        assets_file = ROOT / Path(rel).parent / 'obj/project.assets.json'
        snapshot[name] = (assets_file, assets_file.stat().st_mtime_ns if assets_file.is_file() else None)
    return snapshot


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
    eventstore_errors = [error for error in errors if '$(HexalithEventStoreVersion)' in error.get('Condition', '')]
    if len(eventstore_errors) != 1:
        raise AssertionError(
            f'Expected exactly one HexalithEventStoreVersion <Error> guard in ValidateCanonicalBoundaryPackageCohort, '
            f'found {len(eventstore_errors)}.',
        )
    mcpcli_errors = [error for error in errors if '$(HexalithMcpCliVersion)' in error.get('Condition', '')]
    if len(mcpcli_errors) != 1:
        raise AssertionError(
            f'Expected exactly one HexalithMcpCliVersion <Error> guard in ValidateCanonicalBoundaryPackageCohort, '
            f'found {len(mcpcli_errors)}.',
        )

    eventstore_error = eventstore_errors[0]
    eventstore_condition = eventstore_error.get('Condition', '')
    eventstore_text = eventstore_error.get('Text', '')
    cohorts = tuple(re.findall(r"'\$\(HexalithEventStoreVersion\)'\s*!=\s*'([^']+)'", eventstore_condition))
    diagnostic_versions = tuple(re.findall(r'\b\d+\.\d+\.\d+\b', eventstore_text))

    mcpcli_error = mcpcli_errors[0]
    mcpcli_condition = mcpcli_error.get('Condition', '')
    mcpcli_text = mcpcli_error.get('Text', '')
    mcpcli_versions = tuple(re.findall(r"'\$\(HexalithMcpCliVersion\)'\s*!=\s*'([^']+)'", mcpcli_condition))
    if len(mcpcli_versions) != 1:
        raise AssertionError(
            f'Expected exactly one HexalithMcpCliVersion literal in ValidateCanonicalBoundaryPackageCohort, '
            f'found {mcpcli_versions!r}.',
        )

    return projects, cohorts, diagnostic_versions, eventstore_text, mcpcli_versions[0], mcpcli_text


class RealEventStoreCohortCompilationTests(unittest.TestCase):
    def _assert_workspace_assets_unchanged(self, snapshot):
        for name, (assets_file, mtime_before) in snapshot.items():
            if mtime_before is None:
                self.assertFalse(
                    assets_file.exists(),
                    f'Isolated cohort build must not create workspace assets file {assets_file} for {name}.',
                )
            else:
                self.assertTrue(
                    assets_file.is_file(),
                    f'Isolated cohort build must not delete workspace assets file {assets_file} for {name}.',
                )
                self.assertEqual(
                    assets_file.stat().st_mtime_ns,
                    mtime_before,
                    f'Isolated cohort build must not modify workspace assets file {assets_file} for {name}.',
                )

    def test_advertised_eventstore_cohorts_compile_all_real_production_projects(self):
        (
            advertised_projects,
            advertised_cohorts,
            diagnostic_versions,
            eventstore_text,
            mcpcli_version,
            mcpcli_text,
        ) = _advertised_projects_and_cohorts()
        expected_project_names = {name for name, _, _ in PRODUCTION_PROJECTS}
        self.assertEqual(set(advertised_projects), expected_project_names)
        self.assertTrue(advertised_cohorts, 'Expected at least one advertised EventStore cohort.')
        self.assertEqual(advertised_cohorts, diagnostic_versions)
        self.assertIn(EXACT_SEMVER_DELIMITER, eventstore_text)
        self.assertIn(EXACT_SEMVER_DELIMITER, mcpcli_text)
        for version in advertised_cohorts:
            self.assertRegex(version, r'^\d+\.\d+\.\d+$')
        self.assertRegex(mcpcli_version, r'^\d+\.\d+\.\d+$')

        workspace_assets_before = _snapshot_workspace_assets()
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
                    for _, relative_project, _ in PRODUCTION_PROJECTS:
                        ET.SubElement(solution, 'Project', Path=str(ROOT / relative_project))
                    ET.ElementTree(solution).write(solution_path, encoding='unicode')

                    result = subprocess.run(
                        [
                            'dotnet', 'build', str(solution_path),
                            '-c', 'Release',
                            '-m:1',
                            '-nr:false',
                            '-tl:off',
                            '-warnaserror',
                            '-p:UseHexalithProjectReferences=false',
                            '-p:UseNuGetDeps=true',
                            '-p:HexalithEventStoreFromSource=false',
                            '-p:HexalithMcpCliFromSource=false',
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

                    for project_name, _, expected_direct_packages in PRODUCTION_PROJECTS:
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
                        eventstore_libs = {}
                        for lib_key, lib_meta in libraries.items():
                            package_id, _, resolved_version = lib_key.partition('/')
                            if package_id == 'Hexalith.EventStore' or package_id.startswith('Hexalith.EventStore.'):
                                eventstore_libs[package_id] = (resolved_version, lib_meta)

                        self.assertTrue(
                            eventstore_libs,
                            f'{project_name} must resolve real Hexalith.EventStore packages for {cohort}.',
                        )
                        for expected_package in expected_direct_packages:
                            self.assertIn(
                                expected_package,
                                eventstore_libs,
                                f'{project_name} must resolve direct EventStore package {expected_package} for {cohort}.',
                            )
                        for package_id, (resolved_version, lib_meta) in eventstore_libs.items():
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
                            mcp_key = f'Hexalith.McpCli.Abstractions/{mcpcli_version}'
                            mcp_meta = libraries.get(mcp_key)
                            self.assertIsNotNone(
                                mcp_meta,
                                f'{project_name} must resolve {mcp_key} for {cohort}.',
                            )
                            self.assertEqual(mcp_meta.get('type'), 'package')

        self._assert_workspace_assets_unchanged(workspace_assets_before)

    def test_incompatible_older_and_alias_eventstore_versions_remain_rejected_on_real_projects(self):
        _, advertised_cohorts, _, eventstore_text, _, mcpcli_text = _advertised_projects_and_cohorts()
        self.assertIn(EXACT_SEMVER_DELIMITER, eventstore_text)
        self.assertIn(EXACT_SEMVER_DELIMITER, mcpcli_text)
        eventstore_diagnostic_prefix = eventstore_text.split(EXACT_SEMVER_DELIMITER)[0]
        mcpcli_diagnostic_prefix = mcpcli_text.split(EXACT_SEMVER_DELIMITER)[0]

        rejected_eventstore_cases = (
            ('4.2.0', eventstore_diagnostic_prefix),
            ('5.1.1', eventstore_diagnostic_prefix),
            ('5.2.0', eventstore_diagnostic_prefix),
            (f'{advertised_cohorts[0]}.0', eventstore_diagnostic_prefix),
            (f'{advertised_cohorts[-1]}+sha', eventstore_diagnostic_prefix),
            ('5.6.0-rc.1', "Catalog SDK selection '5.6.0-rc.1' is not qualified"),
            ('5.7.0', "Catalog SDK selection '5.7.0' is not qualified"),
        )
        representative_projects = (
            ('Hexalith.Tenants.Contracts', 'src/Hexalith.Tenants.Contracts/Hexalith.Tenants.Contracts.csproj'),
            ('Hexalith.Tenants', 'src/Hexalith.Tenants/Hexalith.Tenants.csproj'),
        )

        workspace_assets_before = _snapshot_workspace_assets()
        environment = _clean_environment()
        with tempfile.TemporaryDirectory(prefix='tenants-eventstore-cohort-reject-') as temporary:
            temp_root = Path(temporary)
            for project_name, relative_project in representative_projects:
                for version, expected_diagnostic in rejected_eventstore_cases:
                    with self.subTest(project=project_name, rejected_eventstore_version=version):
                        safe_version = re.sub(r'[^0-9A-Za-z._-]', '_', version)
                        artifacts_dir = temp_root / f'{project_name}-es-{safe_version}' / 'artifacts'
                        result = subprocess.run(
                            [
                                'dotnet', 'build', str(ROOT / relative_project),
                                '-c', 'Release',
                                '-m:1',
                                '-nr:false',
                                '-tl:off',
                                '-warnaserror',
                                '-p:UseHexalithProjectReferences=false',
                                '-p:UseNuGetDeps=true',
                                '-p:HexalithEventStoreFromSource=false',
                                '-p:HexalithMcpCliFromSource=false',
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
                        self.assertIn(expected_diagnostic, output)
                        assembly_path = artifacts_dir / 'bin' / project_name / 'release' / f'{project_name}.dll'
                        self.assertFalse(
                            assembly_path.exists(),
                            f'Rejected EventStore {version} must not emit {assembly_path}.',
                        )

                with self.subTest(project=project_name, rejected_mcpcli_version='2.0.1'):
                    artifacts_dir = temp_root / f'{project_name}-mcp-2.0.1' / 'artifacts'
                    result = subprocess.run(
                        [
                            'dotnet', 'build', str(ROOT / relative_project),
                            '-c', 'Release',
                            '-m:1',
                            '-nr:false',
                            '-tl:off',
                            '-warnaserror',
                            '-p:UseHexalithProjectReferences=false',
                            '-p:UseNuGetDeps=true',
                            '-p:HexalithEventStoreFromSource=false',
                            '-p:HexalithMcpCliFromSource=false',
                            f'-p:HexalithEventStoreVersion={advertised_cohorts[-1]}',
                            '-p:HexalithMcpCliVersion=2.0.1',
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
                        f'Expected {project_name} build with McpCli 2.0.1 to be rejected:\n{output}',
                    )
                    self.assertIn(mcpcli_diagnostic_prefix, output)
                    assembly_path = artifacts_dir / 'bin' / project_name / 'release' / f'{project_name}.dll'
                    self.assertFalse(
                        assembly_path.exists(),
                        f'Rejected McpCli 2.0.1 must not emit {assembly_path}.',
                    )

        self._assert_workspace_assets_unchanged(workspace_assets_before)


if __name__ == '__main__':
    unittest.main()
