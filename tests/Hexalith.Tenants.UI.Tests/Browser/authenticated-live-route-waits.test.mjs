import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { createRequire } from 'node:module';
import test from 'node:test';
import { createFunctionalNavigation, functionalWaitMilliseconds } from './authenticated-live-route-waits.mjs';

const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const { chromium } = require('@playwright/test');
const check = (condition, label) => assert.ok(condition, label);

function manualClock() {
    let timestamp = 0;
    const timers = new Map();
    return {
        now: () => timestamp,
        setTimer: (action, delay) => { const timer = {}; timers.set(timer, { action, due: timestamp + delay }); return timer; },
        clearTimer: timer => timers.delete(timer),
        advance(milliseconds, fire = true) {
            timestamp += milliseconds;
            if (fire) for (const [timer, entry] of timers) if (entry.due <= timestamp) { timers.delete(timer); entry.action(); }
        },
        pending: () => timers.size,
    };
}

test('functional polling remains finite and reports the original failed readiness assertion', async t => {
    const clock = manualClock();
    t.mock.method(Date, 'now', () => { throw new Error('wall-clock-must-not-control-deadlines'); });
    const limits = [];
    const page = {
        setDefaultTimeout: value => limits.push(value),
        setDefaultNavigationTimeout: value => limits.push(value),
        waitForTimeout: async value => { clock.advance(value); },
    };
    const { waitUntil } = createFunctionalNavigation(page, check, clock);
    assert.deepEqual(limits, [150_000, 150_000]);
    await assert.rejects(waitUntil(async () => false, 'authoritative-row-required'), /authoritative-row-required/);
    assert.equal(clock.now(), functionalWaitMilliseconds);
    assert.equal(clock.pending(), 0);
});

test('pending and late readiness predicates retain their original deadline failure label', async () => {
    for (const phase of ['pending', 'late-success']) {
        const clock = manualClock();
        const page = { setDefaultTimeout() {}, setDefaultNavigationTimeout() {} };
        const { waitUntil } = createFunctionalNavigation(page, check, clock);
        let calls = 0;
        const waiting = waitUntil(() => {
            calls++;
            if (phase === 'pending') return new Promise(() => {});
            clock.advance(functionalWaitMilliseconds + 1, false);
            return Promise.resolve(true);
        }, 'authoritative-row-required');
        if (phase === 'pending') clock.advance(functionalWaitMilliseconds);
        await assert.rejects(waiting, /authoritative-row-required/);
        assert.equal(calls, 1);
        assert.equal(clock.pending(), 0);
    }
});

test('navigation and network idle share one finite monotonic allowance and refuse cumulative late success', async () => {
    for (const finalDelay of [49_000, 51_000]) {
        const clock = manualClock();
        const allowances = [];
        const response = { status: () => 200 };
        const page = {
            setDefaultTimeout() {}, setDefaultNavigationTimeout() {},
            goto: async (_, options) => { allowances.push(options.timeout); clock.advance(100_000, false); return response; },
            waitForLoadState: async (state, options) => {
                assert.equal(state, 'networkidle'); allowances.push(options.timeout); clock.advance(finalDelay, false);
            },
        };
        const navigation = createFunctionalNavigation(page, check, clock).open('http://synthetic.invalid');
        if (finalDelay === 49_000) assert.equal(await navigation, response);
        else await assert.rejects(navigation, /functional-navigation-deadline/);
        assert.deepEqual(allowances, [150_000, 50_000]);
        assert.equal(clock.pending(), 0);
    }
});

test('real functional navigation accepts 36+55-second prerender and retained 31-second pages with denial', { timeout: 125_000 }, async () => {
    // A synthetic loopback transport contract; this is not live business authorization evidence.
    const timers = new Set();
    const counts = new Map();
    const later = (action, delay = 31_000) => { const timer = setTimeout(() => { timers.delete(timer); action(); }, delay); timers.add(timer); };
    const server = createServer((request, response) => {
        counts.set(request.url, (counts.get(request.url) || 0) + 1);
        if (!request.headers.cookie?.includes('contract-session=synthetic') || request.url === '/denied') {
            response.writeHead(403, { 'Content-Type': 'text/html' });
            response.end('<main data-testid="scope-denied">Denied</main>'); return;
        }
        const ready = '<main data-testid="tenants-workspace"><span class="truth-state-badge--current" data-testid="authoritative-row">Current</span></main>';
        if (request.url === '/composed-prerender') {
            // Own the real response for the sequential initial-list and detail-enrichment stages.
            later(() => later(() => { response.writeHead(200, { 'Content-Type': 'text/html' }); response.end(ready); }, 55_000), 36_000);
        } else if (request.url === '/slow-navigation') {
            later(() => { response.writeHead(200, { 'Content-Type': 'text/html' }); response.end(ready); });
        } else {
            response.writeHead(200, { 'Content-Type': 'text/html' });
            response.end(`<main data-testid="tenants-workspace"></main><script>setTimeout(() => { document.querySelector("main").innerHTML = '<span class="truth-state-badge--current" data-testid="authoritative-row">Current</span>'; },31000)</script>`);
        }
    });
    let browser;
    try {
        await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
        const base = `http://127.0.0.1:${server.address().port}`;
        browser = await chromium.launch({ headless: true });
        const context = await browser.newContext();
        await context.addCookies([{ name: 'contract-session', value: 'synthetic', url: base }]);
        const navigation = await context.newPage();
        const readiness = await context.newPage();
        const prerender = await context.newPage();
        const nav = createFunctionalNavigation(navigation, check);
        const ready = createFunctionalNavigation(readiness, check);
        const composed = createFunctionalNavigation(prerender, check);
        const started = performance.now();
        await Promise.all([
            (async () => {
                const response = await nav.open(`${base}/slow-navigation`);
                assert.equal(response.status(), 200);
                await navigation.getByTestId('authoritative-row').waitFor({ state: 'visible' });
            })(),
            (async () => {
                const response = await ready.open(`${base}/slow-readiness`);
                assert.equal(response.status(), 200);
                await ready.waitUntil(() => readiness.getByTestId('authoritative-row').isVisible(), 'authoritative-row-required');
            })(),
            (async () => {
                const response = await composed.open(`${base}/composed-prerender`);
                assert.equal(response.status(), 200);
                assert.equal(await prerender.getByTestId('authoritative-row').isVisible(), true);
                assert.ok(performance.now() - started >= 91_000);
            })(),
        ]);
        assert.ok(performance.now() - started >= 31_000);
        assert.equal(counts.get('/slow-navigation'), 1);
        assert.equal(counts.get('/slow-readiness'), 1);
        assert.equal(counts.get('/composed-prerender'), 1);
        const denied = await nav.open(`${base}/denied`);
        assert.equal(denied.status(), 403);
        assert.equal(await navigation.getByTestId('authoritative-row').count(), 0);
        assert.equal(counts.get('/denied'), 1);
    } finally {
        await browser?.close();
        for (const timer of timers) clearTimeout(timer);
        server.closeAllConnections();
        await new Promise(resolve => server.close(resolve));
    }
});
