def require(condition, message):
    if not condition:
        raise ValueError(message)


def prepare_job_name(workflow):
    match = re.search(r'(?m)^  prepare-release:\s*$', workflow)
    require(match is not None, 'Source workflow has no prepare-release job.')
    block = workflow[match.end():]
    end = re.search(r'(?m)^  [A-Za-z0-9_-]+:', block)
    if end:
        block = block[:end.start()]
    name = re.search(r'(?m)^    name:\s*(.+)$', block)
    value = name.group(1).strip().strip('"\'') if name else 'prepare-release'
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
