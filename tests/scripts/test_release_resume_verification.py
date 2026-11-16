"""Distinguishing controls for revision-bound fixture receipts and elapsed-time readiness."""

import hashlib
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
import json
from pathlib import Path
import runpy
import subprocess
import tempfile
import threading
import time
import unittest
from unittest.mock import patch

ROOT = Path(__file__).resolve().parents[2]
VERIFIER = ROOT / '_bmad-output/implementation-artifacts/evidence/fix-builds-sdk-cohort-fixtures-2026-11-15/verify-current.py'
PROBE = ROOT / '_bmad-output/implementation-artifacts/evidence/fix-firefox-route-focus-ci-2026-11-15/verify.sh'


class ReleaseResumeVerificationTests(unittest.TestCase):
    def retained_success(self, module, evidence):
        checks = []
        for name in ('module-build', 'manifest-contracts'):
            log = evidence / (name + '.log')
            log.write_text(name + ' original success\n')
            checks.append({'name': name, 'exit_code': 0, 'command': list(module['COMMANDS'][name]),
                           'log': log.name, 'log_sha256': hashlib.sha256(log.read_bytes()).hexdigest()})
        return {'revision': 'a' * 40, 'source_manifest_sha256': 'digest', 'configuration': 'Debug',
                'verification_recipe_sha256': module['recipe_identity'](), 'checks': checks}

    def test_prior_success_is_reused_only_at_its_exact_revision(self):
        module = runpy.run_path(str(VERIFIER))
        with tempfile.TemporaryDirectory() as temporary:
            evidence = Path(temporary)
            prior = self.retained_success(module, evidence)
            original = json.dumps(prior)
            reuse = lambda record, revision='a' * 40, digest='digest': module['reuse'](record, revision, digest, evidence)
            self.assertEqual(reuse(prior), prior['checks'])
            with self.assertRaisesRegex(ValueError, 'exact previous Builds revision'):
                reuse(prior, revision='b' * 40)
            with self.assertRaisesRegex(ValueError, 'same byte-bound source manifest'):
                reuse(prior, digest='different')
            self.assertEqual(json.dumps(prior), original)
            prior['checks'][1]['exit_code'] = 1
            with self.assertRaisesRegex(ValueError, 'both successful module checks'):
                reuse(prior)

    def test_reuse_requires_exact_debug_recipe_and_module_commands(self):
        module = runpy.run_path(str(VERIFIER))
        with tempfile.TemporaryDirectory() as temporary:
            evidence = Path(temporary)
            prior = self.retained_success(module, evidence)
            changes = [('configuration', 'Release', 'Debug configuration'),
                       ('verification_recipe_sha256', 'old-recipe', 'recipe identity')]
            for field, value, message in changes:
                with self.subTest(field=field):
                    changed = json.loads(json.dumps(prior))
                    changed[field] = value
                    with self.assertRaisesRegex(ValueError, message):
                        module['reuse'](changed, 'a' * 40, 'digest', evidence)
            for name in ('module-build', 'manifest-contracts'):
                changed = json.loads(json.dumps(prior))
                next(check for check in changed['checks'] if check['name'] == name)['command'].append('--different')
                with self.subTest(name=name), self.assertRaisesRegex(ValueError, 'exact expected module commands'):
                    module['reuse'](changed, 'a' * 40, 'digest', evidence)
            del prior['verification_recipe_sha256']
            with self.assertRaisesRegex(ValueError, 'old incomplete records are refused'):
                module['reuse'](prior, 'a' * 40, 'digest', evidence)

    def test_reuse_refuses_missing_changed_external_linked_and_duplicate_logs(self):
        module = runpy.run_path(str(VERIFIER))
        with tempfile.TemporaryDirectory() as temporary:
            evidence = Path(temporary) / 'evidence'
            evidence.mkdir()
            prior = self.retained_success(module, evidence)
            originals = {path.name: path.read_bytes() for path in evidence.iterdir()}
            outside = evidence.parent / 'outside.log'
            outside.write_bytes(b'outside')
            (evidence / 'linked.log').symlink_to(evidence / 'module-build.log')
            (evidence / 'linked-dir').symlink_to(evidence, target_is_directory=True)
            for value in (None, 'missing.log', '../outside.log', str(outside), 'linked.log', 'linked-dir/module-build.log'):
                changed = json.loads(json.dumps(prior))
                changed['checks'][0]['log'] = value
                with self.subTest(log=value), self.assertRaisesRegex(ValueError, 'contained real retained log'):
                    module['reuse'](changed, 'a' * 40, 'digest', evidence)
            changed = json.loads(json.dumps(prior))
            changed['checks'][0]['log_sha256'] = 'wrong'
            with self.assertRaisesRegex(ValueError, 'matching retained log hashes'):
                module['reuse'](changed, 'a' * 40, 'digest', evidence)
            changed = json.loads(json.dumps(prior))
            changed['checks'][1].update({key: changed['checks'][0][key] for key in ('log', 'log_sha256')})
            with self.assertRaisesRegex(ValueError, 'distinct retained logs'):
                module['reuse'](changed, 'a' * 40, 'digest', evidence)
            for name, content in originals.items():
                self.assertEqual((evidence / name).read_bytes(), content)
            (evidence / 'module-build.log').write_bytes(b'tampered actual log')
            with self.assertRaisesRegex(ValueError, 'matching retained log hashes'):
                module['reuse'](prior, 'a' * 40, 'digest', evidence)

    def test_selection_binds_bytes_and_refuses_staged_and_unstaged_changes(self):
        selection = runpy.run_path(str(VERIFIER))['selection']
        with tempfile.TemporaryDirectory() as temporary:
            owner = Path(temporary)
            (owner / 'src').mkdir()
            source = owner / 'src/input.txt'
            source.write_text('original\n')
            for status in (b'', b' M src/input.txt\n', b'M  src/input.txt\n'):
                with self.subTest(status=status):
                    def git(arguments, **kwargs):
                        return 'a' * 40 if arguments[1] == 'rev-parse' else status if arguments[1] == 'status' else b'src/input.txt\0'
                    with patch('subprocess.check_output', side_effect=git):
                        if status:
                            with self.assertRaisesRegex(ValueError, 'tracked source must be clean'):
                                selection(owner)
                        else:
                            revision, inputs = selection(owner)
                            self.assertEqual(revision, 'a' * 40)
                            self.assertEqual(inputs, {'src/input.txt': hashlib.sha256(source.read_bytes()).hexdigest()})

    def test_source_changes_during_a_child_cannot_replace_the_previous_receipt(self):
        module = runpy.run_path(str(VERIFIER))
        main = module['main']
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            owner = root / 'owner'
            owner.mkdir()
            (owner / 'input.txt').write_text('original')
            evidence = root / 'evidence'
            evidence.mkdir()
            receipt = evidence / 'current-verification.json'
            receipt.write_text('{"revision":"original-attribution","checks":[{"log":"module-build.log"}]}')
            previous_receipt = receipt.read_bytes()
            previous_log = evidence / 'module-build.log'
            previous_log.write_bytes(b'original successful build log\n')
            selected = ('a' * 40, {'input.txt': hashlib.sha256(b'original').hexdigest()})
            globals_ = main.__globals__
            with patch.dict(globals_, OWNER=owner, EVIDENCE=evidence), \
                 patch.dict('os.environ', TMPDIR=str(root)), \
                 patch.dict(globals_, selection=lambda path: selected if not (owner / 'changed').exists() else ('b' * 40, {})), \
                 patch('subprocess.run', side_effect=lambda *args, **kwargs: (kwargs['stdout'].write('refused attempt log\n') and (owner / 'changed').touch()) or subprocess.CompletedProcess(args[0], 0)), \
                 patch('sys.argv', [str(VERIFIER)]):
                with self.assertRaisesRegex(ValueError, 'source changed during verification'):
                    main()
            self.assertEqual(receipt.read_bytes(), previous_receipt)
            self.assertEqual(previous_log.read_bytes(), b'original successful build log\n')
            attempts = list((evidence / 'attempts').glob('*/module-build.log'))
            self.assertEqual(len(attempts), 1)
            self.assertEqual(attempts[0].read_text(), 'refused attempt log\n')
            refused = json.loads(attempts[0].with_suffix('.json').read_text())
            self.assertFalse(refused['source_stable'])
            self.assertEqual(evidence / refused['log'], attempts[0])
            self.assertEqual(refused['log_sha256'], hashlib.sha256(attempts[0].read_bytes()).hexdigest())

    def test_owner_is_derived_from_the_repository_relative_artifact_location(self):
        module = runpy.run_path(str(VERIFIER))
        self.assertEqual(module['OWNER'], ROOT / 'references/Hexalith.Builds')
        self.assertNotIn('/home/administrator/', VERIFIER.read_text())

    def test_successful_checks_bind_unique_attempt_logs_without_replacing_historical_logs(self):
        main = runpy.run_path(str(VERIFIER))['main']
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            owner = root / 'owner'
            owner.mkdir()
            (owner / 'input.txt').write_text('original')
            evidence = root / 'evidence'
            evidence.mkdir()
            old_log = evidence / 'module-build.log'
            old_log.write_text('historical successful output')
            original_receipt = b'{"historical":"successful original receipt"}\n'
            (evidence / 'current-verification.json').write_bytes(original_receipt)
            selected = ('a' * 40, {'input.txt': hashlib.sha256(b'original').hexdigest()})
            def child(arguments, **kwargs):
                kwargs['stdout'].write('new check output\n')
                return subprocess.CompletedProcess(arguments, 0)
            with patch.dict(main.__globals__, OWNER=owner, EVIDENCE=evidence, selection=lambda path: selected), \
                 patch.dict('os.environ', TMPDIR=str(root)), patch('subprocess.run', side_effect=child), \
                 patch('sys.argv', [str(VERIFIER)]):
                self.assertEqual(main(), 0)
            self.assertEqual(old_log.read_text(), 'historical successful output')
            archives = list((evidence / 'attempts').glob('*/prior-verification.json'))
            self.assertEqual(len(archives), 1)
            self.assertEqual(archives[0].read_bytes(), original_receipt)
            receipt = json.loads((evidence / 'current-verification.json').read_text())
            self.assertEqual(len(receipt['checks']), 4)
            for check in receipt['checks']:
                log = evidence / check['log']
                self.assertEqual(log.read_text(), 'new check output\n')
                self.assertEqual(check['log_sha256'], hashlib.sha256(log.read_bytes()).hexdigest())
                self.assertIn('attempts/', check['log'])

    def test_real_slow_headers_redirects_and_late_responses_obey_the_overall_deadline(self):
        source = PROBE.read_text().split("<<'PY'\n", 1)[1].split('\nPY', 1)[0]
        module = {'__name__': 'probe_test'}
        exec(compile(source, str(PROBE), 'exec'), module)
        wait = module['wait_for_host']
        stop = threading.Event()
        class Handler(BaseHTTPRequestHandler):
            def do_GET(self):
                try:
                    if self.path.startswith('/slow-header'):
                        self.wfile.write(b'HTTP/1.1 200 OK\r\n')
                        for index in range(30):
                            if stop.wait(0.025):
                                return
                            self.wfile.write(f'X-Slow-{index}: value\r\n'.encode())
                            self.wfile.flush()
                        self.wfile.write(b'Content-Length: 0\r\n\r\n')
                    elif self.path.startswith('/redirect'):
                        if stop.wait(0.04):
                            return
                        index = int(self.path.split('=')[-1]) + 1 if '=' in self.path else 1
                        self.send_response(302)
                        self.send_header('Location', '/redirect?index=' + str(index))
                        self.end_headers()
                    else:
                        if self.path == '/late' and stop.wait(0.5):
                            return
                        self.send_response(200)
                        self.end_headers()
                except (BrokenPipeError, ConnectionResetError):
                    pass
            def log_message(self, *args):
                pass
        server = ThreadingHTTPServer(('127.0.0.1', 0), Handler)
        thread = threading.Thread(target=lambda: server.serve_forever(poll_interval=0.01))
        thread.start()
        children = []
        real_popen = subprocess.Popen
        def owned_popen(*args, **kwargs):
            child = real_popen(*args, **kwargs)
            children.append(child)
            return child
        try:
            with patch('subprocess.Popen', side_effect=owned_popen):
                wait(f'http://127.0.0.1:{server.server_port}/ready', timeout=2)
                for path in ('slow-header', 'redirect', 'late'):
                    with self.subTest(path=path):
                        started = time.monotonic()
                        with self.assertRaises(TimeoutError):
                            wait(f'http://127.0.0.1:{server.server_port}/{path}', timeout=0.2)
                        self.assertLess(time.monotonic() - started, 0.6)
                        self.assertTrue(all(child.poll() is not None for child in children))
        finally:
            stop.set()
            server.shutdown()
            server.server_close()
            thread.join(timeout=2)
        self.assertFalse(thread.is_alive())
        self.assertTrue(all(child.poll() is not None for child in children))

    def test_a_success_reported_after_the_deadline_is_not_accepted(self):
        source = PROBE.read_text().split("<<'PY'\n", 1)[1].split('\nPY', 1)[0]
        module = {'__name__': 'probe_test'}
        exec(compile(source, str(PROBE), 'exec'), module)
        with patch('time.monotonic', side_effect=[0, 0.01, 0.3, 0.3, 0.3]), \
             patch('time.sleep'), patch('subprocess.run', return_value=subprocess.CompletedProcess([], 0)):
            with self.assertRaises(TimeoutError):
                module['wait_for_host']('http://localhost', timeout=0.2)


if __name__ == '__main__':
    unittest.main()
