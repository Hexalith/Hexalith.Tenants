import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import test from 'node:test';
import { capturePublishedFrontComposer } from './published-frontcomposer-evidence.mjs';

for (const mutation of ['none', 'source', 'missing-testing', 'different-producer', 'missing-integrity']) {
    test(`Published UI evidence binds all four package inputs and refuses ${mutation}`, async () => {
        const root = await mkdtemp(resolve(tmpdir(), 'tenants-published-browser-'));
        try {
            const definitions = ['Contracts', 'Shell', 'Testing', 'SourceTools'];
            for (const [index, project] of ['src/Hexalith.Tenants.UI', 'tests/Hexalith.Tenants.UI.Tests'].entries()) {
                const libraries = Object.fromEntries(definitions.filter(name => mutation !== 'missing-testing' || name !== 'Testing')
                    .map(name => [`Hexalith.FrontComposer.${name}/${mutation === 'different-producer' && index === 1 ? '6.0.1' : '6.0.0'}`,
                        { type: mutation === 'source' ? 'project' : 'package', sha512: mutation === 'missing-integrity' ? undefined : `integrity-${name}` }]));
                await mkdir(resolve(root, project, 'obj'), { recursive: true });
                await writeFile(resolve(root, project, 'obj/project.assets.json'), JSON.stringify({ libraries }));
            }
            if (mutation === 'none') {
                const result = await capturePublishedFrontComposer(root);
                assert.equal(result.frontComposerPackages.length, 4);
                assert.match(result.frontComposerRestoreSha256, /^[0-9a-f]{64}$/);
                assert.ok(result.frontComposerPackages.every(item => item.version === '6.0.0'));
            } else await assert.rejects(capturePublishedFrontComposer(root));
        } finally { await rm(root, { recursive: true, force: true }); }
    });
}
