import json
import os
from contextlib import contextmanager
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
import subprocess
import sys
import tempfile
import threading
import unittest


ROOT = Path(__file__).resolve().parents[2]
TASK = ROOT / "module/tasks/create-synthetic-tenant.py"
TOKEN = "Bearer secret-operator-token"
BINDING = "secret-catalog-binding"
HEADERS = {"Authorization": TOKEN, "X-Hexalith-Catalog-Binding": BINDING}
MESSAGE_ID = "01ARZ3NDEKTSV4RRFFQ69G5FAW"
CORRELATION_ID = "01ARZ3NDEKTSV4RRFFQ69G5FAX"


@contextmanager
def gateway(submit, statuses=()):
    requests = []
    statuses = iter(statuses)

    class Handler(BaseHTTPRequestHandler):
        def do_POST(self):
            body = json.loads(self.rfile.read(int(self.headers["Content-Length"])))
            self.record(body)
            self.reply(*submit)

        def do_GET(self):
            self.record(None)
            self.reply(*next(statuses, (404, {})))

        def record(self, body):
            requests.append((self.command, self.path, dict(self.headers.items()), body))

        def reply(self, status, body):
            payload = json.dumps(body).encode("utf-8")
            self.send_response(status)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(payload)))
            if status == 307:
                self.send_header("Location", "/untrusted")
            self.end_headers()
            self.wfile.write(payload)

        def log_message(self, format, *args):
            pass

    with ThreadingHTTPServer(("127.0.0.1", 0), Handler) as server:
        worker = threading.Thread(target=server.serve_forever, kwargs={"poll_interval": 0.02})
        worker.start()
        try:
            yield f"http://127.0.0.1:{server.server_port}", requests
        finally:
            server.shutdown()
            worker.join()


def accepted():
    return 202, {"messageId": MESSAGE_ID, "correlationId": CORRELATION_ID}


def status(name, code, **details):
    return 200, {"status": name, "statusCode": code, "messageId": MESSAGE_ID,
                 "correlationId": CORRELATION_ID, **details}


class CreateSyntheticTenantTests(unittest.TestCase):
    def run_task(self, url, tenant="local-ci-1", timeout_seconds=None, **fields):
        invocation = {"syntheticTenantId": tenant, "gatewayUrl": url,
                      "commandTenant": "platform:tenants", "headers": HEADERS, **fields}
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "invocation.json"
            path.write_text(json.dumps(invocation), encoding="utf-8")
            env = {**os.environ, "HEXALITH_TASK_INVOCATION_PATH": str(path)}
            command = [str(TASK)]
            if timeout_seconds is not None:
                command = [sys.executable, "-c",
                           "import runpy, sys; "
                           "sys.exit(runpy.run_path(sys.argv[1])['main'](float(sys.argv[2])))",
                           str(TASK), str(timeout_seconds)]
            result = subprocess.run(command, env=env, capture_output=True, text=True, timeout=10)
        self.assertNotIn(TOKEN, result.stdout + result.stderr)
        self.assertNotIn(BINDING, result.stdout + result.stderr)
        self.assertNotIn("X-Hexalith-Catalog-Binding", result.stdout + result.stderr)
        return result

    def test_submit_shape_and_poll_until_completed_including_no_op(self):
        for count in (1, 0):
            with self.subTest(event_count=count):
                statuses = [(404, {}), status("Processing", 1), status("Completed", 4, eventCount=count)]
                with gateway(accepted(), statuses) as (url, requests):
                    result = self.run_task(url + "/gateway/")
                self.assertEqual(result.returncode, 0, result.stderr)
                self.assertEqual([(m, p) for m, p, _, _ in requests], [
                    ("POST", "/gateway/api/v1/commands"),
                    *[("GET", f"/gateway/api/v1/commands/status/{MESSAGE_ID}")] * 3,
                ])
                body = requests[0][3]
                self.assertRegex(body.pop("messageId"), r"^[0-7][0-9A-HJKMNP-TV-Z]{25}$")
                self.assertEqual(body, {
                    "tenant": "platform:tenants", "domain": "tenants", "aggregateId": "local-ci-1",
                    "commandType": "create-synthetic-tenant", "payload": {"tenantId": "local-ci-1"},
                })
                for _, _, headers, _ in requests:
                    self.assertEqual({key: headers[key] for key in HEADERS}, HEADERS)

    def test_invalid_and_reserved_ids_send_no_request(self):
        with gateway(accepted()) as (url, requests):
            for tenant in ("system", "UPPER", "-tenant", "bad_id", "x" * 65, "alpha\n", None):
                with self.subTest(tenant=tenant):
                    result = self.run_task(url, tenant)
                    self.assertEqual(result.returncode, 2)
                    self.assertIn("SyntheticTenantIdInvalidRejection", result.stderr)
        self.assertEqual(requests, [])

    def test_refused_gateway_url_and_missing_invocation_exit_two_without_request(self):
        with gateway(accepted()) as (url, requests):
            for gateway_url in ("ftp://127.0.0.1", "http://user@127.0.0.1", url + "?x=1", url + "#f",
                                "http://127.0.0.1:0", url + "/ gateway", None):
                with self.subTest(gateway_url=gateway_url):
                    self.assertEqual(self.run_task(url, gatewayUrl=gateway_url).returncode, 2)
            env = {key: value for key, value in os.environ.items() if key != "HEXALITH_TASK_INVOCATION_PATH"}
            result = subprocess.run([str(TASK)], env=env, capture_output=True, text=True, timeout=10)
            self.assertEqual(result.returncode, 2)
        self.assertEqual(requests, [])

    def test_gateway_reported_invalid_id_exits_two(self):
        rejection = "Hexalith.Tenants.Contracts.Events.Rejections.SyntheticTenantIdInvalidRejection"
        for submit, statuses in (
            ((422, {"rejectionType": rejection, "detail": TOKEN}), []),
            (accepted(), [status("Rejected", 5, rejectionEventType=rejection)]),
        ):
            with self.subTest(code=submit[0]), gateway(submit, statuses) as (url, _):
                result = self.run_task(url)
                self.assertEqual(result.returncode, 2)
                self.assertIn("SyntheticTenantIdInvalidRejection", result.stderr)

    def test_retryable_terminal_status_keeps_polling(self):
        retry = status("Rejected", 5, retryable=True,
                       rejectionEventType="Hexalith.Tenants.Contracts.Events.Rejections.TenantAlreadyExistsRejection")
        with gateway(accepted(), [retry, status("Completed", 4)]) as (url, requests):
            result = self.run_task(url)
        self.assertEqual(result.returncode, 0, result.stderr)
        self.assertEqual(sum(method == "GET" for method, *_ in requests), 2)

    def test_bad_invocation_diagnostic_omits_secret(self):
        with gateway(accepted()) as (url, requests):
            result = self.run_task(url, headers={"Bad\nHeader": TOKEN})
        self.assertEqual(result.returncode, 2)
        self.assertEqual(requests, [])

    def test_collision_names_tenant_and_rejection_in_http_and_status_responses(self):
        rejection = "Hexalith.Tenants.Contracts.Events.Rejections.TenantAlreadyExistsRejection"
        for submit, statuses in (
            ((409, {"rejectionType": rejection, "detail": TOKEN}), []),
            (accepted(), [status("Rejected", 5, rejectionEventType=rejection, extra=BINDING)]),
        ):
            with self.subTest(code=submit[0]), gateway(submit, statuses) as (url, _):
                result = self.run_task(url)
                self.assertEqual(result.returncode, 3)
                self.assertIn("local-ci-1: TenantAlreadyExistsRejection", result.stderr)

    def test_no_verified_operator_exits_three(self):
        denial = status("Rejected", 5, rejectionEventType="InsufficientPermissionsRejection")
        with gateway(accepted(), [denial]) as (url, _):
            result = self.run_task(url)
        self.assertEqual(result.returncode, 3)
        self.assertIn("GlobalAdminRequired", result.stderr)
        for code in (401, 403):
            with self.subTest(code=code), gateway((code, {"detail": TOKEN})) as (url, _):
                self.assertEqual(self.run_task(url).returncode, 3)

    def test_gateway_failure_and_unverifiable_status_exit_one_without_printing_details(self):
        for submit, statuses in (
            ((503, {"detail": TOKEN}), []),
            (accepted(), [status("Rejected", 5, failureReason=BINDING)]),
            (accepted(), [status("Completed", 4, messageId="foreign", correlationId="foreign")]),
            (accepted(), [status("PublishFailed", 6, failureReason=TOKEN)]),
            ((202, {"messageId": MESSAGE_ID, "correlationId": CORRELATION_ID, "padding": "x" * 70_000}), []),
        ):
            with self.subTest(code=submit[0], statuses=statuses), gateway(submit, statuses) as (url, _):
                result = self.run_task(url)
                self.assertEqual(result.returncode, 1)

    def test_missing_status_times_out(self):
        with gateway(accepted()) as (url, requests):
            result = self.run_task(url, timeout_seconds=0.2)
        self.assertEqual(result.returncode, 1)
        self.assertIn("Timed out", result.stderr)
        self.assertTrue(any(method == "GET" for method, *_ in requests))

    def test_gateway_redirect_does_not_forward_credentials(self):
        with gateway((307, {})) as (url, requests):
            result = self.run_task(url)
        self.assertEqual(result.returncode, 1)
        self.assertEqual([path for _, path, _, _ in requests], ["/api/v1/commands"])


if __name__ == "__main__":
    unittest.main()
