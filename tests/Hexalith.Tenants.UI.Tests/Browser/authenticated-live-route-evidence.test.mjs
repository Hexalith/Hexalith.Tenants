import assert from 'node:assert/strict';
import { mkdtemp, readFile, readdir, rm, stat } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import test from 'node:test';
import {
    DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS,
    captureFailureDiagnostic,
    captureFailureScreenshot,
    captureLauncherFocusState,
    createLiveRouteCheckpoint,
    recordFailureDiagnostics,
} from './authenticated-live-route-evidence.mjs';

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

function createManualClock() {
    let nextId = 1;
    const active = new Map();
    const scheduled = [];
    const cleared = [];
    return {
        scheduled,
        cleared,
        activeCount: () => active.size,
        setTimer(callback, delay) {
            const id = nextId++;
            active.set(id, callback);
            scheduled.push({ id, delay });
            return id;
        },
        clearTimer(id) {
            cleared.push(id);
            active.delete(id);
        },
        fire(id) {
            const callback = active.get(id);
            assert.ok(callback, `expected active timer ${id}`);
            active.delete(id);
            callback();
        },
    };
}

test('captureFailureDiagnostic returns captured state and clears its deadline timer on success', async () => {
    const clock = createManualClock();
    let resolveCapture;
    const pending = captureFailureDiagnostic(
        () => new Promise(resolve => { resolveCapture = resolve; }),
        { setTimer: clock.setTimer, clearTimer: clock.clearTimer },
    );

    assert.equal(DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS, 5_000);
    assert.deepEqual(clock.scheduled, [{ id: 1, delay: 5_000 }]);
    assert.equal(clock.activeCount(), 1);

    resolveCapture({ available: true, launcherVisible: true });
    const result = await pending;
    assert.deepEqual(result, { available: true, launcherVisible: true });
    assert.deepEqual(clock.cleared, [1]);
    assert.equal(clock.activeCount(), 0);
});

test('captureFailureDiagnostic bounds a wedged capture to its deadline and returns safe fallback', async () => {
    const clock = createManualClock();
    let resolveCapture;
    const pending = captureFailureDiagnostic(
        () => new Promise(resolve => { resolveCapture = resolve; }),
        { setTimer: clock.setTimer, clearTimer: clock.clearTimer },
    );

    assert.deepEqual(clock.scheduled, [{ id: 1, delay: 5_000 }]);
    clock.fire(1);
    const result = await pending;
    assert.deepEqual(result, { available: false });
    assert.equal(clock.activeCount(), 0);

    // Late resolution after timeout must be ignored cleanly.
    resolveCapture({ available: true, leaked: 'private-sentinel' });
    await Promise.resolve();
});

test('captureFailureDiagnostic suppresses late rejection after timeout without unhandledRejection', async () => {
    const clock = createManualClock();
    const unhandled = [];
    const onUnhandled = reason => unhandled.push(reason);
    process.on('unhandledRejection', onUnhandled);
    try {
        let rejectCapture;
        const pending = captureFailureDiagnostic(
            () => new Promise((_, reject) => { rejectCapture = reject; }),
            { setTimer: clock.setTimer, clearTimer: clock.clearTimer },
        );

        clock.fire(1);
        assert.deepEqual(await pending, { available: false });

        rejectCapture(new Error('https://private.invalid/?token=late-rejection-sentinel'));
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(unhandled, []);
    } finally {
        process.off('unhandledRejection', onUnhandled);
    }
});

test('captureFailureDiagnostic handles rejection before deadline and synchronous throws safely', async () => {
    const rejectClock = createManualClock();
    const rejected = await captureFailureDiagnostic(
        () => Promise.reject(new Error('https://private.invalid/?token=rejected-sentinel')),
        { setTimer: rejectClock.setTimer, clearTimer: rejectClock.clearTimer },
    );
    assert.deepEqual(rejected, { available: false });
    assert.deepEqual(rejectClock.cleared, [1]);
    assert.equal(rejectClock.activeCount(), 0);

    const throwClock = createManualClock();
    const thrown = await captureFailureDiagnostic(
        () => { throw new Error('https://private.invalid/?token=sync-throw-sentinel'); },
        { setTimer: throwClock.setTimer, clearTimer: throwClock.clearTimer },
    );
    assert.deepEqual(thrown, { available: false });
    assert.deepEqual(throwClock.scheduled, []);
    assert.equal(throwClock.activeCount(), 0);
});

test('captureFailureDiagnostic hardens null options, invalid timeoutMs, timer throws, and non-record objects', async () => {
    const nullOptionsResult = await captureFailureDiagnostic(() => Promise.resolve({ available: true }), null);
    assert.deepEqual(nullOptionsResult, { available: true });

    for (const invalidTimeout of [0, -1, Number.NaN, Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY, 2_147_483_648, '5000']) {
        const clock = createManualClock();
        const result = await captureFailureDiagnostic(
            () => Promise.resolve({ available: true }),
            { timeoutMs: invalidTimeout, setTimer: clock.setTimer, clearTimer: clock.clearTimer },
        );
        assert.deepEqual(result, { available: true });
        assert.deepEqual(clock.scheduled, [{ id: 1, delay: DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS }]);
        assert.equal(clock.activeCount(), 0);
    }

    const unhandled = [];
    const onUnhandled = reason => unhandled.push(reason);
    process.on('unhandledRejection', onUnhandled);
    try {
        let rejectCapture;
        const setTimerThrows = await captureFailureDiagnostic(
            () => new Promise((_, reject) => { rejectCapture = reject; }),
            { setTimer: () => { throw new Error('timer-registration-failed'); } },
        );
        assert.deepEqual(setTimerThrows, { available: false });
        rejectCapture(new Error('late-rejection-after-setTimer-throw'));
        await new Promise(resolve => setImmediate(resolve));
        assert.deepEqual(unhandled, []);
    } finally {
        process.off('unhandledRejection', onUnhandled);
    }

    const clock = createManualClock();
    const clearTimerThrows = await captureFailureDiagnostic(
        () => Promise.resolve({ available: true, kept: true }),
        {
            setTimer: clock.setTimer,
            clearTimer: () => { throw new Error('timer-cleanup-failed'); },
        },
    );
    assert.deepEqual(clearTimerThrows, { available: true, kept: true });

    for (const invalidValue of [['private-item'], new Error('private-error-sentinel'), null, 'private-text']) {
        const valueClock = createManualClock();
        const result = await captureFailureDiagnostic(
            () => Promise.resolve(invalidValue),
            { setTimer: valueClock.setTimer, clearTimer: valueClock.clearTimer },
        );
        assert.deepEqual(result, { available: false });
        assert.equal(valueClock.activeCount(), 0);
    }
});

test('recordFailureDiagnostics bounds wedged focus and readiness captures and preserves checkpoint order', async () => {
    const clock = createManualClock();
    const evidence = {};
    const checkpoints = [];
    const pending = recordFailureDiagnostics(evidence, {
        phase: 'contextual-audit-back-tenant-detail-focus',
        checkpoint: async currentPhase => {
            checkpoints.push({ phase: currentPhase, focus: evidence.focus, readiness: evidence.readiness });
        },
        captureFocus: () => new Promise(() => {}),
        captureReadiness: () => new Promise(() => {}),
        diagnosticOptions: { setTimer: clock.setTimer, clearTimer: clock.clearTimer },
    });

    assert.deepEqual(clock.scheduled, [{ id: 1, delay: DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS }]);
    clock.fire(1);
    await new Promise(resolve => setImmediate(resolve));

    assert.deepEqual(checkpoints, [{
        phase: 'contextual-audit-back-tenant-detail-focus',
        focus: { available: false },
        readiness: undefined,
    }]);
    assert.deepEqual(clock.scheduled, [
        { id: 1, delay: DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS },
        { id: 2, delay: DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS },
    ]);
    clock.fire(2);
    await pending;

    assert.deepEqual(evidence.focus, { available: false });
    assert.deepEqual(evidence.readiness, { available: false });
    assert.equal(clock.activeCount(), 0);
});

test('captureFailureScreenshot applies bounded timeout and never clobbers primary failures', async () => {
    const calls = [];
    const okClock = createManualClock();
    const ok = await captureFailureScreenshot(
        {
            screenshot: async options => {
                calls.push(options);
                return Buffer.from('png');
            },
        },
        '/tmp/evidence.png',
        { setTimer: okClock.setTimer, clearTimer: okClock.clearTimer },
    );
    assert.deepEqual(ok, { available: true });
    assert.deepEqual(calls, [{ path: '/tmp/evidence.png', timeout: DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS }]);
    assert.equal(okClock.activeCount(), 0);

    const wedgedClock = createManualClock();
    const wedgedPending = captureFailureScreenshot(
        { screenshot: () => new Promise(() => {}) },
        '/tmp/wedged.png',
        { setTimer: wedgedClock.setTimer, clearTimer: wedgedClock.clearTimer },
    );
    wedgedClock.fire(1);
    assert.deepEqual(await wedgedPending, { available: false });
    assert.equal(wedgedClock.activeCount(), 0);

    const primary = Object.assign(new Error('primary'), { safeLabel: 'contextual-audit-launcher-authorized' });
    let rethrown;
    try {
        try {
            throw primary;
        } catch (error) {
            const rejectClock = createManualClock();
            const captureResult = await captureFailureScreenshot(
                { screenshot: () => Promise.reject(new Error('screenshot-failed-sentinel')) },
                '/tmp/rejected.png',
                { setTimer: rejectClock.setTimer, clearTimer: rejectClock.clearTimer },
            );
            assert.deepEqual(captureResult, { available: false });
            assert.equal(rejectClock.activeCount(), 0);
            throw error;
        }
    } catch (error) {
        rethrown = error;
    }
    assert.equal(rethrown?.safeLabel, 'contextual-audit-launcher-authorized');
});
