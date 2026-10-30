// Controls for the existing authenticated fixture's loopback upstream only.
// Nothing is forwarded to a domain service. Control input is stdin, never HTTP.
import { randomUUID } from 'node:crypto';

export function createDeferredHandoffFixture(searchToken) {
    const events = [];
    let state;
    let sessionOrdinal = 0;
    const record = kind => events.push({ sessionOrdinal, utc: new Date().toISOString(), kind });
    const reset = () => {
        sessionOrdinal++;
        state = { search: 'up', rejectCursor: false, status: 'wait', projection: 'hold',
            command: null, simulatedCommands: 0, unexpectedCommands: 0, rejectedCursors: 0,
            recoveryPages: 0, continuations: 0, statusReads: 0, projectionReads: 0, awaitingRecovery: false };
        record('session-reset');
    };
    reset();
    const snapshot = () => ({ sessionOrdinal, search: state.search, rejectCursor: state.rejectCursor,
        status: state.status, projection: state.projection, simulatedCommands: state.simulatedCommands,
        unexpectedCommands: state.unexpectedCommands, rejectedCursors: state.rejectedCursors,
        recoveryPages: state.recoveryPages, continuations: state.continuations,
        statusReads: state.statusReads, projectionReads: state.projectionReads });
    const apply = line => {
        switch (line.trim()) {
            case 'search up': state.search = 'up'; break;
            case 'search down': state.search = 'down'; break;
            case 'cursor reject-once':
                if (state.search !== 'down') throw new Error('Select search down before arming a fallback rejection');
                state.rejectCursor = true; break;
            case 'status wait': state.status = 'wait'; break;
            case 'status events': state.status = 'events'; break;
            case 'status unavailable': state.status = 'unavailable'; break;
            case 'projection hold': state.projection = 'hold'; break;
            case 'projection deliver': state.projection = 'deliver'; break;
            case 'show': return snapshot();
            default: throw new Error('Unknown control; use help');
        }
        record(line.trim());
        return snapshot();
    };
    const tenant = (id, name = `needle ${id}`, description = '') => ({ tenantId: id, name, description,
        status: 'Active', members: [], configuration: {}, createdAt: '2026-10-30T10:00:00Z' });
    const visibleCommand = () => state.command && state.projection === 'deliver';
    const commandTenant = () => tenant(state.command.aggregateId, state.command.payload.Name, state.command.payload.Description || '');
    const handle = async (request, response, url) => {
        const requestState = state;
        if (request.method !== 'GET') record('command-attempt');
        const send = (body, status = 200, problem = false) => {
            if (request.method !== 'GET' && status >= 400) {
                requestState.unexpectedCommands++; record('command-refused');
            }
            const version = visibleCommand() ? 2 : 1;
            response.writeHead(status, { 'Content-Type': problem ? 'application/problem+json' : 'application/json',
                'Cache-Control': 'no-store', ETag: `"handoff-${version}"`,
                'X-Hexalith-Query-Provenance': 'ProjectionBacked', 'X-Hexalith-Projection-Version': `tenant-sequence:${version}`,
                'X-Hexalith-Projection-Lifecycle': 'Current', 'X-Hexalith-Is-Stale': 'false', 'X-Hexalith-Is-Degraded': 'false' });
            response.end(JSON.stringify(body));
        };
        if (url.pathname === '/api/v1/search') {
            if (request.method !== 'GET' || request.headers.authorization !== `Bearer ${searchToken}`) { send({}, 401); return; }
            record(`search-${state.search}`);
            if (state.search === 'down') { send({}, 503); return; }
            const offset = Number(url.searchParams.get('offset') || 0);
            send({ query: 'needle', totalCount: 60, hasIndexedMemoryUnits: true, axesUsed: ['syntactic'],
                results: Array.from({ length: Math.min(20, Math.max(0, 60 - offset)) }, (_, index) => ({
                    memoryUnitId: `handoff-${offset + index}`, score: 1, contentSnippet: 'fixture',
                    sourceUri: `tenant:tenant.search.${offset + index}`, sourceType: 'projection', axis: 'syntactic' })) });
            return;
        }
        // Authentication is performed by the production OIDC/cookie/circuit stack. Like the
        // existing upstream fixture, this checks coherent bearer claims, not backend authorization.
        let claims;
        try { claims = JSON.parse(Buffer.from(request.headers.authorization?.slice(7).split('.')[1] || '', 'base64url')); }
        catch { send({}, 401); return; }
        if (!claims || typeof claims !== 'object' || Array.isArray(claims)
            || !request.headers.authorization?.startsWith('Bearer ') || claims.sub !== 'browser-ui-safety-operator'
            || claims['eventstore:current-tenant'] !== 'system' || claims.global_admin !== 'true') { send({}, 401); return; }
        record('authorized-read');
        if (request.method === 'POST' && url.pathname === '/api/v1/commands') {
            let body = ''; for await (const chunk of request) body += chunk;
            if (requestState !== state) { send({}, 409); return; }
            let command;
            try { command = JSON.parse(body); } catch { send({}, 400); return; }
            if (!command || typeof command !== 'object' || Array.isArray(command)) { send({}, 400); return; }
            if (state.command || command.commandType !== 'CreateTenant' || command.tenant !== 'system'
                || command.domain !== 'tenants' || !/^tenant\.speech\.[a-z0-9-]+$/.test(command.aggregateId)
                || command.payload?.TenantId !== command.aggregateId || !command.messageId || !command.payload.Name) {
                send({ reason: 'commands-prohibited' }, 405); return;
            }
            state.command = { ...command, correlationId: randomUUID() };
            state.simulatedCommands++; record('create-accepted');
            send({ correlationId: state.command.correlationId }, 202); return;
        }
        if (request.method !== 'GET') {
            send({ reason: 'commands-prohibited' }, 405); return;
        }
        if (url.pathname.startsWith('/api/v1/commands/status/')) {
            if (!state.command || decodeURIComponent(url.pathname.split('/').at(-1)) !== state.command.messageId) { send({}, 409); return; }
            state.statusReads++; record(`status-delivered-${state.status}`);
            if (state.status === 'wait') { send({}, 404); return; }
            if (state.status === 'unavailable') { send({}, 200); return; }
            send({ messageId: state.command.messageId, correlationId: state.command.correlationId,
                aggregateId: state.command.aggregateId, tenantId: 'system', domain: 'tenants', status: 'EventsStored', eventCount: 1 });
        } else if (url.pathname === '/api/tenants') {
            state.projectionReads++;
            const cursor = url.searchParams.get('cursor');
            if (cursor && state.search === 'down' && state.rejectCursor) {
                state.rejectCursor = false; state.awaitingRecovery = true; state.rejectedCursors++;
                record('fallback-continuation-rejected');
                send({ type: 'about:blank', title: 'Invalid cursor', status: 400, reason: 'invalid-cursor' }, 400, true); return;
            }
            if (!cursor && state.awaitingRecovery) {
                state.awaitingRecovery = false; state.recoveryPages++; record('authorized-first-page-recovery');
            }
            if (cursor) { state.continuations++; record('continuation-permitted'); }
            const items = [tenant(cursor ? 'tenant.secondary' : 'tenant.alpha')];
            if (visibleCommand()) items.push(commandTenant());
            record(`list-projection-${state.projection}`);
            send({ items, cursor: cursor ? null : 'handoff-page-2', hasMore: !cursor });
        } else if (/^\/api\/tenants\/[^/]+\/audit$/.test(url.pathname)) {
            send({ items: [], cursor: null, hasMore: false });
        } else if (/^\/api\/tenants\/[^/]+$/.test(url.pathname)) {
            const id = decodeURIComponent(url.pathname.split('/').at(-1));
            state.projectionReads++;
            if (state.command && id === state.command.aggregateId) {
                record(`detail-projection-${state.projection}`);
                send(visibleCommand() ? commandTenant() : {}, visibleCommand() ? 200 : 404);
            } else if (id === 'tenant.alpha' || id === 'tenant.secondary' || /^tenant\.search\.\d+$/.test(id)) send(tenant(id));
            else send({}, 404);
        } else if (url.pathname === '/api/global-administrators') {
            send({ items: [{ userId: claims.sub }], cursor: null, hasMore: false });
        } else if (/^\/api\/users\/[^/]+\/tenants$/.test(url.pathname)) {
            send({ items: [], cursor: null, hasMore: false });
        } else send({}, 404);
    };
    return { apply, reset, snapshot, handle, events };
}
