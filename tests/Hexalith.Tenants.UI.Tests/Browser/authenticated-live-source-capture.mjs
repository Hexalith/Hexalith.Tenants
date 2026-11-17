import { isDeepStrictEqual } from 'node:util';

const scalarFields = ['sourceRevision', 'sourceDiffSha256', 'untrackedSourceSha256', 'validatorSha256',
    'uiBinarySha256', 'shellBinarySha256', 'frontComposerRestoreSha256', 'dependencyGitlinksSha256',
    'playwrightPackageSha256'];
const packageIds = new Set(['Hexalith.FrontComposer.Contracts', 'Hexalith.FrontComposer.Shell',
    'Hexalith.FrontComposer.Testing', 'Hexalith.FrontComposer.SourceTools']);

function requireStable(condition, key) {
    if (!condition) {
        const field = /^[A-Za-z][A-Za-z0-9]*$/.test(key) ? key : 'fields';
        // Keep private captured values out of errors and retained browser diagnostics.
        throw Object.assign(new Error(), { safeLabel: `stable-capture-${field}` });
    }
}

// Compare complete provenance values, including newly allocated arrays/objects.
export function assertStableCapture(before, after) {
    for (const capture of [before, after]) {
        for (const field of scalarFields) {
            requireStable(capture != null && Object.hasOwn(capture, field)
                && typeof capture[field] === 'string' && capture[field].length > 0, field);
        }
        const rows = capture.frontComposerPackages;
        requireStable(Array.isArray(rows) && rows.length === packageIds.size
            && rows.every(row => row != null && ['id', 'version', 'sha512'].every(field => Object.hasOwn(row, field)
                && typeof row[field] === 'string' && row[field].length > 0) && packageIds.has(row.id))
            && new Set(rows.map(row => row.id)).size === packageIds.size, 'frontComposerPackages');
    }
    for (const key of new Set([...Object.keys(before), ...Object.keys(after)])) {
        requireStable(Object.hasOwn(before, key) && Object.hasOwn(after, key)
            && isDeepStrictEqual(after[key], before[key]), key);
    }
}
