// Test-only loopback OIDC issuer. Production authentication validates its signed
// tokens through discovery/JWKS; this fixture never connects to a domain service.
import { generateKeyPairSync, randomUUID, sign } from 'node:crypto';
import { createServer } from 'node:http';

export async function startLocalOidcFixture(options = {}) {
    const { privateKey, publicKey } = generateKeyPairSync('rsa', { modulusLength: 2048 });
    const keyId = randomUUID();
    const codes = new Map();
    let authority;
    const claims = { sub: 'browser-ui-safety-operator', global_admin: 'true',
        'eventstore:tenant': ['system', 'tenant.alpha'], 'eventstore:current-tenant': 'system', ...options.claims };
    const jwt = payload => {
        const encoded = value => Buffer.from(JSON.stringify(value)).toString('base64url');
        const input = `${encoded({ alg: 'RS256', typ: 'JWT', kid: keyId })}.${encoded(payload)}`;
        return `${input}.${sign('RSA-SHA256', Buffer.from(input), privateKey).toString('base64url')}`;
    };
    const server = createServer(async (request, response) => {
        const url = new URL(request.url, authority);
        const send = (payload, status = 200) => {
            response.writeHead(status, { 'Content-Type': 'application/json', 'Cache-Control': 'no-store' });
            response.end(JSON.stringify(payload));
        };
        if (url.pathname.endsWith('/.well-known/openid-configuration')) {
            send({ issuer: authority, authorization_endpoint: `${authority}/authorize`, token_endpoint: `${authority}/token`,
                jwks_uri: `${authority}/jwks`, response_types_supported: ['code'], response_modes_supported: ['query'],
                subject_types_supported: ['public'], id_token_signing_alg_values_supported: ['RS256'],
                token_endpoint_auth_methods_supported: ['client_secret_post', 'client_secret_basic'],
                code_challenge_methods_supported: ['S256'] });
        } else if (url.pathname.endsWith('/jwks')) {
            send({ keys: [{ ...publicKey.export({ format: 'jwk' }), kid: keyId, use: 'sig', alg: 'RS256' }] });
        } else if (url.pathname.endsWith('/authorize')) {
            const redirect = new URL(url.searchParams.get('redirect_uri'));
            if (!['localhost', '127.0.0.1'].includes(redirect.hostname) || redirect.pathname !== '/signin-oidc') {
                send({}, 400); return;
            }
            const code = randomUUID();
            codes.set(code, url.searchParams.get('nonce'));
            redirect.searchParams.set('code', code);
            redirect.searchParams.set('state', url.searchParams.get('state'));
            response.writeHead(302, { Location: redirect.href }); response.end();
        } else if (url.pathname.endsWith('/token') && request.method === 'POST') {
            let body = '';
            for await (const chunk of request) body += chunk;
            const form = new URLSearchParams(body);
            if (!codes.has(form.get('code'))) { send({}, 400); return; }
            const nonce = codes.get(form.get('code')); codes.delete(form.get('code'));
            const now = Math.floor(Date.now() / 1000);
            const payload = { ...claims, iss: authority, aud: 'tenants-browser-fixture', iat: now, exp: now + 3600 };
            send({ token_type: 'Bearer', expires_in: 3600,
                access_token: options.accessToken ? options.accessToken() : jwt(payload), id_token: jwt({ ...payload, nonce }) });
        } else send({}, 404);
    });
    await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
    authority = `http://127.0.0.1:${server.address().port}/realms/browser-fixture`;
    return { authority, close: async () => {
        server.closeAllConnections(); await new Promise(resolve => server.close(resolve));
    } };
}
