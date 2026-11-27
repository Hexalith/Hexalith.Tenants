import { execFile } from 'node:child_process';
import { createHash } from 'node:crypto';
import { constants } from 'node:fs';
import { open, readFile, readdir, realpath, stat } from 'node:fs/promises';
import { resolve } from 'node:path';
import { promisify } from 'node:util';

const run = promisify(execFile);
const assemblyName = 'Hexalith.Tenants.UI.dll';
const cliOptions = { encoding: 'utf8', timeout: 15_000, maxBuffer: 2 * 1024 * 1024 };

function requireIdentity(condition, safeLabel) {
    if (!condition) throw Object.assign(new Error(safeLabel), { safeLabel });
}

export function workspaceFocusOrigin(value) {
    const url = URL.parse(value);
    requireIdentity(url && ['http:', 'https:'].includes(url.protocol)
        && ['localhost', '127.0.0.1', '[::1]'].includes(url.hostname)
        && url.href === `${url.origin}/`, 'Use a loopback Aspire UI origin');
    return url;
}

async function registeredProcess(root, base) {
    const env = { ...process.env };
    delete env.TENANTS_BROWSER_USERNAME;
    delete env.TENANTS_BROWSER_PASSWORD;
    const appHost = resolve(root, 'src/Hexalith.Tenants.AppHost/Hexalith.Tenants.AppHost.csproj');
    const { stdout } = await run('aspire', ['describe', 'tenants-ui', '--apphost', appHost, '--format', 'Json',
        '--non-interactive', '--nologo'], { ...cliOptions, cwd: root, env });
    const projects = JSON.parse(stdout).resources.filter(resource =>
        (resource.displayName ?? resource.name) === 'tenants-ui' && resource.resourceType === 'Project');
    const project = projects[0];
    const pid = project?.properties?.['executable.pid'];
    const endpoints = project?.urls?.filter(endpoint => endpoint.name === base.protocol.slice(0, -1));
    requireIdentity(projects.length === 1 && project.state === 'Running'
        && endpoints?.length === 1 && URL.parse(endpoints[0].url)?.href === base.href
        && Number.isSafeInteger(pid) && pid > 0,
        'Workspace origin does not match the running Aspire UI resource');
    return pid;
}

async function processIdentity(pid) {
    const info = await readFile(`/proc/${pid}/stat`, 'utf8');
    // The parenthesized process name may contain spaces and parentheses.
    const fields = info.slice(info.lastIndexOf(')') + 2).split(' ');
    requireIdentity(fields[0] !== 'Z' && fields[0] !== 'X' && /^\d+$/.test(fields[1]) && /^\d+$/.test(fields[19]),
        'The registered Aspire UI process is unavailable');
    return { parentPid: Number(fields[1]), startTicks: BigInt(fields[19]) };
}

async function readLiveProcFile(path) {
    try {
        return await readFile(path, 'utf8');
    } catch (error) {
        if (error.code === 'ENOENT' || error.code === 'ESRCH') return '';
        throw error;
    }
}

async function assemblyMappings(pid) {
    const maps = await readLiveProcFile(`/proc/${pid}/maps`);
    return maps.split('\n')
        .map(line => /^\S+\s+\S+\s+\S+\s+([\da-f]+):([\da-f]+)\s+(\d+)\s+(.+)$/.exec(line))
        .filter(match => match?.[4].endsWith(`/${assemblyName}`) || match?.[4].endsWith(`/${assemblyName} (deleted)`));
}

async function mappedUiProcess(launcherPid) {
    // Aspire may report `dotnet run` instead of its Kestrel apphost child.
    // Linux lists children under the individual spawning thread.
    const tasks = `/proc/${launcherPid}/task`;
    const children = new Set((await Promise.all((await readdir(tasks)).map(thread =>
        readLiveProcFile(`${tasks}/${thread}/children`)))).join(' ').split(/\s+/).filter(Boolean).map(Number));
    const candidates = (await Promise.all([launcherPid, ...children].map(async pid => ({
        pid, mappings: await assemblyMappings(pid),
    })))).filter(candidate => candidate.mappings.length > 0);
    requireIdentity(candidates.length === 1, 'Unable to identify one running Aspire UI assembly process');
    return candidates[0];
}

function checkMappedAssembly(mappings, path, metadata) {
    // Linux dev_t splits major/minor numbers across non-contiguous bit fields.
    const major = ((metadata.dev >> 8n) & 0xfffn) | ((metadata.dev >> 32n) & 0xfffff000n);
    const minor = (metadata.dev & 0xffn) | ((metadata.dev >> 12n) & 0xffffff00n);
    requireIdentity(mappings.every(([, mappedMajor, mappedMinor, inode, file]) =>
        file === path && BigInt(inode) === metadata.ino
            && BigInt(`0x${mappedMajor}`) === major && BigInt(`0x${mappedMinor}`) === minor),
        'The running Aspire UI has a stale or different assembly');
}

export async function captureWorkspaceFocusHost(root, base) {
    try {
        requireIdentity(process.platform === 'linux', 'Workspace assembly verification requires Linux process mappings');
        const launcherPid = await registeredProcess(root, base);
        const launcherStartTicks = (await processIdentity(launcherPid)).startTicks;
        const { pid, mappings } = await mappedUiProcess(launcherPid);
        const { startTicks, parentPid } = await processIdentity(pid);
        requireIdentity(pid === launcherPid || parentPid === launcherPid, 'The running UI process left its registered launcher');
        const path = await realpath(resolve(root, 'src/Hexalith.Tenants.UI/bin/Debug/net11.0', assemblyName));
        const { stdout } = await run('getconf', ['CLK_TCK'], cliOptions);
        const ticksPerSecond = BigInt(stdout.trim());
        const beforeUptime = Date.now();
        const uptimeMs = Math.round(Number((await readFile('/proc/uptime', 'utf8')).split(' ')[0]) * 1_000);
        // /proc/uptime truncates to hundredths. Use a conservative lower bound
        // for launch so a same-inode rewrite cannot pass the mapping check.
        const earliestStartMs = BigInt(beforeUptime - uptimeMs - 20) + startTicks * 1_000n / ticksPerSecond;
        const assembly = await open(path, constants.O_RDONLY | constants.O_NOFOLLOW);
        try {
            const before = await assembly.stat({ bigint: true });
            requireIdentity(before.isFile(), 'The UI assembly is not a regular file');
            checkMappedAssembly(mappings, path, before);
            requireIdentity(before.ctimeNs <= earliestStartMs * 1_000_000n,
                'The UI assembly was changed after the Aspire UI started');
            const sha256 = createHash('sha256').update(await assembly.readFile()).digest('hex');
            const after = await stat(path, { bigint: true });
            const currentProcess = await processIdentity(pid);
            requireIdentity(before.dev === after.dev && before.ino === after.ino && before.ctimeNs === after.ctimeNs
                && currentProcess.startTicks === startTicks && currentProcess.parentPid === parentPid
                && (await processIdentity(launcherPid)).startTicks === launcherStartTicks,
                'Workspace host identity changed while measuring the assembly');
            return { launcherPid, launcherStartTicks, pid, startTicks, device: before.dev, inode: before.ino, sha256 };
        } finally {
            await assembly.close();
        }
    } catch (error) {
        if (error.safeLabel) throw error;
        // Aspire diagnostics and process paths are private to the local run.
        throw Object.assign(new Error(), { safeLabel: 'Unable to verify the running Aspire UI assembly' });
    }
}

export async function requireSameWorkspaceFocusHost(root, base, expected) {
    const current = await captureWorkspaceFocusHost(root, base);
    requireIdentity(Object.keys(expected).every(key => current[key] === expected[key]),
        'The running Aspire UI changed during workspace validation');
}
