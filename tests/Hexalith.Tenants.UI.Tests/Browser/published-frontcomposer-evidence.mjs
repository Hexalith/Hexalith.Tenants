import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { resolve } from 'node:path';

// Bind the actual restored package closure. A dormant source checkout cannot
// establish the identity of the Shell binary exercised by a package-mode UI.
export async function capturePublishedFrontComposer(root) {
    const expected = new Set(['Hexalith.FrontComposer.Contracts', 'Hexalith.FrontComposer.Shell',
        'Hexalith.FrontComposer.Testing', 'Hexalith.FrontComposer.SourceTools']);
    const packages = new Map();
    const restore = createHash('sha256');
    for (const project of ['src/Hexalith.Tenants.UI', 'tests/Hexalith.Tenants.UI.Tests']) {
        const bytes = await readFile(resolve(root, project, 'obj/project.assets.json'));
        restore.update(project).update('\0').update(bytes).update('\0');
        const assets = JSON.parse(bytes);
        for (const [identity, metadata] of Object.entries(assets.libraries)) {
            const separator = identity.lastIndexOf('/');
            const id = identity.slice(0, separator);
            if (!expected.has(id)) continue;
            const value = { id, version: identity.slice(separator + 1), sha512: metadata.sha512 };
            if (metadata.type !== 'package' || !value.version || typeof value.sha512 !== 'string' || !value.sha512
                || packages.has(id) && JSON.stringify(packages.get(id)) !== JSON.stringify(value)) {
                throw Object.assign(new Error(), { safeLabel: 'frontcomposer-published-package-identity-invalid' });
            }
            packages.set(id, value);
        }
    }
    if (packages.size !== expected.size) {
        throw Object.assign(new Error(), { safeLabel: 'frontcomposer-published-package-closure-incomplete' });
    }
    return { frontComposerPackages: [...packages.values()].sort((left, right) => left.id.localeCompare(right.id)),
        frontComposerRestoreSha256: restore.digest('hex') };
}
