import assert from 'node:assert/strict';
import { mkdtemp, readFile, readdir, rm, stat } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import { captureLauncherFocusState, createLiveRouteCheckpoint } from './authenticated-live-route-evidence.mjs';

const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const { chromium } = require('@playwright/test');

test('atomic progress retains completed cases and the current phase before pending work', async () => {
    const directory = await mkdtemp(join(tmpdir(), 'tenants-browser-checkpoint-'));
    let browser;
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

        // An owned synthetic DOM exercises the same private-locator capture used on live failure.
        browser = await chromium.launch({ headless: true });
        const page = await browser.newPage();
        await page.setContent(`<main data-testid="tenants-detail">
            <h1 tabindex="-1">private-heading-text</h1>
            <span data-testid="tenants-detail-truth-state" class="truth-state-badge--current"></span>
            <a id="private-tenant-id" href="https://private.invalid/?token=private-token">private-link-text</a>
            <button>private-button-text</button><input value="private-input-value"><div tabindex="0">private-other</div></main>`);
        const launcher = page.locator('#private-tenant-id');
        await page.locator('h1').focus();
        const focus = await captureLauncherFocusState(launcher);
        assert.deepEqual(focus, { available: true, launcherCount: 1, launcherAttached: true, launcherVisible: true,
            launcherEnabled: true, launcherHasDestination: true, expectedFocus: false,
            detailSurfacePresent: true, detailCurrentState: true, activeElementCategory: 'heading' });
        assert.equal(JSON.stringify(focus).includes('private'), false);
        for (const [selector, category] of [['a', 'link'], ['button', 'button'], ['input', 'input'], ['div', 'other']]) {
            await page.locator(selector).focus();
            const state = await captureLauncherFocusState(launcher);
            assert.equal(state.activeElementCategory, category);
            assert.equal(state.expectedFocus, category === 'link');
        }
        await page.evaluate(() => { document.body.tabIndex = -1; document.body.focus(); });
        assert.equal((await captureLauncherFocusState(launcher)).activeElementCategory, 'body');
        await launcher.evaluate(element => { element.setAttribute('aria-disabled', 'true'); element.removeAttribute('href'); element.style.display = 'none'; });
        const restricted = await captureLauncherFocusState(launcher);
        assert.equal(restricted.launcherAttached, true);
        assert.equal(restricted.launcherVisible, false);
        assert.equal(restricted.launcherEnabled, false);
        assert.equal(restricted.launcherHasDestination, false);
        await launcher.evaluate(element => element.remove());
        const missing = await captureLauncherFocusState(launcher);
        assert.equal(missing.launcherCount, 0);
        assert.equal(missing.launcherAttached, false);
        assert.equal(missing.expectedFocus, false);
        await page.setContent('<a>synthetic</a>'.repeat(101));
        assert.equal((await captureLauncherFocusState(page.locator('a'))).launcherCount, 100);
    } finally {
        await browser?.close();
        await rm(directory, { recursive: true, force: true });
    }
});
