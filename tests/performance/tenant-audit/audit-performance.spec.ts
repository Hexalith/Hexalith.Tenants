import { chromium, expect, test, type Browser, type BrowserContext, type Page } from '@playwright/test';
import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { resolve } from 'node:path';

type Entry = { reference: string; timestampUtc: string; category: string; eventType: string };
type Dataset = { anchorUtc: string; tenantId: string; pageSize: number; entries: Entry[] };
type CaseName = 'unfiltered' | 'access' | 'administrative-seven-day';
type ActionName = 'initial' | 'apply' | 'reset' | 'next' | 'previous';
type Observable = 'initial-ready' | 'result' | 'feedback';
type Sample = {
    viewport: string; batch: number; iteration: number; caseName: CaseName; action: ActionName;
    observable: Observable; durationMs: number | null; responseCount: number | null;
    pageSize: number | null; nextCursorPresent: boolean | null; displayedRowCount: number | null;
    failure: string | null;
};
type FunctionalGate = { viewport: string; gate: string; pass: boolean; failure: string | null };

const baseUrl = requireEnv('AUDIT_PERF_BASE_URL').replace(/\/$/, '');
const username = requireEnv('AUDIT_PERF_USERNAME');
const password = requireEnv('AUDIT_PERF_PASSWORD');
const manifestPath = requireEnv('AUDIT_PERF_MANIFEST');
const resultDir = resolve(requireEnv('AUDIT_PERF_RESULT_DIR'));
const samplesPerBatch = Number(process.env.AUDIT_PERF_SAMPLES ?? '40');
const batchCount = Number(process.env.AUDIT_PERF_BATCHES ?? '3');
const warmCount = Number(process.env.AUDIT_PERF_WARMUPS ?? '5');
const uiPageSize = Number(process.env.AUDIT_PERF_PAGE_SIZE ?? '50');
const fullContract = samplesPerBatch === 40 && batchCount === 3 && warmCount === 5;
const viewports = [
    { name: 'desktop', width: 1365, height: 768 },
    { name: 'phone', width: 390, height: 844 },
];

test('authenticated tenant audit performance contract', async () => {
    const manifest = JSON.parse(await readFile(manifestPath, 'utf8')) as { hashSha256: string; dataset: Dataset };
    const canonical = JSON.stringify(manifest.dataset);
    // The C# generator hashes its compact JSON representation. A mismatch makes the run invalid.
    expect(createHash('sha256').update(canonical).digest('hex')).toBe(manifest.hashSha256);
    expect(manifest.dataset.entries).toHaveLength(500);
    expect(manifest.dataset.pageSize).toBe(50); // The original seed manifest never changes for fallback.
    expect([25, 50]).toContain(uiPageSize);
    const runModePath = process.env.AUDIT_PERF_RUN_MODE;
    const runMode = runModePath ? JSON.parse(await readFile(runModePath, 'utf8')) as {
        mode: string; uiPageSize: number; originalDatasetHash?: string; validMisses?: unknown[]
    } : null;
    if (uiPageSize === 25) {
        expect(runMode?.mode).toBe('fallback');
        expect(runMode?.originalDatasetHash).toBe(manifest.hashSha256);
        expect(runMode?.validMisses?.length).toBeGreaterThan(0);
    } else if (runMode) {
        expect(runMode.mode).toBe('baseline');
    }
    expect(runMode?.uiPageSize ?? 50).toBe(uiPageSize);
    await mkdir(resultDir, { recursive: true });

    const all: Sample[] = [];
    const setupFailures: string[] = [];
    const functionalGates: FunctionalGate[] = [];
    let chromiumVersion: string | null = null;
    for (const viewport of viewports) {
        try {
            const warmBrowser = await chromium.launch();
            try {
                chromiumVersion ??= warmBrowser.version();
                const session = await authenticate(warmBrowser, viewport);
                functionalGates.push(...await checkFunctionalGates(warmBrowser, session, viewport,
                    manifest.dataset));
                for (let i = 0; i < warmCount; i++) {
                    const context = await warmBrowser.newContext({ viewport, storageState: session, ignoreHTTPSErrors: true });
                    try {
                        const page = await context.newPage();
                        await page.goto(`${baseUrl}/tenants/${manifest.dataset.tenantId}/audit`, { waitUntil: 'domcontentloaded' });
                        await assertRows(page, pageEntries(manifest.dataset, 'unfiltered', 0));
                    } finally {
                        await context.close();
                    }
                }
            } finally {
                await warmBrowser.close();
            }
        } catch (error) {
            setupFailures.push(`${viewport.name} warmup: ${safeError(error)}`);
            continue;
        }

        for (let batch = 1; batch <= batchCount; batch++) {
            const batchSamples: Sample[] = [];
            let browser: Browser | undefined;
            try {
                browser = await chromium.launch(); // An independent browser process for every batch.
                const session = await authenticate(browser, viewport);
                for (let iteration = 1; iteration <= samplesPerBatch; iteration++) {
                    const context = await browser.newContext({ viewport, storageState: session, ignoreHTTPSErrors: true });
                    try {
                        const page = await context.newPage();
                        await measureInitial(page, manifest.dataset, viewport.name, batch, iteration, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'unfiltered', 'next', 1, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'unfiltered', 'previous', 0, batchSamples);
                        await setFilters(page, 'access', manifest.dataset);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'access', 'apply', 0, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'access', 'next', 1, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'access', 'previous', 0, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'access', 'reset', 0, batchSamples);
                        await setFilters(page, 'administrative-seven-day', manifest.dataset);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'administrative-seven-day', 'apply', 0, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'administrative-seven-day', 'next', 1, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'administrative-seven-day', 'previous', 0, batchSamples);
                        await measureAction(page, manifest.dataset, viewport.name, batch, iteration, 'administrative-seven-day', 'reset', 0, batchSamples);
                    } catch (error) {
                        // Preserve the failed iteration, including failures before an observable was sampled.
                        batchSamples.push({ viewport: viewport.name, batch, iteration, caseName: 'unfiltered',
                            action: 'initial', observable: 'initial-ready', durationMs: null, responseCount: null,
                            pageSize: null, nextCursorPresent: null, displayedRowCount: null, failure: safeError(error) });
                    } finally {
                        await context.close();
                    }
                }
            } catch (error) {
                setupFailures.push(`${viewport.name} batch ${batch}: ${safeError(error)}`);
            } finally {
                await browser?.close();
                all.push(...batchSamples);
                await writeFile(resolve(resultDir, `raw-${viewport.name}-batch-${batch}.json`),
                    JSON.stringify({ viewport: viewport.name, batch, samples: batchSamples }, null, 2));
            }
        }
    }

    const groups = summarize(all, samplesPerBatch);
    const result = { scriptVersion: 'audit-performance-v3', chromiumVersion, fullContract, samplesPerBatch, batchCount,
        warmCount, uiPageSize, seedManifestPageSize: manifest.dataset.pageSize, datasetHash: manifest.hashSha256,
        setupFailures, functionalGates, groups };
    await writeFile(resolve(resultDir, 'summary.json'), JSON.stringify(result, null, 2));
    expect(setupFailures, 'warmup and batch setup must complete').toEqual([]);
    expect(functionalGates.length).toBe(2 * 5);
    expect(functionalGates.every(gate => gate.pass), 'browser functional gates must pass').toBe(true);
    expect(fullContract, 'smoke settings cannot satisfy the approved contract').toBe(true);
    expect(groups.length).toBe(2 * batchCount * 21); // Initial plus ten actions with result and feedback.
    expect(groups.every(group => group.pass), 'every batch, case, action, and observable must pass').toBe(true);
});

async function checkFunctionalGates(browser: Browser, session: Awaited<ReturnType<BrowserContext['storageState']>>,
    viewport: { name: string; width: number; height: number }, dataset: Dataset): Promise<FunctionalGate[]> {
    const gates: FunctionalGate[] = [];
    const context = await browser.newContext({ viewport, storageState: session, ignoreHTTPSErrors: true, locale: 'en-US' });
    try {
        const page = await context.newPage();
        await page.goto(`${baseUrl}/tenants/${dataset.tenantId}/audit`, { waitUntil: 'domcontentloaded' });
        await assertRows(page, pageEntries(dataset, 'unfiltered', 0));
        await gate('grid semantics and critical fields', async () => {
            await expect(page.getByRole('grid')).toBeVisible();
            for (const field of ['timestamp', 'actor', 'freshness', 'reference']) {
                await expect(page.locator(`[data-testid="tenants-audit-row-${field}"]`)).toHaveCount(uiPageSize);
            }
        });
        await gate('keyboard paging and server order', async () => {
            const next = page.locator('[data-testid="tenants-audit-next"]');
            await next.focus();
            await expect(next).toBeFocused();
            await next.press('Enter');
            await assertRows(page, pageEntries(dataset, 'unfiltered', 1));
            await page.locator('[data-testid="tenants-audit-previous"]').press('Enter');
            await assertRows(page, pageEntries(dataset, 'unfiltered', 0));
        });
        await gate('forced colors and reduced motion', async () => {
            await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
            expect(await page.evaluate(() => matchMedia('(forced-colors: active)').matches)).toBe(true);
            expect(await page.evaluate(() => matchMedia('(prefers-reduced-motion: reduce)').matches)).toBe(true);
            await expect(page.locator('[data-testid="tenants-audit-grid"]')).toBeVisible();
        });
        await gate('phone correction safety and support-safe output', async () => {
            if (viewport.name === 'phone') {
                await expect(page.locator('[data-testid="tenants-correction-start"], [data-testid="tenants-correction-role"]'))
                    .toHaveCount(0);
                await expect.poll(() => page.locator('[data-testid="tenants-correction-mobile-read-only"]').count())
                    .toBeGreaterThan(0);
            }
            const gridText = await page.locator('[data-testid="tenants-audit-grid"]').innerText();
            expect(gridText).not.toMatch(/\b(?:Bearer|Authorization:|password=|secret=|token=)/i);
        });
    } finally {
        await context.close();
    }
    const french = await browser.newContext({ viewport, storageState: session, ignoreHTTPSErrors: true, locale: 'fr-FR' });
    try {
        await gate('French localized current grid', async () => {
            const page = await french.newPage();
            await page.goto(`${baseUrl}/tenants/${dataset.tenantId}/audit`, { waitUntil: 'domcontentloaded' });
            await assertRows(page, pageEntries(dataset, 'unfiltered', 0));
            await expect(page.getByText('Horodatage', { exact: true }).first()).toBeVisible();
        });
    } finally {
        await french.close();
    }
    return gates;

    async function gate(name: string, check: () => Promise<void>) {
        try {
            await check();
            gates.push({ viewport: viewport.name, gate: name, pass: true, failure: null });
        } catch (error) {
            gates.push({ viewport: viewport.name, gate: name, pass: false, failure: safeError(error) });
        }
    }
}

function requireEnv(name: string): string {
    const value = process.env[name];
    if (!value) throw new Error(`${name} is required`);
    return value;
}

async function authenticate(browser: Browser, viewport: { width: number; height: number }) {
    const context = await browser.newContext({ viewport, ignoreHTTPSErrors: true });
    try {
        const page = await context.newPage();
        await page.goto(`${baseUrl}/authentication/challenge?returnUrl=%2Ftenants`, { waitUntil: 'domcontentloaded' });
        await page.locator('#username').fill(username);
        await page.locator('#password').fill(password);
        await page.locator('#kc-login').click();
        await page.waitForURL(url => url.origin === new URL(baseUrl).origin && !url.pathname.includes('signin-oidc'), { timeout: 30_000 });
        expect(page.url()).not.toContain('/realms/');
        return await context.storageState(); // In memory only: never write cookies or tokens to artifacts.
    } finally {
        await context.close();
    }
}

function pageEntries(dataset: Dataset, caseName: CaseName, pageIndex: number): string[] {
    const from = Date.parse(dataset.anchorUtc) - 7 * 24 * 60 * 60 * 1000;
    const filtered = dataset.entries.filter(entry => caseName === 'unfiltered'
        || (caseName === 'access' && entry.category === 'Access')
        || (caseName === 'administrative-seven-day' && entry.category === 'Administrative'
            && Date.parse(entry.timestampUtc) >= from && Date.parse(entry.timestampUtc) <= Date.parse(dataset.anchorUtc)));
    return filtered.slice(pageIndex * uiPageSize, (pageIndex + 1) * uiPageSize).map(entry => entry.reference);
}

async function assertRows(page: Page, expected: string[], stabilityMs = 150): Promise<number> {
    const signature = expected.join('|');
    await page.evaluate(() => { (window as any).__auditRowMatch = undefined; });
    try {
        await page.waitForFunction(({ refs, signature, stabilityMs }) => {
            const ready = document.querySelector('[data-testid="tenants-audit-ready"]');
            const actual = [...document.querySelectorAll('[data-testid="tenants-audit-row"]')]
                .map(element => element.getAttribute('data-audit-reference'));
            const matched = !!ready && actual.length === refs.length && actual.every((value, index) => value === refs[index]);
            const holder = window as any;
            if (!matched) {
                holder.__auditRowMatch = undefined;
                return false;
            }
            if (!holder.__auditRowMatch || holder.__auditRowMatch.signature !== signature) {
                holder.__auditRowMatch = { signature, firstPaint: null as number | null };
                requestAnimationFrame(() => {
                    const rows = [...document.querySelectorAll('[data-testid="tenants-audit-row"]')]
                        .map(element => element.getAttribute('data-audit-reference'));
                    if (holder.__auditRowMatch?.signature === signature && rows.join('|') === signature
                        && document.querySelector('[data-testid="tenants-audit-ready"]')) {
                        holder.__auditRowMatch.firstPaint = performance.timeOrigin + performance.now();
                    }
                });
                return false;
            }
            return holder.__auditRowMatch.firstPaint !== null
                && performance.timeOrigin + performance.now() - holder.__auditRowMatch.firstPaint >= stabilityMs;
        }, { refs: expected, signature, stabilityMs }, { timeout: 30_000, polling: 'raf' });
    } catch (error) {
        const diagnostic = await page.evaluate(() => ({
            path: location.pathname,
            ready: !!document.querySelector('[data-testid="tenants-audit-ready"]'),
            rowCount: document.querySelectorAll('[data-testid="tenants-audit-row"]').length,
            firstReference: document.querySelector('[data-testid="tenants-audit-row"]')?.getAttribute('data-audit-reference') ?? null,
            statusTestIds: [...document.querySelectorAll('[role="status"], [role="alert"]')]
                .map(element => element.getAttribute('data-testid')).filter(Boolean),
        }));
        throw new Error(`Audit rows did not match: ${JSON.stringify(diagnostic)}; ${safeError(error)}`);
    }
    // The painted-frame check above already verified exact row order and stability. A second
    // DOM read here can race a projection notification that starts a later refresh.
    return await page.evaluate(() => (window as any).__auditRowMatch.firstPaint as number);
}

async function metadata(page: Page) {
    const shell = page.locator('.tenant-audit__grid-shell');
    return {
        responseCount: Number(await shell.getAttribute('data-audit-response-count')),
        pageSize: Number(await shell.getAttribute('data-audit-page-size')),
        nextCursorPresent: (await shell.getAttribute('data-audit-next-cursor-present')) === 'true',
        displayedRowCount: await page.locator('[data-testid="tenants-audit-row"]').count(),
    };
}

async function measureInitial(page: Page, dataset: Dataset, viewport: string, batch: number, iteration: number, output: Sample[]) {
    const start = await page.evaluate(() => performance.timeOrigin + performance.now());
    let failure: string | null = null;
    let duration: number | null = null;
    let meta = { responseCount: null as number | null, pageSize: null as number | null,
        nextCursorPresent: null as boolean | null, displayedRowCount: null as number | null };
    try {
        await page.goto(`${baseUrl}/tenants/${dataset.tenantId}/audit`, { waitUntil: 'domcontentloaded' });
        const refs = pageEntries(dataset, 'unfiltered', 0);
        const painted = await assertRows(page, refs, 350);
        duration = painted - start;
        meta = await metadata(page);
        expect(meta).toEqual({ responseCount: refs.length, pageSize: uiPageSize,
            nextCursorPresent: true, displayedRowCount: refs.length });
    } catch (error) { failure = safeError(error); }
    output.push({ viewport, batch, iteration, caseName: 'unfiltered', action: 'initial', observable: 'initial-ready',
        durationMs: duration, ...meta, failure });
    if (failure) throw new Error(failure);
}

async function measureAction(page: Page, dataset: Dataset, viewport: string, batch: number, iteration: number,
    caseName: CaseName, action: Exclude<ActionName, 'initial'>, pageIndex: number, output: Sample[]) {
    const resultCase = action === 'reset' ? 'unfiltered' : caseName;
    const refs = pageEntries(dataset, resultCase, pageIndex);
    expect(refs.length, `${caseName} page ${pageIndex} must exist`).toBeGreaterThan(0);
    const selector = `[data-testid="tenants-audit-${action}"]`;
    const button = page.locator(selector);
    await expect(button).toBeEnabled();
    await page.evaluate(() => {
        const state = document.querySelector('[data-testid^="tenants-audit-"][role="status"], [data-testid^="tenants-audit-"][role="alert"]');
        const priorState = state?.getAttribute('data-testid');
        const priorRows = [...document.querySelectorAll('[data-testid="tenants-audit-row"]')]
            .map(element => element.getAttribute('data-audit-reference')).join('|');
        const timing = { start: 0, feedback: null as number | null };
        (window as any).__auditTiming = timing;
        document.addEventListener('click', () => { timing.start = performance.timeOrigin + performance.now(); }, { capture: true, once: true });
        const observer = new MutationObserver(() => {
            if (!timing.start || timing.feedback !== null) return;
            const currentState = document.querySelector('[data-testid^="tenants-audit-"][role="status"], [data-testid^="tenants-audit-"][role="alert"]')?.getAttribute('data-testid');
            const rows = [...document.querySelectorAll('[data-testid="tenants-audit-row"]')]
                .map(element => element.getAttribute('data-audit-reference')).join('|');
            if (currentState !== priorState || rows !== priorRows) {
                requestAnimationFrame(() => { timing.feedback = performance.timeOrigin + performance.now(); observer.disconnect(); });
            }
        });
        observer.observe(document.querySelector('[data-testid="tenants-audit-surface"]')!,
            { attributes: true, childList: true, characterData: true, subtree: true });
    });
    let failure: string | null = null;
    let duration: number | null = null;
    let feedback: number | null = null;
    let meta = { responseCount: null as number | null, pageSize: null as number | null,
        nextCursorPresent: null as boolean | null, displayedRowCount: null as number | null };
    try {
        await button.click();
        const painted = await assertRows(page, refs);
        const times = await page.evaluate(() => ({ end: performance.timeOrigin + performance.now(),
            start: (window as any).__auditTiming.start as number,
            feedback: (window as any).__auditTiming.feedback as number | null }));
        duration = painted - times.start;
        feedback = times.feedback === null ? null : times.feedback - times.start;
        meta = await metadata(page);
        expect(times.start).toBeGreaterThan(0);
        expect(feedback).not.toBeNull();
        expect(meta.responseCount).toBe(refs.length);
        expect(meta.pageSize).toBe(uiPageSize);
        expect(meta.displayedRowCount).toBe(refs.length);
        expect(meta.nextCursorPresent).toBe(pageEntries(dataset, resultCase, pageIndex + 1).length > 0);
    } catch (error) { failure = safeError(error); }
    output.push({ viewport, batch, iteration, caseName, action, observable: 'result', durationMs: duration, ...meta, failure });
    output.push({ viewport, batch, iteration, caseName, action, observable: 'feedback', durationMs: feedback, ...meta, failure });
    if (failure) throw new Error(failure);
}

async function setFilters(page: Page, caseName: CaseName, dataset: Dataset) {
    if (caseName === 'access') {
        await chooseCategory(page, 'Access');
    } else if (caseName === 'administrative-seven-day') {
        const from = new Date(Date.parse(dataset.anchorUtc) - 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 16);
        const to = new Date(dataset.anchorUtc).toISOString().slice(0, 16);
        await page.locator('[data-testid="tenants-audit-filter-from"] input').fill(from);
        await page.locator('[data-testid="tenants-audit-filter-to"] input').fill(to);
        await chooseCategory(page, 'Administrative');
    }
    await expect(page.locator('[data-testid="tenants-audit-filter-pending"]')).toBeVisible();
}

async function chooseCategory(page: Page, value: 'Access' | 'Administrative') {
    const dropdown = page.locator('[data-testid="tenants-audit-filter-category"]');
    await dropdown.locator('button[role="combobox"]').click();
    await dropdown.locator(`fluent-option[value="${value}"]`).click();
    await expect.poll(() => dropdown.evaluate(element => (element as any).value)).toBe(value);
}

function summarize(samples: Sample[], requiredCount: number) {
    const groups = new Map<string, Sample[]>();
    for (const sample of samples) {
        const key = [sample.viewport, sample.batch, sample.caseName, sample.action, sample.observable].join('|');
        groups.set(key, [...(groups.get(key) ?? []), sample]);
    }
    return [...groups].map(([key, group]) => {
        const [viewport, batch, caseName, action, observable] = key.split('|');
        const values = group.map(sample => sample.durationMs).filter((value): value is number => value !== null).sort((a, b) => a - b);
        const p75 = values.length === requiredCount ? values[Math.ceil(0.75 * values.length) - 1] : null;
        const p95 = values.length === requiredCount ? values[Math.ceil(0.95 * values.length) - 1] : null;
        const limits = observable === 'initial-ready' ? [2500, 4000] : observable === 'feedback' ? [200, 500] : [1500, 3000];
        return { viewport, batch: Number(batch), caseName, action, observable, count: group.length,
            p75, p95, budgetP75: limits[0], budgetP95: limits[1],
            pass: group.length === requiredCount && group.every(sample => sample.failure === null)
                && p75 !== null && p95 !== null && p75 <= limits[0] && p95 <= limits[1] };
    });
}

function safeError(error: unknown): string {
    if (!(error instanceof Error)) return 'UnknownError';
    // Keep the diagnostic first line only, and strip credentials, URLs, and query-like values.
    const detail = error.message.split('\n')[0]
        .replaceAll(username, '[redacted]').replaceAll(password, '[redacted]')
        .replace(/\x1b\[[0-9;]*m/g, '')
        .replace(/https?:\/\/\S+/g, '[url]')
        .replace(/(token|secret|password|code|state)=[^\s&]+/gi, '$1=[redacted]')
        .slice(0, 180);
    return `${error.name}: ${detail}`;
}
