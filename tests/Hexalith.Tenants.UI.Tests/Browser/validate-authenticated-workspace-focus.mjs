// Run against the real local Aspire UI. Reuse the pinned Playwright install from
// tests/performance/tenant-audit (npm ci there), or supply an existing module path.
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import { readFile, writeFile } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const playwrightModule = process.env.TENANTS_BROWSER_PLAYWRIGHT_MODULE || 'playwright';
const { chromium } = require(playwrightModule);
const base = new URL(process.env.TENANTS_BROWSER_BASE_URL);
assert.ok(['localhost', '127.0.0.1', '[::1]'].includes(base.hostname), 'Use a local Aspire endpoint');
const username = process.env.TENANTS_BROWSER_USERNAME;
const password = process.env.TENANTS_BROWSER_PASSWORD;
assert.ok(username && password, 'Supply the credentials chosen before starting the local realm');
const output = process.argv[2];
assert.ok(output, 'Supply a safe result JSON path');
const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim();
const hash = value => createHash('sha256').update(value).digest('hex');
const evidence = {
    sourceBaseRevision: git('rev-parse', 'HEAD'),
    sourceDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src/Hexalith.Tenants.UI')),
    validatorSha256: hash(await readFile(fileURLToPath(import.meta.url))),
    localUiAssemblySha256: hash(await readFile(resolve(root, 'src/Hexalith.Tenants.UI/bin/Debug/net10.0/Hexalith.Tenants.UI.dll'))),
    playwrightVersion: require(`${playwrightModule}/package.json`).version,
    startedUtc: new Date().toISOString(),
    records: ['DW-333', 'DW-335', 'DW-336', 'DW-357', 'DW-359'],
    cases: [],
};
let phase = 'launch';
let browser;
let context;
let page;
let interaction;
const circuitErrors = [];
const keyboardTrace = [];
const controls = {
    acknowledge: 'tenants-global-admin-grant-acknowledge',
    confirm: 'tenants-global-admin-grant-confirm',
    cancel: 'tenants-global-admin-grant-preview-cancel',
    launcher: 'tenants-global-admin-grant-submit',
    start: 'tenants-global-admin-grant-focus-start',
    end: 'tenants-global-admin-grant-focus-end',
};
const observeFocus = async () => page.evaluate(controls => {
    let active = document.activeElement;
    let shadowDepth = 0;
    while (active?.shadowRoot?.activeElement) { active = active.shadowRoot.activeElement; shadowDepth++; }
    let control = 'other';
    for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
        const found = Object.entries(controls).find(([, id]) => node.getAttribute?.('data-testid') === id);
        if (found) { control = found[0]; break; }
    }
    const launcher = document.querySelector('[data-testid="tenants-global-admin-grant-submit"]');
    return { control, element: active?.localName, shadowDepth, width: innerWidth,
        launcherDisabled: launcher?.hasAttribute('disabled'),
        backgroundInert: document.querySelector('[data-testid="tenants-global-admins-area"]')?.hasAttribute('inert') };
}, controls);

// Report only fixed assertion labels and measured UI facts. Never persist login
// URLs, cookies, storage state, screenshots, network bodies or exception stacks.
try {
    browser = await chromium.launch({
        executablePath: process.env.CHROMIUM_BIN || undefined,
        args: ['--no-sandbox'],
    });
    evidence.browserVersion = browser.version();
    context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 1000 } });
    page = await context.newPage();
    page.setDefaultTimeout(20_000);
    page.on('pageerror', () => circuitErrors.push('pageerror'));
    page.on('console', message => {
        if (message.type() === 'error' && /applying batch|current circuit|circuit will be terminated/i.test(message.text())) {
            circuitErrors.push('Blazor circuit failure');
        }
    });
    const check = (condition, label) => {
        if (!condition) throw Object.assign(new Error(), { safeLabel: label });
    };
    const testId = value => page.getByTestId(value);
    const focus = async selector => page.evaluate(selector => {
        let active = document.activeElement;
        while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
        for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
            if (node.matches?.(selector)) return true;
        }
        return false;
    }, selector);
    const waitFocus = async selector => {
        // Match the browser operation deadline; optional circuit setup has its
        // own ten-second budget and may precede an after-render focus callback.
        const deadline = Date.now() + 20_000;
        while (Date.now() < deadline) {
            const sentinel = '[data-testid="tenants-global-admin-grant-focus-start"], [data-testid="tenants-global-admin-grant-focus-end"]';
            if (await focus(selector) && !await focus(sentinel)) return;
            await page.waitForTimeout(50);
        }
        throw Object.assign(new Error(), { safeLabel: `Expected focus target was not reached: ${selector}` });
    };

    phase = 'authenticate';
    await page.goto(new URL('/authentication/challenge?returnUrl=%2Ftenants', base).href);
    await page.locator('#username').fill(username);
    await page.locator('#password').fill(password);
    await page.locator('#kc-login').click();
    await page.waitForURL(url => url.origin === base.origin && !url.pathname.includes('signin-oidc'));
    await testId('tenants-workspace').waitFor();
    // The workspace is prerendered before its Blazor event handlers attach.
    // Wait for startup scripts and circuit negotiation before native tab input.
    await page.waitForLoadState('networkidle');
    const session = await context.newCDPSession(page);
    await session.send('DOM.enable');

    // Activate both retained panels before testing either hidden direction.
    phase = 'workspace-tabs';
    for (const active of ['workspace-users', 'tenants', 'workspace-users', 'tenants']) {
        interaction = { phase, action: 'select-tab', control: active };
        const inactive = active === 'tenants' ? 'workspace-users' : 'tenants';
        await page.locator(`#${active}`).click();
        await page.locator(`#${active}[aria-selected="true"]`).waitFor();
        await page.locator(`#${active}-panel`).waitFor({ state: 'visible' });
        await testId(`tenants-workspace-${active === 'tenants' ? 'tenants' : 'users'}-panel`).waitFor({ state: 'visible' });
        await page.locator(`#${inactive}-panel`).waitFor({ state: 'hidden' });
        await page.waitForFunction(inactive => document.getElementById(`${inactive}-panel`)?.hidden === true, inactive);
        check(await page.locator(`#${inactive}-panel`).count() === 1, 'Inactive panel must remain mounted');
        check(await testId(`tenants-workspace-${inactive === 'tenants' ? 'tenants' : 'users'}-panel`).count() === 1,
            'Inactive panel content must remain mounted');
        const visible = await page.locator(`#${inactive}-panel`).evaluate(element =>
            [...element.getClientRects()].some(rect => rect.width > 0 && rect.height > 0)
                && getComputedStyle(element).visibility === 'visible');
        check(!visible, 'Inactive retained panel must be visually hidden');
        const object = await session.send('Runtime.evaluate', {
            expression: `document.getElementById(${JSON.stringify(`${inactive}-panel`)})`,
        });
        const { node } = await session.send('DOM.describeNode', { objectId: object.result.objectId, depth: -1, pierce: true });
        const backendIds = new Set();
        const collect = node => {
            backendIds.add(node.backendNodeId);
            for (const child of [...(node.children || []), ...(node.shadowRoots || [])]) collect(child);
        };
        collect(node);
        await session.send('Runtime.releaseObject', { objectId: object.result.objectId });
        const ax = await session.send('Accessibility.getFullAXTree');
        check(!ax.nodes.some(node => !node.ignored && backendIds.has(node.backendDOMNodeId)),
            'Inactive panel and descendants must be excluded from the accessibility tree');
        check(ax.nodes.filter(node => !node.ignored && node.role?.value === 'tabpanel').length === 1,
            'Exactly one workspace panel must be exposed');
        await page.locator(`#${active}`).focus();
        for (const key of ['Tab', 'Shift+Tab']) {
            for (let step = 0; step < 30; step++) {
                await page.keyboard.press(key);
                check(!await focus(`#${inactive}-panel`), 'Native keyboard navigation entered the inactive panel');
            }
        }
        evidence.cases.push({ scenario: 'retained-tab', active, inactive, visuallyHidden: true,
            accessibilityExcluded: true, nativeTabSteps: 30, nativeShiftTabSteps: 30 });
    }

    phase = 'administrator-readiness';
    await page.goto(new URL('/global-administrators', base).href);
    await page.waitForLoadState('networkidle');
    await testId('tenants-global-admins-ready').waitFor();
    const launcher = testId('tenants-global-admin-grant-submit');
    const dialogSelector = '[data-testid="tenants-global-admin-grant-preview"]';
    const cancelSelector = '[data-testid="tenants-global-admin-grant-preview-cancel"]';
    const input = page.locator('input[name="global-administrator-user-id"]');
    await input.waitFor();
    const backgroundSelector = '[data-testid="tenants-global-admin-grant-lifecycle"]';
    await page.locator(backgroundSelector).focus();
    await waitFocus(backgroundSelector);
    evidence.backgroundFocusPositiveControl = true;
    check(!await input.getAttribute('maxlength'), 'Literal identity input must not truncate pasted identities');
    check(!await input.evaluate(element => [...element.attributes].some(attribute => attribute.name.includes('@*'))),
        'Razor comments must not become DOM attributes');

    const openPreview = async () => {
        await input.fill('browser-preview-only-user');
        await launcher.click();
        await page.locator(dialogSelector).waitFor();
        await waitFocus('[data-testid="tenants-global-admin-grant-acknowledge"]');
    };
    for (const width of [1366, 768, 1024]) {
        phase = `grant-dialog-${width}`;
        await page.setViewportSize({ width, height: 1000 });
        await launcher.waitFor({ state: 'visible' });
        await page.waitForFunction(() => !document.querySelector('[data-testid="tenants-global-admin-grant-submit"]')?.hasAttribute('disabled'));
        await openPreview();
        check(await testId('tenants-global-admins-area').getAttribute('inert') !== null, 'Dialog background must be inert');
        check(await testId('tenants-global-admins-area').getAttribute('aria-hidden') === 'true', 'Dialog background must be hidden from accessibility');
        await page.locator(backgroundSelector).evaluate(element => element.focus());
        check(!await focus(backgroundSelector), 'Inert background accepted programmatic focus');
        check(await testId('tenants-global-admin-grant-confirm').getAttribute('disabled') !== null,
            'Unchecked acknowledgement must disable Confirm');
        const traverse = async (steps, expectedControls) => {
            for (const key of ['Tab', 'Shift+Tab']) {
                const seen = new Set();
                for (let step = 0; step < steps; step++) {
                    interaction = { phase, key, step };
                    await page.keyboard.press(key);
                    // Sentinels restore focus through an asynchronous Blazor callback.
                    await waitFocus(dialogSelector);
                    const observed = await observeFocus();
                    keyboardTrace.push({ ...interaction, control: observed.control,
                        element: observed.element, shadowDepth: observed.shadowDepth });
                    check(expectedControls.includes(observed.control), 'Native keyboard cycle entered an unexpected control');
                    for (const control of expectedControls) {
                        if (await focus(`[data-testid="${controls[control]}"]`)) seen.add(control);
                    }
                }
                check(seen.size === expectedControls.length, 'Native keyboard cycle missed a dialog control');
            }
        };
        // Cover both enabled and disabled Confirm traversal, without submitting it.
        await traverse(6, ['acknowledge', 'cancel']);
        await testId('tenants-global-admin-grant-acknowledge').click();
        await page.waitForFunction(() => !document.querySelector('[data-testid="tenants-global-admin-grant-confirm"]')?.hasAttribute('disabled'));
        await traverse(12, ['acknowledge', 'confirm', 'cancel']);
        // Exercise a real inner shadow control if the shipped Fluent version has
        // one. V5 currently focuses the custom host and has no inner focus target.
        await page.locator(cancelSelector).evaluate(element =>
            (element.shadowRoot?.querySelector('button, input, [tabindex]:not([tabindex="-1"])') || element).focus());
        await waitFocus(cancelSelector);
        const escapeFocus = await page.evaluate(() => {
            let active = document.activeElement;
            let shadowDepth = 0;
            while (active?.shadowRoot?.activeElement) { active = active.shadowRoot.activeElement; shadowDepth++; }
            const cancel = document.querySelector('[data-testid="tenants-global-admin-grant-preview-cancel"]');
            return { element: active?.localName, shadowDepth, shadowRootPresent: Boolean(cancel?.shadowRoot),
                shadowFocusableDescendants: cancel?.shadowRoot?.querySelectorAll('button, input, [tabindex]:not([tabindex="-1"])').length || 0 };
        });
        check(escapeFocus.shadowFocusableDescendants === 0 || escapeFocus.shadowDepth > 0,
            'Escape must start inside the shipped shadow control when it exists');
        const escapeStarted = Date.now();
        await page.keyboard.press('Escape');
        await page.locator(dialogSelector).waitFor({ state: 'detached' });
        await waitFocus('[data-testid="tenants-global-admin-grant-submit"]');
        const restoredFocus = await observeFocus();
        const restorationDurationMs = Date.now() - escapeStarted;
        check(await testId('tenants-global-admins-area').getAttribute('inert') === null, 'Cancellation must release background inertness');
        evidence.cases.push({ scenario: 'grant-modal', width: await page.evaluate(() => innerWidth),
            nativeTabSteps: 18, nativeShiftTabSteps: 18, disabledConfirmCycle: true, enabledConfirmCycle: true,
            inertBackground: true, escapeFromFluent: true, escapeFocus, launcherRestored: true, restoredFocus, restorationDurationMs });
    }

    phase = 'viewport-downgrade';
    await openPreview();
    await testId('tenants-global-admin-grant-acknowledge').click();
    await page.waitForFunction(() => {
        const confirm = document.querySelector('[data-testid="tenants-global-admin-grant-confirm"]');
        return confirm && !confirm.hasAttribute('disabled');
    });
    await page.setViewportSize({ width: 767, height: 800 });
    await page.waitForFunction(() => document.querySelector('[data-testid="tenants-global-admin-grant-confirm"]')?.hasAttribute('disabled'));
    await page.locator(cancelSelector).click();
    await page.locator(dialogSelector).waitFor({ state: 'detached' });
    await page.waitForFunction(() => document.querySelector('[data-testid="tenants-global-admin-grant-submit"]')?.hasAttribute('disabled'));
    check(await launcher.getAttribute('disabled') !== null, 'Unsafe measured viewport must disable grant initiation');
    evidence.cases.push({ scenario: 'viewport-downgrade', width: await page.evaluate(() => innerWidth), confirmationDisabled: true, cancelAvailable: true, initiationDisabled: true });
    // The existing layout hides mutation launchers below 42rem; the measured
    // safety gate disables them throughout the wider Phone tier as well.
    await page.setViewportSize({ width: 390, height: 800 });
    await launcher.waitFor({ state: 'hidden' });
    check(!await launcher.isVisible(), 'Phone viewport must hide grant initiation');
    check((await testId('tenants-global-admin-grant-lifecycle').getAttribute('class')).includes('--idle'),
        'Preview and cancellation must leave the grant lifecycle idle');
    evidence.cases.push({ scenario: 'phone-layout', width: await page.evaluate(() => innerWidth), initiationHidden: true, lifecycleIdle: true });
    check(circuitErrors.length === 0, 'The interactive Blazor circuit reported an error');
    evidence.status = 'passed';
} catch (error) {
    // Playwright exceptions can contain filled values or OIDC URLs. Keep them local
    // to the process and publish only the phase needed to reproduce a failure.
    evidence.status = 'failed';
    evidence.failedPhase = phase;
    evidence.failureLabel = error.safeLabel || 'Browser operation did not complete';
    evidence.failedInteraction = interaction;
    if (page && !page.isClosed()) evidence.observedFocus = await observeFocus().catch(() => null);
    process.exitCode = 1;
} finally {
    evidence.circuitErrorCount = circuitErrors.length;
    evidence.keyboardTrace = keyboardTrace;
    evidence.finishedUtc = new Date().toISOString();
    await context?.close();
    await browser?.close();
    await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
    console.log(`Authenticated workspace focus: ${evidence.status}; ${evidence.cases.length} cases; phase ${phase}`);
}
