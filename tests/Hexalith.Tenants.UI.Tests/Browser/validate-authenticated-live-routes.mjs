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
const evidence = { startedUtc: new Date().toISOString(), mode: 'Authenticated live Aspire topology; command-seeded persisted projections',
    bindingScope: { artifacts: 'On-disk UI/Shell binaries, validator, and Tenants/FrontComposer working sources',
        runningBackendBinaries: 'Not verified by this browser artifact; qualify through the separate integration results',
        otherDependencyWorkingSources: 'Gitlink state only; source contents are not bound by this artifact' },
    cases: [], passed: false };
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
    await context.addCookies(session.cookie.split(';').map(value => {
        const cookie = value.trim();
        const separator = cookie.indexOf('=');
        check(separator > 0, 'cookie-ticket-required');
        return { name: cookie.slice(0, separator), value: cookie.slice(separator + 1), url: base.origin,
            httpOnly: true, secure: base.protocol === 'https:', sameSite: 'Lax' };
    }));
    const page = await context.newPage();
    page.setDefaultTimeout(25_000);
    const open = async destination => {
        const response = await page.goto(destination);
        // Prerendered rows can arrive before the interactive circuit and Fluent modules.
        // Let their startup requests settle before sending input or measuring replacement DOM.
        await page.waitForLoadState('networkidle');
        return response;
    };
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
    const myTenantsPath = session.myTenantsPath || '/tenants/my';
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
    const intersectsAndReceivesPointer = locator => locator.evaluate(element => {
        let bounds = element.getBoundingClientRect();
        let left = Math.max(0, bounds.left), right = Math.min(innerWidth, bounds.right);
        let top = Math.max(0, bounds.top), bottom = Math.min(innerHeight, bounds.bottom);
        for (let parent = element.parentElement; parent; parent = parent.parentElement || parent.getRootNode()?.host) {
            const style = getComputedStyle(parent), clip = parent.getBoundingClientRect();
            if (/(auto|scroll|hidden|clip)/.test(style.overflowX)) { left = Math.max(left, clip.left); right = Math.min(right, clip.right); }
            if (/(auto|scroll|hidden|clip)/.test(style.overflowY)) { top = Math.max(top, clip.top); bottom = Math.min(bottom, clip.bottom); }
        }
        if (right - left <= 1 || bottom - top <= 1) return false;
        let hit = document.elementFromPoint((left + right) / 2, (top + bottom) / 2);
        while (hit?.shadowRoot?.elementFromPoint) {
            const inner = hit.shadowRoot.elementFromPoint((left + right) / 2, (top + bottom) / 2);
            if (!inner || inner === hit) break;
            hit = inner;
        }
        for (let current = hit; current; current = current.parentElement || current.getRootNode()?.host) if (current === element) return true;
        return false;
    });
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
        [myTenantsPath, 'tenants-my-page'],
        [`/tenants/users?userId=${encodeURIComponent(session.targetUserId)}`, 'tenants-user-lookup'],
        ['/global-administrators', 'tenants-global-admins-area'],
    ];
    for (let index = 0; index < routes.length; index++) {
        phase = `protected-route-${index + 1}`;
        const [path, marker] = routes[index];
        const response = await open(new URL(path, base).href);
        check(response?.status() === 200, 'protected-route-http-200');
        await testId(marker).waitFor({ state: 'visible' });
        check(await testId('fc-scope-blocked').count() === 0, 'validated-scope-required');
        await requireReady(marker);
        evidence.cases.push({ name: phase, passed: true });
    }

    phase = 'interactive-circuit';
    await open(new URL(auditPath, base).href);
    await requireReady('tenants-audit-surface');
    phase = 'interactive-filter-input';
    const fromInput = page.locator('input[data-testid="tenants-audit-filter-from"], [data-testid="tenants-audit-filter-from"] input');
    await fromInput.fill('2020-01-01T00:00');
    await fromInput.press('Tab');
    phase = 'interactive-filter-change';
    await testId('tenants-audit-filter-pending').waitFor({ state: 'visible' });
    phase = 'interactive-filter-reset';
    await testId('tenants-audit-reset').click();
    await testId('tenants-audit-filter-pending').waitFor({ state: 'hidden' });
    check(await fromInput.inputValue() === '', 'server-rendered-filter-reset');
    evidence.cases.push({ name: 'interactive-circuit', passed: true });

    for (const width of [390, 768, 1440]) {
        for (const forcedColors of ['none', 'active']) {
            phase = `audit-layout-${width}-${forcedColors}`;
            await page.setViewportSize({ width, height: 1000 });
            await page.emulateMedia({ forcedColors });
            await open(new URL(auditPath, base).href);
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
                await testId(id).scrollIntoViewIfNeeded();
                check(await intersectsAndReceivesPointer(testId(id)), 'audit-recovery-control-hit-testable');
            }
            evidence.cases.push({ name: phase, ...measured, passed: true });

            phase = `tenant-list-layout-${width}-${forcedColors}`;
            const layoutPhase = phase;
            phase = `${layoutPhase}-open`;
            await open(new URL(listPath, base).href);
            phase = `${layoutPhase}-ready`;
            await requireReady('tenants-workspace');
            const grid = testId('tenants-list-grid');
            const row = seededListRow();
            const pinned = ['tenants-list-tenant-id', 'tenants-list-status', 'tenants-list-truth-state'];
            phase = `${layoutPhase}-scroll`;
            await row.getByTestId('tenants-list-tenant-id').scrollIntoViewIfNeeded();
            await grid.evaluate(element => { element.scrollLeft = 0; });
            const leftBefore = await Promise.all(pinned.map(id => row.getByTestId(id).evaluate(element => element.getBoundingClientRect().left)));
            const dimensions = await grid.evaluate(element => {
                const scrollBefore = element.scrollLeft;
                element.scrollLeft = element.scrollWidth;
                return { width: innerWidth, documentWidth: document.documentElement.scrollWidth, clientWidth: element.clientWidth,
                    scrollWidth: element.scrollWidth, scrollBefore, scrollAfter: element.scrollLeft,
                    forcedColors: matchMedia('(forced-colors: active)').matches };
            });
            await page.waitForTimeout(50);
            const leftAfter = await Promise.all(pinned.map(id => row.getByTestId(id).evaluate(element => element.getBoundingClientRect().left)));
            evidence.lastListMeasurement = { ...dimensions, leftBefore, leftAfter,
                cells: await Promise.all(pinned.map(id => row.getByTestId(id).evaluate(element => {
                    const cell = element.closest('[role="gridcell"], fluent-data-grid-cell, td') || element;
                    const bounds = cell.getBoundingClientRect(), style = getComputedStyle(cell);
                    return { tag: cell.tagName, left: bounds.left, right: bounds.right, position: style.position, inset: style.left };
                }))) };
            check(dimensions.width === width && dimensions.forcedColors === (forcedColors === 'active'), 'measured-list-viewport-and-mode');
            check(dimensions.documentWidth <= width + 2, 'list-document-horizontal-overflow');
            if (width < 1024) {
                check(dimensions.scrollWidth > dimensions.clientWidth + 2, 'narrow-list-requires-real-overflow');
                check(dimensions.scrollAfter > dimensions.scrollBefore + 2, 'narrow-list-requires-measured-scroll-movement');
            }
            const remainPinned = leftBefore.every((left, index) => Math.abs(leftAfter[index] - left) <= 2);
            if (!remainPinned) await page.screenshot({ path: output.replace(/\.json$/, '.png') });
            check(remainPinned, 'identity-status-freshness-remain-pinned');
            phase = `${layoutPhase}-hit-content`;
            evidence.lastListContentHits = [];
            for (const id of pinned) {
                const content = row.getByTestId(id);
                const bounds = await content.evaluate(element => {
                    const r = element.getBoundingClientRect();
                    return { left: r.left, right: r.right, top: r.top, bottom: r.bottom };
                });
                const hit = await intersectsAndReceivesPointer(content);
                evidence.lastListContentHits.push({ id, ...bounds, hit });
                if (!hit) await page.screenshot({ path: output.replace(/\.json$/, '.png') });
                check(hit, `pinned-content-hit-${id}`);
            }
            check((await row.getByTestId('tenants-list-status').getAttribute('class') || '').includes('--active'), 'seeded-status-active');
            phase = `${layoutPhase}-focus`;
            const detailLink = row.getByTestId('tenants-list-detail-link');
            await detailLink.focus();
            await page.keyboard.press('Tab');
            await page.keyboard.press('Shift+Tab');
            check(await focused(detailLink), 'list-detail-keyboard-focus');
            const outline = await detailLink.evaluate(element => ({ width: parseFloat(getComputedStyle(element).outlineWidth), style: getComputedStyle(element).outlineStyle }));
            check(outline.width > 0 && outline.style !== 'none', 'list-detail-visible-focus-outline');
            phase = `${layoutPhase}-copy-action`;
            const copy = row.locator('[data-surface-testid="tenants-list-copy-reference"]');
            check(await copy.count() === 1, 'actual-list-copy-control-required');
            await page.keyboard.press('Tab');
            check(await focused(copy), 'list-copy-keyboard-focus');
            const copyMeasurement = await copy.evaluate(element => {
                const bounds = element.getBoundingClientRect(), cell = element.closest('[role="gridcell"], fluent-data-grid-cell, td').getBoundingClientRect();
                const style = getComputedStyle(element);
                return { left: bounds.left, right: bounds.right, top: bounds.top, bottom: bounds.bottom,
                    cellLeft: cell.left, cellRight: cell.right, cellTop: cell.top, cellBottom: cell.bottom,
                    contained: bounds.left >= cell.left - 1 && bounds.right <= cell.right + 1
                        && bounds.top >= cell.top - 1 && bounds.bottom <= cell.bottom + 1,
                    outlineWidth: parseFloat(style.outlineWidth), outlineStyle: style.outlineStyle };
            });
            evidence.lastListCopyMeasurement = copyMeasurement;
            check(copyMeasurement.contained, 'list-copy-contained-in-identity-cell');
            check(await intersectsAndReceivesPointer(copy), 'list-copy-control-hit-testable');
            check(copyMeasurement.outlineWidth > 0 && copyMeasurement.outlineStyle !== 'none', 'list-copy-visible-focus-outline');
            const overflowAction = row.locator('[data-testid="tenants-audit-entrypoint"][data-audit-source="tenant-list"]:visible');
            phase = `${layoutPhase}-audit-action`;
            await waitUntil(async () => Boolean(await overflowAction.getAttribute('href')), 'list-overflow-action-ready');
            for (let step = 0; step < 16 && !(await focused(overflowAction)); step++) await page.keyboard.press('Tab');
            check(await focused(overflowAction), 'list-overflow-action-keyboard-access');
            const actionMeasurement = await overflowAction.evaluate(element => {
                const bounds = element.getBoundingClientRect();
                const grid = element.closest('[data-testid="tenants-list-grid"]'), container = grid.getBoundingClientRect();
                const style = getComputedStyle(element);
                return { left: bounds.left, right: bounds.right, top: bounds.top, bottom: bounds.bottom,
                    containerLeft: container.left, containerRight: container.right, scrollLeft: grid.scrollLeft,
                    width: style.width, minWidth: style.minWidth,
                    visible: bounds.left >= container.left - 2 && bounds.right <= container.right + 2 };
            });
            evidence.lastListActionMeasurement = actionMeasurement;
            if (!actionMeasurement.visible) await page.screenshot({ path: output.replace(/\.json$/, '.png') });
            check(actionMeasurement.visible, 'list-overflow-action-in-scrollport');
            check(await intersectsAndReceivesPointer(overflowAction), 'list-overflow-action-hit-testable');
            evidence.cases.push({ name: layoutPhase, ...dimensions, pinned: true, keyboardFocus: true, copyAccessible: true, overflowAccessible: true, passed: true });
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
        const contextPhase = phase;
        await open(new URL(detailPath, base).href);
        await requireReady('tenants-detail');
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
        phase = `${contextPhase}-launcher`;
        await launcher.waitFor({ state: 'visible' });
        try {
            await waitUntil(async () => Boolean(await launcher.getAttribute('href')), 'contextual-audit-launcher-authorized');
        } catch (error) {
            await page.screenshot({ path: output.replace(/\.json$/, '.png') });
            throw error;
        }
        await launcher.click();
        phase = `${contextPhase}-audit-ready`;
        await requireReady('tenants-audit-surface');
        const displayedContext = await testId('tenants-audit-context').textContent();
        check(displayedContext.includes(source === 'member-row' ? session.targetUserId : 'tenant detail'), 'displayed-audit-source-or-member-context');
        if (source === 'member-row') await testId('tenants-audit-row-target').filter({ hasText: session.targetUserId }).waitFor({ state: 'visible' });
        check((await testId('tenants-audit-return-context').textContent()).includes('tenant detail'), 'displayed-audit-return-origin');
        const destination = new URL(page.url());
        check(destination.searchParams.get('source') === source, 'audit-origin-preserved');
        check(destination.searchParams.get('returnFocus') === focusId, 'audit-return-focus-preserved');
        check(source !== 'member-row' || destination.searchParams.get('targetUserId') === session.targetUserId, 'audit-target-preserved');
        check(completeContext(destination.searchParams.get('returnUrl')) === completeContext(detailPath), 'complete-nested-audit-return-context');
        const back = new URL(await testId('tenants-audit-back').getAttribute('href'), base);
        check(back.origin === base.origin && back.pathname === `/tenants/${session.tenantId}`, 'safe-local-audit-return');
        check(completeContext(back) === completeContext(detailPath), 'back-href-retains-complete-context');
        phase = `${contextPhase}-back`;
        await testId('tenants-audit-back').click();
        await testId('tenants-detail').waitFor({ state: 'visible' });
        await page.waitForFunction(() => !document.querySelector('[data-testid="tenants-audit-surface"]'));
        phase = `${contextPhase}-focus`;
        await waitUntil(() => focused(launcher), 'audit-back-restores-source-launcher-focus');
        check(await launcher.getAttribute('data-audit-source') === source, 'restored-source-specific-launcher');
        check(completeContext(page.url()) === completeContext(detailPath), 'back-retains-complete-nested-context');
        evidence.cases.push({ name: contextPhase, targetPreserved: true, safeReturn: true, focusRestored: true, passed: true });
    }

    const canonicalListContext = value => {
        const url = new URL(value, base);
        check(url.origin === base.origin && ['/tenants', '/tenants/tenants'].includes(url.pathname), 'list-return-stays-in-canonical-workspace');
        // The production workspace redirects its compatibility path to this route-backed path.
        url.pathname = '/tenants/tenants';
        return completeContext(url);
    };
    for (const width of [390, 768, 1440]) {
        const contextPhase = `contextual-audit-back-tenant-list-${width}`;
        phase = `${contextPhase}-source`;
        await page.setViewportSize({ width, height: 1000 });
        await page.emulateMedia({ forcedColors: 'none' });
        // A cursorless origin keeps this focus control independent of intentional cursor recovery.
        // The six protected pages and twelve layout controls above still use the current seeded tenant.
        await open(new URL('/tenants', base).href);
        await testId('tenants-workspace').waitFor({ state: 'visible' });
        const sourceRow = page.locator('fluent-data-grid-row, [role="row"], tr').filter({ has: testId('tenants-list-tenant-id') }).first();
        await sourceRow.locator('.truth-state-badge--current').waitFor({ state: 'visible' });
        const observedSource = new URL(page.url());
        check(!observedSource.searchParams.has('cursor'), 'list-back-origin-is-cursorless');
        const focusId = await sourceRow.getByTestId('tenants-list-tenant-id').getAttribute('id');
        check(Boolean(focusId), 'list-back-origin-has-unique-anchor');
        const launcher = sourceRow.locator('[data-testid="tenants-audit-entrypoint"][data-audit-source="tenant-list"]:visible');
        await waitUntil(async () => Boolean(await launcher.getAttribute('href')), 'list-back-visible-launcher-authorized');
        check(await launcher.isEnabled(), 'list-back-visible-launcher-enabled');
        const destination = new URL(await launcher.getAttribute('href'), base);
        const tenantId = decodeURIComponent(destination.pathname.split('/')[2]);
        const detail = new URL(await sourceRow.getByTestId('tenants-list-detail-link').getAttribute('href'), base);
        check(destination.origin === base.origin && destination.pathname === `${detail.pathname}/audit`, 'list-back-target-is-origin-tenant');
        check(destination.searchParams.get('source') === 'tenant-list' && destination.searchParams.get('returnFocus') === focusId, 'list-back-source-and-anchor-preserved');
        const expectedReturn = new URL(observedSource);
        expectedReturn.searchParams.set('selected', tenantId);
        expectedReturn.searchParams.set('anchor', focusId);
        check(canonicalListContext(destination.searchParams.get('returnUrl')) === canonicalListContext(expectedReturn), 'list-back-retains-observed-source-context');
        await launcher.scrollIntoViewIfNeeded();
        check(await intersectsAndReceivesPointer(launcher), 'list-back-visible-launcher-hit-testable');
        await launcher.click();
        phase = `${contextPhase}-audit-ready`;
        await requireReady('tenants-audit-surface');
        await testId('tenants-audit-row-scope').first().waitFor({ state: 'visible' });
        check((await testId('tenants-audit-row-scope').allTextContents()).every(scope => scope.trim() === tenantId), 'list-back-ready-audit-rows-match-origin-tenant');
        check((await testId('tenants-audit-context').textContent()).includes('tenant list'), 'list-back-displays-source-context');
        check((await testId('tenants-audit-return-context').textContent()).includes('tenant list'), 'list-back-displays-return-context');
        const current = new URL(page.url());
        check(current.pathname === destination.pathname && current.searchParams.get('source') === 'tenant-list'
            && current.searchParams.get('returnFocus') === focusId, 'list-back-destination-keeps-source-and-anchor');
        check(canonicalListContext(current.searchParams.get('returnUrl')) === canonicalListContext(expectedReturn), 'list-back-destination-keeps-complete-context');
        const back = new URL(await testId('tenants-audit-back').getAttribute('href'), base);
        check(canonicalListContext(back) === canonicalListContext(expectedReturn), 'list-back-href-keeps-complete-context');
        phase = `${contextPhase}-back-focus`;
        await testId('tenants-audit-back').click();
        await page.locator(`[id="${focusId}"]`).waitFor({ state: 'visible' });
        const restored = page.locator('fluent-data-grid-row, [role="row"], tr').filter({ has: page.locator(`[id="${focusId}"]`) })
            .locator('[data-testid="tenants-audit-entrypoint"][data-audit-source="tenant-list"]:visible');
        await waitUntil(() => focused(restored), 'list-back-restores-visible-source-launcher-focus');
        check(await restored.locator('[data-testid="tenants-list-mobile-audit-entrypoint"]').count() === (width < 768 ? 1 : 0), 'list-back-restores-correct-responsive-copy');
        check(canonicalListContext(page.url()) === canonicalListContext(expectedReturn), 'list-back-retains-complete-canonical-context');
        evidence.cases.push({ name: contextPhase, width, readyScopedRows: true, safeReturn: true, focusRestored: true, passed: true });
    }

    phase = 'external-audit-return';
    await open(new URL(`${auditPath}?source=member-row&targetUserId=${encodeURIComponent(session.targetUserId)}&returnUrl=${encodeURIComponent('https://external.invalid/collect')}&returnFocus=tenants-detail-identity`, base).href);
    await testId('tenants-audit-back').waitFor({ state: 'visible' });
    const rejected = new URL(await testId('tenants-audit-back').getAttribute('href'), base);
    check(rejected.origin === base.origin, 'external-return-rejected');
    evidence.cases.push({ name: phase, passed: true });
    check(errors.length === 0, 'browser-page-errors-absent');
    const after = await capture(configuration);
    for (const key of Object.keys(before)) check(after[key] === before[key], `stable-capture-${key}`);
    evidence.passed = true;
} catch (error) {
    // Match only known diagnostic categories; raw browser errors may include signed URLs.
    const message = typeof error.message === 'string' ? error.message.slice(0, 4096) : '';
    const certificateCode = message.match(/\b(ERR_CERT_[A-Z_]{1,60})\b/)?.[1];
    const category = certificateCode ? `browser-certificate-${certificateCode}`
        : /ERR_CONNECTION_REFUSED/i.test(message) ? 'browser-connection-refused'
        : /strict mode violation/i.test(message) ? 'browser-selector-not-unique'
        : error.name === 'TimeoutError' ? 'browser-timeout'
        : error.name === 'TypeError' ? 'browser-type-error'
        : 'live-browser-assertion-failed';
    evidence.failure = { phase, label: error.safeLabel || category };
    process.exitCode = 1;
} finally {
    await browser?.close();
    evidence.finishedUtc = new Date().toISOString();
    if (output) await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
}
