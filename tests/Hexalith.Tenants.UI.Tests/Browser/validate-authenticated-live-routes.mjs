// Authenticated sockets only: the session belongs to the running Aspire fixture.
// Every projection is command-seeded; no upstream response or browser request is substituted.
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFile, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const moduleName = process.env.TENANTS_BROWSER_PLAYWRIGHT_MODULE || '@playwright/test';
const output = process.argv[3];
const evidence = { startedUtc: new Date().toISOString(), mode: 'Authenticated live Aspire topology; command-seeded persisted projections', cases: [], passed: false };
let browser;
let phase = 'session';
const check = (condition, label) => { if (!condition) throw Object.assign(new Error(), { safeLabel: label }); };
const hash = value => createHash('sha256').update(value).digest('hex');
const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim();
const untrackedHash = async repository => {
    const digest = createHash('sha256');
    const paths = git('-C', repository, 'ls-files', '-z', '--others', '--exclude-standard', '--', 'src', 'tests').split('\0').filter(Boolean).sort();
    for (const path of paths) digest.update(path).update('\0').update(await readFile(resolve(root, repository, path))).update('\0');
    return digest.digest('hex');
};
const capture = async configuration => ({
    sourceRevision: git('rev-parse', 'HEAD'),
    sourceDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src', 'tests', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json')),
    untrackedSourceSha256: await untrackedHash('.'),
    validatorSha256: hash(await readFile(fileURLToPath(import.meta.url))),
    uiBinarySha256: hash(await readFile(resolve(root, `src/Hexalith.Tenants.UI/bin/${configuration}/net10.0/Hexalith.Tenants.UI.dll`))),
    shellBinarySha256: hash(await readFile(resolve(root, `src/Hexalith.Tenants.UI/bin/${configuration}/net10.0/Hexalith.FrontComposer.Shell.dll`))),
    frontComposerRevision: git('-C', 'references/Hexalith.FrontComposer', 'rev-parse', 'HEAD'),
    frontComposerDiffSha256: hash(git('-C', 'references/Hexalith.FrontComposer', 'diff', '--binary', 'HEAD', '--', 'src', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json')),
    frontComposerUntrackedSha256: await untrackedHash('references/Hexalith.FrontComposer'),
    dependencyGitlinksSha256: hash(git('diff', '--binary', 'HEAD', '--', 'references')),
    playwrightPackageSha256: hash(await readFile(require.resolve(`${moduleName}/package.json`))),
});
try {
    check(Boolean(output), 'result-path-required');
    const session = JSON.parse(await readFile(process.argv[2], 'utf8'));
    const base = new URL(session.baseUrl);
    check(['localhost', '127.0.0.1'].includes(base.hostname), 'live-loopback-endpoint-required');
    const { chromium } = require(moduleName);
    evidence.playwrightVersion = require(`${moduleName}/package.json`).version;
    const configuration = session.configuration || 'Debug';
    const before = await capture(configuration);
    Object.assign(evidence, before);
    browser = await chromium.launch({ headless: true });
    const context = await browser.newContext();
    const separator = session.cookie.indexOf('=');
    check(separator > 0, 'cookie-ticket-required');
    await context.addCookies([{ name: session.cookie.slice(0, separator), value: session.cookie.slice(separator + 1), url: base.origin, httpOnly: true, secure: base.protocol === 'https:', sameSite: 'Lax' }]);
    const page = await context.newPage();
    page.setDefaultTimeout(25_000);
    const errors = [];
    const recordError = label => { if (errors.length < 8) errors.push(label); };
    page.on('pageerror', () => recordError('browser-page-error'));
    page.on('console', message => {
        if (message.type() === 'error' && /circuit|blazor|unhandled exception|connection disconnected/i.test(message.text().slice(0, 2048))) recordError('blazor-circuit-error');
    });
    const testId = id => page.getByTestId(id);
    const workspaceReturn = `/tenants?tab=tenants&scope=all&status=Active&sort=tenantId&desc=false&selected=${encodeURIComponent(session.tenantId)}`;
    const detailPath = `/tenants/${encodeURIComponent(session.tenantId)}?returnUrl=${encodeURIComponent(workspaceReturn)}`;
    const auditPath = `/tenants/${encodeURIComponent(session.tenantId)}/audit`;
    const listPath = session.listPath || '/tenants';
    const seededListRow = () => page.locator('fluent-data-grid-row, [role="row"], tr').filter({ has: testId('tenants-list-tenant-id').filter({ hasText: session.tenantId }) });
    const focused = locator => locator.evaluate(element => {
        let active = document.activeElement;
        while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
        for (let current = active; current; current = current.parentElement || current.getRootNode()?.host) if (current === element) return true;
        return false;
    });
    const waitUntil = async (condition, label) => {
        const deadline = Date.now() + 25_000;
        while (Date.now() < deadline) { if (await condition()) return; await page.waitForTimeout(100); }
        check(false, label);
    };
    const requireReady = async marker => {
        if (marker === 'tenants-workspace') {
            await seededListRow().locator('.truth-state-badge--current').waitFor({ state: 'visible' });
            check((await seededListRow().textContent()).includes(session.tenantName), 'seeded-list-name');
        } else if (marker === 'tenants-detail') {
            await page.locator(`[id="tenants-member-${session.targetUserId}"]`).waitFor({ state: 'attached' });
            await page.locator('[data-testid="tenants-detail-truth-state"].truth-state-badge--current').waitFor({ state: 'visible' });
        } else if (marker === 'tenants-audit-surface') {
            await testId('tenants-audit-ready').waitFor({ state: 'visible' });
            await testId('tenants-audit-row').first().waitFor({ state: 'visible' });
        } else if (marker === 'tenants-global-admins-area') {
            await testId('tenants-global-admins-ready').waitFor({ state: 'visible' });
            await testId('tenants-global-admins-row').filter({ hasText: session.callerId }).waitFor({ state: 'visible' });
            check(await testId('tenants-global-admins-restricted-header').count() === 0, 'administrator-restricted-state-absent');
            check(await testId('tenants-global-admins-unavailable').count() === 0, 'administrator-unavailable-state-absent');
        } else {
            const prefix = marker === 'tenants-my-page' ? 'tenants-my' : 'tenants-user';
            const row = page.locator('fluent-data-grid-row, [role="row"], tr').filter({ has: testId(`${prefix}-row`).filter({ hasText: session.tenantId }) });
            await row.locator('.truth-state-badge--current').waitFor({ state: 'visible' });
            check((await row.textContent()).includes(session.tenantName), 'seeded-membership-name');
        }
    };
    const routes = [
        [listPath, 'tenants-workspace'],
        [detailPath, 'tenants-detail'],
        [auditPath, 'tenants-audit-surface'],
        ['/tenants/my', 'tenants-my-page'],
        [`/tenants/users?userId=${encodeURIComponent(session.targetUserId)}`, 'tenants-user-lookup'],
        ['/global-administrators', 'tenants-global-admins-area'],
    ];
    for (let index = 0; index < routes.length; index++) {
        phase = `protected-route-${index + 1}`;
        const [path, marker] = routes[index];
        const response = await page.goto(new URL(path, base).href);
        check(response?.status() === 200, 'protected-route-http-200');
        await testId(marker).waitFor({ state: 'visible' });
        check(await testId('fc-scope-blocked').count() === 0, 'validated-scope-required');
        await requireReady(marker);
        evidence.cases.push({ name: phase, passed: true });
    }

    phase = 'interactive-circuit';
    await page.goto(new URL(auditPath, base).href);
    await requireReady('tenants-audit-surface');
    const fromInput = testId('tenants-audit-filter-from').locator('input');
    await fromInput.fill('2020-01-01T00:00');
    await fromInput.press('Tab');
    await testId('tenants-audit-filter-pending').waitFor({ state: 'visible' });
    await testId('tenants-audit-reset').click();
    await testId('tenants-audit-filter-pending').waitFor({ state: 'hidden' });
    check(await fromInput.inputValue() === '', 'server-rendered-filter-reset');
    evidence.cases.push({ name: phase, passed: true });

    for (const width of [390, 768, 1440]) {
        for (const forcedColors of ['none', 'active']) {
            phase = `audit-layout-${width}-${forcedColors}`;
            await page.setViewportSize({ width, height: 1000 });
            await page.emulateMedia({ forcedColors });
            await page.goto(new URL(auditPath, base).href);
            await testId('tenants-audit-surface').waitFor({ state: 'visible' });
            await testId('tenants-audit-row').first().waitFor({ state: 'visible' });
            const measured = await page.evaluate(() => ({ width: innerWidth, documentWidth: document.documentElement.scrollWidth,
                forcedColors: matchMedia('(forced-colors: active)').matches }));
            check(measured.width === width, 'measured-viewport-width');
            check(measured.forcedColors === (forcedColors === 'active'), 'measured-forced-colors-mode');
            check(measured.documentWidth <= measured.width + 2, 'audit-document-horizontal-overflow');
            for (const id of ['tenants-audit-back', 'tenants-audit-reset', 'tenants-audit-refresh']) {
                check(await testId(id).isVisible(), 'audit-recovery-control-visible');
                check(await testId(id).isEnabled(), 'audit-recovery-control-enabled');
            }
            evidence.cases.push({ name: phase, ...measured, passed: true });

            phase = `tenant-list-layout-${width}-${forcedColors}`;
            await page.goto(new URL(listPath, base).href);
            await requireReady('tenants-workspace');
            const grid = testId('tenants-list-grid');
            const row = seededListRow();
            const pinned = ['tenants-list-tenant-id', 'tenants-list-status', 'tenants-list-truth-state'];
            await grid.evaluate(element => { element.scrollLeft = 0; });
            const leftBefore = await Promise.all(pinned.map(id => row.getByTestId(id).evaluate(element => element.getBoundingClientRect().left)));
            const dimensions = await grid.evaluate(element => {
                element.scrollLeft = element.scrollWidth;
                return { width: innerWidth, documentWidth: document.documentElement.scrollWidth, clientWidth: element.clientWidth,
                    scrollWidth: element.scrollWidth, forcedColors: matchMedia('(forced-colors: active)').matches };
            });
            await page.waitForTimeout(50);
            const leftAfter = await Promise.all(pinned.map(id => row.getByTestId(id).evaluate(element => element.getBoundingClientRect().left)));
            check(dimensions.width === width && dimensions.forcedColors === (forcedColors === 'active'), 'measured-list-viewport-and-mode');
            check(dimensions.documentWidth <= width + 2, 'list-document-horizontal-overflow');
            check(leftBefore.every((left, index) => Math.abs(leftAfter[index] - left) <= 2), 'identity-status-freshness-remain-pinned');
            check((await row.getByTestId('tenants-list-status').getAttribute('class')).includes('--active'), 'seeded-status-active');
            const detailLink = row.getByTestId('tenants-list-detail-link');
            await detailLink.focus();
            await page.keyboard.press('Tab');
            await page.keyboard.press('Shift+Tab');
            check(await focused(detailLink), 'list-detail-keyboard-focus');
            const outline = await detailLink.evaluate(element => ({ width: parseFloat(getComputedStyle(element).outlineWidth), style: getComputedStyle(element).outlineStyle }));
            check(outline.width > 0 && outline.style !== 'none', 'list-detail-visible-focus-outline');
            const overflowAction = row.locator('[data-testid="tenants-audit-entrypoint"][data-audit-source="tenant-list"]');
            await waitUntil(async () => Boolean(await overflowAction.getAttribute('href')), 'list-overflow-action-ready');
            for (let step = 0; step < 16 && !(await focused(overflowAction)); step++) await page.keyboard.press('Tab');
            check(await focused(overflowAction), 'list-overflow-action-keyboard-access');
            const actionVisible = await overflowAction.evaluate(element => {
                const bounds = element.getBoundingClientRect();
                const container = element.closest('[data-testid="tenants-list-grid"]').getBoundingClientRect();
                return bounds.left >= container.left - 2 && bounds.right <= container.right + 2;
            });
            check(actionVisible, 'list-overflow-action-in-scrollport');
            evidence.cases.push({ name: phase, ...dimensions, pinned: true, keyboardFocus: true, overflowAccessible: true, passed: true });
        }
    }

    await page.emulateMedia({ forcedColors: 'none' });
    await page.setViewportSize({ width: 1440, height: 1000 });
    const completeContext = value => {
        const url = new URL(value, base);
        url.searchParams.delete('auditFocus');
        url.searchParams.sort();
        return url.pathname + url.search + url.hash;
    };
    for (const source of ['tenant-detail', 'member-row']) {
        phase = `contextual-audit-back-${source}`;
        await page.goto(new URL(detailPath, base).href);
        await testId('tenants-detail').waitFor({ state: 'visible' });
        const focusId = source === 'tenant-detail' ? 'tenants-detail-identity' : `tenants-member-${session.targetUserId}`;
        if (source === 'member-row') {
            const section = page.locator('fluent-accordion-item').filter({ has: page.locator(`[id="${focusId}"]`) });
            if (await section.count() && !(await page.locator(`[id="${focusId}"]`).isVisible())) {
                await section.locator('button, [role="button"]').first().click();
            }
        }
        const launcher = source === 'tenant-detail'
            ? page.locator('[data-testid="tenants-audit-entrypoint"]').filter({ has: testId('tenants-detail-audit-entrypoint') })
            : page.locator('[role="row"], tr').filter({ has: page.locator(`[id="${focusId}"]`) }).locator('[data-testid="tenants-audit-entrypoint"]').filter({ has: testId('tenants-member-audit-entrypoint') });
        await launcher.waitFor({ state: 'visible' });
        await launcher.click();
        await testId('tenants-audit-surface').waitFor({ state: 'visible' });
        const destination = new URL(page.url());
        check(destination.searchParams.get('source') === source, 'audit-origin-preserved');
        check(destination.searchParams.get('returnFocus') === focusId, 'audit-return-focus-preserved');
        check(source !== 'member-row' || destination.searchParams.get('targetUserId') === session.targetUserId, 'audit-target-preserved');
        check(completeContext(destination.searchParams.get('returnUrl')) === completeContext(detailPath), 'complete-nested-audit-return-context');
        const back = new URL(await testId('tenants-audit-back').getAttribute('href'), base);
        check(back.origin === base.origin && back.pathname === `/tenants/${session.tenantId}`, 'safe-local-audit-return');
        check(completeContext(back) === completeContext(detailPath), 'back-href-retains-complete-context');
        await testId('tenants-audit-back').click();
        await testId('tenants-detail').waitFor({ state: 'visible' });
        await page.waitForFunction(() => !document.querySelector('[data-testid="tenants-audit-surface"]'));
        await waitUntil(() => focused(launcher), 'audit-back-restores-source-launcher-focus');
        check(await launcher.getAttribute('data-audit-source') === source, 'restored-source-specific-launcher');
        check(completeContext(page.url()) === completeContext(detailPath), 'back-retains-complete-nested-context');
        evidence.cases.push({ name: phase, targetPreserved: true, safeReturn: true, focusRestored: true, passed: true });
    }

    phase = 'external-audit-return';
    await page.goto(new URL(`${auditPath}?source=member-row&targetUserId=${encodeURIComponent(session.targetUserId)}&returnUrl=${encodeURIComponent('https://external.invalid/collect')}&returnFocus=tenants-detail-identity`, base).href);
    await testId('tenants-audit-back').waitFor({ state: 'visible' });
    const rejected = new URL(await testId('tenants-audit-back').getAttribute('href'), base);
    check(rejected.origin === base.origin, 'external-return-rejected');
    evidence.cases.push({ name: phase, passed: true });
    check(errors.length === 0, 'browser-page-errors-absent');
    const after = await capture(configuration);
    for (const key of Object.keys(before)) check(after[key] === before[key], `stable-capture-${key}`);
    evidence.passed = true;
} catch (error) {
    evidence.failure = { phase, label: error.safeLabel || 'live-browser-assertion-failed' };
    process.exitCode = 1;
} finally {
    await browser?.close();
    evidence.finishedUtc = new Date().toISOString();
    if (output) await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
}
