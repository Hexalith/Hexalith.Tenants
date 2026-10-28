// Use the locked Semantic Release proposal semantics without its push-authentication lifecycle.
import { execFileSync } from 'node:child_process';
import { createRequire } from 'node:module';
import { pathToFileURL } from 'node:url';

const require = createRequire(import.meta.url);
const releaseEntry = pathToFileURL(require.resolve('semantic-release'));
const releaseRequire = createRequire(releaseEntry);
const semver = releaseRequire('semver');
const load = async name => (await import(new URL(`./lib/${name}.js`, releaseEntry))).default;
const getConfig = await load('get-config');
const getLastRelease = await load('get-last-release');
const getCommits = await load('get-commits');
const getNextVersion = await load('get-next-version');
const getLogger = await load('get-logger');
const { getGitHead, getTagHead } = await import(new URL('./lib/git.js', releaseEntry));
const cwd = process.cwd();
const env = process.env;
const logger = getLogger({ stdout: process.stderr, stderr: process.stderr });
const context = { cwd, env, logger, stdout: process.stderr, stderr: process.stderr };
const { options, plugins } = await getConfig(context, { dryRun: true });
if (options.branches.length !== 1 || options.branches[0] !== 'main' || options.tagFormat !== 'v${version}') {
    throw new Error('Read-only release proposals require only stable main and the v${version} tag format.');
}
const branchName = execFileSync('git', ['symbolic-ref', '--quiet', '--short', 'HEAD'], { cwd, env, encoding: 'utf8' }).trim();
if (branchName !== 'main') {
    throw new Error('Read-only release proposals must run from main.');
}
if (execFileSync('git', ['rev-parse', '--is-shallow-repository'], { cwd, env, encoding: 'utf8' }).trim() !== 'false') {
    throw new Error('Read-only release proposals require complete Git history.');
}
const tags = execFileSync('git', ['tag', '--merged', 'HEAD'], { cwd, env, encoding: 'utf8' })
    .split('\n')
    .filter(tag => /^v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)$/.test(tag) && semver.valid(tag.slice(1)))
    .map(gitTag => ({ gitTag, version: gitTag.slice(1), channels: [null] }));
Object.assign(context, { options, branch: { name: 'main', type: 'release', tags }, releases: [] });
context.lastRelease = getLastRelease(context);
if (context.lastRelease.gitHead) {
    context.lastRelease.gitHead = await getTagHead(context.lastRelease.gitHead, { cwd, env });
}
context.commits = await getCommits(context);
const type = await plugins.analyzeCommits(context);
if (!type) {
    process.stdout.write('none\n');
} else {
    context.nextRelease = { type, channel: null, gitHead: await getGitHead({ cwd, env }) };
    const version = getNextVersion(context);
    if (!semver.valid(version)) {
        throw new Error('Semantic Release did not produce a valid stable version.');
    }
    process.stdout.write(`${version}\n`);
}
