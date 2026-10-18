// Capture a validation page after its script reports a status. Chrome's --dump-dom
// exits before async module work on the hosted runner, so this waits on the
// status attribute through DevTools and then prints the document element.
import { spawn } from 'node:child_process';
import { readFile } from 'node:fs/promises';
import { setTimeout as delay } from 'node:timers/promises';

const [browser, url, ...chromeArgs] = process.argv.slice(2);
if (!browser || !url) {
    console.error('dump-validation-dom requires a browser and a URL');
    process.exit(2);
}

const userDataDir = chromeArgs.find(arg => arg.startsWith('--user-data-dir='))?.slice('--user-data-dir='.length);
if (!userDataDir) {
    console.error('dump-validation-dom requires --user-data-dir');
    process.exit(2);
}

const chrome = spawn(browser, [
    '--no-sandbox',
    '--headless=new',
    '--disable-dev-shm-usage',
    '--disable-gpu',
    '--disable-background-networking',
    '--no-default-browser-check',
    '--no-first-run',
    '--remote-debugging-port=0',
    ...chromeArgs.filter(arg => arg !== '--dump-dom' && !arg.startsWith('--virtual-time-budget=') && !arg.startsWith('--timeout=')),
    'about:blank',
], { stdio: 'ignore' });

function stop(code) {
    chrome.kill('SIGKILL');
    process.exit(code);
}

process.on('SIGTERM', () => stop(143));
process.on('SIGINT', () => stop(130));

let port;
const started = Date.now();
while (!port) {
    if (chrome.exitCode !== null) {
        console.error('Chromium exited before DevTools became ready');
        process.exit(1);
    }

    try {
        [port] = (await readFile(`${userDataDir}/DevToolsActivePort`, 'utf8')).split('\n');
    } catch {
        if (Date.now() - started > 10000) {
            console.error('Timed out waiting for DevTools');
            stop(1);
        }

        await delay(25);
    }
}

const targets = await (await fetch(`http://127.0.0.1:${port}/json/list`)).json();
const page = targets.find(target => target.type === 'page');
if (!page?.webSocketDebuggerUrl) {
    console.error('Chromium did not expose a page target');
    stop(1);
}

const socket = new WebSocket(page.webSocketDebuggerUrl);
await new Promise((accept, reject) => {
    socket.addEventListener('open', accept, { once: true });
    socket.addEventListener('error', reject, { once: true });
});

let sequence = 0;
const pending = new Map();
socket.addEventListener('message', event => {
    const message = JSON.parse(event.data);
    const promise = pending.get(message.id);
    if (!promise) {
        return;
    }

    pending.delete(message.id);
    message.error ? promise.reject(new Error(JSON.stringify(message.error))) : promise.accept(message.result);
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
    if (result.exceptionDetails) {
        throw new Error(JSON.stringify(result.exceptionDetails));
    }

    return result.result.value;
}

await cdp('Page.enable');
await cdp('Page.navigate', { url });
const deadline = Date.now() + 20000;
let status = '';
while (!status) {
    if (Date.now() > deadline) {
        console.error('Validation status was not reported before the capture bound');
        stop(1);
    }

    try {
        status = await evaluate("document.documentElement?.dataset.validationStatus || document.body?.dataset.validationStatus || ''");
    } catch {
        status = '';
    }
    if (!status) {
        await delay(25);
    }
}

process.stdout.write(await evaluate('document.documentElement.outerHTML'));
socket.close();
chrome.kill('SIGKILL');
await new Promise(resolve => chrome.once('exit', resolve));
