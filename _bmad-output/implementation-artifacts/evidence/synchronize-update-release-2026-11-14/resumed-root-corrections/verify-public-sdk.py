"""Verify the observed public SDK patch from official NuGet bytes and repository signatures."""
import base64
import datetime
import gzip
import hashlib
import io
import json
import os
from pathlib import Path
import subprocess
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

EVIDENCE = Path(__file__).resolve().parent
ROOT = EVIDENCE.parents[4]
VERSION = '5.1.1'
SOURCE = subprocess.check_output(['git', 'rev-list', '-n', '1', 'v' + VERSION], cwd=ROOT / 'references/Hexalith.EventStore', text=True).strip()
packages = [row['id'] for row in json.loads((EVIDENCE.parent / 'eventstore-publication-verification.json').read_text())['packages']]
assert len(packages) == len(set(packages)) == 14
result = {'verified_at_utc': datetime.datetime.now(datetime.timezone.utc).isoformat(), 'version': VERSION,
          'expected_source': SOURCE, 'source_authority': 'EventStore v5.1.1 tag; every official archive must match', 'packages': []}


def download(url):
    with urllib.request.urlopen(url, timeout=45) as response:
        return response.read()


def document(url):
    data = download(url)
    return json.loads(gzip.decompress(data) if data[:2] == b'\x1f\x8b' else data)


for package in packages:
    identity = package.lower()
    registration_url = f'https://api.nuget.org/v3/registration5-gz-semver2/{identity}/{VERSION}.json'
    registration = document(registration_url)
    assert registration.get('listed') is True, package + ' must be listed'
    catalog_url = registration['catalogEntry']
    catalog = document(catalog_url)
    assert catalog['id'] == package and catalog['version'] == VERSION and catalog.get('listed') is True
    url = f'https://api.nuget.org/v3-flatcontainer/{identity}/{VERSION}/{identity}.{VERSION}.nupkg'
    payload = download(url)
    assert base64.b64encode(hashlib.sha512(payload).digest()).decode() == catalog['packageHash']
    assert catalog['packageHashAlgorithm'] == 'SHA512'
    with zipfile.ZipFile(io.BytesIO(payload)) as archive:
        nuspecs = [name for name in archive.namelist() if name.endswith('.nuspec')]
        assert len(nuspecs) == 1
        metadata = ET.fromstring(archive.read(nuspecs[0])).find('{*}metadata')
        assert metadata.findtext('{*}id') == package and metadata.findtext('{*}version') == VERSION
        source = metadata.find('{*}repository').get('commit')
        assert source == SOURCE, (package, source, SOURCE)
        assert '.signature.p7s' in archive.namelist()
    path = Path(os.environ['TMPDIR']) / (identity + '.nupkg')
    path.write_bytes(payload)
    log = EVIDENCE / (identity + '-5.1.1-signature.log')
    with log.open('w') as output:
        process = subprocess.run(['dotnet', 'nuget', 'verify', str(path), '--all'], stdout=output, stderr=subprocess.STDOUT)
    assert process.returncode == 0, log.read_text()
    result['packages'].append({'id': package, 'version': VERSION, 'source': source,
        'official_archive_url': url, 'registration_url': registration_url, 'catalog_url': catalog_url,
        'sha256': hashlib.sha256(payload).hexdigest(), 'official_sha512': catalog['packageHash'],
        'signature_log_sha256': hashlib.sha256(log.read_bytes()).hexdigest(), 'signature_exit_code': process.returncode})
    print(package + ': source/hash/signature pass', flush=True)
    (EVIDENCE / 'public-sdk-5.1.1.json').write_text(json.dumps(result, indent=2) + '\n')
