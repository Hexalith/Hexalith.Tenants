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
    fixtureScope: 'Qualifies UI focus and composed browser live semantics with deterministic authorized projection responses; does not qualify backend authorization, real domain data or assistive-technology speech',
    records: ['DW-119', 'DW-211', 'DW-212', 'DW-213', 'DW-444', 'DW-116'],
    cases: [],
};
let tenantId = 'tenant.navigation';
const targetId = 'navigation-target';
const eventReference = 'navigation-role-event';
const memoriesFixtureToken = randomUUID();
const summary = id => ({ tenantId: id, name: `needle ${id}`, status: 'Active' });
const member = { userId: targetId, role: 'TenantContributor' };
const audit = { eventId: eventReference, eventType: 'UserRoleChanged', category: 'Access', actorId: 'navigation-actor',
    timestamp: '2026-10-23T12:00:00Z', tenantId, narrativePayload: { userId: targetId, oldRole: 'TenantReader', newRole: 'TenantContributor' } };
const fixture = { holdList: false, terminalList: true, listReads: 0, heldListReads: 0, auditReads: 0,
    resetAudit: false, resetList: false, missingAuditLauncher: false, missingPager: false, searchFallback: false, searchReads: 0, commands: 0, authorizedReads: 0, subscriptions: new Set() };
let releaseList;
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
const recordNoticeSemantics = async (name, expectedNoticeIds) => {
    const selector = '[data-testid="tenants-list-notices"]';
    const composed = await page.locator(selector).evaluate(root => {
        // Retain only structural semantics. No text, accessible names, HTTP bodies or identities.
        const nodes = [];
        const visit = (element, shadowDepth) => {
            const internals = element.elementInternals;
            const role = element.getAttribute('role');
            const live = element.getAttribute('aria-live');
            if (element === root || element.matches('[data-testid$="-notice"]') || role || live || internals?.role) {
                nodes.push({ tag: element.localName, testId: element.getAttribute('data-testid'), shadowDepth,
                    role, live, atomic: element.getAttribute('aria-atomic'),
                    internals: internals ? { role: internals.role, live: internals.ariaLive, atomic: internals.ariaAtomic } : null,
                    upgraded: element.localName.includes('-') ? Boolean(customElements.get(element.localName)) : null,
                    hasShadowRoot: Boolean(element.shadowRoot) });
            }
            for (const child of element.children) visit(child, shadowDepth);
            for (const child of element.shadowRoot?.children || []) visit(child, shadowDepth + 1);
        };
        visit(root, 0);
        return { nodes, noticeIds: [...root.querySelectorAll('[data-testid$="-notice"]')].map(node => node.getAttribute('data-testid')) };
    });
    // Chromium exposes ElementInternals' implicit status/live mapping through AX, even when
    // the host has no literal role/aria-live attribute. Keep only the scoped computed roles.
    const session = await context.newCDPSession(page);
    let accessibility;
    try {
        const { root } = await session.send('DOM.getDocument');
        const { nodeId } = await session.send('DOM.querySelector', { nodeId: root.nodeId, selector });
        const { node } = await session.send('DOM.describeNode', { nodeId });
        const { nodes } = await session.send('Accessibility.getFullAXTree');
        const outer = nodes.find(item => item.backendDOMNodeId === node.backendNodeId);
        check(Boolean(outer), 'Outer notice region must be present in the browser accessibility tree');
        const byId = new Map(nodes.map(item => [item.nodeId, item]));
        accessibility = [];
        const visit = (item, depth) => {
            const properties = Object.fromEntries((item.properties || [])
                .filter(property => ['live', 'atomic', 'relevant'].includes(property.name))
                .map(property => [property.name, property.value.value]));
            if (depth === 0 || ['status', 'alert'].includes(item.role?.value)) {
                accessibility.push({ depth, role: item.role?.value, ignored: item.ignored, ...properties });
            }
            for (const id of item.childIds || []) if (byId.has(id)) visit(byId.get(id), depth + 1);
        };
        visit(outer, 0);
    } finally { await session.detach(); }
    const semantics = { composed, accessibility, speech: 'Not observed; requires the manual AT handoff' };
    // Save observations before assertions so rejected qualifications retain their actual structure.
    evidence.liveSemantics ??= [];
    evidence.liveSemantics.push({ name, ...semantics });
    check(JSON.stringify([...composed.noticeIds].sort()) === JSON.stringify([...expectedNoticeIds].sort()), 'Expected search notices must be contained in the outer region');
    const outer = composed.nodes[0];
    check(outer.role === 'status' && outer.live === 'polite' && outer.atomic === 'true', 'Outer notice region must retain status/polite/atomic attributes');
    check(accessibility[0].role === 'status' && accessibility[0].live === 'polite' && accessibility[0].atomic === true,
        'Outer notice region must expose status/polite/atomic computed semantics');
    const bars = composed.nodes.filter(node => expectedNoticeIds.includes(node.testId));
    check(bars.every(node => node.tag === 'fluent-message-bar' && node.upgraded && node.hasShadowRoot && node.internals?.role === 'status'),
        'Every notice must expose the shipped Fluent message-bar accessibility internals');
    check(accessibility.filter(node => node.role === 'status' && node.live === 'polite').length === expectedNoticeIds.length + 1,
        'Browser must expose one outer status plus each Fluent notice status; this does not count speech');
    await record(name, { noticeCount: expectedNoticeIds.length });
};
const notify = () => {
    for (const socket of sockets.clients) if (socket.readyState === 1) {
        socket.send(JSON.stringify({ type: 1, target: 'ProjectionChanged', arguments: ['tenants', tenantId] }) + '\x1e');
    }
};
try {
    phase = 'build-current-ui';
    const privateDirectory = await mkdtemp(resolve(tmpdir(), 'tenants-navigation-browser-'));
    const buildArguments = ['build', 'src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj', '-c', 'Debug', '-m:1',
        '-p:UseNuGetDeps=false'];
    if (process.env.TENANTS_BROWSER_EVENTSTORE_VERSION) {
        check(/^\d+\.\d+\.\d+$/.test(process.env.TENANTS_BROWSER_EVENTSTORE_VERSION), 'Explicit EventStore build selection must be a stable version');
        buildArguments.push(`-p:HexalithEventStoreVersion=${process.env.TENANTS_BROWSER_EVENTSTORE_VERSION}`);
    }
    evidence.build = { executable: 'dotnet', arguments: buildArguments, succeeded: false };
    try {
        const buildOutput = execFileSync('dotnet', buildArguments, { cwd: root, encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
        await writeFile(resolve(privateDirectory, 'build.log'), buildOutput, { mode: 0o600 });
        evidence.build.succeeded = true;
    } catch {
        throw Object.assign(new Error(), { safeLabel: 'Current Debug source build failed' });
    }
    const uiDependencies = JSON.parse(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.Tenants.UI.deps.json')));
    Object.assign(evidence, {
    sourceBaseRevision: git('rev-parse', 'HEAD'),
    sourceDiffSha256: hash(git('diff', '--binary', 'HEAD', '--', 'src/Hexalith.Tenants.UI')),
    frontComposerBaseRevision: git('-C', 'references/Hexalith.FrontComposer', 'rev-parse', 'HEAD'),
    frontComposerDiffSha256: hash(git('-C', 'references/Hexalith.FrontComposer', 'diff', '--binary', 'HEAD', '--', 'src/Hexalith.FrontComposer.Shell')),
    validatorSha256: hash(await readFile(fileURLToPath(import.meta.url))),
    localUiAssemblySha256: hash(await readFile(assembly)),
    localShellAssemblySha256: hash(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'))),
    fluentPackageVersion: Object.keys(uiDependencies.libraries)
        .find(name => name.startsWith('Microsoft.FluentUI.AspNetCore.Components/'))?.split('/')[1],
    fluentAssemblySha256: hash(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Microsoft.FluentUI.AspNetCore.Components.dll'))),
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
            if (cursor && fixture.resetList) { fixture.resetList = false; send({ reason: 'invalid-cursor', status: 400 }, 400); return; }
            if (cursor && fixture.missingPager) { send({}, 503); return; }
            send({ items: [summary(cursor ? 'tenant.secondary' : tenantId)],
                cursor: cursor ? (fixture.terminalList ? null : 'list-page-3') : 'list-page-2',
                hasMore: cursor ? !fixture.terminalList : true });
        } else if (url.pathname === `/api/tenants/${tenantId}/audit`) {
            fixture.auditReads++;
            const cursor = url.searchParams.get('cursor');
            if (cursor && fixture.resetAudit) {
                fixture.resetAudit = false;
                send({ reason: 'invalid-cursor', status: 400 }, 400);
            } else {
                send({ items: fixture.missingAuditLauncher ? [] : [{ ...audit, tenantId }],
                    cursor: !cursor && !fixture.missingAuditLauncher ? 'audit-page-2' : null, hasMore: !cursor && !fixture.missingAuditLauncher });
            }
        } else if (url.pathname === `/api/tenants/${tenantId}` || /^\/api\/tenants\/tenant\.search\.\d+$/.test(url.pathname)) {
            send({ ...summary(url.pathname.split('/').at(-1)), description: 'Navigation read fixture', members: [member], configuration: {}, createdAt: '2026-10-23T10:00:00Z' });
        } else if (url.pathname === '/api/global-administrators') {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/^\/api\/users\/[^/]+\/tenants$/.test(url.pathname)) {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/\/users$/.test(url.pathname)) {
            send({ items: [member], cursor: null, hasMore: false });
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
        DAPR_HTTP_ENDPOINT: upstreamBase,
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
    const fluentModule = await context.request.get(`${fixtureBase}/_content/Microsoft.FluentUI.AspNetCore.Components/Microsoft.FluentUI.AspNetCore.Components.lib.module.js`);
    check(fluentModule.status() === 200, 'Shipped Fluent runtime module must load');
    evidence.fluentModuleSha256 = hash(await fluentModule.body());
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
    phase = 'held-paging-disabled-endpoint';
    await workspace();
    await recordNoticeSemantics('Empty outer notice region before search notices', []);
    fixture.holdList = true;
    fixture.heldListReads = 0;
    await testId('tenants-list-next').focus();
    await page.keyboard.press('Enter');
    await wait(() => fixture.heldListReads === 1, 'Next must begin one held read');
    await waitFocus('#tenants-list-heading');
    check(fixture.heldListReads === 1, 'The held paging observation must contain one read');
    await record('Focused pager recovers to the list heading while the read is held', { heldReads: fixture.heldListReads });
    fixture.holdList = false;
    releaseList();
    await wait(() => testId('tenants-list-next').evaluate(element => element.hasAttribute('disabled')), 'Terminal Next must be disabled');
    await waitFocus('#tenants-list-heading');
    await record('Disabled paging origin leaves actual focus on the list heading');

    phase = 'held-paging-missing-origin';
    await workspace();
    fixture.holdList = true;
    fixture.heldListReads = 0;
    await testId('tenants-list-next').focus();
    await page.keyboard.press('Enter');
    await wait(() => fixture.heldListReads === 1, 'Missing-origin paging must be held');
    await waitFocus('#tenants-list-heading');
    fixture.missingPager = true;
    fixture.holdList = false;
    releaseList();
    await testId('tenants-list-error').waitFor();
    check(await testId('tenants-list-next').count() === 0, 'Failed hard read must remove the paging origin');
    await waitFocus('#tenants-list-heading');
    await record('A missing paging origin leaves actual focus on the list heading');
    fixture.missingPager = false;

    phase = 'held-paging-enabled-origin';
    fixture.terminalList = false;
    await workspace();
    fixture.holdList = true;
    fixture.heldListReads = 0;
    await testId('tenants-list-next').focus();
    await page.keyboard.press('Enter');
    await wait(() => fixture.heldListReads === 1, 'Enabled-origin paging must be held');
    await waitFocus('#tenants-list-heading');
    fixture.holdList = false;
    releaseList();
    await waitFocus('[data-testid="tenants-list-next"]');
    await record('An available paging origin regains actual focus after rendering');

    phase = 'held-paging-surviving-focus';
    await workspace();
    fixture.holdList = true;
    fixture.heldListReads = 0;
    await testId('tenants-list-next').focus();
    await page.keyboard.press('Enter');
    await wait(() => fixture.heldListReads === 1, 'Surviving-focus paging must be held');
    await waitFocus('#tenants-list-heading');
    await testId('tenants-list-refresh').focus();
    fixture.holdList = false;
    releaseList();
    await testId('tenants-list-next').waitFor();
    await waitFocus('[data-testid="tenants-list-refresh"]');
    await record('Paging completion preserves a surviving control that took focus');

    for (const mode of ['ordinary', 'protected-search', 'search-fallback']) {
        phase = `held-paging-${mode}`;
        fixture.searchFallback = mode === 'search-fallback';
        fixture.terminalList = false;
        const priorSearchReads = fixture.searchReads;
        await workspace(mode !== 'ordinary');
        const semantics = await testId('tenants-list-page-local-semantics').innerText();
        check(mode === 'protected-search' ? semantics.includes('indexed tenant candidates')
            : semantics.includes('current authorized page only'), `${mode} must use the expected paging authority`);
        if (mode !== 'ordinary') check(fixture.searchReads > priorSearchReads, 'Search scenarios must call the real Memories client fixture');
        for (const direction of ['next', 'previous']) {
            fixture.holdList = true;
            fixture.heldListReads = 0;
            await testId(`tenants-list-${direction}`).focus();
            await page.keyboard.press('Enter');
            await wait(() => fixture.heldListReads === 1, `${mode} ${direction} must hold its page read`);
            await waitFocus('#tenants-list-heading');
            await record(`${mode} ${direction} holds focus at the heading while loading`);
            fixture.holdList = false;
            releaseList();
            if (direction === 'previous') {
                await wait(() => testId('tenants-list-previous').evaluate(element => element.hasAttribute('disabled')), `${mode} Previous must be disabled on page one`);
                await waitFocus('#tenants-list-heading');
            } else {
                await waitFocus('[data-testid="tenants-list-next"]');
            }
            await record(`${mode} ${direction} restores an enabled origin or keeps the first-page heading`);
        }
    }
    fixture.searchFallback = false;

    phase = 'search-composed-live-semantics';
    await workspace(true);
    await recordNoticeSemantics('Authoritative search before a notice', []);
    await testId('tenants-list-refresh').click();
    await testId('tenants-list-search-refreshed-notice').waitFor();
    await recordNoticeSemantics('Authoritative search refresh with one Fluent notice', ['tenants-list-search-refreshed-notice']);
    fixture.searchFallback = true;
    await workspace(true);
    await recordNoticeSemantics('Unavailable search fallback with one Fluent notice', ['tenants-list-search-unavailable-notice']);
    fixture.resetList = true;
    await testId('tenants-list-next').click();
    await testId('tenants-list-refreshed-notice').waitFor();
    await recordNoticeSemantics('Search fallback and cursor recovery with two Fluent notices',
        ['tenants-list-search-unavailable-notice', 'tenants-list-refreshed-notice']);
    fixture.searchFallback = false;

    phase = 'missing-row-detail-return';
    await page.goto(`${fixtureBase}/tenants/tenants?scope=mine&selected=tenant.missing&anchor=tenants-my-detail-id-dGVuYW50Lm1pc3Npbmc`);
    await testId('tenants-audit-return-notice').waitFor();
    await waitFocus('#tenants-list-heading');
    check((await testId('tenants-audit-return-notice').innerText()).length > 0, 'Missing-row return notice must remain');
    await record('Missing-row detail return focuses the actual list heading and retains the notice');

    const openAuditPanel = async (origin = 'grid', preview = false) => {
        fixture.resetAudit = false;
        fixture.missingAuditLauncher = false;
        await page.goto(`${fixtureBase}/tenants/${tenantId}/audit`);
        await testId('tenants-audit-next').waitFor();
        await page.waitForLoadState('networkidle');
        await testId('tenants-audit-next').click();
        await wait(() => testId('tenants-audit-previous').evaluate(element => !element.hasAttribute('disabled')), 'Audit second page must be active');
        await wait(() => fixture.subscriptions.has(`tenants:${tenantId}`), 'Production SignalR subscription must join');
        if (origin === 'receipt') await testId('tenants-audit-receipt-open').click();
        const surface = origin === 'receipt' ? 'tenants-audit-receipt' : 'tenants-audit-grid';
        const role = page.locator(`[data-testid="${surface}"] [data-testid="tenants-correction-role"]`);
        await role.click();
        await page.keyboard.press('Home');
        await page.keyboard.press('ArrowDown');
        await page.keyboard.press('Enter');
        await wait(() => page.locator(`[data-testid="${surface}"] [data-testid="tenants-correction-start"]`).count().then(Boolean), 'A current role intent must be available');
        await page.locator(`[data-testid="${surface}"] [data-testid="tenants-correction-start"]`).click();
        await testId('tenants-correction-start-panel').waitFor();
        if (preview) {
            await testId('tenants-correction-start-handoff').click();
            await testId('tenants-correction-panel').waitFor();
            await testId('tenants-correction-cancel').focus();
            await waitFocus('[data-testid="tenants-correction-cancel"]');
        } else {
            await testId('tenants-correction-start-handoff').focus();
            await waitFocus('[data-testid="tenants-correction-start-handoff"]');
        }
    };
    for (const scenario of ['origin', 'missing-launcher', 'surviving-control', 'preview', 'receipt', 'receipt-preview']) {
        phase = `audit-reset-${scenario}`;
        const origin = scenario.startsWith('receipt') ? 'receipt' : 'grid';
        await openAuditPanel(origin, scenario.includes('preview'));
        if (scenario === 'surviving-control') await testId('tenants-audit-refresh').focus();
        fixture.resetAudit = true;
        fixture.missingAuditLauncher = scenario === 'missing-launcher';
        const priorReads = fixture.auditReads;
        notify();
        await wait(() => fixture.auditReads >= priorReads + 2, 'Notification must recover the invalid cursor with a first-page read');
        await testId('tenants-audit-list-refreshed').waitFor();
        await testId('tenants-correction-start-panel').waitFor({ state: 'detached' });
        await testId('tenants-correction-panel').waitFor({ state: 'detached' });
        const selector = scenario === 'missing-launcher' ? '#tenant-audit-heading'
            : scenario === 'surviving-control' ? '[data-testid="tenants-audit-refresh"]'
                : `[data-testid="tenants-correction-unavailable-reason"][data-correction-origin="${origin}"][data-correction-focus-reference="navigation-role-event"]`;
        await waitFocus(selector);
        if (!['missing-launcher', 'surviving-control'].includes(scenario)) {
            check(await page.locator(selector).evaluate(element => element.getAttribute('tabindex') === '-1'
                && element.getClientRects().length > 0 && element.textContent.trim().length > 0),
                'The matching origin reason must be focusable, visible and named');
        }
        await record(`Audit reset focus: ${scenario}`,  { resetReadCount: fixture.auditReads - priorReads });
    }
    check(fixture.commands === 0, 'No domain command may be sent for evidence');
    check(circuitErrors.length === 0, 'No Blazor circuit failures');
    evidence.passed = true;
} catch (error) {
    evidence.passed = false;
    evidence.failure = { phase, label: error.safeLabel || 'Browser operation failed', focus: page ? await observe().catch(() => null) : null };
    process.exitCode = 1;
} finally {
    evidence.fixture = { authorizedReads: fixture.authorizedReads, commandAttempts: fixture.commands,
        listReads: fixture.listReads, searchReads: fixture.searchReads, auditReads: fixture.auditReads, circuitErrors: circuitErrors.length };
    evidence.finishedUtc = new Date().toISOString();
    try {
        await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
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
