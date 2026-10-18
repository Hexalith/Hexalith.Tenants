import json
import shutil
import subprocess
import tempfile
import xml.etree.ElementTree as ET
from pathlib import Path

repo = Path(__file__).resolve().parents[2]
Path('/tmp/dw-621').mkdir(parents=True, exist_ok=True)
baseline = '2994c279bc1c291c69e3d11f6bb6290c5504a13f'
publisher = (repo / 'scripts/publish-partial-release.sh').read_text()
verifier = (repo / 'scripts/verify-partial-release.sh').read_text()
original = lambda file: subprocess.check_output(['git', 'show', f'{baseline}:scripts/{file}']).decode()
collapsed = publisher.replace('  gh release create', '  assets=(nupkgs/*.nupkg)\n  gh release create').replace(' nupkgs/*.nupkg\n', ' "${assets[*]}"\n')
variants = [
    ('publisher-control', 'PartialReleasePublicationTests', publisher, verifier, 0),
    ('publisher-original', 'PartialReleasePublicationTests', original('publish-partial-release.sh'), verifier, 6),
    ('publisher-always-prerelease', 'PartialReleasePublicationTests', publisher.replace('if [[ "$version" == *-* ]]; then', 'if true; then'), verifier, 4),
    ('publisher-collapsed-assets', 'PartialReleasePublicationTests', collapsed, verifier, 10),
    ('verifier-control', 'PartialReleaseVerificationTests', publisher, verifier, 0),
    ('verifier-original', 'PartialReleaseVerificationTests', publisher, original('verify-partial-release.sh'), 8),
    ('verifier-inverted', 'PartialReleaseVerificationTests', publisher, verifier.replace('expected_prerelease=false', 'expected_prerelease=true').replace('  expected_prerelease=true', '  expected_prerelease=false'), 7),
]
results = []
with tempfile.TemporaryDirectory(prefix='dw-621-variants-') as temporary:
    fixture = Path(temporary)
    (fixture / 'Hexalith.Tenants.slnx').touch()
    (fixture / 'scripts').mkdir()
    (fixture / 'tools').mkdir()
    shutil.copy2(repo / 'tools/release-packages.json', fixture / 'tools')
    shutil.copytree(repo / 'tests/Hexalith.Tenants.Contracts.Tests/bin/Debug/net10.0', fixture / 'bin')
    for name, target, selected_publisher, selected_verifier, expected_failures in variants:
        (fixture / 'scripts/publish-partial-release.sh').write_text(selected_publisher)
        (fixture / 'scripts/verify-partial-release.sh').write_text(selected_verifier)
        output = Path('/tmp/dw-621') / name
        command = ['dotnet', str(fixture / 'bin/Hexalith.Tenants.Contracts.Tests.dll'), '-class',
                   f'Hexalith.Tenants.Contracts.Tests.{target}', '-noColor', '-result-xml', str(output) + '.xml']
        with output.with_suffix('.log').open('w') as log:
            run = subprocess.run(command, stdout=log, stderr=subprocess.STDOUT, timeout=60)
        assembly = ET.parse(str(output) + '.xml').getroot().find('assembly')
        result = {'variant': name, 'exit': run.returncode,
                  **{key: int(assembly.get(key)) for key in ('total', 'passed', 'failed', 'skipped')}}
        results.append(result)
        print(json.dumps(result), flush=True)
        assert result['failed'] == expected_failures, result
        assert run.returncode == (0 if expected_failures == 0 else 1), result
Path('/tmp/dw-621/variants.json').write_text(json.dumps(results, indent=2) + '\n')
