// Authenticate through the existing Aspire host, then exercise the rebuilt production
// UI dialogs in a separate process with controlled, read-only loopback responses.
// The fixture never forwards commands: two explicitly selected command simulations
// exercise pending dismissal and conflict recovery entirely within this process.
import assert from 'node:assert/strict';
import { createHash, randomUUID } from 'node:crypto';
import { execFileSync, spawn } from 'node:child_process';
import { createServer } from 'node:http';
import { readFile, writeFile, mkdtemp, open } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { startLocalOidcFixture } from './local-oidc-fixture.mjs';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const require = createRequire(new URL('../../performance/tenant-audit/package.json', import.meta.url));
const playwrightModule = process.env.TENANTS_BROWSER_PLAYWRIGHT_MODULE || 'playwright';
const { chromium } = require(playwrightModule);
const playwrightRoot = dirname(require.resolve(`${playwrightModule}/package.json`));
const { wsServer: WebSocketServer } = require(resolve(playwrightRoot, '../playwright-core/lib/utilsBundle.js'));
const standaloneOidc = process.env.TENANTS_BROWSER_STANDALONE_OIDC === '1';
const safetyOnly = process.env.TENANTS_BROWSER_UI_SAFETY_ONLY === '1';
const base = new URL(process.env.TENANTS_BROWSER_BASE_URL || (standaloneOidc ? 'https://localhost' : ''));
assert.equal(base.hostname, 'localhost', 'Use the registered localhost Aspire endpoint so authentication cookies reach the fixture');
const username = process.env.TENANTS_BROWSER_USERNAME;
const password = process.env.TENANTS_BROWSER_PASSWORD;
if (!standaloneOidc) {
    assert.ok(username && password, 'Supply the chosen local realm credentials through environment variables');
    assert.match(process.env.TENANTS_BROWSER_SOURCE_UI_PID || '', /^\d+$/, 'Supply the current Aspire UI process id');
}
const output = process.argv[2];
assert.ok(output, 'Supply a safe result JSON path');
const git = (...args) => execFileSync('git', args, { cwd: root, encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] }).trim();
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
        validatorSha256: hash(await readFile(fileURLToPath(import.meta.url))),
        oidcFixtureSha256: hash(await readFile(resolve(dirname(fileURLToPath(import.meta.url)), "local-oidc-fixture.mjs"))),
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
    hostMode: 'Production authentication and Razor components; separate UI process; controlled read-only HTTP and SignalR upstream',
    fixtureScope: 'Qualifies authenticated production dialog/create DOM and browser behavior using deterministic upstream responses; does not qualify backend authorization, stored domain data or command delivery',
    authenticationFixture: standaloneOidc ? 'Test-only loopback OIDC issuer; production code validates signed tokens and authenticates its circuit' : 'Existing local realm',
    records: safetyOnly ? ['DW-194', 'DW-201'] : ['DW-268', 'DW-391', 'DW-194', 'DW-201'],
    diagnosticSelectedCasesOnly: safetyOnly,
    cases: [],
};
const longTarget = `dialog-remove-${'literal-identity-'.repeat(15)}tail`.slice(0, 256);
const grantTarget = `dialog-grant-${'literal-identity-'.repeat(15)}tail`.slice(0, 256);
const fixture = { authorizedReads: 0, adminReads: 0, commands: 0, simulatedCommands: 0 };
let commandScenario;
let denyExistingRead = false;
let trackedCommand;
let releasePendingDelivery;
let phase = 'startup';
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
let oidcFixture;
const circuitErrors = [];
const check = (condition, label) => { if (!condition) throw Object.assign(new Error(), { safeLabel: label }); };
const wait = async (condition, label, timeout = 20_000) => {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) { if (await condition()) return; await new Promise(resolve => setTimeout(resolve, 50)); }
    throw Object.assign(new Error(), { safeLabel: label });
};
const testId = id => page.getByTestId(id);
const selector = id => `[data-testid="${id}"]`;
const focused = value => page.evaluate(value => {
    if (!document.hasFocus()) return false;
    let active = document.activeElement;
    while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
    for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
        if (node.matches?.(value)) return true;
    }
    return false;
}, value);
const waitFocus = value => wait(async () => await focused(value) && !await focused('[data-testid$="focus-start"], [data-testid$="focus-end"]'), `Expected focus: ${value}`);
const observeFocus = () => page.evaluate(() => {
    let active = document.activeElement;
    let shadowDepth = 0;
    while (active?.shadowRoot?.activeElement) { active = active.shadowRoot.activeElement; shadowDepth++; }
    const ancestry = [];
    for (let node = active; node; node = node.parentElement || node.getRootNode()?.host) {
        if (node.getAttribute?.('data-testid')) ancestry.push(node.getAttribute('data-testid'));
    }
    return { documentHasFocus: document.hasFocus(), element: active?.localName, shadowDepth, ancestry };
});
const freePort = async () => {
    const server = createServer();
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    const port = server.address().port;
    await new Promise(resolve => server.close(resolve));
    return port;
};
const controls = kind => kind === 'grant' ? {
    dialog: 'tenants-global-admin-grant-preview', acknowledge: 'tenants-global-admin-grant-acknowledge',
    confirm: 'tenants-global-admin-grant-confirm', cancel: 'tenants-global-admin-grant-preview-cancel',
    target: 'tenants-global-admin-grant-preview-target', grid: '.global-admins__grant-preview-grid',
} : {
    dialog: 'tenants-global-admin-remove-preview', acknowledge: 'tenants-global-admin-remove-acknowledge',
    confirm: 'tenants-global-admin-remove-submit', cancel: 'tenants-global-admin-remove-cancel',
    target: 'tenants-global-admin-remove-target', grid: '.global-admins__remove-preview-grid',
};
const boundedDialog = async kind => {
    const ids = controls(kind);
    const measured = await testId(ids.dialog).evaluate((element, ids) => {
        const rect = element.getBoundingClientRect();
        const grid = element.querySelector(ids.grid);
        const facts = [...grid.querySelectorAll('dt, dd')].map(node => {
            const box = node.getBoundingClientRect();
            return { left: box.left - rect.left, right: box.right - rect.left,
                width: box.width, clientWidth: node.clientWidth, scrollWidth: node.scrollWidth,
                textOverflow: getComputedStyle(node).textOverflow, overflowWrap: getComputedStyle(node).overflowWrap,
                hasText: node.textContent.trim().length > 0 };
        });
        const target = element.querySelector(`[data-testid="${ids.target}"]`);
        return { viewport: { width: innerWidth, height: innerHeight },
            bounds: { left: rect.left, right: rect.right, top: rect.top, bottom: rect.bottom, width: rect.width, height: rect.height },
            clientWidth: element.clientWidth, scrollWidth: element.scrollWidth, clientHeight: element.clientHeight,
            scrollHeight: element.scrollHeight, overflowY: getComputedStyle(element).overflowY,
            columns: getComputedStyle(grid).gridTemplateColumns, facts,
            targetLength: target.textContent.length, targetOverflowWrap: getComputedStyle(target).overflowWrap };
    }, ids);
    check(measured.bounds.left >= -1 && measured.bounds.right <= measured.viewport.width + 1, 'Dialog must fit the measured viewport width');
    check(measured.bounds.top >= -1 && measured.bounds.bottom <= measured.viewport.height + 1, 'Dialog must fit the measured viewport height');
    check(measured.scrollWidth <= measured.clientWidth + 1, 'Dialog must not clip content horizontally');
    check(measured.facts.length === 20 && measured.facts.every(fact => fact.hasText), 'All ten preview facts must be rendered with labels and values');
    check(measured.facts.every(fact => fact.left >= -1 && fact.right <= measured.bounds.width + 1 && fact.scrollWidth <= fact.clientWidth + 1), 'Every preview fact must fit inside the dialog without horizontal clipping');
    check(measured.targetOverflowWrap === 'anywhere', 'Long literal preview identities must wrap');
    check((await testId(ids.target).textContent()).includes(kind === 'grant' ? grantTarget : longTarget), 'Preview must preserve the entire literal identity');
    if (measured.scrollHeight > measured.clientHeight + 1) check(['auto', 'scroll'].includes(measured.overflowY), 'Short-height dialog must provide vertical scrolling');
    return measured;
};
try {
    phase = 'build-current-ui';
    const sourceBeforeBuild = await captureSource();
    evidence.sourceBeforeBuild = sourceBeforeBuild;
    const privateDirectory = await mkdtemp(resolve(tmpdir(), 'tenants-admin-dialog-browser-'));
    const buildArguments = ['build', 'src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj', '-c', 'Debug', '-m:1', '-p:UseNuGetDeps=false'];
    evidence.build = { executable: 'dotnet', arguments: buildArguments, succeeded: false,
        skippedForDevelopment: process.env.TENANTS_BROWSER_SKIP_BUILD === '1',
        reusedBuild: process.env.TENANTS_BROWSER_SKIP_BUILD === '1' };
    if (!evidence.build.skippedForDevelopment) {
        try {
            const buildOutput = execFileSync('dotnet', buildArguments, { cwd: root, encoding: 'utf8', maxBuffer: 32 * 1024 * 1024 });
            await writeFile(resolve(privateDirectory, 'build.log'), buildOutput, { mode: 0o600 });
            evidence.build.succeeded = true;
        } catch (error) {
            await writeFile(resolve(privateDirectory, 'build.log'), String(error.stdout || '') + String(error.stderr || ''), { mode: 0o600 });
            throw Object.assign(new Error(), { safeLabel: 'Current Debug source build failed' });
        }
    }
    const sourceAfterBuild = await captureSource();
    evidence.sourceAfterBuild = sourceAfterBuild;
    evidence.sourceStableDuringBuild = JSON.stringify(sourceBeforeBuild) === JSON.stringify(sourceAfterBuild);
    check(evidence.sourceStableDuringBuild, 'Source or build inputs changed during the build; browser evidence is refused');
    Object.assign(evidence, {
        ...sourceAfterBuild,
        validatorSha256: hash(await readFile(fileURLToPath(import.meta.url))),
        localUiAssemblySha256: hash(await readFile(assembly)),
        localShellAssemblySha256: hash(await readFile(resolve(uiDirectory, 'bin/Debug/net10.0/Hexalith.FrontComposer.Shell.dll'))),
    });
    phase = 'fixture-start';
    upstream = createServer(async (request, response) => {
        const url = new URL(request.url, 'http://localhost');
        const send = (body, status = 200) => {
            response.writeHead(status, { 'Content-Type': 'application/json', ETag: '"administrator-dialog-fixture"',
                'X-Hexalith-Query-Provenance': 'ProjectionBacked', 'X-Hexalith-Projection-Version': 'tenant-sequence:1',
                'X-Hexalith-Projection-Lifecycle': 'Current', 'X-Hexalith-Is-Stale': 'false', 'X-Hexalith-Is-Degraded': 'false' });
            response.end(JSON.stringify(body));
        };
        if (url.pathname === '/hubs/projection-changes/negotiate' && request.method === 'POST') {
            const id = randomUUID();
            send({ negotiateVersion: 1, connectionId: id, connectionToken: id,
                availableTransports: [{ transport: 'WebSockets', transferFormats: ['Text'] }] });
            return;
        }
        if (request.method === 'POST' && url.pathname === '/api/v1/commands' && commandScenario) {
            let body = ''; for await (const chunk of request) body += chunk;
            const command = JSON.parse(body);
            if (commandScenario === 'remove-pending' && command.commandType === 'RemoveUserFromTenant') {
                fixture.simulatedCommands++;
                trackedCommand = { ...command, correlationId: 'browser-pending-removal' };
                await new Promise(resolve => { releasePendingDelivery = resolve; });
                send({ correlationId: trackedCommand.correlationId }, 202); return;
            }
            if (commandScenario === 'create-conflict' && command.commandType === 'CreateTenant') {
                fixture.simulatedCommands++;
                send({ status: 409, title: 'TenantAlreadyExists', detail: 'TenantAlreadyExists' }, 409); return;
            }
        }
        if (request.method !== 'GET') { fixture.commands++; send({ reason: 'commands-prohibited' }, 405); return; }
        if (!request.headers.authorization?.startsWith('Bearer ')) { send({}, 401); return; }
        fixture.authorizedReads++;
        // Claims are used in memory only to give production code a coherent complete population.
        // No token, caller identity or header is copied to the result packet or logs.
        let claims;
        try { claims = JSON.parse(Buffer.from(request.headers.authorization.slice(7).split('.')[1], 'base64url')); }
        catch { send({}, 401); return; }
        if (url.pathname === '/api/global-administrators') {
            fixture.adminReads++;
            send({ items: [{ userId: claims.sub }, { userId: longTarget }], cursor: null, hasMore: false });
        } else if (url.pathname === '/api/tenants/tenant.alpha') {
            if (denyExistingRead) { send({}, 403); return; }
            send({ tenantId: 'tenant.alpha', name: 'Browser fixture tenant', description: 'Production safety checks', status: 'Active',
                members: [{ userId: claims.sub, role: 'TenantOwner' }, { userId: 'browser-reader', role: 'TenantReader' },
                    { userId: 'browser-owner', role: 'TenantOwner' }], configuration: {}, createdAt: '2026-10-25T00:00:00Z' });
        } else if (url.pathname === '/api/tenants/tenant.alpha/users') {
            send({ items: [{ userId: claims.sub, role: 'TenantOwner' }, { userId: 'browser-reader', role: 'TenantReader' },
                { userId: 'browser-owner', role: 'TenantOwner' }], cursor: null, hasMore: false });
        } else if (url.pathname === '/api/tenants/tenant.alpha/audit') {
            send({ items: [], cursor: null, hasMore: false });
        } else if (url.pathname.startsWith('/api/v1/commands/status/') && trackedCommand) {
            send({ messageId: trackedCommand.messageId, correlationId: trackedCommand.correlationId,
                aggregateId: trackedCommand.aggregateId, tenantId: 'system', domain: 'tenants', status: 'Received' });
        } else if (url.pathname === '/api/tenants' || /^\/api\/users\/[^/]+\/tenants$/.test(url.pathname)) {
            send({ items: [], cursor: null, hasMore: false });
        } else { send({}, 404); }
    });
    sockets = new WebSocketServer({ noServer: true });
    upstream.on('upgrade', (request, socket, head) => {
        if (!request.url.startsWith('/hubs/projection-changes')) { socket.destroy(); return; }
        sockets.handleUpgrade(request, socket, head, connection => sockets.emit('connection', connection, request));
    });
    sockets.on('connection', socket => socket.on('message', data => {
        for (const part of data.toString().split('\x1e').filter(Boolean)) {
            const message = JSON.parse(part);
            if (message.protocol) socket.send('{}\x1e');
            else if (message.type === 1 && message.invocationId) socket.send(JSON.stringify({ type: 3, invocationId: message.invocationId }) + '\x1e');
        }
    }));
    await new Promise(resolve => upstream.listen(0, '127.0.0.1', resolve));
    const upstreamBase = `http://127.0.0.1:${upstream.address().port}`;
    const uiPort = await freePort();
    const fixtureBase = `https://localhost:${uiPort}`;
    if (standaloneOidc) oidcFixture = await startLocalOidcFixture();
    const originalEnvironment = standaloneOidc ? {} : Object.fromEntries((await readFile(`/proc/${process.env.TENANTS_BROWSER_SOURCE_UI_PID}/environ`, 'utf8'))
        .split('\0').filter(Boolean).map(entry => { const index = entry.indexOf('='); return [entry.slice(0, index), entry.slice(index + 1)]; }));
    const environment = { ...originalEnvironment, ...process.env, ASPNETCORE_URLS: fixtureBase, ASPNETCORE_HTTPS_PORT: String(uiPort),
        Tenants__BaseAddress: upstreamBase, EventStore__BaseAddress: upstreamBase, Memories__BaseAddress: upstreamBase };
    if (standaloneOidc) Object.assign(environment, { ASPNETCORE_ENVIRONMENT: 'Development',
        Authentication__OpenIdConnect__Authority: oidcFixture.authority,
        Authentication__OpenIdConnect__ClientId: 'tenants-browser-fixture', Authentication__OpenIdConnect__ClientSecret: randomUUID() });
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
    if (!standaloneOidc) await context.route(url => url.origin === base.origin
        && ['/authentication/challenge', '/signin-oidc'].includes(url.pathname), async route => {
        const request = route.request();
        const original = new URL(request.url());
        const response = await route.fetch({ url: fixtureBase + original.pathname + original.search,
            headers: { ...request.headers(), host: base.host }, maxRedirects: 0 });
        await route.fulfill({ response });
    });
    phase = 'authenticate-challenge';
    await page.goto(new URL('/authentication/challenge?returnUrl=%2Ftenants', standaloneOidc ? fixtureBase : base).href);
    phase = 'authenticate-credential-form';
    if (!standaloneOidc) {
        await page.locator('#username').fill(username);
        await page.locator('#password').fill(password);
        await page.locator('#kc-login').click();
    }
    phase = 'authenticate-callback';
    await page.waitForURL(url => url.origin === (standaloneOidc ? fixtureBase : base.origin) && !url.pathname.includes('signin-oidc'));
    await testId('tenants-workspace').waitFor();
    phase = 'administrator-route-readiness';
    await page.goto(`${fixtureBase}/global-administrators`);
    await page.waitForLoadState('networkidle');
    await testId('tenants-global-admins-ready').waitFor();
    phase = 'initial-host-styles';
    const measureHostStyles = async () => {
        const styles = await page.evaluate(() => {
            const read = value => [...document.querySelectorAll(value)].map(element => {
                const style = getComputedStyle(element);
                return { tag: element.localName, role: element.getAttribute('role'), display: style.display, minWidth: style.minWidth, minHeight: style.minHeight,
                    maxWidth: style.maxWidth, gap: style.gap, whiteSpace: style.whiteSpace, overflowWrap: style.overflowWrap };
            });
            return { width: innerWidth, area: read('.global-admins'), form: read('.global-admins__grant-form'),
                labels: read('.global-admins__grant label'), grid: read('.global-admins__data-grid'),
                identities: read('.global-admins__identity-value'), states: read('.global-admins__grant-state-symbol, .global-admins__remove-state-symbol'),
                icon: read('.global-admins__state-icon') };
        });
        (evidence.styleObservations ||= []).push(styles);
        check(styles.area.length === 1 && styles.area[0].minWidth === '0px', 'Fluent page stack must receive the owned min-width reset');
        check(styles.form.length === 1 && styles.form[0].display === 'grid' && styles.form[0].gap === '8px', 'Generated grant form must receive the owned grid gap');
        check(styles.labels.length > 0 && styles.labels.every(value => value.display === 'grid' && value.gap === '4px' && value.maxWidth === '576px'), 'Generated Fluent input labels must receive owned layout');
        check(styles.grid.filter(value => value.tag === 'table' && value.role === 'grid').length === 1
            && styles.grid.find(value => value.tag === 'table' && value.role === 'grid').minWidth === '832px'
            && styles.grid.filter(value => value.tag === 'th').every(value => value.minWidth !== '832px'), 'Fluent administrator grid must receive its scroll-container minimum width');
        check(styles.identities.length === 2 && styles.identities.every(value => value.display === 'block' && value.whiteSpace === 'pre-wrap'
            && value.overflowWrap === 'anywhere' && value.maxWidth === (styles.width <= 672 ? '256px' : '384px')),
            'Fluent identity values must receive wrapping and measured responsive width limits');
        check(styles.states.length === 2 && styles.states.every(value => value.display === 'inline-grid' && value.minWidth === '28px' && value.minHeight === '28px'),
            'Fluent lifecycle symbols must receive their owned layout');
        check(styles.icon.length === 1 && ['inline-grid', 'grid'].includes(styles.icon[0].display) && styles.icon[0].minWidth === '32px' && styles.icon[0].minHeight === '32px',
            'Fluent state glyph must receive its owned layout');
        const scopeControls = await page.evaluate(() => {
            const controls = [
                { owner: '.global-admins-style-scope', target: '.global-admins__grant-state-symbol', property: 'minWidth', expected: '28px' },
                { owner: '.global-admins__grant', target: '.global-admins__grant-form', property: 'display', expected: 'grid' },
                { owner: '.global-admins__grant', target: '.global-admins__grant label', property: 'display', expected: 'grid' },
                { owner: '.global-admins__table-scroll', target: 'table.global-admins__data-grid[role="grid"]', property: 'minWidth', expected: '832px' },
            ];
            return controls.map(control => {
                const owner = document.querySelector(control.owner);
                const target = document.querySelector(control.target);
                const attributes = [...owner.attributes].filter(attribute => attribute.name.startsWith('b-')).map(attribute => [attribute.name, attribute.value]);
                const before = getComputedStyle(target)[control.property];
                let removed;
                try {
                    for (const [name] of attributes) owner.removeAttribute(name);
                    removed = getComputedStyle(target)[control.property];
                } finally {
                    for (const [name, value] of attributes) owner.setAttribute(name, value);
                }
                return { owner: control.owner, target: control.target, property: control.property, expected: control.expected,
                    scopeAttributeCount: attributes.length, before, removed, restored: getComputedStyle(target)[control.property] };
            });
        });
        (evidence.scopeNegativeControls ||= []).push(scopeControls);
        check(scopeControls.every(control => control.scopeAttributeCount > 0 && control.before === control.expected
            && control.removed !== control.expected && control.restored === control.expected),
            'Removing each native owner scope must break the fixed style and restoring scope must recover it');
        return styles;
    };
    const grantLauncher = testId('tenants-global-admin-grant-submit');
    const removeLauncher = page.locator(`${selector('tenants-global-admin-remove')}[data-user-id="${longTarget}"]`);
    const origin = kind => kind === 'grant' ? grantLauncher : removeLauncher;
    const originSelector = kind => kind === 'grant' ? selector('tenants-global-admin-grant-submit')
        : `${selector('tenants-global-admin-remove')}[data-user-id="${longTarget}"]`;
    const openPreview = async kind => {
        const ids = controls(kind);
        if (kind === 'grant') {
            const input = page.locator('input[name="global-administrator-user-id"]');
            await input.fill(grantTarget);
            await input.press('Tab');
        }
        await wait(() => origin(kind).evaluate(element => !element.hasAttribute('disabled')), 'Measured safe viewport must allow preview');
        await origin(kind).click();
        await testId(ids.dialog).waitFor();
        await waitFocus(selector(ids.acknowledge));
    };
    const acknowledge = async kind => {
        const ids = controls(kind);
        if (kind === 'grant') await testId(ids.acknowledge).click();
        else {
            const input = testId(ids.acknowledge).locator('input');
            await input.fill(longTarget);
            await input.press('Tab');
        }
        await wait(() => testId(ids.confirm).evaluate(element => !element.hasAttribute('disabled')), 'Exact acknowledgement must enable confirmation');
    };
    const traverse = async (kind, enabled) => {
        const ids = controls(kind);
        const expected = enabled ? [ids.acknowledge, ids.confirm, ids.cancel] : [ids.acknowledge, ids.cancel];
        const traces = [];
        for (const key of ['Tab', 'Shift+Tab']) {
            const seen = new Set();
            for (let step = 0; step < expected.length * 4; step++) {
                await page.keyboard.press(key);
                await waitFocus(selector(ids.dialog));
                const observation = await observeFocus();
                const control = expected.find(id => observation.ancestry.includes(id));
                check(Boolean(control), 'Native keyboard traversal must remain on the enabled dialog controls');
                seen.add(control);
                traces.push({ key, step, control, element: observation.element, shadowDepth: observation.shadowDepth });
            }
            check(seen.size === expected.length, 'Native keyboard traversal must visit every enabled dialog control in both directions');
        }
        return traces;
    };
    const assertInert = async kind => {
        const area = testId('tenants-global-admins-area');
        check(await area.getAttribute('inert') !== null, 'Dialog background must be inert');
        check(await area.getAttribute('aria-hidden') === 'true', 'Dialog background must be hidden from accessibility');
        await testId('tenants-global-admin-grant-lifecycle').evaluate(element => element.focus());
        check(!await focused(selector('tenants-global-admin-grant-lifecycle')), 'Inert background must reject programmatic focus');
        await testId(controls(kind).acknowledge).focus();
        await waitFocus(selector(controls(kind).acknowledge));
    };
    if (!safetyOnly) {
    for (const viewport of [{ width: 1366, height: 1000 }, { width: 768, height: 1000 },
        { width: 1024, height: 400 }]) {
        await page.setViewportSize(viewport);
        const hostStyles = await measureHostStyles();
        for (const kind of ['grant', 'remove']) {
            phase = `${kind}-${viewport.width}x${viewport.height}`;
            console.log(`Administrator dialog case: ${phase}`);
            await openPreview(kind);
            const ids = controls(kind);
            await assertInert(kind);
            check(await testId(ids.confirm).getAttribute('disabled') !== null, 'Confirmation must be disabled before acknowledgement');
            const measurements = await boundedDialog(kind);
            const disabledKeyboard = await traverse(kind, false);
            await acknowledge(kind);
            const enabledKeyboard = await traverse(kind, true);
            // Confirm is only focused. Clicking it would write domain state and is prohibited.
            await testId(ids.cancel).focus();
            await waitFocus(selector(ids.cancel));
            const cancelBounds = await testId(ids.cancel).boundingBox();
            check(cancelBounds.y >= 0 && cancelBounds.y + cancelBounds.height <= viewport.height + 1,
                'Dialog Cancel must scroll into the measured short viewport');
            await page.keyboard.press('Escape');
            await testId(ids.dialog).waitFor({ state: 'detached' });
            await waitFocus(originSelector(kind));
            check(await testId('tenants-global-admins-area').getAttribute('inert') === null, 'Cancellation must release inertness');
            evidence.cases.push({ kind, scenario: 'modal-keyboard-layout', viewport, measurements, hostStyles,
                disabledKeyboard, enabledKeyboard, cancelReachable: true, inertBackground: true,
                escapeRestoresOrigin: true, restoredFocus: await observeFocus() });
        }
    }
    for (const kind of ['grant', 'remove']) {
        phase = `${kind}-unsafe-downgrade`;
        await page.setViewportSize({ width: 1024, height: 800 });
        await openPreview(kind);
        await acknowledge(kind);
        const ids = controls(kind);
        await page.setViewportSize({ width: 767, height: 400 });
        await wait(() => testId(ids.confirm).evaluate(element => element.hasAttribute('disabled')), 'Measured phone tier must disable an existing confirmation');
        const thresholdMeasurements = await boundedDialog(kind);
        await page.setViewportSize({ width: 390, height: 400 });
        const phoneMeasurements = await boundedDialog(kind);
        const hostStyles = await measureHostStyles();
        await testId(ids.cancel).click();
        await testId(ids.dialog).waitFor({ state: 'detached' });
        await grantLauncher.waitFor({ state: 'hidden' });
        await removeLauncher.waitFor({ state: 'hidden' });
        check(await testId('tenants-global-admins-mobile-readonly').isVisible(), 'Phone layout must explain its read-only mutation boundary');
        evidence.cases.push({ kind, scenario: 'unsafe-viewport', thresholdMeasurements, phoneMeasurements, hostStyles,
            confirmationDisabled: true, cancelReachable: true, launchersHidden: true, readonlyExplanationVisible: true });
    }
    phase = 'forced-colors-reduced-motion';
    await page.setViewportSize({ width: 1024, height: 800 });
    await page.emulateMedia({ forcedColors: 'active', reducedMotion: 'reduce' });
    const mediaHostStyles = await measureHostStyles();
    await grantLauncher.focus();
    await page.keyboard.press('Tab');
    // Recovery regions deliberately have tabindex=-1. Establish keyboard modality with
    // native Tab, then use the production region's programmatic recovery-focus contract.
    await testId('tenants-global-admin-grant-lifecycle').focus();
    await waitFocus(selector('tenants-global-admin-grant-lifecycle'));
    const mediaFacts = await page.evaluate(() => {
        const probe = document.createElement('span');
        document.body.append(probe);
        probe.style.color = 'Highlight';
        const highlight = getComputedStyle(probe).color;
        probe.style.color = 'CanvasText';
        const canvasText = getComputedStyle(probe).color;
        probe.remove();
        const region = document.querySelector('[data-testid="tenants-global-admin-grant-lifecycle"]');
        const style = getComputedStyle(region);
        const glyphs = [...document.querySelectorAll('.global-admins__state-icon, .global-admins__grant-state-symbol, .global-admins__remove-state-symbol')]
            .map(element => { const style = getComputedStyle(element); return { borderWidth: style.borderTopWidth, borderColor: style.borderTopColor }; });
        return { forcedColors: matchMedia('(forced-colors: active)').matches,
            reducedMotion: matchMedia('(prefers-reduced-motion: reduce)').matches,
            highlight, canvasText, glyphs, recoveryFocus: { isActive: document.activeElement === region,
                focusVisible: region.matches(':focus-visible'), outlineWidth: style.outlineWidth,
                outlineStyle: style.outlineStyle, outlineColor: style.outlineColor } };
    });
    check(mediaFacts.forcedColors && mediaFacts.reducedMotion, 'Browser must activate both requested accessibility media preferences');
    check(mediaFacts.glyphs.length === 3 && mediaFacts.glyphs.every(glyph => glyph.borderWidth === '2px' && glyph.borderColor === mediaFacts.canvasText),
        'Forced-color Fluent glyph borders must use the actual CanvasText system color');
    check(mediaFacts.recoveryFocus.isActive && mediaFacts.recoveryFocus.focusVisible
        && mediaFacts.recoveryFocus.outlineWidth === '2px' && mediaFacts.recoveryFocus.outlineStyle === 'solid'
        && mediaFacts.recoveryFocus.outlineColor === mediaFacts.highlight,
        'Keyboard-modality recovery focus must render the actual Highlight outline');
    const mediaDialogs = [];
    for (const kind of ['grant', 'remove']) {
        await openPreview(kind);
        const ids = controls(kind);
        const measurements = await boundedDialog(kind);
        const border = await testId(ids.dialog).evaluate(element => {
            const style = getComputedStyle(element);
            return { width: style.borderTopWidth, color: style.borderTopColor };
        });
        check(border.width === '2px' && border.color === mediaFacts.canvasText,
            'Forced-color dialog border must use CanvasText at its approved two-pixel width');
        const keyboard = await traverse(kind, false);
        await testId(ids.cancel).focus();
        await page.keyboard.press('Escape');
        await testId(ids.dialog).waitFor({ state: 'detached' });
        await waitFocus(originSelector(kind));
        mediaDialogs.push({ kind, measurements, border, keyboard, escapeRestoresOrigin: true });
    }
    evidence.cases.push({ scenario: 'forced-colors-reduced-motion', hostStyles: mediaHostStyles,
        mediaFacts, dialogs: mediaDialogs,
        focusMethod: 'Native Tab establishes keyboard modality; programmatic focus uses the non-tab-stop recovery region',
        reducedMotionScope: 'Preference is active and interactions remain usable; no animation-duration contract is claimed' });
    await page.emulateMedia({ forcedColors: 'none', reducedMotion: 'no-preference' });
    }

    phase = 'member-removal-native-modal';
    console.log('UI safety case: member-native-modal-keyboard');
    await page.setViewportSize({ width: 1366, height: 1000 });
    await page.goto(`${fixtureBase}/tenants/tenant.alpha`);
    const memberLauncher = page.getByRole('row', { includeHidden: true }).filter({ has: page.locator('#tenants-member-browser-reader') })
        .getByTestId('tenants-remove-member-open');
    phase = 'member-removal-launcher-ready';
    await memberLauncher.waitFor();
    phase = 'member-removal-open-native-modal';
    await memberLauncher.click();
    await testId('tenants-remove-member-modal').waitFor({ state: 'attached' });
    await waitFocus(selector('tenants-remove-member-cancel'));
    phase = 'member-removal-native-state';
    check(await testId('tenants-remove-member-preview-item').count() === 10, 'Removal modal must preserve all ten consequence facts');
    const nativeModal = async () => testId('tenants-remove-member-modal').evaluate(element => {
        const find = root => {
            for (const child of root.querySelectorAll('*')) {
                if (child.matches(':modal')) return true;
                if (child.shadowRoot && find(child.shadowRoot)) return true;
            }
            return false;
        };
        return element.matches(':modal') || find(element) || !!element.shadowRoot && find(element.shadowRoot);
    });
    await wait(nativeModal, 'Fluent removal must open a native modal surface');
    const memberMeasurements = [];
    for (const viewport of [{ width: 1366, height: 1000 }, { width: 768, height: 800 }, { width: 1024, height: 400 }]) {
        await page.setViewportSize(viewport);
        const measured = await testId('tenants-remove-member-modal').evaluate(element => {
            const dialog = element.shadowRoot.querySelector('dialog');
            const bounds = dialog.getBoundingClientRect();
            return { viewport: { width: innerWidth, height: innerHeight },
                bounds: { left: bounds.left, right: bounds.right, top: bounds.top, bottom: bounds.bottom },
                clientWidth: dialog.clientWidth, scrollWidth: dialog.scrollWidth,
                clientHeight: dialog.clientHeight, scrollHeight: dialog.scrollHeight,
                overflowY: getComputedStyle(dialog).overflowY,
                completeFacts: element.querySelectorAll('[data-testid="tenants-remove-member-preview-item"]').length };
        });
        check(measured.bounds.left >= -1 && measured.bounds.right <= viewport.width + 1
            && measured.bounds.top >= -1 && measured.bounds.bottom <= viewport.height + 1,
            'Complete removal modal must fit measured tablet and short-height viewports');
        check(measured.scrollWidth <= measured.clientWidth + 1 && measured.completeFacts === 10,
            'Complete removal preview must avoid horizontal clipping');
        if (measured.scrollHeight > measured.clientHeight + 1)
            check(['auto', 'scroll'].includes(measured.overflowY), 'Short removal modal must permit vertical scrolling');
        memberMeasurements.push(measured);
    }
    await page.setViewportSize({ width: 1366, height: 1000 });
    phase = 'member-removal-background-inertness';
    await memberLauncher.evaluate(element => element.focus());
    check(!await memberLauncher.evaluate(element => element === document.activeElement
        || element.shadowRoot?.contains(document.activeElement)),
        'Native modal must reject background focus');
    await waitFocus(selector('tenants-remove-member-modal'));
    phase = 'member-removal-native-tab-traversal';
    const memberKeyboard = [];
    for (const key of ['Tab', 'Shift+Tab']) {
        for (let step = 0; step < 12; step++) {
            await page.keyboard.press(key);
            await waitFocus(selector('tenants-remove-member-modal'));
            memberKeyboard.push({ key, step, ...(await observeFocus()) });
        }
    }
    phase = 'member-removal-safe-escape';
    await page.keyboard.press('Escape');
    await testId('tenants-remove-member-modal').waitFor({ state: 'detached' });
    // Fluent also schedules its own 25ms origin return; observe the final owning destination.
    await page.waitForTimeout(75);
    check(await memberLauncher.evaluate(element => {
        let active = document.activeElement;
        while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
        return active === element || element.contains(active) || element.shadowRoot?.contains(active);
    }), 'Safe removal Escape must restore the exact launching row control');
    evidence.cases.push({ scenario: 'member-native-modal-keyboard', allPreviewFacts: true,
        nativeModal: true, backgroundInert: true, measurements: memberMeasurements,
        keyboard: memberKeyboard, exactFocusReturn: true });

    phase = 'member-pending-dismissal-and-resize';
    console.log('UI safety case: member-pending-native-dismissal');
    await memberLauncher.click();
    await testId('tenants-remove-member-confirmation').locator('input').fill('browser-reader');
    await page.keyboard.press('Tab');
    await wait(() => testId('tenants-remove-member-confirm').evaluate(element => !element.hasAttribute('disabled')),
        'Measured complete removal must enable its confirmation');
    commandScenario = 'remove-pending';
    await testId('tenants-remove-member-confirm').click();
    await wait(() => testId('tenants-remove-member-cancel').evaluate(element => element.hasAttribute('disabled')),
        'Pending removal must retain guarded dismissal');
    phase = 'member-pending-before-keyboard';
    check(await nativeModal(), 'Dispatched removal must retain its native modal before keyboard input');
    for (const key of ['Escape', 'Tab', 'Shift+Tab', 'Escape']) {
        phase = `member-pending-key-${key}`;
        await page.keyboard.press(key);
        check(await nativeModal(), 'Pending keyboard dismissal must leave the native modal open');
        await waitFocus(selector('tenants-remove-member-modal'));
    }
    await page.mouse.click(1, 1);
    check(await nativeModal(), 'Pending overlay click must leave the native modal open');
    await page.setViewportSize({ width: 767, height: 500 });
    await wait(() => testId('tenants-remove-member-confirm').evaluate(element => element.hasAttribute('disabled')),
        'Measured narrow resize must refuse removal submission');
    check(await nativeModal(), 'Narrow resize must preserve the pending modal');
    for (const key of ['Tab', 'Shift+Tab', 'Escape']) {
        await page.keyboard.press(key);
        check(await nativeModal(), 'Held delivery on a narrow viewport must retain the modal');
        await waitFocus(selector('tenants-remove-member-modal'));
    }
    releasePendingDelivery();
    await wait(() => testId('tenants-remove-member-refresh').evaluate(element => !element.hasAttribute('disabled')),
        'Accepted pending removal must retain status recovery');
    await testId('tenants-remove-member-refresh').focus();
    await waitFocus(selector('tenants-remove-member-refresh'));
    check(fixture.simulatedCommands === 1 && fixture.commands === 0, 'Pending recovery must not dispatch another command');
    evidence.cases.push({ scenario: 'member-pending-native-dismissal', escapeRetainsModal: true,
        overlayRetainsModal: true, narrowResizeRetainsWork: true, recoveryReachable: true, simulatedCommands: 1 });
    commandScenario = undefined;

    phase = 'create-measured-resize-conflict-recovery';
    console.log('UI safety case: create-measured-conflict-recovery');
    await page.setViewportSize({ width: 1366, height: 1000 });
    await page.goto(`${fixtureBase}/tenants?sort=name`);
    await testId('tenants-create-accordion').getByRole('button').click();
    await testId('tenants-create-tenant-id').locator('input').fill('tenant.alpha');
    await testId('tenants-create-name').locator('input').fill('Attempted name');
    await page.keyboard.press('Tab');
    await wait(() => testId('tenants-create-submit').evaluate(element => !element.hasAttribute('disabled')),
        'Measured safe first-tenant view must permit create');
    await page.setViewportSize({ width: 767, height: 500 });
    await wait(() => testId('tenants-create-submit').evaluate(element => element.hasAttribute('disabled')),
        'Measured unsafe create viewport must disable submission');
    check(await testId('tenants-create-unavailable-reason').isVisible(), 'Create must offer visible localized viewport recovery');
    await page.setViewportSize({ width: 1024, height: 800 });
    await wait(() => testId('tenants-create-submit').evaluate(element => !element.hasAttribute('disabled')),
        'Widening must restore create readiness');
    check(await testId('tenants-create-tenant-id').locator('input').inputValue() === 'tenant.alpha', 'Resize must retain the entered tenant identity');
    commandScenario = 'create-conflict';
    await testId('tenants-create-submit').click();
    await testId('tenants-create-open-existing').waitFor();
    await wait(() => testId('tenants-create-open-existing').evaluate(element =>
        !element.hasAttribute('disabled') && element.getAttribute('aria-disabled') !== 'true'),
        'Open-existing must be enabled before native keyboard activation');
    denyExistingRead = true;
    phase = 'create-failed-click-open-existing';
    await testId('tenants-create-open-existing').focus();
    await page.keyboard.press('Enter');
    await wait(() => testId('tenants-create-open-existing').count().then(count => count === 0),
        'A failed click-time read must withdraw open-existing');
    phase = 'create-failed-click-lifecycle-focus';
    await waitFocus('#tenants-create-lifecycle');
    evidence.failedClickFocus = await observeFocus();
    check(['/tenants', '/tenants/tenants'].includes(new URL(page.url()).pathname),
        'A failed click recheck must keep workspace context');
    denyExistingRead = false;
    // A new measured admission scope can re-evaluate the same retained rejection without another command.
    await page.setViewportSize({ width: 767, height: 500 });
    await wait(() => testId('tenants-create-unavailable-reason').textContent().then(text => text.includes('wider view')),
        'A measured unsafe scope must be observed before recovery is re-evaluated');
    await page.setViewportSize({ width: 1024, height: 800 });
    await testId('tenants-create-open-existing').waitFor();
    await wait(() => testId('tenants-create-open-existing').evaluate(element =>
        !element.hasAttribute('disabled') && element.getAttribute('aria-disabled') !== 'true'),
        'Open-existing must be enabled before native keyboard activation');
    phase = 'create-authorized-open-existing-navigation';
    await testId('tenants-create-open-existing').focus();
    await page.keyboard.press('Enter');
    await page.waitForURL(url => url.pathname === '/tenants/tenant.alpha');
    const returnedContext = new URL(page.url()).searchParams.get('returnUrl');
    check(returnedContext?.includes('sort=name') && returnedContext.includes('anchor=tenants-create-lifecycle'),
        'Authorized conflict navigation must preserve workspace context and create focus return');
    phase = 'create-detail-back-navigation';
    await testId('tenants-detail-back').click();
    await page.waitForURL(url => ['/tenants', '/tenants/tenants'].includes(url.pathname)
        && url.searchParams.get('sort') === 'name');
    await testId('tenants-create-lifecycle').waitFor();
    phase = 'create-back-lifecycle-focus';
    await waitFocus('#tenants-create-lifecycle');
    evidence.backFocus = await observeFocus();
    check(await testId('tenants-create-lifecycle').isVisible(), 'Conflict Back must expand the create accordion before restoring focus');
    evidence.cases.push({ scenario: 'create-measured-conflict-recovery', unsafeSubmitDisabled: true,
        safeResizeRecovers: true, enteredIdentityRetained: true, authorizedOpenExisting: true,
        returnContextPreserved: true, backRestoresVisibleCreateLifecycle: true, failedOpenExistingRestoresLifecycleFocus: true });
    commandScenario = undefined;
    check(fixture.adminReads > 0 && fixture.authorizedReads > 0, 'Real authenticated production gateway reads must reach the fixture');
    check(fixture.commands === 0 && fixture.simulatedCommands === 2, 'Only the two fixture-local command simulations may be attempted');
    check(circuitErrors.length === 0, 'The interactive circuit must remain healthy');
    evidence.sourceAfterBrowser = await captureSource();
    check(JSON.stringify(sourceBeforeBuild) === JSON.stringify(evidence.sourceAfterBrowser),
        'Source or build inputs changed during browser acceptance; evidence is refused');
    evidence.passed = true;
} catch (error) {
    evidence.passed = false;
    evidence.failure = { phase, label: error.safeLabel || 'Browser operation failed', focus: page ? await observeFocus().catch(() => null) : null };
    evidence.failure.operation = typeof error.message === 'string' ? error.message.split(':', 1)[0] : undefined;
    if (page && phase.startsWith('member-')) evidence.failure.memberDiagnostics = await page.evaluate(() => ({
        launchers: document.querySelectorAll('[data-testid="tenants-remove-member-open"]').length,
        targetPresent: !!document.getElementById('tenants-member-browser-reader'),
        state: document.querySelector('[data-testid="tenants-remove-member-state"]')?.textContent.trim(),
        safeMessage: document.querySelector('[data-testid="tenants-remove-member-safe-message"]')?.textContent.trim(),
        reasons: [...document.querySelectorAll('[data-testid="tenants-member-unavailable-reason"], [data-testid="tenants-remove-member-unavailable-reason"]')]
            .map(element => element.textContent.trim()),
        modal: [...document.querySelectorAll('[data-testid="tenants-remove-member-modal"]')].map(element => ({
            tag: element.localName, type: element.getAttribute('type'),
            shadowTags: [...(element.shadowRoot?.querySelectorAll('*') || [])].map(child => child.localName),
            nativeTag: element.dialog?.localName, nativeOpen: element.dialog?.open,
            nativeModal: element.dialog?.matches?.(':modal'),
            escapeHandlerInstalled: typeof element._escapeHandler === 'function',
            formMethod: element.querySelector('form')?.getAttribute('method'),
        })),
    })).catch(() => null);
    process.exitCode = 1;
} finally {
    releasePendingDelivery?.();
    evidence.fixture = { authorizedReads: fixture.authorizedReads, administratorReads: fixture.adminReads,
        unexpectedCommandAttempts: fixture.commands, simulatedCommandAttempts: fixture.simulatedCommands, circuitErrors: circuitErrors.length };
    evidence.acceptedForClosure = evidence.passed === true && evidence.build?.succeeded === true
        && !evidence.build?.reusedBuild && !safetyOnly && evidence.cases.length === 12;
    evidence.finishedUtc = new Date().toISOString();
    let evidenceWriteFailed = false;
    try {
        await writeFile(output, JSON.stringify(evidence, null, 2) + '\n');
    } catch {
        evidenceWriteFailed = true;
        process.exitCode = 1;
        console.error('Authenticated administrator dialog evidence could not be stored');
    } finally {
        await Promise.allSettled([context?.close(), browser?.close(), (async () => {
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
            await oidcFixture?.close();
        })()]);
    }
    console.log(`Authenticated administrator dialogs: ${evidence.passed && !evidenceWriteFailed ? 'passed' : 'failed'}; ${evidence.cases.length} cases; phase ${phase}`);
}
