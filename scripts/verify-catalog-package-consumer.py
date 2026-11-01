#!/usr/bin/env python3
"""Restore actual catalog archives into an isolated package-only consumer and discover their inputs."""

import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
IDS = ('Hexalith.Tenants.Contracts', 'Hexalith.Tenants.Server')


def verify(directory, expected_source=None, output=None):
    archives = {}
    version = None
    sdk = None
    for path in sorted(directory.glob('*.nupkg')):
        with zipfile.ZipFile(path) as archive:
            names = [name for name in archive.namelist() if name.endswith('.nuspec')]
            if len(names) != 1:
                raise ValueError('A single package specification is required.')
            metadata = ET.fromstring(archive.read(names[0])).find('{*}metadata')
            package_id = metadata.findtext('{*}id')
            if package_id not in IDS:
                continue
            if package_id in archives:
                raise ValueError('Duplicate actual catalog package: ' + package_id)
            current_version = metadata.findtext('{*}version')
            if version is not None and current_version != version:
                raise ValueError('Catalog package versions disagree.')
            version = current_version
            repository = metadata.find('{*}repository')
            source = repository.get('commit') if repository is not None else None
            if expected_source is not None and source != expected_source:
                raise ValueError('Catalog package source differs from the exact reviewed source.')
            dependency_ids = {dependency.get("id") for dependency in metadata.findall(".//{*}dependency")}
            required = "Hexalith.EventStore.Contracts" if package_id == IDS[0] else "Hexalith.EventStore.Server"
            if required not in dependency_ids:
                raise ValueError("Catalog package is missing its required SDK dependency.")
            for dependency in metadata.findall('.//{*}dependency'):
                if dependency.get('id', '').startswith('Hexalith.EventStore.'):
                    selected = dependency.get('version', '').lstrip('[(').split(',')[0].strip().rstrip('])')
                    normalized = re.fullmatch(r'(3\.129\.[01]|3\.130\.[012]|3\.131\.0)(?:\.0)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?', selected)
                    if not normalized or sdk is not None and normalized[1] != sdk:
                        raise ValueError('Catalog SDK dependency is not one qualified stable cohort.')
                    sdk = normalized[1]
            dll = archive.read('lib/net10.0/' + package_id + '.dll')
            archives[package_id] = {'file': path, 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                                    'dll_sha256': hashlib.sha256(dll).hexdigest(), 'source': source}
    if set(archives) != set(IDS) or sdk is None:
        raise ValueError('Both actual Contracts and Server archives and their SDK dependencies are required.')
    with tempfile.TemporaryDirectory(prefix='tenants-packed-catalog-consumer-') as temporary:
        root = Path(temporary)
        packages = root / 'packages'
        project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
        properties = ET.SubElement(project, 'PropertyGroup')
        for key, value in {'OutputType': 'Exe', 'TargetFramework': 'net10.0', 'ImplicitUsings': 'enable',
                           'Nullable': 'enable', 'TreatWarningsAsErrors': 'true'}.items():
            ET.SubElement(properties, key).text = value
        references = ET.SubElement(project, 'ItemGroup')
        for package_id in IDS:
            ET.SubElement(references, 'PackageReference', Include=package_id, Version='[' + version + ']')
        for package_id in ('Hexalith.EventStore.ServiceDefaults', 'Hexalith.EventStore.DomainService'):
            ET.SubElement(references, 'PackageReference', Include=package_id, Version='[' + sdk + ']')
        ET.ElementTree(project).write(root / 'Consumer.csproj', encoding='unicode')
        configuration = ET.Element('configuration')
        sources = ET.SubElement(configuration, 'packageSources')
        ET.SubElement(sources, 'clear')
        ET.SubElement(sources, 'add', key='packed', value=str(directory.resolve()))
        ET.SubElement(sources, 'add', key='nuget', value='https://api.nuget.org/v3/index.json')
        mappings = ET.SubElement(configuration, 'packageSourceMapping')
        local = ET.SubElement(mappings, 'packageSource', key='packed')
        for package_id in IDS:
            ET.SubElement(local, 'package', pattern=package_id)
        ET.SubElement(ET.SubElement(mappings, 'packageSource', key='nuget'), 'package', pattern='*')
        ET.ElementTree(configuration).write(root / 'NuGet.Config', encoding='unicode')
        for name in ('Program.cs', 'approved-route-ids.txt'):
            shutil.copyfile(ROOT / 'tools/catalog-package-consumer' / name, root / name)
        environment = os.environ.copy()
        for key in ('CustomBeforeDirectoryBuildProps', 'CustomAfterMicrosoftCommonProps', 'CustomBeforeDirectoryBuildTargets',
                    'CustomAfterDirectoryBuildTargets', 'AfterMicrosoftNETSdkTargets', 'HEXALITH_SOURCE_MAPPING_HASH'):
            environment.pop(key, None)
        subprocess.run(['dotnet', 'restore', str(root / 'Consumer.csproj'), '--configfile', str(root / 'NuGet.Config'),
                        '--packages', str(packages), '--no-cache'], cwd=root, env=environment, check=True, timeout=180)
        assets = json.loads((root / 'obj/project.assets.json').read_text())
        for package_id, record in archives.items():
            key = package_id + '/' + version
            if assets['libraries'][key]['type'] != 'package':
                raise ValueError('Source/project references cannot qualify a packaged catalog.')
            restored = packages / package_id.lower() / version.lower() / (package_id.lower() + '.' + version.lower() + '.nupkg')
            if hashlib.sha256(restored.read_bytes()).hexdigest() != record['sha256']:
                raise ValueError('Restored catalog archive differs from the actual candidate bytes.')
        subprocess.run(['dotnet', 'build', str(root / 'Consumer.csproj'), '--no-restore', '-c', 'Release',
                        '-m:1', '-nr:false', '-warnaserror'], cwd=root, env=environment, check=True, timeout=120)
        for package_id, record in archives.items():
            built = root / 'bin/Release/net10.0' / (package_id + '.dll')
            if hashlib.sha256(built.read_bytes()).hexdigest() != record['dll_sha256']:
                raise ValueError('Consumer loaded a DLL other than the archived catalog artifact.')
        result = subprocess.run(['dotnet', str(root / 'bin/Release/net10.0/Consumer.dll')], cwd=root,
                                env=environment, capture_output=True, text=True, check=True, timeout=30)
        discovery = json.loads(result.stdout)
        receipt = {'version': version, 'sdk': sdk, 'source': expected_source,
                   'packages': {key: {name: value for name, value in row.items() if name != 'file'} for key, row in archives.items()},
                   'discovery': discovery}
        if output:
            output.write_text(json.dumps(receipt, indent=2) + '\n')
        print(json.dumps(receipt, indent=2))
        return receipt


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('package_directory', type=Path)
    parser.add_argument('--expected-source-sha', default=os.environ.get('GITHUB_SHA'))
    parser.add_argument('--output', type=Path)
    arguments = parser.parse_args()
    if arguments.expected_source_sha is not None and not re.fullmatch('[0-9a-f]{40}', arguments.expected_source_sha):
        parser.error('An exact lowercase source SHA is required.')
    verify(arguments.package_directory, arguments.expected_source_sha, arguments.output)
