import assert from 'node:assert/strict';
import { execFile, spawn } from 'node:child_process';
import { createHash } from 'node:crypto';
import { once } from 'node:events';
import { copyFile, mkdir, mkdtemp, readFile, rename, rm, writeFile } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { dirname, join } from 'node:path';
import { createInterface } from 'node:readline';
import test from 'node:test';
import { setTimeout } from 'node:timers/promises';
import { promisify } from 'node:util';
import { captureWorkspaceFocusHost, requireSameWorkspaceFocusHost, workspaceFocusOrigin } from './workspace-focus-host-identity.mjs';

const run = promisify(execFile);
const validator = 'validate-authenticated-workspace-focus.mjs';
const managedFile = 'Hexalith.Tenants.UI.dll';
const fixtureLauncher = `
import signal
import subprocess
import sys

with subprocess.Popen([sys.executable, '-u', '-c', *sys.argv[1:]]) as child:
    signal.signal(signal.SIGTERM, lambda *_: child.terminate())
    sys.exit(child.wait())
`;
const fixtureHost = `
from http.server import BaseHTTPRequestHandler, HTTPServer
from pathlib import Path
import json
import mmap
import os
import sys

class Fixture(BaseHTTPRequestHandler):
    def do_GET(self):
        Path(sys.argv[2]).touch()
        self.send_response(200)
        self.end_headers()

with open(sys.argv[1], 'rb') as f, mmap.mmap(f.fileno(), 0, access=mmap.ACCESS_READ) as mapped:
    with HTTPServer(('127.0.0.1', int(sys.argv[3])), Fixture) as server:
        print(json.dumps({'base': f'http://127.0.0.1:{server.server_port}', 'pid': os.getpid()}), flush=True)
        server.serve_forever()
`;

test('workspace validator distinguishes a stale mapped assembly behind the same registered origin',
    { skip: process.platform !== 'linux' }, async () => {
        const root = await mkdtemp(join(tmpdir(), 'workspace-focus-host-'));
        let host;
        let exited;
        let lines;
        const originalPath = process.env.PATH;
        const originalState = process.env.DW679_ASPIRE_STATE;
        const assembly = join(root, 'src/Hexalith.Tenants.UI/bin/Debug/net11.0', managedFile);
        const requests = join(root, 'requests');
        async function startFixture(port = '0') {
            host = spawn('python3', ['-u', '-c', fixtureLauncher, fixtureHost, assembly, requests, port],
                { stdio: ['ignore', 'pipe', 'inherit'] });
            exited = once(host, 'exit');
            // Record rejection now if spawning fails, even before cleanup awaits it.
            exited.catch(() => {});
            lines = createInterface({ input: host.stdout });
            const [details] = await once(lines, 'line', { signal: AbortSignal.timeout(5_000) });
            return JSON.parse(details);
        }
        async function stopFixture() {
            lines?.close();
            host?.kill();
            await exited?.catch(() => {});
        }
        try {
            const scripts = join(root, 'tests/Hexalith.Tenants.UI.Tests/Browser');
            const bin = join(root, 'bin');
            await Promise.all([scripts, dirname(assembly), bin].map(path => mkdir(path, { recursive: true })));
            await Promise.all([validator, 'workspace-focus-host-identity.mjs'].map(name =>
                copyFile(new URL(name, import.meta.url), join(scripts, name))));
            const contents = 'current mapped assembly bytes';
            await writeFile(assembly, contents);
            // Leave room for the conservative timestamp precision of Linux procfs.
            await setTimeout(50);
            const { base, pid } = await startFixture();

            const state = join(root, 'aspire.json');
            const register = registeredPid => writeFile(state, JSON.stringify({ resources: [{
                name: 'tenants-ui-registered', displayName: 'tenants-ui', resourceType: 'Project', state: 'Running',
                properties: { 'executable.pid': registeredPid }, urls: [{ name: 'http', url: base }],
            }] }));
            await register(host.pid);
            await writeFile(join(bin, 'aspire'), '#!/bin/sh\ncat "$DW679_ASPIRE_STATE"\n', { mode: 0o700 });
            await writeFile(join(bin, 'git'), '#!/bin/sh\nprintf "%040d\\n" 0\n', { mode: 0o700 });
            process.env.PATH = `${bin}:${originalPath}`;
            process.env.DW679_ASPIRE_STATE = state;
            async function invoke(name, origin = base) {
                const output = join(root, `${name}.json`);
                await assert.rejects(run(process.execPath, [join(scripts, validator), output], {
                    timeout: 30_000,
                    env: { ...process.env,
                        TENANTS_BROWSER_BASE_URL: origin, TENANTS_BROWSER_USERNAME: '', TENANTS_BROWSER_PASSWORD: '' },
                }), { code: 1 });
                const evidence = JSON.parse(await readFile(output, 'utf8'));
                assert.deepEqual(evidence.cases, []);
                assert.equal(evidence.browserVersion, undefined);
                assert.equal(JSON.stringify(evidence).includes(root), false);
                assert.equal(JSON.stringify(evidence).includes(base), false);
                return evidence;
            }

            const current = await invoke('current');
            assert.equal(current.failedPhase, 'launch');
            assert.equal(current.failureLabel, 'Supply the credentials chosen before starting the local realm');
            assert.equal(current.runningUiAssemblySha256, createHash('sha256').update(contents).digest('hex'));
            assert.equal(current.localUiAssemblySha256, current.runningUiAssemblySha256);
            assert.equal(current.sourceMetadataScope, 'observed-checkout');

            const origin = workspaceFocusOrigin(base);
            const originalHost = await captureWorkspaceFocusHost(root, origin);
            await requireSameWorkspaceFocusHost(root, origin, originalHost);

            await register(pid);
            const direct = await invoke('direct');
            assert.equal(direct.failedPhase, 'launch');
            assert.equal(direct.runningUiAssemblySha256, current.runningUiAssemblySha256);
            await register(host.pid);

            const other = await invoke('other', base.replace('127.0.0.1', 'localhost'));
            assert.equal(other.failedPhase, 'host-identity');
            assert.equal(other.failureLabel, 'Workspace origin does not match the running Aspire UI resource');
            assert.equal(other.runningUiAssemblySha256, undefined);

            // A new process can qualify in isolation with the same origin and file,
            // but it must never satisfy a recheck of the earlier capture.
            await stopFixture();
            const restarted = await startFixture(origin.port);
            assert.equal(restarted.base, base);
            await register(host.pid);
            const newHost = await captureWorkspaceFocusHost(root, origin);
            assert.equal(newHost.sha256, originalHost.sha256);
            assert.equal(newHost.device, originalHost.device);
            assert.equal(newHost.inode, originalHost.inode);
            assert.notEqual(newHost.pid, originalHost.pid);
            await assert.rejects(requireSameWorkspaceFocusHost(root, origin, originalHost), {
                safeLabel: 'The running Aspire UI changed during workspace validation',
            });

            await writeFile(assembly, 'same inode and different bytes');
            const rewritten = await invoke('rewritten');
            assert.equal(rewritten.failedPhase, 'host-identity');
            assert.equal(rewritten.failureLabel, 'The UI assembly was changed after the Aspire UI started');
            assert.equal(rewritten.runningUiAssemblySha256, undefined);

            const replacement = `${assembly}.replacement`;
            await writeFile(replacement, contents);
            await rename(replacement, assembly);
            const stale = await invoke('stale');
            assert.equal(stale.failedPhase, 'host-identity');
            assert.equal(stale.failureLabel, 'The running Aspire UI has a stale or different assembly');
            assert.equal(stale.runningUiAssemblySha256, undefined);
            assert.equal(host.exitCode, null);

            await assert.rejects(readFile(requests), { code: 'ENOENT' });
        } finally {
            if (originalPath === undefined) delete process.env.PATH;
            else process.env.PATH = originalPath;
            if (originalState === undefined) delete process.env.DW679_ASPIRE_STATE;
            else process.env.DW679_ASPIRE_STATE = originalState;
            await stopFixture();
            await rm(root, { recursive: true, force: true });
        }
    });
