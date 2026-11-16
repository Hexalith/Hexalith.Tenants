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
def require(condition, message):
    if not condition:
        raise ValueError(message)


def prepare_job_name(workflow, job_id='prepare-release'):
    match = re.search(r'(?m)^  ' + re.escape(job_id) + r':\s*$', workflow)
    require(match is not None, 'Source workflow has no ' + job_id + ' job.')
    block = workflow[match.end():]
    end = re.search(r'(?m)^  [A-Za-z0-9_-]+:', block)
    if end:
        block = block[:end.start()]
    name = re.search(r'(?m)^    name:\s*(.+)$', block)
    value = name.group(1).strip().strip('"\'') if name else job_id
    require('${{' not in value, 'Dynamic prepare-release job names cannot prove an exact producer.')
    return value


def producer_attempt(artifact, run, fetch, job_name, owner_name):
    """Bind names/time windows to one actual successful prepare job; never infer latest."""
    run_id = run['id']
    latest = run['run_attempt']
    require(type(latest) is int and latest > 0, 'Invalid candidate run attempt.')
    name = artifact['name']
    scoped = re.fullmatch(r'release-candidate-([0-9]+)-([0-9]+)', name)
    if scoped:
        require(int(scoped.group(1)) == run_id and 1 <= int(scoped.group(2)) <= latest,
                'Candidate artifact has a different run/attempt scope.')
        attempts = [int(scoped.group(2))]
    else:
        require(owner_name == 'Tenants' and name == 'release-candidate',
                'Candidate artifact needs an exact run/attempt name.')
        attempts = range(1, latest + 1)
    def instant(value):
        require(isinstance(value, str) and value.endswith('Z'), 'Missing GitHub producer timestamp evidence.')
        return datetime.datetime.fromisoformat(value.replace('Z', '+00:00'))
    created = instant(artifact.get('created_at'))
    updated = instant(artifact.get('updated_at'))
    require(created <= updated, 'Candidate artifact timestamps are inconsistent.')
    candidates = []
    for attempt in attempts:
        page = 1
        while True:
            response = fetch(f'actions/runs/{run_id}/attempts/{attempt}/jobs?per_page=100&page={page}')
            jobs = response['jobs']
            for job in jobs:
                if job.get('name') != job_name or job.get('status') != 'completed' or job.get('conclusion') != 'success':
                    continue
                require(job.get('run_id') == run_id and job.get('head_sha') == run['head_sha'],
                        'Prepare job has different run/source evidence.')
                require(job.get('run_attempt', attempt) == attempt, 'Prepare job has inconsistent attempt evidence.')
                started = instant(job.get('started_at'))
                completed = instant(job.get('completed_at'))
                if started <= created <= updated <= completed:
                    candidates.append({'attempt': attempt, 'jobId': job['id'], 'jobName': job['name'],
                        'jobStartedAt': job['started_at'], 'jobCompletedAt': job['completed_at'],
                        'artifactCreatedAt': artifact['created_at'], 'artifactUpdatedAt': artifact['updated_at'],
                        'jobsEvidenceSha256': hashlib.sha256(json.dumps(response, sort_keys=True).encode()).hexdigest()})
            if len(jobs) < 100:
                break
            page += 1
    require(len(candidates) == 1, 'Missing or ambiguous successful prepare-release producing-attempt evidence.')
    return candidates[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner', choices=('FrontComposer', 'Tenants'), required=True)
    parser.add_argument('--version', required=True)
    parser.add_argument('--source', required=True)
    parser.add_argument('--artifact-id', type=int, required=True)
    parser.add_argument('--artifact-zip', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    require(re.fullmatch(r'\d+\.\d+\.\d+', args.version), "Release verification failed: re.fullmatch(r'\\d+\\.\\d+\\.\\d+', args.version)")
    require(re.fullmatch(r'[0-9a-f]{40}', args.source), "Release verification failed: re.fullmatch(r'[0-9a-f]{40}', args.source)")
    repo = 'Hexalith/Hexalith.' + args.owner
    owner = ROOT if args.owner == 'Tenants' else ROOT / 'references/Hexalith.FrontComposer'
    scratch = Path(os.environ['TMPDIR']) / ('public-' + args.owner)
    scratch.mkdir()
    args.output.mkdir(parents=True, exist_ok=True)
    receipt_path = args.output / 'verification.json'
    require(not receipt_path.exists(), 'Use a new output attempt; preserve earlier receipts.')


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
    require(len(ids) == len(set(ids)) == (5 if args.owner == 'Tenants' else 8), "Release verification failed: len(ids) == len(set(ids)) == (5 if args.owner == 'Tenants' else 8)")
    release = api('releases/tags/v' + args.version)
    require(release['tag_name'] == 'v' + args.version, "Release verification failed: release['tag_name'] == 'v' + args.version")
    require(not release['draft'] and not release['prerelease'], "Release verification failed: not release['draft'] and not release['prerelease']")
    if args.owner == 'FrontComposer':
        require(release['immutable'], 'FrontComposer requires an immutable GitHub Release.')
    tag = api('git/ref/tags/v' + args.version)['object']
    for _ in range(4):
        if tag['type'] == 'commit':
            break
        require(tag['type'] == 'tag', "Release verification failed: tag['type'] == 'tag'")
        tag = api('git/tags/' + tag['sha'])['object']
    require(tag['type'] == 'commit' and tag['sha'] == args.source, "Release verification failed: tag['type'] == 'commit' and tag['sha'] == args.source")
    artifact = api('actions/artifacts/' + str(args.artifact_id))
    require(not artifact['expired'] and artifact['workflow_run']['head_sha'] == args.source, "Release verification failed: not artifact['expired'] and artifact['workflow_run']['head_sha'] == args.source")
    run = api('actions/runs/' + str(artifact['workflow_run']['id']))
    require(run['head_sha'] == args.source and run['head_branch'] == 'main', "Release verification failed: run['head_sha'] == args.source and run['head_branch'] == 'main'")
    require(run['event'] == 'workflow_dispatch' and run['conclusion'] == 'success', "Release verification failed: run['event'] == 'workflow_dispatch' and run['conclusion'] == 'success'")
    require(run['path'].split('@')[0] == '.github/workflows/release.yml', "Release verification failed: run['path'].split('@')[0] == '.github/workflows/release.yml'")
    workflow = subprocess.check_output(['git', 'show', args.source + ':.github/workflows/release.yml'], cwd=owner, text=True)
    producing = producer_attempt(artifact, run, api, prepare_job_name(workflow, 'prepare-release' if args.owner == 'Tenants' else 'prepare-candidate'), args.owner)
    archive_bytes = args.artifact_zip.read_bytes()
    require(artifact['digest'] == 'sha256:' + hashlib.sha256(archive_bytes).hexdigest(), "Release verification failed: artifact['digest'] == 'sha256:' + hashlib.sha256(archive_bytes).hexdigest()")
    require(artifact['size_in_bytes'] == len(archive_bytes), "Release verification failed: artifact['size_in_bytes'] == len(archive_bytes)")
    prepared = {}
    with zipfile.ZipFile(io.BytesIO(archive_bytes)) as archive:
        for name in archive.namelist():
            if name.startswith('nupkgs/') and name.endswith('.nupkg'):
                basename = Path(name).name
                require(basename not in prepared, 'Release verification failed: basename not in prepared')
                prepared[basename] = archive.read(name)
            elif name == 'release-candidate.tar':
                with tarfile.open(fileobj=io.BytesIO(archive.read(name))) as candidate:
                    for item in candidate:
                        if item.name.startswith('nupkgs/') and item.name.endswith('.nupkg'):
                            require(item.isfile(), 'Release verification failed: item.isfile()')
                            basename = Path(item.name).name
                            require(basename not in prepared, 'Release verification failed: basename not in prepared')
                            prepared[basename] = candidate.extractfile(item).read()
    require(set(prepared) == {identity + '.' + args.version + '.nupkg' for identity in ids}, "Release verification failed: set(prepared) == {identity + '.' + args.version + '.nupkg' for identity in ids}")
    result = {'status': 'in-progress', 'verifiedAtUtc': datetime.datetime.now(datetime.timezone.utc).isoformat(),
              'repository': repo, 'version': args.version, 'source': args.source,
              'release': {'id': release['id'], 'url': release['html_url'], 'tagCommit': tag['sha'],
                          'immutable': release['immutable']},
              'candidate': {'artifactId': args.artifact_id, 'name': artifact['name'], 'digest': artifact['digest'],
                            'runId': run['id'], 'runAttempt': producing['attempt'], 'runLatestAttempt': run['run_attempt'], 'producerEvidence': producing, 'conclusion': run['conclusion']},
              'comparisonHelperSha256': hashlib.sha256((ROOT / 'scripts/release-package-content.py').read_bytes()).hexdigest(),
              'packages': []}
    receipt_path.write_text(json.dumps(result, indent=2) + '\n')
    for identity in ids:
        lower = identity.lower()
        registration_url = f'https://api.nuget.org/v3/registration5-gz-semver2/{lower}/{args.version}.json'
        registration = document(registration_url)
        require(registration['listed'] is True, "Release verification failed: registration['listed'] is True")
        catalog = document(registration['catalogEntry'])
        require(catalog['id'] == identity and catalog['version'] == args.version and catalog['listed'] is True, "Release verification failed: catalog['id'] == identity and catalog['version'] == args.version and catalog['listed'] is True")
        url = f'https://api.nuget.org/v3-flatcontainer/{lower}/{args.version}/{lower}.{args.version}.nupkg'
        public_bytes = download(url)
        require(catalog['packageHashAlgorithm'] == 'SHA512', "Release verification failed: catalog['packageHashAlgorithm'] == 'SHA512'")
        require(base64.b64encode(hashlib.sha512(public_bytes).digest()).decode() == catalog['packageHash'], "Release verification failed: base64.b64encode(hashlib.sha512(public_bytes).digest()).decode() == catalog['packageHash']")
        with zipfile.ZipFile(io.BytesIO(public_bytes)) as archive:
            names = [name for name in archive.namelist() if name.endswith('.nuspec')]
            require(len(names) == 1 and '.signature.p7s' in archive.namelist(), "Release verification failed: len(names) == 1 and '.signature.p7s' in archive.namelist()")
            metadata = ET.fromstring(archive.read(names[0])).find('{*}metadata')
            require(metadata.findtext('{*}id') == identity and metadata.findtext('{*}version') == args.version, "Release verification failed: metadata.findtext('{*}id') == identity and metadata.findtext('{*}version') == args.version")
            require(metadata.find('{*}repository').get('commit') == args.source, "Release verification failed: metadata.find('{*}repository').get('commit') == args.source")
        candidate_path = scratch / (identity + '.candidate.nupkg')
        public_path = scratch / (identity + '.public.nupkg')
        candidate_path.write_bytes(prepared[identity + '.' + args.version + '.nupkg'])
        public_path.write_bytes(public_bytes)
        compared = subprocess.run(['python3', str(ROOT / 'scripts/release-package-content.py'),
                                  'compare', str(candidate_path), str(public_path)], check=False)
        require(compared.returncode == 0, identity + ': original payload and native signature comparison failed')
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


if __name__ == '__main__':
    main()
