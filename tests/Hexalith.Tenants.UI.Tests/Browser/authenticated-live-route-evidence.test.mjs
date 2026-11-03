import assert from 'node:assert/strict';
import { mkdtemp, readFile, readdir, rm, stat } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { createLiveRouteCheckpoint } from './authenticated-live-route-evidence.mjs';

test('atomic progress retains completed cases and the current phase before pending work', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'tenants-browser-checkpoint-'));
    try {
        const output = join(directory, 'result.json');
        let now = 0;
        const evidence = { mode: 'synthetic checkpoint control', cases: [], passed: false };
        const checkpoint = createLiveRouteCheckpoint(output, evidence, () => now);
        await checkpoint('protected-route-1');
        evidence.cases.push({ name: 'protected-route-1', passed: true });
        now = 480_000;
        await checkpoint('tenant-list-layout-768-none-ready');
        const saved = JSON.parse(await readFile(output, 'utf8'));
        assert.deepEqual(saved.cases, [{ name: 'protected-route-1', passed: true }]);
        assert.deepEqual(saved.progress, { phase: 'tenant-list-layout-768-none-ready', completedCases: 1, elapsedMilliseconds: 480_000 });
        assert.equal(saved.passed, false);
        assert.equal(saved.finishedUtc, undefined);
        assert.deepEqual(await readdir(directory), ['result.json']);
        if (process.platform !== 'win32') assert.equal((await stat(output)).mode & 0o777, 0o600);
        await assert.rejects(checkpoint('https://private.invalid/?token=sentinel'), /invalid-evidence-phase/);
        assert.equal(await readFile(output, 'utf8'), JSON.stringify(saved, null, 2) + '\n');
    } finally {
        await rm(directory, { recursive: true, force: true });
    }
});
