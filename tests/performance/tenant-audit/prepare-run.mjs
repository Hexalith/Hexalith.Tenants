import { createHash } from 'node:crypto';
import { copyFile, readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const viewports = ['desktop', 'phone'];
const measurements = [
    ['unfiltered', 'initial', 'initial-ready', 0],
    ...['next', 'previous'].flatMap((action, index) =>
        ['result', 'feedback'].map(observable => ['unfiltered', action, observable, index === 0 ? 1 : 0])),
    ...['access', 'administrative-seven-day'].flatMap(caseName =>
        ['apply', 'next', 'previous', 'reset'].flatMap(action =>
            ['result', 'feedback'].map(observable => [caseName, action, observable, action === 'next' ? 1 : 0]))),
];

export async function prepareRun(resultDirectory, fallbackDirectory = '', requestedPageSize = '') {
    if (requestedPageSize && requestedPageSize !== (fallbackDirectory ? '25' : '50')) {
        throw new Error('UI page size 25 requires a validated fallback source; baseline page size is 50.');
    }
    if (!fallbackDirectory) {
        return { mode: 'baseline', uiPageSize: 50, seedManifestPageSize: 50 };
    }

    const source = resolve(fallbackDirectory);
    const destination = resolve(resultDirectory);
    if (source === destination) {
        throw new Error('Fallback source and result directories must differ.');
    }
    const manifest = await readJson(resolve(source, 'dataset-manifest.json'));
    const summary = await readJson(resolve(source, 'summary.json'));
    const environment = await readJson(resolve(source, 'environment.json'));
    const dataset = manifest.dataset;
    const hash = createHash('sha256').update(JSON.stringify(dataset)).digest('hex');
    requireValid(hash === manifest.hashSha256 && summary.datasetHash === hash,
        'Original dataset hash is inconsistent.');
    requireValid(dataset?.pageSize === 50 && dataset?.entries?.length === 500,
        'Original manifest must contain the approved 500-entry, 50-row baseline.');
    requireValid(summary.scriptVersion === 'audit-performance-v3' && summary.uiPageSize === 50,
        'Original run must be a 50-row measurement by the current browser script.');
    requireValid(summary.fullContract === true && summary.samplesPerBatch === 40
        && summary.batchCount === 3 && summary.warmCount === 5
        && Array.isArray(summary.setupFailures) && summary.setupFailures.length === 0,
    'Original run is not a complete 3×40 contract batch.');
    requireValid(Array.isArray(summary.functionalGates) && summary.functionalGates.length === 10
        && summary.functionalGates.every(gate => gate.pass === true),
    'Original browser functional gates did not all pass.');
    requireValid(environment.referenceRunner === true && environment.measurementMode === 'contract'
        && environment.hardwareMatch === true && environment.dedicatedRunnerDeclared === true
        && environment.idlePreflight === true && environment.cpuCount === 4
        && environment.hostCpuCount === 4 && environment.memoryKiB >= 7340032
        && environment.memoryKiB <= 9437184,
    'Original run was not recorded on the approved idle dedicated runner.');

    const expected = new Map(measurements.map(([caseName, action, observable, pageIndex]) =>
        [[caseName, action, observable].join('|'), pageIndex]));
    requireValid(expected.size === 21 && Array.isArray(summary.groups)
        && summary.groups.length === viewports.length * 3 * expected.size,
    'Original summary has missing or duplicate observable groups.');
    const summaryGroups = new Map();
    for (const group of summary.groups) {
        const key = [group.viewport, group.batch, group.caseName, group.action, group.observable].join('|');
        requireValid(viewports.includes(group.viewport) && Number.isInteger(group.batch)
            && group.batch >= 1 && group.batch <= 3
            && expected.has([group.caseName, group.action, group.observable].join('|'))
            && !summaryGroups.has(key), 'Original summary contains an unexpected or duplicate group.');
        summaryGroups.set(key, group);
    }

    const misses = [];
    for (const viewport of viewports) {
        for (let batch = 1; batch <= 3; batch++) {
            const raw = await readJson(resolve(source, `raw-${viewport}-batch-${batch}.json`));
            requireValid(raw.viewport === viewport && raw.batch === batch
                && Array.isArray(raw.samples) && raw.samples.length === expected.size * 40,
            'Original raw batch is incomplete.');
            const samplesByGroup = new Map();
            for (const sample of raw.samples) {
                const key = [sample.caseName, sample.action, sample.observable].join('|');
                requireValid(sample.viewport === viewport && sample.batch === batch && expected.has(key)
                    && Number.isInteger(sample.iteration) && sample.iteration >= 1 && sample.iteration <= 40
                    && sample.failure === null && Number.isFinite(sample.durationMs)
                    && sample.durationMs >= 0 && sample.pageSize === 50,
                'Original raw batch contains a failed, invalid, or wrong-page-size sample.');
                const pageIndex = expected.get(key);
                const expectedCount = rowsFor(dataset, sample.caseName, sample.action, pageIndex);
                const nextCount = rowsFor(dataset, sample.caseName, sample.action, pageIndex + 1);
                requireValid(sample.responseCount === expectedCount && sample.displayedRowCount === expectedCount
                    && sample.nextCursorPresent === (nextCount > 0),
                'Original raw sample has incorrect rows or paging metadata.');
                const group = samplesByGroup.get(key) ?? [];
                requireValid(!group.some(existing => existing.iteration === sample.iteration),
                    'Original raw batch repeats an iteration.');
                group.push(sample);
                samplesByGroup.set(key, group);
            }
            for (const [key] of expected) {
                const samples = samplesByGroup.get(key) ?? [];
                requireValid(samples.length === 40, 'Original raw group lacks 40 samples.');
                const values = samples.map(sample => sample.durationMs).sort((a, b) => a - b);
                const p75 = values[29];
                const p95 = values[37];
                const [caseName, action, observable] = key.split('|');
                const limits = observable === 'initial-ready' ? [2500, 4000]
                    : observable === 'feedback' ? [200, 500] : [1500, 3000];
                const group = summaryGroups.get([viewport, batch, key].join('|'));
                const miss = p75 > limits[0] || p95 > limits[1];
                requireValid(group && group.count === 40 && Math.abs(group.p75 - p75) < 0.001
                    && Math.abs(group.p95 - p95) < 0.001 && group.budgetP75 === limits[0]
                    && group.budgetP95 === limits[1] && group.pass === !miss,
                'Original summary percentiles disagree with the raw samples.');
                if (miss) {
                    misses.push({ viewport, batch, caseName, action, observable, p75, p95 });
                }
            }
        }
    }
    requireValid(misses.length > 0, 'Original run has no valid percentile miss; fallback is not triggered.');
    await copyFile(resolve(source, 'dataset-manifest.json'), resolve(destination, 'dataset-manifest.json'));
    return { mode: 'fallback', uiPageSize: 25, seedManifestPageSize: 50,
        originalDatasetHash: hash, originalRunDirectory: source, validMisses: misses };
}

function rowsFor(dataset, caseName, action, pageIndex) {
    const resultCase = action === 'reset' ? 'unfiltered' : caseName;
    const from = Date.parse(dataset.anchorUtc) - 7 * 24 * 60 * 60 * 1000;
    const entries = dataset.entries.filter(entry => resultCase === 'unfiltered'
        || (resultCase === 'access' && entry.category === 'Access')
        || (resultCase === 'administrative-seven-day' && entry.category === 'Administrative'
            && Date.parse(entry.timestampUtc) >= from
            && Date.parse(entry.timestampUtc) <= Date.parse(dataset.anchorUtc)));
    return entries.slice(pageIndex * 50, (pageIndex + 1) * 50).length;
}

async function readJson(path) {
    return JSON.parse(await readFile(path, 'utf8'));
}

function requireValid(condition, message) {
    if (!condition) throw new Error(message);
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
    try {
        const mode = await prepareRun(process.argv[2], process.argv[3] ?? '', process.argv[4] ?? '');
        process.stdout.write(`${JSON.stringify(mode, null, 2)}\n`);
    } catch (error) {
        process.stderr.write(`Fallback setup rejected: ${error instanceof Error ? error.message : 'unknown error'}\n`);
        process.exitCode = 1;
    }
}
