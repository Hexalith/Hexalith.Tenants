import assert from 'node:assert/strict';
import { createInterface } from 'node:readline';

const availability = '[data-testid="tenants-create-audit"] [data-testid="tenants-audit-availability"]';
const lifecycleRefresh = '[data-testid="tenants-create-flow"] [data-testid="tenants-create-refresh"]';
const auditRefresh = '[data-testid="tenants-create-audit"] [data-testid="tenants-audit-availability-recovery-refresh"]';
const help = 'session en|fr; search up|down; cursor reject-once; status wait|events|unavailable; projection hold|deliver; show; help; quit';

export async function runDeferredHandoff({ manual, fixture, newSession, evidence, wait }) {
    let page;
    let currentCulture;
    let sessionStartedUtc;
    const sessions = evidence.handoffSessions = [];
    const capture = async () => {
        if (page && !page.isClosed()) sessions.push({ culture: currentCulture, startedUtc: sessionStartedUtc, finishedUtc: new Date().toISOString(), fixture: fixture.snapshot(),
            states: await page.evaluate(() => window.__handoffStates || []).catch(() => []) });
    };
    const session = async culture => {
        assert.ok(['en', 'fr'].includes(culture), 'Use session en or session fr');
        await capture();
        sessionStartedUtc = new Date().toISOString();
        page = await newSession(culture);
        currentCulture = culture;
        if (manual) console.log(`Ready: ${culture}; ${page.url().split('?')[0]}; ${help}`);
        return page;
    };
    if (manual) {
        await session('en');
        console.log('Start Speech Viewer before opening the surface. Terminal changes do not activate UI Refresh. Browser evidence is not speech evidence.');
        const input = createInterface({ input: process.stdin, crlfDelay: Infinity });
        const stop = () => input.close();
        process.once('SIGINT', stop);
        process.once('SIGTERM', stop);
        try {
            for await (const line of input) {
                try {
                    const command = line.trim();
                    if (command === 'quit') break;
                    if (command === 'help') { console.log(help); continue; }
                    if (command.startsWith('session ')) { await session(command.slice(8)); continue; }
                    console.log(JSON.stringify({ utc: new Date().toISOString(), ...fixture.apply(command) }));
                } catch (error) { console.log(error.message); }
            }
        } finally {
            input.close();
            process.removeListener('SIGINT', stop);
            process.removeListener('SIGTERM', stop);
            await capture();
        }
        evidence.manualSessionRecorded = true;
        evidence.fixtureScope = 'Manual authenticated fixture session; speech must be captured and independently accepted separately';
        return;
    }
    const record = name => evidence.cases.push({ name, passed: true });
    const id = value => page.getByTestId(value);
    const notices = () => page.locator('[data-testid="tenants-list-notices"] [data-testid$="-notice"]');
    const openSearch = async () => {
        await id('tenants-list-search').locator('input').fill('needle');
        await page.keyboard.press('Tab');
        await page.waitForURL(url => url.searchParams.get('search') === 'needle');
        await wait(async () => await id('tenants-list-tenant-id').count() === 20, 'Debounced protected search');
        await page.waitForLoadState('networkidle');
    };
    for (const culture of ['en', 'fr']) {
        await session(culture);
        assert.equal(await notices().count(), 0);
        await openSearch();
        assert.equal(await notices().count(), 0);
        record(`${culture}: S0 empty notice region and protected search`);
        // S2: leave a later server-held search page through normal UI navigation, then Back.
        await id('tenants-list-next').click();
        await id('tenants-list-previous').waitFor();
        await wait(() => id('tenants-list-previous').evaluate(element => !element.hasAttribute('disabled')), 'Search page two');
        await id('tenants-list-detail-link').first().click();
        await id('tenants-detail-back').waitFor();
        await page.goBack();
        await id('tenants-list-search-refreshed-notice').waitFor();
        record(`${culture}: S2 protected search restarts after Back`);
        fixture.apply('search down');
        await id('tenants-list-refresh').click();
        await id('tenants-list-search-unavailable-notice').waitFor();
        // A source-mode transition includes an extra restart notice; a stable fallback Refresh removes it.
        await id('tenants-list-refresh').click();
        await wait(async () => await notices().count() === 1, 'Stable fallback');
        const text = await id('tenants-list-search-unavailable-notice').innerText();
        assert.ok(text.includes(culture === 'fr' ? 'recherche protégée' : 'Protected whole-set search'));
        record(`${culture}: S1 unavailable search with authorized fallback`);
        assert.throws(() => { fixture.apply('search up'); fixture.apply('cursor reject-once'); });
        fixture.apply('search down');
        fixture.apply('cursor reject-once');
        await id('tenants-list-refresh').click();
        await page.waitForLoadState('networkidle');
        assert.equal(fixture.snapshot().rejectCursor, true, 'Cursorless Refresh cannot consume the rejection');
        await id('tenants-list-next').click();
        await id('tenants-list-refreshed-notice').waitFor();
        assert.deepEqual((await notices().evaluateAll(elements => elements.map(element => element.dataset.testid))).sort(),
            ['tenants-list-refreshed-notice', 'tenants-list-search-unavailable-notice']);
        assert.equal(fixture.snapshot().rejectedCursors, 1);
        assert.equal(fixture.snapshot().recoveryPages, 1);
        assert.equal(fixture.snapshot().rejectCursor, false);
        assert.ok((await id('tenants-list-tenant-id').allTextContents()).some(text => text.includes('tenant.alpha')));
        record(`${culture}: S3 exactly one rejected continuation and authorized cursorless recovery`);
        await id('tenants-list-next').click();
        await wait(() => id('tenants-list-next').evaluate(element => element.hasAttribute('disabled')), 'Subsequent continuation succeeds');
        assert.equal(fixture.snapshot().rejectedCursors, 1);
        assert.equal(fixture.snapshot().continuations, 1);
        record(`${culture}: S3 subsequent continuation permitted`);

        for (const status of ['events', 'unavailable']) {
            // Separate first-insertion trials also prevent retry-bound exhaustion.
            for (const refresh of [lifecycleRefresh, auditRefresh]) {
                await session(culture);
                fixture.apply(`status ${status}`);
                assert.equal(fixture.snapshot().projection, 'hold');
                await id('tenants-create-accordion').getByRole('button').click();
                assert.equal(await page.locator(availability).count(), 0);
                await id('tenants-create-tenant-id').locator('input').fill(`tenant.speech.${culture}-${status}`);
                await id('tenants-create-name').locator('input').fill('Speech fixture tenant');
                await page.keyboard.press('Tab');
                await wait(() => id('tenants-create-submit').evaluate(element => !element.hasAttribute('disabled')), 'Create enabled');
                await id('tenants-create-submit').click();
                const expected = status === 'events' ? 'pending' : 'unavailable';
                await page.locator(`${availability}[data-state="${expected}"]`).waitFor();
                const trace = await page.evaluate(() => window.__handoffStates);
                assert.equal(trace[0]?.state, expected, 'Expected first insertion, with no earlier Pending');
                const label = await page.locator(availability).getAttribute('aria-label');
                assert.equal(label, culture === 'fr' ? (status === 'events' ? 'Audit en attente' : 'Audit indisponible')
                    : (status === 'events' ? 'Audit pending' : 'Audit unavailable'));
                assert.equal(await page.locator(`${availability} [data-testid="tenants-audit-availability-announcement"]`).getAttribute('aria-live'),
                    status === 'events' ? 'polite' : 'assertive');
                assert.ok(await id('tenants-create-live-region').isVisible());
                record(`${culture}: ${status} first insertion, projection unable to confirm`);
                const prior = fixture.snapshot().statusReads;
                await wait(() => page.locator(refresh).evaluate(element => !element.hasAttribute('disabled')), 'Refresh enabled');
                await page.locator(refresh).click();
                await wait(() => fixture.snapshot().statusReads > prior, 'Selected Refresh queries status');
                await wait(() => page.locator(refresh).evaluate(element => !element.hasAttribute('disabled')), 'Refresh completes');
                await page.locator(`${availability}[data-state="${expected}"]`).waitFor();
                assert.equal(fixture.snapshot().simulatedCommands, 1);
                assert.equal(fixture.snapshot().projection, 'hold');
                assert.equal(fixture.snapshot().unexpectedCommands, 0);
                record(`${culture}: ${status} same-state ${refresh === lifecycleRefresh ? 'lifecycle' : 'availability'} Refresh without redispatch`);
                if (status === 'events' && refresh === lifecycleRefresh) {
                    fixture.apply('projection deliver');
                    assert.equal(fixture.snapshot().status, 'events');
                    await page.locator(lifecycleRefresh).click();
                    await wait(async () => (await id('tenants-create-state').innerText()).includes(culture === 'fr'
                        ? 'La projection confirme' : 'Projection confirmed'), 'Independent projection release confirms');
                    assert.equal(fixture.snapshot().simulatedCommands, 1);
                    record(`${culture}: projection release confirms independently of status`);
                }
            }
        }
    }
    const timeline = evidence.handoffUiEvents;
    for (const culture of ['en', 'fr']) {
        assert.ok(timeline.some(event => event.culture === culture && event.kind === 'search-notices'
            && event.notices.includes('tenants-list-refreshed-notice') && event.notices.includes('tenants-list-search-unavailable-notice')));
        for (const control of ['tenants-create-submit', 'tenants-create-refresh', 'tenants-audit-availability-recovery-refresh'])
            assert.ok(timeline.some(event => event.culture === culture && event.kind === 'activation' && event.control === control));
        assert.ok(timeline.some(event => event.culture === culture && event.kind === 'audit-state' && event.state === 'pending'));
        assert.ok(timeline.some(event => event.culture === culture && event.kind === 'audit-state' && event.state === 'unavailable'));
    }
    await capture();
    evidence.fixtureScope = 'Authenticated production UI and manual control browser checks in EN/FR; no actual AT speech or backend delivery qualification';
}
