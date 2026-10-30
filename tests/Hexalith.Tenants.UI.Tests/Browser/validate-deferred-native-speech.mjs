// Native-only, opt-in orchestration. Speech comes exclusively from the separate
// NVDA-owned Speech Viewer collector; these browser observations are not speech.
import assert from 'node:assert/strict';
import { createInterface } from 'node:readline';

const acknowledgementMilliseconds = 90_000;
const trialPattern = /^(S[0-3]|C[12]-(lifecycle|availability)|C3-(events|unavailable)-(lifecycle|availability))-(en|fr)$/;

export function resolveNativeSpeechSelection(environment = process.env) {
    const group = environment.TENANTS_NATIVE_SPEECH_GROUP ?? 'all';
    const cultures = (environment.TENANTS_NATIVE_SPEECH_CULTURES ?? 'en,fr').split(',');
    const rawTrials = environment.TENANTS_NATIVE_SPEECH_TRIALS ?? '';
    const trials = rawTrials === '' ? [] : rawTrials.split(',');
    assert.ok(['all', 'search', 'create'].includes(group), 'Unknown native trial group');
    assert.ok(cultures.length > 0 && cultures.every(culture => ['en', 'fr'].includes(culture)), 'Invalid native cultures');
    assert.equal(new Set(cultures).size, cultures.length, 'Duplicate native cultures');
    assert.ok(trials.every(trial => trialPattern.test(trial)), 'Invalid native trial');
    assert.equal(new Set(trials).size, trials.length, 'Duplicate native trials');
    for (const trial of trials) {
        assert.ok(cultures.includes(trial.slice(-2)), `Requested trial culture is excluded: ${trial}`);
        assert.ok(group === 'all' || (group === 'search') === trial.startsWith('S'), `Requested trial is excluded by group: ${trial}`);
    }
    const required = new Set(trials);
    if (trials.length === 0) {
        for (const culture of cultures) {
            if (group !== 'create') for (const item of ['S0', 'S1', 'S2', 'S3']) required.add(`${item}-${culture}`);
            if (group !== 'search') for (const status of ['events', 'unavailable']) for (const refresh of ['lifecycle', 'availability']) {
                required.add(`${status === 'events' ? 'C1' : 'C2'}-${refresh}-${culture}`);
                required.add(`C3-${status}-${refresh}-${culture}`);
            }
        }
    }
    for (const trial of [...required]) {
        const pair = /^C3-(events|unavailable)-(lifecycle|availability)-(en|fr)$/.exec(trial);
        if (pair) required.add(`${pair[1] === 'events' ? 'C1' : 'C2'}-${pair[2]}-${pair[3]}`);
    }
    return { group, cultures, requiredTrials: [...required].sort() };
}

export function requireCompletedNativeTrials(required, completed) {
    assert.ok(required.length > 0, 'No required native trials');
    assert.equal(new Set(completed).size, completed.length, 'Duplicate completed native trials');
    assert.deepEqual([...completed].sort(), [...required].sort(), 'Requested native trial coverage is incomplete');
}

export async function awaitNativeAcknowledgement(pending, milliseconds = acknowledgementMilliseconds) {
    let timer;
    try {
        return await Promise.race([pending, new Promise((_, reject) => {
            timer = setTimeout(() => reject(new Error('Native collector acknowledgement timed out')), milliseconds);
        })]);
    } finally { clearTimeout(timer); }
}

export async function runDeferredNativeSpeech({ fixture, newSession, enterWorkspace, evidence, wait }) {
    assert.equal(process.platform, 'win32', 'Native speech qualification requires Windows');
    assert.equal(process.env.TENANTS_NATIVE_SPEECH_CAPTURE, '1', 'Use capture-deferred-nvda-speech.ps1');
    const { group, cultures, requiredTrials } = resolveNativeSpeechSelection();
    const wants = trial => requiredTrials.includes(trial);
    const completedTrials = [];
    const reader = createInterface({ input: process.stdin, crlfDelay: Infinity });
    const input = reader[Symbol.asyncIterator]();
    let sequence = 0;
    const exchange = async event => {
        const request = { nativeSpeech: true, sequence: ++sequence, utc: new Date().toISOString(), ...event };
        console.log(JSON.stringify(request));
        const response = await awaitNativeAcknowledgement(input.next());
        assert.equal(response.done, false, 'Native collector closed before acknowledgement');
        const reply = JSON.parse(response.value);
        assert.equal(reply.sequence, sequence, 'Native collector acknowledgement mismatch');
        assert.equal(reply.ok, true, 'Native collector could not retain a valid observation');
        return reply;
    };
    try {
    await exchange({ kind: 'plan', requiredTrials });
    let page;
    const id = value => page.getByTestId(value);
    const focus = () => page.evaluate(() => {
        let node = document.activeElement;
        while (node?.shadowRoot?.activeElement) node = node.shadowRoot.activeElement;
        const ancestry = [];
        for (let item = node; item; item = item.parentElement || item.getRootNode()?.host)
            if (item.getAttribute?.('data-testid')) ancestry.push(item.getAttribute('data-testid'));
        return { documentHasFocus: document.hasFocus(), element: node?.localName, ancestry };
    });
    const tabTo = async locator => {
        assert.equal(await locator.count(), 1, 'The selected keyboard target must be unique');
        await wait(() => locator.evaluate(node => !node.hasAttribute('disabled')), 'Native keyboard target enabled');
        for (let count = 0; count < 100; count++) {
            if (await locator.evaluate(node => {
                let active = document.activeElement;
                while (active?.shadowRoot?.activeElement) active = active.shadowRoot.activeElement;
                return node === active || node.contains(active) || node.shadowRoot?.contains(active);
            })) return;
            await page.keyboard.press('Tab');
        }
        assert.fail('Selected control was not reached by keyboard');
    };
    const session = async (culture, blankEntry = false) => {
        page = await newSession(culture, { deferWorkspaceEntry: blankEntry });
        await page.evaluate(() => { document.title = 'Tenants native speech fixture'; });
        await page.bringToFront();
        await exchange({ kind: 'session', culture, entryDocument: blankEntry ? 'authenticated-blank' : 'workspace' });
    };
    const observe = async (trial, culture, action, state) => {
        await exchange({ kind: 'arm', trial, culture, focus: await focus() });
        await exchange({ kind: 'quiet', trial });
        await exchange({ kind: 'trigger', trial, focus: await focus() });
        await action();
        await state();
        const result = await exchange({ kind: 'observe', trial, stateUtc: new Date().toISOString(), focus: await focus(), fixture: fixture.snapshot() });
        // A product focus announcement after the window is an omission control.
        // It is retained separately and cannot satisfy the live-announcement gate.
        await exchange({ kind: 'control-start', trial, culture });
        await id('tenants-list-search').locator('input').focus();
        await page.keyboard.press('Tab');
        await page.keyboard.press('Shift+Tab');
        const control = await exchange({ kind: 'control-end', trial, culture, focus: await focus() });
        assert.equal(control.control?.verified, true, 'Search focus/speech control was not verified');
        assert.equal(control.interruption?.verified, true, 'NVDA cancellation response remains unverified');
        evidence.nativeSpeechTrials ??= [];
        evidence.nativeSpeechTrials.push({ trial, culture, observation: result.observation, control: control.control, interruption: control.interruption,
            focus: await focus(), fixture: fixture.snapshot() });
        completedTrials.push(trial);
    };
    const search = async () => {
        await id('tenants-list-search').locator('input').fill('needle');
        await page.keyboard.press('Tab');
        await page.waitForURL(url => url.searchParams.get('search') === 'needle');
        await page.waitForLoadState('networkidle');
    };
    const availability = pageState => page.locator(`[data-testid="tenants-create-audit"] [data-testid="tenants-audit-availability"][data-state="${pageState}"]`);
    for (const culture of cultures) {
        if (group !== 'create') {
            for (const searchCase of ['S0', 'S1', 'S2', 'S3']) {
                const trial = `${searchCase}-${culture}`;
                if (!wants(trial)) continue;
                await session(culture, searchCase === 'S0');
                if (searchCase === 'S0') {
                    assert.equal(page.url(), 'about:blank', 'S0 must arm on an authenticated blank document');
                    await observe(trial, culture, async () => {
                        await enterWorkspace(page);
                        assert.equal(await id('tenants-list-notices').locator('[data-testid$="-notice"]').count(), 0);
                        await search();
                    }, async () => {
                        assert.equal(await id('tenants-list-notices').locator('[data-testid$="-notice"]').count(), 0);
                    });
                } else if (searchCase === 'S1') {
                    fixture.apply('search down');
                    await observe(trial, culture, search, () => id('tenants-list-search-unavailable-notice').waitFor());
                } else if (searchCase === 'S2') {
                    await search();
                    await id('tenants-list-next').click();
                    await wait(() => id('tenants-list-previous').evaluate(node => !node.hasAttribute('disabled')), 'Protected page two');
                    await id('tenants-list-detail-link').first().click();
                    await id('tenants-detail-back').waitFor();
                    await observe(trial, culture, () => page.goBack(), () => id('tenants-list-search-refreshed-notice').waitFor());
                } else {
                    fixture.apply('search down');
                    await search();
                    await id('tenants-list-search-unavailable-notice').waitFor();
                    fixture.apply('cursor reject-once');
                    await tabTo(id('tenants-list-next'));
                    await observe(trial, culture, () => page.keyboard.press('Enter'), async () => {
                        await id('tenants-list-refreshed-notice').waitFor();
                        assert.equal(fixture.snapshot().rejectedCursors, 1);
                        assert.equal(fixture.snapshot().recoveryPages, 1);
                        assert.equal(fixture.snapshot().rejectCursor, false);
                    });
                    await id('tenants-list-next').click();
                    await wait(() => fixture.snapshot().continuations === 1, 'Subsequent continuation permitted');
                    assert.equal(fixture.snapshot().rejectedCursors, 1);
                }
            }
        }
        if (group === 'search') continue;

        for (const status of ['events', 'unavailable']) {
            for (const refresh of ['lifecycle', 'availability']) {
                const trial = `${status === 'events' ? 'C1' : 'C2'}-${refresh}-${culture}`;
                const refreshTrial = `C3-${status}-${refresh}-${culture}`;
                if (!wants(trial) && !wants(refreshTrial)) continue;
                await session(culture);
                fixture.apply(`status ${status}`);
                fixture.apply('projection hold');
                await id('tenants-create-accordion').getByRole('button').click();
                assert.equal(await page.locator('[data-testid="tenants-create-audit"] [data-testid="tenants-audit-availability"]').count(), 0);
                const initialHosts = await page.locator('[data-testid^="tenants-create-audit-live-"]').elementHandles();
                assert.equal(initialHosts.length, 2);
                for (const host of initialHosts) assert.equal((await host.textContent()).trim(), '');
                await id('tenants-create-tenant-id').locator('input').fill(`tenant.speech.${culture}-${status}-${refresh}`);
                await id('tenants-create-name').locator('input').fill('Speech fixture tenant');
                await page.keyboard.press('Tab');
                await tabTo(id('tenants-create-submit'));
                const expected = status === 'events' ? 'pending' : 'unavailable';
                await observe(trial, culture, () => page.keyboard.press('Enter'), async () => {
                    await availability(expected).waitFor();
                    for (const host of initialHosts) assert.equal(await host.evaluate(node => node.isConnected), true, 'Audit host survives first insertion');
                    assert.equal((await page.evaluate(() => window.__handoffStates))[0]?.state, expected);
                    assert.equal(fixture.snapshot().projection, 'hold');
                    assert.equal(fixture.snapshot().simulatedCommands, 1);
                });
                if (!wants(refreshTrial)) continue;
                const button = refresh === 'lifecycle' ? id('tenants-create-refresh')
                    : page.locator('[data-testid="tenants-create-audit"] [data-testid="tenants-audit-availability-recovery-refresh"]');
                await tabTo(button);
                let prior;
                await observe(refreshTrial, culture, async () => {
                    prior = fixture.snapshot().statusReads;
                    await page.keyboard.press('Enter');
                }, async () => {
                    await wait(() => fixture.snapshot().statusReads > prior, 'Selected Refresh performs a status read');
                    await wait(() => button.evaluate(node => !node.hasAttribute('disabled')), 'Selected Refresh completes');
                    await availability(expected).waitFor();
                    assert.equal(fixture.snapshot().simulatedCommands, 1);
                    assert.equal(fixture.snapshot().unexpectedCommands, 0);
                    assert.equal(fixture.snapshot().projection, 'hold');
                });
            }
        }
    }
    requireCompletedNativeTrials(requiredTrials, completedTrials);
    await exchange({ kind: 'complete', completedTrials });
    evidence.fixtureScope = 'Native authenticated disposable production UI; actual Speech Viewer output is retained separately and requires independent assessment';
    } finally { reader.close(); }
}
