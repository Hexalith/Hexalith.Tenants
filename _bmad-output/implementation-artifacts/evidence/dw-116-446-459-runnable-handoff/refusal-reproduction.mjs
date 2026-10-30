// Review regression reproduction; run with Node from any working directory.
import assert from 'node:assert/strict';
import { Readable } from 'node:stream';
import { createDeferredHandoffFixture } from '../../../../tests/Hexalith.Tenants.UI.Tests/Browser/deferred-handoff-fixture.mjs';

const fixture = createDeferredHandoffFixture('test-search-token');
const jwt = claims => `Bearer header.${Buffer.from(JSON.stringify(claims)).toString('base64url')}.signature`;
const bearer = jwt({ sub: 'browser-ui-safety-operator', global_admin: 'true', 'eventstore:current-tenant': 'system' });
const cases = [
    [bearer, '{', 400],
    [undefined, '{}', 401],
    [bearer, '{}', 405],
    [bearer, 'null', 400],
    [jwt(null), '{}', 401],
    [bearer, '[]', 400],
    [jwt([]), '{}', 401],
];
for (const [authorization, body, expectedStatus] of cases) {
    const request = Readable.from([body]);
    request.method = 'POST';
    request.headers = { authorization };
    let status;
    await fixture.handle(request, { writeHead(value) { status = value; }, end() {} }, new URL('http://localhost/api/v1/commands'));
    assert.equal(status, expectedStatus);
}
assert.equal(fixture.snapshot().unexpectedCommands, cases.length);
assert.equal(fixture.events.filter(event => event.kind === 'command-attempt').length, cases.length);
assert.equal(fixture.events.filter(event => event.kind === 'command-refused').length, cases.length);
console.log('7/7 invalid command/claim shapes refused, counted once each, with no exceptions.');
