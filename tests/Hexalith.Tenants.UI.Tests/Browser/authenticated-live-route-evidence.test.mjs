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

test('audit Back restores the detail launcher when its Fluent host delegates focus', async () => {
    const source = await readFile(new URL('../../../src/Hexalith.Tenants.UI/wwwroot/js/tenantsFocus.js', import.meta.url), 'utf8');
    const browser = await chromium.launch({ headless: true });
    try {
        const page = await browser.newPage();
        await page.setContent(`<div data-testid="tenants-detail-audit-origin" data-audit-focus-terminal="true">
            <header data-testid="tenants-detail-identity"><h1 id="tenants-detail-identity" tabindex="-1">Tenant</h1></header>
            <fluent-anchor-button style="display:inline-block" href="/audit"
                data-testid="tenants-audit-entrypoint" data-audit-source="tenant-detail"></fluent-anchor-button>
            </div>`);
        const result = await page.evaluate(async script => {
            customElements.define('fluent-anchor-button', class extends HTMLElement {
                constructor() {
                    super();
                    this.attachShadow({ mode: 'open' }).innerHTML = '<a href="/audit">Audit</a>';
                }
                focus() { /* This host delegates focus to its shadow anchor. */ }
            });
            const module = await import(`data:text/javascript;charset=utf-8,${encodeURIComponent(script)}`);
            const outcome = await module.restoreAuditFocus('tenants-detail-identity',
                'tenants-detail-audit-origin', 'tenants-detail-identity');
            return { outcome, focused: document.activeElement === document.querySelector('fluent-anchor-button') };
        }, source);
        assert.deepEqual(result, { outcome: 'found', focused: true });
    } finally {
        await browser.close();
    }
});
