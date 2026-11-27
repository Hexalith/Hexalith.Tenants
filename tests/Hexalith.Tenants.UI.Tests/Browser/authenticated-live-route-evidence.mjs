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

export const DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS = 5_000;
const MAX_TIMER_DELAY_MS = 2_147_483_647;

function resolveDiagnosticTimeoutMs(options) {
    const raw = options && typeof options === 'object' ? options.timeoutMs : undefined;
    return typeof raw === 'number' && Number.isFinite(raw) && raw > 0 && raw <= MAX_TIMER_DELAY_MS
        ? raw
        : DEFAULT_FAILURE_DIAGNOSTIC_TIMEOUT_MS;
}

function isValidDiagnosticObject(value) {
    return Boolean(
        value
        && typeof value === 'object'
        && !Array.isArray(value)
        && !(value instanceof Error)
        && Object.prototype.toString.call(value) !== '[object Error]',
    );
}

// Failure diagnostics run after an assertion or timeout already failed. Bound every capture
// with a finite deadline, clear its timer on settlement, and absorb any late rejection so
// a wedged renderer cannot hang exit or trigger an unhandledRejection.
export function captureFailureDiagnostic(capture, options = {}) {
    const resolvedOptions = options && typeof options === 'object' ? options : {};
    const timeoutMs = resolveDiagnosticTimeoutMs(resolvedOptions);
    const setTimer = typeof resolvedOptions.setTimer === 'function' ? resolvedOptions.setTimer : setTimeout;
    const clearTimer = typeof resolvedOptions.clearTimer === 'function' ? resolvedOptions.clearTimer : clearTimeout;

    let capturePromise;
    try {
        capturePromise = Promise.resolve(capture());
    } catch {
        return Promise.resolve({ available: false });
    }

    const safeCapture = capturePromise.then(
        value => (isValidDiagnosticObject(value) ? value : { available: false }),
        () => ({ available: false }),
    );

    return new Promise(resolve => {
        let settled = false;
        let timerId;
        let hasTimer = false;
        const finish = value => {
            if (settled) return;
            settled = true;
            if (hasTimer) {
                hasTimer = false;
                try { clearTimer(timerId); } catch { /* Best-effort timer cleanup. */ }
            }
            resolve(value);
        };

        let createdTimerId;
        try {
            createdTimerId = setTimer(() => {
                hasTimer = false;
                finish({ available: false });
            }, timeoutMs);
        } catch {
            finish({ available: false });
            return;
        }

        if (!settled) {
            timerId = createdTimerId;
            hasTimer = true;
        }

        safeCapture.then(finish);
    });
}

// Bound failure-path screenshots to the diagnostic deadline and swallow capture errors so
// screenshot failures never hang or clobber the primary failure label.
export function captureFailureScreenshot(page, path, diagnosticOptions) {
    const timeoutMs = resolveDiagnosticTimeoutMs(diagnosticOptions);
    return captureFailureDiagnostic(
        async () => {
            await page.screenshot({ path, timeout: timeoutMs });
            return { available: true };
        },
        diagnosticOptions,
    );
}

// Record optional focus and readiness diagnostics in the outer failure handler without
// allowing a wedged renderer to block checkpoint persistence or exit.
export async function recordFailureDiagnostics(
    evidence,
    { phase, checkpoint, captureFocus, captureReadiness, diagnosticOptions } = {},
) {
    if (captureFocus) {
        evidence.focus = await captureFailureDiagnostic(captureFocus, diagnosticOptions);
        if (checkpoint) await checkpoint(phase);
    }
    if (captureReadiness) {
        evidence.readiness = await captureFailureDiagnostic(captureReadiness, diagnosticOptions);
    }
}
