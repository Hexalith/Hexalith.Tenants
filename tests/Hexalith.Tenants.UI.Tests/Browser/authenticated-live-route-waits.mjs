// Functional qualification encloses the finite page/read flow. Performance limits live in their independent suite.
export const functionalWaitMilliseconds = 150_000;

export function createFunctionalNavigation(page, check, {
    now = () => performance.now(), setTimer = setTimeout, clearTimer = clearTimeout,
} = {}) {
    page.setDefaultTimeout(functionalWaitMilliseconds);
    page.setDefaultNavigationTimeout(functionalWaitMilliseconds);
    const fail = label => { check(false, label); throw new Error(label); };
    const remaining = (deadline, label) => {
        const milliseconds = deadline - now();
        if (milliseconds <= 0) fail(label);
        return milliseconds;
    };
    async function bounded(action, deadline, label) {
        const milliseconds = remaining(deadline, label);
        let timer;
        const expired = new Promise((_, reject) => {
            timer = setTimer(() => {
                try { fail(label); } catch (error) { reject(error); }
            }, milliseconds);
        });
        try {
            const result = await Promise.race([action(milliseconds), expired]);
            remaining(deadline, label); // A late success is still a deadline failure.
            return result;
        } finally {
            clearTimer(timer);
        }
    }
    return {
        async open(destination) {
            const deadline = now() + functionalWaitMilliseconds;
            const label = 'functional-navigation-deadline';
            const response = await bounded(timeout => page.goto(destination, { timeout }), deadline, label);
            // Prerender can precede the interactive circuit and Fluent modules.
            await bounded(timeout => page.waitForLoadState('networkidle', { timeout }), deadline, label);
            return response;
        },
        async waitUntil(condition, label) {
            const deadline = now() + functionalWaitMilliseconds;
            while (true) {
                if (await bounded(condition, deadline, label)) return;
                await bounded(timeout => page.waitForTimeout(Math.min(100, timeout)), deadline, label);
            }
        },
    };
}
