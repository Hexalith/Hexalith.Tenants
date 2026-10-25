// Reuse the signed loopback identity provider, with a real backend token supplied
// privately over stdin. Successful production OIDC validation populates token relay.
import { createInterface } from 'node:readline';
import { startLocalOidcFixture } from './local-oidc-fixture.mjs';

let accessToken;
const fixture = await startLocalOidcFixture({
    claims: { sub: '11111111-1111-1111-1111-111111111111' },
    accessToken: () => accessToken,
});
process.stdout.write(`${fixture.authority}\n`);
try {
    for await (const line of createInterface({ input: process.stdin, crlfDelay: Infinity })) {
        const value = JSON.parse(line);
        if (typeof value !== 'string' || !value) throw new Error('invalid-fixture-token');
        accessToken = value;
        process.stdout.write('ready\n');
    }
} catch {
    process.stderr.write('OIDC fixture input failed\n');
    process.exitCode = 1;
} finally {
    await fixture.close();
}
