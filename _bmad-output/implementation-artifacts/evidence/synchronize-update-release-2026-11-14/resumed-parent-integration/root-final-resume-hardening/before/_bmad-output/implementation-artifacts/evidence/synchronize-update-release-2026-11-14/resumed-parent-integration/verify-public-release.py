"""Verify public release archives against an authenticated successful candidate artifact."""
import argparse
import base64
import datetime
import gzip
import hashlib
import io
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[5]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--owner', choices=('FrontComposer', 'Tenants'), required=True)
parser.add_argument('--version', required=True)
parser.add_argument('--source', required=True)
parser.add_argument('--artifact-id', type=int, required=True)
parser.add_argument('--artifact-zip', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
args = parser.parse_args()
assert re.fullmatch(r'\d+\.\d+\.\d+', args.version)
assert re.fullmatch(r'[0-9a-f]{40}', args.source)
repo = 'Hexalith/Hexalith.' + args.owner
owner = ROOT if args.owner == 'Tenants' else ROOT / 'references/Hexalith.FrontComposer'
scratch = Path(os.environ['TMPDIR']) / ('public-' + args.owner)
scratch.mkdir()
args.output.mkdir(parents=True, exist_ok=True)
receipt_path = args.output / 'verification.json'
assert not receipt_path.exists(), 'Use a new output attempt; preserve earlier receipts.'


def api(path):
    return json.loads(subprocess.check_output(['gh', 'api', 'repos/' + repo + '/' + path]))


def download(url):
    with urllib.request.urlopen(url, timeout=60) as response:
        return response.read()


def document(url):
    data = download(url)
    return json.loads(gzip.decompress(data) if data[:2] == b'\x1f\x8b' else data)


inventory = json.loads(subprocess.check_output(
    ['git', 'show', args.source + ':tools/release-packages.json'], cwd=owner))['packages']
ids = [item['id'] for item in inventory]
assert len(ids) == len(set(ids)) == (5 if args.owner == 'Tenants' else 8)
release = api('releases/tags/v' + args.version)
assert release['tag_name'] == 'v' + args.version
assert not release['draft'] and not release['prerelease']
if args.owner == 'FrontComposer':
    assert release['immutable'], 'FrontComposer requires an immutable GitHub Release.'
tag = api('git/ref/tags/v' + args.version)['object']
for _ in range(4):
    if tag['type'] == 'commit':
        break
    assert tag['type'] == 'tag'
    tag = api('git/tags/' + tag['sha'])['object']
assert tag['type'] == 'commit' and tag['sha'] == args.source
artifact = api('actions/artifacts/' + str(args.artifact_id))
assert not artifact['expired'] and artifact['workflow_run']['head_sha'] == args.source
run = api('actions/runs/' + str(artifact['workflow_run']['id']))
assert run['head_sha'] == args.source and run['head_branch'] == 'main'
assert run['event'] == 'workflow_dispatch' and run['conclusion'] == 'success'
assert run['path'].split('@')[0] == '.github/workflows/release.yml'
assert artifact['name'] in ('release-candidate', f"release-candidate-{run['id']}-{run['run_attempt']}")
archive_bytes = args.artifact_zip.read_bytes()
assert artifact['digest'] == 'sha256:' + hashlib.sha256(archive_bytes).hexdigest()
assert artifact['size_in_bytes'] == len(archive_bytes)
prepared = {}
with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
    for name in archive.namelist():
        if name.startswith('nupkgs/') and name.endswith('.nupkg'):
            basename = Path(name).name
            assert basename not in prepared
            prepared[basename] = archive.read(name)
        elif name == 'release-candidate.tar':
            with tarfile.open(fileobj=io.BytesIO(archive.read(name))) as candidate:
                for item in candidate:
                    if item.name.startswith('nupkgs/') and item.name.endswith('.nupkg'):
                        assert item.isfile()
                        basename = Path(item.name).name
                        assert basename not in prepared
                        prepared[basename] = candidate.extractfile(item).read()
assert set(prepared) == {identity + '.' + args.version + '.nupkg' for identity in ids}
result = {'status': 'in-progress', 'verifiedAtUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
          'repository': repo, 'version': args.version, 'source': args.source,
          'release': {'id': release['id'], 'url': release['html_url'], 'tagCommit': tag['sha'],
                      'immutable': release['immutable']},
          'candidate': {'artifactId': args.artifact_id, 'name': artifact['name'], 'digest': artifact['digest'],
                        'runId': run['id'], 'runAttempt': run['run_attempt'], 'conclusion': run['conclusion']},
          'comparisonHelperSha256': hashlib.sha256((ROOT / 'scripts/release-package-content.py').read_bytes()).hexdigest(),
          'packages': []}
receipt_path.write_text(json.dumps(result, indent=2) + '\n')
for identity in ids:
    lower = identity.lower()
    registration_url = f'https://api.nuget.org/v3/registration5-gz-semver2/{lower}/{args.version}.json'
    registration = document(registration_url)
    assert registration['listed'] is True
    catalog = document(registration['catalogEntry'])
    assert catalog['id'] == identity and catalog['version'] == args.version and catalog['listed'] is True
    url = f'https://api.nuget.org/v3-flatcontainer/{lower}/{args.version}/{lower}.{args.version}.nupkg'
    public_bytes = download(url)
    assert catalog['packageHashAlgorithm'] == 'SHA512'
    assert base64.b64encode(hashlib.sha512(public_bytes).digest()).decode() == catalog['packageHash']
    with zipfile.ZipFile(io.BytesIO(public_bytes)) as archive:
        names = [name for name in archive.namelist() if name.endswith('.nuspec')]
        assert len(names) == 1 and '.signature.p7s' in archive.namelist()
        metadata = ET.fromstring(archive.read(names[0])).find('{*}metadata')
        assert metadata.findtext('{*}id') == identity and metadata.findtext('{*}version') == args.version
        assert metadata.find('{*}repository').get('commit') == args.source
    candidate_path = scratch / (identity + '.candidate.nupkg')
    public_path = scratch / (identity + '.public.nupkg')
    candidate_path.write_bytes(prepared[identity + '.' + args.version + '.nupkg'])
    public_path.write_bytes(public_bytes)
    compared = subprocess.run(['python3', str(ROOT / 'scripts/release-package-content.py'),
                              'compare', str(candidate_path), str(public_path)], check=False)
    assert compared.returncode == 0, identity + ': original payload and native signature comparison failed'
    signature_log = public_path.with_name(public_path.name + '.signature-verification.log')
    retained_log = args.output / (identity + '.signature-verification.txt')
    shutil.copyfile(signature_log, retained_log)
    result['packages'].append({'id': identity, 'version': args.version, 'source': args.source, 'listed': True,
        'registrationUrl': registration_url, 'catalogUrl': registration['catalogEntry'], 'archiveUrl': url,
        'sha256': hashlib.sha256(public_bytes).hexdigest(), 'officialSha512': catalog['packageHash'],
        'preparedSha256': hashlib.sha256(candidate_path.read_bytes()).hexdigest(),
        'payloadComparison': 'identical except repository signature', 'nativeSignatureVerificationExitCode': 0,
        'signatureLogSha256': hashlib.sha256(retained_log.read_bytes()).hexdigest()})
    receipt_path.write_text(json.dumps(result, indent=2) + '\n')
    print(identity + ': listed/source/hash/signature/prepared-payload verified', flush=True)
result['status'] = 'passed'
receipt_path.write_text(json.dumps(result, indent=2) + '\n')
