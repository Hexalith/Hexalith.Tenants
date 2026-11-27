"""Execute documented bootstrap, handoff, and tenant commands against loopback dummy HTTP.

No live IdP, valid authentication token or domain service is used. The dummy token
has an inert header/signature and only exercises local subject decoding.
"""

import ast
import base64
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
import os
from pathlib import Path
import re
import socket
import subprocess
import tempfile
import threading
import time
import unittest
from urllib.parse import parse_qs, urlparse


ROOT = Path(__file__).resolve().parents[2]
QUICKSTART = (ROOT / 'docs/quickstart.md').read_text()
READINESS = (ROOT / 'docs/production-auth-readiness.md').read_text()


def shell_functions(text, only=None):
    return '\n'.join(match[0] for match in re.finditer(
        r'^(\w+)\(\) (?:\{\n.*?^\}|\(\n.*?^\))(?=\n|$)', text, re.M | re.S)
        if only is None or match[1] in only)


CURRENT_READINESS, LEGACY_READINESS = READINESS.split('#### Legacy public bootstrap recovery', 1)
CURRENT_FUNCTIONS = shell_functions(QUICKSTART) + '\n' + shell_functions(CURRENT_READINESS)

# The retained mutation uses the legacy wire. Keep these fixture helpers
# independent of current scope and identity selection.
LEGACY_FUNCTIONS = '\n'.join((
    shell_functions(QUICKSTART, {'platform_api', 'verify_configured_identity', 'token_subject', 'new_ulid'}),
    shell_functions(CURRENT_READINESS, {'retain_startup_attempt'}),
    shell_functions(LEGACY_READINESS),
    '''
has_admin_membership() {
  local body
  body=$(platform_api "$1" -fsS "$TENANTS_API_URL/api/global-administrators?pageSize=100") || return 1
  jq -e --arg subject "$2" '.items | any(.userId == $subject)' >/dev/null <<< "$body"
}
resume_bootstrap() {
  BOOTSTRAP_PROJECTED=false
  [[ -n "${BOOTSTRAP_MESSAGE_ID:-}" && -n "${BOOTSTRAP_CORRELATION_ID:-}" ]] || return 1
  verify_configured_identity || return 1
  local body
  body=$(platform_api "$ADMIN_TOKEN" -fsS \
    "$EVENTSTORE_URL/api/v1/commands/status/$BOOTSTRAP_CORRELATION_ID") || return 1
  jq -e --arg correlation "$BOOTSTRAP_CORRELATION_ID" \
    '.correlationId == $correlation and .status == "Completed"' >/dev/null <<< "$body" || return 1
  has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || return 1
  BOOTSTRAP_PROJECTED=true
}
''',
))


def bash_block(first_line):
    return first_line + QUICKSTART.split('```bash\n' + first_line, 1)[1].split('```', 1)[0]


def dummy_token(subject):
    payload = base64.urlsafe_b64encode(json.dumps({'sub': subject}).encode()).decode().rstrip('=')
    return f'fixture.{payload}.fixture'


class DummyServer(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self):
        super().__init__(('127.0.0.1', 0), DummyHandler)
        self.calls = []
        self.payloads = []
        self.scopes = []
        self.mode = 'normal'
        self.status = 'Completed'
        self.status_overrides = {}
        self.hosted_bootstrap = None
        self.legacy = False
        self.metadata = {}
        self.paged = False
        self.bootstrap = False
        self.stall = threading.Event()
        self.status_reads = 0

    @property
    def url(self):
        return f'http://127.0.0.1:{self.server_port}'


class DummyHandler(BaseHTTPRequestHandler):
    def log_message(self, *args):
        pass

    def reply(self, body, status=200, headers=()):
        encoded = json.dumps(body).encode()
        self.send_response(status)
        self.send_header('Content-Type', 'application/json')
        self.send_header('Content-Length', str(len(encoded)))
        for name, value in headers:
            self.send_header(name, value)
        self.end_headers()
        try:
            self.wfile.write(encoded)
        except (BrokenPipeError, ConnectionResetError):
            pass

    def stalled_reply(self):
        self.send_response(200)
        self.send_header('Content-Length', '1000')
        self.end_headers()
        self.server.stall.wait(5)

    def record_request(self, data=None):
        self.server.calls.append((self.path, self.headers.get('Authorization'), data))
        self.server.scopes.append((urlparse(self.path).path,
                                   self.headers.get_all('X-Hexalith-Scope', []),
                                   self.headers.get_all('X-Hexalith-Tenant', [])))

    def has_scope(self, scope):
        return (self.headers.get_all('X-Hexalith-Scope', []) == ([scope] if scope else [])
                and not self.headers.get_all('X-Hexalith-Tenant'))

    def do_POST(self):
        data = self.rfile.read(int(self.headers.get('Content-Length', '0')))
        self.record_request(data)
        if self.path == '/api/v1/commands':
            command = json.loads(data)
            tenant = command['tenant']
            if self.server.legacy:
                if tenant != 'system' or not self.has_scope(None):
                    return self.reply({}, 403)
            elif tenant not in ('platform:tenants', 'platform:global-administrators') or not self.has_scope(tenant):
                return self.reply({}, 403)
            self.server.payloads.append(command)
            if self.server.mode == 'lost_202':
                self.send_response(202)
                self.send_header('Content-Length', '100')
                self.end_headers()
                self.connection.shutdown(socket.SHUT_RDWR)
                self.connection.close()
                return
            return self.reply({}, 202)
        self.reply({}, 404)

    def do_GET(self):
        self.record_request()
        if self.server.mode in ('api_stall', 'status_stall') and (
                self.server.mode == 'api_stall' or '/status/' in self.path):
            return self.stalled_reply()
        if self.server.mode == 'http_failure':
            return self.reply({}, 503)
        if '/api/v1/commands/status/' in self.path:
            self.server.status_reads += 1
            id_field = 'correlationId' if self.server.legacy else 'messageId'
            identifier = self.path.rsplit('/', 1)[1]
            commands = self.server.payloads + ([self.server.hosted_bootstrap] if self.server.hosted_bootstrap else [])
            command = next((item for item in commands if item.get(id_field) == identifier), None)
            if command is None:
                return self.reply({'type': 'https://hexalith.io/problems/command-status-not-found'}, 404)
            if not self.has_scope(None if self.server.legacy else command['tenant']):
                return self.reply({}, 403)
            status = self.server.status
            if self.server.mode == 'publish_then_completed':
                status = 'PublishFailed' if self.server.status_reads == 1 else 'Completed'
            correlation = command.get('correlationId', command['messageId'])
            body = {'status': status, 'correlationId': correlation}
            if not self.server.legacy:
                body['messageId'] = command['messageId']
            return self.reply(body | self.server.status_overrides)
        if self.path == '/api/tenants/my-first-tenant':
            return self.reply({'tenantId': 'my-first-tenant'})
        if self.path == '/api/tenants/my-first-tenant/users':
            return self.reply({'items': [{'userId': 'jane-doe', 'role': 'TenantContributor'}]})
        if urlparse(self.path).path != '/api/global-administrators':
            return self.reply({}, 404)
        if not self.has_scope(None if self.server.legacy else 'platform:global-administrators'):
            return self.reply({}, 403)
        second_page = parse_qs(urlparse(self.path).query).get('cursor') == ['next']
        users = ['fixture-admin', 'fixture-human']
        if self.server.bootstrap and not self.server.payloads:
            users = []
        if self.server.mode == 'human_missing':
            users = ['fixture-admin']
        if self.server.mode == 'missing':
            users = []
        if self.server.paged:
            users = ['fixture-human'] if second_page else ['fixture-admin']
        metadata = {
            'X-Hexalith-Query-Provenance': 'ProjectionBacked',
            'X-Hexalith-Projection-Lifecycle': 'Current',
            'X-Hexalith-Is-Stale': 'false',
            'X-Hexalith-Projection-Version': 'fixture-version',
        }
        metadata.update(self.server.metadata)
        if self.server.mode == 'mixed_version' and second_page:
            metadata['X-Hexalith-Projection-Version'] = 'other-version'
        headers = []
        for name, value in metadata.items():
            if value is not None:
                headers.extend((name, item) for item in (value if isinstance(value, list) else [value]))
        if self.server.mode == 'malformed':
            return self.reply('invalid-body', headers=headers)
        return self.reply({'items': [{'userId': user} for user in users],
                           'cursor': 'next' if self.server.paged and not second_page else None,
                           'hasMore': self.server.mode == 'bad_cursor' or (self.server.paged and not second_page)}, headers=headers)


class BootstrapHandoffTests(unittest.TestCase):
    def setUp(self):
        self.server = DummyServer()
        self.thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.thread.start()
        self.addCleanup(self.close_server)

    def close_server(self):
        self.server.stall.set()
        self.server.shutdown()
        self.server.server_close()
        self.thread.join(2)

    def run_shell(self, script, *, stdin='', timeout=12, cwd=None, legacy=False):
        self.server.legacy = legacy
        environment = dict(os.environ, ADMIN_TOKEN=dummy_token('fixture-admin'),
                           ADMIN_SUBJECT='fixture-admin',
                           TENANTS_API_URL=self.server.url, EVENTSTORE_URL=self.server.url,
                           NO_PROXY='127.0.0.1', no_proxy='127.0.0.1')
        functions = LEGACY_FUNCTIONS if legacy else CURRENT_FUNCTIONS
        result = subprocess.run(['bash', '-c', functions + '\n' + '''
CONNECT_TIMEOUT=1
REQUEST_TIMEOUT=1
POLL_SECONDS=3
''' + script], input=stdin, text=True, capture_output=True, env=environment, timeout=timeout, cwd=cwd)
        self.assertEqual(result.returncode, 0, f'Fixture shell failed: {result.stderr}')
        return result

    def test_documentation_syntax_and_owned_links(self):
        for document in ('quickstart.md', 'production-auth-readiness.md', 'production-auth-claim-contract.md'):
            text = (ROOT / 'docs' / document).read_text()
            for kind, body in re.findall(r'^```(bash|json)\n(.*?)^```', text, re.M | re.S):
                if kind == 'json':
                    json.loads(body)
                else:
                    result = subprocess.run(['bash', '-n'], input=body, text=True, capture_output=True)
                    self.assertEqual(result.returncode, 0, result.stderr)
                    for program in re.findall(r"<<'PYTHON'\n(.*?)^PYTHON$", body, re.M | re.S):
                        ast.parse(program)
            for link in re.findall(r'\]\(([^)]+)\)', text):
                if not re.match(r'[a-z]+://|#', link):
                    target = (ROOT / 'docs' / link.split('#')[0]).resolve()
                    # This workflow does not initialize reference submodules.
                    if ROOT / 'references' not in target.parents:
                        self.assertTrue(target.exists(), (document, link))
        self.assertEqual(QUICKSTART.count('dotnet run --project src/Hexalith.Tenants.AppHost/'), 1)

    def test_issued_token_is_read_without_echo_and_invalid_tokens_are_not_sent(self):
        result = self.run_shell('''
authenticate_configured_admin || exit 10
[[ "$(token_subject "$ADMIN_TOKEN")" == fixture-admin ]] || exit 11
has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || exit 12
if token_subject invalid >/dev/null 2>&1; then exit 13; fi
if platform_api 'fixture.invalid".fixture' -fsS "$TENANTS_API_URL/api/global-administrators"; then exit 14; fi
''', stdin=dummy_token('fixture-admin') + '\n')
        self.assertEqual(result.stdout, '\n')
        (api_call,) = self.server.calls
        self.assertEqual(api_call[1], 'Bearer ' + dummy_token('fixture-admin'))

    def test_current_membership_and_legacy_current_metadata(self):
        for lifecycle, degraded in (('Current', None), (None, None), ('Current', 'false')):
            with self.subTest(lifecycle=lifecycle, degraded=degraded):
                self.server.metadata = {'X-Hexalith-Projection-Lifecycle': lifecycle,
                                        'X-Hexalith-Is-Degraded': degraded}
                self.run_shell('has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT" || exit 10')

    def test_projection_metadata_fails_closed(self):
        cases = [
            {'X-Hexalith-Query-Provenance': 'Live'},
            {'X-Hexalith-Query-Provenance': 'projectionbacked'},
            {'X-Hexalith-Query-Provenance': None},
            {'X-Hexalith-Projection-Lifecycle': 'Stale'},
            {'X-Hexalith-Projection-Lifecycle': 'Degraded'},
            {'X-Hexalith-Projection-Lifecycle': 'Unknown'},
            {'X-Hexalith-Projection-Lifecycle': 'current'},
            {'X-Hexalith-Is-Degraded': 'invalid'},
            {'X-Hexalith-Is-Stale': 'true'},
            {'X-Hexalith-Is-Degraded': 'true'},
            {'X-Hexalith-Projection-Version': None},
            {'X-Hexalith-Projection-Version': ''},
            {'X-Hexalith-Projection-Version': ['v1', 'v1']},
            {'X-Hexalith-Projection-Lifecycle': None, 'X-Hexalith-Is-Stale': None},
        ]
        for metadata in cases:
            with self.subTest(metadata=metadata):
                self.server.metadata = metadata
                self.run_shell('if has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then exit 10; fi')

    def test_paged_membership_requires_same_version_on_every_page(self):
        self.server.paged = True
        self.run_shell('has_admin_membership "$ADMIN_TOKEN" fixture-human || exit 10')
        self.server.mode = 'mixed_version'
        self.run_shell('if has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then exit 10; fi')

    def test_wrong_case_and_missing_subject_do_not_confirm_membership(self):
        self.run_shell('if has_admin_membership "$ADMIN_TOKEN" Fixture-Admin; then exit 10; fi')
        self.server.mode = 'missing'
        self.run_shell('if has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then exit 10; fi')

    def test_malformed_http_failure_and_missing_cursor_stay_unresolved(self):
        for mode in ('malformed', 'http_failure', 'bad_cursor'):
            with self.subTest(mode=mode):
                self.server.mode = mode
                self.run_shell('if has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"; then exit 10; fi')
        self.server.mode = 'http_failure'
        self.run_shell('if wait_command_completed "$ADMIN_TOKEN" fixture-id platform:global-administrators; then exit 11; fi')

    def test_initial_membership_failure_keeps_all_recovery_helpers_and_shell(self):
        self.server.mode = 'missing'
        check = QUICKSTART.split('### Verify configured administrator membership', 1)[1].split('```bash\n', 1)[1].split('```', 1)[0]
        self.run_shell(check + '''
declare -F resume_handoff resume_bootstrap wait_command_completed new_ulid >/dev/null || exit 10
verify_configured_identity || exit 11
''')

    def test_grant_success_checks_exact_payload_then_authenticates_human(self):
        self.run_shell('''
grant_human_access || exit 10
[[ "$HANDOFF_PROJECTED" == true ]] || exit 11
authenticate_human || exit 12
[[ "$(token_subject "$TOKEN")" == "$HANDOFF_SUBJECT" ]] || exit 13
''', stdin='fixture-human\n' + dummy_token('fixture-human') + '\n')
        self.assertEqual(len(self.server.payloads), 1)
        command = self.server.payloads[0]
        self.assertEqual((command['tenant'], command['domain'], command['aggregateId'], command['commandType'], command['payload']),
                         ('platform:global-administrators', 'global-administrators', 'global-administrators',
                          'SetGlobalAdministrator', {'UserId': 'fixture-human'}))
        for key in ('messageId', 'correlationId'):
            self.assertRegex(command[key], r'^[0-7][0-9A-HJKMNP-TV-Z]{25}$')
        self.assertTrue(any(call[1] == 'Bearer ' + dummy_token('fixture-human') for call in self.server.calls))
        self.assertTrue(all(scope == ['platform:global-administrators'] and tenant == []
                            for _, scope, tenant in self.server.scopes))
        self.assertIn(('/api/v1/commands/status/' + command['messageId'], ['platform:global-administrators'], []),
                      self.server.scopes)

    def test_documented_create_and_add_submit_and_poll_platform_scope_then_read_route_tenant(self):
        requests = {}
        for body in re.findall(r'^```json\n(.*?)^```$', QUICKSTART, re.M | re.S):
            command = json.loads(body)
            requests[command['commandType']] = body
        with tempfile.TemporaryDirectory() as directory:
            (Path(directory) / 'create-tenant.json').write_text(requests['CreateTenant'])
            (Path(directory) / 'add-user.json').write_text(requests['AddUserToTenant'])
            self.run_shell('set -e\nTOKEN=$ADMIN_TOKEN\n' + ''.join(map(bash_block, (
                'STATUS_PROBE_ID=', 'CREATE_MESSAGE_ID=',
                'wait_command_completed "$TOKEN" "$CREATE_MESSAGE_ID"',
                'platform_api "$TOKEN" -fsS "$TENANTS_API_URL/api/tenants/my-first-tenant"',
                'ADD_USER_MESSAGE_ID=',
                'platform_api "$TOKEN" -fsS "$TENANTS_API_URL/api/tenants/my-first-tenant/users"',
            ))), cwd=directory)

        create, add = self.server.payloads
        for command in (create, add):
            self.assertEqual(command['tenant'], 'platform:tenants')
            self.assertEqual(command['domain'], 'tenants')
            self.assertEqual(command['aggregateId'], command['payload']['TenantId'])
            self.assertRegex(command['messageId'], r'^[0-7][0-9A-HJKMNP-TV-Z]{25}$')
            self.assertNotEqual(command['messageId'], json.loads(requests[command['commandType']])['messageId'])
        self.assertEqual(create['commandType'], 'CreateTenant')
        self.assertEqual(add['commandType'], 'AddUserToTenant')
        self.assertNotEqual(create['messageId'], add['messageId'])
        probe, *requests_after_probe = self.server.scopes
        self.assertRegex(probe[0], r'^/api/v1/commands/status/[0-7][0-9A-HJKMNP-TV-Z]{25}$')
        self.assertEqual(probe[1:], (['platform:tenants'], []))
        self.assertEqual(requests_after_probe, [
            ('/api/v1/commands', ['platform:tenants'], []),
            ('/api/v1/commands/status/' + create['messageId'], ['platform:tenants'], []),
            ('/api/tenants/my-first-tenant', [], []),
            ('/api/v1/commands', ['platform:tenants'], []),
            ('/api/v1/commands/status/' + add['messageId'], ['platform:tenants'], []),
            ('/api/tenants/my-first-tenant/users', [], []),
        ])

    def test_lost_response_blocks_replacement_and_human_auth_then_recovers_same_ids(self):
        self.server.mode = 'lost_202'
        self.run_shell('''
if grant_human_access; then exit 10; fi
saved_message=$HANDOFF_MESSAGE_ID
saved_correlation=$HANDOFF_CORRELATION_ID
saved_subject=$HANDOFF_SUBJECT
if grant_human_access; then exit 11; fi
if authenticate_human; then exit 12; fi
IFS= read -r leftover
[[ "$leftover" == must-stay-unread ]] || exit 13
[[ "$saved_message" == "$HANDOFF_MESSAGE_ID" && "$saved_correlation" == "$HANDOFF_CORRELATION_ID" && "$saved_subject" == "$HANDOFF_SUBJECT" ]] || exit 14
resume_handoff || exit 15
[[ "$HANDOFF_PROJECTED" == true ]] || exit 16
''', stdin='fixture-human\nmust-stay-unread\n')
        self.assertEqual(len(self.server.payloads), 1)
        status_calls = [call[0] for call in self.server.calls if '/status/' in call[0]]
        self.assertEqual(status_calls, ['/api/v1/commands/status/' + self.server.payloads[0]['messageId']])

    def test_publish_failed_retains_handles_and_requires_same_status_recovery(self):
        self.server.mode = 'publish_then_completed'
        self.run_shell('''
if grant_human_access; then exit 10; fi
saved=$HANDOFF_CORRELATION_ID
if grant_human_access; then exit 11; fi
if authenticate_human; then exit 12; fi
resume_handoff || exit 13
[[ "$HANDOFF_CORRELATION_ID" == "$saved" && "$HANDOFF_PROJECTED" == true ]] || exit 14
''', stdin='fixture-human\n')
        self.assertEqual(len(self.server.payloads), 1)
        statuses = [call[0] for call in self.server.calls if '/status/' in call[0]]
        self.assertEqual(len(statuses), 2)
        self.assertEqual(statuses[0], statuses[1])

    def test_status_failures_and_projection_absence_keep_handoff_unresolved(self):
        for status in ('Rejected', 'PublishFailed', 'TimedOut', 'Failed', 'Processing'):
            with self.subTest(status=status):
                self.server.status = status
                self.run_shell('''
if grant_human_access; then exit 10; fi
[[ "$HANDOFF_PROJECTED" == false && -n "$HANDOFF_CORRELATION_ID" ]] || exit 11
if authenticate_human; then exit 12; fi
''', stdin='fixture-human\n')
        self.server.status = 'Completed'
        self.server.mode = 'human_missing'
        self.run_shell('''
if grant_human_access; then exit 10; fi
[[ "$HANDOFF_PROJECTED" == false && -n "$HANDOFF_CORRELATION_ID" ]] || exit 11
if authenticate_human; then exit 12; fi
''', stdin='fixture-human\n')

    def test_completed_status_for_another_message_or_correlation_cannot_complete_handoff(self):
        for field in ('messageId', 'correlationId'):
            with self.subTest(field=field):
                self.server.status_overrides = {field: '01JQK000000000000000009997'}
                self.run_shell('''
if grant_human_access; then exit 10; fi
[[ "$HANDOFF_PROJECTED" == false && -n "$HANDOFF_MESSAGE_ID" && -n "$HANDOFF_CORRELATION_ID" ]] || exit 11
if authenticate_human; then exit 12; fi
''', stdin='fixture-human\n')
                command = self.server.payloads[-1]
                self.assertEqual(self.server.scopes[-1],
                                 ('/api/v1/commands/status/' + command['messageId'], ['platform:global-administrators'], []))

    def test_wrong_configured_identity_or_missing_membership_cannot_grant(self):
        self.run_shell('''
ADMIN_TOKEN=''' + dummy_token('someone-else') + '''
if grant_human_access; then exit 10; fi
[[ -z "${HANDOFF_CORRELATION_ID:-}" ]] || exit 11
''')
        self.server.mode = 'missing'
        self.run_shell('if grant_human_access; then exit 10; fi')
        self.assertEqual(self.server.payloads, [])

    def test_legacy_bootstrap_needs_explicit_state_proof_and_preserves_unresolved_handles(self):
        self.server.bootstrap = True
        self.server.mode = 'lost_202'
        self.run_shell('''
if recover_bootstrap; then exit 10; fi
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
if recover_bootstrap; then exit 11; fi
saved_message=$BOOTSTRAP_MESSAGE_ID
saved_correlation=$BOOTSTRAP_CORRELATION_ID
if recover_bootstrap; then exit 12; fi
if retain_startup_attempt; then exit 13; fi
[[ "$BOOTSTRAP_MESSAGE_ID" == "$saved_message" && "$BOOTSTRAP_CORRELATION_ID" == "$saved_correlation" ]] || exit 14
resume_bootstrap || exit 15
[[ "$BOOTSTRAP_PROJECTED" == true ]] || exit 16
''', legacy=True)
        self.assertEqual(len(self.server.payloads), 1)
        command = self.server.payloads[0]
        self.assertEqual(command['tenant'], 'system')
        self.assertEqual(command['payload'], {'UserId': 'fixture-admin'})
        self.assertIn(('/api/v1/commands/status/' + command['correlationId'], [], []), self.server.scopes)
        self.assertTrue(all(not scope and not tenant for _, scope, tenant in self.server.scopes))

    def test_legacy_bootstrap_completed_without_projection_stays_unresolved(self):
        self.server.bootstrap = True
        self.server.mode = 'missing'
        self.server.status = 'Completed'
        result = self.run_shell('''
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
if recover_bootstrap; then exit 10; fi
[[ "$BOOTSTRAP_PROJECTED" == false && -n "$BOOTSTRAP_MESSAGE_ID" && -n "$BOOTSTRAP_CORRELATION_ID" ]] || exit 11
saved_message=$BOOTSTRAP_MESSAGE_ID
saved_correlation=$BOOTSTRAP_CORRELATION_ID
if recover_bootstrap; then exit 12; fi
[[ "$BOOTSTRAP_PROJECTED" == false && "$BOOTSTRAP_MESSAGE_ID" == "$saved_message" && "$BOOTSTRAP_CORRELATION_ID" == "$saved_correlation" ]] || exit 13
printf '%s\\n' "$BOOTSTRAP_MESSAGE_ID" "$BOOTSTRAP_CORRELATION_ID"
''', legacy=True)
        self.assertEqual(len(self.server.payloads), 1)
        self.assertEqual(sum(data is not None for _, _, data in self.server.calls), 1)
        command = self.server.payloads[0]
        self.assertEqual(result.stdout.splitlines(), [command['messageId'], command['correlationId']])
        self.assertEqual(self.server.status_reads, 1)
        self.assertEqual([call[0] for call in self.server.calls if '/status/' in call[0]],
                         ['/api/v1/commands/status/' + command['correlationId']])

    def test_current_startup_event_handles_support_read_only_bootstrap_recovery(self):
        message = '01JQK000000000000000009998'
        correlation = '01JQK000000000000000009999'
        self.server.hosted_bootstrap = {'messageId': message, 'correlationId': correlation,
                                        'tenant': 'platform:global-administrators'}
        self.run_shell('''
retain_startup_attempt || exit 10
resume_bootstrap || exit 11
[[ "$BOOTSTRAP_PROJECTED" == true ]] || exit 12
''', stdin=message + '\n' + correlation + '\n')
        self.assertEqual(self.server.payloads, [])
        self.assertEqual(self.server.scopes[0], ('/api/v1/commands/status/' + message, ['platform:global-administrators'], []))

    def test_current_bootstrap_refuses_completed_status_from_another_message_or_correlation(self):
        message = '01JQK000000000000000009998'
        correlation = '01JQK000000000000000009999'
        self.server.hosted_bootstrap = {'messageId': message, 'correlationId': correlation,
                                        'tenant': 'platform:global-administrators'}
        for field in ('messageId', 'correlationId'):
            with self.subTest(field=field):
                self.server.status_overrides = {field: '01JQK000000000000000009997'}
                self.run_shell('''
retain_startup_attempt || exit 10
if resume_bootstrap; then exit 11; fi
[[ "$BOOTSTRAP_PROJECTED" == false ]] || exit 12
''', stdin=message + '\n' + correlation + '\n')
                self.assertEqual(self.server.scopes[-1],
                                 ('/api/v1/commands/status/' + message, ['platform:global-administrators'], []))
        self.assertEqual(self.server.payloads, [])

    def test_wrong_identity_cannot_legacy_bootstrap_even_with_operator_state_proof(self):
        self.server.bootstrap = True
        self.run_shell('''
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
ADMIN_TOKEN=''' + dummy_token('someone-else') + '''
if recover_bootstrap; then exit 10; fi
[[ -z "${BOOTSTRAP_MESSAGE_ID:-}" && -z "${BOOTSTRAP_CORRELATION_ID:-}" ]] || exit 11
''', legacy=True)
        self.assertEqual(self.server.calls, [])

    def test_existing_member_cannot_submit_legacy_bootstrap(self):
        self.run_shell('''
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
if recover_bootstrap; then exit 10; fi
''', legacy=True)
        self.assertEqual(self.server.payloads, [])

    def test_stalled_api_and_token_responses_are_time_bounded(self):
        for mode, invocation in (
                ('api_stall', 'has_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"'),
                ('status_stall', 'wait_command_completed "$ADMIN_TOKEN" fixture-id platform:global-administrators'),
                ('api_stall', 'wait_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"')):
            with self.subTest(mode=mode, invocation=invocation):
                self.server.mode = mode
                started = time.monotonic()
                self.run_shell('POLL_SECONDS=1\nif ' + invocation + '; then exit 10; fi', timeout=5)
                self.assertLess(time.monotonic() - started, 3.5)


if __name__ == '__main__':
    unittest.main()
