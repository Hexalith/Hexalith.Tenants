import assert from 'node:assert/strict';
import { mkdtemp, mkdir, writeFile, rm } from 'node:fs/promises';
import { tmpdir } from 'node:os';
import { resolve } from 'node:path';
import test from 'node:test';
import { capturePublishedFrontComposer } from './published-frontcomposer-evidence.mjs';
import { assertStableCapture } from './authenticated-live-source-capture.mjs';

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

const scalarFields = ['sourceRevision', 'sourceDiffSha256', 'untrackedSourceSha256', 'validatorSha256',
    'uiBinarySha256', 'shellBinarySha256', 'frontComposerRestoreSha256', 'dependencyGitlinksSha256',
    'playwrightPackageSha256'];

async function independentCaptures() {
    const root = await mkdtemp(resolve(tmpdir(), 'tenants-stable-capture-'));
    try {
        const libraries = Object.fromEntries(['Contracts', 'Shell', 'Testing', 'SourceTools']
            .map(name => [`Hexalith.FrontComposer.${name}/6.0.0`, { type: 'package', sha512: `integrity-${name}` }]));
        for (const project of ['src/Hexalith.Tenants.UI', 'tests/Hexalith.Tenants.UI.Tests']) {
            await mkdir(resolve(root, project, 'obj'), { recursive: true });
            await writeFile(resolve(root, project, 'obj/project.assets.json'), JSON.stringify({ libraries }));
        }
        const capture = async () => ({
            sourceRevision: 'a'.repeat(40), sourceDiffSha256: '1'.repeat(64), untrackedSourceSha256: '2'.repeat(64),
            validatorSha256: '3'.repeat(64), uiBinarySha256: '4'.repeat(64), shellBinarySha256: '5'.repeat(64),
            ...await capturePublishedFrontComposer(root),
            dependencyGitlinksSha256: '6'.repeat(64), playwrightPackageSha256: '7'.repeat(64),
        });
        return [await capture(), await capture()];
    } finally { await rm(root, { recursive: true, force: true }); }
}

function refuses(before, after, field) {
    assert.throws(() => assertStableCapture(before, after), error => {
        assert.equal(error.safeLabel, `stable-capture-${field}`);
        assert.equal(error.message, '');
        assert.equal(error.cause, undefined);
        assert.ok(!String(error).includes('credential-sentinel'));
        return true;
    });
}

test('Live capture accepts independent complete repeated package/source captures', async () => {
    const [before, after] = await independentCaptures();
    assert.notEqual(before.frontComposerPackages, after.frontComposerPackages);
    assert.equal(before.frontComposerPackages.length, 4);
    assert.deepEqual(after, before);
    assert.doesNotThrow(() => assertStableCapture(before, after));
});

test('Live capture compares complete structured contents without depending on object property order', async () => {
    const [before, after] = await independentCaptures();
    before.additionalProvenance = { producer: { revision: 'actual', integrity: 'complete' }, rows: [{ id: 'closure', required: true }] };
    after.additionalProvenance = { rows: [{ required: true, id: 'closure' }], producer: { integrity: 'complete', revision: 'actual' } };
    assert.doesNotThrow(() => assertStableCapture(before, after));
});

for (const field of scalarFields) {
    for (const mutation of ['changed', 'missing-before', 'missing-after', 'missing-both', 'empty-both', 'undefined-both']) {
        test(`Live capture refuses ${mutation} scalar ${field}`, async () => {
            const [before, after] = await independentCaptures();
            if (mutation === 'changed') after[field] = 'credential-sentinel-changed';
            if (mutation === 'missing-before' || mutation === 'missing-both') delete before[field];
            if (mutation === 'missing-after' || mutation === 'missing-both') delete after[field];
            if (mutation === 'empty-both') before[field] = after[field] = '';
            if (mutation === 'undefined-both') before[field] = after[field] = undefined;
            refuses(before, after, field);
        });
    }
}

for (let row = 0; row < 4; row++) {
    for (const field of ['id', 'version', 'sha512']) {
        for (const mutation of ['changed', 'missing-before', 'missing-after', 'missing-both']) {
            test(`Live capture refuses ${mutation} package row ${row} ${field}`, async () => {
                const [before, after] = await independentCaptures();
                if (mutation === 'changed') after.frontComposerPackages[row][field] = 'credential-sentinel-changed';
                if (mutation === 'missing-before' || mutation === 'missing-both') delete before.frontComposerPackages[row][field];
                if (mutation === 'missing-after' || mutation === 'missing-both') delete after.frontComposerPackages[row][field];
                refuses(before, after, 'frontComposerPackages');
            });
        }
    }
    for (const side of ['before', 'after', 'both']) {
        test(`Live capture refuses missing package closure row ${row} in ${side}`, async () => {
            const [before, after] = await independentCaptures();
            if (side !== 'after') before.frontComposerPackages.splice(row, 1);
            if (side !== 'before') after.frontComposerPackages.splice(row, 1);
            refuses(before, after, 'frontComposerPackages');
        });
    }
}

for (const mutation of ['missing-before', 'missing-after', 'missing-both', 'empty-both', 'duplicate-both', 'extra-both', 'wrong-type', 'reordered']) {
    test(`Live capture refuses ${mutation} package closure`, async () => {
        const [before, after] = await independentCaptures();
        if (mutation === 'missing-before' || mutation === 'missing-both') delete before.frontComposerPackages;
        if (mutation === 'missing-after' || mutation === 'missing-both') delete after.frontComposerPackages;
        if (mutation === 'empty-both') before.frontComposerPackages = after.frontComposerPackages = [];
        if (mutation === 'duplicate-both') {
            before.frontComposerPackages[1] = { ...before.frontComposerPackages[0] };
            after.frontComposerPackages[1] = { ...after.frontComposerPackages[0] };
        }
        if (mutation === 'extra-both') {
            before.frontComposerPackages.push({ id: 'Unexpected', version: '1', sha512: 'other' });
            after.frontComposerPackages.push({ id: 'Unexpected', version: '1', sha512: 'other' });
        }
        if (mutation === 'wrong-type') after.frontComposerPackages = { ...after.frontComposerPackages };
        if (mutation === 'reordered') after.frontComposerPackages.reverse();
        refuses(before, after, 'frontComposerPackages');
    });
}

for (const mutation of ['changed', 'missing', 'extra-after', 'extra-before']) {
    test(`Live capture refuses complete structured extension ${mutation}`, async () => {
        const [before, after] = await independentCaptures();
        before.additionalProvenance = { producer: { revision: 'actual', integrity: 'complete' }, rows: [{ id: 'closure', required: true }] };
        after.additionalProvenance = structuredClone(before.additionalProvenance);
        if (mutation === 'changed') after.additionalProvenance.producer.integrity = 'credential-sentinel';
        if (mutation === 'missing') delete after.additionalProvenance.rows[0].required;
        if (mutation === 'extra-after') after.additionalProvenance.rows[0].extra = true;
        if (mutation === 'extra-before') before.additionalProvenance.producer.extra = true;
        refuses(before, after, 'additionalProvenance');
    });
}
