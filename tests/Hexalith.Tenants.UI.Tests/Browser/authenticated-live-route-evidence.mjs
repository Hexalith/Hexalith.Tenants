import { rename, writeFile } from 'node:fs/promises';

// The locator stays private; return only fixed categories and bounded state about
// the exact expected launcher, never selector, destination, DOM text, or identity.
export function captureLauncherFocusState(launcher) {
    return launcher.evaluateAll(elements => {
        let active = document.activeElement;
        while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
        const ownsFocus = element => {
            for (let current = active; current; current = current.parentElement || current.getRootNode()?.host)
                if (current === element) return true;
            return false;
        };
        const expected = elements.length === 1 ? elements[0] : null;
        const visible = expected && getComputedStyle(expected).visibility !== 'hidden'
            && getComputedStyle(expected).visibility !== 'collapse'
            && [...expected.getClientRects()].some(bounds => bounds.width > 0 && bounds.height > 0);
        const tag = active?.localName;
        const role = active?.getAttribute('role');
        const activeElementCategory = tag === 'body' ? 'body'
            : /^h[1-6]$/.test(tag) || role === 'heading' ? 'heading'
            : tag === 'button' || role === 'button' ? 'button'
            : ['input', 'select', 'textarea'].includes(tag) ? 'input'
            : tag === 'a' || role === 'link' ? 'link' : 'other';
        return {
            available: true, launcherCount: Math.min(elements.length, 100),
            launcherAttached: elements.length > 0, launcherVisible: Boolean(visible),
            launcherEnabled: Boolean(expected && !expected.matches(':disabled') && !expected.hasAttribute('disabled')
                && expected.getAttribute('aria-disabled') !== 'true'),
            launcherHasDestination: Boolean(expected?.getAttribute('href')),
            expectedFocus: Boolean(expected && ownsFocus(expected)),
            detailSurfacePresent: Boolean(document.querySelector('[data-testid="tenants-detail"]')),
            detailCurrentState: Boolean(document.querySelector('[data-testid="tenants-detail-truth-state"].truth-state-badge--current')),
            activeElementCategory,
        };
    });
}

// Persist before each potentially blocking phase. Atomic replacement preserves the last
// complete support-safe snapshot even when the enclosing process is killed during a write.
export function createLiveRouteCheckpoint(output, evidence, now = () => performance.now()) {
    const started = now();
    return async phase => {
        if (!/^[a-z0-9-]{1,96}$/.test(phase)) throw new Error('invalid-evidence-phase');
        evidence.progress = { phase, completedCases: evidence.cases.length,
            elapsedMilliseconds: Math.max(0, Math.round(now() - started)) };
        if (!output) return;
        const temporary = `${output}.${process.pid}.pending`;
        await writeFile(temporary, JSON.stringify(evidence, null, 2) + '\n', { mode: 0o600 });
        await rename(temporary, output);
    };
}
