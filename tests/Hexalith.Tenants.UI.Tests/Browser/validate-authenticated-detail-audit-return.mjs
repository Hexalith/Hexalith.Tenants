// DW-434: authenticate through Aspire, then exercise current production UI against
// an isolated loopback fixture. Fake command acceptance never reaches a domain host.
// Its unchanged projections and unavailable status keep actual Accepted attempts retained.
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
const evidence = {
    playwrightVersion: require(`${playwrightModule}/package.json`).version,
    startedUtc: new Date().toISOString(),
    hostMode: 'Production authentication and Razor components; separate UI process; isolated HTTP and SignalR fixture',
    fixtureScope: 'Fake202 Accepted and held-POST RequestSent with unchanged authorized projections; a synthetic display-safe billing.mode grant permits fixture removal; no forwarding, event writes or backend command qualification',
    navigationSetup: 'Capture production flow audit href during Accepted; reuse the same permitted context for RequestSent (whose audit launcher is deliberately absent); collapse the enclosing accordion; use Blazor.navigateTo and the real audit Back link',
    records: ['DW-434'],
    cases: [],
};
let tenantId = 'tenant.audit-return';
let callerId = 'audit-return-operator';
const memoriesFixtureToken = randomUUID();
const member = () => ({ userId: callerId, role: 'TenantOwner' });
const summary = id => ({ tenantId: id, name: `Audit return ${id}`, status: 'Active' });
const fixture = { commands: [], acceptedResponses: 0, statusReads: [], authorizedReads: 0, auditReads: 0, unexpectedWrites: 0, holdCommands: false, heldResponses: new Set(), routes: [], subscriptions: new Set() };
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
try {
    phase = 'build-current-ui';
    const privateDirectory = await mkdtemp(resolve(tmpdir(), 'tenants-detail-audit-return-'));
    // Capture the source host environment before compiling: an Aspire watch restart can replace
    // that process while the same production UI assembly is rebuilt. Values stay private in memory.
    const originalEnvironment = Object.fromEntries((await readFile(`/proc/${process.env.TENANTS_BROWSER_SOURCE_UI_PID}/environ`, 'utf8'))
        .split('\0').filter(Boolean).map(entry => { const index = entry.indexOf('='); return [entry.slice(0, index), entry.slice(index + 1)]; }));
    const sourceBeforeBuild = await captureSource();
    const validatorBeforeBuild = hash(await readFile(fileURLToPath(import.meta.url)));
    const buildArguments = ['build', 'src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj', '-c', 'Debug', '-m:1',
        '-p:UseNuGetDeps=false'];
    const skipBuildForDevelopment = process.env.TENANTS_BROWSER_SKIP_BUILD === '1';
    evidence.build = { executable: 'dotnet', arguments: buildArguments, succeeded: false,
        reusedBuild: skipBuildForDevelopment, skippedForDevelopment: skipBuildForDevelopment };
    if (!skipBuildForDevelopment) {
        try {
            const buildOutput = execFileSync('dotnet', buildArguments, { cwd: root, encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
            await writeFile(resolve(privateDirectory, 'build.log'), buildOutput, { mode: 0o600 });
            evidence.build.succeeded = true;
        } catch {
            throw Object.assign(new Error(), { safeLabel: 'Current Debug source build failed' });
        }
    }
    const sourceAfterBuild = await captureSource();
    check(JSON.stringify(sourceBeforeBuild) === JSON.stringify(sourceAfterBuild), 'Source changed during the browser validation build');
    Object.assign(evidence, sourceAfterBuild, {
        sourceBeforeBuild, sourceAfterBuild, validatorSha256: validatorBeforeBuild,
        localUiAssemblySha256: hash(await readFile(assembly)),
        localShellAssemblySha256: hash(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'))),
    });
    phase = 'fixture-start';
    upstream = createServer(async (request, response) => {
        const url = new URL(request.url, 'http://localhost');
        fixture.routes.push(`${request.method} ${url.pathname.replace(/\/api\/v1\/commands\/status\/.+$/, '/api/v1/commands/status/identity')}`);
        const send = (body, status = 200) => {
            response.writeHead(status, { 'Content-Type': 'application/json', ETag: '"navigation-fixture"',
                'X-Hexalith-Query-Provenance': 'ProjectionBacked', 'X-Hexalith-Projection-Version': 'tenant-sequence:41',
                'X-Hexalith-Projection-Lifecycle': 'Current', 'X-Hexalith-Is-Stale': 'false', 'X-Hexalith-Is-Degraded': 'false' });
            response.end(JSON.stringify(body));
        };
        if (url.pathname === '/hubs/projection-changes/negotiate' && request.method === 'POST') {
            const id = randomUUID();
            send({ negotiateVersion: 1, connectionId: id, connectionToken: id,
                availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] });
            return;
        }
        if (!request.headers.authorization?.startsWith('Bearer ')) { send({}, 401); return; }
        fixture.authorizedReads++;
        // Tokens and command payload values stay in memory and never enter evidence/logs.
        const claims = JSON.parse(Buffer.from(request.headers.authorization.slice(7).split('.')[1], 'base64url'));
        const currentTenant = claims['eventstore:current-tenant'];
        if (typeof currentTenant === 'string' && /^[a-zA-Z0-9._-]+$/.test(currentTenant)) tenantId = currentTenant;
        if (typeof claims.sub === 'string') callerId = claims.sub;
        if (request.method === 'POST' && url.pathname === '/api/v1/commands') {
            let raw = '';
            for await (const chunk of request) raw += chunk;
            const command = JSON.parse(raw);
            if (!['DisableTenant', 'SetTenantConfiguration', 'RemoveTenantConfiguration'].includes(command.commandType)
                || command.domain !== 'tenants' || command.tenant !== 'system'
                || command.aggregateId !== tenantId || typeof command.messageId !== 'string') {
                fixture.unexpectedWrites++;
                send({}, 400);
                return;
            }
            const correlationId = command.messageId;
            const registered = { messageId: command.messageId, correlationId, commandType: command.commandType,
                aggregateId: command.aggregateId, tenant: command.tenant, domain: command.domain,
                payloadSha256: hash(JSON.stringify(command.payload)) };
            const previous = fixture.commands.find(entry => entry.messageId === command.messageId);
            if (previous && (!fixture.holdCommands || command.commandType !== 'DisableTenant'
                || JSON.stringify(previous) !== JSON.stringify(registered))) {
                fixture.unexpectedWrites++;
                send({ reason: 'unexpected-retry-identity' }, 400);
                return;
            }
            fixture.commands.push(registered);
            // Local transport simulation only: deliberately no mutation of projection or domain state.
            if (fixture.holdCommands) {
                await new Promise(resolve => { fixture.heldResponses.add(resolve); });
            }
            if (!response.destroyed) {
                if (previous) send({ reason: 'isolated-retry-unavailable' }, 503);
                else { fixture.acceptedResponses++; send({ correlationId }, 202); }
            }
        } else if (request.method !== 'GET') {
            fixture.unexpectedWrites++;
            send({ reason: 'fixture-route-prohibited' }, 405);
        } else if (url.pathname.startsWith('/api/v1/commands/status/')) {
            const messageId = url.pathname.split('/').at(-1);
            fixture.statusReads.push(messageId);
            // A transient status failure preserves Accepted ownership while making audit Inspect available.
            // Received/Processing themselves correctly carry no audit record and hide that control.
            send({ status: 503, reason: 'isolated-status-unavailable' }, 503);
        } else if (url.pathname === '/api/tenants') {
            send({ items: [summary(tenantId)], cursor: null, hasMore: false });
        } else if (url.pathname === `/api/tenants/${tenantId}/audit`) {
            fixture.auditReads++;
            send({ items: [], cursor: null, hasMore: false });
        } else if (url.pathname === `/api/tenants/${tenantId}`) {
            send({ ...summary(tenantId), description: 'Unchanged audit-return fixture projection', members: [member()],
                configuration: { 'billing.mode': 'fixture-old-value' }, createdAt: '2026-10-23T10:00:00Z' });
        } else if (url.pathname === '/api/global-administrators') {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/^\/api\/users\/[^/]+\/tenants$/.test(url.pathname)) {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/\/users$/.test(url.pathname)) {
            send({ items: [member()], cursor: null, hasMore: false });
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
    const environment = { ...originalEnvironment, ...process.env, ASPNETCORE_URLS: fixtureBase, ASPNETCORE_HTTPS_PORT: String(uiPort),
        Tenants__BaseAddress: upstreamBase, EventStore__BaseAddress: upstreamBase, Memories__BaseAddress: upstreamBase,
        Tenants__ConfigurationReadPolicy__DisplaySafe__0: 'billing.mode',
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
    // The registered callback completed the fixture's real code exchange/token relay. Enter the fixture
    // immediately; the original watch-managed host may be restarting after the preceding source build.
    phase = 'hydrate-authenticated-fixture-scope';
    await page.goto(`${fixtureBase}/tenants/tenants`);
    await testId('tenants-list-refresh').waitFor();
    await wait(() => fixture.authorizedReads > 0, 'Fixture must resolve the authenticated tenant scope');
    await page.waitForLoadState('networkidle');
    const selectors = {
        lifecycle: { root: 'tenants-lifecycle-command-flow', state: 'tenants-lifecycle-state', lifecycle: 'tenants-lifecycle-lifecycle',
            audit: 'tenants-lifecycle-audit', refresh: 'tenants-lifecycle-refresh', abandon: 'tenants-lifecycle-abandon', cancel: 'tenants-lifecycle-cancel' },
        set: { root: 'tenants-config-set-flow', state: 'tenants-config-set-state', lifecycle: 'tenants-config-set-lifecycle',
            audit: 'tenants-config-set-audit', refresh: 'tenants-config-set-refresh', abandon: 'tenants-config-set-abandon', cancel: 'tenants-config-set-cancel' },
        remove: { root: 'tenants-config-remove-flow', state: 'tenants-config-remove-state', lifecycle: 'tenants-config-remove-lifecycle',
            audit: 'tenants-config-remove-audit', refresh: 'tenants-config-remove-refresh', abandon: 'tenants-config-remove-abandon', cancel: 'tenants-config-remove-cancel' },
    };
    const fill = async (id, value) => {
        const host = testId(id);
        const input = host.locator('input,textarea');
        await input.waitFor();
        await input.fill(value);
        await page.keyboard.press('Tab');
    };
    const enabled = async id => wait(() => testId(id).evaluate(element => !element.hasAttribute('disabled')
        && element.getAttribute('aria-disabled') !== 'true'), `Expected enabled ${id}`);
    const settledDetail = async () => {
        await wait(() => testId('tenants-detail-audit-origin').getAttribute('data-audit-focus-terminal')
            .then(value => value !== null && value.toLowerCase() !== 'false'), 'Detail audit authority must settle');
        let observedReads = fixture.authorizedReads;
        let settledSince = Date.now();
        await wait(() => {
            if (fixture.authorizedReads !== observedReads) {
                observedReads = fixture.authorizedReads;
                settledSince = Date.now();
            }
            return Date.now() - settledSince >= 750;
        }, 'Initial detail and authorization reads must settle before interaction');
    };
    const enclosingItem = id => testId(id).locator('xpath=ancestor::fluent-accordion-item[1]');
    const collapse = async item => {
        const button = item.locator('button[aria-expanded]').first();
        await button.click();
        await wait(() => button.getAttribute('aria-expanded').then(value => value === 'false'), 'Accordion must really collapse');
    };
    // Obtain the actual token-derived tenant before entering detail. Full navigation starts one fresh circuit;
    // each audit round trip below then stays within that circuit and its scoped attempt tracker.
    check(fixture.authorizedReads > 0, 'Authentication must reach protected fixture reads');
    const auditHrefs = new Map();
    for (const [kind, delivery] of ['set', 'remove', 'lifecycle'].flatMap(kind => [[kind, 'Accepted'], [kind, 'RequestSent']])) {
        fixture.holdCommands = delivery === 'RequestSent';
        const commandsAtStart = fixture.commands.length;
        phase = `${kind}-${delivery}-initial-detail`;
        await page.goto(`${fixtureBase}/tenants/${tenantId}`);
        await testId('tenants-detail-identity').waitFor();
        await page.waitForLoadState('networkidle');
        await settledDetail();
        const s = selectors[kind];
        if (kind === 'lifecycle') {
            await enabled('tenants-lifecycle-disable');
            await testId('tenants-lifecycle-disable').click();
            await fill('tenants-lifecycle-confirmation', tenantId);
            await enabled('tenants-lifecycle-confirm');
            await testId('tenants-lifecycle-confirm').click();
        } else if (kind === 'set') {
            await enabled('tenants-config-set-open');
            await testId('tenants-config-set-open').click();
            await fill('tenants-config-set-namespace', 'billing');
            await fill('tenants-config-set-key-suffix', 'mode');
            await fill('tenants-config-set-value', 'fixture-replacement-value');
            await enabled('tenants-config-set-submit');
            await testId('tenants-config-set-submit').click();
        } else {
            await enabled('tenants-config-management-remove-open');
            await testId('tenants-config-management-remove-open').click();
            await fill('tenants-config-remove-confirmation', 'billing.mode');
            await enabled('tenants-config-remove-submit');
            await testId('tenants-config-remove-submit').click();
        }
        phase = `${kind}-${delivery}-retained-attempt`;
        await wait(() => fixture.commands.length === commandsAtStart + 1, 'Exactly one isolated command must be accepted');
        const command = fixture.commands.at(-1);
        const expectedCommand = { lifecycle: 'DisableTenant', set: 'SetTenantConfiguration', remove: 'RemoveTenantConfiguration' }[kind];
        check(command.commandType === expectedCommand, 'The expected command type must reach only the local fixture');
        const state = testId(s.root).getByTestId(s.state);
        const expectedState = delivery === 'Accepted' ? /accepted/i : /request sent/i;
        if (delivery === 'Accepted') {
            await wait(() => state.innerText().then(text => expectedState.test(text)), 'Accepted response must render Accepted truth');
        } else {
            // The original submit callback is still suspended; its last browser render can be Previewed.
            // The received held POST proves RequestSent registration occurred before transport awaited.
            check(fixture.heldResponses.size === 1, 'RequestSent requires one still-held isolated POST response');
        }
        const preNavigationStateText = (await state.innerText()).trim();
        if (kind === 'lifecycle' && delivery === 'Accepted') {
            await enabled(s.refresh);
            await testId(s.refresh).click();
        }
        const auditLink = testId(s.audit).locator('[data-testid="tenants-audit-entrypoint"][href]');
        let href;
        if (delivery === 'Accepted') {
            await auditLink.waitFor();
            href = await auditLink.getAttribute('href');
            auditHrefs.set(kind, href);
        } else {
            // RequestSent deliberately has no audit record/control. Reuse that same flow's real
            // captured route context to qualify its retained remount and approved missing-origin fallback.
            href = auditHrefs.get(kind);
            check(Boolean(href), 'A production audit href must already have been captured for this flow');
        }
        check(new URL(href, fixtureBase).searchParams.get('returnFocus') === s.lifecycle, 'Production audit link must identify its actual flow');
        const item = enclosingItem(s.root);
        await collapse(item);
        check(!await auditLink.isVisible(), 'The retained flow audit launcher must really be hidden before navigation');
        const marker = randomUUID();
        await page.evaluate(marker => { window.__dw434Document = marker; }, marker);
        const statusBefore = fixture.statusReads.filter(id => id === command.messageId).length;
        const commandsBefore = fixture.commands.length;
        phase = `${kind}-${delivery}-enhanced-audit-navigation`;
        await page.evaluate(href => window.Blazor.navigateTo(href), href);
        await testId('tenants-audit-back').waitFor();
        check(await page.evaluate(() => window.__dw434Document) === marker, 'Audit navigation must preserve the live document/circuit');
        const backHref = await testId('tenants-audit-back').getAttribute('href');
        check(new URL(backHref, fixtureBase).searchParams.get('auditFocus') === s.lifecycle, 'Audit Back must carry the original flow focus');
        await testId('tenants-audit-back').click();
        phase = `${kind}-${delivery}-readopted-return`;
        await testId(s.root).waitFor();
        if (delivery === 'Accepted') await wait(() => fixture.statusReads.filter(id => id === command.messageId).length > statusBefore, 'Readopted flow must query its original message identity');
        await wait(() => state.innerText().then(text => expectedState.test(text)), `Re-adoption must retain ${delivery} truth`);
        const expectedRetries = delivery === 'RequestSent' && kind === 'lifecycle' ? 1 : 0;
        await wait(() => fixture.commands.length === commandsBefore + expectedRetries,
            'Audit return must preserve the existing flow redelivery contract');
        const retries = fixture.commands.slice(commandsBefore);
        check(retries.every(retry => JSON.stringify(retry) === JSON.stringify(command)),
            'Every permitted redelivery must preserve exact message, tenant, aggregate, domain and payload');
        check(await page.evaluate(() => window.__dw434Document) === marker, 'Audit Back must preserve scoped trackers in the live circuit');
        if (delivery === 'Accepted') {
            const returnedLink = testId(s.audit).locator('[data-testid="tenants-audit-entrypoint"][href]');
            await returnedLink.waitFor();
            check(await returnedLink.isVisible(), 'The matching retained-flow accordion must be expanded and its launcher reachable after return');
            await waitFocus(`[data-testid="${s.audit}"] [data-testid="tenants-audit-entrypoint"]`);
            check(await testId('tenants-audit-return-notice').count() === 0, 'A reachable retained-flow audit origin must restore without a missing-origin notice');
        } else {
            await testId('tenants-audit-return-notice').waitFor();
            await waitFocus('#tenants-detail-identity');
            check(await auditLink.count() === 0, 'RequestSent must not invent an audit record or launcher');
            check(await testId(s.abandon).isVisible(), 'Retained RequestSent must remain reachable for explicit abandon');
        }
        await record(`${kind}: collapsed ${delivery} flow preserves exact logical attempt and restores governed focus`, {
            isolatedCommandType: command.commandType, retainedState: delivery,
            preNavigationStateText,
            sameMessageStatusReads: fixture.statusReads.filter(id => id === command.messageId).length,
            transportRetries: retries.length, retryIdentityAndPayloadMatch: true, newLogicalCommands: 0,
            collapsedBeforeNavigation: true, expandedOnReturn: true,
            documentPreserved: true, stateText: (await state.innerText()).trim(),
            focusOutcome: delivery === 'Accepted' ? 'matching audit launcher' : 'Option A heading plus missing-origin notice',
        });
        // Let the original response finish after observing RequestSent. Lifecycle's existing same-ID
        // redelivery gets a transient unavailable response so explicit abandon can finish normally.
        fixture.holdCommands = false;
        for (const release of fixture.heldResponses) release();
        fixture.heldResponses.clear();
        await enabled(s.abandon);
        await testId(s.abandon).click();
        await enabled(s.cancel);
        await testId(s.cancel).click();
    }
    phase = 'option-a-terminal-fallback';
    // No terminal-result persistence is added: a missing flow still uses the approved heading-plus-notice fallback.
    await page.goto(`${fixtureBase}/tenants/${tenantId}?auditFocus=tenants-config-remove-lifecycle`);
    await testId('tenants-audit-return-notice').waitFor();
    await waitFocus('#tenants-detail-identity');
    check((await testId('tenants-audit-return-notice').innerText()).trim().length > 0, 'Fallback notice must explain the unavailable origin');
    await record('Option A: absent terminal flow falls back to the real detail heading with notice');
    check(new Set(fixture.commands.map(command => command.messageId)).size === 6,
        'Exactly six logical isolated attempts must cover both states of all three flows');
    check(fixture.commands.length === 7, 'Only lifecycle RequestSent uses its existing single same-ID transport redelivery');
    check(fixture.unexpectedWrites === 0, 'No unsupported or forwarded writes');
    check(circuitErrors.length === 0, 'No Blazor circuit failure');
    const sourceBeforeAcceptance = await captureSource();
    check(JSON.stringify(evidence.sourceAfterBuild) === JSON.stringify(sourceBeforeAcceptance), 'Source changed before browser acceptance');
    check(evidence.validatorSha256 === hash(await readFile(fileURLToPath(import.meta.url))), 'Validator changed during execution');
    evidence.sourceBeforeAcceptance = sourceBeforeAcceptance;
    evidence.passed = true;
} catch (error) {
    evidence.passed = false;
    evidence.failure = { phase, label: error.safeLabel || 'Browser operation failed', focus: page ? await observe().catch(() => null) : null };
    if (phase === 'authenticate-original-aspire-host') {
        evidence.failure.diagnostic = `${error.name}: ${error.message?.split('\n')[0] || ''}`
            .replace(/https?:\/\/\S+/g, '[url]').replaceAll(username, '[user]').replaceAll(password, '[credential]');
    }
    if (/^(lifecycle|set|remove|option-a)-/.test(phase)) {
        evidence.failure.diagnostic = error.message?.slice(0, 2000);
        evidence.failure.flowText = await page.locator('[data-testid="tenants-lifecycle-command-flow"], [data-testid="tenants-config-set-flow"], [data-testid="tenants-config-remove-flow"]').allTextContents().catch(() => []);
        evidence.failure.fixtureRoutes = fixture.routes;
    }
    process.exitCode = 1;
} finally {
    evidence.fixture = { authorizedReads: fixture.authorizedReads, isolatedCommandAttempts: fixture.commands.map(command => command.commandType),
        fakeAcceptedResponses: fixture.acceptedResponses,
        logicalCommandAttempts: new Set(fixture.commands.map(command => command.messageId)).size,
        statusReads: fixture.statusReads.length, auditReads: fixture.auditReads, unexpectedWrites: fixture.unexpectedWrites,
        domainWrites: 0, commandForwarding: false, circuitErrors: circuitErrors.length };
    evidence.finishedUtc = new Date().toISOString();
    evidence.acceptedForClosure = evidence.passed === true && evidence.build?.succeeded === true && !evidence.build?.reusedBuild;
    try {
        await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
    } catch {
        process.stderr.write('Could not write browser evidence output.\n');
        process.exitCode = 1;
    } finally {
        // Evidence storage can fail independently of the resources that still need shutdown.
        await Promise.allSettled([context?.close(), browser?.close(), (async () => {
            for (const release of fixture.heldResponses) release();
            fixture.heldResponses.clear();
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
