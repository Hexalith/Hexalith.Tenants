// Authenticate through the existing Aspire host, then exercise the rebuilt production
// UI in a separate process with controlled, read-only loopback upstream responses.
// The fixture never changes domain state and rejects all command requests.
import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import { execFileSync, spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { readFile, writeFile, mkdtemp, open } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const playwrightModule = process.env.TENANTS_BROWSER_PLAYWRIGHT_MODULE || 'playwright';
const { chromium } = require(playwrightModule);
const playwrightRoot = dirname(require.resolve(`${playwrightModule}/package.json`));
const { wsServer: WebSocketServer } = require(resolve(playwrightRoot, '../playwright-core/lib/utilsBundle.js'));
const base = new URL(process.env.TENANTS_BROWSER_BASE_URL);
assert.equal(base.hostname, 'localhost', 'Use the registered localhost Aspire endpoint so authentication cookies reach the fixture');
const username = process.env.TENANTS_BROWSER_USERNAME;
const password = process.env.TENANTS_BROWSER_PASSWORD;
assert.ok(username && password, 'Supply the chosen local realm credentials through environment variables');
assert.match(process.env.TENANTS_BROWSER_SOURCE_UI_PID || '', /^\d+$/, 'Supply the current Aspire UI process id');
const output = process.argv[2];
assert.ok(output, 'Supply a safe result JSON path');
const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8' }).trim();
const hash = value => createHash('sha256').update(value).digest('hex');
const uiDirectory = resolve(root, 'src/Hexalith.Tenants.UI');
const assembly = resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.Tenants.UI.dll');
const evidence = {
    playwrightVersion: require(`${playwrightModule}/package.json`).version,
    startedUtc: new Date().toISOString(),
    hostMode: 'Production authentication and Razor components; separate UI process; controlled read-only HTTP and SignalR upstream',
    fixtureScope: 'Qualifies composed layout and focus with deterministic authorized projection responses; does not qualify backend authorization or real domain data',
    records: ['DW-268', 'DW-489'],
    cases: [],
};
let tenantId = 'tenant.navigation';
const targetId = 'layout-target-' + 'x'.repeat(160);
const eventReference = 'navigation-role-event';
const memoriesFixtureToken = randomUUID();
const summary = id => ({ tenantId: id, name: `needle ${id}`, status: 'Active' });
let callerId;
const member = { userId: targetId, role: 'TenantContributor' };
const audit = { eventId: eventReference, eventType: 'UserRoleChanged', category: 'Access', actorId: 'navigation-actor',
    timestamp: '2026-10-24T12:00:00Z', tenantId, narrativePayload: { userId: targetId, oldRole: 'TenantReader', newRole: 'TenantContributor' } };
const fixture = { holdList: false, terminalList: true, listReads: 0, heldListReads: 0, auditReads: 0,
    resetAudit: false, auditUnavailable: false, missingAuditLauncher: false, missingPager: false, searchFallback: false, searchReads: 0, commands: 0, authorizedReads: 0, subscriptions: new Set() };
let releaseList;
const captureSource = async () => {
    const untrackedHash = async repository => {
        const paths = git('-C', repository, 'ls-files', '--others', '--exclude-standard', '-z', '--', 'src').split('\0').filter(Boolean).sort();
        const digest = createHash('sha256');
        for (const path of paths) {
            digest.update(path).update('\0').update(await readFile(resolve(root, repository, path))).update('\0');
        }
        return digest.digest('hex');
    };
    return {
        sourceBaseRevision: git('rev-parse', 'HEAD'),
        sourceDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src/Hexalith.Tenants.UI')),
        buildInputsDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json')),
        untrackedSourceSha256: await untrackedHash('.'),
        frontComposerBaseRevision: git('-C', 'references/Hexalith.FrontComposer', 'rev-parse', 'HEAD'),
        frontComposerDiffSha256: hash(git('-C', 'references/Hexalith.FrontComposer', 'diff', '--binary', 'HEAD', '--', 'src', 'Directory.Build.props', 'Directory.Build.targets', 'Directory.Packages.props', 'global.json')),
        frontComposerUntrackedSourceSha256: await untrackedHash('references/Hexalith.FrontComposer'),
    };
};
let sourceBefore;
let validatorBefore;
let phase = 'fixture-start';
let browser;
let context;
let page;
let ui;
let uiExited = false;
let uiStartupFailure = false;
let uiExit;
let upstream;
let sockets;
let uiLog;
const circuitErrors = [];
const check = (condition, label) => { if (!condition) throw Object.assign(new Error(), { safeLabel: label }); };
const wait = async (condition, label, timeout = 20_000) => {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) { if (await condition()) return; await new Promise(resolve => setTimeout(resolve, 50)); }
    throw Object.assign(new Error(), { safeLabel: label });
};
const freePort = async () => {
    const server = createServer();
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const port = server.address().port;
    await new Promise(resolve => server.close(resolve));
    return port;
};
const observe = () => page.evaluate(() => {
    let active = document.activeElement;
    let shadowDepth = 0;
    while (active?.shadowRoot?.activeElement) { active = active.shadowRoot.activeElement; shadowDepth++; }
    const ancestry = [];
    for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
        if (node.id || node.getAttribute?.('data-testid')) ancestry.push({ id: node.id || null, testId: node.getAttribute?.('data-testid') || null });
    }
    return { element: active?.localName, shadowDepth, ancestry };
});
const focused = selector => page.evaluate(selector => {
    let active = document.activeElement;
    while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
    for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
        if (node.matches?.(selector)) return true;
    }
    return false;
}, selector);
const waitFocus = selector => wait(() => focused(selector), `Expected focus: ${selector}`);
const record = async (name, extra = {}) => evidence.cases.push({ name, passed: true, focus: await observe(), ...extra });
const testId = id => page.getByTestId(id);
const notify = () => {
    for (const socket of sockets.clients) if (socket.readyState === 1) {
        socket.send(JSON.stringify({ type: 1, target: 'ProjectionChanged', arguments: ['tenants', tenantId] }) + '\x1e');
    }
};
try {
    phase = 'build-current-ui';
    sourceBefore = await captureSource();
    validatorBefore = hash(await readFile(fileURLToPath(import.meta.url)));
    const privateDirectory = await mkdtemp(resolve(tmpdir(), 'tenants-navigation-browser-'));
    const buildArguments = ['build', 'src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj', '-c', 'Debug', '-m:1',
        '-p:UseNuGetDeps=false'];
    evidence.build = { executable: 'dotnet', arguments: buildArguments, succeeded: false };
    if (process.env.TENANTS_BROWSER_SKIP_BUILD === '1') {
        evidence.build.reusedForDebug = true;
    } else try {
        const buildOutput = execFileSync('dotnet', buildArguments, { cwd: root, encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
        await writeFile(resolve(privateDirectory, 'build.log'), buildOutput, { mode: 0o600 });
        evidence.build.succeeded = true;
    } catch {
        throw Object.assign(new Error(), { safeLabel: 'Current Debug source build failed' });
    }
    check(JSON.stringify(sourceBefore) === JSON.stringify(await captureSource()), 'Source changed during build');
    check(validatorBefore === hash(await readFile(fileURLToPath(import.meta.url))), 'Validator changed during build');
    evidence.build.sourceBefore = sourceBefore;
    Object.assign(evidence, {
    sourceBaseRevision: git('rev-parse', 'HEAD'),
    sourceDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src/Hexalith.Tenants.UI')),
    frontComposerBaseRevision: git('-C', 'references/Hexalith.FrontComposer', 'rev-parse', 'HEAD'),
    frontComposerDiffSha256: hash(git('-C', 'references/Hexalith.FrontComposer', 'diff', '--binary', 'HEAD', '--', 'src/Hexalith.FrontComposer.Shell')),
    validatorSha256: validatorBefore,
    localUiAssemblySha256: hash(await readFile(assembly)),
    localShellAssemblySha256: hash(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'))),
    });
    phase = 'fixture-start';
    upstream = createServer(async (request, response) => {
        const url = new URL(request.url, 'http://localhost');
        const send = (body, status = 200) => {
            response.writeHead(status, { 'Content-Type': 'application/json', ETag: '"navigation-fixture"',
                'X-Hexalith-Query-Provenance': 'ProjectionBacked', 'X-Hexalith-Projection-Version': 'navigation-fixture-1',
                'X-Hexalith-Projection-Lifecycle': 'Current', 'X-Hexalith-Is-Stale': 'false', 'X-Hexalith-Is-Degraded': 'false' });
            response.end(JSON.stringify(body));
        };
        if (url.pathname === '/hubs/projection-changes/negotiate' && request.method === 'POST') {
            const id = randomUUID();
            send({ negotiateVersion: 1, connectionId: id, connectionToken: id,
                availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] });
            return;
        }
        if (request.method !== 'GET') { fixture.commands++; send({ reason: 'commands-prohibited' }, 405); return; }
        if (!request.headers.authorization?.startsWith('Bearer ')) { send({}, 401); return; }
        fixture.authorizedReads++;
        // Match the caller's authenticated tenant for the production subscription guard.
        // Token contents stay in memory and are never written to evidence or logs.
        if (url.pathname !== '/api/v1/search') {
            const claims = JSON.parse(Buffer.from(request.headers.authorization.slice(7).split('.')[1], 'base64url'));
            callerId = claims.sub;
            const currentTenant = claims['eventstore:current-tenant'];
            if (typeof currentTenant === 'string' && /^[a-zA-Z0-9._-]+$/.test(currentTenant)) tenantId = currentTenant;
        } else if (request.headers.authorization !== `Bearer ${memoriesFixtureToken}`) {
            send({}, 401); return;
        }
        if (url.pathname === '/api/v1/search') {
            fixture.searchReads++;
            if (fixture.searchFallback) { send({}, 503); return; }
            if (fixture.holdList) {
                fixture.heldListReads++;
                await new Promise(resolve => { releaseList = resolve; });
            }
            const offset = Number(url.searchParams.get('offset') || 0);
            send({ query: 'needle', totalCount: 60, hasIndexedMemoryUnits: true, axesUsed: ['syntactic'],
                results: Array.from({ length: 20 }, (_, index) => ({ memoryUnitId: `navigation-${offset + index}`,
                    score: 1, contentSnippet: 'fixture', sourceUri: `tenant:tenant.search.${offset + index}`, sourceType: 'projection', axis: 'syntactic' })) });
        } else if (url.pathname === '/api/tenants') {
            fixture.listReads++;
            const cursor = url.searchParams.get('cursor');
            if (fixture.holdList) {
                fixture.heldListReads++;
                await new Promise(resolve => { releaseList = resolve; });
            }
            if (cursor && fixture.missingPager) { send({}, 503); return; }
            send({ items: [summary(cursor ? 'tenant.secondary' : tenantId)],
                cursor: cursor ? (fixture.terminalList ? null : 'list-page-3') : 'list-page-2',
                hasMore: cursor ? !fixture.terminalList : true });
        } else if (url.pathname === `/api/tenants/${tenantId}/audit`) {
            fixture.auditReads++;
            if (fixture.auditUnavailable) { send({}, 503); return; }
            const cursor = url.searchParams.get('cursor');
            if (cursor && fixture.resetAudit) {
                fixture.resetAudit = false;
                send({ reason: 'invalid-cursor', status: 400 }, 400);
            } else {
                send({ items: fixture.missingAuditLauncher ? [] : [{ ...audit, tenantId }],
                    cursor: !cursor && !fixture.missingAuditLauncher ? 'audit-page-2' : null, hasMore: !cursor && !fixture.missingAuditLauncher });
            }
        } else if (url.pathname === `/api/tenants/${tenantId}` || /^\/api\/tenants\/tenant\.search\.\d+$/.test(url.pathname)) {
            send({ ...summary(url.pathname.split('/').at(-1)), description: 'Navigation read fixture', members: [{ userId: callerId, role: 'TenantOwner' }, member], configuration: {}, createdAt: '2026-10-23T10:00:00Z' });
        } else if (url.pathname === '/api/global-administrators') {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/^\/api\/users\/[^/]+\/tenants$/.test(url.pathname)) {
            send({ items: [{ tenantId, name: 'Layout fixture', status: 'Active', role: 'TenantContributor' }], cursor: null, hasMore: false });
        } else if (/\/users$/.test(url.pathname)) {
            send({ items: [{ userId: callerId, role: 'TenantOwner' }, member], cursor: null, hasMore: false });
        } else { send({}, 404); }
    });
    sockets = new WebSocketServer({ noServer: true });
    upstream.on('upgrade', (request, socket, head) => {
        if (!request.url.startsWith('/hubs/projection-changes')) { socket.destroy(); return; }
        sockets.handleUpgrade(request, socket, head, connection => sockets.emit('connection', connection, request));
    });
    sockets.on('connection', socket => {
        socket.on('message', data => {
            for (const part of data.toString().split('\x1e').filter(Boolean)) {
                const message = JSON.parse(part);
                if (message.protocol) socket.send('{}\x1e');
                else if (message.type === 1) {
                    if (message.target?.startsWith('JoinGroup')) fixture.subscriptions.add(message.arguments.join(':'));
                    if (message.invocationId) socket.send(JSON.stringify({ type: 3, invocationId: message.invocationId }) + '\x1e');
                }
            }
        });
    });
    await new Promise(resolve => upstream.listen(0, '127.0.0.1', resolve));
    const upstreamBase = `http://127.0.0.1:${upstream.address().port}`;
    const uiPort = await freePort();
    const fixtureBase = `https://localhost:${uiPort}`;
    const originalEnvironment = Object.fromEntries((await readFile(`/proc/${process.env.TENANTS_BROWSER_SOURCE_UI_PID}/environ`, 'utf8'))
        .split('\0').filter(Boolean).map(entry => { const index = entry.indexOf('='); return [entry.slice(0, index), entry.slice(index + 1)]; }));
    const environment = { ...originalEnvironment, ...process.env, ASPNETCORE_URLS: fixtureBase, ASPNETCORE_HTTPS_PORT: String(uiPort),
        Tenants__BaseAddress: upstreamBase, EventStore__BaseAddress: upstreamBase, Memories__BaseAddress: upstreamBase,
        HEXALITH_MEMORIES_API_TOKEN: memoriesFixtureToken };
    for (const key of Object.keys(environment)) if (/^(OTEL_|ASPIRE_)/.test(key)) delete environment[key];
    uiLog = await open(resolve(privateDirectory, 'ui.log'), 'w', 0o600);
    ui = spawn('dotnet', [assembly, '--urls', fixtureBase], { cwd: uiDirectory, env: environment, stdio: ['ignore', uiLog.fd, uiLog.fd] });
    uiExit = new Promise(resolve => {
        ui.once('error', () => { uiStartupFailure = true; uiExited = true; resolve(); });
        ui.once('exit', () => { uiExited = true; resolve(); });
    });
    browser = await chromium.launch({ executablePath: process.env.CHROMIUM_BIN || undefined, args: ['--no-sandbox'] });
    evidence.browserVersion = browser.version();
    context = await browser.newContext({ ignoreHTTPSErrors: true, viewport: { width: 1366, height: 1000 } });
    await wait(async () => {
        check(!uiStartupFailure, 'Separate production UI process could not start');
        check(!uiExited, 'Separate production UI process exited during startup');
        try { const response = await context.request.get(`${fixtureBase}/js/tenantsFocus.js`); return response.status() === 200; } catch { return false; }
    }, 'Separate production UI did not start', 30_000);
    page = await context.newPage();
    page.setDefaultTimeout(20_000);
    page.on('pageerror', () => circuitErrors.push('pageerror'));
    page.on('console', message => {
        if (message.type() === 'error' && /applying batch|current circuit|circuit will be terminated/i.test(message.text())) circuitErrors.push('Blazor circuit failure');
    });
    // Relay only the registered OIDC entry/callback to the separate production process.
    // Retaining the registered Host keeps the realm configuration intact, and completing
    // its own real code exchange fills that process's in-memory bearer-token relay store.
    await context.route(url => url.origin === base.origin
        && ['/authentication/challenge', '/signin-oidc'].includes(url.pathname), async route => {
        const request = route.request();
        const original = new URL(request.url());
        const response = await route.fetch({ url: fixtureBase + original.pathname + original.search,
            headers: { ...request.headers(), host: base.host }, maxRedirects: 0 });
        await route.fulfill({ response });
    });
    phase = 'authenticate-original-aspire-host';
    await page.goto(new URL('/authentication/challenge?returnUrl=%2Ftenants', base).href);
    await page.locator('#username').fill(username);
    await page.locator('#password').fill(password);
    await page.locator('#kc-login').click();
    await page.waitForURL(url => url.origin === base.origin && !url.pathname.includes('signin-oidc'));
    await testId('tenants-workspace').waitFor();
    const workspace = async (search = false) => {
        await page.goto(`${fixtureBase}/tenants/tenants${search ? '?search=needle' : ''}`);
        await testId('tenants-list-next').waitFor();
        await page.waitForLoadState('networkidle');
    };

    await workspace();
    const layouts = evidence.layouts = [];
    const measure = async (selector, name, { minHeight, display, gap, allowScroll = false } = {}) => {
        const element = page.locator(selector).first();
        await element.waitFor({ state: 'visible' });
        await element.scrollIntoViewIfNeeded();
        const measurement = await element.evaluate(element => {
            const rect = element.getBoundingClientRect();
            const style = getComputedStyle(element);
            return { width: innerWidth, height: innerHeight, left: rect.left, right: rect.right,
                elementWidth: rect.width, elementHeight: rect.height, scrollWidth: element.scrollWidth,
                clientWidth: element.clientWidth, display: style.display, gap: style.gap, minHeight: style.minHeight,
                minInlineSize: style.minInlineSize, maxInlineSize: style.maxInlineSize, whiteSpace: style.whiteSpace,
                overflowX: style.overflowX };
        });
        layouts.push({ name, ...measurement });
        check(measurement.left >= -1 && measurement.right <= measurement.width + 1, `${name}: horizontal bounds`);
        check(allowScroll || measurement.scrollWidth <= measurement.clientWidth + 1, `${name}: text must wrap without clipping`);
        if (minHeight) check(measurement.elementHeight >= minHeight, `${name}: minimum target height`);
        if (display) check(measurement.display === display, `${name}: computed display`);
        if (gap) check(measurement.gap === gap, `${name}: computed gap`);
        return measurement;
    };
    const scopeControl = async (selector, ownerSelector, property, expected) => {
        const observation = await page.locator(selector).first().evaluate((element, args) => {
            const owner = element.closest(args.ownerSelector);
            const attributes = [...owner.attributes].filter(a => /^b-[a-z0-9]+$/.test(a.name)).map(a => [a.name, a.value]);
            const before = getComputedStyle(element)[args.property];
            try {
                for (const [name] of attributes) owner.removeAttribute(name);
                return { attributes: attributes.length, before, withoutScope: getComputedStyle(element)[args.property] };
            } finally { for (const [name, value] of attributes) owner.setAttribute(name, value); }
        }, { ownerSelector, property });
        const restored = await page.locator(selector).first().evaluate((element, property) => getComputedStyle(element)[property], property);
        check(observation.attributes === 1 && observation.before === expected && observation.withoutScope !== expected
            && restored === expected, `Scope negative control: ${selector}`);
        evidence.cases.push({ name: 'Owner-scope removal rejects styling and restoration recovers it', selector, property, ...observation, restored });
    };
    const auditUrl = () => `${fixtureBase}/tenants/${tenantId}/audit`;
    for (const viewport of [
        { width: 1366, height: 1000 }, { width: 1024, height: 768 }, { width: 768, height: 1000 },
        { width: 767, height: 844 }, { width: 390, height: 844 }, { width: 1024, height: 400 },
    ]) {
        phase = `composed-layout-${viewport.width}-${viewport.height}`;
        await page.setViewportSize(viewport);
        await page.goto(auditUrl());
        await testId('tenants-audit-grid').waitFor();
        await page.waitForLoadState('networkidle');
        await measure('[data-testid="tenants-audit-grid"]', 'audit-grid', { allowScroll: true });
        await measure('.tenant-audit__support', 'audit-support', { display: 'grid', gap: '12px' });
        if (viewport.width === 1366) await scopeControl('.tenant-audit__support', '.tenant-audit-style-scope', 'display', 'grid');
        const scroll = await testId('tenants-audit-grid').evaluate(element => {
            const before = element.scrollLeft; element.scrollLeft = element.scrollWidth;
            const reached = element.scrollLeft; element.scrollLeft = before;
            return { overflowX: getComputedStyle(element).overflowX, maxScroll: element.scrollWidth - element.clientWidth, reached };
        });
        check(scroll.overflowX === 'auto' && Math.abs(scroll.maxScroll - scroll.reached) <= 1, 'Audit grid horizontal scroll reaches the final column');
        layouts.push({ name: 'audit-grid-scroll', ...viewport, ...scroll });
        await testId('tenants-audit-receipt-open').first().click();
        await measure('[data-testid="tenants-audit-receipt"]', 'receipt');
        for (const [field, expected] of Object.entries({ actor: audit.actorId, target: targetId, scope: tenantId,
            outcome: 'User role changed', 'audit-reference': eventReference })) {
            check((await testId(`tenants-audit-receipt-${field}`).locator('dd').innerText()).trim() === expected, `Receipt ${field}: fixture value`);
        }
        for (const field of ['actor', 'target', 'scope', 'outcome', 'timestamp', 'projection', 'audit-reference']) {
            await measure(`[data-testid="tenants-audit-receipt-${field}"]`, `receipt-${field}`);
        }
        if (viewport.width < 768) {
            check(await testId('tenants-correction-start').count() === 0, 'Unsafe narrow viewport must refuse correction start');
            await measure('[data-testid="tenants-correction-unavailable-reason"][data-correction-origin="receipt"]', 'narrow-read-only-recovery');
            await record('Narrow composed audit and receipt remain readable with correction refused', viewport);
            continue;
        }
        for (const origin of ['grid', 'receipt']) {
            const surface = origin === 'grid' ? 'tenants-audit-grid' : 'tenants-audit-receipt';
            const role = page.locator(`[data-testid="${surface}"] [data-testid="tenants-correction-role"]`).first();
            await role.click();
            await page.keyboard.press('Home');
            await page.keyboard.press('ArrowDown');
            await page.keyboard.press('Enter');
            const launcher = page.locator(`[data-testid="${surface}"] [data-testid="tenants-correction-start"]`).first();
            await launcher.waitFor();
            if (origin === 'receipt') {
                await measure(`[data-testid="${surface}"] .audit-evidence-receipt__action`, 'receipt-action', { minHeight: 40 });
                if (viewport.width === 1366) await scopeControl('.audit-evidence-receipt__action', '.audit-evidence-receipt', 'minHeight', '40px');
            }
            await launcher.click();
            await measure('[data-testid="tenants-correction-start-panel"]', `${origin}-correction-start`);
            const expectedStart = { reference: eventReference, timestamp: '2026-10-24 12:00:00 UTC', scope: tenantId,
                target: targetId, 'current-role': 'Tenant contributor', 'intended-role': 'Tenant owner',
                projection: 'Current state from the tenant projection; current authority was checked.', command: 'Change user role' };
            for (const [field, expected] of Object.entries(expectedStart)) {
                check((await testId(`tenants-correction-start-${field}`).innerText()).trim() === expected, `Start ${field}: fixture value`);
            }
            for (const field of ['reference', 'timestamp', 'scope', 'target', 'current-role', 'intended-role', 'projection', 'command']) {
                await measure(`[data-testid="tenants-correction-start-${field}"]`, `${origin}-start-${field}`);
            }
            await testId('tenants-correction-start-handoff').click();
            await measure('[data-testid="tenants-correction-panel"]', `${origin}-correction-preview`);
            const facts = page.locator('[data-testid="tenants-correction-panel"] dd');
            check(await facts.count() >= 10, 'Composed correction must expose all ten preview facts');
            for (let index = 0; index < await facts.count(); index++) {
                const field = facts.nth(index);
                await field.scrollIntoViewIfNeeded();
                const bounds = await field.evaluate(element => {
                    const r = element.getBoundingClientRect();
                    return { textPresent: element.textContent.trim().length > 0, left: r.left, right: r.right,
                        width: innerWidth, scroll: element.scrollWidth, client: element.clientWidth };
                });
                check(bounds.textPresent && bounds.left >= -1 && bounds.right <= bounds.width + 1
                    && bounds.scroll <= bounds.client + 1, `${origin}: preview fact readable`);
            }
            const cancel = testId('tenants-correction-cancel');
            await cancel.scrollIntoViewIfNeeded();
            await cancel.focus();
            await waitFocus('[data-testid="tenants-correction-cancel"]');
            const cancelBounds = await cancel.boundingBox();
            check(cancelBounds.y >= 0 && cancelBounds.y + cancelBounds.height <= viewport.height + 1, 'Cancel must be reachable at short heights');
            await page.keyboard.press('Enter');
            await testId('tenants-correction-panel').waitFor({ state: 'detached' });
            await record('Composed start and ten-fact preview stay readable; Cancel is reachable', { origin, ...viewport });
        }
    }
    phase = 'entrypoint-and-membership-styles';
    for (const width of [390, 767, 768, 1024, 1366]) {
        await page.setViewportSize({ width, height: 1000 });
        await page.goto(`${fixtureBase}/tenants/${tenantId}`);
        const entry = page.locator('[data-testid="tenants-detail-audit-origin"]');
        await entry.waitFor();
        await page.waitForLoadState('networkidle');
        await page.locator('.tenants-audit-entrypoint').first().waitFor({ state: 'visible' });
        const style = await page.locator('.tenants-audit-entrypoint').first().evaluate(element => {
            const s = getComputedStyle(element), r = element.getBoundingClientRect();
            const parent = element.closest('[data-fc-page-header-metadata]') || element.closest('.tenants-audit-availability__action-shell');
            const p = parent.getBoundingClientRect(), ps = getComputedStyle(parent);
            const owner = element.closest('.tenants-audit-entrypoint-style-scope');
            return { ownerDisplay: getComputedStyle(owner).display, ownerWidth: owner.getBoundingClientRect().width,
                minInlineSize: s.minInlineSize, maxInlineSize: s.maxInlineSize, whiteSpace: s.whiteSpace, width: r.width, parentWidth: p.width - parseFloat(ps.paddingLeft) - parseFloat(ps.paddingRight) - parseFloat(ps.borderLeftWidth) - parseFloat(ps.borderRightWidth) };
        });
        layouts.push({ name: 'audit-entrypoint', viewport: width, ...style });
        check(style.minInlineSize === '128px' && style.maxInlineSize === '100%' && style.whiteSpace === 'normal', 'Audit entrypoint scoped host style');
        if (width === 1366) await scopeControl('.tenants-audit-entrypoint', '.tenants-audit-entrypoint-style-scope', 'minInlineSize', '128px');
        check(style.ownerDisplay === 'contents' && style.ownerWidth === 0, 'Scope owner must preserve Fluent layout without a box');
        if (width <= 640) check(style.parentWidth > 128 && Math.abs(style.width - style.parentWidth) < 2, 'Narrow entrypoint fills actual recovery region');
    }
    phase = 'unavailable-entrypoint-and-accessibility-media';
    fixture.auditUnavailable = true;
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto(`${fixtureBase}/tenants/${tenantId}`);
    const unavailable = page.locator('.tenants-audit-entrypoint--unavailable').first();
    await unavailable.waitFor();
    await page.waitForLoadState('networkidle');
    check(await unavailable.getAttribute('disabled') !== null, 'Unavailable audit button remains disabled');
    const unavailableStyle = await unavailable.evaluate(element => ({ min: getComputedStyle(element).minInlineSize,
        wrap: getComputedStyle(element).whiteSpace, reason: element.closest('.tenants-audit-entrypoint-style-scope').querySelector('.tenants-audit-entrypoint__reason')?.textContent.trim() }));
    evidence.unavailableEntrypoint = unavailableStyle;
    check(unavailableStyle.min === '128px' && unavailableStyle.wrap === 'normal' && unavailableStyle.reason?.length > 0,
        'Unavailable Fluent root and named explanation receive scoped treatment');
    await measure('.tenants-audit-entrypoint__reason', 'unavailable-entrypoint-reason');
    await scopeControl('.tenants-audit-entrypoint--unavailable', '.tenants-audit-entrypoint-style-scope', 'minInlineSize', '128px');
    evidence.cases.push({ name: 'Unavailable audit entrypoint remains styled and named', passed: true, ...unavailableStyle });
    fixture.auditUnavailable = false;
    await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
    await page.setViewportSize({ width: 1024, height: 768 });
    phase = 'accessibility-media-audit-load';
    await page.goto(auditUrl());
    await testId('tenants-audit-grid').waitFor();
    await page.waitForLoadState('networkidle');
    phase = 'accessibility-media-receipt-open';
    await testId('tenants-audit-receipt-open').first().click();
    await testId('tenants-audit-receipt').waitFor();
    phase = 'accessibility-media-computed-styles';
    const media = await testId('tenants-audit-receipt').evaluate(element => {
        const button = element.querySelector('[data-testid="tenants-audit-receipt-close"]');
        const probe = document.createElement('span'); element.append(probe);
        probe.style.border = '1px solid CanvasText';
        probe.style.outline = '2px solid Highlight';
        const canvas = getComputedStyle(probe).borderTopColor;
        const highlight = getComputedStyle(probe).outlineColor;
        probe.remove();
        const durationTargets = [button, element.querySelector('dd'), element.querySelector('.audit-evidence-receipt__state-icon')];
        const transitions = durationTargets.map(target => ({ tag: target.localName,
            normal: getComputedStyle(target).transitionDuration,
            before: getComputedStyle(target, '::before').transitionDuration,
            after: getComputedStyle(target, '::after').transitionDuration }));
        return { forcedColors: matchMedia('(forced-colors: active)').matches,
            reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
            border: getComputedStyle(element).borderColor, borderWidth: getComputedStyle(element).borderTopWidth,
            canvas, highlight, transitions };
    });
    evidence.accessibilityMedia = media;
    check(media.forcedColors && media.reducedMotion && media.border === media.canvas && media.borderWidth === '1px'
        && media.transitions.every(target => ['normal', 'before', 'after'].every(pseudo =>
            target[pseudo].split(',').every(value => parseFloat(value) <= 0.00001))),
        'Forced colors and reduced motion reach compiled receipt light-DOM children and pseudo-elements');
    await page.keyboard.press('Tab');
    await testId('tenants-audit-receipt-close').focus();
    await waitFocus('[data-testid="tenants-audit-receipt-close"]');
    const receiptOutline = await testId('tenants-audit-receipt-close').evaluate(element => {
        const style = getComputedStyle(element);
        return { focusVisible: element.matches(':focus-visible'), width: style.outlineWidth,
            offset: style.outlineOffset, style: style.outlineStyle, color: style.outlineColor };
    });
    evidence.receiptMediaFocus = receiptOutline;
    check(receiptOutline.focusVisible && receiptOutline.width === '2px' && receiptOutline.offset === '3px'
        && receiptOutline.style === 'solid' && receiptOutline.color === media.highlight,
        'Receipt keyboard-modality focus receives the actual Highlight outline');
    await page.goto(`${fixtureBase}/tenants/${tenantId}`);
    const mediaEntry = page.locator('.tenants-audit-entrypoint').first();
    await mediaEntry.waitFor(); await page.waitForLoadState('networkidle');
    await page.keyboard.press('Tab'); await mediaEntry.focus();
    await waitFocus('.tenants-audit-entrypoint');
    const entryOutline = await mediaEntry.evaluate(element => {
        const style = getComputedStyle(element);
        return { focusVisible: element.matches(':focus-visible'), width: style.outlineWidth,
            offset: style.outlineOffset, style: style.outlineStyle, color: style.outlineColor,
            adjust: style.forcedColorAdjust };
    });
    evidence.entrypointMediaFocus = entryOutline;
    check(entryOutline.focusVisible && entryOutline.width === '2px' && entryOutline.offset === '2px'
        && entryOutline.style === 'solid' && entryOutline.color === media.highlight && entryOutline.adjust === 'auto',
        'Entrypoint keyboard-modality focus and forced colors use the actual Highlight outline');
    evidence.cases.push({ name: 'Compiled forced-color focus and reduced-motion rules', passed: true, ...media, receiptOutline, entryOutline });
    await page.emulateMedia({ forcedColors: 'none', reducedMotion: 'no-preference' });
    for (const width of [390, 767, 768, 1024, 1366]) {
        phase = `lookup-scoped-styles-${width}`;
        await page.setViewportSize({ width, height: 1000 });
        await workspace();
        await page.locator('#workspace-users').click();
        await testId('tenants-user-lookup').waitFor();
        await page.locator('[data-testid="tenants-user-lookup-input"] input, input[data-testid="tenants-user-lookup-input"]').fill('layout-reader');
        await testId('tenants-user-lookup-submit').click();
        await testId('tenants-user-lookup-results').waitFor();
        await page.waitForLoadState('networkidle');
        const styles = await page.locator('.user-lookup-page__sections').evaluate(element => ({ gap: getComputedStyle(element).gap }));
        check(styles.gap === '8px', 'Lookup Fluent accordion scoped gap');
        const sort = await page.locator('.user-lookup-page__sort').evaluate(element => ({ maxWidth: getComputedStyle(element).maxWidth }));
        check(sort.maxWidth === '288px', 'Lookup Fluent sort scoped width');
        if (width === 1366) await scopeControl('.user-lookup-page__sort', '.user-lookup-page-style-scope', 'maxWidth', '288px');
        layouts.push({ name: 'lookup-scoped-styles', viewport: width, ...styles, ...sort });
    }
    evidence.layouts = layouts;
    check(fixture.commands === 0, 'No domain command may be sent for evidence');
    check(circuitErrors.length === 0, 'No Blazor circuit failures');
    check(JSON.stringify(sourceBefore) === JSON.stringify(await captureSource()), 'Source changed during browser acceptance');
    check(validatorBefore === hash(await readFile(fileURLToPath(import.meta.url))), 'Validator changed during browser acceptance');
    evidence.passed = true;
} catch (error) {
    evidence.passed = false;
    evidence.failure = { phase, label: error.safeLabel || 'Browser operation failed', focus: page ? await observe().catch(() => null) : null };
    process.exitCode = 1;
} finally {
    evidence.fixture = { authorizedReads: fixture.authorizedReads, commandAttempts: fixture.commands,
        listReads: fixture.listReads, searchReads: fixture.searchReads, auditReads: fixture.auditReads, circuitErrors: circuitErrors.length };
    evidence.acceptedForClosure = evidence.passed === true && evidence.build?.succeeded === true && !evidence.build?.reusedForDebug;
    evidence.finishedUtc = new Date().toISOString();
    try {
        await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
    } catch {
        process.exitCode = 1;
        console.error('Authenticated audit layout evidence could not be stored');
    } finally {
        // Evidence storage can fail independently of the resources that still need shutdown.
        await Promise.allSettled([context?.close(), browser?.close(), (async () => {
            releaseList?.();
            if (ui && !uiExited) {
                ui.kill('SIGTERM');
                const watchdog = setTimeout(() => { if (!uiExited) ui.kill('SIGKILL'); }, 5000);
                try { await uiExit; } finally { clearTimeout(watchdog); }
            }
            await uiLog?.close();
        })(), (async () => {
            for (const socket of sockets?.clients || []) socket.terminate();
            sockets?.close();
            upstream?.closeAllConnections();
            if (upstream) await new Promise(resolve => upstream.close(resolve));
        })()]);
    }
}
