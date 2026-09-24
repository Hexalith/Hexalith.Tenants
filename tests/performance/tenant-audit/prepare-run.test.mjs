import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { mkdtemp, readFile, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { after, test } from 'node:test';

import { prepareRun } from './prepare-run.mjs';

const temporaryDirectories = [];
after(async () => {
    await Promise.all(temporaryDirectories.map(directory => rm(directory, { recursive: true, force: true })));
});

test('baseline keeps 50 rows and rejects an untriggered 25-row selection', async () => {
    const result = await temporaryDirectory();
    assert.deepEqual(await prepareRun(result), { mode: 'baseline', uiPageSize: 50, seedManifestPageSize: 50 });
    assert.deepEqual(await prepareRun(result, '', '50'), { mode: 'baseline', uiPageSize: 50, seedManifestPageSize: 50 });
    await assert.rejects(prepareRun(result, '', '25'), /requires a validated fallback source/);
});

test('a complete authoritative percentile miss reuses the exact manifest with 25 UI rows', async () => {
    const source = await makeBaseline({ miss: true });
    const result = await temporaryDirectory();
    const mode = await prepareRun(result, source);
    assert.equal(mode.mode, 'fallback');
    assert.equal(mode.uiPageSize, 25);
    assert.equal(mode.seedManifestPageSize, 50);
    assert.equal(mode.validMisses.length, 1);
    assert.equal(await readFile(join(result, 'dataset-manifest.json'), 'utf8'),
        await readFile(join(source, 'dataset-manifest.json'), 'utf8'));
    await assert.rejects(prepareRun(result, source, '50'), /page size 25/);
    await assert.rejects(prepareRun(source, source), /directories must differ/);
});

test('passing baseline cannot trigger fallback', async () => {
    const source = await makeBaseline({ miss: false });
    await assert.rejects(prepareRun(await temporaryDirectory(), source), /no valid percentile miss/);
});

test('shared-machine or incomplete baseline cannot trigger fallback', async () => {
    const sharedSource = await makeBaseline({ miss: true, referenceRunner: false });
    await assert.rejects(prepareRun(await temporaryDirectory(), sharedSource), /approved idle dedicated runner/);

    const incompleteSource = await makeBaseline({ miss: true });
    const rawPath = join(incompleteSource, 'raw-desktop-batch-1.json');
    const raw = JSON.parse(await readFile(rawPath, 'utf8'));
    raw.samples.pop();
    await writeFile(rawPath, JSON.stringify(raw));
    await assert.rejects(prepareRun(await temporaryDirectory(), incompleteSource), /raw batch is incomplete/);
});

async function temporaryDirectory() {
    const directory = await mkdtemp(join(tmpdir(), 'tenant-audit-mode-'));
    temporaryDirectories.push(directory);
    return directory;
}

async function makeBaseline({ miss, referenceRunner = true }) {
    const directory = await temporaryDirectory();
    const anchor = Date.parse('2026-09-23T00:00:00.000Z');
    const dataset = {
        generatorRevision: 'tenant-audit-seed-v2', randomSeed: 5101,
        tenantId: 'audit-perf-fixture', anchorUtc: new Date(anchor).toISOString(),
        projectedAtUtc: new Date(anchor + 1000).toISOString(), pageSize: 50,
        entries: Array.from({ length: 500 }, (_, index) => ({
            reference: `audit-event-${String(index).padStart(6, '0')}`,
            timestampUtc: new Date(anchor - 30 * 86400000 + index * 30 * 86400000 / 499).toISOString(),
            category: index % 2 === 0 ? 'Access' : 'Administrative',
            eventType: index % 2 === 0 ? 'UserAddedToTenant' : 'TenantUpdated',
        })),
    };
    const hashSha256 = createHash('sha256').update(JSON.stringify(dataset)).digest('hex');
    await writeFile(join(directory, 'dataset-manifest.json'), JSON.stringify({ hashSha256, dataset }));
    await writeFile(join(directory, 'environment.json'), JSON.stringify({
        referenceRunner, measurementMode: 'contract', hardwareMatch: true,
        dedicatedRunnerDeclared: true, idlePreflight: true,
        cpuCount: 4, hostCpuCount: 4, memoryKiB: 8 * 1024 * 1024,
    }));

    const groups = [];
    const cases = [
        ['unfiltered', 'initial', 'initial-ready', 0],
        ...['next', 'previous'].flatMap((action, index) =>
            ['result', 'feedback'].map(observable => ['unfiltered', action, observable, index === 0 ? 1 : 0])),
        ...['access', 'administrative-seven-day'].flatMap(caseName =>
            ['apply', 'next', 'previous', 'reset'].flatMap(action =>
                ['result', 'feedback'].map(observable => [caseName, action, observable, action === 'next' ? 1 : 0]))),
    ];
    for (const viewport of ['desktop', 'phone']) {
        for (let batch = 1; batch <= 3; batch++) {
            const samples = [];
            for (const [caseName, action, observable, pageIndex] of cases) {
                const resultCase = action === 'reset' ? 'unfiltered' : caseName;
                const filtered = dataset.entries.filter(entry => resultCase === 'unfiltered'
                    || (resultCase === 'access' && entry.category === 'Access')
                    || (resultCase === 'administrative-seven-day' && entry.category === 'Administrative'
                        && Date.parse(entry.timestampUtc) >= anchor - 7 * 86400000));
                const responseCount = filtered.slice(pageIndex * 50, (pageIndex + 1) * 50).length;
                const nextCursorPresent = filtered.length > (pageIndex + 1) * 50;
                const limits = observable === 'initial-ready' ? [2500, 4000]
                    : observable === 'feedback' ? [200, 500] : [1500, 3000];
                const isMiss = miss && viewport === 'desktop' && batch === 1 && action === 'initial';
                const durationMs = isMiss ? 4500 : observable === 'initial-ready' ? 1000
                    : observable === 'feedback' ? 100 : 200;
                for (let iteration = 1; iteration <= 40; iteration++) {
                    samples.push({ viewport, batch, iteration, caseName, action, observable,
                        durationMs, responseCount, pageSize: 50, nextCursorPresent,
                        displayedRowCount: responseCount, failure: null });
                }
                groups.push({ viewport, batch, caseName, action, observable, count: 40,
                    p75: durationMs, p95: durationMs, budgetP75: limits[0], budgetP95: limits[1],
                    pass: !isMiss });
            }
            await writeFile(join(directory, `raw-${viewport}-batch-${batch}.json`),
                JSON.stringify({ viewport, batch, samples }));
        }
    }
    await writeFile(join(directory, 'summary.json'), JSON.stringify({
        scriptVersion: 'audit-performance-v3', uiPageSize: 50, fullContract: true,
        samplesPerBatch: 40, batchCount: 3, warmCount: 5, datasetHash: hashSha256,
        setupFailures: [], functionalGates: Array.from({ length: 10 }, () => ({ pass: true })), groups,
    }));
    return directory;
}
