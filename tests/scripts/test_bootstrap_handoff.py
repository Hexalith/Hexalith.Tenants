"""Execute documented bootstrap/handoff shell functions against loopback dummy HTTP.

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
import threading
import time
import unittest
from urllib.parse import parse_qs, urlparse


ROOT = Path(__file__).resolve().parents[2]
QUICKSTART = (ROOT / 'docs/quickstart.md').read_text()
READINESS = (ROOT / 'docs/production-auth-readiness.md').read_text()


def shell_functions(text):
    return '\n'.join(match[0] for match in re.finditer(
        r'^\w+\(\) (?:\{\n.*?^\}|\(\n.*?^\))(?=\n|$)', text, re.M | re.S))


FUNCTIONS = shell_functions(QUICKSTART) + '\n' + shell_functions(READINESS)


def dummy_token(subject):
    payload = base64.urlsafe_b64encode(json.dumps({'sub': subject}).encode()).decode().rstrip('=')
    return f'fixture.{payload}.fixture'


class DummyServer(ThreadingHTTPServer):
    daemon_threads = True

    def __init__(self):
        super().__init__(('127.0.0.1', 0), DummyHandler)
        self.calls = []
        self.payloads = []
        self.mode = 'normal'
        self.status = 'Completed'
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

    def do_POST(self):
        data = self.rfile.read(int(self.headers.get('Content-Length', '0')))
        self.server.calls.append((self.path, self.headers.get('Authorization'), data))
        if self.path.endswith('/token'):
            if self.server.mode == 'http_failure':
                return self.reply({}, 503)
            if self.server.mode == 'token_stall':
                return self.stalled_reply()
            form = parse_qs(data.decode(), keep_blank_values=True)
            subject = 'fixture-human' if form.get('username', [''])[0] == 'human' else 'fixture-admin'
            return self.reply({'access_token': dummy_token(subject)})
        if self.path == '/api/v1/commands':
            self.server.payloads.append(json.loads(data))
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
        self.server.calls.append((self.path, self.headers.get('Authorization'), None))
        if self.server.mode in ('api_stall', 'status_stall') and (
                self.server.mode == 'api_stall' or '/status/' in self.path):
            return self.stalled_reply()
        if self.server.mode == 'http_failure':
            return self.reply({}, 503)
        if '/api/v1/commands/status/' in self.path:
            self.server.status_reads += 1
            status = self.server.status
            if self.server.mode == 'publish_then_completed':
                status = 'PublishFailed' if self.server.status_reads == 1 else 'Completed'
            return self.reply({'status': status})
        if urlparse(self.path).path != '/api/global-administrators':
            return self.reply({}, 404)
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

    def run_shell(self, script, *, stdin='', timeout=12):
        environment = dict(os.environ, ADMIN_TOKEN=dummy_token('fixture-admin'),
                           ADMIN_SUBJECT='fixture-admin', SECURITY_URL=self.server.url,
                           TENANTS_API_URL=self.server.url, EVENTSTORE_URL=self.server.url,
                           NO_PROXY='127.0.0.1', no_proxy='127.0.0.1')
        result = subprocess.run(['bash', '-c', FUNCTIONS + '\n' + '''
CONNECT_TIMEOUT=1
REQUEST_TIMEOUT=1
POLL_SECONDS=3
''' + script], input=stdin, text=True, capture_output=True, env=environment, timeout=timeout)
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
        workflow = (ROOT / '.github/workflows/story-guards.yml').read_text()
        self.assertIn('run: python3 tests/scripts/test_bootstrap_handoff.py', workflow)

    def test_actual_token_request_subject_decoder_and_credential_descriptors(self):
        result = self.run_shell('''
LOCAL_PLATFORM_USERNAME='fixture &=username'
LOCAL_PLATFORM_PASSWORD='fixture &=password+'
TOKEN=$(request_token) || exit 10
[[ "$(token_subject "$TOKEN")" == fixture-admin ]] || exit 11
platform_api "$TOKEN" -fsS "$TENANTS_API_URL/api/global-administrators" >/dev/null || exit 12
if token_subject invalid >/dev/null 2>&1; then exit 13; fi
''')
        self.assertEqual(result.stdout, '')
        token_call, api_call = self.server.calls
        self.assertEqual(parse_qs(token_call[2].decode()), {
            'grant_type': ['password'], 'client_id': ['hexalith-eventstore'],
            'username': ['fixture &=username'], 'password': ['fixture &=password+']})
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
        self.run_shell('''
LOCAL_PLATFORM_USERNAME=fixture LOCAL_PLATFORM_PASSWORD=fixture
if request_token >/dev/null; then exit 10; fi
if wait_command_completed "$ADMIN_TOKEN" fixture-correlation; then exit 11; fi
''')

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
''', stdin='fixture-human\nhuman\nfixture-password\n')
        self.assertEqual(len(self.server.payloads), 1)
        command = self.server.payloads[0]
        self.assertEqual((command['tenant'], command['domain'], command['aggregateId'], command['commandType'], command['payload']),
                         ('system', 'global-administrators', 'global-administrators', 'SetGlobalAdministrator', {'UserId': 'fixture-human'}))
        for key in ('messageId', 'correlationId'):
            self.assertRegex(command[key], r'^[0-7][0-9A-HJKMNP-TV-Z]{25}$')
        self.assertTrue(any(call[1] == 'Bearer ' + dummy_token('fixture-human') for call in self.server.calls))

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
        self.assertFalse(any(call[0].endswith('/token') for call in self.server.calls))
        status_calls = [call[0] for call in self.server.calls if '/status/' in call[0]]
        self.assertEqual(status_calls, ['/api/v1/commands/status/' + self.server.payloads[0]['correlationId']])

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

    def test_wrong_configured_identity_or_missing_membership_cannot_grant(self):
        self.run_shell('''
ADMIN_TOKEN=''' + dummy_token('someone-else') + '''
if grant_human_access; then exit 10; fi
[[ -z "${HANDOFF_CORRELATION_ID:-}" ]] || exit 11
''')
        self.server.mode = 'missing'
        self.run_shell('if grant_human_access; then exit 10; fi')
        self.assertEqual(self.server.payloads, [])

    def test_bootstrap_needs_explicit_state_proof_and_preserves_unresolved_handles(self):
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
''')
        self.assertEqual(len(self.server.payloads), 1)
        self.assertEqual(self.server.payloads[0]['payload'], {'UserId': 'fixture-admin'})

    def test_bootstrap_completed_without_projection_stays_unresolved(self):
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
''')
        self.assertEqual(len(self.server.payloads), 1)
        self.assertEqual(sum(data is not None for _, _, data in self.server.calls), 1)
        command = self.server.payloads[0]
        self.assertEqual(result.stdout.splitlines(), [command['messageId'], command['correlationId']])
        self.assertEqual(self.server.status_reads, 1)
        self.assertEqual([call[0] for call in self.server.calls if '/status/' in call[0]],
                         ['/api/v1/commands/status/' + command['correlationId']])

    def test_startup_event_handles_support_read_only_bootstrap_recovery(self):
        message = '01JQK000000000000000009998'
        correlation = '01JQK000000000000000009999'
        self.run_shell('''
retain_startup_attempt || exit 10
resume_bootstrap || exit 11
[[ "$BOOTSTRAP_PROJECTED" == true ]] || exit 12
''', stdin=message + '\n' + correlation + '\n')
        self.assertEqual(self.server.payloads, [])
        self.assertEqual(self.server.calls[0][0], '/api/v1/commands/status/' + correlation)

    def test_wrong_identity_cannot_bootstrap_even_with_operator_state_proof(self):
        self.server.bootstrap = True
        self.run_shell('''
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
ADMIN_TOKEN=''' + dummy_token('someone-else') + '''
if recover_bootstrap; then exit 10; fi
[[ -z "${BOOTSTRAP_MESSAGE_ID:-}" && -z "${BOOTSTRAP_CORRELATION_ID:-}" ]] || exit 11
''')
        self.assertEqual(self.server.calls, [])

    def test_existing_member_cannot_submit_bootstrap(self):
        self.run_shell('''
BOOTSTRAP_UNBOOTSTRAPPED_CONFIRMED=true
if recover_bootstrap; then exit 10; fi
''')
        self.assertEqual(self.server.payloads, [])

    def test_stalled_api_and_token_responses_are_time_bounded(self):
        for mode, invocation in (
                ('api_stall', 'platform_api "$ADMIN_TOKEN" -fsS "$TENANTS_API_URL/api/global-administrators" >/dev/null'),
                ('token_stall', 'LOCAL_PLATFORM_USERNAME=fixture LOCAL_PLATFORM_PASSWORD=fixture request_token >/dev/null'),
                ('status_stall', 'wait_command_completed "$ADMIN_TOKEN" fixture-correlation'),
                ('api_stall', 'wait_admin_membership "$ADMIN_TOKEN" "$ADMIN_SUBJECT"')):
            with self.subTest(mode=mode, invocation=invocation):
                self.server.mode = mode
                started = time.monotonic()
                self.run_shell('POLL_SECONDS=1\nif ' + invocation + '; then exit 10; fi', timeout=5)
                self.assertLess(time.monotonic() - started, 3.5)


if __name__ == '__main__':
    unittest.main()
