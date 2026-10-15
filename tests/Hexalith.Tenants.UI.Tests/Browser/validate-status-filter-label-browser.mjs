// Verify Chromium's computed accessibility tree for actual bUnit/Fluent output.
// Node's built-in WebSocket keeps this lane free of new package dependencies.
import assert from 'node:assert/strict';
import { spawn, spawnSync } from 'node:child_process';
import { once } from 'node:events';
import { createServer } from 'node:http';
import { mkdtemp, readFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '../../..');
const [configuration = 'Debug', browser = 'google-chrome-stable', timeoutSeconds = '30'] = process.argv.slice(2);
assert.match(timeoutSeconds, /^[1-9][0-9]*$/);
const temporary = await mkdtemp(join(tmpdir(), 'tenants-status-label-'));
let chrome, socket, server, deadline;
try {
    const executable = join(root, 'tests/Hexalith.Tenants.UI.Tests/bin', configuration, 'net10.0/Hexalith.Tenants.UI.Tests');
    const exported = spawnSync(executable, ['-class', 'Hexalith.Tenants.UI.Tests.Components.StatusFilterLabelRenderingTests'], {
        cwd: root, encoding: 'utf8', env: { ...process.env, TENANTS_STATUS_LABEL_FIXTURE_DIRECTORY: temporary },
        timeout: Number(timeoutSeconds) * 1000, killSignal: 'SIGKILL',
    });
    assert.equal(exported.status, 0, `Rendered label fixture export failed: ${exported.error ?? ''}\n${exported.stdout}\n${exported.stderr}`);
    const assets = JSON.parse(await readFile(join(root, 'src/Hexalith.Tenants.UI/obj/project.assets.json'), 'utf8'));
    const library = Object.keys(assets.libraries).find(name => name.toLowerCase().startsWith('microsoft.fluentui.aspnetcore.components/'));
    assert.ok(library, 'Restored Fluent package must exist');
    const modulePath = join(Object.keys(assets.packageFolders)[0], library.toLowerCase(),
        'staticwebassets/Microsoft.FluentUI.AspNetCore.Components.lib.module.js');
    const fluent = await readFile(modulePath);
    const labelModule = await readFile(join(root, 'src/Hexalith.Tenants.UI/wwwroot/js/tenantsStatusFilter.js'));
    server = createServer((request, response) => {
        if (request.url === '/fluent-ui.js') {
            response.writeHead(200, { 'content-type': 'text/javascript' });
            response.end(fluent);
        } else if (request.url === '/status-filter.js') {
            response.writeHead(200, { 'content-type': 'text/javascript' });
            response.end(labelModule);
        } else if (request.url === '/') {
            response.writeHead(200, { 'content-type': 'text/html; charset=utf-8' });
            response.end('<!doctype html><html lang="en"><body><div id="fixture"></div></body></html>');
        } else {
            response.writeHead(404).end();
        }
    });
    server.listen(0, '127.0.0.1');
    await once(server, 'listening');
    chrome = spawn(browser, ['--no-sandbox', '--headless=new', '--disable-dev-shm-usage', '--disable-gpu',
        '--no-first-run', '--no-default-browser-check', '--remote-debugging-port=0',
        `--user-data-dir=${temporary}/profile`, 'about:blank'], { stdio: 'ignore' });
    await once(chrome, 'spawn');
    deadline = setTimeout(() => {
        console.error(`Chromium status label validation exceeded ${timeoutSeconds}s.`);
        chrome.kill('SIGKILL');
        process.exitCode = 1;
        socket?.close();
    }, Number(timeoutSeconds) * 1000);
    let port;
    while (!port) {
        assert.equal(chrome.exitCode, null, 'Chromium exited before CDP became ready');
        try { [port] = (await readFile(`${temporary}/profile/DevToolsActivePort`, 'utf8')).split('\n'); }
        catch { await delay(25); }
    }
    const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
    socket = new WebSocket(targets.find(target => target.type === 'page').webSocketDebuggerUrl);
    await new Promise((accept, reject) => {
        socket.addEventListener('open', accept, { once: true });
        socket.addEventListener('error', reject, { once: true });
    });
    let sequence = 0;
    const pending = new Map();
    socket.addEventListener('message', event => {
        const message = JSON.parse(event.data);
        const promise = pending.get(message.id);
        if (!promise) return;
        pending.delete(message.id);
        message.error ? promise.reject(new Error(JSON.stringify(message.error))) : promise.accept(message.result);
    });
    socket.addEventListener('close', () => {
        for (const promise of pending.values()) promise.reject(new Error('Chromium connection closed'));
        pending.clear();
    });
    function cdp(method, params = {}) {
        return new Promise((accept, reject) => {
            const id = ++sequence;
            pending.set(id, { accept, reject });
            socket.send(JSON.stringify({ id, method, params }));
        });
    }
    async function evaluate(expression) {
        const result = await cdp('Runtime.evaluate', { expression, awaitPromise: true, returnByValue: true });
        assert.equal(result.exceptionDetails, undefined, JSON.stringify(result.exceptionDetails));
        return result.result.value;
    }
    await cdp('Page.enable');
    await cdp('Page.navigate', { url: `http://127.0.0.1:${server.address().port}/` });
    await evaluate(`(async () => { const fluent = await import('/fluent-ui.js'); fluent.beforeStart(document, 'web'); await customElements.whenDefined('fluent-dropdown'); })()`);
    await cdp('Accessibility.enable');
    const results = [];
    for (const culture of ['en', 'fr']) {
        for (const mode of ['ordinary', 'fallback', 'authoritative', 'partial', 'empty',
            'transition-0', 'transition-1', 'transition-2', 'transition-3', 'transition-4', 'delayed-0', 'delayed-1',
            'status-0', 'status-1', 'status-2']) {
            const markup = await readFile(join(temporary, `status-${culture}-${mode}.html`), 'utf8');
            const { syncCount } = JSON.parse(await readFile(join(temporary, `status-${culture}-${mode}.json`), 'utf8'));
            const authoritative = ['authoritative', 'partial', 'empty', 'transition-1', 'transition-3', 'delayed-1', 'status-0', 'status-2'].includes(mode);
            const expected = culture === 'fr'
                ? (authoritative ? 'Statut parmi les candidats indexés' : 'Statut sur la page actuelle')
                : (authoritative ? 'Status across indexed candidates' : 'Status on current page');
            // Reconcile actual successive render output onto the same Fluent element during transitions.
            const visible = await evaluate(`(async () => {
                const template = document.createElement('template'); template.innerHTML = ${JSON.stringify(markup)};
                const next = template.content.querySelector('fluent-field');
                const current = document.querySelector('#fixture fluent-field');
                let firstRender = true;
                const mode = ${JSON.stringify(mode)};
                const sequence = /^(transition|delayed|status)-/.test(mode);
                if (sequence && current && !mode.endsWith('-0')) {
                    const before = current.querySelector('fluent-dropdown');
                    const after = next.querySelector('fluent-dropdown');
                    if (before.id === after.id) {
                        firstRender = false;
                        for (const attribute of after.attributes) before.setAttribute(attribute.name, attribute.value);
                        current.querySelector('label').textContent = next.querySelector('label').textContent;
                        if (before !== document.querySelector('#fixture fluent-dropdown')) throw new Error('Selector identity changed');
                    } else { throw new Error('A scope change replaced the Fluent selector'); }
                } else { document.getElementById('fixture').replaceChildren(next); window.labelSyncCount = 0; }
                document.documentElement.lang = ${JSON.stringify(culture)};
                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                const field = document.querySelector('#fixture fluent-field');
                if (firstRender) Microsoft.FluentUI.Blazor.Components.Select.Initialize(field.querySelector('fluent-dropdown').id, '');
                await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));
                if (${syncCount} > window.labelSyncCount) {
                    const helper = await import('/status-filter.js');
                    if (!helper.syncStatusFilterLabel()) throw new Error('The shipped label update did not reach the native control');
                    window.labelSyncCount = ${syncCount};
                }
                const control = field.querySelector('button[slot=control]');
                if (mode === 'status-0') { control.focus(); window.focusedStatusControl = control; }
                if (mode.startsWith('status-') && (document.activeElement !== control || window.focusedStatusControl !== control)) {
                    throw new Error('Status selection lost its native control or keyboard focus');
                }
                const label = field.querySelector('label');
                if (!label.getClientRects().length || getComputedStyle(label).visibility !== 'visible') throw new Error('Label is hidden');
                return label.textContent.trim();
            })()`);
            assert.equal(visible, expected, `${culture}/${mode} visible label`);
            const tree = await cdp('Accessibility.getFullAXTree');
            const controls = tree.nodes.filter(node => !node.ignored && node.role?.value === 'combobox');
            assert.equal(controls.length, 1, JSON.stringify(tree.nodes));
            assert.equal(controls[0].name?.value, expected, `${culture}/${mode} computed accessible name`);
            results.push({ culture, mode, visible, accessibleName: controls[0].name.value });
            if (mode === 'authoritative') {
                const stale = culture === 'fr' ? 'Statut sur la page actuelle' : 'Status on current page';
                await evaluate(`document.querySelector('fluent-dropdown [slot=control]').setAttribute('aria-label', ${JSON.stringify(stale)})`);
                const mutation = (await cdp('Accessibility.getFullAXTree')).nodes.find(node => !node.ignored && node.role?.value === 'combobox');
                assert.equal(mutation?.name?.value, stale, 'Control must set a stale name on the actual combobox');
                assert.notEqual(mutation.name.value, expected, 'Stale-name control must fail the naming contract');
            }
        }
    }
    console.log(JSON.stringify({ browser: await cdp('Browser.getVersion'), modulePath, results,
        staleNameControl: 'rejected', scope: 'rendered Fluent fixtures; no authenticated topology or human AT claim' }));
} finally {
    clearTimeout(deadline);
    socket?.close();
    if (chrome?.pid && chrome.exitCode === null) {
        chrome.kill('SIGKILL');
        await once(chrome, 'exit');
    }
    if (server) await new Promise(resolve => server.close(resolve));
    await rm(temporary, { recursive: true, force: true });
}
